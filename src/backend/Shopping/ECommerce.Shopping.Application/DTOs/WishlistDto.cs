namespace ECommerce.Shopping.Application.DTOs;

public record WishlistDto(
    Guid        Id,
    Guid        UserId,
    List<Guid>  ProductIds
);