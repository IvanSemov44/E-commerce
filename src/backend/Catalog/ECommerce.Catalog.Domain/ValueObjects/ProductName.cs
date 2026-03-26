using ECommerce.Catalog.Domain.Errors;
using ECommerce.SharedKernel.Results;

namespace ECommerce.Catalog.Domain.ValueObjects;

public sealed record ProductName
{
    public string Value { get; }

    private ProductName(string value) => Value = value;

    public static Result<ProductName> Create(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
            return Result<ProductName>.Fail(CatalogErrors.ProductNameEmpty);
        if (raw.Trim().Length > 200)
            return Result<ProductName>.Fail(CatalogErrors.ProductNameTooLong);
        return Result<ProductName>.Ok(new ProductName(raw.Trim()));
    }
}
