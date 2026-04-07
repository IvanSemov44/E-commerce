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
using ECommerce.SharedKernel.Interfaces;
using ECommerce.Payments.Application.Interfaces;
using ECommerce.SharedKernel.Interfaces;
using ECommerce.Catalog.Infrastructure.Persistence;
using ECommerce.Inventory.Domain.Aggregates.InventoryItem;
using ECommerce.Inventory.Infrastructure.Persistence;
using ECommerce.Reviews.Infrastructure.Persistence;
using ECommerce.Identity.Infrastructure.Persistence;
using ECommerce.Ordering.Infrastructure.Persistence;
using ECommerce.Shopping.Infrastructure.Persistence;
using ECommerce.Promotions.Application.Interfaces;
using ECommerce.Promotions.Domain.Interfaces;
using ECommerce.Promotions.Infrastructure.Persistence;
using BCrypt.Net;
using Microsoft.Extensions.Hosting;

namespace ECommerce.Tests.Integration;

/// <summary>
/// Conditional authentication handler that can enable/disable authentication based on configuration.
/// </summary>
public class ConditionalTestAuthHandler(IOptionsMonitor<AuthenticationSchemeOptions> options, ILoggerFactory logger, System.Text.Encodings.Web.UrlEncoder encoder, ISystemClock clock) : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder, clock)
{
    public const string TestUserId = "11111111-1111-1111-1111-111111111111";
    public const string TestAdminUserId = "33333333-3333-3333-3333-333333333333";
    public const string TestOrderId = "44444444-4444-4444-4444-444444444444";

    // Static flags to control authentication and user context per test session
    public static bool IsAuthenticationEnabled { get; set; } = true;
    public static string CurrentUserId { get; set; } = TestUserId;
    public static string CurrentUserRole { get; set; } = "Customer";

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
    private readonly string _databaseName = $"IntegrationTestsDb_{Guid.NewGuid():N}";
    private readonly string _catalogDatabaseName = $"IntegrationTestsCatalogDb_{Guid.NewGuid():N}";
    private readonly string _identityDatabaseName = $"IntegrationTestsIdentityDb_{Guid.NewGuid():N}";
    private readonly string _inventoryDatabaseName = $"IntegrationTestsInventoryDb_{Guid.NewGuid():N}";
    private readonly string _orderingDatabaseName = $"IntegrationTestsOrderingDb_{Guid.NewGuid():N}";
    private readonly string _reviewsDatabaseName = $"IntegrationTestsReviewsDb_{Guid.NewGuid():N}";
    private readonly string _shoppingDatabaseName = $"IntegrationTestsShoppingDb_{Guid.NewGuid():N}";
    private readonly string _promotionsDatabaseName = $"IntegrationTestsPromotionsDb_{Guid.NewGuid():N}";
    private readonly FakePromoCodeRepository _promoCodeRepository = new();
    private static readonly string _testPasswordHash = BCrypt.Net.BCrypt.HashPassword("TestPassword123!");
    private static readonly Guid SeededPromoCodeId = Guid.Parse("55555555-5555-5555-5555-555555555555");
    private static readonly string _defaultConnectionString =
        Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection")
        ?? "Host=localhost;Database=ECommerceDb;Username=ecommerce;Password=local-dev-password-123";

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
        builder.UseSetting("ConnectionStrings:DefaultConnection", _defaultConnectionString);
        builder.UseSetting("Serilog:MinimumLevel:Default", "Debug");
        builder.UseSetting("RateLimiting:GlobalLimit", "100000");
        builder.UseSetting("RateLimiting:AuthLimit", "100000");
        builder.UseSetting("RateLimiting:PasswordResetLimit", "100000");

        builder.ConfigureTestServices(services =>
        {
            // Register Promotions configuration assembly before replacing DbContext
            // This ensures the PromoCode entity configuration is available
            ECommerce.Infrastructure.Data.AppDbContext.RegisterConfigurationAssembly(
                typeof(ECommerce.Promotions.Infrastructure.DependencyInjection).Assembly);

            // Replace AppDbContext with InMemory DB
            var descriptor = services.SingleOrDefault(d => d.ServiceType == typeof(DbContextOptions<AppDbContext>));
            if (descriptor != null) services.Remove(descriptor);

            var reviewsDescriptor = services.SingleOrDefault(d => d.ServiceType == typeof(DbContextOptions<ReviewsDbContext>));
            if (reviewsDescriptor != null) services.Remove(reviewsDescriptor);

            var catalogDescriptor = services.SingleOrDefault(d => d.ServiceType == typeof(DbContextOptions<CatalogDbContext>));
            if (catalogDescriptor != null) services.Remove(catalogDescriptor);

            var identityDescriptor = services.SingleOrDefault(d => d.ServiceType == typeof(DbContextOptions<IdentityDbContext>));
            if (identityDescriptor != null) services.Remove(identityDescriptor);

            var inventoryDescriptor = services.SingleOrDefault(d => d.ServiceType == typeof(DbContextOptions<InventoryDbContext>));
            if (inventoryDescriptor != null) services.Remove(inventoryDescriptor);

            var orderingDescriptor = services.SingleOrDefault(d => d.ServiceType == typeof(DbContextOptions<OrderingDbContext>));
            if (orderingDescriptor != null) services.Remove(orderingDescriptor);

            var promotionsDescriptor = services.SingleOrDefault(d => d.ServiceType == typeof(DbContextOptions<PromotionsDbContext>));
            if (promotionsDescriptor != null) services.Remove(promotionsDescriptor);

            var shoppingDescriptor = services.SingleOrDefault(d => d.ServiceType == typeof(DbContextOptions<ShoppingDbContext>));
            if (shoppingDescriptor != null) services.Remove(shoppingDescriptor);

            services.RemoveAll(typeof(IEmailService));
            services.AddScoped<IEmailService, NoOpEmailService>();

            services.RemoveAll(typeof(IDistributedCache));
            services.AddDistributedMemoryCache();

            // Replace webhook verification service with test implementation (always returns true)
            services.RemoveAll(typeof(IWebhookVerificationService));
            services.AddScoped<IWebhookVerificationService, TestWebhookVerificationService>();

            services.RemoveAll<IPromoCodeRepository>();
            services.AddSingleton<IPromoCodeRepository>(_promoCodeRepository);

            services.RemoveAll<IPromoProjectionEventPublisher>();
            services.AddSingleton<IPromoProjectionEventPublisher, NoOpPromoProjectionEventPublisher>();

            services.RemoveAll<ECommerce.Inventory.Application.Interfaces.IInventoryProjectionEventPublisher>();
            services.AddScoped<ECommerce.Inventory.Application.Interfaces.IInventoryProjectionEventPublisher, NoOpInventoryProjectionEventPublisher>();

            // Use a separate internal service provider for EF InMemory to avoid multiple provider registrations
            var inMemoryServiceProvider = new ServiceCollection()
                .AddEntityFrameworkInMemoryDatabase()
                .BuildServiceProvider();

            services.AddDbContext<AppDbContext>(options =>
            {
                options.UseInMemoryDatabase(_databaseName);
                options.UseInternalServiceProvider(inMemoryServiceProvider);
                // Suppress transaction warnings for InMemory provider (doesn't support transactions)
                options.ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning));
            });

            services.AddDbContext<ReviewsDbContext>(options =>
            {
                options.UseInMemoryDatabase(_reviewsDatabaseName);
                options.UseInternalServiceProvider(inMemoryServiceProvider);
                options.ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning));
            });

            services.AddDbContext<CatalogDbContext>(options =>
            {
                options.UseInMemoryDatabase(_catalogDatabaseName);
                options.UseInternalServiceProvider(inMemoryServiceProvider);
                options.ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning));
            });

            services.AddDbContext<IdentityDbContext>(options =>
            {
                options.UseInMemoryDatabase(_identityDatabaseName);
                options.UseInternalServiceProvider(inMemoryServiceProvider);
                options.ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning));
            });

            services.AddDbContext<InventoryDbContext>(options =>
            {
                options.UseInMemoryDatabase(_inventoryDatabaseName);
                options.UseInternalServiceProvider(inMemoryServiceProvider);
                options.ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning));
            });

            services.AddDbContext<OrderingDbContext>(options =>
            {
                options.UseInMemoryDatabase(_orderingDatabaseName);
                options.UseInternalServiceProvider(inMemoryServiceProvider);
                options.ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning));
            });

            services.AddDbContext<PromotionsDbContext>(options =>
            {
                options.UseInMemoryDatabase(_promotionsDatabaseName);
                options.UseInternalServiceProvider(inMemoryServiceProvider);
                options.ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning));
            });

            services.AddDbContext<ShoppingDbContext>(options =>
            {
                options.UseInMemoryDatabase(_shoppingDatabaseName);
                options.UseInternalServiceProvider(inMemoryServiceProvider);
                options.ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning));
            });

            services.RemoveAll<IUnitOfWork>();
            services.AddScoped<IUnitOfWork, TestUnitOfWork>();

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
                db.Database.EnsureCreated();

                var reviewsDb = scopedServices.GetRequiredService<ReviewsDbContext>();
                reviewsDb.Database.EnsureCreated();

                var catalogDb = scopedServices.GetRequiredService<CatalogDbContext>();
                catalogDb.Database.EnsureCreated();

                var identityDb = scopedServices.GetRequiredService<IdentityDbContext>();
                identityDb.Database.EnsureCreated();

                var inventoryDb = scopedServices.GetRequiredService<InventoryDbContext>();
                inventoryDb.Database.EnsureCreated();

                var orderingDb = scopedServices.GetRequiredService<OrderingDbContext>();
                orderingDb.Database.EnsureCreated();

                var promotionsDb = scopedServices.GetRequiredService<PromotionsDbContext>();
                promotionsDb.Database.EnsureCreated();

                var shoppingDb = scopedServices.GetRequiredService<ShoppingDbContext>();
                shoppingDb.Database.EnsureCreated();

                // Precomputed once per process to avoid repeated hash cost on startup
                string passwordHash = _testPasswordHash;

                var userId = Guid.Parse(ConditionalTestAuthHandler.TestUserId);
                var adminId = Guid.Parse(ConditionalTestAuthHandler.TestAdminUserId);

                db.Users.Add(new User
                {
                    Id = userId,
                    Email = "integration@test.com",
                    FirstName = "Integration",
                    LastName = "User",
                    Role = Core.Enums.UserRole.Customer,
                    PasswordHash = passwordHash,
                    IsEmailVerified = true,
                    CreatedAt = DateTime.UtcNow
                });

                identityDb.Users.Add(new User
                {
                    Id = userId,
                    Email = "integration@test.com",
                    FirstName = "Integration",
                    LastName = "User",
                    Role = Core.Enums.UserRole.Customer,
                    PasswordHash = passwordHash,
                    IsEmailVerified = true,
                    CreatedAt = DateTime.UtcNow
                });

                db.Users.Add(new User
                {
                    Id = adminId,
                    Email = "admin@test.com",
                    FirstName = "Admin",
                    LastName = "User",
                    Role = Core.Enums.UserRole.Admin,
                    PasswordHash = passwordHash,
                    IsEmailVerified = true,
                    CreatedAt = DateTime.UtcNow
                });

                identityDb.Users.Add(new User
                {
                    Id = adminId,
                    Email = "admin@test.com",
                    FirstName = "Admin",
                    LastName = "User",
                    Role = Core.Enums.UserRole.Admin,
                    PasswordHash = passwordHash,
                    IsEmailVerified = true,
                    CreatedAt = DateTime.UtcNow
                });

                var categoryId = Guid.Parse("66666666-6666-6666-6666-666666666666");
                db.Categories.Add(new Category
                {
                    Id = categoryId,
                    Name = "Test Category",
                    Slug = "test-category",
                    IsActive = true
                });

                var productId = Guid.Parse("22222222-2222-2222-2222-222222222222");
                db.Products.Add(new Product
                {
                    Id = productId,
                    Name = "IntegrationProduct",
                    Slug = "integration-product",
                    Price = 10.0m,
                    StockQuantity = 100,
                    IsActive = true,
                    Sku = "TEST-SKU-001",
                    CategoryId = categoryId
                });

                var inventoryResult = InventoryItem.Create(productId, 100, 10);
                if (inventoryResult.IsSuccess)
                {
                    db.InventoryItems.Add(inventoryResult.GetDataOrThrow());
                }

                var inventoryResultForInventoryDb = InventoryItem.Create(productId, 100, 10);
                if (inventoryResultForInventoryDb.IsSuccess)
                {
                    inventoryDb.InventoryItems.Add(inventoryResultForInventoryDb.GetDataOrThrow());
                }

                catalogDb.Categories.Add(new Category
                {
                    Id = categoryId,
                    Name = "Test Category",
                    Slug = "test-category",
                    IsActive = true
                });

                catalogDb.Products.Add(new Product
                {
                    Id = productId,
                    Name = "IntegrationProduct",
                    Slug = "integration-product",
                    Price = 10.0m,
                    StockQuantity = 100,
                    IsActive = true,
                    Sku = "TEST-SKU-001",
                    CategoryId = categoryId
                });

                orderingDb.Products.Add(new ECommerce.Ordering.Infrastructure.Persistence.ProductReadModel
                {
                    Id = productId,
                    Name = "IntegrationProduct",
                    Price = 10.0m,
                    UpdatedAt = DateTime.UtcNow
                });

                orderingDb.PromoCodes.Add(new PromoCodeReadModel
                {
                    Id = Guid.Parse("55555555-5555-5555-5555-555555555555"),
                    Code = "SAVE20",
                    DiscountValue = 20m,
                    IsActive = true,
                    UpdatedAt = DateTime.UtcNow
                });

                orderingDb.Addresses.Add(new AddressReadModel
                {
                    Id = Guid.Parse("77777777-7777-7777-7777-777777777777"),
                    UserId = userId,
                    StreetLine1 = "123 Test St",
                    City = "Testville",
                    Country = "US",
                    PostalCode = "12345",
                    UpdatedAt = DateTime.UtcNow
                });

                // Seed a pending order in OrderingDbContext so ship/cancel handlers can find it
                var testOrderId = Guid.Parse(ConditionalTestAuthHandler.TestOrderId);
                orderingDb.Orders.Add(new ECommerce.Core.Entities.Order
                {
                    Id = testOrderId,
                    OrderNumber = "TEST-ORDER-001",
                    UserId = userId,
                    Status = Core.Enums.OrderStatus.Pending,
                    PaymentStatus = Core.Enums.PaymentStatus.Paid,
                    Subtotal = 20.00m,
                    DiscountAmount = 0.00m,
                    ShippingAmount = 10.00m,
                    TaxAmount = 0.00m,
                    TotalAmount = 30.00m,
                    Currency = "USD",
                    RowVersion = Array.Empty<byte>()
                });
                orderingDb.OrderItems.Add(new ECommerce.Core.Entities.OrderItem
                {
                    Id = Guid.NewGuid(),
                    OrderId = testOrderId,
                    ProductId = productId,
                    ProductName = "IntegrationProduct",
                    UnitPrice = 10.00m,
                    Quantity = 2,
                    TotalPrice = 20.00m
                });

                // Seed a shipped order so cancel-shipped tests can find it
                var shippedOrderId = Guid.Parse("55555555-5555-5555-5555-555555555555");
                orderingDb.Orders.Add(new ECommerce.Core.Entities.Order
                {
                    Id = shippedOrderId,
                    OrderNumber = "TEST-ORDER-SHIPPED-001",
                    UserId = userId,
                    Status = Core.Enums.OrderStatus.Shipped,
                    PaymentStatus = Core.Enums.PaymentStatus.Paid,
                    Subtotal = 20.00m,
                    DiscountAmount = 0.00m,
                    ShippingAmount = 10.00m,
                    TaxAmount = 0.00m,
                    TotalAmount = 30.00m,
                    Currency = "USD",
                    RowVersion = Array.Empty<byte>()
                });
                orderingDb.OrderItems.Add(new ECommerce.Core.Entities.OrderItem
                {
                    Id = Guid.NewGuid(),
                    OrderId = shippedOrderId,
                    ProductId = productId,
                    ProductName = "IntegrationProduct",
                    UnitPrice = 10.00m,
                    Quantity = 2,
                    TotalPrice = 20.00m
                });

                var promoCodeResult = ECommerce.Promotions.Domain.ValueObjects.PromoCodeString.Create("SAVE20");
                var discountValueResult = ECommerce.Promotions.Domain.ValueObjects.DiscountValue.Percentage(20);
                if (promoCodeResult.IsSuccess && discountValueResult.IsSuccess)
                {
                    var promo = ECommerce.Promotions.Domain.Aggregates.PromoCode.PromoCode.Create(
                        promoCodeResult.GetDataOrThrow(),
                        discountValueResult.GetDataOrThrow(),
                        null);
                    SetEntityId(promo, SeededPromoCodeId);
                    _promoCodeRepository.Seed(promo);
                }

                shoppingDb.Products.Add(new ECommerce.Shopping.Infrastructure.Persistence.ProductReadModel
                {
                    Id = productId,
                    IsActive = true,
                    Price = 10.0m,
                    Sku = "TEST-SKU-001"
                });

                shoppingDb.InventoryItems.Add(new InventoryItemReadModel
                {
                    ProductId = productId,
                    Quantity = 100,
                    UpdatedAt = DateTime.UtcNow
                });

                var orderId = Guid.Parse(ConditionalTestAuthHandler.TestOrderId);
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

                db.SaveChanges();
                catalogDb.SaveChanges();
                identityDb.SaveChanges();
                inventoryDb.SaveChanges();
                orderingDb.SaveChanges();
                shoppingDb.SaveChanges();
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
            new Claim(ClaimTypes.Name, roles.Contains("Admin") ? "admin@test.com" : "integration@test.com"),
            new Claim(ClaimTypes.Email, roles.Contains("Admin") ? "admin@test.com" : "integration@test.com")
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

    private sealed class NoOpInventoryProjectionEventPublisher : ECommerce.Inventory.Application.Interfaces.IInventoryProjectionEventPublisher
    {
        public Task PublishStockProjectionUpdatedAsync(
            Guid productId,
            int quantity,
            string reason,
            CancellationToken cancellationToken = default)
            => Task.CompletedTask;
    }

    private sealed class NoOpPromoProjectionEventPublisher : IPromoProjectionEventPublisher
    {
        public Task PublishPromoProjectionUpdatedAsync(
            Guid promoCodeId,
            string code,
            decimal discountValue,
            bool isActive,
            bool isDeleted,
            CancellationToken cancellationToken = default)
            => Task.CompletedTask;
    }

    private static void SetEntityId(object entity, Guid id)
    {
        var property = entity.GetType().BaseType?.GetProperty("Id") ?? entity.GetType().GetProperty("Id");
        property?.SetValue(entity, id);
    }

    private sealed class TestUnitOfWork(IServiceProvider serviceProvider) : IUnitOfWork
    {
        private readonly IServiceProvider _serviceProvider = serviceProvider;

        public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
            => SaveAllAsync(cancellationToken);

        public Task BeginTransactionAsync(CancellationToken cancellationToken = default)
            => Task.CompletedTask;

        public Task CommitTransactionAsync(CancellationToken cancellationToken = default)
            => SaveAllAsync(cancellationToken);

        public Task RollbackTransactionAsync(CancellationToken cancellationToken = default)
            => Task.CompletedTask;

        public bool HasActiveTransaction => false;

        public void Dispose()
        {
        }

        private async Task<int> SaveAllAsync(CancellationToken cancellationToken)
        {
            var total = 0;

            total += await SaveIfAvailableAsync<AppDbContext>(cancellationToken);
            total += await SaveIfAvailableAsync<IdentityDbContext>(cancellationToken);
            total += await SaveIfAvailableAsync<ReviewsDbContext>(cancellationToken);
            total += await SaveIfAvailableAsync<CatalogDbContext>(cancellationToken);
            total += await SaveIfAvailableAsync<InventoryDbContext>(cancellationToken);
            total += await SaveIfAvailableAsync<OrderingDbContext>(cancellationToken);
            total += await SaveIfAvailableAsync<PromotionsDbContext>(cancellationToken);
            total += await SaveIfAvailableAsync<ShoppingDbContext>(cancellationToken);

            return total;
        }

        private async Task<int> SaveIfAvailableAsync<TDbContext>(CancellationToken cancellationToken)
            where TDbContext : DbContext
        {
            var dbContext = _serviceProvider.GetService<TDbContext>();
            if (dbContext is null)
                return 0;

            return await dbContext.SaveChangesAsync(cancellationToken);
        }
    }
}

