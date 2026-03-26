using ECommerce.Catalog.Domain.Aggregates.Product;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ECommerce.Catalog.Infrastructure.Configurations;

public class ProductImageConfiguration : IEntityTypeConfiguration<ProductImage>
{
    public void Configure(EntityTypeBuilder<ProductImage> builder)
    {
        builder.ToTable("ProductImages");
        builder.HasKey(pi => pi.Id);
        builder.Property(pi => pi.ProductId).IsRequired();
        builder.Property(pi => pi.Url).HasMaxLength(2000).IsRequired();
        builder.Property(pi => pi.AltText).HasMaxLength(500).IsRequired(false);
        builder.Property(pi => pi.IsPrimary).IsRequired();
        builder.Property(pi => pi.DisplayOrder).IsRequired();
    }
}
