namespace InventoryService.Locking;

public interface IDistributedLockProvider
{
    Task<IDistributedLockHandle?> TryAcquireAsync(
        string resourceKey,
        TimeSpan timeout,
        CancellationToken cancellationToken);
}

public interface IDistributedLockHandle : IAsyncDisposable
{
}
