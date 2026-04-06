using Microsoft.AspNetCore.Antiforgery;
using System.Security.Claims;
using Microsoft.Extensions.Hosting;

namespace ECommerce.API.Middleware;

/// <summary>
/// Middleware that provides CSRF protection for authenticated requests.
/// Generates CSRF tokens for GET requests and validates them for state-changing methods.
/// </summary>
public class CsrfMiddleware(RequestDelegate next, ILogger<CsrfMiddleware> logger, IHostEnvironment environment)
{
    private readonly RequestDelegate _next = next;
    private readonly ILogger<CsrfMiddleware> _logger = logger;
    private readonly IHostEnvironment _environment = environment;
    private static readonly string[] _safeMethods = { "GET", "HEAD", "OPTIONS", "TRACE" };

    public async Task InvokeAsync(HttpContext context, IAntiforgery antiforgery)
    {
        var path = context.Request.Path.Value ?? "";

        // Skip CSRF validation entirely in test environment
        // Tests use authenticated clients with JWT tokens, not browser cookies
        if (_environment.IsEnvironment("Test"))
        {
            await _next(context);
            return;
        }

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
            if (_safeMethods.Contains(context.Request.Method))
            {
                // Generate and set CSRF token for safe methods
                // GetAndStoreTokens generates both cookie and request tokens
                // The cookie token is automatically set as an httpOnly cookie by the antiforgery system
                // We need to provide the request token to the client in a readable cookie
                var tokens = antiforgery.GetAndStoreTokens(context);
                if (tokens.RequestToken != null)
                {
                    // Set the request token in a readable cookie (non-httpOnly) for client-side access
                    // The client reads this and sends it back in the X-XSRF-TOKEN header
                    context.Response.Cookies.Append("XSRF-TOKEN", tokens.RequestToken, new CookieOptions
                    {
                        HttpOnly = false, // Must be readable by JavaScript
                        Secure = !context.Request.IsDevelopment(),
                        SameSite = context.Request.IsDevelopment() ? SameSiteMode.Lax : SameSiteMode.None,
                        Path = "/"
                    });
                }
            }
            else
            {
                // Validate CSRF token for state-changing methods (POST, PUT, DELETE, PATCH)
                // The antiforgery system validates:
                // 1. The cookie token from the httpOnly cookie (set by GetAndStoreTokens)
                // 2. The request token from the X-XSRF-TOKEN header (sent by client)
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
        return host == "localhost" || host == "127.0.0.1" || host.StartsWith("192.168.", StringComparison.OrdinalIgnoreCase);
    }
}
