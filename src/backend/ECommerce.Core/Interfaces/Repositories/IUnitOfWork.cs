using ECommerce.Core.Entities;

namespace ECommerce.Core.Interfaces.Repositories;

/// <summary>
/// Unit of Work pattern interface for managing repositories and database transactions.
/// Coordinates multiple repository operations and ensures data consistency.
/// Supports CancellationToken for graceful async cancellation.
/// </summary>
public interface IUnitOfWork : IDisposable, IAsyncDisposable
{
    #region Repositories

    /// <summary>
    /// Gets the product repository with specialized product operations.
    /// </summary>
    IProductRepository Products { get; }

    /// <summary>
    /// Gets the order repository with specialized order operations.
    /// </summary>
    IOrderRepository Orders { get; }

    /// <summary>
    /// Gets the user repository with specialized user operations.
    /// </summary>
    IUserRepository Users { get; }

    /// <summary>
    /// Gets the category repository with specialized category operations.
    /// </summary>
    ICategoryRepository Categories { get; }

    /// <summary>
    /// Gets the cart repository with specialized cart operations.
    /// </summary>
    ICartRepository Carts { get; }

    /// <summary>
    /// Gets the review repository with specialized review operations.
    /// </summary>
    IReviewRepository Reviews { get; }

    /// <summary>
    /// Gets the wishlist repository with specialized wishlist operations.
    /// </summary>
    IWishlistRepository Wishlists { get; }

    /// <summary>
    /// Generic repository for order items (simple CRUD operations).
    /// </summary>
    IRepository<OrderItem> OrderItems { get; }

    /// <summary>
    /// Generic repository for cart items (simple CRUD operations).
    /// </summary>
    IRepository<CartItem> CartItems { get; }

    /// <summary>
    /// Generic repository for addresses (simple CRUD operations).
    /// </summary>
    IRepository<Address> Addresses { get; }

    /// <summary>
    /// Generic repository for promo codes (simple CRUD operations).
    /// </summary>
    IRepository<PromoCode> PromoCodes { get; }

    /// <summary>
    /// Generic repository for inventory logs (simple CRUD operations).
    /// </summary>
    IRepository<InventoryLog> InventoryLogs { get; }

    /// <summary>
    /// Generic repository for product images (simple CRUD operations).
    /// </summary>
    IRepository<ProductImage> ProductImages { get; }

    #endregion

    #region Transaction Management

    /// <summary>
    /// Saves all changes made in the unit of work to the database.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>The number of entities written to the database.</returns>
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Begins a new database transaction.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>The transaction object that must be committed or rolled back.</returns>
    Task<IAsyncTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default);

    #endregion
}

/// <summary>
/// Represents an async database transaction that can be committed or rolled back.
/// </summary>
public interface IAsyncTransaction : IAsyncDisposable
{
    /// <summary>
    /// Commits the transaction.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    Task CommitAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Rolls back the transaction.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    Task RollbackAsync(CancellationToken cancellationToken = default);
}
