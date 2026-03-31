using ECommerce.Identity.Application.Interfaces;

namespace ECommerce.Identity.Infrastructure.Services;

/// <summary>
/// BCrypt-based password hashing implementation.
/// Work factor 12 balances security and performance.
/// </summary>
public class PasswordHasher : IPasswordHasher
{
    public string Hash(string rawPassword)
        => BCrypt.Net.BCrypt.HashPassword(rawPassword, workFactor: 12);

    public bool Verify(string rawPassword, string hash)
        => BCrypt.Net.BCrypt.Verify(rawPassword, hash);
}
