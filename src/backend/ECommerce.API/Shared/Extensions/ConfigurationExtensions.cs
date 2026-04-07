using Microsoft.Extensions.Configuration;

namespace ECommerce.API.Shared.Extensions;

/// <summary>
/// Extension methods for IConfiguration to validate required configuration values.
/// </summary>
public static class ConfigurationExtensions
{
    /// <summary>
    /// Validates that a required configuration value exists and is not empty.
    /// </summary>
    /// <param name="configuration">The configuration instance.</param>
    /// <param name="key">The configuration key (supports colon-separated paths).</param>
    /// <param name="environmentVariableName">The environment variable name for error messages.</param>
    /// <returns>The configuration value if valid.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the value is missing or empty.</exception>
    public static string GetRequiredValue(
        this IConfiguration configuration,
        string key,
        string? environmentVariableName = null)
    {
        var value = configuration[key];

        if (string.IsNullOrEmpty(value))
        {
            var envVarName = environmentVariableName ?? key.Replace(":", "__");
            throw new InvalidOperationException(
                $"Required configuration value '{key}' is missing. " +
                $"Set it via environment variable '{envVarName}' or in configuration.");
        }

        return value;
    }

    /// <summary>
    /// Validates that a required configuration value meets a minimum length requirement.
    /// </summary>
    /// <param name="configuration">The configuration instance.</param>
    /// <param name="key">The configuration key.</param>
    /// <param name="minLength">The minimum required length.</param>
    /// <param name="environmentVariableName">The environment variable name for error messages.</param>
    /// <returns>The configuration value if valid.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the value is missing or too short.</exception>
    public static string GetRequiredValueWithMinLength(
        this IConfiguration configuration,
        string key,
        int minLength,
        string? environmentVariableName = null)
    {
        var value = configuration.GetRequiredValue(key, environmentVariableName);

        if (value.Length < minLength)
        {
            var envVarName = environmentVariableName ?? key.Replace(":", "__");
            throw new InvalidOperationException(
                $"Configuration value '{key}' must be at least {minLength} characters long. " +
                $"Current length: {value.Length}. " +
                $"Update the environment variable '{envVarName}'.");
        }

        return value;
    }

    /// <summary>
    /// Validates that a connection string exists and contains required components.
    /// </summary>
    /// <param name="configuration">The configuration instance.</param>
    /// <param name="name">The connection string name.</param>
    /// <returns>The connection string if valid.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the connection string is invalid.</exception>
    public static string GetRequiredConnectionString(
        this IConfiguration configuration,
        string name = "DefaultConnection")
    {
        var connectionString = configuration.GetConnectionString(name);

        if (string.IsNullOrEmpty(connectionString))
        {
            throw new InvalidOperationException(
                $"Connection string '{name}' is missing. " +
                $"Set it via environment variable 'ConnectionStrings__{name}'.");
        }

        // Validate that the connection string contains a password for security
        if (!connectionString.Contains("Password=", StringComparison.OrdinalIgnoreCase) &&
            !connectionString.Contains("Pwd=", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                $"Connection string '{name}' must include a Password. " +
                $"Ensure the connection string includes authentication credentials.");
        }

        return connectionString;
    }

    /// <summary>
    /// Validates JWT configuration for security requirements.
    /// </summary>
    /// <param name="configuration">The configuration instance.</param>
    /// <exception cref="InvalidOperationException">Thrown when JWT configuration is invalid.</exception>
    public static void ValidateJwtConfiguration(this IConfiguration configuration)
    {
        var secretKey = configuration.GetRequiredValueWithMinLength(
            "Jwt:SecretKey",
            minLength: 32,
            environmentVariableName: "Jwt__SecretKey");

        // Validate issuer and audience are set
        configuration.GetRequiredValue("Jwt:Issuer", "Jwt__Issuer");
        configuration.GetRequiredValue("Jwt:Audience", "Jwt__Audience");
    }

    /// <summary>
    /// Validates all required configuration values for the application.
    /// </summary>
    /// <param name="configuration">The configuration instance.</param>
    /// <exception cref="InvalidOperationException">Thrown when any required configuration is missing.</exception>
    public static void ValidateRequiredConfiguration(this IConfiguration configuration)
    {
        var errors = new List<string>();

        // Validate JWT configuration
        try
        {
            configuration.ValidateJwtConfiguration();
        }
        catch (InvalidOperationException ex)
        {
            errors.Add(ex.Message);
        }

        // Validate connection string
        try
        {
            configuration.GetRequiredConnectionString();
        }
        catch (InvalidOperationException ex)
        {
            errors.Add(ex.Message);
        }

        if (errors.Count > 0)
        {
            throw new InvalidOperationException(
                "Configuration validation failed:\n" + string.Join("\n", errors));
        }
    }
}

