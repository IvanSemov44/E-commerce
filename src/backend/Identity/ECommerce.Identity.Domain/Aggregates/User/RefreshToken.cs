using ECommerce.SharedKernel.Domain;

namespace ECommerce.Identity.Domain.Aggregates.User;

/// <summary>
/// Child entity — manages refresh tokens for a user.
/// Only <see cref="User.AddRefreshToken"/> can create tokens (constructor is internal).
/// </summary>
public sealed class RefreshToken : Entity
{
    public Guid UserId { get; private set; }
    public string Token { get; private set; } = null!;
    public DateTime ExpiresAt { get; private set; }
    public bool IsRevoked { get; private set; }
    public string? RevokedReason { get; private set; }

    public bool IsExpired => DateTime.UtcNow > ExpiresAt;
    public bool IsActive => !IsRevoked && !IsExpired;

    private RefreshToken() { } // EF Core

    internal RefreshToken(Guid id, Guid userId, string token, DateTime expiresAt)
    {
        Id = id;
        UserId = userId;
        Token = token;
        ExpiresAt = expiresAt;
    }

    internal void Revoke(string reason)
    {
        IsRevoked = true;
        RevokedReason = reason;
    }
}
