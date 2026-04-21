using ECommerce.Identity.Domain.Aggregates.User;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ECommerce.Identity.Infrastructure.Persistence.Configurations;

public class RefreshTokenEntityConfiguration : IEntityTypeConfiguration<RefreshToken>
{
    public void Configure(EntityTypeBuilder<RefreshToken> builder)
    {
        builder.ToTable("RefreshTokens");
        builder.HasKey(rt => rt.Id);

        builder.HasIndex(rt => rt.Token).IsUnique();

        builder.Property(rt => rt.Token)
            .HasColumnName("Token")
            .HasMaxLength(256)
            .IsRequired();

        builder.Property(rt => rt.UserId).HasColumnName("UserId").IsRequired();
        builder.Property(rt => rt.ExpiresAt).HasColumnName("ExpiresAt").IsRequired();
        builder.Property(rt => rt.IsRevoked).HasColumnName("IsRevoked").IsRequired();
        builder.Property(rt => rt.RevokedReason).HasColumnName("RevokedReason");
    }
}
