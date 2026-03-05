namespace ECommerce.Application.DTOs.Dashboard;

public record RevenueTrendDto
{
    public string Date { get; init; } = null!;
    public decimal Amount { get; init; }
}
