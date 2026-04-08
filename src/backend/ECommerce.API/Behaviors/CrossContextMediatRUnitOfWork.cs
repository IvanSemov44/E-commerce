using ECommerce.Catalog.Infrastructure.Persistence;
using ECommerce.Identity.Infrastructure.Persistence;
using ECommerce.Infrastructure.Data;
using ECommerce.Inventory.Infrastructure.Persistence;
using ECommerce.Ordering.Infrastructure.Persistence;
using ECommerce.Payments.Infrastructure.Persistence;
using ECommerce.Promotions.Infrastructure.Persistence;
using ECommerce.Reviews.Infrastructure.Persistence;
using ECommerce.SharedKernel.Interfaces;
using ECommerce.Shopping.Infrastructure.Persistence;

namespace ECommerce.API.Behaviors;

/// <summary>
/// Transitional unit of work that coordinates SaveChanges across registered DbContexts.
///
/// Phase 10 BR-002 removed explicit multi-DbContext transaction orchestration because
/// it creates pseudo-distributed transaction assumptions. Transaction boundary must be
/// local to one context and cross-context consistency should rely on outbox/events.
/// </summary>
public sealed class CrossContextMediatRUnitOfWork(
    AppDbContext appDbContext,
    CatalogDbContext catalogDbContext,
    IdentityDbContext identityDbContext,
    InventoryDbContext inventoryDbContext,
    OrderingDbContext orderingDbContext,
    PaymentsDbContext paymentsDbContext,
    PromotionsDbContext promotionsDbContext,
    ReviewsDbContext reviewsDbContext,
    ShoppingDbContext shoppingDbContext) : IUnitOfWork
{
    public bool HasActiveTransaction => false;

    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        int total = 0;
        total += await appDbContext.SaveChangesAsync(cancellationToken);
        total += await catalogDbContext.SaveChangesAsync(cancellationToken);
        total += await identityDbContext.SaveChangesAsync(cancellationToken);
        total += await inventoryDbContext.SaveChangesAsync(cancellationToken);
        total += await orderingDbContext.SaveChangesAsync(cancellationToken);
        total += await paymentsDbContext.SaveChangesAsync(cancellationToken);
        total += await promotionsDbContext.SaveChangesAsync(cancellationToken);
        total += await reviewsDbContext.SaveChangesAsync(cancellationToken);
        total += await shoppingDbContext.SaveChangesAsync(cancellationToken);
        return total;
    }

    public Task BeginTransactionAsync(CancellationToken cancellationToken = default)
        => Task.CompletedTask;

    public async Task CommitTransactionAsync(CancellationToken cancellationToken = default)
    {
        // Keep SaveChanges behavior for compatibility while migration slices move
        // handlers fully to local context transaction + outbox/event boundaries.
        await SaveChangesAsync(cancellationToken);
    }

    public Task RollbackTransactionAsync(CancellationToken cancellationToken = default)
        => Task.CompletedTask;

    public void Dispose() => GC.SuppressFinalize(this);
}
