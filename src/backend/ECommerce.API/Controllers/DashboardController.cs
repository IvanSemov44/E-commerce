using ECommerce.Application.DTOs.Common;
using ECommerce.Application.DTOs.Dashboard;
using ECommerce.Application.Interfaces;
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
    /// Retrieves dashboard statistics including sales, revenue, and order metrics (admin only).
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Comprehensive dashboard statistics and analytics.</returns>
    /// <response code="200">Dashboard statistics retrieved successfully.</response>
    /// <response code="401">User is not authenticated.</response>
    /// <response code="403">User does not have permission to view dashboard statistics.</response>
    [HttpGet("stats")]
    [ProducesResponseType(typeof(ApiResponse<DashboardStatsDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorDetails), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetStats(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Retrieving dashboard statistics");
        var stats = await _dashboardService.GetDashboardStatsAsync(cancellationToken: cancellationToken);
        return Ok(ApiResponse<DashboardStatsDto>.Ok(stats, "Dashboard statistics retrieved successfully"));
    }
}
