using ECommerce.SharedKernel.Results;
using ECommerce.Identity.Domain.Errors;

namespace ECommerce.Identity.Domain.ValueObjects;

// The domain holds a hash, NEVER plain text.
// Infrastructure calls IPasswordHasher to produce the hash; domain wraps it here.
public sealed record PasswordHash
{
    public string Hash { get; }
    private PasswordHash(string hash) => Hash = hash;

    // Called by Infrastructure after hashing
    public static Result<PasswordHash> FromHash(string hash)
    {
        if (string.IsNullOrWhiteSpace(hash))
            return Result<PasswordHash>.Fail(IdentityErrors.PasswordHashEmpty);
        return Result<PasswordHash>.Ok(new PasswordHash(hash));
    }

    // Domain enforces password POLICY before Infrastructure hashes it
    public static Result ValidateRawPassword(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
            return Result.Fail(IdentityErrors.PasswordEmpty);
        if (raw.Length < 8)
            return Result.Fail(IdentityErrors.PasswordTooShort);
        if (!raw.Any(char.IsUpper))
            return Result.Fail(IdentityErrors.PasswordNoUpper);
        if (!raw.Any(char.IsDigit))
            return Result.Fail(IdentityErrors.PasswordNoDigit);
        return Result.Ok();
    }
}
