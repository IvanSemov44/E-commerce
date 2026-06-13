using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.IdentityModel.Tokens;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using ECommerce.SharedKernel.DTOs;
using ECommerce.SharedKernel.Interfaces;
using CatalogCategory = ECommerce.Catalog.Domain.Aggregates.Category.Category;
using CatalogProduct = ECommerce.Catalog.Domain.Aggregates.Product.Product;
using IInventoryProjectionEventPublisher = ECommerce.Inventory.Application.Interfaces.IInventoryProjectionEventPublisher;
using ECommerce.Payments.Application.Interfaces;
using ECommerce.Catalog.Infrastructure.Persistence;
using ECommerce.Inventory.Domain.Aggregates.InventoryItem;
using ECommerce.Inventory.Infrastructure.Persistence;
using ECommerce.Reviews.Infrastructure.Persistence;
using ECommerce.Identity.Infrastructure.Persistence;
using ECommerce.Ordering.Infrastructure.Persistence;
using ECommerce.Payments.Infrastructure.Persistence;
using ECommerce.Shopping.Infrastructure.Persistence;
using ECommerce.Promotions.Infrastructure.Persistence;
using ECommerce.API.Services;
using ECommerce.Identity.Domain.Interfaces;
using IdentityUser             = ECommerce.Identity.Domain.Aggregates.User.User;
using OrderingProductReadModel = ECommerce.Ordering.Infrastructure.Persistence.ProductReadModel;
using ShoppingProductReadModel = ECommerce.Shopping.Infrastructure.Persistence.ProductReadModel;
using ReviewsProductReadModel  = ECommerce.Reviews.Infrastructure.Persistence.ProductReadModel;
using PromotionsPromoCode      = ECommerce.Promotions.Domain.Aggregates.PromoCode.PromoCode;
using PromotionsPromoCodeString = ECommerce.Promotions.Domain.ValueObjects.PromoCodeString;
using PromotionsDiscountValue  = ECommerce.Promotions.Domain.ValueObjects.DiscountValue;
using PromotionsDateRange      = ECommerce.Promotions.Domain.ValueObjects.DateRange;
using Microsoft.Extensions.Hosting;
using DotNet.Testcontainers.Builders;
using Testcontainers.PostgreSql;
using Npgsql;

namespace ECommerce.Tests.Integration;

public class TestWebApplicationFactory : WebApplicationFactory<Program>
{
    public const string TestUserId = "11111111-1111-1111-1111-111111111111";
    public const string TestAdminUserId = "33333333-3333-3333-3333-333333333333";

    // ── Per-instance database names ───────────────────────────────────────────
    private readonly string _catalogDatabaseName    = $"testcatalog_{Guid.NewGuid():N}";
    private readonly string _identityDatabaseName   = $"testidentity_{Guid.NewGuid():N}";
    private readonly string _inventoryDatabaseName  = $"testinventory_{Guid.NewGuid():N}";
    private readonly string _orderingDatabaseName   = $"testordering_{Guid.NewGuid():N}";
    private readonly string _paymentsDatabaseName   = $"testpayments_{Guid.NewGuid():N}";
    private readonly string _reviewsDatabaseName    = $"testreviews_{Guid.NewGuid():N}";
    private readonly string _shoppingDatabaseName   = $"testshopping_{Guid.NewGuid():N}";
    private readonly string _promotionsDatabaseName = $"testpromotions_{Guid.NewGuid():N}";

    // ── Postgres container helpers (start lazily on first use) ────────────────
    private readonly PostgresTestContainer _catalogContainer;
    private readonly PostgresTestContainer _identityContainer;
    private readonly PostgresTestContainer _inventoryContainer;
    private readonly PostgresTestContainer _orderingContainer;
    private readonly PostgresTestContainer _paymentsContainer;
    private readonly PostgresTestContainer _reviewsContainer;
    private readonly PostgresTestContainer _shoppingContainer;
    private readonly PostgresTestContainer _promotionsContainer;

    private static readonly string _testPasswordHash = BCrypt.Net.BCrypt.HashPassword("TestPassword123!");
    private static readonly Guid _seededPromoCodeId = Guid.Parse("55555555-5555-5555-5555-555555555555");

    public TestWebApplicationFactory()
    {
        _catalogContainer    = new PostgresTestContainer(_catalogDatabaseName,    "Catalog");
        _identityContainer   = new PostgresTestContainer(_identityDatabaseName,   "Identity");
        _inventoryContainer  = new PostgresTestContainer(_inventoryDatabaseName,  "Inventory");
        _orderingContainer   = new PostgresTestContainer(_orderingDatabaseName,   "Ordering");
        _paymentsContainer   = new PostgresTestContainer(_paymentsDatabaseName,   "Payments");
        _reviewsContainer    = new PostgresTestContainer(_reviewsDatabaseName,    "Reviews");
        _shoppingContainer   = new PostgresTestContainer(_shoppingDatabaseName,   "Shopping");
        _promotionsContainer = new PostgresTestContainer(_promotionsDatabaseName, "Promotions");
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Test");
        builder.UseSetting("Jwt:SecretKey", "SuperSecretKeyForTestingPurposesOnlyThatIsLongEnough");
        builder.UseSetting("Jwt:Issuer", "test");
        builder.UseSetting("Jwt:Audience", "test");
        builder.UseSetting("ConnectionStrings:DefaultConnection",    _orderingContainer.GetConnectionString());
        builder.UseSetting("ConnectionStrings:DataProtectionConnection", _identityContainer.GetConnectionString());
        builder.UseSetting("ConnectionStrings:CatalogConnection",    _catalogContainer.GetConnectionString());
        builder.UseSetting("ConnectionStrings:ReviewsConnection",    _reviewsContainer.GetConnectionString());
        builder.UseSetting("ConnectionStrings:PromotionsConnection", _promotionsContainer.GetConnectionString());
        builder.UseSetting("DataProtection:UseDatabaseStorage", "false");
        builder.UseSetting("Serilog:MinimumLevel:Default", "Warning");
        builder.UseSetting("RateLimiting:GlobalLimit", "100000");
        builder.UseSetting("RateLimiting:AuthLimit", "100000");
        builder.UseSetting("RateLimiting:PasswordResetLimit", "100000");

        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll(typeof(DbContextOptions<CatalogDbContext>));
            services.RemoveAll(typeof(DbContextOptions<IdentityDbContext>));
            services.RemoveAll(typeof(DbContextOptions<InventoryDbContext>));
            services.RemoveAll(typeof(DbContextOptions<OrderingDbContext>));
            services.RemoveAll(typeof(DbContextOptions<PaymentsDbContext>));
            services.RemoveAll(typeof(DbContextOptions<ReviewsDbContext>));
            services.RemoveAll(typeof(DbContextOptions<ShoppingDbContext>));
            services.RemoveAll(typeof(DbContextOptions<PromotionsDbContext>));
            services.RemoveAll(typeof(DbContextOptions<DataProtectionKeysContext>));

            services.AddDbContext<CatalogDbContext>(o    => o.UseNpgsql(_catalogContainer.GetConnectionString()));
            services.AddDbContext<IdentityDbContext>(o   => o.UseNpgsql(_identityContainer.GetConnectionString()));
            services.AddDbContext<InventoryDbContext>(o  => o.UseNpgsql(_inventoryContainer.GetConnectionString()));
            services.AddDbContext<OrderingDbContext>(o   => o.UseNpgsql(_orderingContainer.GetConnectionString()));
            services.AddDbContext<PaymentsDbContext>(o   => o.UseNpgsql(_paymentsContainer.GetConnectionString()));
            services.AddDbContext<ReviewsDbContext>(o    => o.UseNpgsql(_reviewsContainer.GetConnectionString()));
            services.AddDbContext<ShoppingDbContext>(o   => o.UseNpgsql(_shoppingContainer.GetConnectionString()));
            services.AddDbContext<PromotionsDbContext>(o => o.UseNpgsql(_promotionsContainer.GetConnectionString()));
            services.AddDbContext<DataProtectionKeysContext>(o => o.UseNpgsql(_identityContainer.GetConnectionString()));

            services.RemoveAll(typeof(IEmailService));
            services.AddScoped<IEmailService, NoOpEmailService>();

            services.RemoveAll(typeof(IDistributedCache));
            services.AddDistributedMemoryCache();

            services.RemoveAll(typeof(IWebhookVerificationService));
            services.AddScoped<IWebhookVerificationService, TestWebhookVerificationService>();

            services.RemoveAll<IInventoryProjectionEventPublisher>();
            services.AddScoped<IInventoryProjectionEventPublisher, NoOpInventoryProjectionEventPublisher>();

            var outboxHostedServices = services
                .Where(d => d.ServiceType == typeof(IHostedService)
                    && d.ImplementationType?.Name.Contains("OutboxDispatcherHostedService", StringComparison.OrdinalIgnoreCase) == true)
                .ToList();

            foreach (var hostedService in outboxHostedServices)
                services.Remove(hostedService);

            using var sp = services.BuildServiceProvider();
            using var scope = sp.CreateScope();
            SeedTestData(scope.ServiceProvider);
        });
    }

    protected override void Dispose(bool disposing)
    {
        foreach (var c in AllContainers()) c.StopAsync().GetAwaiter().GetResult();
        base.Dispose(disposing);
    }

    public override async ValueTask DisposeAsync()
    {
        foreach (var c in AllContainers()) await c.StopAsync();
        GC.SuppressFinalize(this);
        await base.DisposeAsync();
    }

    private IEnumerable<PostgresTestContainer> AllContainers() =>
        [_catalogContainer, _identityContainer, _inventoryContainer, _orderingContainer,
         _paymentsContainer, _reviewsContainer, _shoppingContainer, _promotionsContainer];

    // ── Public helpers ────────────────────────────────────────────────────────

    public static string GenerateJwtToken(string userId = "", params string[] roles)
    {
        userId = string.IsNullOrEmpty(userId) ? TestUserId : userId;

        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, userId),
            new Claim(ClaimTypes.Name,  roles.Contains("Admin") ? "admin@test.com" : "integration@test.com"),
            new Claim(ClaimTypes.Email, roles.Contains("Admin") ? "admin@test.com" : "integration@test.com")
        };

        foreach (var role in roles)
            claims.Add(new Claim(ClaimTypes.Role, role));

        if (roles.Length == 0)
            claims.Add(new Claim(ClaimTypes.Role, "Customer"));

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

    public HttpClient CreateAuthenticatedClient()
    {
        var client = CreateClient();
        var token = GenerateJwtToken(TestUserId, "Customer");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        client.DefaultRequestHeaders.Remove("Idempotency-Key");
        client.DefaultRequestHeaders.Add("Idempotency-Key", Guid.NewGuid().ToString());
        return client;
    }

    public HttpClient CreateAuthenticatedClientNoRedirect()
    {
        var client = CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        var token = GenerateJwtToken(TestUserId, "Customer");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        client.DefaultRequestHeaders.Remove("Idempotency-Key");
        client.DefaultRequestHeaders.Add("Idempotency-Key", Guid.NewGuid().ToString());
        return client;
    }

    public HttpClient CreateAdminClient()
    {
        var client = CreateClient();
        var token = GenerateJwtToken(TestAdminUserId, "Admin");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        client.DefaultRequestHeaders.Remove("Idempotency-Key");
        client.DefaultRequestHeaders.Add("Idempotency-Key", Guid.NewGuid().ToString());
        return client;
    }

    public HttpClient CreateAdminClientNoRedirect()
    {
        var client = CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        var token = GenerateJwtToken(TestAdminUserId, "Admin");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        client.DefaultRequestHeaders.Remove("Idempotency-Key");
        client.DefaultRequestHeaders.Add("Idempotency-Key", Guid.NewGuid().ToString());
        return client;
    }

    public HttpClient CreateUnauthenticatedClient()
    {
        var client = CreateClient();
        client.DefaultRequestHeaders.Remove("Idempotency-Key");
        client.DefaultRequestHeaders.Add("Idempotency-Key", Guid.NewGuid().ToString());
        return client;
    }

    /// <summary>
    /// Creates an authenticated client using a brand-new random userId.
    /// Use this in cart/wishlist/review tests so parallel tests don't share user state.
    /// </summary>
    public HttpClient CreateFreshAuthenticatedClient()
    {
        var client = CreateClient();
        var token = GenerateJwtToken(Guid.NewGuid().ToString(), "Customer");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        client.DefaultRequestHeaders.Add("Idempotency-Key", Guid.NewGuid().ToString());
        return client;
    }

    /// <summary>
    /// Places an order via the API using the seeded product and address, returns the new order id.
    /// Also seeds a PaymentOrderReadModel in the payments DB because the outbox is disabled in tests.
    /// Throws if the order cannot be created so the calling test fails with a clear message.
    /// </summary>
    public async Task<Guid> PlaceOrderAsync(HttpClient client)
    {
        var body = new StringContent(
            System.Text.Json.JsonSerializer.Serialize(new
            {
                PaymentMethod = "credit_card",
                ShippingAddress = new
                {
                    Id = "77777777-7777-7777-7777-777777777777",
                    FirstName = "Test", LastName = "User", Phone = "555-0101",
                    StreetLine1 = "123 Test St", City = "New York",
                    State = "NY", PostalCode = "10001", Country = "US"
                },
                Items = new[] { new { ProductId = "22222222-2222-2222-2222-222222222222", Quantity = 1 } }
            }),
            Encoding.UTF8, "application/json");

        var response = await client.PostAsync("/api/orders", body);
        if (!response.IsSuccessStatusCode)
            throw new InvalidOperationException(
                $"PlaceOrderAsync failed: {response.StatusCode} — {await response.Content.ReadAsStringAsync()}");

        var json = System.Text.Json.JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var orderId = json.RootElement.GetProperty("data").GetGuid();

        // The outbox dispatcher is removed in tests, so the OrderPlaced integration event never
        // reaches the payments service. Seed the PaymentOrderReadModel directly so payment
        // tests can process payments against this order.
        var userId = ExtractUserIdFromClient(client);
        using var scope = Services.CreateScope();
        var paymentsDb = scope.ServiceProvider.GetRequiredService<PaymentsDbContext>();
        if (!paymentsDb.PaymentOrders.Any(x => x.OrderId == orderId))
        {
            paymentsDb.PaymentOrders.Add(new PaymentOrderReadModel
            {
                Id = Guid.NewGuid(),
                OrderId = orderId,
                Amount = 100.00m,
                UserId = userId,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            });
            await paymentsDb.SaveChangesAsync();
        }

        return orderId;
    }

    /// <summary>
    /// Processes a credit-card payment for the given order. Succeeds on OK or Conflict (already paid).
    /// </summary>
#pragma warning disable CA1822 // kept as instance method for API symmetry with PlaceOrderAsync
    public async Task ProcessPaymentAsync(HttpClient client, Guid orderId)
#pragma warning restore CA1822
    {
        var body = new StringContent(
            System.Text.Json.JsonSerializer.Serialize(new
            {
                OrderId = orderId,
                PaymentMethod = "credit_card",
                Amount = 100.00m,
                CardToken = "tok_visa"
            }),
            Encoding.UTF8, "application/json");

        var response = await client.PostAsync("/api/payments/process", body);
        if (response.StatusCode is not (System.Net.HttpStatusCode.OK or System.Net.HttpStatusCode.Conflict))
            throw new InvalidOperationException(
                $"ProcessPaymentAsync failed: {response.StatusCode} — {await response.Content.ReadAsStringAsync()}");
    }

    private static Guid ExtractUserIdFromClient(HttpClient client)
    {
        var authHeader = client.DefaultRequestHeaders.Authorization?.Parameter;
        if (authHeader is null) return Guid.Parse(TestUserId);
        try
        {
            var token = new JwtSecurityTokenHandler().ReadJwtToken(authHeader);
            var sub = token.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;
            return Guid.TryParse(sub, out var id) ? id : Guid.Parse(TestUserId);
        }
        catch
        {
            return Guid.Parse(TestUserId);
        }
    }

    public HttpClient CreateUnauthenticatedClientNoRedirect()
    {
        var client = CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        client.DefaultRequestHeaders.Remove("Idempotency-Key");
        client.DefaultRequestHeaders.Add("Idempotency-Key", Guid.NewGuid().ToString());
        return client;
    }

    // ── Private helpers ───────────────────────────────────────────────────────

    // WebApplicationFactory.EnsureServer() has no lock; parallel test classes can race to build
    // the host concurrently and call SeedTestData multiple times on the same fresh databases.
    // This guard ensures migrations and seeding run exactly once.
    private static readonly object _seedLock = new();
    private static volatile bool _seeded;

    private static void SeedTestData(IServiceProvider services)
    {
        lock (_seedLock)
        {
            if (_seeded) return;

        var reviewsDb = services.GetRequiredService<ReviewsDbContext>();
        var catalogDb = services.GetRequiredService<CatalogDbContext>();
        var identityDb = services.GetRequiredService<IdentityDbContext>();
        var inventoryDb = services.GetRequiredService<InventoryDbContext>();
        var orderingDb = services.GetRequiredService<OrderingDbContext>();
        var paymentsDb = services.GetRequiredService<PaymentsDbContext>();
        var promotionsDb = services.GetRequiredService<PromotionsDbContext>();
        var shoppingDb = services.GetRequiredService<ShoppingDbContext>();

        catalogDb.Database.Migrate();
        identityDb.Database.Migrate();
        inventoryDb.Database.Migrate();
        orderingDb.Database.Migrate();
        paymentsDb.Database.Migrate();
        reviewsDb.Database.Migrate();
        shoppingDb.Database.Migrate();
        promotionsDb.Database.Migrate();

        // Shopping migration maps RowVersion as non-null bytea without a database generator.
        // Set a deterministic default in test databases so cart inserts can succeed.
        shoppingDb.Database.ExecuteSqlRaw(@"ALTER TABLE shopping.""Carts"" ALTER COLUMN ""RowVersion"" SET DEFAULT decode('00', 'hex');");

        var userId = Guid.Parse(TestUserId);
        var adminId = Guid.Parse(TestAdminUserId);
        var categoryId = Guid.Parse("66666666-6666-6666-6666-666666666666");
        var productId = Guid.Parse("22222222-2222-2222-2222-222222222222");
        var orderingPromoCodeId = Guid.Parse("55555555-5555-5555-5555-555555555555");
        var orderingAddressId = Guid.Parse("77777777-7777-7777-7777-777777777777");

        if (!identityDb.Users.Any(x => x.Id == userId))
            identityDb.Users.Add(CreateSeedIdentityUser(userId, "integration@test.com", "Integration", "User", SharedKernel.Enums.UserRole.Customer, _testPasswordHash));

        if (!identityDb.Users.Any(x => x.Id == adminId))
            identityDb.Users.Add(CreateSeedIdentityUser(adminId, "admin@test.com", "Admin", "User", SharedKernel.Enums.UserRole.Admin, _testPasswordHash));

        var inventoryResult = InventoryItem.Create(productId, 100, 10);
        if (inventoryResult.IsSuccess && !inventoryDb.InventoryItems.Any(x => x.ProductId == productId))
            inventoryDb.InventoryItems.Add(inventoryResult.GetDataOrThrow());

        var categoryResult = CatalogCategory.Create("Test Category", null, "test-category");
        if (categoryResult.IsSuccess && !catalogDb.Categories.Any(x => x.Id == categoryId))
        {
            var category = categoryResult.GetDataOrThrow();
            SetEntityId(category, categoryId);
            catalogDb.Categories.Add(category);
        }

        var productResult = CatalogProduct.Create("IntegrationProduct", 10.0m, "USD", categoryId, "TEST-SKU-001", "integration-product");
        if (productResult.IsSuccess && !catalogDb.Products.Any(x => x.Id == productId))
        {
            var product = productResult.GetDataOrThrow();
            SetEntityId(product, productId);
            product.SetStock(100);
            product.Activate();
            catalogDb.Products.Add(product);
        }

        if (!orderingDb.Products.Any(x => x.Id == productId))
        {
            orderingDb.Products.Add(new OrderingProductReadModel
            {
                Id = productId,
                Name = "IntegrationProduct",
                Price = 10.0m,
                UpdatedAt = DateTime.UtcNow
            });
        }

        if (!orderingDb.PromoCodes.Any(x => x.Id == orderingPromoCodeId))
        {
            orderingDb.PromoCodes.Add(new PromoCodeReadModel
            {
                Id = orderingPromoCodeId,
                Code = "SAVE20",
                DiscountValue = 20m,
                IsActive = true,
                UpdatedAt = DateTime.UtcNow
            });
        }

        if (!orderingDb.Addresses.Any(x => x.Id == orderingAddressId))
        {
            orderingDb.Addresses.Add(new AddressReadModel
            {
                Id = orderingAddressId,
                UserId = userId,
                StreetLine1 = "123 Test St",
                City = "Testville",
                Country = "US",
                PostalCode = "12345",
                UpdatedAt = DateTime.UtcNow
            });
        }


        var promoCodeResult = PromotionsPromoCodeString.Create("SAVE20");
        var discountValueResult = PromotionsDiscountValue.Percentage(20);
        var validPeriodResult = PromotionsDateRange.Create(
            DateTime.UtcNow.AddYears(-1), DateTime.UtcNow.AddYears(10));
        if (promoCodeResult.IsSuccess && discountValueResult.IsSuccess && validPeriodResult.IsSuccess && !promotionsDb.PromoCodes.Any(x => x.Id == _seededPromoCodeId))
        {
            var promo = PromotionsPromoCode.Create(
                promoCodeResult.GetDataOrThrow(),
                discountValueResult.GetDataOrThrow(),
                validPeriodResult.GetDataOrThrow());
            SetEntityId(promo, _seededPromoCodeId);
            promotionsDb.PromoCodes.Add(promo);
        }

        if (!shoppingDb.Products.Any(x => x.Id == productId))
        {
            shoppingDb.Products.Add(new ShoppingProductReadModel
            {
                Id = productId,
                IsActive = true,
                Price = 10.0m
            });
        }

        if (!reviewsDb.Products.Any(x => x.Id == productId))
        {
            reviewsDb.Products.Add(new ReviewsProductReadModel
            {
                Id = productId,
                IsActive = true,
                UpdatedAt = DateTime.UtcNow
            });
        }

        if (!shoppingDb.InventoryItems.Any(x => x.ProductId == productId))
        {
            shoppingDb.InventoryItems.Add(new InventoryItemReadModel
            {
                ProductId = productId,
                Quantity = 100,
                UpdatedAt = DateTime.UtcNow
            });
        }

        orderingDb.SaveChanges();
        catalogDb.SaveChanges();
        identityDb.SaveChanges();
        inventoryDb.SaveChanges();
        paymentsDb.SaveChanges();
        promotionsDb.SaveChanges();
        shoppingDb.SaveChanges();
        reviewsDb.SaveChanges();

            _seeded = true;
        } // end lock
    }

    private static void SetEntityId(object entity, Guid id)
    {
        var property = entity.GetType().BaseType?.GetProperty("Id") ?? entity.GetType().GetProperty("Id");
        property?.SetValue(entity, id);
    }

    private static IdentityUser CreateSeedIdentityUser(
        Guid id, string email, string firstName, string lastName,
        SharedKernel.Enums.UserRole role, string passwordHash)
    {
        var user = IdentityUser.Register(email, firstName, lastName, "TestPassword123!", new PrecomputedHasher(passwordHash)).GetDataOrThrow();
        SetEntityId(user, id);
        if (!string.IsNullOrWhiteSpace(user.EmailVerificationToken))
            user.VerifyEmail(user.EmailVerificationToken);
        user.GetType().GetProperty(nameof(IdentityUser.Role))?.SetValue(user, role);
        return user;
    }

    // ── Inner classes ─────────────────────────────────────────────────────────

    private sealed class PostgresTestContainer(string databaseName, string serviceName)
    {
        private static readonly object _sharedSync = new();
        private static readonly Dictionary<string, PostgreSqlContainer> _sharedContainers = new(StringComparer.OrdinalIgnoreCase);

        private string? _connectionString;
        private readonly object _instanceSync = new();

        public string GetConnectionString()
        {
            if (_connectionString is not null) return _connectionString;
            lock (_instanceSync)
            {
                if (_connectionString is not null) return _connectionString;
                try
                {
                    var container = GetOrStartSharedContainer(serviceName);
                    var sharedConnectionString = container.GetConnectionString();

                    EnsureDatabaseExists(sharedConnectionString, databaseName);

                    var connectionStringBuilder = new NpgsqlConnectionStringBuilder(sharedConnectionString)
                    {
                        Database = databaseName
                    };

                    _connectionString = connectionStringBuilder.ConnectionString;
                    return _connectionString;
                }
                catch (Exception ex)
                {
                    throw new InvalidOperationException(
                        $"{serviceName} PostgreSQL Testcontainer failed to start. " +
                        "Verify Docker is running and accessible.", ex);
                }
            }
        }

        public async Task StopAsync()
        {
            _connectionString = null;
            await Task.CompletedTask;
        }

        private static PostgreSqlContainer GetOrStartSharedContainer(string serviceName)
        {
            lock (_sharedSync)
            {
                if (_sharedContainers.TryGetValue(serviceName, out var existingContainer))
                    return existingContainer;

                var container = new PostgreSqlBuilder()
                    .WithImage("postgres:16-alpine")
                    .WithDatabase("postgres")
                    .WithUsername("postgres")
                    .WithPassword("postgres")
                    .WithWaitStrategy(Wait.ForUnixContainer().UntilInternalTcpPortIsAvailable(5432))
                    .WithCleanUp(true)
                    .Build();

                container.StartAsync().GetAwaiter().GetResult();
                _sharedContainers[serviceName] = container;
                return container;
            }
        }

        private static void EnsureDatabaseExists(string sharedConnectionString, string databaseName)
        {
            var adminConnectionString = new NpgsqlConnectionStringBuilder(sharedConnectionString)
            {
                Database = "postgres"
            }.ConnectionString;

            using var connection = new NpgsqlConnection(adminConnectionString);
            connection.Open();

            var safeDatabaseName = databaseName.Replace("\"", "\"\"");
            using var command = connection.CreateCommand();
            command.CommandText = $"CREATE DATABASE \"{safeDatabaseName}\"";

            try
            {
                command.ExecuteNonQuery();
            }
            catch (PostgresException ex) when (ex.SqlState == "42P04")
            {
                // Database already exists from a retry path.
            }
        }
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

    private sealed class NoOpInventoryProjectionEventPublisher : IInventoryProjectionEventPublisher
    {
        public Task PublishStockProjectionUpdatedAsync(Guid productId, int quantity, string reason, CancellationToken cancellationToken = default) => Task.CompletedTask;
    }

    private sealed class PrecomputedHasher(string hash) : IPasswordHasher
    {
        public string Hash(string _) => hash;
        public bool Verify(string raw, string h) => BCrypt.Net.BCrypt.Verify(raw, h);
    }
}
