using ECommerce.Core.Entities;
using ECommerce.Core.Interfaces.Repositories;
using ECommerce.Infrastructure.Data;
using ECommerce.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore.Storage;

namespace ECommerce.Infrastructure;

public class UnitOfWork : IUnitOfWork
{
    private readonly AppDbContext _context;

    // Specialized repositories
    private IProductRepository? _products;
    private IOrderRepository? _orders;
    private IUserRepository? _users;
    private ICategoryRepository? _categories;
    private ICartRepository? _carts;
    private IReviewRepository? _reviews;
    private IWishlistRepository? _wishlists;

    // Generic repositories for simple entities
    private IRepository<OrderItem>? _orderItems;
    private IRepository<CartItem>? _cartItems;
    private IRepository<Address>? _addresses;
    private IRepository<PromoCode>? _promoCodes;
    private IRepository<InventoryLog>? _inventoryLogs;
    private IRepository<ProductImage>? _productImages;

    public UnitOfWork(AppDbContext context)
    {
        _context = context;
    }

    // Specialized repositories
    public IProductRepository Products => _products ??= new ProductRepository(_context);
    public IOrderRepository Orders => _orders ??= new OrderRepository(_context);
    public IUserRepository Users => _users ??= new UserRepository(_context);
    public ICategoryRepository Categories => _categories ??= new CategoryRepository(_context);
    public ICartRepository Carts => _carts ??= new CartRepository(_context);
    public IReviewRepository Reviews => _reviews ??= new ReviewRepository(_context);
    public IWishlistRepository Wishlists => _wishlists ??= new WishlistRepository(_context);

    // Generic repositories
    public IRepository<OrderItem> OrderItems => _orderItems ??= new Repository<OrderItem>(_context);
    public IRepository<CartItem> CartItems => _cartItems ??= new Repository<CartItem>(_context);
    public IRepository<Address> Addresses => _addresses ??= new Repository<Address>(_context);
    public IRepository<PromoCode> PromoCodes => _promoCodes ??= new Repository<PromoCode>(_context);
    public IRepository<InventoryLog> InventoryLogs => _inventoryLogs ??= new Repository<InventoryLog>(_context);
    public IRepository<ProductImage> ProductImages => _productImages ??= new Repository<ProductImage>(_context);

    public async Task<int> SaveChangesAsync()
    {
        return await _context.SaveChangesAsync();
    }

    public async Task<IAsyncTransaction> BeginTransactionAsync()
    {
        var transaction = await _context.Database.BeginTransactionAsync();
        return new AsyncTransaction(transaction);
    }

    public void Dispose()
    {
        _context?.Dispose();
    }

    public ValueTask DisposeAsync()
    {
        return _context?.DisposeAsync() ?? default;
    }

    private class AsyncTransaction : IAsyncTransaction
    {
        private readonly IDbContextTransaction _transaction;

        public AsyncTransaction(IDbContextTransaction transaction)
        {
            _transaction = transaction;
        }

        public async Task CommitAsync()
        {
            await _transaction.CommitAsync();
        }

        public async Task RollbackAsync()
        {
            await _transaction.RollbackAsync();
        }

        public async ValueTask DisposeAsync()
        {
            await _transaction.DisposeAsync();
        }
    }
}
