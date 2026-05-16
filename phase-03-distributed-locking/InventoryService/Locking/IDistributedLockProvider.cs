// Namespace for lock abstractions used in Phase 3 demos.
namespace InventoryService.Locking;

// Abstraction for acquiring a distributed lock.
public interface IDistributedLockProvider
{
    // Attempts to acquire a lock for a resource within the lease time.
    Task<IDistributedLockHandle?> TryAcquireAsync(
        // Resource key used to scope the lock.
        string resourceKey,
        // Lease time for the lock before expiry.
        TimeSpan leaseTime,
        // Cancellation token for cooperative cancellation.
        CancellationToken cancellationToken);
}

// Represents an acquired lock that must be released.
public interface IDistributedLockHandle : IAsyncDisposable
{
    // Monotonic token used to detect stale owners.
    long FencingToken { get; }
    // Identifier for the lock owner.
    string OwnerId { get; }
}
