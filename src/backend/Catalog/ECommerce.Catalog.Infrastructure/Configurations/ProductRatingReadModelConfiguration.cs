using ECommerce.Catalog.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ECommerce.Catalog.Infrastructure.Configurations;

public class ProductRatingReadModelConfiguration : IEntityTypeConfiguration<ProductRatingReadModel>
{
    public void Configure(EntityTypeBuilder<ProductRatingReadModel> builder)
    {
        builder.HasNoKey();
        builder.ToTable("Reviews", "public");
        builder.Property(x => x.ProductId).HasColumnName("ProductId");
        builder.Property(x => x.Rating).HasColumnName("Rating");
    }
}
