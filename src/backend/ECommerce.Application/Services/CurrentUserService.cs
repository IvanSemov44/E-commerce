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
            var userIdClaim = _httpContextAccessor.HttpContext?.User
                ?.FindFirst(ClaimTypes.NameIdentifier)
                ?? _httpContextAccessor.HttpContext?.User?.FindFirst("sub");

            if (userIdClaim?.Value == null || !Guid.TryParse(userIdClaim.Value, out var userId))
                throw new UnauthorizedAccessException("User ID not found in token");

            return userId;
        }
    }

    public Guid? UserIdOrNull
    {
        get
        {
            var userIdClaim = _httpContextAccessor.HttpContext?.User
                ?.FindFirst(ClaimTypes.NameIdentifier)
                ?? _httpContextAccessor.HttpContext?.User?.FindFirst("sub");

            return userIdClaim?.Value != null && Guid.TryParse(userIdClaim.Value, out var userId)
                ? userId
                : null;
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
            var emailClaim = _httpContextAccessor.HttpContext?.User?.FindFirst(ClaimTypes.Email);
            if (emailClaim?.Value == null)
                throw new UnauthorizedAccessException("Email not found in token");

            return emailClaim.Value;
        }
    }

    public UserRole Role
    {
        get
        {
            var roleClaim = _httpContextAccessor.HttpContext?.User?.FindFirst(ClaimTypes.Role);
            if (roleClaim?.Value == null || !Enum.TryParse<UserRole>(roleClaim.Value, out var role))
                throw new UnauthorizedAccessException("Role not found in token");

            return role;
        }
    }

    public bool IsAuthenticated =>
        _httpContextAccessor.HttpContext?.User?.Identity?.IsAuthenticated ?? false;
}
