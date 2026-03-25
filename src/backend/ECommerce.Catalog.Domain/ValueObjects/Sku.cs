using ECommerce.Catalog.Domain.Errors;
using ECommerce.SharedKernel.Results;

namespace ECommerce.Catalog.Domain.ValueObjects;

public sealed record Sku
{
    public string Value { get; }

    private Sku(string value) => Value = value;

    public static Result<Sku> Create(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
            return Result<Sku>.Fail(CatalogErrors.SkuEmpty);
        if (raw.Trim().Length > 100)
            return Result<Sku>.Fail(CatalogErrors.SkuTooLong);
        return Result<Sku>.Ok(new Sku(raw.Trim()));
    }
}
