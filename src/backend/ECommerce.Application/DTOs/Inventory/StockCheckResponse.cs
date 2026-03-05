namespace ECommerce.Application.DTOs.Inventory;

public record StockCheckResponse
{
    public bool IsAvailable { get; init; }
    public List<StockIssueDto> Issues { get; init; } = new();
}
