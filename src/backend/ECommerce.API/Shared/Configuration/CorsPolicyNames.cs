namespace ECommerce.API.Common.Configuration;

/// <summary>
/// CORS policy names used throughout the application.
/// Defines standardized policy identifiers for different environments.
/// </summary>
public static class CorsPolicyNames
{
    /// <summary>
    /// Development policy - allows specific development origins.
    /// Used when the application runs in development environment.
    /// Permits localhost and alternative dev ports with all methods.
    /// </summary>
    public const string Development = "Development";

    /// <summary>
    /// Production policy - restricts to configured allowed origins only.
    /// Used when the application runs in production environment.
    /// Only allows origins specified in configuration (appsettings.json or environment variables).
    /// </summary>
    public const string Production = "Production";
}


