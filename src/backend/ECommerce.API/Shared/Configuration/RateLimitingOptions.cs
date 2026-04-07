namespace ECommerce.API.Shared.Configuration;

/// <summary>
/// Configuration options for rate limiting policies.
/// Allows externalization of rate limiting thresholds to appsettings.json.
/// </summary>
public class RateLimitingOptions
{
    /// <summary>
    /// Configuration section name in appsettings.json.
    /// </summary>
    public const string SectionName = "RateLimiting";

    /// <summary>
    /// Global rate limit: maximum requests per IP per window.
    /// Default: 100 requests per minute.
    /// </summary>
    public int GlobalLimit { get; set; } = 100;

    /// <summary>
    /// Global rate limiting window duration in seconds.
    /// Default: 60 seconds (1 minute).
    /// </summary>
    public int GlobalWindowSeconds { get; set; } = 60;

    /// <summary>
    /// Authentication endpoints rate limit (login, register, password reset).
    /// Default: 5 requests per minute per IP.
    /// </summary>
    public int AuthLimit { get; set; } = 5;

    /// <summary>
    /// Authentication endpoint rate limiting window in seconds.
    /// Default: 60 seconds (1 minute).
    /// </summary>
    public int AuthWindowSeconds { get; set; } = 60;

    /// <summary>
    /// Password reset endpoint rate limit.
    /// Default: 3 requests per IP within the reset window.
    /// </summary>
    public int PasswordResetLimit { get; set; } = 3;

    /// <summary>
    /// Password reset endpoint rate limiting window in minutes.
    /// Default: 15 minutes.
    /// </summary>
    public int PasswordResetWindowMinutes { get; set; } = 15;

    /// <summary>
    /// HTTP status code to return when rate limit is exceeded.
    /// Default: 429 Too Many Requests.
    /// </summary>
    public int RejectionStatusCode { get; set; } = StatusCodes.Status429TooManyRequests;
}

