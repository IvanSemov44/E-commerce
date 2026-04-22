namespace ECommerce.Identity.Infrastructure.Persistence.Configurations;

public class AddressEntityConfiguration : IEntityTypeConfiguration<Address>
{
    public void Configure(EntityTypeBuilder<Address> builder)
    {
        builder.ToTable("Addresses");
        builder.HasKey(a => a.Id);

        builder.Property(a => a.Street).HasMaxLength(255).IsRequired();
        builder.Property(a => a.City).HasMaxLength(100).IsRequired();
        builder.Property(a => a.Country).HasMaxLength(2).IsRequired();
        builder.Property(a => a.PostalCode).HasMaxLength(20);

        builder.Property(a => a.IsDefaultShipping).HasDefaultValue(false);
        builder.Property(a => a.IsDefaultBilling).HasDefaultValue(false);
        builder.Property(a => a.DeletedAt).IsRequired(false);

        builder.HasQueryFilter(a => a.DeletedAt == null);
    }
}
