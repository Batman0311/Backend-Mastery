using Microsoft.EntityFrameworkCore;
using ParallelTaskProcessor.Models;

namespace ParallelTaskProcessor.Data;

public sealed class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    public DbSet<InventoryItem> InventoryItems => Set<InventoryItem>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<InventoryItem>(entity =>
        {
            entity.HasKey(item => item.Id);
            entity.Property(item => item.Sku).IsRequired();
            entity.Property(item => item.Quantity).IsRequired();
            entity.Property(item => item.Version).IsConcurrencyToken();
        });
    }
}
