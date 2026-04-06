using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using ECommerce.API.Configuration;

namespace ECommerce.API.HealthChecks;

/// <summary>
/// Health check that monitors memory usage and reports degraded status
/// when memory consumption exceeds the configured threshold.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="MemoryHealthCheck"/> class.
/// </remarks>
/// <param name="options">The monitoring configuration options.</param>
public class MemoryHealthCheck(IOptions<MonitoringOptions> options) : IHealthCheck
{
    private readonly MonitoringOptions _options = options.Value;

    /// <summary>
    /// Performs the memory health check.
    /// </summary>
    /// <param name="context">The health check context.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task containing the health check result.</returns>
    public Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        // Get total allocated memory in megabytes
        var allocatedMB = GC.GetTotalMemory(forceFullCollection: false) / 1024 / 1024;
        var thresholdMB = _options.MemoryThresholdMB;

        var data = new Dictionary<string, object>
        {
            { "AllocatedMB", allocatedMB },
            { "ThresholdMB", thresholdMB },
            { "Gen0Collections", GC.CollectionCount(0) },
            { "Gen1Collections", GC.CollectionCount(1) },
            { "Gen2Collections", GC.CollectionCount(2) }
        };

        // Determine health status based on memory usage
        if (allocatedMB > thresholdMB)
        {
            return Task.FromResult(HealthCheckResult.Degraded(
                description: $"Memory usage high: {allocatedMB}MB exceeds threshold of {thresholdMB}MB",
                data: data));
        }

        // Warn at 80% of threshold
        if (allocatedMB > thresholdMB * 0.8)
        {
            return Task.FromResult(HealthCheckResult.Healthy(
                description: $"Memory usage elevated: {allocatedMB}MB (approaching threshold of {thresholdMB}MB)",
                data: data));
        }

        return Task.FromResult(HealthCheckResult.Healthy(
            description: $"Memory usage normal: {allocatedMB}MB",
            data: data));
    }
}
