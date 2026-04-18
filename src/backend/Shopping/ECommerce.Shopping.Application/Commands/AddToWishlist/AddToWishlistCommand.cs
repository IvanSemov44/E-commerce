namespace ECommerce.Shopping.Application.Commands.AddToWishlist;

public record AddToWishlistCommand(Guid UserId, Guid ProductId)
    : IRequest<Result>, ITransactionalCommand;