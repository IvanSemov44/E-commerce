using ECommerce.Inventory.Domain.Aggregates.InventoryItem;
using ECommerce.Inventory.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ECommerce.Inventory.Infrastructure.Persistence.Configurations;

public class InventoryItemConfiguration : IEntityTypeConfiguration<InventoryItem>
{
    public void Configure(EntityTypeBuilder<InventoryItem> builder)
    {
        builder.ToTable("InventoryItems");
        builder.HasKey(i => i.Id);

        builder.OwnsOne(i => i.Stock, stock =>
        {
            stock.Property(s => s.Quantity)
                 .HasColumnName("Quantity")
                 .IsRequired();
        });

        builder.Property(i => i.ProductId).IsRequired();
        builder.Property(i => i.LowStockThreshold).IsRequired();
        builder.Property(i => i.TrackInventory).IsRequired();

        builder.HasIndex(i => i.ProductId).IsUnique();

        // OwnsMany prevents EF convention from creating a second relationship for
    // The public Log property is a projection of _logEntries and must be ignored so EF
    // does not attempt to create a second relationship navigation alongside OwnsMany.
    builder.Ignore(i => i.Log);

    // OwnsMany prevents EF convention from creating a second relationship for
        // InventoryLog.InventoryItemId (which caused duplicate shadow FK "InventoryItemId1"
        // and DbUpdateConcurrencyException when saving).  Owned types are excluded from
        // convention-based relationship discovery.
        builder.OwnsMany<InventoryLog>("_logEntries", log =>
        {
            // Use a dedicated table to avoid collisions with legacy Core.InventoryLog mapping.
            log.ToTable("InventoryItemLogs");
            log.HasKey(l => l.Id);
            log.WithOwner().HasForeignKey(l => l.InventoryItemId);
            log.Property(l => l.InventoryItemId).IsRequired();
            log.Property(l => l.Delta).IsRequired();
            log.Property(l => l.Reason).IsRequired().HasMaxLength(500);
            log.Property(l => l.StockAfter).IsRequired();
            log.Property(l => l.OccurredAt).IsRequired();
        });
    }
}
