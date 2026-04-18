
namespace ECommerce.Shopping.Application.Errors;

public static class ShoppingApplicationErrors
{
    public static readonly DomainError CartNotFound     = new("CART_NOT_FOUND",     "Cart not found.");
    public static readonly DomainError WishlistNotFound = new("WISHLIST_NOT_FOUND", "Wishlist not found.");
    public static readonly DomainError ProductNotFound  = new("PRODUCT_NOT_FOUND",  "Product not found or inactive.");
    public static readonly DomainError Unauthorized     = new("UNAUTHORIZED",       "Authentication required.");
    public static readonly DomainError Forbidden        = new("FORBIDDEN",          "You do not have permission to access this cart.");
}