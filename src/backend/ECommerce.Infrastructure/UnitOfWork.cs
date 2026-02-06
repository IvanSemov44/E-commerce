using ECommerce.Core.Entities;
using ECommerce.Core.Interfaces.Repositories;
using ECommerce.Infrastructure.Data;
using ECommerce.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore.Storage;

namespace ECommerce.Infrastructure;

/// <summary>
/// Unit of Work pattern implementation managing repositories and transactions.
/// Coordinates multiple repository operations and ensures data consistency.
/// Supports CancellationToken for graceful async cancellation.
/// </summary>
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
    private IRepository<RefreshToken>? _refreshTokens;

    /// <summary>
    /// Initializes a new instance of the UnitOfWork class.
    /// </summary>
    /// <param name="context">The database context.</param>
    public UnitOfWork(AppDbContext context)
    {
        _context = context;
    }

    #region Repositories

    /// <summary>
    /// Gets the product repository with lazy initialization.
    /// </summary>
    public IProductRepository Products => _products ??= new ProductRepository(_context);

    /// <summary>
    /// Gets the order repository with lazy initialization.
    /// </summary>
    public IOrderRepository Orders => _orders ??= new OrderRepository(_context);

    /// <summary>
    /// Gets the user repository with lazy initialization.
    /// </summary>
    public IUserRepository Users => _users ??= new UserRepository(_context);

    /// <summary>
    /// Gets the category repository with lazy initialization.
    /// </summary>
    public ICategoryRepository Categories => _categories ??= new CategoryRepository(_context);

    /// <summary>
    /// Gets the cart repository with lazy initialization.
    /// </summary>
    public ICartRepository Carts => _carts ??= new CartRepository(_context);

    /// <summary>
    /// Gets the review repository with lazy initialization.
    /// </summary>
    public IReviewRepository Reviews => _reviews ??= new ReviewRepository(_context);

    /// <summary>
    /// Gets the wishlist repository with lazy initialization.
    /// </summary>
    public IWishlistRepository Wishlists => _wishlists ??= new WishlistRepository(_context);

    /// <summary>
    /// Gets the generic order items repository with lazy initialization.
    /// </summary>
    public IRepository<OrderItem> OrderItems => _orderItems ??= new Repository<OrderItem>(_context);

    /// <summary>
    /// Gets the generic cart items repository with lazy initialization.
    /// </summary>
    public IRepository<CartItem> CartItems => _cartItems ??= new Repository<CartItem>(_context);

    /// <summary>
    /// Gets the generic addresses repository with lazy initialization.
    /// </summary>
    public IRepository<Address> Addresses => _addresses ??= new Repository<Address>(_context);

    /// <summary>
    /// Gets the generic promo codes repository with lazy initialization.
    /// </summary>
    public IRepository<PromoCode> PromoCodes => _promoCodes ??= new Repository<PromoCode>(_context);

    /// <summary>
    /// Gets the generic inventory logs repository with lazy initialization.
    /// </summary>
    public IRepository<InventoryLog> InventoryLogs => _inventoryLogs ??= new Repository<InventoryLog>(_context);

    /// <summary>
    /// Gets the generic product images repository with lazy initialization.
    /// </summary>
    public IRepository<ProductImage> ProductImages => _productImages ??= new Repository<ProductImage>(_context);

    /// <summary>
    /// Gets the generic refresh tokens repository with lazy initialization.
    /// </summary>
    public IRepository<RefreshToken> RefreshTokens => _refreshTokens ??= new Repository<RefreshToken>(_context);

    #endregion

    #region Transaction Management

    /// <summary>
    /// Saves all changes made in the unit of work to the database.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>The number of entities written to the database.</returns>
    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return await _context.SaveChangesAsync(cancellationToken);
    }

    /// <summary>
    /// Begins a new database transaction.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>The transaction object that must be committed or rolled back.</returns>
    public async Task<IAsyncTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default)
    {
        var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
        return new AsyncTransaction(transaction);
    }

    #endregion

    #region Disposal

    /// <summary>
    /// Disposes the unit of work and its context.
    /// </summary>
    public void Dispose()
    {
        _context?.Dispose();
    }

    /// <summary>
    /// Asynchronously disposes the unit of work and its context.
    /// </summary>
    public ValueTask DisposeAsync()
    {
        return _context?.DisposeAsync() ?? default;
    }

    #endregion

    /// <summary>
    /// Wraps IDbContextTransaction to implement IAsyncTransaction.
    /// </summary>
    private class AsyncTransaction : IAsyncTransaction
    {
        private readonly IDbContextTransaction _transaction;

        /// <summary>
        /// Initializes a new instance of the AsyncTransaction class.
        /// </summary>
        /// <param name="transaction">The database transaction to wrap.</param>
        public AsyncTransaction(IDbContextTransaction transaction)
        {
            _transaction = transaction;
        }

        /// <summary>
        /// Commits the transaction.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token for the operation.</param>
        public async Task CommitAsync(CancellationToken cancellationToken = default)
        {
            await _transaction.CommitAsync(cancellationToken);
        }

        /// <summary>
        /// Rolls back the transaction.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token for the operation.</param>
        public async Task RollbackAsync(CancellationToken cancellationToken = default)
        {
            await _transaction.RollbackAsync(cancellationToken);
        }

        /// <summary>
        /// Disposes the transaction.
        /// </summary>
        public async ValueTask DisposeAsync()
        {
            await _transaction.DisposeAsync();
        }
    }
}
