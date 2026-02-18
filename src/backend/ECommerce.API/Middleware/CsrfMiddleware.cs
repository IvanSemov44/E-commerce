using Microsoft.AspNetCore.Antiforgery;
using System.Security.Claims;

namespace ECommerce.API.Middleware;

/// <summary>
/// Middleware that provides CSRF protection for authenticated requests.
/// Generates CSRF tokens for GET requests and validates them for state-changing methods.
/// </summary>
public class CsrfMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<CsrfMiddleware> _logger;
    private static readonly string[] SafeMethods = { "GET", "HEAD", "OPTIONS", "TRACE" };

    public CsrfMiddleware(RequestDelegate next, ILogger<CsrfMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context, IAntiforgery antiforgery)
    {
        var path = context.Request.Path.Value ?? "";
        
        // Skip CSRF for auth endpoints that set cookies (login, register, refresh)
        if (IsAuthEndpoint(path) && context.Request.Method == "POST")
        {
            await _next(context);
            return;
        }

        // Skip CSRF for health checks and other non-protected endpoints
        if (IsExcludedPath(path))
        {
            await _next(context);
            return;
        }

        // For authenticated users: generate CSRF token for safe methods, validate for unsafe methods
        var user = context.User;
        var isAuthenticated = user?.Identity?.IsAuthenticated == true;

        if (isAuthenticated)
        {
            if (SafeMethods.Contains(context.Request.Method))
            {
                // Generate and set CSRF token for safe methods
                var tokens = antiforgery.GetAndStoreTokens(context);
                if (tokens.RequestToken != null)
                {
                    // Set CSRF token in a readable cookie (non-httpOnly) for client-side access
                    context.Response.Cookies.Append("XSRF-TOKEN", tokens.RequestToken, new CookieOptions
                    {
                        HttpOnly = false, // Must be readable by JavaScript
                        Secure = !context.Request.IsDevelopment(),
                        SameSite = context.Request.IsDevelopment() ? SameSiteMode.Lax : SameSiteMode.Strict,
                        Path = "/"
                    });
                }
            }
            else
            {
                // Validate CSRF token for state-changing methods (POST, PUT, DELETE, PATCH)
                try
                {
                    await antiforgery.ValidateRequestAsync(context);
                }
                catch (AntiforgeryValidationException ex)
                {
                    _logger.LogWarning("CSRF validation failed for {Method} {Path}: {Message}", 
                        context.Request.Method, path, ex.Message);
                    
                    context.Response.StatusCode = StatusCodes.Status400BadRequest;
                    context.Response.ContentType = "application/json";
                    await context.Response.WriteAsJsonAsync(new
                    {
                        success = false,
                        message = "Invalid CSRF token. Please refresh the page and try again."
                    });
                    return;
                }
            }
        }

        await _next(context);
    }

    private static bool IsAuthEndpoint(string path)
    {
        return path.StartsWith("/api/auth/login", StringComparison.OrdinalIgnoreCase) ||
               path.StartsWith("/api/auth/register", StringComparison.OrdinalIgnoreCase) ||
               path.StartsWith("/api/auth/refresh-token", StringComparison.OrdinalIgnoreCase) ||
               path.StartsWith("/api/auth/logout", StringComparison.OrdinalIgnoreCase) ||
               path.StartsWith("/api/auth/forgot-password", StringComparison.OrdinalIgnoreCase) ||
               path.StartsWith("/api/auth/reset-password", StringComparison.OrdinalIgnoreCase) ||
               path.StartsWith("/api/auth/verify-email", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsExcludedPath(string path)
    {
        return path.Equals("/health", StringComparison.OrdinalIgnoreCase) ||
               path.StartsWith("/swagger", StringComparison.OrdinalIgnoreCase) ||
               path.StartsWith("/openapi", StringComparison.OrdinalIgnoreCase);
    }
}

/// <summary>
/// Extension methods for CSRF middleware configuration.
/// </summary>
public static class CsrfMiddlewareExtensions
{
    /// <summary>
    /// Adds CSRF protection middleware to the application pipeline.
    /// </summary>
    public static IApplicationBuilder UseCsrfProtection(this IApplicationBuilder app)
    {
        return app.UseMiddleware<CsrfMiddleware>();
    }
}

/// <summary>
/// Extension methods for HttpContext.
/// </summary>
internal static class HttpContextExtensions
{
    public static bool IsDevelopment(this HttpRequest request)
    {
        var host = request.Host.Host;
        return host == "localhost" || host == "127.0.0.1" || host.StartsWith("192.168.");
    }
}
