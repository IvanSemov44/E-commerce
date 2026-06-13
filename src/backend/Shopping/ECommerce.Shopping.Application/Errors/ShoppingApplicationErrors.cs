namespace ECommerce.Shopping.Application.Errors;

public static class ShoppingApplicationErrors
{
    public static readonly DomainError CartNotFound     = new("CART_NOT_FOUND",     "Cart not found.",                                          ErrorType.NotFound);
    public static readonly DomainError WishlistNotFound = new("WISHLIST_NOT_FOUND", "Wishlist not found.",                                      ErrorType.NotFound);
    public static readonly DomainError ProductNotFound  = new("PRODUCT_NOT_FOUND",  "Product not found or inactive.",                           ErrorType.NotFound);
    public static readonly DomainError Unauthorized     = new("UNAUTHORIZED",       "Authentication required.",                                 ErrorType.Unauthorized);
    public static readonly DomainError Forbidden        = new("FORBIDDEN",          "You do not have permission to access this cart.",          ErrorType.Forbidden);
}
