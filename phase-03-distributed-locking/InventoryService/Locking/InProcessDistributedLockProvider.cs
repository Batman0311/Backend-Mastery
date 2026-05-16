// Concurrent collections for per-key semaphores.
using System.Collections.Concurrent;

// Namespace for lock implementations.
namespace InventoryService.Locking;

// In-process lock provider used to show cross-instance failure.
public sealed class InProcessDistributedLockProvider : IDistributedLockProvider
{
    // Demo-only: process-local locks do not coordinate across instances.
    private readonly ConcurrentDictionary<string, SemaphoreSlim> _locks =
        new(StringComparer.Ordinal);
    // Stable owner id for this provider instance.
    private readonly string _ownerId = $"local-{Guid.NewGuid():N}";

    // Attempts to acquire a process-local semaphore for the resource.
    public async Task<IDistributedLockHandle?> TryAcquireAsync(
        // Resource key used to select the semaphore.
        string resourceKey,
        // Lease time used as a timeout for waiting.
        TimeSpan leaseTime,
        // Cancellation token for the wait.
        CancellationToken cancellationToken)
    {
        // One semaphore per resource key.
        var semaphore = _locks.GetOrAdd(resourceKey, _ => new SemaphoreSlim(1, 1));
        // Wait for the semaphore using the lease time as a timeout.
        var acquired = await semaphore.WaitAsync(leaseTime, cancellationToken);
        // Return null on timeout.
        if (!acquired)
        {
            return null;
        }

        // Wrap the semaphore in a releaser handle.
        return new SemaphoreReleaser(semaphore, _ownerId);
    }

    // Releases the semaphore when disposed.
    private sealed class SemaphoreReleaser : IDistributedLockHandle
    {
        // Underlying semaphore.
        private readonly SemaphoreSlim _semaphore;
        // Owner id used for logging.
        private readonly string _ownerId;

        // Initializes the releaser with the semaphore and owner.
        public SemaphoreReleaser(SemaphoreSlim semaphore, string ownerId)
        {
            _semaphore = semaphore;
            _ownerId = ownerId;
        }

        // Local locks do not provide fencing tokens.
        public long FencingToken => 0;

        // Expose the owner id.
        public string OwnerId => _ownerId;

        // Release the semaphore on disposal.
        public ValueTask DisposeAsync()
        {
            _semaphore.Release();
            return ValueTask.CompletedTask;
        }
    }
}
