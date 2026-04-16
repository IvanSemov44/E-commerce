using ECommerce.Catalog.Domain.Aggregates.Category;
using ECommerce.Catalog.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ECommerce.Catalog.Infrastructure.Configurations;

public class CategoryAggregateConfiguration : IEntityTypeConfiguration<Category>
{
    public void Configure(EntityTypeBuilder<Category> builder)
    {
        builder.ToTable("Categories", "catalog");
        builder.HasKey(c => c.Id);
        builder.HasIndex(c => c.Slug).IsUnique();
        builder.HasIndex(c => c.IsActive);

        builder.Property(c => c.Id).HasColumnName("Id");
        builder.Property(c => c.CreatedAt).HasColumnName("CreatedAt");
        builder.Property(c => c.UpdatedAt).HasColumnName("UpdatedAt");

        builder.Property(c => c.Name)
            .HasColumnName("Name")
            .HasMaxLength(100)
            .IsRequired()
            .HasConversion(
                n => n.Value,
                s => CategoryName.Create(s).GetDataOrThrow());

        builder.Property(c => c.Slug)
            .HasColumnName("Slug")
            .HasMaxLength(100)
            .IsRequired()
            .HasConversion(
                s => s.Value,
                s => Slug.Create(s).GetDataOrThrow());

        builder.Property(c => c.ParentId).HasColumnName("ParentId");
        builder.Property(c => c.IsActive).HasColumnName("IsActive");

        builder.HasOne<Category>()
            .WithMany()
            .HasForeignKey(c => c.ParentId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
