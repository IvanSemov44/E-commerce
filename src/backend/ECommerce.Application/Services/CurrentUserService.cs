using ECommerce.Application.Interfaces;
using ECommerce.Core.Enums;
using Microsoft.AspNetCore.Http;
using System.Security.Claims;

namespace ECommerce.Application.Services;

/// <summary>
/// Service for accessing current user context from HTTP claims.
/// </summary>
public class CurrentUserService : ICurrentUserService
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CurrentUserService(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

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
            return _httpContextAccessor.HttpContext?.Request.Cookies
                .TryGetValue("sessionId", out var sessionId) == true
                ? sessionId
                : null;
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
        _httpContextAccessor.HttpContext?.User?.Identity?.IsAuthenticated ?? false;

    private bool TryGetUserId(out Guid userId)
    {
        var userIdClaim = _httpContextAccessor.HttpContext?.User
            ?.FindFirst(ClaimTypes.NameIdentifier)
            ?? _httpContextAccessor.HttpContext?.User?.FindFirst("sub");

        return Guid.TryParse(userIdClaim?.Value, out userId);
    }

    private bool TryGetEmail(out string email)
    {
        var emailClaim = _httpContextAccessor.HttpContext?.User?.FindFirst(ClaimTypes.Email);
        email = emailClaim?.Value ?? string.Empty;
        return !string.IsNullOrWhiteSpace(email);
    }

    private bool TryGetRole(out UserRole role)
    {
        var roleClaim = _httpContextAccessor.HttpContext?.User?.FindFirst(ClaimTypes.Role);
        return Enum.TryParse<UserRole>(roleClaim?.Value, out role);
    }
}
