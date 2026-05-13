using System.Threading;

namespace InventoryService.Demos;

public sealed class InMemoryLockingDemo
{
    private const int MaxOptimisticRetries = 5;
    private readonly ILogger<InMemoryLockingDemo> _logger;

    public InMemoryLockingDemo(ILogger<InMemoryLockingDemo> logger)
    {
        _logger = logger;
    }

    public async Task<InMemoryDemoResult> RunOptimisticAsync(
        int initialQuantity,
        int requestCount,
        CancellationToken cancellationToken)
    {
        // Enterprise scenario: single-node hot counter with optimistic concurrency on a version.
        long state = ((long)0 << 32) | (uint)initialQuantity;
        var successCount = 0;

        var tasks = Enumerable.Range(0, requestCount)
            .Select(_ => Task.Run(() =>
            {
                for (var attempt = 1; attempt <= MaxOptimisticRetries; attempt++)
                {
                    var snapshot = Volatile.Read(ref state);
                    var version = (int)(snapshot >> 32);
                    var quantity = (int)(snapshot & 0xFFFFFFFF);

                    if (quantity <= 0)
                    {
                        return;
                    }

                    var updated = ((long)(version + 1) << 32) | (uint)(quantity - 1);

                    // Compare-and-swap on the packed state to detect conflicts.
                    if (Interlocked.CompareExchange(ref state, updated, snapshot) == snapshot)
                    {
                        Interlocked.Increment(ref successCount);
                        return;
                    }
                }
            }, cancellationToken))
            .ToArray();

        await Task.WhenAll(tasks);

        var finalState = Volatile.Read(ref state);
        var finalQuantity = (int)(finalState & 0xFFFFFFFF);
        var failureCount = requestCount - successCount;

        _logger.LogInformation(
            "In-memory optimistic demo complete. Success={Success} Failure={Failure}",
            successCount,
            failureCount);

        return new InMemoryDemoResult(
            "optimistic",
            requestCount,
            successCount,
            failureCount,
            finalQuantity);
    }

    public async Task<InMemoryDemoResult> RunPessimisticAsync(
        int initialQuantity,
        int requestCount,
        CancellationToken cancellationToken)
    {
        // Enterprise scenario: single-node reservation serializes updates with a lock.
        var gate = new object();
        var quantity = initialQuantity;
        var successCount = 0;

        var tasks = Enumerable.Range(0, requestCount)
            .Select(_ => Task.Run(() =>
            {
                lock (gate)
                {
                    if (quantity <= 0)
                    {
                        return;
                    }

                    quantity -= 1;
                    successCount += 1;
                }
            }, cancellationToken))
            .ToArray();

        await Task.WhenAll(tasks);

        var failureCount = requestCount - successCount;

        _logger.LogInformation(
            "In-memory pessimistic demo complete. Success={Success} Failure={Failure}",
            successCount,
            failureCount);

        return new InMemoryDemoResult(
            "pessimistic",
            requestCount,
            successCount,
            failureCount,
            quantity);
    }
}

public sealed record InMemoryDemoResult(
    string Mode,
    int Requests,
    int SuccessCount,
    int FailureCount,
    int FinalQuantity);
