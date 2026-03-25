using ECommerce.Infrastructure.Data;
using ECommerce.SharedKernel.Interfaces;

namespace ECommerce.Infrastructure;

public sealed class MediatRUnitOfWork(AppDbContext context) : IUnitOfWork
{
    public bool HasActiveTransaction => context.Database.CurrentTransaction != null;

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        => context.SaveChangesAsync(cancellationToken);

    public Task BeginTransactionAsync(CancellationToken cancellationToken = default)
        => context.Database.BeginTransactionAsync(cancellationToken);

    public Task CommitTransactionAsync(CancellationToken cancellationToken = default)
        => context.Database.CurrentTransaction!.CommitAsync(cancellationToken);

    public Task RollbackTransactionAsync(CancellationToken cancellationToken = default)
        => context.Database.CurrentTransaction!.RollbackAsync(cancellationToken);

    public void Dispose() => GC.SuppressFinalize(this);
}
