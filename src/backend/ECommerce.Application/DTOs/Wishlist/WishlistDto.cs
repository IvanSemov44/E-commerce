namespace ECommerce.Application.DTOs.Wishlist;

public record WishlistDto
{
    public Guid Id { get; init; }
    public List<WishlistItemDto> Items { get; init; } = new();
    public int ItemCount { get; init; }
}
