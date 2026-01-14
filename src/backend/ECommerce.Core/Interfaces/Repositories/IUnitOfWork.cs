using ECommerce.Core.Entities;

namespace ECommerce.Core.Interfaces.Repositories;

public interface IUnitOfWork : IDisposable, IAsyncDisposable
{
    IRepository<User> Users { get; }
    IRepository<Product> Products { get; }
    IRepository<Category> Categories { get; }
    IRepository<Order> Orders { get; }
    IRepository<OrderItem> OrderItems { get; }
    IRepository<Cart> Carts { get; }
    IRepository<CartItem> CartItems { get; }
    IRepository<Address> Addresses { get; }
    IRepository<Review> Reviews { get; }
    IRepository<PromoCode> PromoCodes { get; }
    IRepository<Wishlist> Wishlists { get; }
    IRepository<InventoryLog> InventoryLogs { get; }
    IRepository<ProductImage> ProductImages { get; }

    IProductRepository ProductRepository { get; }
    IOrderRepository OrderRepository { get; }
    IUserRepository UserRepository { get; }

    Task<int> SaveChangesAsync();
    Task<IAsyncTransaction> BeginTransactionAsync();
}

public interface IAsyncTransaction : IAsyncDisposable
{
    Task CommitAsync();
    Task RollbackAsync();
}
