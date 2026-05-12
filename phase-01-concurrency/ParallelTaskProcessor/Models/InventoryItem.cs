namespace ParallelTaskProcessor.Models;

public sealed class InventoryItem
{
    public int Id { get; set; }

    public string Sku { get; set; } = string.Empty;

    public int Quantity { get; set; }

    public int Version { get; set; }
}
