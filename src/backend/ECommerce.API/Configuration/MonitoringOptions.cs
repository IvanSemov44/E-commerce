namespace ECommerce.API.Configuration;

/// <summary>
/// Configuration options for health checks and monitoring.
/// </summary>
public class MonitoringOptions
{
    /// <summary>
    /// The configuration section name in appsettings.json.
    /// </summary>
    public const string SectionName = "Monitoring";

    /// <summary>
    /// Gets or sets whether detailed health checks are enabled.
    /// When disabled, only basic liveness probe is available.
    /// </summary>
    public bool EnableDetailedHealthChecks { get; set; } = true;

    /// <summary>
    /// Gets or sets the timeout in milliseconds for health check operations.
    /// </summary>
    public int HealthCheckTimeoutMs { get; set; } = 5000;

    /// <summary>
    /// Gets or sets the memory threshold in megabytes.
    /// If memory usage exceeds this value, the health check returns degraded status.
    /// </summary>
    public int MemoryThresholdMB { get; set; } = 1024;

    /// <summary>
    /// Gets or sets whether the health check UI is enabled (development only).
    /// </summary>
    public bool EnableHealthCheckUI { get; set; }
}
