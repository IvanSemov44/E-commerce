namespace ECommerce.Shopping.Application.Mapping;

public static class ShoppingMappingExtensions
{
    public static CartDto ToDto(this Cart cart) => new()
    {
        Id = cart.Id,
        UserId = cart.UserId == Guid.Empty ? null : cart.UserId,
        Items = cart.Items.Select(i => new CartItemDto
        {
            Id = i.Id,
            ProductId = i.ProductId,
            Quantity = i.Quantity,
            Price = i.UnitPrice,
            Total = i.UnitPrice * i.Quantity
        }).ToList(),
        Subtotal = cart.Subtotal,
        Total = cart.Subtotal
    };

    public static WishlistDto ToDto(this Wishlist wishlist) => new(
        wishlist.Id,
        wishlist.UserId,
        wishlist.ProductIds.ToList());
}
