using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ECommerce.Shopping.Infrastructure.Persistence.Configurations;

public class InventoryItemReadModelConfiguration : IEntityTypeConfiguration<InventoryItemReadModel>
{
    public void Configure(EntityTypeBuilder<InventoryItemReadModel> builder)
    {
        builder.HasKey(x => x.ProductId);
        builder.ToTable("InventoryStockProjections");
    }
}
