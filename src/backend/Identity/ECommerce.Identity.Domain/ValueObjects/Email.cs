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

    public static Result<Email> Create(string? raw) =>
        string.IsNullOrWhiteSpace(raw)
            ? Result<Email>.Fail(IdentityErrors.EmailEmpty)
            : raw.Trim().ToLowerInvariant() is var normalized && normalized.Length > 256
                ? Result<Email>.Fail(IdentityErrors.EmailTooLong)
                : !normalized.Contains('@') || normalized.IndexOf('.', normalized.IndexOf('@')) < 0
                    ? Result<Email>.Fail(IdentityErrors.EmailInvalid)
                    : Result<Email>.Ok(new Email(normalized));

    public static implicit operator string(Email email) => email.Value;
    public override string ToString() => Value;
}
