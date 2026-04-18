namespace ECommerce.Shopping.Application.Commands.ClearCart;

public record ClearCartCommand(Guid? UserId, string? SessionId)
    : IRequest<Result<CartDto>>, ITransactionalCommand;