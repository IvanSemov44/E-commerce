using ECommerce.Inventory.Domain.Aggregates.InventoryItem;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ECommerce.Inventory.Infrastructure.Persistence.Configurations;

public class InventoryLogConfiguration : IEntityTypeConfiguration<InventoryLog>
{
    public void Configure(EntityTypeBuilder<InventoryLog> builder)
    {
        builder.ToTable("InventoryLogs");
        builder.HasKey(l => l.Id);
        builder.Property(l => l.InventoryItemId).IsRequired();
        builder.Property(l => l.Delta).IsRequired();
        builder.Property(l => l.Reason).IsRequired().HasMaxLength(500);
        builder.Property(l => l.StockAfter).IsRequired();
        builder.Property(l => l.OccurredAt).IsRequired();
    }
}