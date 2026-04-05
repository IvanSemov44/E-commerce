using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ECommerce.Promotions.Domain.Aggregates.PromoCode;
using ECommerce.Promotions.Domain.Enums;
using ECommerce.Promotions.Domain.ValueObjects;

namespace ECommerce.Promotions.Infrastructure.Persistence.Configurations;

public class PromoCodeConfiguration : IEntityTypeConfiguration<PromoCode>
{
    public void Configure(EntityTypeBuilder<PromoCode> builder)
    {
        builder.ToTable("PromoCodes");
        builder.HasKey(p => p.Id);

        builder.Property(p => p.RowVersion)
               .IsRowVersion()
               .IsConcurrencyToken();

              builder.Property(p => p.Code)
                     .HasColumnName("Code")
                     .HasMaxLength(50)
                     .HasConversion(
                            value => value.Value,
                            value => PromoCodeString.Reconstitute(value))
                     .IsRequired();

              builder.HasIndex(p => p.Code).IsUnique();

        builder.ComplexProperty(p => p.Discount, db =>
        {
            db.Property(d => d.Type)
              .HasColumnName("DiscountType")
              .HasConversion<string>()
              .IsRequired();
            db.Property(d => d.Amount)
              .HasColumnName("DiscountValue")
              .HasPrecision(18, 2)
              .IsRequired();
        });

        builder.ComplexProperty(p => p.ValidPeriod, vb =>
        {
            vb.Property(v => v.Start)
              .HasColumnName("StartDate");
            vb.Property(v => v.End)
              .HasColumnName("EndDate");
        });

        builder.Property(p => p.MaxUses)
               .HasColumnName("MaxUses");

        builder.Property(p => p.UsedCount)
               .HasColumnName("UsedCount")
               .IsRequired();

        builder.Property(p => p.IsActive)
               .HasColumnName("IsActive")
               .IsRequired();

        builder.Property(p => p.MinimumOrderAmount)
               .HasColumnName("MinOrderAmount")
               .HasPrecision(18, 2);

        builder.Property(p => p.MaxDiscountAmount)
               .HasColumnName("MaxDiscountAmount")
               .HasPrecision(18, 2);

        builder.Property(p => p.CreatedAt)
               .HasColumnName("CreatedAt");

        builder.Property(p => p.UpdatedAt)
               .HasColumnName("UpdatedAt");
    }
}
