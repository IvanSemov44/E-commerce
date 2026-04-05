using ECommerce.Ordering.Application.Interfaces;
using Microsoft.AspNetCore.Http;
using System.Security.Claims;
using CoreUserService = ECommerce.Application.Interfaces.ICurrentUserService;

namespace ECommerce.Ordering.Infrastructure.Services;

public class OrderingCurrentUserService : ICurrentUserService
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public OrderingCurrentUserService(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public Guid? UserId
    {
        get
        {
            var userIdClaim = _httpContextAccessor.HttpContext?.User
                ?.FindFirst(ClaimTypes.NameIdentifier)
                ?? _httpContextAccessor.HttpContext?.User?.FindFirst("sub");

            return Guid.TryParse(userIdClaim?.Value, out var userId) ? userId : null;
        }
    }

    public bool IsAuthenticated =>
        _httpContextAccessor.HttpContext?.User?.Identity?.IsAuthenticated ?? false;
}