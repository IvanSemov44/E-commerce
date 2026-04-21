using ECommerce.Identity.Domain.Aggregates.User;
using ECommerce.Identity.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ECommerce.Identity.Infrastructure.Persistence.Configurations;

public class UserAggregateConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("Users");
        builder.HasKey(u => u.Id);

        builder.HasIndex(u => u.Email).IsUnique();

        builder.Property(u => u.Email)
            .HasColumnName("Email")
            .HasMaxLength(255)
            .IsRequired()
            .HasConversion(
                e => e.Value,
                v => Email.Create(v).GetDataOrThrow());

        builder.OwnsOne(u => u.Name, nameBuilder =>
        {
            nameBuilder.Property(n => n.First)
                .HasColumnName("FirstName")
                .HasMaxLength(100)
                .IsRequired();
            nameBuilder.Property(n => n.Last)
                .HasColumnName("LastName")
                .HasMaxLength(100)
                .IsRequired();
        });

        builder.Property(u => u.PasswordHash)
            .HasColumnName("PasswordHash")
            .IsRequired()
            .HasConversion(
                p => p.Hash,
                v => PasswordHash.FromHash(v).GetDataOrThrow());

        builder.Property(u => u.PhoneNumber).HasColumnName("Phone");
        builder.Property(u => u.Role).HasColumnName("Role").HasConversion<int>();
        builder.Property(u => u.IsEmailVerified).HasColumnName("IsEmailVerified").IsRequired();
        builder.Property(u => u.EmailVerificationToken).HasColumnName("EmailVerificationToken");
        builder.Property(u => u.PasswordResetToken).HasColumnName("PasswordResetToken");
        builder.Property(u => u.PasswordResetExpiry).HasColumnName("PasswordResetExpires");

        builder.HasMany<Address>(u => u.Addresses)
            .WithOne()
            .HasForeignKey("UserId")
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(u => u.Addresses)
            .HasField("_addresses")
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.HasMany<RefreshToken>(u => u.RefreshTokens)
            .WithOne()
            .HasForeignKey(rt => rt.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(u => u.RefreshTokens)
            .HasField("_refreshTokens")
            .UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
