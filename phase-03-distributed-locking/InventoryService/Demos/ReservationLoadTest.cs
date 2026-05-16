// Service-layer types for running reservations.
using InventoryService.Services;

// Namespace for demo runners.
namespace InventoryService.Demos;

// Demo runner that fires many reservations in one instance.
public sealed class ReservationLoadTest
{
    // Reservation service to call.
    private readonly InventoryReservationService _service;
    // Logger for demo progress.
    private readonly ILogger<ReservationLoadTest> _logger;

    // Inject dependencies.
    public ReservationLoadTest(
        InventoryReservationService service,
        ILogger<ReservationLoadTest> logger)
    {
        _service = service;
        _logger = logger;
    }

    // Runs a concurrent reservation workload.
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
        var requestId = "demo-loadtest";

        // Emit a start log line with correlation fields.
        _logger.LogInformation(
            "Load test start. service={Service} operation={Operation} correlationId={CorrelationId} requestId={RequestId} entityId={EntityId} demoMode={DemoMode} result=start",
            "inventory-service",
            "loadtest",
            correlationId,
            requestId,
            sku,
            mode.ToString());

        // Seed the inventory before running the test.
        await _service.SeedAsync(sku, initialQuantity, cancellationToken);

        // Task.Run uses the thread pool to overlap requests like production traffic bursts.
        var tasks = Enumerable.Range(0, requestCount)
            // Create a task per request.
            .Select(index => Task.Run(() =>
            {
                // Build per-request context.
                var context = new RequestContext(
                    correlationId,
                    $"{requestId}-{index}",
                    mode.ToString());

                // Call the reservation service.
                return _service.ReserveAsync(
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
        var finalItem = await _service.GetItemAsync(sku, cancellationToken);

        // Emit a completion log line.
        _logger.LogInformation(
            "Load test complete. service={Service} operation={Operation} correlationId={CorrelationId} requestId={RequestId} entityId={EntityId} demoMode={DemoMode} result=complete",
            "inventory-service",
            "loadtest",
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
}

// Summary result for a load test run.
public sealed record LoadTestResult(
    // Mode used for the run.
    string Mode,
    // Total number of requests.
    int Requests,
    // Number of successful reservations.
    int SuccessCount,
    // Number of failed reservations.
    int FailureCount,
    // Quantity remaining at the end.
    int FinalQuantity);
