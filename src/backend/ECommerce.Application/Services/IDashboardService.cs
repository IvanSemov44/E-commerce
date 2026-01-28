using ECommerce.Application.DTOs.Dashboard;

namespace ECommerce.Application.Services;

public interface IDashboardService
{
    Task<DashboardStatsDto> GetDashboardStatsAsync();
}
