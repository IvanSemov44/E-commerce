using ECommerce.SharedKernel.Domain;
using ECommerce.SharedKernel.Results;
using ECommerce.Shopping.Domain.Errors;

namespace ECommerce.Shopping.Domain.Aggregates.Wishlist;

public sealed class Wishlist : AggregateRoot
{
    public Guid UserId { get; private set; }

    private readonly List<Guid> _productIds = new();
    public IReadOnlyCollection<Guid> ProductIds => _productIds.AsReadOnly();

    private Wishlist() { }

    public static Wishlist Create(Guid userId)
        => new()
        {
            Id     = Guid.NewGuid(),
            UserId = userId,
        };

    public Result AddProduct(Guid productId)
    {
        if (_productIds.Contains(productId)) return Result.Ok();
        if (_productIds.Count >= 100)
            return Result.Fail(ShoppingErrors.WishlistFull);
        _productIds.Add(productId);
        return Result.Ok();
    }

    public void RemoveProduct(Guid productId) => _productIds.Remove(productId);

    public bool Contains(Guid productId) => _productIds.Contains(productId);

    public void Clear() => _productIds.Clear();
}