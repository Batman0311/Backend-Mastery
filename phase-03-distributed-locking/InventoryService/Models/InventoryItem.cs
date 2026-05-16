// Namespace for EF Core models.
namespace InventoryService.Models;

// Inventory row stored in SQLite.
public sealed class InventoryItem
{
    // Primary key.
    public int Id { get; set; }

    // Business identifier for the item.
    public string Sku { get; set; } = string.Empty;

    // Remaining stock quantity.
    public int Quantity { get; set; }

    // Optimistic concurrency token.
    public int Version { get; set; }

    // Last accepted fencing token for stale-write protection.
    public long LastFencingToken { get; set; }
}
