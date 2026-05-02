using ECommerce.Ordering.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ECommerce.Ordering.Infrastructure.Persistence.Configurations;

public sealed class ProductImageReadModelConfiguration : IEntityTypeConfiguration<ProductImageReadModel>
{
    public void Configure(EntityTypeBuilder<ProductImageReadModel> builder)
    {
        builder.ToTable("ProductImages");
        builder.HasKey(x => x.Id);
    }
}
