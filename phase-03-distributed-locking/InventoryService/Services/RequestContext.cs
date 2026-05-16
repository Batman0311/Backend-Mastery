// Namespace for Phase 3 service-layer types.
namespace InventoryService.Services;

// Per-request metadata used by demos and logs.
public sealed record RequestContext(
    // Correlates a batch of related requests.
    string CorrelationId,
    // Unique identifier for a specific request.
    string RequestId,
    // Demo mode string used for tagging logs/metrics.
    string DemoMode,
    // When true, simulate a lock-expiry timing window.
    bool SimulateExpiry = false);
