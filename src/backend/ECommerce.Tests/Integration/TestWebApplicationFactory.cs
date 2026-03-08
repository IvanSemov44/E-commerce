using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text.Json;
using System.Text;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.IdentityModel.Tokens;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.Extensions.Options;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection.Extensions;
using ECommerce.Infrastructure.Data;
using ECommerce.Core.Entities;
using ECommerce.Application.Interfaces;
using BCrypt.Net;
using Microsoft.Extensions.Hosting;

namespace ECommerce.Tests.Integration;

/// <summary>
/// Conditional authentication handler that can enable/disable authentication based on configuration.
/// </summary>
public class ConditionalTestAuthHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    public const string TestUserId = "11111111-1111-1111-1111-111111111111";
    public const string TestAdminUserId = "33333333-3333-3333-3333-333333333333";
    public const string TestOrderId = "44444444-4444-4444-4444-444444444444";

    // Static flags to control authentication and user context per test session
    public static bool IsAuthenticationEnabled { get; set; } = true;
    public static string CurrentUserId { get; set; } = TestUserId;
    public static string CurrentUserRole { get; set; } = "Customer";

    public ConditionalTestAuthHandler(IOptionsMonitor<AuthenticationSchemeOptions> options, ILoggerFactory logger, System.Text.Encodings.Web.UrlEncoder encoder, ISystemClock clock)
        : base(options, logger, encoder, clock)
    {
    }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        // If authentication is disabled, return no result (will fall through to next auth scheme)
        if (!IsAuthenticationEnabled)
        {
            return Task.FromResult(AuthenticateResult.NoResult());
        }

        // First, try to extract JWT Bearer token from Authorization header
        var authHeader = Request.Headers["Authorization"].ToString();
        if (!string.IsNullOrEmpty(authHeader) && authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
        {
            var token = authHeader.Substring("Bearer ".Length).Trim();
            try
            {
                var handler = new JwtSecurityTokenHandler();
                var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes("SuperSecretKeyForTestingPurposesOnlyThatIsLongEnough"));
                var validationParameters = new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = key,
                    ValidateIssuer = true,
                    ValidIssuer = "test",
                    ValidateAudience = true,
                    ValidAudience = "test",
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.FromSeconds(10)
                };

                var principal = handler.ValidateToken(token, validationParameters, out SecurityToken validatedToken);
                var ticket = new AuthenticationTicket(principal, "Test");
                return Task.FromResult(AuthenticateResult.Success(ticket));
            }
            catch (Exception ex)
            {
                return Task.FromResult(AuthenticateResult.Fail($"JWT validation failed: {ex.Message}"));
            }
        }

        // If no Bearer token is present, return NoResult() to let [Authorize] reject with 401
        // This ensures unauthenticated clients properly get 401 on protected endpoints
        return Task.FromResult(AuthenticateResult.NoResult());
    }
}

public class TestWebApplicationFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        // Reset auth state at the beginning of each WebHost configuration
        ConditionalTestAuthHandler.IsAuthenticationEnabled = true;
        ConditionalTestAuthHandler.CurrentUserId = ConditionalTestAuthHandler.TestUserId;
        ConditionalTestAuthHandler.CurrentUserRole = "Customer";

        // Use "Test" environment to skip CSRF validation in tests
        builder.UseEnvironment("Test");

        // Configure test-specific configuration values for secrets using UseSetting
        // This ensures values are available before Program.cs runs validation
        builder.UseSetting("Jwt:SecretKey", "SuperSecretKeyForTestingPurposesOnlyThatIsLongEnough");
        builder.UseSetting("Jwt:Issuer", "test");
        builder.UseSetting("Jwt:Audience", "test");
        builder.UseSetting("ConnectionStrings:DefaultConnection", "Host=localhost;Database=TestDb;Username=test;Password=testpassword");

        builder.ConfigureTestServices(services =>
        {
            // Replace AppDbContext with InMemory DB
            var descriptor = services.SingleOrDefault(d => d.ServiceType == typeof(DbContextOptions<AppDbContext>));
            if (descriptor != null) services.Remove(descriptor);

            services.RemoveAll(typeof(IEmailService));
            services.AddScoped<IEmailService, NoOpEmailService>();

            services.RemoveAll(typeof(IDistributedCache));
            services.AddDistributedMemoryCache();

            // Replace webhook verification service with test implementation (always returns true)
            services.RemoveAll(typeof(IWebhookVerificationService));
            services.AddScoped<IWebhookVerificationService, TestWebhookVerificationService>();

            // Use a separate internal service provider for EF InMemory to avoid multiple provider registrations
            var inMemoryServiceProvider = new ServiceCollection()
                .AddEntityFrameworkInMemoryDatabase()
                .BuildServiceProvider();

            services.AddDbContext<AppDbContext>(options =>
            {
                options.UseInMemoryDatabase("IntegrationTestsDb");
                options.UseInternalServiceProvider(inMemoryServiceProvider);
                // Suppress transaction warnings for InMemory provider (doesn't support transactions)
                options.ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning));
            });

            // Replace authentication with conditional test scheme
            services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = "ConditionalTest";
                options.DefaultChallengeScheme = "ConditionalTest";
            }).AddScheme<AuthenticationSchemeOptions, ConditionalTestAuthHandler>("ConditionalTest", _ => { });

            // Ensure DB created and seeded
            var sp = services.BuildServiceProvider();
            using (var scope = sp.CreateScope())
            {
                var scopedServices = scope.ServiceProvider;
                var db = scopedServices.GetRequiredService<AppDbContext>();
                db.Database.EnsureDeleted();
                db.Database.EnsureCreated();

                // Generate proper BCrypt hash for test password
                string passwordHash = BCrypt.Net.BCrypt.HashPassword("TestPassword123!");

                // Seed a customer user
                var userId = Guid.Parse(ConditionalTestAuthHandler.TestUserId);
                if (!db.Users.Any(u => u.Id == userId))
                {
                    db.Users.Add(new User
                    {
                        Id = userId,
                        Email = "integration@test",
                        FirstName = "Integration",
                        LastName = "User",
                        Role = Core.Enums.UserRole.Customer,
                        PasswordHash = passwordHash
                    });
                }

                // Seed an admin user
                var adminId = Guid.Parse(ConditionalTestAuthHandler.TestAdminUserId);
                if (!db.Users.Any(u => u.Id == adminId))
                {
                    db.Users.Add(new User
                    {
                        Id = adminId,
                        Email = "admin@test",
                        FirstName = "Admin",
                        LastName = "User",
                        Role = Core.Enums.UserRole.Admin,
                        PasswordHash = passwordHash
                    });
                }

                // Seed a test product
                var productId = Guid.Parse("22222222-2222-2222-2222-222222222222");
                if (!db.Products.Any())
                {
                    db.Products.Add(new Product
                    {
                        Id = productId,
                        Name = "IntegrationProduct",
                        Slug = "integration-product",
                        Price = 10.0m,
                        StockQuantity = 100,
                        IsActive = true
                    });
                }

                // Seed a test promo code (SAVE20 - 20% discount)
                if (!db.PromoCodes.Any(p => p.Code == "SAVE20"))
                {
                    db.PromoCodes.Add(new PromoCode
                    {
                        Id = Guid.Parse("55555555-5555-5555-5555-555555555555"),
                        Code = "SAVE20",
                        DiscountType = ECommerce.Core.Enums.DiscountType.Percentage,
                        DiscountValue = 20,
                        IsActive = true,
                        MaxUses = null,
                        UsedCount = 0,
                        MinOrderAmount = null,
                        MaxDiscountAmount = null,
                        StartDate = null,
                        EndDate = null,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    });
                }

                // Seed a test order for payment processing tests
                var orderId = Guid.Parse(ConditionalTestAuthHandler.TestOrderId);
                if (!db.Orders.Any(o => o.Id == orderId))
                {
                    db.Orders.Add(new Order
                    {
                        Id = orderId,
                        OrderNumber = "TEST-ORDER-001",
                        UserId = userId,
                        Status = Core.Enums.OrderStatus.Pending,
                        PaymentStatus = Core.Enums.PaymentStatus.Paid,  // Pre-paid for refund testing
                        Subtotal = 100.00m,
                        DiscountAmount = 0.00m,
                        ShippingAmount = 10.00m,
                        TaxAmount = 0.00m,
                        TotalAmount = 100.00m,
                        Currency = "USD"
                    });
                }

                db.SaveChanges();
            }

            // Reset auth to enabled by default
            ConditionalTestAuthHandler.IsAuthenticationEnabled = true;
        });
    }

    /// <summary>
    /// Generates a JWT token for testing with specified roles.
    /// </summary>
    public static string GenerateJwtToken(string userId = "", params string[] roles)
    {
        userId = string.IsNullOrEmpty(userId) ? ConditionalTestAuthHandler.TestUserId : userId;

        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, userId),
            new Claim(ClaimTypes.Name, roles.Contains("Admin") ? "admin@test" : "integration@test"),
            new Claim(ClaimTypes.Email, roles.Contains("Admin") ? "admin@test" : "integration@test")
        };

        // Add role claims
        foreach (var role in roles)
        {
            claims.Add(new Claim(ClaimTypes.Role, role));
        }

        // If no roles specified, add Customer role
        if (roles.Length == 0)
        {
            claims.Add(new Claim(ClaimTypes.Role, "Customer"));
        }

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes("SuperSecretKeyForTestingPurposesOnlyThatIsLongEnough"));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: "test",
            audience: "test",
            claims: claims,
            expires: DateTime.UtcNow.AddHours(1),
            signingCredentials: creds);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    /// <summary>
    /// Creates an authenticated HTTP client (customer user).
    /// CSRF is skipped in Test environment, so no CSRF token handling needed.
    /// </summary>
    public HttpClient CreateAuthenticatedClient()
    {
        var client = CreateClient();
        var token = GenerateJwtToken(ConditionalTestAuthHandler.TestUserId, "Customer");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        client.DefaultRequestHeaders.Remove("Idempotency-Key");
        client.DefaultRequestHeaders.Add("Idempotency-Key", Guid.NewGuid().ToString());
        return client;
    }

    /// <summary>
    /// Creates an authenticated HTTP client with admin privileges.
    /// CSRF is skipped in Test environment, so no CSRF token handling needed.
    /// </summary>
    public HttpClient CreateAdminClient()
    {
        var client = CreateClient();
        var token = GenerateJwtToken(ConditionalTestAuthHandler.TestAdminUserId, "Admin");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        client.DefaultRequestHeaders.Remove("Idempotency-Key");
        client.DefaultRequestHeaders.Add("Idempotency-Key", Guid.NewGuid().ToString());
        return client;
    }

    /// <summary>
    /// Creates an unauthenticated HTTP client (no Authorization header).
    /// This allows [Authorize] decorators to properly reject the request with 401.
    /// </summary>
    public HttpClient CreateUnauthenticatedClient()
    {
        var client = CreateClient();
        client.DefaultRequestHeaders.Remove("Idempotency-Key");
        client.DefaultRequestHeaders.Add("Idempotency-Key", Guid.NewGuid().ToString());
        return client;
    }

    /// <summary>
    /// Resets the authentication state to default (enabled, customer role).
    /// Call this in test cleanup to prevent state leakage to other tests.
    /// </summary>
    public static void ResetAuthState()
    {
        ConditionalTestAuthHandler.IsAuthenticationEnabled = true;
        ConditionalTestAuthHandler.CurrentUserId = ConditionalTestAuthHandler.TestUserId;
        ConditionalTestAuthHandler.CurrentUserRole = "Customer";
    }

    private sealed class NoOpEmailService : IEmailService
    {
        public Task SendWelcomeEmailAsync(string email, string firstName, string verificationLink, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task SendEmailVerificationAsync(string email, string firstName, string verificationLink, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task SendPasswordResetEmailAsync(string email, string firstName, string resetLink, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task SendOrderConfirmationEmailAsync(string email, Order order, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task SendOrderShippedEmailAsync(string email, Order order, string trackingNumber, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task SendOrderDeliveredEmailAsync(string email, Order order, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task SendAbandonedCartEmailAsync(string email, string firstName, Cart cart, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task SendLowStockAlertAsync(string email, string firstName, string productName, int currentStock, int threshold, string? sku = null, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task SendMarketingEmailAsync(string email, string firstName, string subject, string htmlContent, CancellationToken cancellationToken = default) => Task.CompletedTask;
    }

    private sealed class TestWebhookVerificationService : IWebhookVerificationService
    {
        public bool VerifySignature(string payload, string signature) => true;
    }
}
