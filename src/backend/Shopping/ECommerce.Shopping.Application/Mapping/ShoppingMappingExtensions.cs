
namespace ECommerce.Shopping.Application.Mapping;

public static class ShoppingMappingExtensions
{
    public static CartDto ToDto(this Cart cart) => new(
        cart.Id,
        cart.UserId,
        cart.Items.Select(i => new CartItemDto(
            i.Id, i.ProductId, i.Quantity, i.UnitPrice, i.Currency,
            i.UnitPrice * i.Quantity)).ToList(),
        cart.Subtotal);

    public static WishlistDto ToDto(this Wishlist wishlist) => new(
        wishlist.Id,
        wishlist.UserId,
        wishlist.ProductIds.ToList());
}