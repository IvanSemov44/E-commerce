using ECommerce.SharedKernel.Domain;

namespace ECommerce.Shopping.Domain.Aggregates.Cart;

public sealed class CartItem : Entity
{
    public Guid    CartId    { get; private set; }
    public Guid    ProductId { get; private set; }
    public int     Quantity  { get; private set; }
    public decimal UnitPrice { get; private set; }
    public string  Currency  { get; private set; } = null!;

    private CartItem() { }

    internal static CartItem Create(
        Guid id, Guid cartId, Guid productId,
        int quantity, decimal unitPrice, string currency)
        => new()
        {
            Id        = id,
            CartId    = cartId,
            ProductId = productId,
            Quantity  = quantity,
            UnitPrice = unitPrice,
            Currency  = currency,
        };

    internal void IncreaseQuantity(int amount) => Quantity += amount;
    internal void SetQuantity(int quantity)    => Quantity = quantity;
}