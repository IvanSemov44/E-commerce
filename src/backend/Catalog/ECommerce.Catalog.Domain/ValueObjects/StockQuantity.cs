using System.Collections.Generic;
using ECommerce.Catalog.Domain.Errors;
using ECommerce.SharedKernel.Domain;
using ECommerce.SharedKernel.Results;

namespace ECommerce.Catalog.Domain.ValueObjects;

public sealed class StockQuantity : ValueObject
{
    public int Value { get; }

    private StockQuantity() { Value = 0; }
    private StockQuantity(int value) => Value = value;

    public static Result<StockQuantity> Create(int value)
    {
        if (value < 0)
            return Result<StockQuantity>.Fail(CatalogErrors.StockQuantityNegative);
        return Result<StockQuantity>.Ok(new StockQuantity(value));
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }
}
