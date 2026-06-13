using ECommerce.SharedKernel.Results;

namespace ECommerce.Promotions.Domain.Errors;

public static class PromotionsErrors
{
    public static readonly DomainError CodeEmpty              = new("CODE_EMPTY",              "Promo code cannot be empty",                                       ErrorType.Validation);
    public static readonly DomainError CodeLength             = new("CODE_LENGTH",             "Promo code must be between 3 and 20 characters",                  ErrorType.Validation);
    public static readonly DomainError CodeChars              = new("CODE_CHARS",              "Promo code may only contain letters, digits, and hyphens",         ErrorType.Validation);
    public static readonly DomainError DiscountPercentRange   = new("DISCOUNT_PERCENT_RANGE",  "Percentage discount must be between 1 and 100",                   ErrorType.Validation);
    public static readonly DomainError DiscountAmountPositive = new("DISCOUNT_AMOUNT_POSITIVE","Fixed discount amount must be greater than 0",                    ErrorType.Validation);
    public static readonly DomainError InvalidDiscountType    = new("INVALID_DISCOUNT_TYPE",   "Discount type must be 'PERCENTAGE' or 'FIXED'",                   ErrorType.Validation);
    public static readonly DomainError DateRangeInvalid       = new("DATE_RANGE_INVALID",      "Start date must be before end date",                              ErrorType.Validation);

    public static readonly DomainError PromoNotFound      = new("PROMO_CODE_NOT_FOUND",  "Promo code not found",                                                  ErrorType.NotFound);
    public static readonly DomainError DuplicateCode      = new("DUPLICATE_PROMO_CODE",  "A promo code with this value already exists",                          ErrorType.Conflict);
    public static readonly DomainError ConcurrencyConflict = new("CONCURRENCY_CONFLICT", "Promo code was modified by another operation. Please retry.",           ErrorType.Conflict);
    public static readonly DomainError PromoNotValid      = new("PROMO_NOT_VALID",       "This promo code is not valid",                                          ErrorType.Validation);
    public static readonly DomainError PromoMinOrder      = new("PROMO_MIN_ORDER",       "Order amount does not meet the minimum required for this code",         ErrorType.Validation);
}
