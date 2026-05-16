// Namespace for Phase 3 service-layer types.
namespace InventoryService.Services;

// Supported demo modes for reservations.
public enum ReservationMode
{
    // No lock or concurrency control.
    Naive,
    // In-process lock only.
    InstanceLock,
    // Cross-instance test using local locks.
    CrossInstanceLocal,
    // Redis lock with normal lease.
    Redis,
    // Redis lock with short lease to force expiry.
    RedisUnsafe,
    // Redis lock with short lease and fencing tokens.
    RedisFenced
}

// Parses text mode values from API requests.
public static class ReservationModeParser
{
    // Attempts to parse a mode string into the enum.
    public static bool TryParse(string? value, out ReservationMode mode)
    {
        // Default to Naive if parsing fails.
        mode = ReservationMode.Naive;

        // Reject empty or whitespace input.
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        // Normalize input so "redis-unsafe" maps to "RedisUnsafe".
        var normalized = value.Replace("-", string.Empty).Replace("_", string.Empty);
        // Case-insensitive enum parse.
        return Enum.TryParse(normalized, true, out mode);
    }
}
