using ECommerce.SharedKernel.Results;

namespace ECommerce.Shopping.Domain.Errors;

public static class ShoppingErrors
{
    public static readonly DomainError QuantityInvalid  = new("QUANTITY_INVALID",   "Quantity must be greater than zero.");
    public static readonly DomainError CartFull         = new("CART_FULL",          "Cart cannot hold more than 50 distinct items.");
    public static readonly DomainError CartItemNotFound = new("CART_ITEM_NOT_FOUND","Cart item not found.");

    public static readonly DomainError WishlistFull     = new("WISHLIST_FULL",      "Wishlist cannot hold more than 100 products.");

    // NOTE: CartNotFound, WishlistNotFound, ProductNotFound require repo lookups —
    // they live in ShoppingApplicationErrors (step-2), not here.
}