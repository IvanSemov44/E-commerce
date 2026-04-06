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
    public const string OrderCreationFailed = "ORDER_CREATION_FAILED";

    // Category errors
    public const string CategoryNotFound = "CATEGORY_NOT_FOUND";
    public const string CategoryAlreadyExists = "CATEGORY_ALREADY_EXISTS";
    public const string DuplicateCategorySlug = "DUPLICATE_CATEGORY_SLUG";
    public const string CategoryHasProducts = "CATEGORY_HAS_PRODUCTS";

    // Product slug errors
    public const string DuplicateProductSlug = "DUPLICATE_PRODUCT_SLUG";

    // Promo Code errors
    public const string InvalidPromoCode = "INVALID_PROMO_CODE";
    public const string DuplicatePromoCode = "DUPLICATE_PROMO_CODE";
    public const string PromoCodeNotFound = "PROMO_CODE_NOT_FOUND";
    public const string PromoCodeUsageLimitReached = "PROMO_CODE_USAGE_LIMIT_REACHED";

    // Review errors
    public const string ReviewNotFound = "REVIEW_NOT_FOUND";
    public const string InvalidRating = "INVALID_RATING";
    public const string EmptyReviewComment = "EMPTY_REVIEW_COMMENT";
    public const string DuplicateReview = "DUPLICATE_REVIEW";
    public const string ReviewUpdateExpired = "REVIEW_UPDATE_EXPIRED";

    // Wishlist errors
    public const string DuplicateWishlistItem = "DUPLICATE_WISHLIST_ITEM";

    // Pagination errors
    public const string InvalidPagination = "INVALID_PAGINATION";

    // User errors
    public const string UserNotFound = "USER_NOT_FOUND";

    // Payment errors
    public const string UnsupportedPaymentMethod = "UNSUPPORTED_PAYMENT_METHOD";
    public const string PaymentAmountMismatch = "PAYMENT_AMOUNT_MISMATCH";
    public const string NoPaymentFound = "NO_PAYMENT_FOUND";
    public const string PaymentIntentNotFound = "PAYMENT_INTENT_NOT_FOUND";
    public const string InvalidRefund = "INVALID_REFUND";

    // Authentication/Authorization errors
    public const string Unauthorized = "UNAUTHORIZED";
    public const string Forbidden = "FORBIDDEN";
    public const string InvalidCredentials = "INVALID_CREDENTIALS";
    public const string InvalidToken = "INVALID_TOKEN";
    public const string DuplicateEmail = "DUPLICATE_EMAIL";

    // Concurrency errors
    public const string ConcurrencyConflict = "CONCURRENCY_CONFLICT";

    // Integration reliability errors
    public const string DeadLetterMessageNotFound = "DEAD_LETTER_MESSAGE_NOT_FOUND";
    public const string DeadLetterAlreadyRequeued = "DEAD_LETTER_ALREADY_REQUEUED";
    public const string InvalidIntegrationEventPayload = "INVALID_INTEGRATION_EVENT_PAYLOAD";
}
