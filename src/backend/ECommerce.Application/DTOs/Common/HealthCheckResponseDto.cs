namespace ECommerce.Application.DTOs.Common;

/// <summary>
/// Response DTO for service health check endpoints.
/// </summary>
public record HealthCheckResponseDto
{
    /// <summary>
    /// Health status of the service (e.g., "healthy", "unhealthy").
    /// </summary>
    public string Status { get; init; } = null!;

    /// <summary>
    /// Name of the service being checked.
    /// </summary>
    public string Service { get; init; } = null!;

    /// <summary>
    /// Timestamp of when the health check was performed.
    /// </summary>
    public DateTime Timestamp { get; init; }
}
