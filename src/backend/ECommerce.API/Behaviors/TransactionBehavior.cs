using ECommerce.SharedKernel.Interfaces;
using ECommerce.SharedKernel.Results;
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

            if (IsFailedResultResponse(response))
            {
                await unitOfWork.RollbackTransactionAsync(cancellationToken);
                return response;
            }

            await unitOfWork.CommitTransactionAsync(cancellationToken);
            return response;
        }
        catch
        {
            await unitOfWork.RollbackTransactionAsync(cancellationToken);
            throw;
        }
    }

    private static bool IsFailedResultResponse(TResponse response)
    {
        if (response is Result nonGenericResult)
            return !nonGenericResult.IsSuccess;

        var responseType = response?.GetType();
        if (responseType is null)
            return false;

        var isSuccessProperty = responseType.GetProperty(nameof(Result.IsSuccess));
        if (isSuccessProperty?.PropertyType != typeof(bool))
            return false;

        var declaringType = isSuccessProperty.DeclaringType;
        if (declaringType is null)
            return false;

        bool isGenericSharedKernelResult = declaringType.IsGenericType
            && declaringType.GetGenericTypeDefinition() == typeof(Result<>);

        if (!isGenericSharedKernelResult)
            return false;

        return !(bool)isSuccessProperty.GetValue(response)!;
    }
}
