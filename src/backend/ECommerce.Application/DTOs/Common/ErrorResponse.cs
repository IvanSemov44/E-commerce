namespace ECommerce.Application.DTOs.Common;

/// <summary>
/// Standard error response with semantic codes for client error handling.
/// </summary>
public record ErrorResponse
{
    /// <summary>User-friendly error message (never expose internals)</summary>
    public required string Message { get; init; }

    /// <summary>Semantic error code for client logic: "PRODUCT_NOT_FOUND", "INSUFFICIENT_INVENTORY", etc.</summary>
    public string? Code { get; init; }

    /// <summary>Validation errors by field: { "Email": ["Invalid format"], "Name": ["Required"] }</summary>
    public Dictionary<string, string[]>? Errors { get; init; }

    /// <summary>Trace ID for support tickets (correlate with logs)</summary>
    public string? TraceId { get; init; }
}
