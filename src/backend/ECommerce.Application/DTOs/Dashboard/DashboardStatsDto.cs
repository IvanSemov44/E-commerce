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

public record OrderTrendDto
{
    public string Date { get; init; } = null!;
    public int Count { get; init; }
}

public record RevenueTrendDto
{
    public string Date { get; init; } = null!;
    public decimal Amount { get; init; }
}
