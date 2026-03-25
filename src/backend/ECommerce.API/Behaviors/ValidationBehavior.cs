using FluentValidation;
using MediatR;

namespace ECommerce.API.Behaviors;

public class ValidationBehavior<TRequest, TResponse>(IEnumerable<IValidator<TRequest>> validators)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        if (!validators.Any()) // IEnumerable — no Count property, Any() is correct here
            return await next(cancellationToken);

        var context = new ValidationContext<TRequest>(request);
        var failures = validators
            .Select(v => v.Validate(context))
            .SelectMany(r => r.Errors)
            .Where(f => f is not null)
            .ToList();

        if (failures.Count > 0)
            throw new ValidationException(failures);

        // Note: ValidationException is caught by GlobalExceptionHandler → returns HTTP 400.
        // This is the input validation path (malformed data).
        // Business rule failures return Result.Fail(...) from the handler → HTTP 422.
        // Two different failure paths, both produce user-friendly error responses.

        return await next(cancellationToken);
    }
}
