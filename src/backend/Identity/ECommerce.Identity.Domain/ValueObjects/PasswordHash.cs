using ECommerce.Identity.Domain.Errors;
using ECommerce.SharedKernel.Results;

namespace ECommerce.Identity.Domain.ValueObjects;

/// <summary>
/// Wraps a bcrypt hash — NEVER stores plain text.
/// Two responsibilities: validate raw password policy, wrap pre-hashed values.
/// </summary>
public sealed record PasswordHash
{
    public string Hash { get; }
    private PasswordHash(string hash) => Hash = hash;

    /// <summary>Called by Infrastructure after bcrypt hashing.</summary>
    public static Result<PasswordHash> FromHash(string hash) =>
        string.IsNullOrWhiteSpace(hash)
            ? Result<PasswordHash>.Fail(IdentityErrors.PasswordHashEmpty)
            : Result<PasswordHash>.Ok(new PasswordHash(hash));

    /// <summary>Domain enforces password POLICY before Infrastructure hashes it.</summary>
    public static Result ValidateRawPassword(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return Result.Fail(IdentityErrors.PasswordEmpty);
        if (raw.Length < 8)                 return Result.Fail(IdentityErrors.PasswordTooShort);
        if (!raw.Any(char.IsUpper))         return Result.Fail(IdentityErrors.PasswordNoUpper);
        if (!raw.Any(char.IsDigit))         return Result.Fail(IdentityErrors.PasswordNoDigit);
        return Result.Ok();
    }
}
