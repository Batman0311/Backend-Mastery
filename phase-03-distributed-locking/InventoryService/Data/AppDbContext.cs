// EF Core model definitions.
using InventoryService.Models;
// EF Core DbContext base.
using Microsoft.EntityFrameworkCore;

// Namespace for EF Core data access.
namespace InventoryService.Data;

// Database context for inventory data.
public sealed class AppDbContext : DbContext
{
    // Constructor receives options from DI.
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    // Inventory items table.
    public DbSet<InventoryItem> InventoryItems => Set<InventoryItem>();

    // Configure model constraints and concurrency.
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Configure InventoryItem entity.
        modelBuilder.Entity<InventoryItem>(entity =>
        {
            // Unique SKU per row.
            entity.HasIndex(item => item.Sku).IsUnique();
            // Version used for optimistic concurrency.
            entity.Property(item => item.Version).IsConcurrencyToken();
        });
    }
}
