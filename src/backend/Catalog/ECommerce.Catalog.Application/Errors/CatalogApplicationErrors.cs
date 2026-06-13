using ECommerce.SharedKernel.Results;

namespace ECommerce.Catalog.Application.Errors;

public static class CatalogApplicationErrors
{
    public static readonly DomainError ProductNotFound      = new("PRODUCT_NOT_FOUND",      "Product was not found.",                          ErrorType.NotFound);
    public static readonly DomainError CategoryNotFound     = new("CATEGORY_NOT_FOUND",     "Category was not found.",                         ErrorType.NotFound);
    public static readonly DomainError SkuAlreadyExists     = new("SKU_ALREADY_EXISTS",     "A product with this SKU already exists.",         ErrorType.Conflict);
    public static readonly DomainError DuplicateProductSlug = new("DUPLICATE_PRODUCT_SLUG", "A product with this slug already exists.",        ErrorType.Validation);
    public static readonly DomainError DuplicateCategorySlug = new("DUPLICATE_CATEGORY_SLUG","A category with this slug already exists.",      ErrorType.Validation);
    public static readonly DomainError CategoryHasProducts  = new("CATEGORY_HAS_PRODUCTS",  "Category has products and cannot be deleted.",    ErrorType.Validation);
}
