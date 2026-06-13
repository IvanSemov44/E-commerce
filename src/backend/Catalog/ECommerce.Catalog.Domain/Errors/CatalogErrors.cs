using ECommerce.SharedKernel.Results;

namespace ECommerce.Catalog.Domain.Errors;

public static class CatalogErrors
{
    // ProductName
    public static readonly DomainError ProductNameEmpty  = new("PRODUCT_NAME_EMPTY",  "Product name cannot be empty.",                ErrorType.Validation);
    public static readonly DomainError ProductNameTooLong = new("PRODUCT_NAME_TOO_LONG","Product name cannot exceed 200 characters.", ErrorType.Validation);

    // Slug
    public static readonly DomainError SlugEmpty   = new("SLUG_EMPTY",   "Slug cannot be empty.",                    ErrorType.Validation);
    public static readonly DomainError SlugInvalid = new("SLUG_INVALID", "Slug produced no valid characters.",       ErrorType.Validation);

    // Sku
    public static readonly DomainError SkuEmpty   = new("SKU_EMPTY",   "SKU cannot be empty.",                      ErrorType.Validation);
    public static readonly DomainError SkuTooLong = new("SKU_TOO_LONG", "SKU cannot exceed 100 characters.",        ErrorType.Validation);

    // Money
    public static readonly DomainError MoneyNegative         = new("MONEY_NEGATIVE",          "Amount cannot be negative.",                     ErrorType.Validation);
    public static readonly DomainError MoneyInvalidCurrency  = new("MONEY_INVALID_CURRENCY",  "Currency must be a 3-letter ISO code.",          ErrorType.Validation);
    public static readonly DomainError MoneyCurrencyMismatch = new("MONEY_CURRENCY_MISMATCH", "Cannot add money with different currencies.",    ErrorType.Validation);

    // Weight
    public static readonly DomainError WeightNegative = new("WEIGHT_NEGATIVE", "Weight cannot be negative.", ErrorType.Validation);

    // StockQuantity
    public static readonly DomainError StockQuantityNegative = new("STOCK_QUANTITY_NEGATIVE", "Stock quantity cannot be negative.", ErrorType.Validation);

    // CategoryName
    public static readonly DomainError CategoryNameEmpty   = new("CATEGORY_NAME_EMPTY",   "Category name cannot be empty.",                   ErrorType.Validation);
    public static readonly DomainError CategoryNameTooLong = new("CATEGORY_NAME_TOO_LONG", "Category name cannot exceed 100 characters.",     ErrorType.Validation);

    // Product aggregate
    public static readonly DomainError ProductDiscontinued = new("PRODUCT_DISCONTINUED", "Cannot deactivate a discontinued product.", ErrorType.Validation);
    public static readonly DomainError ProductMaxImages    = new("PRODUCT_MAX_IMAGES",   "Product cannot have more than 10 images.",  ErrorType.Validation);
    public static readonly DomainError ProductImageNotFound = new("IMAGE_NOT_FOUND",     "Image not found on this product.",          ErrorType.NotFound);

    // Category aggregate
    public static readonly DomainError CategoryCircularParent = new("CATEGORY_CIRCULAR", "Category cannot be its own parent.", ErrorType.Validation);
}
