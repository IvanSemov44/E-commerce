using MediatR;
using ECommerce.SharedKernel.Results;
using ECommerce.Ordering.Application.DTOs;

namespace ECommerce.Ordering.Application.Commands.PlaceOrder;

public record PlaceOrderCommand : IRequest<Result<OrderDto>>
{
    public required Guid UserId { get; init; }
    public required Guid ShippingAddressId { get; init; }
    public required List<CartItemInput> CartItems { get; init; }
    public string? PromoCode { get; init; }
    public required string PaymentReference { get; init; }
    public required string PaymentMethod { get; init; }
    public decimal ShippingCost { get; init; }
    public decimal TaxAmount { get; init; }
}

public record CartItemInput(Guid ProductId, int Quantity);

public record PlaceOrderResponse(Guid OrderId, string OrderNumber);
