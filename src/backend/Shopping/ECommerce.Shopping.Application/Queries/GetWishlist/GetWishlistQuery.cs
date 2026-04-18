
namespace ECommerce.Shopping.Application.Queries.GetWishlist;

public record GetWishlistQuery(Guid UserId) : IRequest<Result<WishlistDto>>;