using InventoryService.Data;
using InventoryService.Locking;
using InventoryService.Models;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace InventoryService.Services;

public sealed class InventoryReservationService
{
    private const int MaxOptimisticRetries = 5;
    private readonly AppDbContext _db;
    private readonly IDistributedLockProvider _lockProvider;
    private readonly ILogger<InventoryReservationService> _logger;
    private readonly string _connectionString;

    public InventoryReservationService(
        AppDbContext db,
        IDistributedLockProvider lockProvider,
        IConfiguration configuration,
        ILogger<InventoryReservationService> logger)
    {
        _db = db;
        _lockProvider = lockProvider;
        _logger = logger;
        _connectionString = configuration.GetConnectionString("InventoryDb")
            ?? "Data Source=inventory.db";
    }

    public async Task SeedAsync(string sku, int quantity, CancellationToken cancellationToken)
    {
        var item = await _db.InventoryItems.SingleOrDefaultAsync(
            existing => existing.Sku == sku,
            cancellationToken);

        if (item is null)
        {
            item = new InventoryItem
            {
                Sku = sku,
                Quantity = quantity,
                Version = 0
            };

            _db.InventoryItems.Add(item);
        }
        else
        {
            item.Quantity = quantity;
            item.Version = 0;
        }

        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task<InventoryItem?> GetItemAsync(string sku, CancellationToken cancellationToken)
    {
        return await _db.InventoryItems
            .AsNoTracking()
            .SingleOrDefaultAsync(item => item.Sku == sku, cancellationToken);
    }

    public Task<ReservationResult> ReserveAsync(
        string sku,
        int quantity,
        ReservationMode mode,
        CancellationToken cancellationToken)
    {
        return mode switch
        {
            ReservationMode.Optimistic => ReserveOptimisticAsync(sku, quantity, cancellationToken),
            ReservationMode.Pessimistic => ReservePessimisticAsync(sku, quantity, cancellationToken),
            ReservationMode.Distributed => ReserveDistributedAsync(sku, quantity, cancellationToken),
            _ => ReserveNaiveAsync(sku, quantity, cancellationToken)
        };
    }

    public async Task<ReservationResult> ReleaseAsync(
        string sku,
        int quantity,
        CancellationToken cancellationToken)
    {
        // Enterprise scenario: customer cancels an order after reserve but before payment capture.
        var item = await _db.InventoryItems
            .AsNoTracking()
            .SingleOrDefaultAsync(existing => existing.Sku == sku, cancellationToken);

        if (item is null)
        {
            return new ReservationResult(false, "SKU not found.");
        }

        var updated = await _db.Database.ExecuteSqlRawAsync(
            "UPDATE InventoryItems SET Quantity = Quantity + {0}, Version = Version + 1 WHERE Id = {1}",
            quantity,
            item.Id);

        return updated > 0
            ? new ReservationResult(true, "Released stock.", item.Quantity + quantity)
            : new ReservationResult(false, "Release failed.");
    }

    private async Task<ReservationResult> ReserveNaiveAsync(
        string sku,
        int quantity,
        CancellationToken cancellationToken)
    {
        // Enterprise scenario: flash sale reservation with no concurrency control.
        var item = await _db.InventoryItems
            .AsNoTracking()
            .SingleOrDefaultAsync(existing => existing.Sku == sku, cancellationToken);

        if (item is null)
        {
            return new ReservationResult(false, "SKU not found.");
        }

        if (item.Quantity < quantity)
        {
            return new ReservationResult(false, "Insufficient stock.", item.Quantity);
        }

        var newQuantity = item.Quantity - quantity;

        // Timing window: read-modify-write without any concurrency guard.
        var updated = await _db.Database.ExecuteSqlRawAsync(
            "UPDATE InventoryItems SET Quantity = {0}, Version = Version + 1 WHERE Id = {1}",
            newQuantity,
            item.Id);

        return updated > 0
            ? new ReservationResult(true, "Reserved (naive).", newQuantity)
            : new ReservationResult(false, "Reservation failed.");
    }

    private async Task<ReservationResult> ReserveOptimisticAsync(
        string sku,
        int quantity,
        CancellationToken cancellationToken)
    {
        // Enterprise scenario: read-heavy inventory service that retries on conflicts.
        for (var attempt = 1; attempt <= MaxOptimisticRetries; attempt++)
        {
            var item = await _db.InventoryItems
                .SingleOrDefaultAsync(existing => existing.Sku == sku, cancellationToken);

            if (item is null)
            {
                return new ReservationResult(false, "SKU not found.");
            }

            if (item.Quantity < quantity)
            {
                return new ReservationResult(false, "Insufficient stock.", item.Quantity);
            }

            item.Quantity -= quantity;
            item.Version += 1;

            try
            {
                // Optimistic locking: EF Core verifies the original Version before updating.
                await _db.SaveChangesAsync(cancellationToken);
                return new ReservationResult(true, "Reserved (optimistic).", item.Quantity);
            }
            catch (DbUpdateConcurrencyException)
            {
                // Conflict detected by the version check; retry with fresh state.
                foreach (var entry in _db.ChangeTracker.Entries())
                {
                    entry.State = EntityState.Detached;
                }

                if (attempt == MaxOptimisticRetries)
                {
                    return new ReservationResult(false, "Conflict after retries.");
                }

                var delay = TimeSpan.FromMilliseconds(50 * attempt);
                await Task.Delay(delay, cancellationToken);
            }
        }

        return new ReservationResult(false, "Conflict after retries.");
    }

    private async Task<ReservationResult> ReservePessimisticAsync(
        string sku,
        int quantity,
        CancellationToken cancellationToken)
    {
        // Enterprise scenario: hot SKU guarded by a pessimistic lock to serialize writers.
        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        try
        {
            // SQLite uses database-level write locks, so BEGIN IMMEDIATE stands in for row locking.
            // This blocks other writers until the transaction is committed or rolled back.
            await ExecuteNonQueryAsync(connection, "BEGIN IMMEDIATE;", cancellationToken);

            var row = await ReadItemAsync(connection, sku, cancellationToken);
            if (row is null)
            {
                await ExecuteNonQueryAsync(connection, "ROLLBACK;", cancellationToken);
                return new ReservationResult(false, "SKU not found.");
            }

            if (row.Quantity < quantity)
            {
                await ExecuteNonQueryAsync(connection, "ROLLBACK;", cancellationToken);
                return new ReservationResult(false, "Insufficient stock.", row.Quantity);
            }

            var newQuantity = row.Quantity - quantity;
            await UpdateQuantityAsync(connection, row.Id, newQuantity, cancellationToken);
            await ExecuteNonQueryAsync(connection, "COMMIT;", cancellationToken);

            return new ReservationResult(true, "Reserved (pessimistic).", newQuantity);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Pessimistic reservation failed.");
            await ExecuteNonQueryAsync(connection, "ROLLBACK;", cancellationToken);
            throw;
        }
    }

    private async Task<ReservationResult> ReserveDistributedAsync(
        string sku,
        int quantity,
        CancellationToken cancellationToken)
    {
        // Enterprise scenario: multiple API instances coordinate via a distributed lock.
        await using var handle = await _lockProvider.TryAcquireAsync(
            $"inventory:{sku}",
            TimeSpan.FromSeconds(2),
            cancellationToken);

        if (handle is null)
        {
            return new ReservationResult(false, "Distributed lock timeout.");
        }

        return await ReserveNaiveAsync(sku, quantity, cancellationToken);
    }

    private static async Task<InventoryRow?> ReadItemAsync(
        SqliteConnection connection,
        string sku,
        CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT Id, Quantity FROM InventoryItems WHERE Sku = $sku;";
        command.Parameters.AddWithValue("$sku", sku);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken))
        {
            return null;
        }

        return new InventoryRow(reader.GetInt32(0), reader.GetInt32(1));
    }

    private static async Task UpdateQuantityAsync(
        SqliteConnection connection,
        int id,
        int newQuantity,
        CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.CommandText =
            "UPDATE InventoryItems SET Quantity = $quantity, Version = Version + 1 WHERE Id = $id;";
        command.Parameters.AddWithValue("$quantity", newQuantity);
        command.Parameters.AddWithValue("$id", id);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private static async Task ExecuteNonQueryAsync(
        SqliteConnection connection,
        string sql,
        CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = sql;
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private sealed record InventoryRow(int Id, int Quantity);
}
