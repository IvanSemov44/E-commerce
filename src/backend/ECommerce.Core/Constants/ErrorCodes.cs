namespace ECommerce.Core.Constants;

/// <summary>
/// Semantic error codes for Result<T> failures.
/// Used for client-side error handling and logic.
/// </summary>
public static class ErrorCodes
{
    // Cart errors
    public const string CartNotFound = "CART_NOT_FOUND";
    public const string CartItemNotFound = "CART_ITEM_NOT_FOUND";
    
    // Product errors
    public const string ProductNotFound = "PRODUCT_NOT_FOUND";
    public const string ProductNotAvailable = "PRODUCT_NOT_AVAILABLE";
    
    // Inventory errors
    public const string InsufficientStock = "INSUFFICIENT_STOCK";
    public const string InvalidQuantity = "INVALID_QUANTITY";
    
    // Order errors
    public const string OrderNotFound = "ORDER_NOT_FOUND";
    public const string OrderAlreadyProcessed = "ORDER_ALREADY_PROCESSED";
    public const string InvalidOrderState = "INVALID_ORDER_STATE";
    public const string InvalidOrderStatus = "INVALID_ORDER_STATUS";
    
    // Category errors
    public const string CategoryNotFound = "CATEGORY_NOT_FOUND";
    public const string CategoryAlreadyExists = "CATEGORY_ALREADY_EXISTS";
    public const string DuplicateCategorySlug = "DUPLICATE_CATEGORY_SLUG";
    public const string CategoryHasProducts = "CATEGORY_HAS_PRODUCTS";
    
    // Product slug errors
    public const string DuplicateProductSlug = "DUPLICATE_PRODUCT_SLUG";
    
    // Promo Code errors
    public const string InvalidPromoCode = "INVALID_PROMO_CODE";
    
    // User errors
    public const string UserNotFound = "USER_NOT_FOUND";
    
    // Authentication/Authorization errors
    public const string Unauthorized = "UNAUTHORIZED";
    public const string Forbidden = "FORBIDDEN";
}
