using ECommerce.Inventory.Domain.Aggregates.InventoryItem;
using Microsoft.EntityFrameworkCore;
using ECommerce.Inventory.Infrastructure.Persistence.Configurations;

namespace ECommerce.Inventory.Infrastructure.Persistence;

public class InventoryDbContext(DbContextOptions<InventoryDbContext> options) : DbContext(options)
{
    public DbSet<InventoryItem> InventoryItems => Set<InventoryItem>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.HasDefaultSchema("inventory");
        modelBuilder.ApplyConfiguration(new InventoryItemConfiguration());
        modelBuilder.Entity<InventoryItem>().ToTable("InventoryItems");
    }
}
