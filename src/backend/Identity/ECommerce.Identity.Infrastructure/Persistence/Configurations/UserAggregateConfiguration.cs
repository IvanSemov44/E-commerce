namespace ECommerce.Identity.Infrastructure.Persistence.Configurations;

public class UserAggregateConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("Users");
        builder.HasKey(u => u.Id);
        builder.HasIndex(u => u.Email).IsUnique();

        builder.Property(u => u.Email).HasMaxLength(255);
        builder.Property(u => u.DeletedAt).IsRequired(false);

        builder.ComplexProperty(u => u.Name, nameBuilder =>
        {
            nameBuilder.Property(n => n.First).HasColumnName("FirstName").HasMaxLength(100).IsRequired();
            nameBuilder.Property(n => n.Last).HasColumnName("LastName").HasMaxLength(100).IsRequired();
        });

        builder.HasMany(u => u.Addresses)
            .WithOne()
            .HasForeignKey("UserId")
            .IsRequired()
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(u => u.RefreshTokens)
            .WithOne()
            .HasForeignKey(rt => rt.UserId)
            .IsRequired()
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasQueryFilter(u => u.DeletedAt == null);
    }
}
