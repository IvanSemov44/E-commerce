using ECommerce.Catalog.Infrastructure.Persistence;
using ECommerce.Identity.Infrastructure.Persistence;
using ECommerce.Infrastructure.Data;
using ECommerce.Inventory.Infrastructure.Persistence;
using ECommerce.Ordering.Infrastructure.Persistence;
using ECommerce.Promotions.Infrastructure.Persistence;
using ECommerce.Reviews.Infrastructure.Persistence;
using ECommerce.SharedKernel.Interfaces;
using ECommerce.Shopping.Infrastructure.Persistence;

namespace ECommerce.API.Behaviors;

/// <summary>
/// Coordinates MediatR command persistence across all registered bounded-context DbContexts.
/// </summary>
public sealed class CrossContextMediatRUnitOfWork(
    AppDbContext appDbContext,
    CatalogDbContext catalogDbContext,
    IdentityDbContext identityDbContext,
    InventoryDbContext inventoryDbContext,
    OrderingDbContext orderingDbContext,
    PromotionsDbContext promotionsDbContext,
    ReviewsDbContext reviewsDbContext,
    ShoppingDbContext shoppingDbContext) : IUnitOfWork
{
    public bool HasActiveTransaction =>
        appDbContext.Database.CurrentTransaction != null ||
        catalogDbContext.Database.CurrentTransaction != null ||
        identityDbContext.Database.CurrentTransaction != null ||
        inventoryDbContext.Database.CurrentTransaction != null ||
        orderingDbContext.Database.CurrentTransaction != null ||
        promotionsDbContext.Database.CurrentTransaction != null ||
        reviewsDbContext.Database.CurrentTransaction != null ||
        shoppingDbContext.Database.CurrentTransaction != null;

    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        int total = 0;
        total += await appDbContext.SaveChangesAsync(cancellationToken);
        total += await catalogDbContext.SaveChangesAsync(cancellationToken);
        total += await identityDbContext.SaveChangesAsync(cancellationToken);
        total += await inventoryDbContext.SaveChangesAsync(cancellationToken);
        total += await orderingDbContext.SaveChangesAsync(cancellationToken);
        total += await promotionsDbContext.SaveChangesAsync(cancellationToken);
        total += await reviewsDbContext.SaveChangesAsync(cancellationToken);
        total += await shoppingDbContext.SaveChangesAsync(cancellationToken);
        return total;
    }

    public async Task BeginTransactionAsync(CancellationToken cancellationToken = default)
    {
        if (appDbContext.Database.CurrentTransaction == null)
            await appDbContext.Database.BeginTransactionAsync(cancellationToken);
        if (catalogDbContext.Database.CurrentTransaction == null)
            await catalogDbContext.Database.BeginTransactionAsync(cancellationToken);
        if (identityDbContext.Database.CurrentTransaction == null)
            await identityDbContext.Database.BeginTransactionAsync(cancellationToken);
        if (inventoryDbContext.Database.CurrentTransaction == null)
            await inventoryDbContext.Database.BeginTransactionAsync(cancellationToken);
        if (orderingDbContext.Database.CurrentTransaction == null)
            await orderingDbContext.Database.BeginTransactionAsync(cancellationToken);
        if (promotionsDbContext.Database.CurrentTransaction == null)
            await promotionsDbContext.Database.BeginTransactionAsync(cancellationToken);
        if (reviewsDbContext.Database.CurrentTransaction == null)
            await reviewsDbContext.Database.BeginTransactionAsync(cancellationToken);
        if (shoppingDbContext.Database.CurrentTransaction == null)
            await shoppingDbContext.Database.BeginTransactionAsync(cancellationToken);
    }

    public async Task CommitTransactionAsync(CancellationToken cancellationToken = default)
    {
        await SaveChangesAsync(cancellationToken);

        if (appDbContext.Database.CurrentTransaction != null)
            await appDbContext.Database.CurrentTransaction.CommitAsync(cancellationToken);
        if (catalogDbContext.Database.CurrentTransaction != null)
            await catalogDbContext.Database.CurrentTransaction.CommitAsync(cancellationToken);
        if (identityDbContext.Database.CurrentTransaction != null)
            await identityDbContext.Database.CurrentTransaction.CommitAsync(cancellationToken);
        if (inventoryDbContext.Database.CurrentTransaction != null)
            await inventoryDbContext.Database.CurrentTransaction.CommitAsync(cancellationToken);
        if (orderingDbContext.Database.CurrentTransaction != null)
            await orderingDbContext.Database.CurrentTransaction.CommitAsync(cancellationToken);
        if (promotionsDbContext.Database.CurrentTransaction != null)
            await promotionsDbContext.Database.CurrentTransaction.CommitAsync(cancellationToken);
        if (reviewsDbContext.Database.CurrentTransaction != null)
            await reviewsDbContext.Database.CurrentTransaction.CommitAsync(cancellationToken);
        if (shoppingDbContext.Database.CurrentTransaction != null)
            await shoppingDbContext.Database.CurrentTransaction.CommitAsync(cancellationToken);
    }

    public async Task RollbackTransactionAsync(CancellationToken cancellationToken = default)
    {
        if (appDbContext.Database.CurrentTransaction != null)
            await appDbContext.Database.CurrentTransaction.RollbackAsync(cancellationToken);
        if (catalogDbContext.Database.CurrentTransaction != null)
            await catalogDbContext.Database.CurrentTransaction.RollbackAsync(cancellationToken);
        if (identityDbContext.Database.CurrentTransaction != null)
            await identityDbContext.Database.CurrentTransaction.RollbackAsync(cancellationToken);
        if (inventoryDbContext.Database.CurrentTransaction != null)
            await inventoryDbContext.Database.CurrentTransaction.RollbackAsync(cancellationToken);
        if (orderingDbContext.Database.CurrentTransaction != null)
            await orderingDbContext.Database.CurrentTransaction.RollbackAsync(cancellationToken);
        if (promotionsDbContext.Database.CurrentTransaction != null)
            await promotionsDbContext.Database.CurrentTransaction.RollbackAsync(cancellationToken);
        if (reviewsDbContext.Database.CurrentTransaction != null)
            await reviewsDbContext.Database.CurrentTransaction.RollbackAsync(cancellationToken);
        if (shoppingDbContext.Database.CurrentTransaction != null)
            await shoppingDbContext.Database.CurrentTransaction.RollbackAsync(cancellationToken);
    }

    public void Dispose() => GC.SuppressFinalize(this);
}
