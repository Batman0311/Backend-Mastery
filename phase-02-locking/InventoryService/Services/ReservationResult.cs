namespace InventoryService.Services;

public sealed record ReservationResult(bool Success, string Message, int? RemainingQuantity = null);
