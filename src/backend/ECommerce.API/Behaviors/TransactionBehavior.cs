using ECommerce.SharedKernel.Interfaces;
using MediatR;

namespace ECommerce.API.Behaviors;

public class TransactionBehavior<TRequest, TResponse>(IUnitOfWork unitOfWork)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        // Only wrap in transaction if the request is a transactional command.
        // Queries pass through untouched.
        if (request is not ITransactionalCommand)
            return await next(cancellationToken);

        // Don't nest transactions — if one is already open, let it own the scope.
        if (unitOfWork.HasActiveTransaction)
            return await next(cancellationToken);

        await unitOfWork.BeginTransactionAsync(cancellationToken);
        try
        {
            var response = await next(cancellationToken);
            await unitOfWork.CommitTransactionAsync(cancellationToken);
            return response;
        }
        catch
        {
            await unitOfWork.RollbackTransactionAsync(cancellationToken);
            throw;
        }
    }
}
