using ECommerce.Catalog.Domain.Aggregates.Category;
using ECommerce.Catalog.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;

namespace ECommerce.Catalog.Infrastructure.Configurations;

public class CategoryConfiguration : IEntityTypeConfiguration<Category>
{
    public void Configure(EntityTypeBuilder<Category> builder)
    {
        builder.ToTable("Categories");
        builder.HasKey(c => c.Id);

        builder.Property(c => c.Name)
            .HasConversion(n => n.Value, v => CategoryName.Create(v).GetDataOrThrow())
            .HasColumnName("Name")
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(c => c.Slug)
            .HasConversion(s => s.Value, v => Slug.Create(v).GetDataOrThrow())
            .HasColumnName("Slug")
            .IsRequired()
            .HasMaxLength(250);
        builder.HasIndex(c => c.Slug).IsUnique();

        builder.Property(c => c.ParentId).HasColumnName("ParentId");
        builder.Property(c => c.IsActive).HasColumnName("IsActive");

        builder.Property<DateTime>("CreatedAt");
        builder.Property<DateTime>("UpdatedAt");
    }
}
