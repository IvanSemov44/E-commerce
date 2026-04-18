namespace ECommerce.Shopping.Application.Queries.IsProductInWishlist;

public record IsProductInWishlistQuery(Guid UserId, Guid ProductId) : IRequest<Result<bool>>;