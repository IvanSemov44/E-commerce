using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text.Json;
using System.Text;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection.Extensions;
using ECommerce.Infrastructure.Data;
using ECommerce.Core.Entities;
using ECommerce.Application.Interfaces;

namespace ECommerce.Tests.Integration;

/// <summary>
/// Conditional authentication handler that can enable/disable authentication based on configuration.
/// </summary>
public class ConditionalTestAuthHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    public const string TestUserId = "11111111-1111-1111-1111-111111111111";
    public const string TestAdminUserId = "33333333-3333-3333-3333-333333333333";
    
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

        // Build claims based on current user role
        var claims = new List<Claim>
        { 
            new Claim(ClaimTypes.NameIdentifier, CurrentUserId), 
            new Claim(ClaimTypes.Name, CurrentUserRole == "Admin" ? "admin@test" : "integration@test"),
        };
        
        // Add role claims (can add multiple roles)
        if (CurrentUserRole == "Admin" || CurrentUserRole == "SuperAdmin")
        {
            claims.Add(new Claim(ClaimTypes.Role, "Admin"));
            if (CurrentUserRole == "SuperAdmin")
                claims.Add(new Claim(ClaimTypes.Role, "SuperAdmin"));
        }
        else
        {
            claims.Add(new Claim(ClaimTypes.Role, CurrentUserRole));
        }

        var identity = new ClaimsIdentity(claims, "Test");
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, "Test");
        return Task.FromResult(AuthenticateResult.Success(ticket));
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

        builder.UseEnvironment("Development");
        builder.ConfigureTestServices(services =>
        {
            // Replace AppDbContext with InMemory DB
            var descriptor = services.SingleOrDefault(d => d.ServiceType == typeof(DbContextOptions<AppDbContext>));
            if (descriptor != null) services.Remove(descriptor);

            services.RemoveAll(typeof(IEmailService));
            services.AddScoped<IEmailService, NoOpEmailService>();

            // Use a separate internal service provider for EF InMemory to avoid multiple provider registrations
            var inMemoryServiceProvider = new ServiceCollection()
                .AddEntityFrameworkInMemoryDatabase()
                .BuildServiceProvider();

            services.AddDbContext<AppDbContext>(options =>
            {
                options.UseInMemoryDatabase("IntegrationTestsDb");
                options.UseInternalServiceProvider(inMemoryServiceProvider);
            });

            // Replace authentication with conditional test scheme
            services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = "ConditionalTest";
                options.DefaultChallengeScheme = "ConditionalTest";
            }).AddScheme<AuthenticationSchemeOptions, ConditionalTestAuthHandler>("ConditionalTest", o => { });

            // Ensure DB created and seeded
            var sp = services.BuildServiceProvider();
            using (var scope = sp.CreateScope())
            {
                var scopedServices = scope.ServiceProvider;
                var db = scopedServices.GetRequiredService<AppDbContext>();
                db.Database.EnsureDeleted();
                db.Database.EnsureCreated();

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
                        PasswordHash = "test_hash"
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
                        PasswordHash = "test_hash"
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

                db.SaveChanges();
            }

            // Reset auth to enabled by default
            ConditionalTestAuthHandler.IsAuthenticationEnabled = true;
        });
    }

    /// <summary>
    /// Creates an authenticated HTTP client (customer user).
    /// </summary>
    public HttpClient CreateAuthenticatedClient()
    {
        ConditionalTestAuthHandler.IsAuthenticationEnabled = true;
        ConditionalTestAuthHandler.CurrentUserId = ConditionalTestAuthHandler.TestUserId;
        ConditionalTestAuthHandler.CurrentUserRole = "Customer";
        return CreateClient();
    }

    /// <summary>
    /// Creates an authenticated HTTP client with admin privileges.
    /// </summary>
    public HttpClient CreateAdminClient()
    {
        ConditionalTestAuthHandler.IsAuthenticationEnabled = true;
        ConditionalTestAuthHandler.CurrentUserId = ConditionalTestAuthHandler.TestAdminUserId;
        ConditionalTestAuthHandler.CurrentUserRole = "Admin";
        return CreateClient();
    }

    /// <summary>
    /// Creates an unauthenticated HTTP client.
    /// </summary>
    public HttpClient CreateUnauthenticatedClient()
    {
        ConditionalTestAuthHandler.IsAuthenticationEnabled = false;
        return CreateClient();
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

    private class NoOpEmailService : IEmailService
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
}
