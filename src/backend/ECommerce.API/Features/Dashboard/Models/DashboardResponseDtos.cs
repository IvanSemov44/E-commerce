namespace ECommerce.API.Features.Dashboard.Models;

public record DashboardStatsResponseDto
{
    public int TotalOrders { get; init; }
    public decimal TotalRevenue { get; init; }
    public int TotalCustomers { get; init; }
    public int TotalProducts { get; init; }
    public List<OrderTrendResponseDto> OrdersTrend { get; init; } = new();
    public List<RevenueTrendResponseDto> RevenueTrend { get; init; } = new();
}

public record OrderStatsResponseDto
{
    public int TotalOrders { get; init; }
    public List<OrderTrendResponseDto> OrdersTrend { get; init; } = new();
}

public record UserStatsResponseDto
{
    public int TotalCustomers { get; init; }
}

public record RevenueStatsResponseDto
{
    public decimal TotalRevenue { get; init; }
    public List<RevenueTrendResponseDto> RevenueTrend { get; init; } = new();
}

public record OrderTrendResponseDto
{
    public string Date { get; init; } = null!;
    public int Count { get; init; }
}

public record RevenueTrendResponseDto
{
    public string Date { get; init; } = null!;
    public decimal Amount { get; init; }
}
