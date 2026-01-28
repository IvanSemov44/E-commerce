namespace ECommerce.Application.DTOs.Dashboard;

public class DashboardStatsDto
{
    public int TotalOrders { get; set; }
    public decimal TotalRevenue { get; set; }
    public int TotalCustomers { get; set; }
    public int TotalProducts { get; set; }
    public List<OrderTrendDto> OrdersTrend { get; set; } = new();
    public List<RevenueTrendDto> RevenueTrend { get; set; } = new();
}

public class OrderTrendDto
{
    public string Date { get; set; } = null!;
    public int Count { get; set; }
}

public class RevenueTrendDto
{
    public string Date { get; set; } = null!;
    public decimal Amount { get; set; }
}
