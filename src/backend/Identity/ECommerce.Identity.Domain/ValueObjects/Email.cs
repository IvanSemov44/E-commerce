using ECommerce.SharedKernel.Results;
using ECommerce.Identity.Domain.Errors;

namespace ECommerce.Identity.Domain.ValueObjects;

// Single-property VO → use sealed record (Rule 8: sealed prevents subclassing that breaks value equality)
public sealed record Email
{
    public string Value { get; }
    private Email(string value) => Value = value;

    public static Result<Email> Create(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
            return Result<Email>.Fail(IdentityErrors.EmailEmpty);

        var normalized = raw.Trim().ToLowerInvariant();

        if (normalized.Length > 256)
            return Result<Email>.Fail(IdentityErrors.EmailTooLong);

        if (!normalized.Contains('@') || normalized.IndexOf('.', normalized.IndexOf('@')) < 0)
            return Result<Email>.Fail(IdentityErrors.EmailInvalid);

        return Result<Email>.Ok(new Email(normalized));
    }
}
