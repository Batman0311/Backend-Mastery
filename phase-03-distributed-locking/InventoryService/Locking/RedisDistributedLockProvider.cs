// Redis client abstractions.
using StackExchange.Redis;

// Namespace for lock implementations.
namespace InventoryService.Locking;

// Redis-backed distributed lock provider with fencing tokens.
public sealed class RedisDistributedLockProvider : IDistributedLockProvider
{
    // Lua script to release a lock only if the owner id matches.
    private const string ReleaseScript =
        "if redis.call('GET', KEYS[1]) == ARGV[1] then return redis.call('DEL', KEYS[1]) else return 0 end";

    // Shared Redis connection.
    private readonly IConnectionMultiplexer _redis;
    // Owner id for this process instance.
    private readonly string _ownerId = $"redis-{Environment.MachineName}-{Guid.NewGuid():N}";

    // Creates the provider with a Redis connection.
    public RedisDistributedLockProvider(IConnectionMultiplexer redis)
    {
        _redis = redis;
    }

    // Attempts to acquire a Redis lock and returns a handle on success.
    public async Task<IDistributedLockHandle?> TryAcquireAsync(
        // Resource key used to derive Redis keys.
        string resourceKey,
        // Lease time for the lock.
        TimeSpan leaseTime,
        // Cancellation token for cooperative cancellation.
        CancellationToken cancellationToken)
    {
        // Resolve the Redis database.
        var db = _redis.GetDatabase();
        // Lock key stores the owner id.
        var lockKey = (RedisKey)$"lock:{resourceKey}";
        // Fence key stores the monotonic fencing token.
        var fenceKey = (RedisKey)$"fence:{resourceKey}";

        // Fencing tokens are monotonic and can skip values on failed attempts.
        var fencingToken = await db.StringIncrementAsync(fenceKey);
        // Owner id + token is used as the lock value.
        var lockValue = (RedisValue)$"{_ownerId}:{fencingToken}";

        // SET NX PX equivalent: only acquire if the key is absent.
        var acquired = await db.StringSetAsync(lockKey, lockValue, leaseTime, When.NotExists);
        // Return null on lock contention.
        if (!acquired)
        {
            return null;
        }

        // Return a handle that can safely release the lock.
        return new RedisLockHandle(db, lockKey, lockValue, fencingToken, _ownerId);
    }

    // Releases a Redis lock if the owner id matches.
    private sealed class RedisLockHandle : IDistributedLockHandle
    {
        // Redis database used for release.
        private readonly IDatabase _db;
        // Lock key to delete.
        private readonly RedisKey _lockKey;
        // Expected lock value for safe release.
        private readonly RedisValue _lockValue;
        // Prevent double release.
        private bool _released;

        // Creates a handle bound to a specific lock value.
        public RedisLockHandle(
            IDatabase db,
            RedisKey lockKey,
            RedisValue lockValue,
            long fencingToken,
            string ownerId)
        {
            _db = db;
            _lockKey = lockKey;
            _lockValue = lockValue;
            FencingToken = fencingToken;
            OwnerId = ownerId;
        }

        // Fencing token assigned on acquisition.
        public long FencingToken { get; }

        // Owner id that acquired the lock.
        public string OwnerId { get; }

        // Releases the lock only if the stored value matches.
        public async ValueTask DisposeAsync()
        {
            // Avoid releasing twice.
            if (_released)
            {
                return;
            }

            _released = true;
            // Lua script ensures only the owner can delete the key.
            await _db.ScriptEvaluateAsync(ReleaseScript, new RedisKey[] { _lockKey }, new RedisValue[] { _lockValue });
        }
    }
}
