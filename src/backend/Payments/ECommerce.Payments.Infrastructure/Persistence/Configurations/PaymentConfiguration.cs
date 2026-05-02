using ECommerce.Payments.Domain.Aggregates.Payment;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ECommerce.Payments.Infrastructure.Persistence.Configurations;

public sealed class PaymentConfiguration : IEntityTypeConfiguration<Payment>
{
    public void Configure(EntityTypeBuilder<Payment> builder)
    {
        builder.ToTable("Payments");
        builder.HasKey(p => p.Id);

        builder.Property(p => p.OrderId).IsRequired();
        builder.Property(p => p.PaymentMethod).HasMaxLength(50).IsRequired();
        builder.Property(p => p.Amount).HasPrecision(18, 2).IsRequired();
        builder.Property(p => p.Currency).HasMaxLength(3).IsRequired();
        builder.Property(p => p.Status).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(p => p.PaymentIntentId).HasMaxLength(100);
        builder.Property(p => p.TransactionId).HasMaxLength(100);
        builder.Property(p => p.ProcessedAt);
        builder.Property(p => p.FailureReason).HasMaxLength(500);

        builder.HasIndex(p => p.OrderId).IsUnique();
    }
}
