using ECommerce.Application.DTOs.Common;
using ECommerce.Application.DTOs.Dashboard;
using ECommerce.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ECommerce.API.Controllers;

/// <summary>
/// Controller for dashboard statistics and analytics.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
[Authorize(Roles = "Admin,SuperAdmin")]
public class DashboardController : ControllerBase
{
    private readonly IDashboardService _dashboardService;
    private readonly ILogger<DashboardController> _logger;

    public DashboardController(IDashboardService dashboardService, ILogger<DashboardController> logger)
    {
        _dashboardService = dashboardService;
        _logger = logger;
    }

    /// <summary>
    /// Retrieves dashboard statistics including orders, revenue, customers, and products.
    /// </summary>
    /// <returns>Dashboard statistics.</returns>
    /// <response code="200">Dashboard statistics retrieved successfully.</response>
    /// <response code="401">User is not authenticated.</response>
    /// <response code="403">User does not have permission to view dashboard statistics.</response>
    /// <response code="500">Internal server error.</response>
    [HttpGet("stats")]
    [ProducesResponseType(typeof(ApiResponse<DashboardStatsDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<DashboardStatsDto>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<DashboardStatsDto>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponse<DashboardStatsDto>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<DashboardStatsDto>>> GetStats()
    {
        try
        {
            var stats = await _dashboardService.GetDashboardStatsAsync();
            return Ok(ApiResponse<DashboardStatsDto>.Ok(stats, "Dashboard statistics retrieved successfully"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving dashboard statistics");
            return StatusCode(500, ApiResponse<DashboardStatsDto>.Error("An error occurred while retrieving dashboard statistics"));
        }
    }
}
