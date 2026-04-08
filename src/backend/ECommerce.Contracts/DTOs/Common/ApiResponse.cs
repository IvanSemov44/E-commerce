namespace ECommerce.Contracts.DTOs.Common;

/// <summary>
/// Standard API response wrapper for all endpoints.
/// Success responses contain Data, failures contain Error.
/// </summary>
public record ApiResponse<T>
{
    /// <summary>True if request succeeded, false otherwise</summary>
    public required bool Success { get; init; }

    /// <summary>Response data (populated on success)</summary>
    public T? Data { get; init; }

    /// <summary>Error details (populated on failure)</summary>
    public ErrorResponse? ErrorDetails { get; init; }

    /// <summary>Trace ID for correlating with logs and support tickets</summary>
    public string? TraceId { get; init; }

    /// <summary>Creates a successful response</summary>
    public static ApiResponse<T> Ok(T data, string? _ = null) =>
        new() { Success = true, Data = data };

    /// <summary>Creates a failed response with semantic error code and message</summary>
    public static ApiResponse<T> Failure(string message, string? code = null, Dictionary<string, string[]>? errors = null) =>
        new() { Success = false, ErrorDetails = new ErrorResponse { Message = message, Code = code, Errors = errors } };

    /// <summary>Creates a failed response with ErrorResponse object</summary>
    public static ApiResponse<T> Failure(ErrorResponse errorResponse) =>
        new() { Success = false, ErrorDetails = errorResponse };

    /// <summary>Creates a failed response (backward compatibility)</summary>
    public static ApiResponse<T> Error(string message, List<string>? errors = null) =>
        Failure(message, errors: errors?.Any() == true ? new Dictionary<string, string[]> { ["general"] = errors.ToArray() } : null);
}

