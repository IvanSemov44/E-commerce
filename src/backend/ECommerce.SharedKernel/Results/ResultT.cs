namespace ECommerce.SharedKernel.Results;

/// <summary>
/// Generic result for domain operations that return a value.
/// Domain validation is expected flow — not exceptional. Use this instead of throwing.
/// Infrastructure failures (DB down, null ref) should still throw exceptions.
/// </summary>
public abstract record Result<T>
{
    public sealed record Success(T Data) : Result<T>;
    public sealed record Failure(DomainError Error) : Result<T>;

    public static Result<T> Ok(T data) => new Success(data);
    public static Result<T> Fail(DomainError error) => new Failure(error);

    public bool IsSuccess => this is Success;

    /// <summary>Returns the data. Safe to call only after confirming IsSuccess.</summary>
    public T GetDataOrThrow() =>
        this is Success s ? s.Data : throw new InvalidOperationException("Result is not successful.");

    /// <summary>Returns the error. Safe to call only after confirming !IsSuccess.</summary>
    public DomainError GetErrorOrThrow() =>
        this is Failure f ? f.Error : throw new InvalidOperationException("Result is successful, no error to get.");
}
