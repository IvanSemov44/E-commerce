namespace ECommerce.Application.DTOs.Common;

/// <summary>
/// Response DTO for service health check endpoints.
/// </summary>
public class HealthCheckResponseDto
{
    /// <summary>
    /// Health status of the service (e.g., "healthy", "unhealthy").
    /// </summary>
    public string Status { get; set; } = null!;

    /// <summary>
    /// Name of the service being checked.
    /// </summary>
    public string Service { get; set; } = null!;

    /// <summary>
    /// Timestamp of when the health check was performed.
    /// </summary>
    public DateTime Timestamp { get; set; }
}
