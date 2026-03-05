namespace ECommerce.Application.DTOs.Dashboard;

public record OrderTrendDto
{
    public string Date { get; init; } = null!;
    public int Count { get; init; }
}
