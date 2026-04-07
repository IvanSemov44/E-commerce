using System.Globalization;

namespace ECommerce.Ordering.Application.DTOs.Dashboard;

public record OrderStatsDto
{
    public int TotalOrders { get; init; }
    public decimal TotalRevenue { get; init; }
    public List<OrderTrendPointDto> OrdersTrend { get; init; } = new();
    public List<RevenueTrendPointDto> RevenueTrend { get; init; } = new();
}

public record OrderTrendPointDto
{
    public string Date { get; init; } = null!;
    public int Count { get; init; }

    public static OrderTrendPointDto From(DateTime date, int count) => new()
    {
        Date = date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
        Count = count
    };
}

public record RevenueTrendPointDto
{
    public string Date { get; init; } = null!;
    public decimal Amount { get; init; }

    public static RevenueTrendPointDto From(DateTime date, decimal amount) => new()
    {
        Date = date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
        Amount = amount
    };
}
