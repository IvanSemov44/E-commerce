// ============================================================================
// E-Commerce API - Serilog Logging Configuration Extensions
// ============================================================================

using Microsoft.ApplicationInsights.Extensibility;
using Serilog;
using Serilog.Events;

namespace ECommerce.API.Common.Extensions;

/// <summary>
/// Extension methods for configuring Serilog logging with cloud-ready sinks.
/// </summary>
public static class LoggingExtensions
{
    /// <summary>
    /// Configures Serilog with structured logging, correlation IDs, and environment-specific sinks.
    /// Supports Seq for development/staging and Application Insights for production.
    /// </summary>
    /// <param name="builder">The web application builder.</param>
    /// <returns>The web application builder for chaining.</returns>
    public static WebApplicationBuilder ConfigureSerilog(this WebApplicationBuilder builder)
    {
        // Configure Serilog with configuration-based settings and additional programmatic configuration
        builder.Host.UseSerilog((context, services, loggerConfiguration) =>
        {
            // Read from configuration (appsettings.json)
            loggerConfiguration
                .ReadFrom.Configuration(context.Configuration)
                .ReadFrom.Services(services);

            // Core enrichers
            loggerConfiguration
                .Enrich.FromLogContext()
                .Enrich.WithMachineName()
                .Enrich.WithCorrelationId()
                .Enrich.WithProperty("Environment", context.HostingEnvironment.EnvironmentName)
                .Enrich.WithProperty("Application", "ECommerce.API");

            // Console sink with structured output
#pragma warning disable CA1305 // Serilog output templates do not use IFormatProvider
            loggerConfiguration.WriteTo.Console(
                outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj} {Properties:j}{NewLine}{Exception}");

            // Main application log file
            loggerConfiguration.WriteTo.File(
                path: "logs/app-.txt",
                rollingInterval: RollingInterval.Day,
                outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj} {Properties:j}{NewLine}{Exception}");

            // Security audit log file (warnings and above)
            loggerConfiguration.WriteTo.File(
                path: "logs/security-.txt",
                rollingInterval: RollingInterval.Day,
                restrictedToMinimumLevel: LogEventLevel.Warning,
                outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj} {Properties:j}{NewLine}{Exception}");
#pragma warning restore CA1305

            // Seq sink for development/staging (if configured)
            var seqServerUrl = context.Configuration.GetValue<string>("Seq:ServerUrl");
            if (!string.IsNullOrEmpty(seqServerUrl))
            {
                loggerConfiguration.WriteTo.Seq(
                    serverUrl: seqServerUrl,
                    restrictedToMinimumLevel: LogEventLevel.Information);
            }

            // Application Insights sink for production (if configured)
            var appInsightsKey = context.Configuration.GetValue<string>("ApplicationInsights:InstrumentationKey");
            if (!string.IsNullOrEmpty(appInsightsKey))
            {
                var telemetryConfig = new TelemetryConfiguration
                {
                    InstrumentationKey = appInsightsKey
                };

                loggerConfiguration.WriteTo.ApplicationInsights(
                    telemetryConfiguration: telemetryConfig,
                    telemetryConverter: TelemetryConverter.Traces,
                    restrictedToMinimumLevel: LogEventLevel.Information);
            }
        });

        return builder;
    }
}


