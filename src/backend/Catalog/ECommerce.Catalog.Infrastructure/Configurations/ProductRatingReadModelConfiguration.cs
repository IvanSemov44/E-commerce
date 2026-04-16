using ECommerce.Catalog.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ECommerce.Catalog.Infrastructure.Configurations;

public class ProductRatingReadModelConfiguration : IEntityTypeConfiguration<ProductRatingReadModel>
{
    public void Configure(EntityTypeBuilder<ProductRatingReadModel> builder)
    {
        builder.HasKey(x => x.ProductId);
        builder.ToTable("ProductRatings", "catalog");
        builder.Property(x => x.ProductId).HasColumnName("ProductId");
        builder.Property(x => x.AverageRating).HasColumnName("AverageRating").HasPrecision(5, 2);
        builder.Property(x => x.ReviewCount).HasColumnName("ReviewCount");
        builder.Property(x => x.UpdatedAt).HasColumnName("UpdatedAt");
    }
}
