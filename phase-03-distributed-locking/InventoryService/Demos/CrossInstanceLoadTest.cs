// EF Core context factory.
using InventoryService.Data;
// Lock abstractions and providers.
using InventoryService.Locking;
// Service-layer types.
using InventoryService.Services;
// EF Core query extensions.
using Microsoft.EntityFrameworkCore;

// Namespace for demo runners.
namespace InventoryService.Demos;

// Demo runner that simulates two API instances.
public sealed class CrossInstanceLoadTest
{
    // Factory for creating DbContext instances.
    private readonly IDbContextFactory<AppDbContext> _dbFactory;
    // Shared Redis lock provider across simulated instances.
    private readonly RedisDistributedLockProvider _redisLockProvider;
    // Logger factory to give each instance its own logger.
    private readonly ILoggerFactory _loggerFactory;
    // Logger for the demo runner.
    private readonly ILogger<CrossInstanceLoadTest> _logger;

    // Inject dependencies.
    public CrossInstanceLoadTest(
        IDbContextFactory<AppDbContext> dbFactory,
        RedisDistributedLockProvider redisLockProvider,
        ILoggerFactory loggerFactory,
        ILogger<CrossInstanceLoadTest> logger)
    {
        _dbFactory = dbFactory;
        _redisLockProvider = redisLockProvider;
        _loggerFactory = loggerFactory;
        _logger = logger;
    }

    // Runs a cross-instance workload that alternates between two instances.
    public async Task<LoadTestResult> RunAsync(
        // SKU to test.
        string sku,
        // Starting inventory quantity.
        int initialQuantity,
        // Quantity per reservation.
        int reservationQuantity,
        // Number of concurrent requests.
        int requestCount,
        // Reservation mode.
        ReservationMode mode,
        // Cancellation token.
        CancellationToken cancellationToken)
    {
        // Correlation id shared across the batch.
        var correlationId = Guid.NewGuid().ToString("N");
        // Request id prefix for the batch.
        var requestId = "demo-cross-instance";

        // Emit a start log line with correlation fields.
        _logger.LogInformation(
            "Cross-instance test start. service={Service} operation={Operation} correlationId={CorrelationId} requestId={RequestId} entityId={EntityId} demoMode={DemoMode} result=start",
            "inventory-service",
            "cross-instance",
            correlationId,
            requestId,
            sku,
            mode.ToString());

        // Seed the inventory before the test.
        await SeedAsync(sku, initialQuantity, cancellationToken);

        // Simulate two separate app instances.
        var instanceA = CreateInstance(new InProcessDistributedLockProvider());
        // Each instance uses its own local lock provider.
        var instanceB = CreateInstance(new InProcessDistributedLockProvider());

        // Create one task per request.
        var tasks = Enumerable.Range(0, requestCount)
            .Select(index => Task.Run(() =>
            {
                // Alternate which instance handles the request.
                var instance = index % 2 == 0 ? instanceA : instanceB;
                // Enterprise scenario: one node hits a GC pause and exceeds the lock lease.
                var simulateExpiry = (mode is ReservationMode.RedisUnsafe or ReservationMode.RedisFenced)
                    && index % 2 == 0;

                // Build per-request context.
                var context = new RequestContext(
                    correlationId,
                    $"{requestId}-{index}",
                    mode.ToString(),
                    simulateExpiry);

                // Execute the reservation on the chosen instance.
                return instance.ReserveAsync(
                    sku,
                    reservationQuantity,
                    mode,
                    context,
                    cancellationToken);
            }, cancellationToken))
            // Materialize to an array to start execution.
            .ToArray();

        // Wait for all requests to complete.
        var results = await Task.WhenAll(tasks);
        // Count successes and failures.
        var successCount = results.Count(result => result.Success);
        var failureCount = results.Length - successCount;

        // Read final inventory state.
        var finalItem = await GetItemAsync(sku, cancellationToken);

        // Emit a completion log line.
        _logger.LogInformation(
            "Cross-instance test complete. service={Service} operation={Operation} correlationId={CorrelationId} requestId={RequestId} entityId={EntityId} demoMode={DemoMode} result=complete",
            "inventory-service",
            "cross-instance",
            correlationId,
            requestId,
            sku,
            mode.ToString());

        // Return the summary of this run.
        return new LoadTestResult(
            mode.ToString(),
            requestCount,
            successCount,
            failureCount,
            finalItem?.Quantity ?? 0);
    }

    // Creates a reservation service instance with its own local lock provider.
    private InventoryReservationService CreateInstance(IDistributedLockProvider localLockProvider)
    {
        return new InventoryReservationService(
            _dbFactory,
            localLockProvider,
            _redisLockProvider,
            _loggerFactory.CreateLogger<InventoryReservationService>());
    }

    // Seeds the database for the demo run.
    private async Task SeedAsync(string sku, int quantity, CancellationToken cancellationToken)
    {
        // Enterprise scenario: database is seeded before a traffic burst across instances.
        await using var db = await _dbFactory.CreateDbContextAsync(cancellationToken);
        // Look up any existing row for the SKU.
        var item = await db.InventoryItems.SingleOrDefaultAsync(
            existing => existing.Sku == sku,
            cancellationToken);

        // Insert or reset the item.
        if (item is null)
        {
            // Create a new row when missing.
            db.InventoryItems.Add(new InventoryService.Models.InventoryItem
            {
                Sku = sku,
                Quantity = quantity,
                Version = 0,
                LastFencingToken = 0
            });
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

    // Reads the final inventory state after the run.
    private async Task<InventoryService.Models.InventoryItem?> GetItemAsync(
        // SKU to fetch.
        string sku,
        // Cancellation token.
        CancellationToken cancellationToken)
    {
        // Create a new DbContext for the read.
        await using var db = await _dbFactory.CreateDbContextAsync(cancellationToken);
        // Query without tracking for read-only usage.
        return await db.InventoryItems.AsNoTracking()
            .SingleOrDefaultAsync(item => item.Sku == sku, cancellationToken);
    }
}
