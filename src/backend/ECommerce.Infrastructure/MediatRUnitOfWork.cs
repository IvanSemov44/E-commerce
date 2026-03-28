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

    public async Task CommitTransactionAsync(CancellationToken cancellationToken = default)
    {
        await context.SaveChangesAsync(cancellationToken);
        var tx = context.Database.CurrentTransaction;
        if (tx is not null) await tx.CommitAsync(cancellationToken);
    }

    public Task RollbackTransactionAsync(CancellationToken cancellationToken = default)
    {
        var tx = context.Database.CurrentTransaction;
        return tx is not null ? tx.RollbackAsync(cancellationToken) : Task.CompletedTask;
    }

    public void Dispose() => GC.SuppressFinalize(this);
}
