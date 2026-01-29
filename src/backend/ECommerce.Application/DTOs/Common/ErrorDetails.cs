using System.Text.Json;

namespace ECommerce.Application.DTOs.Common;

/// <summary>
/// Represents standardized error details returned to the client.
/// </summary>
public class ErrorDetails
{
    /// <summary>
    /// HTTP status code of the error.
    /// </summary>
    public int StatusCode { get; set; }

    /// <summary>
    /// Error message describing what went wrong.
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// Converts the error details to JSON string.
    /// </summary>
    public override string ToString() => JsonSerializer.Serialize(this);
}
