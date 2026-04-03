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

        builder.HasMany<InventoryLog>("_logEntries")
               .WithOne()
               .HasForeignKey(l => l.InventoryItemId)
               .OnDelete(DeleteBehavior.Cascade);
    }
}