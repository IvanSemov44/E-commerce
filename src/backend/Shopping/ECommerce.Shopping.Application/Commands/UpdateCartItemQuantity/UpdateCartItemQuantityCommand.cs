
namespace ECommerce.Shopping.Application.Commands.UpdateCartItemQuantity;

public record UpdateCartItemQuantityCommand(
    Guid? UserId,
    string? SessionId,
    Guid CartItemId,
    int  NewQuantity
) : IRequest<Result<CartDto>>, ITransactionalCommand;