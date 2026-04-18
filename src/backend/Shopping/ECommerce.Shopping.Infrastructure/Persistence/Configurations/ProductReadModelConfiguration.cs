using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ECommerce.Shopping.Infrastructure.Persistence.Configurations;

public class ProductReadModelConfiguration : IEntityTypeConfiguration<ProductReadModel>
{
    public void Configure(EntityTypeBuilder<ProductReadModel> builder)
    {
        builder.HasKey(x => x.Id);
        builder.ToTable("ProductProjections");
        builder.Property(x => x.Price).HasColumnType("decimal(18,2)");
    }
}
