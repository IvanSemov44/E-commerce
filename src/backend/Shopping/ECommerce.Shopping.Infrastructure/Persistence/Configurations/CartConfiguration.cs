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

        builder.Property(c => c.RowVersion)
               .IsRowVersion()
               .IsConcurrencyToken();

        builder.HasMany<CartItem>("_items")
               .WithOne()
               .HasForeignKey(i => i.CartId)
               .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(c => c.Items).UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}