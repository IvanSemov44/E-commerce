using System.Collections.Generic;
using ECommerce.Catalog.Domain.Errors;
using ECommerce.SharedKernel.Domain;
using ECommerce.SharedKernel.Results;

namespace ECommerce.Catalog.Domain.ValueObjects;

public sealed class Weight : ValueObject
{
    public decimal Value { get; }

    private Weight() { Value = 0; }
    private Weight(decimal value) => Value = value;

    public static Result<Weight> Create(decimal value)
    {
        if (value < 0)
            return Result<Weight>.Fail(CatalogErrors.WeightNegative);
        return Result<Weight>.Ok(new Weight(value));
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }
}
