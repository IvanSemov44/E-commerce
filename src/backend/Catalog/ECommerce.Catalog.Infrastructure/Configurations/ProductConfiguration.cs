using ECommerce.SharedKernel.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ECommerce.Catalog.Infrastructure.Configurations;

public class ProductConfiguration : IEntityTypeConfiguration<Product>
{
    public void Configure(EntityTypeBuilder<Product> builder)
    {
        builder.ToTable("Products");
        builder.HasKey(p => p.Id);
        builder.HasIndex(p => p.Slug).IsUnique();
        builder.HasIndex(p => p.IsActive);
        builder.HasIndex(p => p.IsFeatured);
        builder.HasIndex(p => new { p.IsActive, p.Price });

        builder.Property(p => p.Name)
            .HasColumnName("Name")
            .IsRequired()
            .HasMaxLength(255);

        builder.Property(p => p.Slug)
            .HasColumnName("Slug")
            .IsRequired()
            .HasMaxLength(255);

        builder.Property(p => p.Price).HasColumnName("Price").HasPrecision(10, 2);
        builder.Property(p => p.CompareAtPrice).HasColumnName("CompareAtPrice").HasPrecision(10, 2);
        builder.Property(p => p.CostPrice).HasColumnName("CostPrice").HasPrecision(10, 2);

        builder.Property(p => p.Sku)
            .HasColumnName("Sku")
            .IsRequired(false)
            .HasMaxLength(100);
        builder.HasIndex(p => p.Sku).IsUnique().HasFilter("\"Sku\" IS NOT NULL");

        builder.Property(p => p.ShortDescription).HasColumnName("ShortDescription").HasMaxLength(500).IsRequired(false);
        builder.Property(p => p.Description).HasColumnName("Description").IsRequired(false);
        builder.Property(p => p.Barcode).HasColumnName("Barcode").HasMaxLength(128).IsRequired(false);
        builder.Property(p => p.MetaTitle).HasColumnName("MetaTitle").HasMaxLength(255).IsRequired(false);
        builder.Property(p => p.MetaDescription).HasColumnName("MetaDescription").HasMaxLength(2000).IsRequired(false);
        builder.Property(p => p.Weight).HasColumnName("Weight");
        builder.Property(p => p.LowStockThreshold).HasColumnName("LowStockThreshold");
        builder.Property(p => p.StockQuantity).HasColumnName("StockQuantity");

        builder.Property(p => p.RowVersion).IsRowVersion();

        builder.Property(p => p.CategoryId).HasColumnName("CategoryId");
        builder.Property(p => p.IsActive).HasColumnName("IsActive");
        builder.Property(p => p.IsFeatured).HasColumnName("IsFeatured");

        builder.HasOne(p => p.Category)
            .WithMany(c => c.Products)
            .HasForeignKey(p => p.CategoryId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
