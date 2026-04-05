using ECommerce.SharedKernel.Results;

namespace ECommerce.Ordering.Domain.Errors;

public static class OrderingErrors
{
    public static readonly DomainError StatusUnknown = new("ORDER_STATUS_UNKNOWN", "Unknown order status.");
    public static readonly DomainError PaymentRefEmpty = new("PAYMENT_REF_EMPTY", "Payment reference cannot be empty.");
    public static readonly DomainError PaymentAmountInvalid = new("PAYMENT_AMOUNT_INVALID", "Payment amount must be greater than zero.");
    public static readonly DomainError OrderEmpty = new("ORDER_EMPTY", "Order must have at least one item.");
    public static readonly DomainError OrderTotalInvalid = new("ORDER_TOTAL_INVALID", "Order total must be greater than zero.");
    public static readonly DomainError OrderInvalidTransition = new("ORDER_INVALID_TRANSITION", "Invalid status transition for this order.");
    public static readonly DomainError PromoCodeNotFound = new("PROMO_CODE_NOT_FOUND", "Promo code not found.");
    public static readonly DomainError PromoCodeInvalid = new("PROMO_CODE_INVALID", "Promo code is invalid or expired.");
    public static readonly DomainError AddressNotFound = new("ADDRESS_NOT_FOUND", "Shipping address not found.");
    public static readonly DomainError ProductsUnavailable = new("PRODUCTS_UNAVAILABLE", "One or more products are unavailable.");
}
