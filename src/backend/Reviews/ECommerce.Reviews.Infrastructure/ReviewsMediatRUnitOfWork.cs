using ECommerce.Infrastructure.Data;
using ECommerce.Reviews.Infrastructure.Persistence;
using ECommerce.SharedKernel.Interfaces;

namespace ECommerce.Reviews.Infrastructure;

public sealed class ReviewsMediatRUnitOfWork(
    AppDbContext appDbContext,
    ReviewsDbContext reviewsDbContext) : IUnitOfWork
{
    public bool HasActiveTransaction =>
        appDbContext.Database.CurrentTransaction != null ||
        reviewsDbContext.Database.CurrentTransaction != null;

    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        int appChanges = await appDbContext.SaveChangesAsync(cancellationToken);
        int reviewChanges = await reviewsDbContext.SaveChangesAsync(cancellationToken);
        return appChanges + reviewChanges;
    }

    public async Task BeginTransactionAsync(CancellationToken cancellationToken = default)
    {
        if (appDbContext.Database.CurrentTransaction == null)
            await appDbContext.Database.BeginTransactionAsync(cancellationToken);

        if (reviewsDbContext.Database.CurrentTransaction == null)
            await reviewsDbContext.Database.BeginTransactionAsync(cancellationToken);
    }

    public async Task CommitTransactionAsync(CancellationToken cancellationToken = default)
    {
        await appDbContext.SaveChangesAsync(cancellationToken);
        await reviewsDbContext.SaveChangesAsync(cancellationToken);

        if (appDbContext.Database.CurrentTransaction != null)
            await appDbContext.Database.CurrentTransaction.CommitAsync(cancellationToken);

        if (reviewsDbContext.Database.CurrentTransaction != null)
            await reviewsDbContext.Database.CurrentTransaction.CommitAsync(cancellationToken);
    }

    public async Task RollbackTransactionAsync(CancellationToken cancellationToken = default)
    {
        if (appDbContext.Database.CurrentTransaction != null)
            await appDbContext.Database.CurrentTransaction.RollbackAsync(cancellationToken);

        if (reviewsDbContext.Database.CurrentTransaction != null)
            await reviewsDbContext.Database.CurrentTransaction.RollbackAsync(cancellationToken);
    }

    public void Dispose() => GC.SuppressFinalize(this);
}
