using ECommerce.SharedKernel.Results;

namespace ECommerce.Catalog.Application.Errors;

public static class CatalogApplicationErrors
{
    public static readonly DomainError ProductNotFound = new("PRODUCT_NOT_FOUND", "Product was not found.");
    public static readonly DomainError CategoryNotFound = new("CATEGORY_NOT_FOUND", "Category was not found.");
    public static readonly DomainError SkuAlreadyExists = new("SKU_ALREADY_EXISTS", "A product with this SKU already exists.");
    public static readonly DomainError CategoryHasProducts = new("CATEGORY_HAS_PRODUCTS", "Category has products and cannot be deleted.");
    // Image error is defined in the domain as IMAGE_NOT_FOUND; avoid duplicate application-level constant.
}
