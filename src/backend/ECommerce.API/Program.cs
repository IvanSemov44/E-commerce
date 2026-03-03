using ECommerce.API.ActionFilters;
using ECommerce.API.Extensions;
using ECommerce.Application.Interfaces;

// ============================================================================
// E-Commerce API - Application Entry Point
// ============================================================================

var builder = WebApplication.CreateBuilder(args);

// ============================================================================
// Serilog Configuration (Configuration-Based with Cloud-Ready Sinks)
// ============================================================================
builder.ConfigureSerilog();

// ============================================================================
// Startup Validation - Fail Fast on Missing Secrets
// ============================================================================
builder.Configuration.ValidateRequiredConfiguration();

// ============================================================================
// Service Registration
// ============================================================================

// Configure forwarded headers for reverse proxy (Render, Azure, AWS, etc.)
// This must be called BEFORE other services that use IHttpContextAccessor
builder.Services.AddForwardedHeadersConfiguration();

// Database
builder.Services.AddPostgreSqlDatabase(builder.Configuration);

// Data Protection - persistent key storage for containerized environments
builder.Services.AddDataProtectionConfiguration(builder.Configuration);

// Authentication & Authorization
builder.Services.AddJwtAuthentication(builder.Configuration);

// Business Rules
builder.Services.AddBusinessRulesConfiguration(builder.Configuration);

// Health Checks
builder.Services.AddHealthChecksConfiguration(builder.Configuration);

// Redis Caching
builder.Services.AddRedisCaching(builder.Configuration);

// JSON Serialization
builder.Services.AddStrictJsonSerialization();

// CORS
builder.Services.AddCorsConfiguration(builder.Environment.IsDevelopment(), builder.Configuration);

// Rate Limiting
builder.Services.AddRateLimitingConfiguration(builder.Configuration);

// CSRF Protection
builder.Services.AddCsrfProtection(builder.Environment);

// Resilience Policies (Polly - Retry, Circuit Breaker, Bulkhead, Timeout)
builder.Services.AddResiliencePolicies();

// Application Services
builder.Services.AddApplicationServices(builder.Configuration);

// Controllers & Validation
builder.Services.AddControllers();
builder.Services.AddScoped<ValidationFilterAttribute>();

// Swagger Documentation
builder.Services.AddSwaggerDocumentation();

// ============================================================================
// Application Pipeline
// ============================================================================

var app = builder.Build();

// ============================================================================
// DI Validation - Ensure all critical services are resolvable
// ============================================================================
try
{
    using (var scope = app.Services.CreateScope())
    {
        // Validate each critical service can be instantiated
        // This catches missing dependencies and circular references early
        _ = scope.ServiceProvider.GetRequiredService<IOrderService>();
        _ = scope.ServiceProvider.GetRequiredService<IAuthService>();
        _ = scope.ServiceProvider.GetRequiredService<IProductService>();
        _ = scope.ServiceProvider.GetRequiredService<ICartService>();
        _ = scope.ServiceProvider.GetRequiredService<IPaymentService>();
    }
    Serilog.Log.Information("✓ Dependency injection validation passed. All critical services are resolvable.");
}
catch (Exception ex)
{
    Serilog.Log.Fatal(ex, "✗ Dependency injection validation failed - missing or circular dependencies");
    throw;
}

// Apply migrations and seed database
await app.ApplyMigrationsAndSeedAsync();

// Configure middleware pipeline
app.ConfigureMiddlewarePipeline();

app.Run();
