namespace ECommerce.Application.DTOs.Wishlist;

public record WishlistItemDto
{
    public Guid Id { get; init; }
    public Guid ProductId { get; init; }
    public string ProductName { get; init; } = null!;
    public string? ProductImage { get; init; }
    public decimal Price { get; init; }
    public decimal? CompareAtPrice { get; init; }
    public int StockQuantity { get; init; }
    public bool IsAvailable { get; init; }
    public DateTime AddedAt { get; init; }
}
