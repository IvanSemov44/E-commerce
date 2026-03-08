namespace ECommerce.Core.Results;

/// <summary>
/// Represents the result of an operation, either success or failure.
/// Use for predictable business outcomes (validation, state, ownership, inventory).
/// Infrastructure failures should still throw typed exceptions.
/// </summary>
public abstract record Result<T>
{
    /// <summary>
    /// Success result with data.
    /// </summary>
    public sealed record Success(T Data) : Result<T>;

    /// <summary>
    /// Failure result with semantic error code and message.
    /// </summary>
    public sealed record Failure(string Code, string Message) : Result<T>;

    /// <summary>
    /// Validation failure with field-level errors.
    /// </summary>
    public sealed record ValidationFailure(Dictionary<string, string[]> Errors) : Result<T>;

    /// <summary>
    /// Pattern matching helper for explicit failure handling.
    /// </summary>
    public TResult Match<TResult>(
        Func<T, TResult> onSuccess,
        Func<Result<T>, TResult> onFailure) =>
        this switch
        {
            Success s => onSuccess(s.Data),
            _ => onFailure(this)
        };

    /// <summary>
    /// Pattern matching helper (void version for side effects).
    /// </summary>
    public void Match(
        Action<T> onSuccess,
        Action<Result<T>> onFailure)
    {
        if (this is Success s)
            onSuccess(s.Data);
        else
            onFailure(this);
    }

    /// <summary>
    /// Create a success result.
    /// </summary>
    public static Result<T> Ok(T data) => new Success(data);

    /// <summary>
    /// Create a failure result.
    /// </summary>
    public static Result<T> Fail(string code, string message) =>
        new Failure(code, message);

    /// <summary>
    /// Create a validation failure result.
    /// </summary>
    public static Result<T> ValidationFail(Dictionary<string, string[]> errors) =>
        new ValidationFailure(errors);

    /// <summary>
    /// Check if result is success.
    /// </summary>
    public bool IsSuccess => this is Success;

    /// <summary>
    /// Get data if success, otherwise throw.
    /// </summary>
    public T GetDataOrThrow() =>
        this is Success s
            ? s.Data
            : throw new InvalidOperationException($"Result is not successful: {this}");

    /// <summary>
    /// Get failure details if failed.
    /// </summary>
    public (string Code, string Message)? GetFailureOrNull() =>
        this is Failure f
            ? (f.Code, f.Message)
            : null;

    /// <summary>
    /// Get validation errors if validation failure.
    /// </summary>
    public Dictionary<string, string[]>? GetValidationErrorsOrNull() =>
        this is ValidationFailure vf
            ? vf.Errors
            : null;
}
