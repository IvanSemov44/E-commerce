using ECommerce.SharedKernel.Domain;

namespace ECommerce.Ordering.Domain.Aggregates.Order;

public sealed class OrderItem : Entity
{
    public Guid OrderId { get; private set; }
    public Guid ProductId { get; private set; }
    public string ProductName { get; private set; } = null!;
    public decimal UnitPrice { get; private set; }
    public int Quantity { get; private set; }
    public string? ProductImageUrl { get; private set; }

    private OrderItem() { }

    internal static OrderItem Create(Guid id, Guid orderId, Guid productId, string productName, decimal unitPrice, int quantity, string? imageUrl)
    {
        return new OrderItem
        {
            Id = id,
            OrderId = orderId,
            ProductId = productId,
            ProductName = productName,
            UnitPrice = unitPrice,
            Quantity = quantity,
            ProductImageUrl = imageUrl
        };
    }
}
