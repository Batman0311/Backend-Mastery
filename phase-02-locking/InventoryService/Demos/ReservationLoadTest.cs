using InventoryService.Services;

namespace InventoryService.Demos;

public sealed class ReservationLoadTest
{
    private readonly InventoryReservationService _service;
    private readonly ILogger<ReservationLoadTest> _logger;

    public ReservationLoadTest(
        InventoryReservationService service,
        ILogger<ReservationLoadTest> logger)
    {
        _service = service;
        _logger = logger;
    }

    public async Task<LoadTestResult> RunAsync(
        string sku,
        int initialQuantity,
        int reservationQuantity,
        int requestCount,
        ReservationMode mode,
        CancellationToken cancellationToken)
    {
        await _service.SeedAsync(sku, initialQuantity, cancellationToken);

        // Task.Run uses the thread pool to overlap requests like production traffic bursts.
        var tasks = Enumerable.Range(0, requestCount)
            .Select(_ => Task.Run(() => _service.ReserveAsync(
                sku,
                reservationQuantity,
                mode,
                cancellationToken), cancellationToken))
            .ToArray();

        var results = await Task.WhenAll(tasks);

        var successCount = results.Count(result => result.Success);
        var failureCount = results.Length - successCount;
        var finalItem = await _service.GetItemAsync(sku, cancellationToken);

        _logger.LogInformation(
            "Load test complete. Mode={Mode} Success={Success} Failure={Failure}",
            mode,
            successCount,
            failureCount);

        return new LoadTestResult(
            mode.ToString(),
            requestCount,
            successCount,
            failureCount,
            finalItem?.Quantity ?? 0);
    }
}

public sealed record LoadTestResult(
    string Mode,
    int Requests,
    int SuccessCount,
    int FailureCount,
    int FinalQuantity);
