namespace ECommerce.API.Shared.Configuration;

/// <summary>
/// JWT (JSON Web Token) configuration options.
/// Centralizes all JWT-related settings for dependency injection and validation.
/// </summary>
public class JwtOptions
{
    /// <summary>
    /// Configuration section name in appsettings.json.
    /// </summary>
    public const string SectionName = "Jwt";

    /// <summary>
    /// Secret key used to sign JWT tokens.
    /// MUST be at least 32 characters for HMAC256.
    /// Keep this secure and out of version control.
    /// </summary>
    public string SecretKey { get; set; } = null!;

    /// <summary>
    /// JWT issuer - identifies the principal that issued the token.
    /// Example: "ecommerce-api"
    /// </summary>
    public string Issuer { get; set; } = null!;

    /// <summary>
    /// JWT audience - identifies the principal(s) the token is intended for.
    /// Example: "ecommerce-frontend"
    /// </summary>
    public string Audience { get; set; } = null!;

    /// <summary>
    /// Token expiration time in minutes.
    /// Default: 60 minutes.
    /// </summary>
    public int ExpirationMinutes { get; set; } = 60;

    /// <summary>
    /// Refresh token expiration time in days.
    /// Default: 7 days.
    /// </summary>
    public int RefreshTokenExpirationDays { get; set; } = 7;

    /// <summary>
    /// Clock skew tolerance in seconds.
    /// Allows for minor time differences between servers.
    /// Default: 0 seconds (strict validation).
    /// </summary>
    public int ClockSkewSeconds { get; set; }

    /// <summary>
    /// Validates JWT configuration for security requirements.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown when configuration is invalid.</exception>
    public void Validate()
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(SecretKey))
            errors.Add("SecretKey is required");
        else if (SecretKey.Length < 32)
            errors.Add($"SecretKey must be at least 32 characters (current: {SecretKey.Length})");

        if (string.IsNullOrWhiteSpace(Issuer))
            errors.Add("Issuer is required");

        if (string.IsNullOrWhiteSpace(Audience))
            errors.Add("Audience is required");

        if (ExpirationMinutes <= 0)
            errors.Add("ExpirationMinutes must be greater than 0");

        if (RefreshTokenExpirationDays <= 0)
            errors.Add("RefreshTokenExpirationDays must be greater than 0");

        if (errors.Count > 0)
            throw new InvalidOperationException($"JWT configuration validation failed:\n{string.Join("\n", errors)}");
    }
}

