using ECommerce.Ordering.Application.Interfaces;
using Microsoft.AspNetCore.Http;
using System.Security.Claims;
using CoreUserService = ECommerce.SharedKernel.Interfaces.ICurrentUserService;

namespace ECommerce.Ordering.Infrastructure.Services;

public class OrderingCurrentUserService(IHttpContextAccessor httpContextAccessor) : ICurrentUserService
{

    public Guid? UserId
    {
        get
        {
            var userIdClaim = httpContextAccessor.HttpContext?.User
                ?.FindFirst(ClaimTypes.NameIdentifier)
                ?? httpContextAccessor.HttpContext?.User?.FindFirst("sub");

            return Guid.TryParse(userIdClaim?.Value, out var userId) ? userId : null;
        }
    }

    public bool IsAuthenticated =>
        httpContextAccessor.HttpContext?.User?.Identity?.IsAuthenticated ?? false;
}
