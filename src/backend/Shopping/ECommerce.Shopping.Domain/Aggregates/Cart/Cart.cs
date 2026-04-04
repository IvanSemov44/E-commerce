using ECommerce.SharedKernel.Domain;
using ECommerce.SharedKernel.Results;
using ECommerce.Shopping.Domain.Errors;
using ECommerce.Shopping.Domain.Events;

namespace ECommerce.Shopping.Domain.Aggregates.Cart;

public sealed class Cart : AggregateRoot
{
    public Guid    UserId     { get; private set; }
    public string? SessionId  { get; private set; }  // For anonymous/session-based carts
    public byte[]  RowVersion { get; private set; } = Array.Empty<byte>();

    private readonly List<CartItem> _items = new();
    public IReadOnlyCollection<CartItem> Items => _items.AsReadOnly();

    public int  ItemCount => _items.Sum(i => i.Quantity);
    public bool IsEmpty   => _items.Count == 0;

    public decimal Subtotal => _items.Sum(i => i.UnitPrice * i.Quantity);

    private Cart() { }

    public static Cart Create(Guid userId)
        => new()
        {
            Id     = Guid.NewGuid(),
            UserId = userId,
        };

    public static Cart CreateWithId(Guid id, Guid userId)
        => new()
        {
            Id     = id,
            UserId = userId,
        };

    /// <summary>
    /// Creates an anonymous (session-based) cart. UserId is set to Guid.Empty as a sentinel.
    /// </summary>
    public static Cart CreateAnonymous(string sessionId)
        => new()
        {
            Id        = Guid.NewGuid(),
            UserId    = Guid.Empty,
            SessionId = sessionId,
        };

    /// <summary>
    /// Creates an anonymous (session-based) cart with a specific ID. Used by repository when loading from database.
    /// </summary>
    public static Cart CreateAnonymousWithId(Guid id, string sessionId)
        => new()
        {
            Id        = id,
            UserId    = Guid.Empty,
            SessionId = sessionId,
        };

    public Result AddItem(Guid productId, int quantity, decimal unitPrice, string currency)
    {
        if (quantity <= 0)
            return Result.Fail(ShoppingErrors.QuantityInvalid);

        CartItem? existing = _items.FirstOrDefault(i => i.ProductId == productId);
        if (existing is not null)
        {
            existing.IncreaseQuantity(quantity);
            AddDomainEvent(new CartItemQuantityUpdatedEvent(Id, productId, existing.Quantity));
            return Result.Ok();
        }

        if (_items.Count >= 50)
            return Result.Fail(ShoppingErrors.CartFull);

        _items.Add(CartItem.Create(Guid.NewGuid(), Id, productId, quantity, unitPrice, currency));
        AddDomainEvent(new ItemAddedToCartEvent(Id, productId, quantity));
        return Result.Ok();
    }

    public Result UpdateItemQuantity(Guid cartItemId, int newQuantity)
    {
        if (newQuantity <= 0)
            return Result.Fail(ShoppingErrors.QuantityInvalid);

        CartItem? item = _items.FirstOrDefault(i => i.Id == cartItemId);
        if (item is null) return Result.Fail(ShoppingErrors.CartItemNotFound);

        item.SetQuantity(newQuantity);
        AddDomainEvent(new CartItemQuantityUpdatedEvent(Id, item.ProductId, newQuantity));
        return Result.Ok();
    }

    public Result RemoveItem(Guid cartItemId)
    {
        CartItem? item = _items.FirstOrDefault(i => i.Id == cartItemId);
        if (item is null) return Result.Fail(ShoppingErrors.CartItemNotFound);

        _items.Remove(item);
        return Result.Ok();
    }

    public void Clear()
    {
        _items.Clear();
        AddDomainEvent(new CartClearedEvent(Id, UserId));
    }
}