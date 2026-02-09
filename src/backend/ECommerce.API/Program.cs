using ECommerce.API.Middleware;
using ECommerce.Application;
using ECommerce.Application.Configuration;
using ECommerce.Application.Interfaces;
using ECommerce.Application.Services;
using ECommerce.Core.Interfaces.Repositories;
using ECommerce.Infrastructure;
using ECommerce.Infrastructure.Data;
using ECommerce.Infrastructure.Data.Seeders;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Serilog;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using ECommerce.Application.Validators.Cart;
using ECommerce.API.ActionFilters;
using FluentValidation;
using FluentValidation.AspNetCore;
using System.Threading.RateLimiting;

var builder = WebApplication.CreateBuilder(args);

// Serilog configuration
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .Enrich.FromLogContext()
    .Enrich.WithMachineName()
    .WriteTo.Console()
    .WriteTo.File("logs/app-.txt", rollingInterval: RollingInterval.Day)
    .WriteTo.File("logs/security-.txt",
        rollingInterval: RollingInterval.Day,
        restrictedToMinimumLevel: Serilog.Events.LogEventLevel.Warning)
    .CreateLogger();

builder.Host.UseSerilog();

// Add services to the container
var configuration = builder.Configuration;

// Database configuration
var connectionString = configuration.GetConnectionString("DefaultConnection")
    ?? "Host=localhost;Database=ECommerceDb;Username=ecommerce;Password=YourPassword123!";

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(connectionString)
);

// JWT Authentication configuration
var jwtSettings = configuration.GetSection("Jwt");
var secretKey = Encoding.UTF8.GetBytes(jwtSettings["SecretKey"] ?? "your-secret-key-minimum-32-characters-long");

var isDevelopment = builder.Environment.IsDevelopment();

builder.Services.AddAuthentication(options =>
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
});

builder.Services.AddAuthorization();

// Configure business rules options
builder.Services.Configure<BusinessRulesOptions>(
    configuration.GetSection(BusinessRulesOptions.SectionName));

// Configure JSON serialization to be case-sensitive (strict)
builder.Services.Configure<JsonOptions>(options =>
{
    options.SerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
    options.SerializerOptions.PropertyNameCaseInsensitive = false; // Strict: reject PascalCase
    options.SerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
});

// Add CORS
builder.Services.AddCors(options =>
{
    if (isDevelopment)
    {
        options.AddPolicy("AllowAll", policy =>
        {
            policy.AllowAnyOrigin()
                .AllowAnyMethod()
                .AllowAnyHeader();
        });
    }
    else
    {
        var allowedOrigins = configuration.GetSection("Cors:Origins").Get<string[]>()
            ?? Array.Empty<string>();

        if (allowedOrigins.Length == 0)
            throw new InvalidOperationException("CORS origins must be configured for production");

        options.AddPolicy("AllowAll", policy =>
        {
            policy.WithOrigins(allowedOrigins)
                .WithMethods("GET", "POST", "PUT", "DELETE", "OPTIONS")
                .WithHeaders("Content-Type", "Authorization", "Accept")
                .AllowCredentials();
        });
    }
});

// Add Rate Limiting
builder.Services.AddRateLimiter(options =>
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

// Add AutoMapper
builder.Services.AddAutoMapper(cfg => cfg.AddProfile<MappingProfile>());

// Add FluentValidation
builder.Services.AddFluentValidationAutoValidation();
builder.Services.AddValidatorsFromAssemblyContaining<AddToCartDtoValidator>();

// Register Unit of Work
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();

// Register HTTP context accessor
builder.Services.AddHttpContextAccessor();

// Register services
builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IProductService, ProductService>();
builder.Services.AddScoped<ICategoryService, CategoryService>();
builder.Services.AddScoped<IPaymentService, PaymentService>();
builder.Services.AddSingleton<IPaymentStore, InMemoryPaymentStore>();
builder.Services.AddScoped<IOrderService, OrderService>();
builder.Services.AddScoped<ICartService, CartService>();
builder.Services.AddScoped<IReviewService, ReviewService>();
builder.Services.AddScoped<IWishlistService, WishlistService>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IDashboardService, DashboardService>();
builder.Services.AddScoped<IPromoCodeService, PromoCodeService>();
builder.Services.AddScoped<IInventoryService, InventoryService>();
builder.Services.AddScoped<IWebhookVerificationService, WebhookVerificationService>();

// Register email service based on configuration
var emailProvider = configuration["EmailProvider"] ?? "SendGrid";
if (emailProvider.Equals("Smtp", StringComparison.OrdinalIgnoreCase))
{
    builder.Services.AddScoped<IEmailService, SmtpEmailService>();
    Log.Information("Using SMTP email provider");
}
else
{
    builder.Services.AddScoped<IEmailService, SendGridEmailService>();
    Log.Information("Using SendGrid email provider");
}

// Register seeders
builder.Services.AddScoped<IUserSeeder, UserSeeder>();
builder.Services.AddScoped<ICategorySeeder, CategorySeeder>();
builder.Services.AddScoped<IProductSeeder, ProductSeeder>();
builder.Services.AddScoped<DatabaseSeeder>();

// Add logging
builder.Services.AddLogging();

// Add controllers
builder.Services.AddControllers();
// Register action filters
builder.Services.AddScoped<ValidationFilterAttribute>();

// Add Swagger/OpenAPI
builder.Services.AddOpenApi();
builder.Services.AddSwaggerGen(c =>
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

var app = builder.Build();

// Apply migrations and seed database
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var context = services.GetRequiredService<AppDbContext>();
        // Only call relational-specific migration APIs when using a relational provider
        try
        {
            // Try to get pending migrations; GetPendingMigrations may throw for non-relational providers
            IEnumerable<string> pendingMigrations = Enumerable.Empty<string>();
            try
            {
                pendingMigrations = context.Database.GetPendingMigrations();
            }
            catch (InvalidOperationException ex)
            {
                Log.Warning(ex, "Skipping migration checks for non-relational provider.");
            }

            if (pendingMigrations != null && pendingMigrations.Any())
            {
                Log.Information("Applying pending migrations...");
                context.Database.Migrate();
            }

            // Seed sample data (seeders should be resilient to InMemory provider)
            // Seeding is skipped in production environments for safety
            Log.Information("Seeding database with sample data...");
            var seeder = services.GetRequiredService<DatabaseSeeder>();
            await seeder.SeedAsync(context, app.Environment);
            Log.Information("Database seeding completed.");
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "An error occurred while applying migrations or seeding database (non-fatal in tests).");
        }
    }
    catch (Exception ex)
    {
        Log.Error(ex, "An error occurred while applying migrations or seeding database");
    }
}

// Configure security headers middleware (must be first in pipeline)
app.UseMiddleware<SecurityHeadersMiddleware>();

// Configure global exception handler middleware
app.UseMiddleware<GlobalExceptionMiddleware>();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "E-Commerce API v1");
        options.RoutePrefix = string.Empty; // Set Swagger UI at root
    });
}

// Skip HTTPS redirect in development to allow localhost access
if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

app.UseCors("AllowAll");

// Rate limiting must come after routing and before authentication
app.UseRateLimiter();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// Health check endpoint for Docker
app.MapGet("/health", () => Results.Ok(new { status = "healthy", timestamp = DateTime.UtcNow }))
    .WithName("Health")
    .WithOpenApi()
    .AllowAnonymous();

app.Run();
