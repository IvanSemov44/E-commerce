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
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Http.Json;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Serilog;
using StackExchange.Redis;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.RateLimiting;
using AutoMapper;

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
        // Register JwtOptions for DI
        services.Configure<JwtOptions>(configuration.GetSection(JwtOptions.SectionName));

        // Get JWT settings from configuration
        var jwtSettings = configuration.GetSection("Jwt");
        var jwtOptions = jwtSettings.Get<JwtOptions>() ?? new JwtOptions();

        // Validate JWT configuration
        jwtOptions.Validate();

        var secretKey = Encoding.UTF8.GetBytes(jwtOptions.SecretKey);

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
                ValidIssuer = jwtOptions.Issuer,
                ValidAudience = jwtOptions.Audience,
                ValidateLifetime = true,
                ClockSkew = TimeSpan.FromSeconds(jwtOptions.ClockSkewSeconds)
            };

            // 🔒 SECURITY: Support reading JWT from httpOnly cookie
            // Uses app-specific cookie names to prevent conflicts between admin and storefront
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
                        // Determine which app cookie to read based on Origin header
                        var cookiePrefix = GetCookiePrefix(context.Request);
                        var accessToken = context.Request.Cookies[$"{cookiePrefix}_accessToken"];
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
    /// <param name="environment">The host environment to determine cookie settings.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddCsrfProtection(this IServiceCollection services, IHostEnvironment environment)
    {
        var isProduction = !environment.IsDevelopment();

        services.AddAntiforgery(options =>
        {
            // Use standard XSRF header name that SPAs expect
            options.HeaderName = "X-XSRF-TOKEN";
            // The antiforgery system uses an internal cookie for the cookie token
            // We'll set the request token in a separate cookie via middleware
            options.Cookie.HttpOnly = true; // The internal cookie should be httpOnly
            options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
            // Use SameSite=None in production for cross-origin support (required for Render deployment)
            // Use SameSite=Lax in development for local testing
            options.Cookie.SameSite = isProduction ? SameSiteMode.None : SameSiteMode.Lax;
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
    /// Configures CORS policies based on the hosting environment.
    /// Uses distinct policy names for development and production environments.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">The application configuration.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddCorsConfiguration(
        this IServiceCollection services,
        bool _,
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
                // Try to get allowed origins from configuration
                // Supports both array format (from appsettings.json) and comma-separated string (from environment variables)
                var allowedOriginsArray = configuration.GetSection("AllowedOrigins").Get<string[]>();
                var allowedOriginsString = configuration["AllowedOrigins"];

                var allowedOrigins = allowedOriginsArray?.Length > 0
                    ? allowedOriginsArray
                    : allowedOriginsString?.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                    ?? Array.Empty<string>();

                if (allowedOrigins.Length == 0)
                {
                    // Log warning but don't throw - allow configuration via environment variables
                    Log.Warning("No AllowedOrigins configured. Set AllowedOrigins environment variable with comma-separated URLs (e.g., 'https://example.com,https://app.example.com')");
                }
                else
                {
                    Log.Information("CORS allowed origins: {Origins}", string.Join(", ", allowedOrigins));
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
    /// <param name="configuration">The application configuration.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddRateLimitingConfiguration(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Register rate limiting options for DI
        services.Configure<RateLimitingOptions>(configuration.GetSection(RateLimitingOptions.SectionName));

        var rateLimitOptions = configuration.GetSection(RateLimitingOptions.SectionName).Get<RateLimitingOptions>()
            ?? new RateLimitingOptions();

        services.AddRateLimiter(options =>
        {
            // Global rate limit: configured requests per IP per window
            options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
                RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey: context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                    factory: _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = rateLimitOptions.GlobalLimit,
                        Window = TimeSpan.FromSeconds(rateLimitOptions.GlobalWindowSeconds),
                        QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                        QueueLimit = 0
                    }));

            // Strict limiter for auth endpoints (login/register)
            options.AddPolicy("AuthLimit", context =>
                RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey: context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                    factory: _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = rateLimitOptions.AuthLimit,
                        Window = TimeSpan.FromSeconds(rateLimitOptions.AuthWindowSeconds),
                        QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                        QueueLimit = 0
                    }));

            // Strict limiter for password reset
            options.AddPolicy("PasswordResetLimit", context =>
                RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey: context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                    factory: _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = rateLimitOptions.PasswordResetLimit,
                        Window = TimeSpan.FromMinutes(rateLimitOptions.PasswordResetWindowMinutes),
                        QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                        QueueLimit = 0
                    }));

            options.RejectionStatusCode = rateLimitOptions.RejectionStatusCode;
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

        // Old UnitOfWork — used by existing services, untouched.
        services.AddScoped<IUnitOfWork, UnitOfWork>();
        // MediatRUnitOfWork — used by MediatR handlers, old code knows nothing about it.
        services.AddScoped<ECommerce.SharedKernel.Interfaces.IUnitOfWork, MediatRUnitOfWork>();

        // HTTP context accessor
        services.AddHttpContextAccessor();

        // Domain services
        services.AddScoped<ICurrentUserService, CurrentUserService>();
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IProductService, ProductService>();
        services.AddScoped<ICategoryService, CategoryService>();
        services.AddScoped<IPaymentService, PaymentService>();
        services.AddSingleton<IPaymentStore, InMemoryPaymentStore>();
        services.AddSingleton<IIdempotencyStore, DistributedIdempotencyStore>();
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
        {
            options.UseNpgsql(connectionString, npgsqlOptions =>
            {
                // NOTE: EnableRetryOnFailure is not compatible with manual transactions (BeginTransactionAsync)
                // The OrderService uses manual transactions for atomicity, so we cannot use retry on failure here.
                // If retry logic is needed, it should be implemented at the application level using IExecutionStrategy.

                // SplitQuery separates complex multi-include queries into multiple SQL queries
                // to avoid generating a large cartesian product.
                npgsqlOptions.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery);
            });

            // Configure warnings - ignore pending model changes warning
            options.ConfigureWarnings(warnings => warnings
                .Ignore(Microsoft.EntityFrameworkCore.Diagnostics.RelationalEventId.PendingModelChangesWarning));
        });

        Log.Information("PostgreSQL database configured with retry on failure and split query behavior");
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

            try
            {
                var redisOptions = ConfigurationOptions.Parse(redisConnectionString);
                redisOptions.AbortOnConnectFail = false;

                var multiplexer = ConnectionMultiplexer.Connect(redisOptions);
                services.AddSingleton<IConnectionMultiplexer>(multiplexer);
            }
            catch (Exception exception)
            {
                Log.Warning(exception, "Redis multiplexer initialization failed; idempotency will use distributed-cache fallback semantics");
            }

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

    /// <summary>
    /// Configures forwarded headers middleware for proper HTTPS detection behind reverse proxies.
    /// Required for Render, Azure, AWS, and other cloud providers that terminate SSL at the load balancer.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddForwardedHeadersConfiguration(this IServiceCollection services)
    {
        services.Configure<ForwardedHeadersOptions>(options =>
        {
            options.ForwardedHeaders = Microsoft.AspNetCore.HttpOverrides.ForwardedHeaders.XForwardedFor |
                                       Microsoft.AspNetCore.HttpOverrides.ForwardedHeaders.XForwardedProto;

            // Clear known networks and proxies to trust all proxies
            // This is safe because Render's load balancer sets these headers
            options.KnownIPNetworks.Clear();
            options.KnownProxies.Clear();

            // Increase the limit for the number of proxies in the chain
            options.ForwardLimit = null;
        });

        Log.Information("Forwarded headers configured for reverse proxy (Render)");
        return services;
    }

    /// <summary>
    /// Configures ASP.NET Core Data Protection with persistent key storage.
    /// This ensures that encrypted cookies and sensitive data remain valid across container restarts.
    /// For Render and similar containerized environments, keys are stored in the database.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">The application configuration.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddDataProtectionConfiguration(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var dataProtectionSection = configuration.GetSection("DataProtection");

        // Check if database storage is configured (recommended for production)
        var useDatabaseStorage = dataProtectionSection.GetValue<bool>("UseDatabaseStorage");

        if (useDatabaseStorage)
        {
            // Get the connection string for the Data Protection keys database
            // By default, use the same database as the application
            var connectionString = configuration.GetConnectionString("DefaultConnection")
                ?? configuration.GetConnectionString("DataProtectionConnection");

            if (!string.IsNullOrEmpty(connectionString))
            {
                services.AddDataProtection()
                    .PersistKeysToDbContext<AppDbContext>()
                    .SetApplicationName("ECommerce-API");

                Log.Information("Data Protection keys configured with database persistence");
                return services;
            }
        }

        // Fallback: Use file system storage (works for containers with persistent volumes)
        var keysDirectory = dataProtectionSection["KeysDirectory"];

        if (!string.IsNullOrEmpty(keysDirectory))
        {
            var keysPath = Path.Combine(keysDirectory, "DataProtection-Keys");
            Directory.CreateDirectory(keysPath);

            services.AddDataProtection()
                .PersistKeysToFileSystem(new DirectoryInfo(keysPath))
                .SetApplicationName("ECommerce-API");

            Log.Information("Data Protection keys configured with file system persistence: {Path}", keysPath);
            return services;
        }

        // Default: Let ASP.NET Core use its default location
        // This will show a warning in containers without persistent storage
        Log.Warning("Data Protection keys using default ephemeral storage. " +
                    "Keys will be lost on container restart. " +
                    "Configure DataProtection:UseDatabaseStorage=true for production.");
        return services;
    }

    /// <summary>
    /// Gets the cookie prefix based on the requesting app.
    /// Admin panel uses "admin" prefix, storefront uses "storefront" prefix.
    /// This prevents cookie conflicts when both apps are used in the same browser.
    /// </summary>
    private static string GetCookiePrefix(HttpRequest request)
    {
        // Check for custom header that identifies the app
        if (request.Headers.TryGetValue("X-App-Origin", out var appOrigin))
        {
            var appOriginStr = appOrigin.ToString().ToLowerInvariant();
            if (appOriginStr.Contains("admin") || appOriginStr.Contains("5177") || appOriginStr.Contains("3001"))
            {
                return "admin";
            }
        }

        // Check Origin header
        if (request.Headers.TryGetValue("Origin", out var originHeader))
        {
            var originStr = originHeader.ToString().ToLowerInvariant();
            if (originStr.Contains("5177") || originStr.Contains("3001"))
            {
                return "admin";
            }
        }

        // Check Referer header as fallback
        if (request.Headers.TryGetValue("Referer", out var referer))
        {
            var refererStr = referer.ToString().ToLowerInvariant();
            if (refererStr.Contains("5177") || refererStr.Contains("3001"))
            {
                return "admin";
            }
        }

        // Default to storefront
        return "storefront";
    }
}
