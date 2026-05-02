namespace ECommerce.Ordering.Application.Commands.ShipOrder;

public record ShipOrderCommand(Guid OrderId, string TrackingNumber) : IRequest<Result<Guid>>;
