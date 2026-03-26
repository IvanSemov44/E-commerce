using ECommerce.Catalog.Domain.Aggregates.Category;
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

        builder.OwnsOne(c => c.Name, nb => {
            nb.Property(n => n.Value).HasColumnName("Name").IsRequired().HasMaxLength(200);
        });

        builder.OwnsOne(c => c.Slug, sb => {
            sb.Property(s => s.Value).HasColumnName("Slug").IsRequired().HasMaxLength(250);
        });

        builder.HasIndex(c => EF.Property<string>(c, "Slug")).IsUnique();

        builder.Property(c => c.ParentId).HasColumnName("ParentId");
        builder.Property(c => c.IsActive).HasColumnName("IsActive");

        builder.Property<DateTime>("CreatedAt");
        builder.Property<DateTime>("UpdatedAt");
    }
}
