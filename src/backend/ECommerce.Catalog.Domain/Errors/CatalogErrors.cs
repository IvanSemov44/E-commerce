using ECommerce.SharedKernel.Results;

namespace ECommerce.Catalog.Domain.Errors;

// Each error is a static readonly value — the code and message are fixed.
// Callers pass these into Result.Fail(). No magic strings at throw sites.
public static class CatalogErrors
{
    // ProductName
    public static readonly DomainError ProductNameEmpty = new("PRODUCT_NAME_EMPTY", "Product name cannot be empty.");
    public static readonly DomainError ProductNameTooLong = new("PRODUCT_NAME_TOO_LONG", "Product name cannot exceed 200 characters.");

    // Slug
    public static readonly DomainError SlugEmpty = new("SLUG_EMPTY", "Slug cannot be empty.");
    public static readonly DomainError SlugInvalid = new("SLUG_INVALID", "Slug produced no valid characters.");

    // Sku
    public static readonly DomainError SkuEmpty = new("SKU_EMPTY", "SKU cannot be empty.");
    public static readonly DomainError SkuTooLong = new("SKU_TOO_LONG", "SKU cannot exceed 100 characters.");

    // Money
    public static readonly DomainError MoneyNegative = new("MONEY_NEGATIVE", "Amount cannot be negative.");
    public static readonly DomainError MoneyInvalidCurrency = new("MONEY_INVALID_CURRENCY", "Currency must be a 3-letter ISO code.");
    public static readonly DomainError MoneyCurrencyMismatch = new("MONEY_CURRENCY_MISMATCH", "Cannot add money with different currencies.");

    // Weight
    public static readonly DomainError WeightNegative = new("WEIGHT_NEGATIVE", "Weight cannot be negative.");

    // StockQuantity
    public static readonly DomainError StockQuantityNegative = new("STOCK_QUANTITY_NEGATIVE", "Stock quantity cannot be negative.");

    // CategoryName
    public static readonly DomainError CategoryNameEmpty = new("CATEGORY_NAME_EMPTY", "Category name cannot be empty.");
    public static readonly DomainError CategoryNameTooLong = new("CATEGORY_NAME_TOO_LONG", "Category name cannot exceed 100 characters.");

    // Product aggregate
    public static readonly DomainError ProductDiscontinued = new("PRODUCT_DISCONTINUED", "Cannot deactivate a discontinued product.");
    public static readonly DomainError ProductMaxImages = new("PRODUCT_MAX_IMAGES", "Product cannot have more than 10 images.");
    public static readonly DomainError ProductImageNotFound = new("IMAGE_NOT_FOUND", "Image not found on this product.");

    // Category aggregate
    public static readonly DomainError CategoryCircularParent = new("CATEGORY_CIRCULAR", "Category cannot be its own parent.");
}
