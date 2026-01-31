using ECommerce.Core.Entities;

namespace ECommerce.Core.Interfaces.Repositories;

public interface IUnitOfWork : IDisposable, IAsyncDisposable
{
    // Specialized repositories (preferred for entities with custom methods)
    IProductRepository Products { get; }
    IOrderRepository Orders { get; }
    IUserRepository Users { get; }
    ICategoryRepository Categories { get; }
    ICartRepository Carts { get; }
    IReviewRepository Reviews { get; }
    IWishlistRepository Wishlists { get; }

    // Generic repositories for simple entities without specialized methods
    IRepository<OrderItem> OrderItems { get; }
    IRepository<CartItem> CartItems { get; }
    IRepository<Address> Addresses { get; }
    IRepository<PromoCode> PromoCodes { get; }
    IRepository<InventoryLog> InventoryLogs { get; }
    IRepository<ProductImage> ProductImages { get; }

    // Transaction management
    Task<int> SaveChangesAsync();
    Task<IAsyncTransaction> BeginTransactionAsync();
}

public interface IAsyncTransaction : IAsyncDisposable
{
    Task CommitAsync();
    Task RollbackAsync();
}
