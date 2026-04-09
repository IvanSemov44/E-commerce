using ECommerce.SharedKernel.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ECommerce.Catalog.Infrastructure.Configurations;

public class CategoryConfiguration : IEntityTypeConfiguration<Category>
{
    public void Configure(EntityTypeBuilder<Category> builder)
    {
        builder.ToTable("Categories");
        builder.HasKey(c => c.Id);
        builder.HasIndex(c => c.Slug).IsUnique();

        builder.Property(c => c.Name)
            .HasColumnName("Name")
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(c => c.Slug)
            .HasColumnName("Slug")
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(c => c.Description).HasColumnName("Description").HasMaxLength(1000).IsRequired(false);
        builder.Property(c => c.ImageUrl).HasColumnName("ImageUrl").HasMaxLength(2000).IsRequired(false);
        builder.Property(c => c.SortOrder).HasColumnName("SortOrder");

        builder.Property(c => c.ParentId).HasColumnName("ParentId");
        builder.Property(c => c.IsActive).HasColumnName("IsActive");

        builder.HasOne(c => c.Parent)
            .WithMany(c => c.Children)
            .HasForeignKey(c => c.ParentId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
