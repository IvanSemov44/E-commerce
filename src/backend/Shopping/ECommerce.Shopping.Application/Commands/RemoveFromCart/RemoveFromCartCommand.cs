namespace ECommerce.Shopping.Application.Commands.RemoveFromCart;

public record RemoveFromCartCommand(
    Guid? UserId,
    string? SessionId,
    Guid CartItemId
) : IRequest<Result<CartDto>>, ITransactionalCommand;