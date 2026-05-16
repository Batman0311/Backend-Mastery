// Namespace for Phase 3 service-layer types.
namespace InventoryService.Services;

// Outcome for a reservation or release attempt.
public sealed record ReservationResult(
	// True when the reservation succeeded.
	bool Success,
	// Human-readable status for demos.
	string Message,
	// Optional remaining quantity on success/failure.
	int? RemainingQuantity = null);
