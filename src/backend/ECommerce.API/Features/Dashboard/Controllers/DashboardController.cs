using System.Collections.Frozen;
using ECommerce.API.Shared.Extensions;
using ECommerce.API.Features.Dashboard.Models;
using ECommerce.Application.DTOs.Common;
using ECommerce.Catalog.Application.Queries.GetProductStats;
using ECommerce.Identity.Application.Queries.GetUserStats;
using ECommerce.Ordering.Application.DTOs.Dashboard;
using ECommerce.Ordering.Application.Queries.GetOrderStats;
using ECommerce.SharedKernel.Results;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ECommerce.API.Features.Dashboard.Controllers;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
[Tags("Dashboard")]
[Authorize(Roles = "Admin,SuperAdmin")]
public class DashboardController(IMediator mediator, ILogger<DashboardController> logger) : ControllerBase
{
    private static readonly FrozenSet<string> _notFound = FrozenSet.ToFrozenSet(new[] { "NOT_FOUND" });
    private static readonly FrozenSet<string> _conflict = FrozenSet.ToFrozenSet(new[] { "CONCURRENCY_CONFLICT" });
    private static readonly FrozenSet<string> _unprocessable = FrozenSet.ToFrozenSet(new[] { "VALIDATION_FAILED" });

    [HttpGet("stats")]
    [ProducesResponseType(typeof(ApiResponse<DashboardStatsResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetStats(CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Retrieving dashboard statistics");

        var orderStatsResult = await mediator.Send(new GetOrderStatsQuery(), cancellationToken);
        if (orderStatsResult is Result<OrderStatsDto>.Failure orderFail)
            return MapError(orderFail.Error);
        var orderStats = ((Result<OrderStatsDto>.Success)orderStatsResult).Data;

        var userStatsResult = await mediator.Send(new GetUserStatsQuery(), cancellationToken);
        if (userStatsResult is Result<ECommerce.Identity.Application.DTOs.UserStatsDto>.Failure userFail)
            return MapError(userFail.Error);
        var userStats = ((Result<ECommerce.Identity.Application.DTOs.UserStatsDto>.Success)userStatsResult).Data;

        var productStatsResult = await mediator.Send(new GetProductStatsQuery(), cancellationToken);
        if (productStatsResult is Result<ECommerce.Catalog.Application.DTOs.Products.ProductStatsDto>.Failure productFail)
            return MapError(productFail.Error);
        var productStats = ((Result<ECommerce.Catalog.Application.DTOs.Products.ProductStatsDto>.Success)productStatsResult).Data;

        var response = new DashboardStatsResponseDto
        {
            TotalOrders = orderStats.TotalOrders,
            TotalRevenue = orderStats.TotalRevenue,
            TotalCustomers = userStats.TotalCustomers,
            TotalProducts = productStats.TotalProducts,
            OrdersTrend = orderStats.OrdersTrend.Select(x => new OrderTrendResponseDto { Date = x.Date, Count = x.Count }).ToList(),
            RevenueTrend = orderStats.RevenueTrend.Select(x => new RevenueTrendResponseDto { Date = x.Date, Amount = x.Amount }).ToList()
        };

        return Ok(ApiResponse<DashboardStatsResponseDto>.Ok(response, "Dashboard statistics retrieved successfully"));
    }

    [HttpGet("order-stats")]
    [ProducesResponseType(typeof(ApiResponse<OrderStatsResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetOrderStats(CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Retrieving order statistics");

        var result = await mediator.Send(new GetOrderStatsQuery(), cancellationToken);
        return result.ToActionResult(
            data => Ok(ApiResponse<OrderStatsResponseDto>.Ok(
                new OrderStatsResponseDto
                {
                    TotalOrders = data.TotalOrders,
                    OrdersTrend = data.OrdersTrend.Select(x => new OrderTrendResponseDto { Date = x.Date, Count = x.Count }).ToList()
                },
                "Order statistics retrieved successfully")),
            MapError);
    }

    [HttpGet("user-stats")]
    [ProducesResponseType(typeof(ApiResponse<UserStatsResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetUserStats(CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Retrieving user statistics");

        var result = await mediator.Send(new GetUserStatsQuery(), cancellationToken);
        return result.ToActionResult(
            data => Ok(ApiResponse<UserStatsResponseDto>.Ok(
                new UserStatsResponseDto { TotalCustomers = data.TotalCustomers },
                "User statistics retrieved successfully")),
            MapError);
    }

    [HttpGet("revenue-stats")]
    [ProducesResponseType(typeof(ApiResponse<RevenueStatsResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetRevenueStats(CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Retrieving revenue statistics");

        var result = await mediator.Send(new GetOrderStatsQuery(), cancellationToken);
        return result.ToActionResult(
            data => Ok(ApiResponse<RevenueStatsResponseDto>.Ok(
                new RevenueStatsResponseDto
                {
                    TotalRevenue = data.TotalRevenue,
                    RevenueTrend = data.RevenueTrend.Select(x => new RevenueTrendResponseDto { Date = x.Date, Amount = x.Amount }).ToList()
                },
                "Revenue statistics retrieved successfully")),
            MapError);
    }

    private IActionResult MapError(DomainError error)
    {
        var body = ApiResponse<object>.Failure(error.Message, error.Code);

        if (_notFound.Contains(error.Code))
            return NotFound(body);

        if (_conflict.Contains(error.Code))
            return Conflict(body);

        if (_unprocessable.Contains(error.Code))
            return UnprocessableEntity(body);

        return BadRequest(body);
    }
}

