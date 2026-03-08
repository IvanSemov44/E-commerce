using ECommerce.Core.Enums;

namespace ECommerce.Application.Interfaces;

/// <summary>
/// Service for accessing current user context from HTTP claims.
/// </summary>
public interface ICurrentUserService
{
    /// <summary>
    /// Gets the current authenticated user's ID. Throws if not authenticated.
    /// </summary>
    /// <exception cref="UnauthorizedAccessException">Thrown when user ID is not found in token.</exception>
    Guid UserId { get; }

    /// <summary>
    /// Gets the current user's ID or null if not authenticated.
    /// </summary>
    Guid? UserIdOrNull { get; }

    /// <summary>
    /// Gets the current session ID from cookies or null if not present.
    /// </summary>
    string? SessionId { get; }

    /// <summary>
    /// Gets the current authenticated user's email. Throws if not authenticated.
    /// </summary>
    /// <exception cref="UnauthorizedAccessException">Thrown when email is not found in token.</exception>
    string Email { get; }

    /// <summary>
    /// Gets the current user's email or null if not available.
    /// </summary>
    string? EmailOrNull { get; }

    /// <summary>
    /// Gets the current authenticated user's role. Throws if not authenticated.
    /// </summary>
    /// <exception cref="UnauthorizedAccessException">Thrown when role is not found in token.</exception>
    UserRole Role { get; }

    /// <summary>
    /// Gets the current user's role or null if not available.
    /// </summary>
    UserRole? RoleOrNull { get; }

    /// <summary>
    /// Gets whether the current user is authenticated.
    /// </summary>
    bool IsAuthenticated { get; }
}
