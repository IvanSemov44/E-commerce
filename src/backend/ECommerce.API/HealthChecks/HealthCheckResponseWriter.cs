using System.Text.Json;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace ECommerce.API.HealthChecks;

/// <summary>
/// Custom response writer for health checks that outputs detailed JSON
/// suitable for monitoring systems like Prometheus, Datadog, etc.
/// </summary>
public static class HealthCheckResponseWriter
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true
    };

    /// <summary>
    /// Writes a detailed health check response in JSON format.
    /// </summary>
    /// <param name="context">The HTTP context.</param>
    /// <param name="healthReport">The health check report.</param>
    public static async Task WriteHealthCheckResponse(
        HttpContext context,
        HealthReport healthReport)
    {
        context.Response.ContentType = "application/json";

        var response = new HealthCheckResponse
        {
            Status = healthReport.Status.ToString(),
            TotalDurationMs = healthReport.TotalDuration.TotalMilliseconds,
            Timestamp = DateTime.UtcNow,
            Checks = healthReport.Entries.Select(entry => new HealthCheckEntry
            {
                Name = entry.Key,
                Status = entry.Value.Status.ToString(),
                Description = entry.Value.Description,
                DurationMs = entry.Value.Duration.TotalMilliseconds,
                Tags = entry.Value.Tags.ToList(),
                Data = entry.Value.Data.ToDictionary(
                    kvp => kvp.Key,
                    kvp => kvp.Value)
            }).ToList()
        };

        // Set appropriate HTTP status code based on health
        context.Response.StatusCode = healthReport.Status switch
        {
            HealthStatus.Healthy => StatusCodes.Status200OK,
            HealthStatus.Degraded => StatusCodes.Status200OK, // Still operational
            HealthStatus.Unhealthy => StatusCodes.Status503ServiceUnavailable,
            _ => StatusCodes.Status503ServiceUnavailable
        };

        await context.Response.WriteAsync(
            JsonSerializer.Serialize(response, JsonOptions));
    }

    /// <summary>
    /// Writes a simple liveness response for basic health checks.
    /// </summary>
    /// <param name="context">The HTTP context.</param>
    public static async Task WriteLivenessResponse(
        HttpContext context,
        HealthReport _)
    {
        context.Response.ContentType = "application/json";

        var response = new
        {
            status = "healthy",
            timestamp = DateTime.UtcNow
        };

        context.Response.StatusCode = StatusCodes.Status200OK;

        await context.Response.WriteAsync(
            JsonSerializer.Serialize(response, JsonOptions));
    }
}

/// <summary>
/// Represents the overall health check response.
/// </summary>
public class HealthCheckResponse
{
    /// <summary>
    /// Gets or sets the overall health status.
    /// </summary>
    public string Status { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the total duration in milliseconds.
    /// </summary>
    public double TotalDurationMs { get; set; }

    /// <summary>
    /// Gets or sets the timestamp of the health check.
    /// </summary>
    public DateTime Timestamp { get; set; }

    /// <summary>
    /// Gets or sets the individual health check entries.
    /// </summary>
    public List<HealthCheckEntry> Checks { get; set; } = new();
}

/// <summary>
/// Represents an individual health check entry.
/// </summary>
public class HealthCheckEntry
{
    /// <summary>
    /// Gets or sets the name of the health check.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the status of this health check.
    /// </summary>
    public string Status { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the description of the health check result.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Gets or sets the duration in milliseconds.
    /// </summary>
    public double DurationMs { get; set; }

    /// <summary>
    /// Gets or sets the tags associated with this health check.
    /// </summary>
    public List<string> Tags { get; set; } = new();

    /// <summary>
    /// Gets or sets additional data from the health check.
    /// </summary>
    public Dictionary<string, object> Data { get; set; } = new();
}
