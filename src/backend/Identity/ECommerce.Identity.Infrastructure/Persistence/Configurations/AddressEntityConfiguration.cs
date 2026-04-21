using ECommerce.Identity.Domain.Aggregates.User;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ECommerce.Identity.Infrastructure.Persistence.Configurations;

public class AddressEntityConfiguration : IEntityTypeConfiguration<Address>
{
    public void Configure(EntityTypeBuilder<Address> builder)
    {
        builder.ToTable("Addresses");
        builder.HasKey(a => a.Id);

        builder.Property(a => a.Street)
            .HasColumnName("StreetLine1")
            .HasMaxLength(255)
            .IsRequired();

        builder.Property(a => a.City)
            .HasColumnName("City")
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(a => a.Country)
            .HasColumnName("Country")
            .HasMaxLength(2)
            .IsRequired();

        builder.Property(a => a.PostalCode)
            .HasColumnName("PostalCode")
            .HasMaxLength(20);

        builder.Property(a => a.IsDefaultShipping)
            .HasColumnName("IsDefault")
            .IsRequired();

        builder.Property(a => a.IsDefaultBilling)
            .HasColumnName("IsDefaultBilling")
            .IsRequired()
            .HasDefaultValue(false);

        // Legacy columns — not part of the domain model; retained in DB to avoid data loss.
        builder.Property<string>("Type").HasColumnName("Type").HasMaxLength(50).IsRequired(false);
        builder.Property<string>("FirstName").HasColumnName("FirstName").HasMaxLength(100).IsRequired(false);
        builder.Property<string>("LastName").HasColumnName("LastName").HasMaxLength(100).IsRequired(false);
        builder.Property<string>("State").HasColumnName("State").HasMaxLength(100).IsRequired(false);
    }
}
