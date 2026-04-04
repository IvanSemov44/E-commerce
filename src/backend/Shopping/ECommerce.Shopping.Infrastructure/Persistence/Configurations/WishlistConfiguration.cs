using ECommerce.Shopping.Domain.Aggregates.Wishlist;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ECommerce.Shopping.Infrastructure.Persistence.Configurations;

public class WishlistConfiguration : IEntityTypeConfiguration<Wishlist>
{
    public void Configure(EntityTypeBuilder<Wishlist> builder)
    {
        builder.ToTable("Wishlists");
        builder.HasKey(w => w.Id);
        builder.Property(w => w.UserId).IsRequired();
        builder.Ignore(w => w.ProductIds);
    }
}