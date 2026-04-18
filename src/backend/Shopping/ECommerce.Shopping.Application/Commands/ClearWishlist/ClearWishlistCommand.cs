namespace ECommerce.Shopping.Application.Commands.ClearWishlist;

public record ClearWishlistCommand(Guid UserId)
    : IRequest<Result<WishlistDto>>, ITransactionalCommand;