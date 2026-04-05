using MediatR;
using ECommerce.SharedKernel.Results;
using ECommerce.Ordering.Application.DTOs;

namespace ECommerce.Ordering.Application.Commands.ShipOrder;

public record ShipOrderCommand(Guid OrderId, string TrackingNumber) : IRequest<Result<OrderDto>>;
