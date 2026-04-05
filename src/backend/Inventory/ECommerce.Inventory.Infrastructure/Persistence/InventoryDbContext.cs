using ECommerce.Inventory.Domain.Aggregates.InventoryItem;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Inventory.Infrastructure.Persistence;

public class InventoryDbContext(DbContextOptions<InventoryDbContext> options) : DbContext(options)
{
    public DbSet<InventoryItem> InventoryItems => Set<InventoryItem>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.HasDefaultSchema("inventory");
        modelBuilder.Entity<InventoryItem>().ToTable("InventoryItems");
    }
}
