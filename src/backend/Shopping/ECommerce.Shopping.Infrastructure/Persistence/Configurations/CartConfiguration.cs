using ECommerce.Shopping.Domain.Aggregates.Cart;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ECommerce.Shopping.Infrastructure.Persistence.Configurations;

public class CartConfiguration : IEntityTypeConfiguration<Cart>
{
    public void Configure(EntityTypeBuilder<Cart> builder)
    {
        builder.ToTable("Carts");
        builder.HasKey(c => c.Id);

        builder.Property(c => c.UserId).IsRequired();
        builder.Property(c => c.SessionId).HasMaxLength(100).IsRequired(false);
        builder.HasIndex(c => c.SessionId);

        builder.Property(c => c.RowVersion)
               .IsRowVersion()
               .IsConcurrencyToken();

        builder.HasMany(c => c.Items)
               .WithOne()
               .HasForeignKey(i => i.CartId)
               .OnDelete(DeleteBehavior.Cascade);
    }
}