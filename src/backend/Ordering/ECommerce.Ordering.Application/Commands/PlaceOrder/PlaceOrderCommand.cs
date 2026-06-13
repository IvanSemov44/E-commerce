namespace ECommerce.Ordering.Application.Commands.PlaceOrder;

public record PlaceOrderCommand : IRequest<Result<Guid>>, ITransactionalCommand
{
    public required Guid UserId { get; init; }
    public required Guid ShippingAddressId { get; init; }
    public required List<CartItemInput> CartItems { get; init; }
    public string? PromoCode { get; init; }
    public required string PaymentMethod { get; init; }
    public decimal ShippingCost { get; init; }
    public decimal TaxRate { get; init; }
}

public record CartItemInput(Guid ProductId, int Quantity);
