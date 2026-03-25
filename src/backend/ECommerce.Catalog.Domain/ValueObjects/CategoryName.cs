using ECommerce.Catalog.Domain.Errors;
using ECommerce.SharedKernel.Results;

namespace ECommerce.Catalog.Domain.ValueObjects;

public sealed record CategoryName
{
    public string Value { get; }

    private CategoryName(string value) => Value = value;

    public static Result<CategoryName> Create(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
            return Result<CategoryName>.Fail(CatalogErrors.CategoryNameEmpty);
        if (raw.Trim().Length > 100)
            return Result<CategoryName>.Fail(CatalogErrors.CategoryNameTooLong);
        return Result<CategoryName>.Ok(new CategoryName(raw.Trim()));
    }
}
