namespace ECommerce.Ordering.Application.Commands.DeliverOrder;

public record DeliverOrderCommand(Guid OrderId) : IRequest<Result<Guid>>;
