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
using ECommerce.Infrastructure.Integration;
using ECommerce.SharedKernel.Entities;
using ECommerce.SharedKernel.DTOs;
using ECommerce.SharedKernel.Interfaces;
using CatalogCategory = ECommerce.Catalog.Domain.Aggregates.Category.Category;
using CatalogProduct = ECommerce.Catalog.Domain.Aggregates.Product.Product;
using ECommerce.Payments.Application.Interfaces;
using ECommerce.Catalog.Infrastructure.Persistence;
using ECommerce.Inventory.Domain.Aggregates.InventoryItem;
using OrderingOrder = ECommerce.Ordering.Domain.Aggregates.Order.Order;
using OrderingOrderItem = ECommerce.Ordering.Domain.Aggregates.Order.OrderItem;
using OrderingOrderItemData = ECommerce.Ordering.Domain.Aggregates.Order.OrderItemData;
using OrderingShippingAddress = ECommerce.Ordering.Domain.ValueObjects.ShippingAddress;
using OrderingOrderStatus = ECommerce.Ordering.Domain.ValueObjects.OrderStatus;
using ECommerce.Inventory.Infrastructure.Persistence;
using ECommerce.Reviews.Infrastructure.Persistence;
using ECommerce.Identity.Infrastructure.Persistence;
using ECommerce.Ordering.Infrastructure.Persistence;
using ECommerce.Payments.Infrastructure.Persistence;
using ECommerce.Shopping.Infrastructure.Persistence;
using ECommerce.Promotions.Infrastructure.Persistence;
using ECommerce.Identity.Domain.Interfaces;
using ECommerce.Identity.Domain.ValueObjects;
using IdentityUser = ECommerce.Identity.Domain.Aggregates.User.User;
using BCrypt.Net;
using Microsoft.Extensions.Hosting;
using DotNet.Testcontainers.Builders;
using Testcontainers.PostgreSql;

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

public class TestWebApplicationFactory(
    bool useReviewsPostgresContainer = false,
    bool useCatalogPostgresContainer = true,
    bool usePromotionsPostgresContainer = true) : WebApplicationFactory<Program>
{
    private readonly bool _useReviewsPostgresContainer = useReviewsPostgresContainer;
    private readonly bool _useCatalogPostgresContainer = useCatalogPostgresContainer;
    private readonly bool _usePromotionsPostgresContainer = usePromotionsPostgresContainer;
    private readonly string _databaseName = $"IntegrationTestsDb_{Guid.NewGuid():N}";
    private readonly string _catalogDatabaseName = $"IntegrationTestsCatalogDb_{Guid.NewGuid():N}";
    private readonly string _integrationDatabaseName = $"IntegrationTestsIntegrationDb_{Guid.NewGuid():N}";
    private readonly string _identityDatabaseName = $"IntegrationTestsIdentityDb_{Guid.NewGuid():N}";
    private readonly string _inventoryDatabaseName = $"IntegrationTestsInventoryDb_{Guid.NewGuid():N}";
    private readonly string _orderingDatabaseName = $"IntegrationTestsOrderingDb_{Guid.NewGuid():N}";
    private readonly string _paymentsDatabaseName = $"IntegrationTestsPaymentsDb_{Guid.NewGuid():N}";
    private readonly string _reviewsDatabaseName = $"IntegrationTestsReviewsDb_{Guid.NewGuid():N}";
    private readonly string _shoppingDatabaseName = $"IntegrationTestsShoppingDb_{Guid.NewGuid():N}";
    private readonly string _promotionsDatabaseName = $"IntegrationTestsPromotionsDb_{Guid.NewGuid():N}";
    private PostgreSqlContainer? _catalogPostgresContainer;
    private string? _catalogPostgresConnectionString;
    private readonly object _catalogContainerSync = new();
    private PostgreSqlContainer? _reviewsPostgresContainer;
    private string? _reviewsPostgresConnectionString;
    private readonly object _reviewsContainerSync = new();
    private PostgreSqlContainer? _promotionsPostgresContainer;
    private string? _promotionsPostgresConnectionString;
    private readonly object _promotionsContainerSync = new();
    private static readonly string _testPasswordHash = BCrypt.Net.BCrypt.HashPassword("TestPassword123!");
    private static readonly Guid _seededPromoCodeId = Guid.Parse("55555555-5555-5555-5555-555555555555");
    private static readonly string _defaultConnectionString =
        Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection")
        ?? "Host=localhost;Database=ECommerceDb;Username=ecommerce;Password=local-dev-password-123";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        var catalogPostgresConnectionString = _useCatalogPostgresContainer
            ? TryGetCatalogPostgresConnectionString()
            : null;
        var reviewsPostgresConnectionString = _useReviewsPostgresContainer
            ? TryGetReviewsPostgresConnectionString()
            : null;
        var promotionsPostgresConnectionString = _usePromotionsPostgresContainer
            ? TryGetPromotionsPostgresConnectionString()
            : null;

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
        builder.UseSetting(
            "ConnectionStrings:CatalogConnection",
            _useCatalogPostgresContainer
                ? catalogPostgresConnectionString!
                : _defaultConnectionString);
        builder.UseSetting(
            "ConnectionStrings:ReviewsConnection",
            _useReviewsPostgresContainer
                ? reviewsPostgresConnectionString!
                : _defaultConnectionString);
        builder.UseSetting(
            "ConnectionStrings:PromotionsConnection",
            _usePromotionsPostgresContainer
                ? promotionsPostgresConnectionString!
                : _defaultConnectionString);
        builder.UseSetting("Serilog:MinimumLevel:Default", "Debug");
        builder.UseSetting("RateLimiting:GlobalLimit", "100000");
        builder.UseSetting("RateLimiting:AuthLimit", "100000");
        builder.UseSetting("RateLimiting:PasswordResetLimit", "100000");

        builder.ConfigureTestServices(services =>
        {
            // Replace BC DbContexts with InMemory DB
            var reviewsDescriptor = services.SingleOrDefault(d => d.ServiceType == typeof(DbContextOptions<ReviewsDbContext>));
            if (reviewsDescriptor != null) services.Remove(reviewsDescriptor);

            var catalogDescriptor = services.SingleOrDefault(d => d.ServiceType == typeof(DbContextOptions<CatalogDbContext>));
            if (catalogDescriptor != null) services.Remove(catalogDescriptor);

            var integrationDescriptor = services.SingleOrDefault(d => d.ServiceType == typeof(DbContextOptions<IntegrationPersistenceDbContext>));
            if (integrationDescriptor != null) services.Remove(integrationDescriptor);

            var identityDescriptor = services.SingleOrDefault(d => d.ServiceType == typeof(DbContextOptions<IdentityDbContext>));
            if (identityDescriptor != null) services.Remove(identityDescriptor);

            var inventoryDescriptor = services.SingleOrDefault(d => d.ServiceType == typeof(DbContextOptions<InventoryDbContext>));
            if (inventoryDescriptor != null) services.Remove(inventoryDescriptor);

            var orderingDescriptor = services.SingleOrDefault(d => d.ServiceType == typeof(DbContextOptions<OrderingDbContext>));
            if (orderingDescriptor != null) services.Remove(orderingDescriptor);

            var paymentsDescriptor = services.SingleOrDefault(d => d.ServiceType == typeof(DbContextOptions<PaymentsDbContext>));
            if (paymentsDescriptor != null) services.Remove(paymentsDescriptor);

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

            services.RemoveAll<ECommerce.Inventory.Application.Interfaces.IInventoryProjectionEventPublisher>();
            services.AddScoped<ECommerce.Inventory.Application.Interfaces.IInventoryProjectionEventPublisher, NoOpInventoryProjectionEventPublisher>();

            // Use a separate internal service provider for EF InMemory to avoid multiple provider registrations
            var inMemoryServiceProvider = new ServiceCollection()
                .AddEntityFrameworkInMemoryDatabase()
                .BuildServiceProvider();

            if (_useReviewsPostgresContainer)
            {
                services.AddDbContext<ReviewsDbContext>(options =>
                {
                    options.UseNpgsql(reviewsPostgresConnectionString!);
                });
            }
            else
            {
                services.AddDbContext<ReviewsDbContext>(options =>
                {
                    options.UseInMemoryDatabase(_reviewsDatabaseName);
                    options.UseInternalServiceProvider(inMemoryServiceProvider);
                    options.ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning));
                });
            }

            if (_useCatalogPostgresContainer)
            {
                services.AddDbContext<CatalogDbContext>(options =>
                {
                    options.UseNpgsql(catalogPostgresConnectionString!);
                });
            }
            else
            {
                services.AddDbContext<CatalogDbContext>(options =>
                {
                    options.UseInMemoryDatabase(_catalogDatabaseName);
                    options.UseInternalServiceProvider(inMemoryServiceProvider);
                    options.ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning));
                });
            }

            services.AddDbContext<IntegrationPersistenceDbContext>(options =>
            {
                options.UseInMemoryDatabase(_integrationDatabaseName);
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

            services.AddDbContext<PaymentsDbContext>(options =>
            {
                options.UseInMemoryDatabase(_paymentsDatabaseName);
                options.UseInternalServiceProvider(inMemoryServiceProvider);
                options.ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning));
            });

            if (_usePromotionsPostgresContainer)
            {
                services.AddDbContext<PromotionsDbContext>(options =>
                {
                    options.UseNpgsql(promotionsPostgresConnectionString!);
                });
            }
            else
            {
                services.AddDbContext<PromotionsDbContext>(options =>
                {
                    options.UseInMemoryDatabase(_promotionsDatabaseName);
                    options.UseInternalServiceProvider(inMemoryServiceProvider);
                    options.ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning));
                });
            }

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
                var reviewsDb = scopedServices.GetRequiredService<ReviewsDbContext>();
                if (_useReviewsPostgresContainer)
                {
                    reviewsDb.Database.EnsureCreated();
                }
                else
                {
                    reviewsDb.Database.EnsureCreated();
                }

                var catalogDb = scopedServices.GetRequiredService<CatalogDbContext>();
                if (_useCatalogPostgresContainer)
                {
                    catalogDb.Database.Migrate();
                }
                else
                {
                    catalogDb.Database.EnsureCreated();
                }

                var integrationDb = scopedServices.GetRequiredService<IntegrationPersistenceDbContext>();
                integrationDb.Database.EnsureCreated();

                var identityDb = scopedServices.GetRequiredService<IdentityDbContext>();
                identityDb.Database.EnsureCreated();

                var inventoryDb = scopedServices.GetRequiredService<InventoryDbContext>();
                inventoryDb.Database.EnsureCreated();

                var orderingDb = scopedServices.GetRequiredService<OrderingDbContext>();
                orderingDb.Database.EnsureCreated();

                var paymentsDb = scopedServices.GetRequiredService<PaymentsDbContext>();
                paymentsDb.Database.EnsureCreated();

                var promotionsDb = scopedServices.GetRequiredService<PromotionsDbContext>();
                if (_usePromotionsPostgresContainer)
                {
                    promotionsDb.Database.Migrate();
                }
                else
                {
                    promotionsDb.Database.EnsureCreated();
                }

                var shoppingDb = scopedServices.GetRequiredService<ShoppingDbContext>();
                shoppingDb.Database.EnsureCreated();

                // Precomputed once per process to avoid repeated hash cost on startup
                string passwordHash = _testPasswordHash;

                var userId = Guid.Parse(ConditionalTestAuthHandler.TestUserId);
                var adminId = Guid.Parse(ConditionalTestAuthHandler.TestAdminUserId);

                identityDb.Users.Add(CreateSeedIdentityUser(
                    userId,
                    "integration@test.com",
                    "Integration",
                    "User",
                    SharedKernel.Enums.UserRole.Customer,
                    passwordHash));

                identityDb.Users.Add(CreateSeedIdentityUser(
                    adminId,
                    "admin@test.com",
                    "Admin",
                    "User",
                    SharedKernel.Enums.UserRole.Admin,
                    passwordHash));

                var categoryId = Guid.Parse("66666666-6666-6666-6666-666666666666");
                var productId = Guid.Parse("22222222-2222-2222-2222-222222222222");
                var inventoryResultForInventoryDb = InventoryItem.Create(productId, 100, 10);
                if (inventoryResultForInventoryDb.IsSuccess)
                {
                    inventoryDb.InventoryItems.Add(inventoryResultForInventoryDb.GetDataOrThrow());
                }

                var categoryResult = CatalogCategory.Create("Test Category", null, "test-category");
                if (categoryResult.IsSuccess)
                {
                    var category = categoryResult.GetDataOrThrow();
                    SetEntityId(category, categoryId);
                    catalogDb.Categories.Add(category);
                }

                var productResult = CatalogProduct.Create(
                    "IntegrationProduct",
                    10.0m,
                    "USD",
                    categoryId,
                    "TEST-SKU-001",
                    "integration-product");
                if (productResult.IsSuccess)
                {
                    var product = productResult.GetDataOrThrow();
                    SetEntityId(product, productId);
                    product.SetStock(100);
                    product.Activate();
                    catalogDb.Products.Add(product);
                }

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
                var testAddress = OrderingShippingAddress.Create("123 Test St", "Test City", "US", "12345");
                var testItems = new List<OrderingOrderItemData>
                {
                    new(productId, "IntegrationProduct", 10.00m, 2, null)
                };
                var pendingOrderResult = OrderingOrder.Place(userId, testAddress, testItems, 10.00m, 0m, "PAY-REF-TEST", "Card");
                if (pendingOrderResult.IsSuccess)
                {
                    var pendingOrder = pendingOrderResult.GetDataOrThrow();
                    SetEntityId(pendingOrder, testOrderId);
                    orderingDb.Orders.Add(pendingOrder);
                }

                // Seed a shipped order so cancel-shipped tests can find it
                var shippedOrderId = Guid.Parse("55555555-5555-5555-5555-555555555555");
                var shippedOrderResult = OrderingOrder.Place(userId, testAddress, testItems, 10.00m, 0m, "PAY-REF-SHIPPED", "Card");
                if (shippedOrderResult.IsSuccess)
                {
                    var shippedOrder = shippedOrderResult.GetDataOrThrow();
                    SetEntityId(shippedOrder, shippedOrderId);
                    // Bypass state machine with reflection (test seeding only)
                    var statusProp = typeof(OrderingOrder).GetProperty("Status", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
                    statusProp?.SetValue(shippedOrder, OrderingOrderStatus.Shipped);
                    orderingDb.Orders.Add(shippedOrder);
                }

                var promoCodeResult = ECommerce.Promotions.Domain.ValueObjects.PromoCodeString.Create("SAVE20");
                var discountValueResult = ECommerce.Promotions.Domain.ValueObjects.DiscountValue.Percentage(20);
                var validPeriodResult = ECommerce.Promotions.Domain.ValueObjects.DateRange.Create(
                    DateTime.UtcNow.AddYears(-1), DateTime.UtcNow.AddYears(10));
                if (promoCodeResult.IsSuccess && discountValueResult.IsSuccess && validPeriodResult.IsSuccess)
                {
                    var promo = ECommerce.Promotions.Domain.Aggregates.PromoCode.PromoCode.Create(
                        promoCodeResult.GetDataOrThrow(),
                        discountValueResult.GetDataOrThrow(),
                        validPeriodResult.GetDataOrThrow());
                    SetEntityId(promo, _seededPromoCodeId);
                    promotionsDb.PromoCodes.Add(promo);
                }

                shoppingDb.Products.Add(new ECommerce.Shopping.Infrastructure.Persistence.ProductReadModel
                {
                    Id = productId,
                    IsActive = true,
                    Price = 10.0m
                });

                reviewsDb.Products.Add(new ECommerce.Reviews.Infrastructure.Persistence.ProductReadModel
                {
                    Id = productId,
                    IsActive = true,
                    UpdatedAt = DateTime.UtcNow
                });

                shoppingDb.InventoryItems.Add(new InventoryItemReadModel
                {
                    ProductId = productId,
                    Quantity = 100,
                    UpdatedAt = DateTime.UtcNow
                });

                var orderId = Guid.Parse(ConditionalTestAuthHandler.TestOrderId);
                paymentsDb.PaymentOrders.Add(new PaymentOrderReadModel
                {
                    Id = Guid.NewGuid(),
                    OrderId = orderId,
                    Amount = 100.00m,
                    UserId = userId,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                });

                catalogDb.SaveChanges();
                identityDb.SaveChanges();
                inventoryDb.SaveChanges();
                orderingDb.SaveChanges();
                paymentsDb.SaveChanges();
                promotionsDb.SaveChanges();
                shoppingDb.SaveChanges();
                reviewsDb.SaveChanges();
            }

            // Reset auth to enabled by default
            ConditionalTestAuthHandler.IsAuthenticationEnabled = true;
        });
    }

    protected override void Dispose(bool disposing)
    {
        StopCatalogContainerAsync().GetAwaiter().GetResult();
        StopReviewsContainerAsync().GetAwaiter().GetResult();
        StopPromotionsContainerAsync().GetAwaiter().GetResult();
        base.Dispose(disposing);
    }

    public override async ValueTask DisposeAsync()
    {
        await StopCatalogContainerAsync();
        await StopReviewsContainerAsync();
        await StopPromotionsContainerAsync();
        GC.SuppressFinalize(this);
        await base.DisposeAsync();
    }

    private string TryGetCatalogPostgresConnectionString()
    {
        if (_catalogPostgresConnectionString is not null)
            return _catalogPostgresConnectionString;

        lock (_catalogContainerSync)
        {
            if (_catalogPostgresConnectionString is not null)
                return _catalogPostgresConnectionString;

            try
            {
                _catalogPostgresContainer = new PostgreSqlBuilder()
                    .WithImage("postgres:16-alpine")
                    .WithDatabase(_catalogDatabaseName.ToLowerInvariant())
                    .WithUsername("postgres")
                    .WithPassword("postgres")
                    .WithWaitStrategy(Wait.ForUnixContainer().UntilInternalTcpPortIsAvailable(5432))
                    .WithCleanUp(true)
                    .Build();

                _catalogPostgresContainer.StartAsync().GetAwaiter().GetResult();
                _catalogPostgresConnectionString = _catalogPostgresContainer.GetConnectionString();
                return _catalogPostgresConnectionString;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException(
                    "Catalog PostgreSQL Testcontainer failed to start. " +
                    "Verify Docker is running and accessible.",
                    ex);
            }
        }
    }

    private async Task StopCatalogContainerAsync()
    {
        if (_catalogPostgresContainer is null)
            return;

        await _catalogPostgresContainer.DisposeAsync();
        _catalogPostgresContainer = null;
        _catalogPostgresConnectionString = null;
    }

    private string TryGetReviewsPostgresConnectionString()
    {
        if (_reviewsPostgresConnectionString is not null)
            return _reviewsPostgresConnectionString;

        lock (_reviewsContainerSync)
        {
            if (_reviewsPostgresConnectionString is not null)
                return _reviewsPostgresConnectionString;

            try
            {
                _reviewsPostgresContainer = new PostgreSqlBuilder()
                    .WithImage("postgres:16-alpine")
                    .WithDatabase(_reviewsDatabaseName.ToLowerInvariant())
                    .WithUsername("postgres")
                    .WithPassword("postgres")
                    .WithWaitStrategy(Wait.ForUnixContainer().UntilInternalTcpPortIsAvailable(5432))
                    .WithCleanUp(true)
                    .Build();

                _reviewsPostgresContainer.StartAsync().GetAwaiter().GetResult();
                _reviewsPostgresConnectionString = _reviewsPostgresContainer.GetConnectionString();
                return _reviewsPostgresConnectionString;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException(
                    "Reviews PostgreSQL Testcontainer failed to start. " +
                    "Verify Docker is running and accessible.",
                    ex);
            }
        }
    }

    private async Task StopReviewsContainerAsync()
    {
        if (_reviewsPostgresContainer is null)
            return;

        await _reviewsPostgresContainer.DisposeAsync();
        _reviewsPostgresContainer = null;
        _reviewsPostgresConnectionString = null;
    }

    private string TryGetPromotionsPostgresConnectionString()
    {
        if (_promotionsPostgresConnectionString is not null)
            return _promotionsPostgresConnectionString;

        lock (_promotionsContainerSync)
        {
            if (_promotionsPostgresConnectionString is not null)
                return _promotionsPostgresConnectionString;

            try
            {
                _promotionsPostgresContainer = new PostgreSqlBuilder()
                    .WithImage("postgres:16-alpine")
                    .WithDatabase(_promotionsDatabaseName.ToLowerInvariant())
                    .WithUsername("postgres")
                    .WithPassword("postgres")
                    .WithWaitStrategy(Wait.ForUnixContainer().UntilInternalTcpPortIsAvailable(5432))
                    .WithCleanUp(true)
                    .Build();

                _promotionsPostgresContainer.StartAsync().GetAwaiter().GetResult();
                _promotionsPostgresConnectionString = _promotionsPostgresContainer.GetConnectionString();
                return _promotionsPostgresConnectionString;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException(
                    "Promotions PostgreSQL Testcontainer failed to start. " +
                    "Verify Docker is running and accessible.",
                    ex);
            }
        }
    }

    private async Task StopPromotionsContainerAsync()
    {
        if (_promotionsPostgresContainer is null)
            return;

        await _promotionsPostgresContainer.DisposeAsync();
        _promotionsPostgresContainer = null;
        _promotionsPostgresConnectionString = null;
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

    public HttpClient CreateAuthenticatedClientNoRedirect()
    {
        var client = CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
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

    public HttpClient CreateAdminClientNoRedirect()
    {
        var client = CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
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

    public HttpClient CreateUnauthenticatedClientNoRedirect()
    {
        var client = CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
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
        public Task SendOrderConfirmationEmailAsync(string email, OrderEmailDto order, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task SendOrderShippedEmailAsync(string email, OrderEmailDto order, string trackingNumber, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task SendOrderDeliveredEmailAsync(string email, OrderEmailDto order, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task SendAbandonedCartEmailAsync(string email, string firstName, CartEmailDto cart, CancellationToken cancellationToken = default) => Task.CompletedTask;
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

    private static void SetEntityId(object entity, Guid id)
    {
        var property = entity.GetType().BaseType?.GetProperty("Id") ?? entity.GetType().GetProperty("Id");
        property?.SetValue(entity, id);
    }

    private sealed class PrecomputedHasher(string hash) : IPasswordHasher
    {
        public string Hash(string _) => hash;
        public bool Verify(string raw, string h) => BCrypt.Net.BCrypt.Verify(raw, h);
    }

    private static IdentityUser CreateSeedIdentityUser(
        Guid id,
        string email,
        string firstName,
        string lastName,
        SharedKernel.Enums.UserRole role,
        string passwordHash)
    {
        var user = IdentityUser.Register(email, firstName, lastName, "TestPassword123!", new PrecomputedHasher(passwordHash)).GetDataOrThrow();

        SetEntityId(user, id);

        if (!string.IsNullOrWhiteSpace(user.EmailVerificationToken))
            user.VerifyEmail(user.EmailVerificationToken);

        // Tests seed both customer and admin identities.
        var roleProperty = user.GetType().GetProperty(nameof(IdentityUser.Role));
        roleProperty?.SetValue(user, role);

        return user;
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

            total += await SaveIfAvailableAsync<IntegrationPersistenceDbContext>(cancellationToken);
            total += await SaveIfAvailableAsync<IdentityDbContext>(cancellationToken);
            total += await SaveIfAvailableAsync<ReviewsDbContext>(cancellationToken);
            total += await SaveIfAvailableAsync<CatalogDbContext>(cancellationToken);
            total += await SaveIfAvailableAsync<InventoryDbContext>(cancellationToken);
            total += await SaveIfAvailableAsync<OrderingDbContext>(cancellationToken);
            total += await SaveIfAvailableAsync<PaymentsDbContext>(cancellationToken);
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

