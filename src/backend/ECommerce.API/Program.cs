using ECommerce.API.ActionFilters;
using ECommerce.API.Extensions;

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
builder.Services.AddRateLimitingConfiguration();

// CSRF Protection
builder.Services.AddCsrfProtection();

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

// Apply migrations and seed database
await app.ApplyMigrationsAndSeedAsync();

// Configure middleware pipeline
app.ConfigureMiddlewarePipeline();

app.Run();
