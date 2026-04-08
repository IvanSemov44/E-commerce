using ECommerce.SharedKernel.Interfaces;
using ECommerce.SharedKernel.Enums;
using Microsoft.AspNetCore.Http;
using System.Security.Claims;

namespace ECommerce.Infrastructure.Services;

/// <summary>
/// Service for accessing current user context from HTTP claims.
/// </summary>
public class CurrentUserService(IHttpContextAccessor httpContextAccessor) : ICurrentUserService
{
    public Guid UserId
    {
        get
        {
            if (!TryGetUserId(out var userId))
                throw new UnauthorizedAccessException("User ID not found in token");

            return userId;
        }
    }

    public Guid? UserIdOrNull
    {
        get
        {
            return TryGetUserId(out var userId) ? userId : null;
        }
    }

    public string? SessionId
    {
        get
        {
            var httpContext = httpContextAccessor.HttpContext;
            if (httpContext == null) return null;

            // Check header first (for API/mobile clients)
            if (httpContext.Request.Headers.TryGetValue("X-Session-ID", out var headerSessionId) && !string.IsNullOrWhiteSpace(headerSessionId))
                return headerSessionId.ToString();

            // Fall back to cookie
            if (httpContext.Request.Cookies.TryGetValue("sessionId", out var cookieSessionId) && !string.IsNullOrWhiteSpace(cookieSessionId))
                return cookieSessionId;

            return null;
        }
    }

    public string Email
    {
        get
        {
            if (!TryGetEmail(out var email))
                throw new UnauthorizedAccessException("Email not found in token");

            return email;
        }
    }

    public string? EmailOrNull
    {
        get
        {
            return TryGetEmail(out var email) ? email : null;
        }
    }

    public UserRole Role
    {
        get
        {
            if (!TryGetRole(out var role))
                throw new UnauthorizedAccessException("Role not found in token");

            return role;
        }
    }

    public UserRole? RoleOrNull
    {
        get
        {
            return TryGetRole(out var role) ? role : null;
        }
    }

    public bool IsAuthenticated =>
        httpContextAccessor.HttpContext?.User?.Identity?.IsAuthenticated ?? false;

    private bool TryGetUserId(out Guid userId)
    {
        var userIdClaim = httpContextAccessor.HttpContext?.User
            ?.FindFirst(ClaimTypes.NameIdentifier)
            ?? httpContextAccessor.HttpContext?.User?.FindFirst("sub");

        return Guid.TryParse(userIdClaim?.Value, out userId);
    }

    private bool TryGetEmail(out string email)
    {
        var emailClaim = httpContextAccessor.HttpContext?.User?.FindFirst(ClaimTypes.Email);
        email = emailClaim?.Value ?? string.Empty;
        return !string.IsNullOrWhiteSpace(email);
    }

    private bool TryGetRole(out UserRole role)
    {
        var roleClaim = httpContextAccessor.HttpContext?.User?.FindFirst(ClaimTypes.Role);
        return Enum.TryParse<UserRole>(roleClaim?.Value, out role);
    }
}

