using ECommerce.Catalog.Domain.Aggregates.Product;
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

        builder.OwnsOne(p => p.Name, nb => {
            nb.Property(n => n.Value).HasColumnName("Name").IsRequired().HasMaxLength(200);
        });

        builder.OwnsOne(p => p.Slug, sb => {
            sb.Property(s => s.Value).HasColumnName("Slug").IsRequired().HasMaxLength(250);
        });

        // Index on slug (unique)
        builder.HasIndex(p => EF.Property<string>(p, "Slug")).IsUnique();

        builder.OwnsOne(p => p.Price, pb => {
            pb.Property(m => m.Amount).HasColumnName("Price").IsRequired();
            pb.Property(m => m.Currency).HasColumnName("Currency").IsRequired().HasMaxLength(10);
        });

        builder.OwnsOne(p => p.CompareAtPrice, cb => {
            cb.Property(m => m.Amount).HasColumnName("CompareAtPrice");
            cb.Property(m => m.Currency).HasColumnName("CompareAtPriceCurrency");
        });

        builder.OwnsOne(p => p.Sku, kb => {
            kb.Property(s => s.Value).HasColumnName("Sku").IsRequired().HasMaxLength(100);
        });

        // Unique index on SKU
        builder.HasIndex(p => EF.Property<string>(p, "Sku")).IsUnique();

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
