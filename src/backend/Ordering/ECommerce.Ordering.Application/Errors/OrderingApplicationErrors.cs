namespace ECommerce.Ordering.Application.Errors;

public static class OrderingApplicationErrors
{
    public static readonly DomainError OrderNotFound = new("ORDER_NOT_FOUND", "Order not found.");
    public static readonly DomainError Unauthorized = new("UNAUTHORIZED", "User not authenticated.");
    public static readonly DomainError ProductsUnavailable = new("PRODUCTS_UNAVAILABLE", "One or more products are unavailable.");
    public static readonly DomainError PromoCodeNotFound = new("PROMO_CODE_NOT_FOUND", "Promo code not found.");
    public static readonly DomainError PromoCodeInvalid = new("PROMO_CODE_INVALID", "Promo code is invalid or expired.");
    public static readonly DomainError AddressNotFound = new("ADDRESS_NOT_FOUND", "Shipping address not found.");
}
