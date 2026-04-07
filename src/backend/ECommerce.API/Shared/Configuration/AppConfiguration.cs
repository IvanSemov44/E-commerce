namespace ECommerce.API.Shared.Configuration;

/// <summary>
/// Centralized application configuration.
/// Mirrors frontend config.ts pattern for consistency across stack.
/// Settings are injected from appsettings.json and environment variables.
/// </summary>
public class AppConfiguration
{
    /// <summary>
    /// JWT (Json Web Token) authentication settings.
    /// </summary>
    public JwtSettings Jwt { get; set; } = new();

    /// <summary>
    /// CORS (Cross-Origin Resource Sharing) settings.
    /// </summary>
    public CorsSettings Cors { get; set; } = new();

    /// <summary>
    /// Database connection settings.
    /// </summary>
    public DatabaseSettings Database { get; set; } = new();

    /// <summary>
    /// Email provider settings (SMTP or SendGrid).
    /// </summary>
    public EmailSettings Email { get; set; } = new();

    /// <summary>
    /// Application URL for links in emails, etc.
    /// </summary>
    public string? AppUrl { get; set; }

    /// <summary>
    /// Email provider type: "Smtp" or "SendGrid" (default).
    /// </summary>
    public string? EmailProvider { get; set; } = "SendGrid";
}

/// <summary>
/// JWT authentication configuration.
/// </summary>
public class JwtSettings
{
    /// <summary>
    /// Secret key for signing tokens (minimum 32 characters).
    /// ⚠️ MUST be at least 32 characters long for security.
    /// </summary>
    public string? SecretKey { get; set; }

    /// <summary>
    /// Token issuer (who created the token).
    /// Used in development; not validated in development mode.
    /// </summary>
    public string? Issuer { get; set; }

    /// <summary>
    /// Token audience (who can use the token).
    /// Used in development; not validated in development mode.
    /// </summary>
    public string? Audience { get; set; }

    /// <summary>
    /// Token expiration time in minutes.
    /// </summary>
    public int ExpireMinutes { get; set; } = 60;
}

/// <summary>
/// CORS (Cross-Origin Resource Sharing) settings.
/// </summary>
public class CorsSettings
{
    /// <summary>
    /// Allowed origin URLs for CORS requests.
    /// In development: allows all origins.
    /// In production: only listed origins are allowed.
    /// </summary>
    public string[] Origins { get; set; } = new[] { "http://localhost:5173" };
}

/// <summary>
/// Database connection settings.
/// </summary>
public class DatabaseSettings
{
    /// <summary>
    /// Database connection string.
    /// Format for PostgreSQL: Host=localhost;Database=ECommerceDb;Username=user;Password=pass
    /// </summary>
    public string? ConnectionString { get; set; }
}

/// <summary>
/// Email service configuration.
/// </summary>
public class EmailSettings
{
    /// <summary>
    /// SMTP host (used when EmailProvider=Smtp).
    /// </summary>
    public string? SmtpHost { get; set; }

    /// <summary>
    /// SMTP port (default 587 for TLS).
    /// </summary>
    public int SmtpPort { get; set; } = 587;

    /// <summary>
    /// SMTP username/email.
    /// </summary>
    public string? SmtpUsername { get; set; }

    /// <summary>
    /// SMTP password.
    /// </summary>
    public string? SmtpPassword { get; set; }

    /// <summary>
    /// SendGrid API key (used when EmailProvider=SendGrid).
    /// </summary>
    public string? SendGridApiKey { get; set; }

    /// <summary>
    /// From email address for outgoing emails.
    /// </summary>
    public string? FromEmail { get; set; } = "noreply@ecommerce.com";

    /// <summary>
    /// From display name for outgoing emails.
    /// </summary>
    public string? FromName { get; set; } = "E-Commerce Team";
}

