using ECommerce.API.Configuration;

namespace ECommerce.API.Extensions;

/// <summary>
/// Extension methods for registering application configuration.
/// </summary>
public static class ConfigurationExtensions
{
    /// <summary>
    /// Registers AppConfiguration into the dependency injection container.
    /// Loads settings from appsettings.json and environment variables.
    /// 
    /// Usage:
    /// builder.Services.AddAppConfiguration(builder.Configuration);
    /// 
    /// Then in services:
    /// public MyService(IOptions<AppConfiguration> config)
    /// {
    ///     var jwtSecret = config.Value.Jwt.SecretKey;
    /// }
    /// </summary>
    public static IServiceCollection AddAppConfiguration(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Bind configuration section to AppConfiguration class
        services.Configure<AppConfiguration>(configuration);

        // Also register as singleton for direct injection if needed
        var appConfig = new AppConfiguration();
        configuration.Bind(appConfig);
        services.AddSingleton(appConfig);

        return services;
    }
}
