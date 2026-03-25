using System.Collections.Generic;
using ECommerce.Catalog.Domain.Errors;
using ECommerce.SharedKernel.Domain;
using ECommerce.SharedKernel.Results;

namespace ECommerce.Catalog.Domain.ValueObjects;

public sealed class Money : ValueObject
{
    public decimal Amount { get; }
    public string Currency { get; }

    private Money() { Amount = 0; Currency = "USD"; }
    private Money(decimal amount, string currency)
    {
        Amount = amount;
        Currency = currency;
    }

    public static Result<Money> Create(decimal amount, string currency)
    {
        if (amount < 0)
            return Result<Money>.Fail(CatalogErrors.MoneyNegative);
        if (string.IsNullOrWhiteSpace(currency) || currency.Length != 3)
            return Result<Money>.Fail(CatalogErrors.MoneyInvalidCurrency);
        return Result<Money>.Ok(new Money(amount, currency.ToUpperInvariant()));
    }

    public Result<Money> Add(Money other)
    {
        if (Currency != other.Currency)
            return Result<Money>.Fail(CatalogErrors.MoneyCurrencyMismatch);
        return Result<Money>.Ok(new Money(Amount + other.Amount, Currency));
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Amount;
        yield return Currency;
    }
}
