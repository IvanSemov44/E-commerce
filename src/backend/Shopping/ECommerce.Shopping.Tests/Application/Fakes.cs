using ECommerce.Shopping.Application.Interfaces;
using ECommerce.Shopping.Domain.Aggregates.Cart;
using ECommerce.Shopping.Domain.Aggregates.Wishlist;
using ECommerce.Shopping.Domain.Interfaces;
using ECommerce.SharedKernel.Interfaces;

namespace ECommerce.Shopping.Tests.Application;

sealed class FakeCartRepository : ICartRepository
{
    public List<Cart> Store = new();

    public Task<Cart?> GetByUserIdAsync(Guid userId, CancellationToken ct = default)
        => Task.FromResult(Store.FirstOrDefault(c => c.UserId == userId));

    public Task<Cart?> GetByIdAsync(Guid cartId, CancellationToken ct = default)
        => Task.FromResult(Store.FirstOrDefault(c => c.Id == cartId));

    public Task<Cart?> GetBySessionIdAsync(string sessionId, CancellationToken ct = default)
        => Task.FromResult(Store.FirstOrDefault(c => c.SessionId == sessionId));

    public Task UpsertAsync(Cart cart, CancellationToken ct = default)
    {
        Store.RemoveAll(c => c.Id == cart.Id);
        Store.Add(cart);
        return Task.CompletedTask;
    }

    public Task DeleteAsync(Cart cart, CancellationToken ct = default)
    {
        Store.RemoveAll(c => c.Id == cart.Id);
        return Task.CompletedTask;
    }
}

sealed class FakeWishlistRepository : IWishlistRepository
{
    public List<Wishlist> Store = new();

    public Task<Wishlist?> GetByUserIdAsync(Guid userId, CancellationToken ct = default)
        => Task.FromResult(Store.FirstOrDefault(w => w.UserId == userId));

    public Task UpsertAsync(Wishlist wishlist, CancellationToken ct = default)
    {
        Store.RemoveAll(w => w.UserId == wishlist.UserId);
        Store.Add(wishlist);
        return Task.CompletedTask;
    }
}

sealed class FakeShoppingProductReader : IShoppingProductReader
{
    public Dictionary<Guid, (decimal Price, string Currency)> Products = new();

    public Task<ProductPriceInfo?> GetProductPriceAsync(Guid productId, CancellationToken ct)
    {
        if (Products.TryGetValue(productId, out var info))
            return Task.FromResult<ProductPriceInfo?>(new ProductPriceInfo(info.Price, info.Currency));
        return Task.FromResult<ProductPriceInfo?>(null);
    }
}

sealed class FakeStockAvailabilityReader : IStockAvailabilityReader
{
    public Task<bool> IsInStockAsync(Guid productId, int quantity, CancellationToken ct)
        => Task.FromResult(true);
}

sealed class FakeUnitOfWork : IUnitOfWork
{
    public int SaveChangesCount;

    public Task<int> SaveChangesAsync(CancellationToken ct = default)
    {
        SaveChangesCount++;
        return Task.FromResult(1);
    }

    public Task BeginTransactionAsync(CancellationToken ct = default)
        => Task.CompletedTask;

    public Task CommitTransactionAsync(CancellationToken ct = default)
        => Task.CompletedTask;

    public Task RollbackTransactionAsync(CancellationToken ct = default)
        => Task.CompletedTask;

    public bool HasActiveTransaction => false;

    public void Dispose() { }
}
