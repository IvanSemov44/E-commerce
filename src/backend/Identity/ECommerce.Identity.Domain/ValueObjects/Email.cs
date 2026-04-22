using ECommerce.Identity.Domain.Errors;
using ECommerce.SharedKernel.Results;

namespace ECommerce.Identity.Domain.ValueObjects;

/// <summary>
/// Normalized email value object — single property, sealed record.
/// Always stored lowercase with trimmed whitespace.
/// </summary>
public sealed record Email
{
    public string Value { get; }

    private Email(string value) => Value = value;

    public static Result<Email> Create(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
            return Result<Email>.Fail(IdentityErrors.EmailEmpty);

        var normalized = raw.Trim().ToLowerInvariant();

        if (normalized.Length > 256)
            return Result<Email>.Fail(IdentityErrors.EmailTooLong);

        var atIndex = normalized.IndexOf('@');
        var dotAfterAtIndex = atIndex >= 0 ? normalized.IndexOf('.', atIndex) : -1;
        if (atIndex < 0 || dotAfterAtIndex < 0)
            return Result<Email>.Fail(IdentityErrors.EmailInvalid);

        return Result<Email>.Ok(new Email(normalized));
    }

    public static implicit operator string(Email email) => email.Value;
    public override string ToString() => Value;
}
