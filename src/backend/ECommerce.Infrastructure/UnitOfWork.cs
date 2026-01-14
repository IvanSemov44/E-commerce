using ECommerce.Core.Entities;
using ECommerce.Core.Interfaces.Repositories;
using ECommerce.Infrastructure.Data;
using ECommerce.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore.Storage;

namespace ECommerce.Infrastructure;

public class UnitOfWork : IUnitOfWork
{
    private readonly AppDbContext _context;
    private IRepository<User>? _users;
    private IRepository<Product>? _products;
    private IRepository<Category>? _categories;
    private IRepository<Order>? _orders;
    private IRepository<OrderItem>? _orderItems;
    private IRepository<Cart>? _carts;
    private IRepository<CartItem>? _cartItems;
    private IRepository<Address>? _addresses;
    private IRepository<Review>? _reviews;
    private IRepository<PromoCode>? _promoCodes;
    private IRepository<Wishlist>? _wishlists;
    private IRepository<InventoryLog>? _inventoryLogs;
    private IRepository<ProductImage>? _productImages;
    private IProductRepository? _productRepository;
    private IOrderRepository? _orderRepository;
    private IUserRepository? _userRepository;

    public UnitOfWork(AppDbContext context)
    {
        _context = context;
    }

    public IRepository<User> Users => _users ??= new Repository<User>(_context);
    public IRepository<Product> Products => _products ??= new Repository<Product>(_context);
    public IRepository<Category> Categories => _categories ??= new Repository<Category>(_context);
    public IRepository<Order> Orders => _orders ??= new Repository<Order>(_context);
    public IRepository<OrderItem> OrderItems => _orderItems ??= new Repository<OrderItem>(_context);
    public IRepository<Cart> Carts => _carts ??= new Repository<Cart>(_context);
    public IRepository<CartItem> CartItems => _cartItems ??= new Repository<CartItem>(_context);
    public IRepository<Address> Addresses => _addresses ??= new Repository<Address>(_context);
    public IRepository<Review> Reviews => _reviews ??= new Repository<Review>(_context);
    public IRepository<PromoCode> PromoCodes => _promoCodes ??= new Repository<PromoCode>(_context);
    public IRepository<Wishlist> Wishlists => _wishlists ??= new Repository<Wishlist>(_context);
    public IRepository<InventoryLog> InventoryLogs => _inventoryLogs ??= new Repository<InventoryLog>(_context);
    public IRepository<ProductImage> ProductImages => _productImages ??= new Repository<ProductImage>(_context);

    public IProductRepository ProductRepository => _productRepository ??= new ProductRepository(_context);
    public IOrderRepository OrderRepository => _orderRepository ??= new OrderRepository(_context);
    public IUserRepository UserRepository => _userRepository ??= new UserRepository(_context);

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
