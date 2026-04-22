namespace ECommerce.Identity.Infrastructure.Persistence.Configurations;

public class RefreshTokenEntityConfiguration : IEntityTypeConfiguration<RefreshToken>
{
    public void Configure(EntityTypeBuilder<RefreshToken> builder)
    {
        builder.ToTable("RefreshTokens");
        builder.HasKey(rt => rt.Id);

        builder.HasIndex(rt => rt.Token).IsUnique();

        builder.Property(rt => rt.Token).HasMaxLength(256).IsRequired();
        builder.Property(rt => rt.DeletedAt).IsRequired(false);

        builder.HasQueryFilter(rt => rt.DeletedAt == null);
    }
}
