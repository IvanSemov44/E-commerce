using ECommerce.Application.DTOs.Dashboard;

namespace ECommerce.Application.Interfaces;

/// <summary>
/// Service interface for dashboard statistics and analytics.
/// </summary>
public interface IDashboardService
{
    Task<DashboardStatsDto> GetDashboardStatsAsync();
}
