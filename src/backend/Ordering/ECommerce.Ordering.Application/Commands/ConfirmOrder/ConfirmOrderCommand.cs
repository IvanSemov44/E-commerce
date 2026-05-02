namespace ECommerce.Ordering.Application.Commands.ConfirmOrder;

public record ConfirmOrderCommand(Guid OrderId) : IRequest<Result<Guid>>, ITransactionalCommand;
