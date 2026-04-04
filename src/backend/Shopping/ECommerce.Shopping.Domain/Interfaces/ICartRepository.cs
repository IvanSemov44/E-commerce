using ECommerce.Shopping.Domain.Aggregates.Cart;

namespace ECommerce.Shopping.Domain.Interfaces;

public interface ICartRepository
{
    Task<Cart?>  GetByUserIdAsync(Guid userId, CancellationToken ct = default);
    Task<Cart?>  GetByIdAsync(Guid cartId, CancellationToken ct = default);
    Task UpsertAsync(Cart cart, CancellationToken ct = default);
    Task DeleteAsync(Cart cart, CancellationToken ct = default);
}