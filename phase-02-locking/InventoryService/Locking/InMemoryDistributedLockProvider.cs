using System.Collections.Concurrent;

namespace InventoryService.Locking;

public sealed class InMemoryDistributedLockProvider : IDistributedLockProvider
{
    // Demo-only: simulates cross-instance coordination. Replace with Redis/Redlock in production.
    private static readonly ConcurrentDictionary<string, SemaphoreSlim> Locks =
        new(StringComparer.Ordinal);

    public async Task<IDistributedLockHandle?> TryAcquireAsync(
        string resourceKey,
        TimeSpan timeout,
        CancellationToken cancellationToken)
    {
        var semaphore = Locks.GetOrAdd(resourceKey, _ => new SemaphoreSlim(1, 1));
        var acquired = await semaphore.WaitAsync(timeout, cancellationToken);
        if (!acquired)
        {
            return null;
        }

        return new SemaphoreReleaser(semaphore);
    }

    private sealed class SemaphoreReleaser : IDistributedLockHandle
    {
        private readonly SemaphoreSlim _semaphore;

        public SemaphoreReleaser(SemaphoreSlim semaphore)
        {
            _semaphore = semaphore;
        }

        public ValueTask DisposeAsync()
        {
            _semaphore.Release();
            return ValueTask.CompletedTask;
        }
    }
}
