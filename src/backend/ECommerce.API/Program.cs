using ECommerce.API.ActionFilters;
using ECommerce.API.Behaviors;
using ECommerce.API.Extensions;
using ECommerce.Application.Interfaces;
using FluentValidation;
using MediatR;
using ECommerce.Catalog.Infrastructure;
using ECommerce.Catalog.Application.Commands.CreateProduct;
using ECommerce.Identity.Infrastructure;
using ECommerce.Inventory.Application.Commands.ReduceStock;
using ECommerce.Inventory.Infrastructure;
using ECommerce.Shopping.Application.Commands.AddToCart;
using ECommerce.Shopping.Infrastructure;
using ECommerce.Promotions.Infrastructure;
using ECommerce.Promotions.Application.Commands.CreatePromoCode;
using ECommerce.Reviews.Application.Commands;
using ECommerce.Reviews.Infrastructure;
using ECommerce.Ordering.Infrastructure;
using ECommerce.Ordering.Application.Commands.PlaceOrder;
using Microsoft.AspNetCore.Mvc;

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

// Integration Messaging (Phase 8 Step 4)
builder.Services.AddIntegrationMessaging(builder.Configuration);

// Catalog Infrastructure
builder.Services.AddCatalogInfrastructure();

// Identity Infrastructure
builder.Services.AddIdentityInfrastructure();

// Promotions Infrastructure (Phase 5)
builder.Services.AddPromotionsInfrastructure();

// Reviews Infrastructure (Phase 6)
builder.Services.AddReviewsInfrastructure(builder.Configuration);

// Inventory Infrastructure (Phase 3)
builder.Services.AddInventoryInfrastructure();

// Shopping Infrastructure (Phase 4)
builder.Services.AddShoppingInfrastructure();

// Ordering Infrastructure (Phase 7)
builder.Services.AddOrderingInfrastructure();

// MediatR
builder.Services.AddMediatR(cfg =>
{
    cfg.RegisterServicesFromAssembly(typeof(Program).Assembly);

    // Uncomment each line when that bounded context's Application project is created:
    cfg.RegisterServicesFromAssembly(typeof(CreateProductCommand).Assembly);
    cfg.RegisterServicesFromAssembly(typeof(ECommerce.Identity.Application.Commands.Register.RegisterCommand).Assembly);
    cfg.RegisterServicesFromAssembly(typeof(ECommerce.Inventory.Application.Commands.ReduceStock.ReduceStockCommand).Assembly);
    cfg.RegisterServicesFromAssembly(typeof(ECommerce.Shopping.Application.Commands.AddToCart.AddToCartCommand).Assembly);
    cfg.RegisterServicesFromAssembly(typeof(CreatePromoCodeCommand).Assembly);
    cfg.RegisterServicesFromAssembly(typeof(CreateReviewCommand).Assembly);
    cfg.RegisterServicesFromAssembly(typeof(ECommerce.Ordering.Application.Commands.PlaceOrder.PlaceOrderCommand).Assembly);

    // Pipeline order matters: outermost first
    cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(LoggingBehavior<,>));
    cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(PerformanceBehavior<,>));
    cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
    cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(TransactionBehavior<,>));
});

// FluentValidation
builder.Services.AddValidatorsFromAssembly(typeof(Program).Assembly);
// Also register validators from the Catalog application assembly
builder.Services.AddValidatorsFromAssembly(typeof(CreateProductCommand).Assembly);
// Also register validators from the Shopping application assembly
builder.Services.AddValidatorsFromAssembly(typeof(ECommerce.Shopping.Application.Commands.AddToCart.AddToCartCommand).Assembly);
// Also register validators from the Promotions application assembly
builder.Services.AddValidatorsFromAssembly(typeof(ECommerce.Promotions.Application.Commands.CreatePromoCode.CreatePromoCodeCommand).Assembly);
// Also register validators from the Identity application assembly
builder.Services.AddValidatorsFromAssembly(typeof(ECommerce.Identity.Application.Commands.Register.RegisterCommand).Assembly);

// Controllers & Validation
builder.Services.AddControllers()
    .ConfigureApiBehaviorOptions(options =>
    {
        options.InvalidModelStateResponseFactory = context =>
        {
            var validationErrors = context.ModelState
                .Where(ms => ms.Value?.Errors.Count > 0)
                .ToDictionary(
                    x => x.Key,
                    x => x.Value!.Errors.Select(e => e.ErrorMessage).ToArray());

            var errorResponse = ECommerce.Application.DTOs.Common.ApiResponse<object>.Failure(
                "Validation failed",
                "VALIDATION_FAILED",
                validationErrors);

            return new BadRequestObjectResult(errorResponse);
        };
    });
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
if (!app.Environment.IsEnvironment("Test"))
{
    try
    {
        using (var scope = app.Services.CreateScope())
        {
            // Validate each critical service can be instantiated
            // This catches missing dependencies and circular references early
            _ = scope.ServiceProvider.GetRequiredService<IMediator>();

            _ = scope.ServiceProvider.GetRequiredService<IPaymentService>();
        }
        Serilog.Log.Information("✓ Dependency injection validation passed. All critical services are resolvable.");
    }
    catch (Exception ex)
    {
        Serilog.Log.Fatal(ex, "✗ Dependency injection validation failed - missing or circular dependencies");
        throw;
    }
}

// Apply migrations and seed database
await app.ApplyMigrationsAndSeedAsync();

// Configure middleware pipeline
app.ConfigureMiddlewarePipeline();

app.Run();
