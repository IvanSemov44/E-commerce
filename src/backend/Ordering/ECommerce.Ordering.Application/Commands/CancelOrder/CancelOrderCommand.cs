namespace ECommerce.Ordering.Application.Commands.CancelOrder;

public record CancelOrderCommand(Guid OrderId, string Reason) : IRequest<Result<Guid>>, ITransactionalCommand;
