namespace ECommerce.Shopping.Application.Commands.RemoveFromWishlist;

public record RemoveFromWishlistCommand(Guid UserId, Guid ProductId)
    : IRequest<Result<WishlistDto>>, ITransactionalCommand;