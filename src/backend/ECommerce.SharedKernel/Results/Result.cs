namespace ECommerce.SharedKernel.Results;

/// <summary>
/// Non-generic result for domain operations that return no value (void operations that can fail).
/// </summary>
public abstract record Result
{
    public sealed record Success : Result;
    public sealed record Failure(DomainError Error) : Result;

    public static Result Ok() => new Success();
    public static Result Fail(DomainError error) => new Failure(error);

    public bool IsSuccess => this is Success;

    /// <summary>Returns the error. Safe to call only after confirming !IsSuccess.</summary>
    public DomainError GetErrorOrThrow() =>
        this is Failure f ? f.Error : throw new InvalidOperationException("Result is successful, no error to get.");

    public TOut Match<TOut>(Func<TOut> onSuccess, Func<DomainError, TOut> onFailure) => this switch
    {
        Success   => onSuccess(),
        Failure f => onFailure(f.Error),
        _ => throw new InvalidOperationException($"Unknown Result subtype: {GetType().Name}")
    };
}
