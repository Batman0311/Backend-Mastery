// ActivitySource for tracing spans.
using System.Diagnostics;
// Metrics primitives like Meter and Counter.
using System.Diagnostics.Metrics;
// EF Core context factory.
using InventoryService.Data;
// Lock abstractions and providers.
using InventoryService.Locking;
// EF Core model.
using InventoryService.Models;
// EF Core query/update APIs.
using Microsoft.EntityFrameworkCore;

// Namespace for service-layer types.
namespace InventoryService.Services;

// Core service implementing reservation and release logic.
public sealed class InventoryReservationService
{
    // Service name used in logs and metrics.
    private const string ServiceName = "inventory-service";
    // Activity source for tracing spans.
    private static readonly ActivitySource ActivitySource = new("InventoryService.DistributedLocking");
    // Meter used for emitting metrics.
    private static readonly Meter Meter = new("InventoryService.DistributedLocking");
    // Counter for total requests.
    private static readonly Counter<long> RequestsTotal = Meter.CreateCounter<long>("requests_total");
    // Counter for errors and failures.
    private static readonly Counter<long> ErrorsTotal = Meter.CreateCounter<long>("errors_total");
    // Histogram for request duration.
    private static readonly Histogram<double> RequestDurationMs =
        Meter.CreateHistogram<double>("request_duration_ms");
    // Up/down counter for inflight requests.
    private static readonly UpDownCounter<long> InflightRequests =
        Meter.CreateUpDownCounter<long>("inflight_requests");

    // Factory for creating DbContext instances.
    private readonly IDbContextFactory<AppDbContext> _dbFactory;
    // Local (in-process) lock provider.
    private readonly IDistributedLockProvider _localLockProvider;
    // Redis lock provider for cross-instance coordination.
    private readonly RedisDistributedLockProvider _redisLockProvider;
    // Logger for request flow.
    private readonly ILogger<InventoryReservationService> _logger;

    // Inject dependencies.
    public InventoryReservationService(
        IDbContextFactory<AppDbContext> dbFactory,
        IDistributedLockProvider localLockProvider,
        RedisDistributedLockProvider redisLockProvider,
        ILogger<InventoryReservationService> logger)
    {
        _dbFactory = dbFactory;
        _localLockProvider = localLockProvider;
        _redisLockProvider = redisLockProvider;
        _logger = logger;
    }

    // Seeds inventory for a SKU.
    public async Task SeedAsync(string sku, int quantity, CancellationToken cancellationToken)
    {
        // Production scenario: catalog refresh reseeds inventory for a SKU.
        await using var db = await _dbFactory.CreateDbContextAsync(cancellationToken);
        // Look up any existing row for this SKU.
        var item = await db.InventoryItems.SingleOrDefaultAsync(
            existing => existing.Sku == sku,
            cancellationToken);

        // Insert or reset the row.
        if (item is null)
        {
            // Create a new row when missing.
            item = new InventoryItem
            {
                Sku = sku,
                Quantity = quantity,
                Version = 0,
                LastFencingToken = 0
            };

            // Add the row to the context.
            db.InventoryItems.Add(item);
        }
        else
        {
            // Reset existing row state.
            item.Quantity = quantity;
            item.Version = 0;
            item.LastFencingToken = 0;
        }

        // Persist changes.
        await db.SaveChangesAsync(cancellationToken);
    }

    // Reads an inventory item for a SKU.
    public async Task<InventoryItem?> GetItemAsync(string sku, CancellationToken cancellationToken)
    {
        // Production scenario: read path used by checkout to show availability.
        await using var db = await _dbFactory.CreateDbContextAsync(cancellationToken);
        // Read without tracking for performance.
        return await db.InventoryItems
            .AsNoTracking()
            .SingleOrDefaultAsync(item => item.Sku == sku, cancellationToken);
    }

    // Reserves stock using the selected concurrency strategy.
    public async Task<ReservationResult> ReserveAsync(
        // SKU to reserve.
        string sku,
        // Quantity to reserve.
        int quantity,
        // Reservation mode to execute.
        ReservationMode mode,
        // Request context for logging and metrics.
        RequestContext context,
        // Cancellation token.
        CancellationToken cancellationToken)
    {
        // Production scenario: reservation during high-concurrency order placement.
        var tags = new TagList
        {
            { "service", ServiceName },
            { "operation", "reserve" },
            { "phase", "03" },
            { "demoMode", context.DemoMode }
        };

        // Increment request counters.
        RequestsTotal.Add(1, tags);
        InflightRequests.Add(1, tags);

        // Create a tracing span for the reservation.
        using var activity = ActivitySource.StartActivity("reserve");
        // Attach common tags for tracing.
        activity?.SetTag("service", ServiceName);
        activity?.SetTag("operation", "reserve");
        activity?.SetTag("phase", "03");
        activity?.SetTag("demoMode", context.DemoMode);
        activity?.SetTag("correlationId", context.CorrelationId);
        activity?.SetTag("entityId", sku);

        // Start a stopwatch for latency.
        var stopwatch = Stopwatch.StartNew();

        // Execute the reservation path.
        try
        {
            // Select the mode-specific implementation.
            var result = mode switch
            {
                // Local lock (single-instance).
                ReservationMode.InstanceLock => await ReserveWithLocalLockAsync(
                    sku,
                    quantity,
                    cancellationToken),
                // Local lock used in cross-instance test.
                ReservationMode.CrossInstanceLocal => await ReserveWithLocalLockAsync(
                    sku,
                    quantity,
                    cancellationToken),
                // Redis lock with normal lease time.
                ReservationMode.Redis => await ReserveWithRedisLockAsync(
                    sku,
                    quantity,
                    context,
                    TimeSpan.FromSeconds(2),
                    useFencing: false,
                    cancellationToken),
                // Redis lock with short lease to force expiry.
                ReservationMode.RedisUnsafe => await ReserveWithRedisLockAsync(
                    sku,
                    quantity,
                    context,
                    TimeSpan.FromMilliseconds(200),
                    useFencing: false,
                    cancellationToken),
                // Redis lock with short lease and fencing tokens.
                ReservationMode.RedisFenced => await ReserveWithRedisLockAsync(
                    sku,
                    quantity,
                    context,
                    TimeSpan.FromMilliseconds(200),
                    useFencing: true,
                    cancellationToken),
                // Default to naive (no lock).
                _ => await ReserveNaiveAsync(sku, quantity, cancellationToken)
            };

            // Stop the timer and record latency.
            stopwatch.Stop();
            RequestDurationMs.Record(stopwatch.Elapsed.TotalMilliseconds, tags);

            // Emit warning on failure results.
            if (!result.Success)
            {
                // Increment error counter for failed results.
                ErrorsTotal.Add(1, tags);

                // Log failure result.
                _logger.LogWarning(
                    "Reservation complete. service={Service} operation={Operation} correlationId={CorrelationId} requestId={RequestId} entityId={EntityId} durationMs={DurationMs} result=failure",
                    ServiceName,
                    "reserve",
                    context.CorrelationId,
                    context.RequestId,
                    sku,
                    stopwatch.Elapsed.TotalMilliseconds);

                // Return the failed result.
                return result;
            }

            // Log success result.
            _logger.LogInformation(
                "Reservation complete. service={Service} operation={Operation} correlationId={CorrelationId} requestId={RequestId} entityId={EntityId} durationMs={DurationMs} result=success",
                ServiceName,
                "reserve",
                context.CorrelationId,
                context.RequestId,
                sku,
                stopwatch.Elapsed.TotalMilliseconds);

            // Return the successful result.
            return result;
        }
        // Handle unexpected exceptions.
        catch (Exception ex)
        {
            // Stop the timer and record error.
            stopwatch.Stop();
            ErrorsTotal.Add(1, tags);

            // Log the exception with context.
            _logger.LogError(
                ex,
                "Reservation failed. service={Service} operation={Operation} correlationId={CorrelationId} requestId={RequestId} entityId={EntityId} durationMs={DurationMs} result=error",
                ServiceName,
                "reserve",
                context.CorrelationId,
                context.RequestId,
                sku,
                stopwatch.Elapsed.TotalMilliseconds);

            // Re-throw so callers can see failures.
            throw;
        }
        // Always decrement inflight counter.
        finally
        {
            InflightRequests.Add(-1, tags);
        }
    }

    // Releases previously reserved stock.
    public async Task<ReservationResult> ReleaseAsync(
        // SKU to release.
        string sku,
        // Quantity to release.
        int quantity,
        // Request context for logging.
        RequestContext context,
        // Cancellation token.
        CancellationToken cancellationToken)
    {
        // Production scenario: customer cancels before payment is captured.
        await using var db = await _dbFactory.CreateDbContextAsync(cancellationToken);
        // Read the item with no tracking.
        var item = await db.InventoryItems
            .AsNoTracking()
            .SingleOrDefaultAsync(existing => existing.Sku == sku, cancellationToken);

        // Return not found when SKU is missing.
        if (item is null)
        {
            return new ReservationResult(false, "SKU not found.");
        }

        // Apply an atomic update to increment quantity.
        var updated = await db.Database.ExecuteSqlRawAsync(
            "UPDATE InventoryItems SET Quantity = Quantity + {0}, Version = Version + 1 WHERE Id = {1}",
            quantity,
            item.Id);

        // Return success if the update applied.
        return updated > 0
            ? new ReservationResult(true, "Released stock.", item.Quantity + quantity)
            : new ReservationResult(false, "Release failed.");
    }

    // Naive reservation without a shared lock.
    private async Task<ReservationResult> ReserveNaiveAsync(
        // SKU to reserve.
        string sku,
        // Quantity to reserve.
        int quantity,
        // Cancellation token.
        CancellationToken cancellationToken)
    {
        // Enterprise scenario: flash sale reservation with no cross-instance coordination.
        await using var db = await _dbFactory.CreateDbContextAsync(cancellationToken);
        // Read the row without tracking.
        var item = await db.InventoryItems
            .AsNoTracking()
            .SingleOrDefaultAsync(existing => existing.Sku == sku, cancellationToken);

        // Return not found when SKU is missing.
        if (item is null)
        {
            return new ReservationResult(false, "SKU not found.");
        }

        // Guard against insufficient stock.
        if (item.Quantity < quantity)
        {
            return new ReservationResult(false, "Insufficient stock.", item.Quantity);
        }

        // Calculate the new quantity.
        var newQuantity = item.Quantity - quantity;

        // Timing window: read-modify-write without a shared lock.
        var updated = await db.Database.ExecuteSqlRawAsync(
            "UPDATE InventoryItems SET Quantity = {0}, Version = Version + 1 WHERE Id = {1}",
            newQuantity,
            item.Id);

        // Return success if the update applied.
        return updated > 0
            ? new ReservationResult(true, "Reserved (naive).", newQuantity)
            : new ReservationResult(false, "Reservation failed.");
    }

    // Reservation guarded by a process-local lock.
    private async Task<ReservationResult> ReserveWithLocalLockAsync(
        // SKU to reserve.
        string sku,
        // Quantity to reserve.
        int quantity,
        // Cancellation token.
        CancellationToken cancellationToken)
    {
        // Enterprise scenario: single-node lock that breaks under multi-node deployments.
        await using var handle = await _localLockProvider.TryAcquireAsync(
            $"inventory:{sku}",
            TimeSpan.FromSeconds(2),
            cancellationToken);

        // Return timeout when the lock cannot be acquired.
        if (handle is null)
        {
            return new ReservationResult(false, "Local lock timeout.");
        }

        // Execute the naive reservation while the lock is held.
        return await ReserveNaiveAsync(sku, quantity, cancellationToken);
    }

    // Reservation guarded by a Redis distributed lock.
    private async Task<ReservationResult> ReserveWithRedisLockAsync(
        // SKU to reserve.
        string sku,
        // Quantity to reserve.
        int quantity,
        // Request context with simulate-expiry flag.
        RequestContext context,
        // Lease time for the lock.
        TimeSpan leaseTime,
        // Whether to enforce fencing tokens on write.
        bool useFencing,
        // Cancellation token.
        CancellationToken cancellationToken)
    {
        // Enterprise scenario: multiple API instances coordinate via Redis.
        await using var handle = await _redisLockProvider.TryAcquireAsync(
            $"inventory:{sku}",
            leaseTime,
            cancellationToken);

        // Return timeout when the lock cannot be acquired.
        if (handle is null)
        {
            return new ReservationResult(false, "Redis lock timeout.");
        }

        // Simulate lock expiry for unsafe modes.
        if (context.SimulateExpiry)
        {
            // Timing window: lock expires before the reservation write completes.
            await Task.Delay(leaseTime + TimeSpan.FromMilliseconds(50), cancellationToken);
        }

        // Choose between fenced and naive write paths.
        return useFencing
            ? await ReserveWithFencingTokenAsync(sku, quantity, handle.FencingToken, cancellationToken)
            : await ReserveNaiveAsync(sku, quantity, cancellationToken);
    }

    // Reservation that enforces fencing token ordering.
    private async Task<ReservationResult> ReserveWithFencingTokenAsync(
        // SKU to reserve.
        string sku,
        // Quantity to reserve.
        int quantity,
        // Fencing token from the lock.
        long fencingToken,
        // Cancellation token.
        CancellationToken cancellationToken)
    {
        // Enterprise scenario: long-running reservation flow needs write ordering.
        await using var db = await _dbFactory.CreateDbContextAsync(cancellationToken);
        // Read the row without tracking.
        var item = await db.InventoryItems
            .AsNoTracking()
            .SingleOrDefaultAsync(existing => existing.Sku == sku, cancellationToken);

        // Return not found when SKU is missing.
        if (item is null)
        {
            return new ReservationResult(false, "SKU not found.");
        }

        // Guard against insufficient stock.
        if (item.Quantity < quantity)
        {
            return new ReservationResult(false, "Insufficient stock.", item.Quantity);
        }

        // Calculate the new quantity.
        var newQuantity = item.Quantity - quantity;

        // Timing window: stale owners are rejected by the fencing token check.
        var updated = await db.Database.ExecuteSqlRawAsync(
            "UPDATE InventoryItems SET Quantity = {0}, Version = Version + 1, LastFencingToken = {1} WHERE Id = {2} AND LastFencingToken < {1}",
            newQuantity,
            fencingToken,
            item.Id);

        // Return success if the token check passes.
        return updated > 0
            ? new ReservationResult(true, "Reserved (fenced).", newQuantity)
            : new ReservationResult(false, "Stale lock owner rejected.");
    }
}
