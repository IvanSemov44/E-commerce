namespace ECommerce.Application.DTOs.Wishlist;

public class WishlistItemDto
{
    public Guid Id { get; set; }
    public Guid ProductId { get; set; }
    public string ProductName { get; set; } = null!;
    public string? ProductImage { get; set; }
    public decimal Price { get; set; }
    public decimal? CompareAtPrice { get; set; }
    public int StockQuantity { get; set; }
    public bool IsAvailable { get; set; }
    public DateTime AddedAt { get; set; }
}

public class WishlistDto
{
    public Guid Id { get; set; }
    public List<WishlistItemDto> Items { get; set; } = new();
    public int ItemCount { get; set; }
}

public class AddToWishlistDto
{
    public Guid ProductId { get; set; }
}
