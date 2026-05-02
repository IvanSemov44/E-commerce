using ECommerce.Ordering.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ECommerce.Ordering.Infrastructure.Persistence.Configurations;

public sealed class PromoCodeReadModelConfiguration : IEntityTypeConfiguration<PromoCodeReadModel>
{
    public void Configure(EntityTypeBuilder<PromoCodeReadModel> builder)
    {
        builder.ToTable("PromoCodes");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.DiscountValue).HasPrecision(18, 2);
    }
}
