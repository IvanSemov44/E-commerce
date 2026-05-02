using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ECommerce.Payments.Infrastructure.Persistence.Configurations;

public sealed class PaymentOrderReadModelConfiguration : IEntityTypeConfiguration<PaymentOrderReadModel>
{
    public void Configure(EntityTypeBuilder<PaymentOrderReadModel> builder)
    {
        builder.ToTable("PaymentOrders");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Amount).HasPrecision(18, 2).IsRequired();

        builder.HasIndex(x => x.OrderId).IsUnique();
    }
}
