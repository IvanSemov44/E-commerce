using ECommerce.Ordering.Domain.Aggregates.Order;
using ECommerce.Ordering.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ECommerce.Ordering.Infrastructure.Persistence.Configurations;

public sealed class OrderConfiguration : IEntityTypeConfiguration<Order>
{
    public void Configure(EntityTypeBuilder<Order> builder)
    {
        builder.ToTable("Orders");
        builder.HasKey(o => o.Id);

        builder.Property(o => o.OrderNumber).HasMaxLength(60).IsRequired();
        builder.Property(o => o.UserId).IsRequired();
        builder.Property(o => o.PromoCodeId);
        builder.Property(o => o.Subtotal).HasPrecision(18, 2).IsRequired();
        builder.Property(o => o.DiscountAmount).HasPrecision(18, 2).IsRequired();
        builder.Property(o => o.ShippingCost).HasPrecision(18, 2).IsRequired();
        builder.Property(o => o.TaxAmount).HasPrecision(18, 2).IsRequired();
        builder.Property(o => o.Total).HasPrecision(18, 2).IsRequired();

        builder.Property(o => o.Status)
            .HasConversion(
                s => s.Name,
                name => OrderStatus.FromName(name))
            .HasMaxLength(20)
            .IsRequired();

        builder.OwnsOne(o => o.ShippingAddress, sa =>
        {
            sa.Property(a => a.Street).HasColumnName("ShippingAddress_Street").HasMaxLength(200).IsRequired();
            sa.Property(a => a.City).HasColumnName("ShippingAddress_City").HasMaxLength(100).IsRequired();
            sa.Property(a => a.Country).HasColumnName("ShippingAddress_Country").HasMaxLength(100).IsRequired();
            sa.Property(a => a.PostalCode).HasColumnName("ShippingAddress_PostalCode").HasMaxLength(20);
        });

        builder.OwnsOne(o => o.Payment, p =>
        {
            p.Property(pi => pi.PaymentReference).HasColumnName("Payment_Reference").HasMaxLength(200).IsRequired();
            p.Property(pi => pi.PaymentMethod).HasColumnName("Payment_Method").HasMaxLength(50).IsRequired();
            p.Property(pi => pi.PaidAmount).HasColumnName("Payment_Amount").HasPrecision(18, 2).IsRequired();
            p.Property(pi => pi.PaidAt).HasColumnName("Payment_PaidAt").IsRequired();
        });

        builder.Ignore(o => o.Items);

        builder.HasMany<OrderItem>("_items")
            .WithOne()
            .HasForeignKey(i => i.OrderId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation("_items").AutoInclude(true);

        builder.Property(o => o.DeletedAt);
        builder.HasQueryFilter(o => o.DeletedAt == null);

        builder.HasIndex(o => o.UserId);
        builder.HasIndex(o => o.OrderNumber).IsUnique();
    }
}
