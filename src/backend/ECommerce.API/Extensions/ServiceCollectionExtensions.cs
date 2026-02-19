using ECommerce.API.Configuration;
using ECommerce.API.HealthChecks;
using ECommerce.Application;
using ECommerce.Application.Configuration;
using ECommerce.Application.Interfaces;
using ECommerce.Application.Services;
using ECommerce.Application.Validators.Cart;
using ECommerce.Core.Interfaces.Repositories;
using ECommerce.Infrastructure;
using ECommerce.Infrastructure.Data;
using ECommerce.Infrastructure.Data.Seeders;
using FluentValidation;
using FluentValidation.AspNetCore;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Http.Json;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Serilog;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.RateLimiting;

namespace ECommerce.API.Extensions;

/// <summary>
/// Extension methods for IServiceCollection to configure application services.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Configures JWT authentication for the application.
    /// Supports both Authorization header and httpOnly cookie-based authentication.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">The application configuration.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddJwtAuthentication(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var jwtSettings = configuration.GetSection("Jwt");
        var secretKey = Encoding.UTF8.GetBytes(jwtSettings["SecretKey"] 
            ?? throw new InvalidOperationException("JWT SecretKey is not configured."));

        services.AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
        })
        .AddJwtBearer(options =>
        {
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(secretKey),
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidIssuer = jwtSettings["Issuer"],
                ValidAudience = jwtSettings["Audience"],
                ValidateLifetime = true,
                ClockSkew = TimeSpan.Zero
            };

            // 🔒 SECURITY: Support reading JWT from httpOnly cookie
            options.Events = new Microsoft.AspNetCore.Authentication.JwtBearer.JwtBearerEvents
            {
                OnMessageReceived = context =>
                {
                    // First, try to get token from Authorization header
                    var authHeader = context.Request.Headers.Authorization.FirstOrDefault();
                    if (!string.IsNullOrEmpty(authHeader) && authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
                    {
                        context.Token = authHeader.Substring("Bearer ".Length).Trim();
                    }
                    else
                    {
                        // Fall back to httpOnly cookie
                        var accessToken = context.Request.Cookies["accessToken"];
                        if (!string.IsNullOrEmpty(accessToken))
                        {
                            context.Token = accessToken;
                        }
                    }
                    return Task.CompletedTask;
                }
            };
        });

        services.AddAuthorization();
        return services;
    }

    /// <summary>
    /// Configures CSRF/antiforgery protection for the application.
    /// Uses the X-XSRF-TOKEN header pattern for SPA compatibility.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddCsrfProtection(this IServiceCollection services)
    {
        services.AddAntiforgery(options =>
        {
            // Use standard XSRF header name that SPAs expect
            options.HeaderName = "X-XSRF-TOKEN";
            // Cookie name for the CSRF token (readable by JavaScript)
            options.Cookie.Name = "XSRF-TOKEN";
            options.Cookie.HttpOnly = false; // Must be readable by JavaScript
            options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
            options.Cookie.SameSite = SameSiteMode.Strict;
        });
        return services;
    }

    /// <summary>
    /// Configures JSON serialization options for strict case-sensitive handling.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddStrictJsonSerialization(this IServiceCollection services)
    {
        services.Configure<JsonOptions>(options =>
        {
            options.SerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
            options.SerializerOptions.PropertyNameCaseInsensitive = false; // Strict: reject PascalCase
            options.SerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
        });
        return services;
    }

    /// <summary>
    /// CORS policy names used throughout the application.
    /// </summary>
    public static class CorsPolicyNames
    {
        /// <summary>
        /// Development policy - allows specific development origins.
        /// </summary>
        public const string Development = "Development";

        /// <summary>
        /// Production policy - restricts to configured allowed origins.
        /// </summary>
        public const string Production = "Production";
    }

    /// <summary>
    /// Configures CORS policies based on the hosting environment.
    /// Uses distinct policy names for development and production environments.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="isDevelopment">Whether the application is running in development mode.</param>
    /// <param name="configuration">The application configuration.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddCorsConfiguration(
        this IServiceCollection services,
        bool isDevelopment,
        IConfiguration configuration)
    {
        services.AddCors(options =>
        {
            // Always add both policies so they can be selected at runtime
            // Development policy - permissive for local development
            options.AddPolicy(CorsPolicyNames.Development, policy =>
            {
                // 🔒 SECURITY: Allow credentials for httpOnly cookie support
                policy.WithOrigins(
                        "http://localhost:5173",  // Vite dev server (storefront)
                        "http://localhost:5177",  // Vite dev server (admin)
                        "http://localhost:3000",  // Alternative dev port
                        "http://localhost:3001"   // Alternative dev port
                    )
                    .AllowAnyMethod()
                    .AllowAnyHeader()
                    .AllowCredentials(); // Required for httpOnly cookies
            });

            // Production policy - strict, configured origins only
            options.AddPolicy(CorsPolicyNames.Production, policy =>
            {
                var allowedOrigins = configuration.GetSection("AllowedOrigins").Get<string[]>()
                    ?? Array.Empty<string>();

                if (allowedOrigins.Length == 0)
                {
                    // Log warning but don't throw - allow configuration via environment variables
                    Log.Warning("No AllowedOrigins configured in appsettings.json. Ensure CORS_ALLOWED_ORIGINS is set via environment variable for production.");
                }

                policy.WithOrigins(allowedOrigins)
                    .WithMethods("GET", "POST", "PUT", "DELETE", "OPTIONS", "PATCH")
                    .WithHeaders("Content-Type", "Authorization", "Accept", "X-XSRF-TOKEN", "X-Requested-With")
                    .AllowCredentials(); // Required for httpOnly cookies
            });
        });
        return services;
    }

    /// <summary>
    /// Configures rate limiting policies for API protection.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddRateLimitingConfiguration(this IServiceCollection services)
    {
        services.AddRateLimiter(options =>
        {
            // Global rate limit: 100 requests per minute per IP
            options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
                RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey: context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                    factory: _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = 100,
                        Window = TimeSpan.FromMinutes(1),
                        QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                        QueueLimit = 0
                    }));

            // Strict limiter for auth endpoints (login/register)
            options.AddPolicy("AuthLimit", context =>
                RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey: context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                    factory: _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = 5,
                        Window = TimeSpan.FromMinutes(1),
                        QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                        QueueLimit = 0
                    }));

            // Strict limiter for password reset
            options.AddPolicy("PasswordResetLimit", context =>
                RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey: context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                    factory: _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = 3,
                        Window = TimeSpan.FromMinutes(15),
                        QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                        QueueLimit = 0
                    }));

            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            options.OnRejected = async (context, cancellationToken) =>
            {
                context.HttpContext.Response.ContentType = "application/json";
                var response = new { message = "Too many requests. Please try again later." };
                await context.HttpContext.Response.WriteAsJsonAsync(response, cancellationToken);
            };
        });
        return services;
    }

    /// <summary>
    /// Registers all application services including domain services, repositories, and infrastructure.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">The application configuration.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddApplicationServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // AutoMapper
        services.AddAutoMapper(cfg => cfg.AddProfile<MappingProfile>());

        // FluentValidation
        services.AddFluentValidationAutoValidation();
        services.AddValidatorsFromAssemblyContaining<AddToCartDtoValidator>();

        // Unit of Work
        services.AddScoped<IUnitOfWork, UnitOfWork>();

        // HTTP context accessor
        services.AddHttpContextAccessor();

        // Domain services
        services.AddScoped<ICurrentUserService, CurrentUserService>();
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IProductService, ProductService>();
        services.AddScoped<ICategoryService, CategoryService>();
        services.AddScoped<IPaymentService, PaymentService>();
        services.AddSingleton<IPaymentStore, InMemoryPaymentStore>();
        services.AddScoped<IOrderService, OrderService>();
        services.AddScoped<ICartService, CartService>();
        services.AddScoped<IReviewService, ReviewService>();
        services.AddScoped<IWishlistService, WishlistService>();
        services.AddScoped<IUserService, UserService>();
        services.AddScoped<IDashboardService, DashboardService>();
        services.AddScoped<IPromoCodeService, PromoCodeService>();
        services.AddScoped<IInventoryService, InventoryService>();
        services.AddScoped<IWebhookVerificationService, WebhookVerificationService>();

        // Email service based on configuration
        var emailProvider = configuration["EmailProvider"] ?? "SendGrid";
        if (emailProvider.Equals("Smtp", StringComparison.OrdinalIgnoreCase))
        {
            services.AddScoped<IEmailService, SmtpEmailService>();
            Log.Information("Using SMTP email provider");
        }
        else
        {
            services.AddScoped<IEmailService, SendGridEmailService>();
            Log.Information("Using SendGrid email provider");
        }

        // Database seeders
        services.AddScoped<IUserSeeder, UserSeeder>();
        services.AddScoped<ICategorySeeder, CategorySeeder>();
        services.AddScoped<IProductSeeder, ProductSeeder>();
        services.AddScoped<DatabaseSeeder>();

        // Logging
        services.AddLogging();

        return services;
    }

    /// <summary>
    /// Configures Swagger/OpenAPI documentation.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddSwaggerDocumentation(this IServiceCollection services)
    {
        services.AddOpenApi();
        services.AddSwaggerGen(c =>
        {
            c.SwaggerDoc("v1", new()
            {
                Title = "E-Commerce API",
                Version = "v1",
                Description = "A comprehensive e-commerce platform API with product management, orders, payments, and more."
            });

            // Include XML comments
            var xmlFile = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
            var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
            if (File.Exists(xmlPath))
            {
                c.IncludeXmlComments(xmlPath);
            }
        });
        return services;
    }

    /// <summary>
    /// Configures the PostgreSQL database context.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">The application configuration.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddPostgreSqlDatabase(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Connection string 'DefaultConnection' is not configured.");

        services.AddDbContext<AppDbContext>(options =>
            options.UseNpgsql(connectionString)
                .ConfigureWarnings(warnings => warnings
                    .Ignore(Microsoft.EntityFrameworkCore.Diagnostics.RelationalEventId.PendingModelChangesWarning)));

        return services;
    }

    /// <summary>
    /// Configures business rules options from appsettings.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">The application configuration.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddBusinessRulesConfiguration(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<BusinessRulesOptions>(
            configuration.GetSection(BusinessRulesOptions.SectionName));
        return services;
    }

    /// <summary>
    /// Registers AppConfiguration into the dependency injection container.
    /// Loads settings from appsettings.json and environment variables.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">The application configuration.</param>
    /// <returns>The service collection for chaining.</returns>
    /// <example>
    /// Usage:
    /// <code>
    /// builder.Services.AddAppConfiguration(builder.Configuration);
    /// 
    /// // Then in services:
    /// public MyService(IOptions&lt;AppConfiguration&gt; config)
    /// {
    ///     var jwtSecret = config.Value.Jwt.SecretKey;
    /// }
    /// </code>
    /// </example>
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

    /// <summary>
    /// Configures health checks for the application including database connectivity,
    /// memory usage monitoring, and self-checks.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">The application configuration.</param>
    /// <returns>The service collection for chaining.</returns>
    /// <example>
    /// Usage:
    /// <code>
    /// builder.Services.AddHealthChecksConfiguration(builder.Configuration);
    /// </code>
    /// </example>
    public static IServiceCollection AddHealthChecksConfiguration(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Configure monitoring options
        services.Configure<MonitoringOptions>(
            configuration.GetSection(MonitoringOptions.SectionName));

        var monitoringOptions = configuration
            .GetSection(MonitoringOptions.SectionName)
            .Get<MonitoringOptions>() ?? new MonitoringOptions();

        var connectionString = configuration.GetConnectionString("DefaultConnection");

        var healthChecksBuilder = services.AddHealthChecks();

        // Add PostgreSQL database health check if connection string is available
        if (!string.IsNullOrEmpty(connectionString))
        {
            healthChecksBuilder.AddNpgSql(
                connectionString,
                name: "postgresql",
                failureStatus: HealthStatus.Unhealthy,
                tags: new[] { "db", "sql", "ready" },
                timeout: TimeSpan.FromMilliseconds(monitoringOptions.HealthCheckTimeoutMs));
        }

        // Add memory health check
        healthChecksBuilder.AddCheck<MemoryHealthCheck>(
            name: "memory",
            failureStatus: HealthStatus.Degraded,
            tags: new[] { "system", "ready" },
            timeout: TimeSpan.FromMilliseconds(monitoringOptions.HealthCheckTimeoutMs));

        // Add self-check (API is running)
        healthChecksBuilder.AddCheck(
            name: "self",
            check: () => HealthCheckResult.Healthy("API is running"),
            tags: new[] { "api", "ready" });

        // Add Redis health check if connection string is available
        var redisConnectionString = configuration["Redis:ConnectionString"];
        if (!string.IsNullOrEmpty(redisConnectionString))
        {
            healthChecksBuilder.AddRedis(
                redisConnectionString,
                name: "redis",
                failureStatus: HealthStatus.Degraded,
                tags: new[] { "cache", "ready" },
                timeout: TimeSpan.FromMilliseconds(monitoringOptions.HealthCheckTimeoutMs));
            Log.Information("Health checks configured: postgresql, redis, memory, self");
        }
        else
        {
            Log.Information("Health checks configured: postgresql, memory, self");
        }

        return services;
    }

    /// <summary>
    /// Configures Redis distributed caching for the application.
    /// Falls back to in-memory caching if Redis is not available.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">The application configuration.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddRedisCaching(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var redisConnectionString = configuration["Redis:ConnectionString"];
        var instanceName = configuration["Redis:InstanceName"] ?? "ecommerce:";

        if (!string.IsNullOrEmpty(redisConnectionString))
        {
            services.AddStackExchangeRedisCache(options =>
            {
                options.Configuration = redisConnectionString;
                options.InstanceName = instanceName;
            });
            Log.Information("Redis caching configured: {ConnectionString}", redisConnectionString);
        }
        else
        {
            // Fallback to in-memory caching
            services.AddDistributedMemoryCache();
            Log.Information("Using in-memory distributed cache (Redis not configured)");
        }

        return services;
    }
}
