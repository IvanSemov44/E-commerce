using ECommerce.Catalog.Domain.Aggregates.Product;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ECommerce.Catalog.Infrastructure.Configurations;

public class ProductImageAggregateConfiguration : IEntityTypeConfiguration<ProductImage>
{
    public void Configure(EntityTypeBuilder<ProductImage> builder)
    {
        builder.ToTable("ProductImages", "catalog");
        builder.HasKey(pi => pi.Id);
        builder.Property(pi => pi.Id).HasColumnName("Id");
        builder.Property(pi => pi.ProductId).HasColumnName("ProductId").IsRequired();
        builder.Property(pi => pi.Url).HasColumnName("Url").HasMaxLength(2000).IsRequired();
        builder.Property(pi => pi.AltText).HasColumnName("AltText").HasMaxLength(500).IsRequired(false);
        builder.Property(pi => pi.IsPrimary).HasColumnName("IsPrimary").IsRequired();
        builder.Property(pi => pi.DisplayOrder).HasColumnName("SortOrder").IsRequired();
    }
}
