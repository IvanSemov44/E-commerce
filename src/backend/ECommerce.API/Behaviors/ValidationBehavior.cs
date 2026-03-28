using System.Reflection;
using FluentValidation;
using MediatR;
using ECommerce.SharedKernel.Results;

namespace ECommerce.API.Behaviors;

public class ValidationBehavior<TRequest, TResponse>(IEnumerable<IValidator<TRequest>> validators)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    // Cached per closed generic type — avoids reflection lookup on every request.
    private static readonly MethodInfo? _resultFailMethod = typeof(TResponse).IsGenericType
        && typeof(TResponse).GetGenericTypeDefinition() == typeof(Result<>)
            ? typeof(TResponse).GetMethod("Fail",
                BindingFlags.Static | BindingFlags.Public,
                null,
                [typeof(DomainError)],
                null)
            : null;

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        if (!validators.Any())
            return await next(cancellationToken);

        var context = new ValidationContext<TRequest>(request);
        var failures = validators
            .Select(v => v.Validate(context))
            .SelectMany(r => r.Errors)
            .Where(f => f is not null)
            .ToList();

        if (failures.Count == 0)
            return await next(cancellationToken);

        var error = new DomainError("VALIDATION_FAILED", failures.First().ErrorMessage);

        // Return Result.Fail without throwing — keeps validation errors in the Result<T>
        // pipeline so controllers see a clean failure rather than an unhandled exception.
        if (typeof(TResponse) == typeof(Result))
            return (TResponse)(object)Result.Fail(error);

        if (_resultFailMethod is not null)
            return (TResponse)_resultFailMethod.Invoke(null, [error])!;

        // TResponse is not a Result type — should not happen in this codebase.
        // Throw so the wiring bug is immediately visible.
        throw new ValidationException(failures);
    }
}
