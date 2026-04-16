using ECommerce.Reviews.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ECommerce.Reviews.Infrastructure.Persistence.Configurations;

public class ProductReadModelConfiguration : IEntityTypeConfiguration<ProductReadModel>
{
    public void Configure(EntityTypeBuilder<ProductReadModel> builder)
    {
        builder.ToTable("ReviewProductProjections");

        builder.HasKey(product => product.Id);

        builder.Property(product => product.Id)
            .HasColumnName("Id")
            .IsRequired();

        builder.Property(product => product.IsActive)
            .HasColumnName("IsActive")
            .IsRequired();

        builder.Property(product => product.UpdatedAt)
            .HasColumnName("UpdatedAt")
            .IsRequired();
    }
}
