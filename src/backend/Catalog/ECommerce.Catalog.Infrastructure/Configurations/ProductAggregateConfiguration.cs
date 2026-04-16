using ECommerce.Catalog.Domain.Aggregates.Category;
using ECommerce.Catalog.Domain.Aggregates.Product;
using ECommerce.Catalog.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ECommerce.Catalog.Infrastructure.Configurations;

public class ProductAggregateConfiguration : IEntityTypeConfiguration<Product>
{
    public void Configure(EntityTypeBuilder<Product> builder)
    {
        builder.ToTable("Products", "catalog");
        builder.HasKey(p => p.Id);

        builder.HasIndex(p => p.Slug).IsUnique();
        builder.HasIndex(p => p.IsFeatured);
        builder.HasIndex("Status", nameof(Product.Price));

        builder.Property(p => p.Id).HasColumnName("Id");
        builder.Property(p => p.CreatedAt).HasColumnName("CreatedAt");
        builder.Property(p => p.UpdatedAt).HasColumnName("UpdatedAt");

        builder.Property(p => p.Name)
            .HasColumnName("Name")
            .HasMaxLength(255)
            .IsRequired()
            .HasConversion(
                n => n.Value,
                s => ProductName.Create(s).GetDataOrThrow());

        builder.Property(p => p.Slug)
            .HasColumnName("Slug")
            .HasMaxLength(255)
            .IsRequired()
            .HasConversion(
                s => s.Value,
                s => Slug.Create(s).GetDataOrThrow());

        builder.Property(p => p.Price)
            .HasColumnName("Price")
            .HasPrecision(10, 2)
            .IsRequired()
            .HasConversion(
                m => m.Amount,
                v => Money.Create(v, "USD").GetDataOrThrow());

        builder.Property(p => p.CompareAtPrice)
            .HasColumnName("CompareAtPrice")
            .HasPrecision(10, 2)
            .HasConversion(
                m => m != null ? (decimal?)m.Amount : null,
                v => v != null ? Money.Create(v.Value, "USD").GetDataOrThrow() : null);

        builder.Property(p => p.Sku)
            .HasColumnName("Sku")
            .HasMaxLength(100)
            .IsRequired(false)
            .HasConversion(
                s => s != null ? s.Value : (string?)null,
                s => !string.IsNullOrWhiteSpace(s) ? Sku.Create(s).GetDataOrThrow() : null);

        builder.HasIndex(p => p.Sku).IsUnique().HasFilter("\"Sku\" IS NOT NULL");

        builder.Property(p => p.Description).HasColumnName("Description").IsRequired(false);
        builder.Property(p => p.IsFeatured).HasColumnName("IsFeatured");
        builder.Property(p => p.StockQuantity).HasColumnName("StockQuantity");
        // Legacy schema column retained in migrations; send 0 explicitly on inserts without exposing it in the domain model.
        builder.Property<int>("LowStockThreshold").HasColumnName("LowStockThreshold").ValueGeneratedNever();
        builder.Property(p => p.CategoryId).HasColumnName("CategoryId");

        // Persist full lifecycle states (Draft/Active/Inactive/Discontinued).
        builder.Property(p => p.Status)
            .HasColumnName("Status")
            .HasConversion<string>()
            .HasMaxLength(32)
            .IsRequired();

        builder.HasOne<Category>()
            .WithMany()
            .HasForeignKey(p => p.CategoryId)
            .OnDelete(DeleteBehavior.Restrict);

        // Optimistic concurrency — shadow property, not exposed on the domain.
        builder.Property<byte[]>("RowVersion").IsRowVersion().HasColumnName("RowVersion");

        // Images navigation uses the private backing field.
        builder.HasMany(p => p.Images)
            .WithOne()
            .HasForeignKey(pi => pi.ProductId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(p => p.Images)
            .HasField("_images")
            .UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
