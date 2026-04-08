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

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        // InventoryLog entries are append-only. In EF InMemory + OwnsMany scenarios,
        // newly added owned entities can be tracked as Modified instead of Added,
        // which causes DbUpdateConcurrencyException ("entity does not exist in store").
        // Normalize to Added before persisting.
        var modifiedLogs = ChangeTracker.Entries<InventoryLog>()
            .Where(e => e.State == EntityState.Modified)
            .ToList();

        foreach (var entry in modifiedLogs)
            entry.State = EntityState.Added;

        return base.SaveChangesAsync(cancellationToken);
    }
}
