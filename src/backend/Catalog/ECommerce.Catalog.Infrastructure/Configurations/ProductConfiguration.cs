using ECommerce.Catalog.Domain.Aggregates.Product;
using ECommerce.Catalog.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;

namespace ECommerce.Catalog.Infrastructure.Configurations;

public class ProductConfiguration : IEntityTypeConfiguration<Product>
{
    public void Configure(EntityTypeBuilder<Product> builder)
    {
        builder.ToTable("Products");
        builder.HasKey(p => p.Id);

        builder.Property(p => p.Name)
            .HasConversion(n => n.Value, v => ProductName.Create(v).GetDataOrThrow())
            .HasColumnName("Name")
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(p => p.Slug)
            .HasConversion(s => s.Value, v => Slug.Create(v).GetDataOrThrow())
            .HasColumnName("Slug")
            .IsRequired()
            .HasMaxLength(250);

        builder.HasIndex(p => p.Slug).IsUnique();

        builder.OwnsOne(p => p.Price, pb => {
            pb.Property(m => m.Amount).HasColumnName("Price").IsRequired();
            pb.Property(m => m.Currency).HasColumnName("Currency").IsRequired().HasMaxLength(10);
        });

        builder.OwnsOne(p => p.CompareAtPrice, cb => {
            cb.Property(m => m.Amount).HasColumnName("CompareAtPrice");
            cb.Property(m => m.Currency).HasColumnName("CompareAtPriceCurrency");
        });

        // Sku is optional — null means no SKU assigned yet.
        // The converter handles null on both sides so EF doesn't NPE on nullable Sku.
        builder.Property(p => p.Sku)
            .HasConversion(
                s => s == null ? null : s.Value,
                v => v == null ? null : Sku.Create(v).GetDataOrThrow())
            .HasColumnName("Sku")
            .IsRequired(false)
            .HasMaxLength(100);
        builder.HasIndex(p => p.Sku).IsUnique().HasFilter("\"Sku\" IS NOT NULL");

        builder.Property(p => p.Description).HasColumnName("Description").IsRequired(false);

        builder.Property<int>("Status").HasColumnName("Status");
        builder.Property(p => p.IsFeatured).HasColumnName("IsFeatured");
        builder.Property(p => p.IsDeleted).HasColumnName("IsDeleted");

        builder.Property(p => p.CategoryId).HasColumnName("CategoryId");

        // Images as owned collection
        builder.OwnsMany(p => p.Images, ib => {
            ib.ToTable("ProductImages");
            ib.HasKey(i => i.Id);
            ib.Property<Guid>("ProductId").HasColumnName("ProductId");
            ib.Property(i => i.Url).HasMaxLength(2000).IsRequired();
            ib.Property(i => i.AltText).HasMaxLength(500).IsRequired(false);
            ib.Property(i => i.IsPrimary).HasColumnName("IsPrimary");
            ib.Property(i => i.DisplayOrder).HasColumnName("DisplayOrder");
            ib.WithOwner().HasForeignKey("ProductId");
        });

        builder.Property<DateTime>("CreatedAt");
        builder.Property<DateTime>("UpdatedAt");
    }
}
