namespace ECommerce.Application.DTOs.Dashboard;

public record DashboardStatsDto
{
    public int TotalOrders { get; init; }
    public decimal TotalRevenue { get; init; }
    public int TotalCustomers { get; init; }
    public int TotalProducts { get; init; }
    public List<OrderTrendDto> OrdersTrend { get; init; } = new();
    public List<RevenueTrendDto> RevenueTrend { get; init; } = new();
}
