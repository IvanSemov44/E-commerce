using ECommerce.SharedKernel.Results;
using ECommerce.Promotions.Domain.Errors;
using System.Text.RegularExpressions;

namespace ECommerce.Promotions.Domain.ValueObjects;

public sealed record PromoCodeString
{
    public string Value { get; }

    private PromoCodeString(string value) => Value = value;

    public static Result<PromoCodeString> Create(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
            return Result<PromoCodeString>.Fail(PromotionsErrors.CodeEmpty);

        var normalized = raw.Trim().ToUpperInvariant();

        if (normalized.Length < 3 || normalized.Length > 20)
            return Result<PromoCodeString>.Fail(PromotionsErrors.CodeLength);

        if (!Regex.IsMatch(normalized, @"^[A-Z0-9\-]+$"))
            return Result<PromoCodeString>.Fail(PromotionsErrors.CodeChars);

        return Result<PromoCodeString>.Ok(new PromoCodeString(normalized));
    }

    internal static PromoCodeString Reconstitute(string stored) => new(stored);
}