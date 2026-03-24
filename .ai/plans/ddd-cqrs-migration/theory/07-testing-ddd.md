# Testing Strategy for DDD & CQRS

**Read this after `05-api-layer.md`.**

DDD produces code that is easier to test than anemic service classes. Each layer has a distinct, clean testing strategy. This document shows you exactly what to test at each layer and what the tests look like.

---

## Three Test Categories

| Category | What It Tests | Speed | Infrastructure |
|----------|--------------|-------|----------------|
| **Domain unit tests** | Aggregates, value objects, domain events | Fast (ms) | None — pure C# |
| **Handler unit tests** | Command/query handlers | Fast (ms) | Mocked repos + UoW |
| **Integration tests** | Full request → DB → response | Slow (seconds) | Real DB, real HTTP |

Write tests from the bottom up. Domain tests first — they are the fastest, easiest, and most valuable.

---

## 1. Domain Unit Tests

### What to test

- **Aggregate factory methods**: valid input creates correctly, invalid input throws `DomainException`
- **Aggregate domain methods**: state changes, invariants enforced, events raised
- **Value object validation**: invalid inputs throw, valid inputs succeed

### What NOT to test

- Aggregate properties (don't test that `product.Name == "X"` after `product.Name = "X"`)
- EF Core mapping or configuration
- Infrastructure code

### Examples

```csharp
// ECommerce.Catalog.Domain.Tests/Aggregates/ProductTests.cs

public class ProductTests
{
    // ── Factory method ──────────────────────────────────────────

    [Fact]
    public void Create_WithValidData_ReturnsProduct()
    {
        var product = Product.Create(
            ProductName.Create("Test Shirt"),
            Money.Create(29.99m, "USD"),
            Sku.Create("SHIRT-001"),
            categoryId: Guid.NewGuid());

        Assert.NotEqual(Guid.Empty, product.Id);
        Assert.Equal("Test Shirt", product.Name.Value);
    }

    [Fact]
    public void Create_RaisesProductCreatedEvent()
    {
        var product = Product.Create(
            ProductName.Create("Test"),
            Money.Create(10m, "USD"),
            Sku.Create("T-001"),
            Guid.NewGuid());

        var events = product.DomainEvents;
        Assert.Single(events);
        Assert.IsType<ProductCreatedEvent>(events[0]);
    }

    // ── Invariants ──────────────────────────────────────────────

    [Fact]
    public void Create_WithNegativePrice_ThrowsDomainException()
    {
        Assert.Throws<CatalogDomainException>(() =>
            Product.Create(
                ProductName.Create("Test"),
                Money.Create(-1m, "USD"),  // ← invalid
                Sku.Create("T-001"),
                Guid.NewGuid()));
    }

    [Fact]
    public void AddImage_FirstImage_SetsPrimary()
    {
        var product = CreateValidProduct();

        product.AddImage("https://cdn.example.com/img.jpg", "Product photo");

        Assert.Single(product.Images);
        Assert.True(product.Images.First().IsPrimary);
    }

    [Fact]
    public void AddImage_SecondImage_DoesNotOverridePrimary()
    {
        var product = CreateValidProduct();
        product.AddImage("https://cdn.example.com/first.jpg", "First");

        product.AddImage("https://cdn.example.com/second.jpg", "Second");

        Assert.Equal(2, product.Images.Count);
        Assert.Single(product.Images.Where(i => i.IsPrimary));
    }

    [Fact]
    public void UpdatePrice_RaisesPriceChangedEvent()
    {
        var product = CreateValidProduct();

        product.UpdatePrice(Money.Create(49.99m, "USD"));

        var priceEvent = product.DomainEvents.OfType<ProductPriceChangedEvent>().Single();
        Assert.Equal(49.99m, priceEvent.NewPrice.Amount);
    }

    // ── Value objects ────────────────────────────────────────────

    [Theory]
    [InlineData("")]
    [InlineData("  ")]
    [InlineData(null)]
    public void ProductName_Empty_ThrowsDomainException(string? name)
    {
        Assert.Throws<CatalogDomainException>(() => ProductName.Create(name!));
    }

    [Fact]
    public void Money_NegativeAmount_ThrowsDomainException()
    {
        Assert.Throws<CatalogDomainException>(() => Money.Create(-0.01m, "USD"));
    }

    [Fact]
    public void Slug_Create_NormalizesToLowerKebab()
    {
        var slug = Slug.Create("My Product Name");
        Assert.Equal("my-product-name", slug.Value);
    }

    // ── Helpers ──────────────────────────────────────────────────

    private static Product CreateValidProduct() =>
        Product.Create(
            ProductName.Create("Test Product"),
            Money.Create(29.99m, "USD"),
            Sku.Create("TEST-001"),
            categoryId: Guid.NewGuid());
}
```

**Key point**: No mocks. No database. No HTTP. Pure C# method calls. These run in milliseconds.

---

## 2. Handler Unit Tests

### What to test

- The handler calls the correct repository method
- The handler calls the correct aggregate method
- The handler saves via UnitOfWork
- The handler returns the correct Result

### What NOT to test

- Domain invariants (that's the domain tests' job)
- The actual SQL executed
- EF Core behavior

```csharp
// ECommerce.Catalog.Application.Tests/Commands/CreateProductCommandHandlerTests.cs

public class CreateProductCommandHandlerTests
{
    private readonly Mock<IProductRepository> _repoMock = new();
    private readonly Mock<ICategoryRepository> _categoryRepoMock = new();
    private readonly Mock<IUnitOfWork> _uowMock = new();
    private readonly CreateProductCommandHandler _handler;

    public CreateProductCommandHandlerTests()
    {
        _handler = new CreateProductCommandHandler(
            _repoMock.Object,
            _categoryRepoMock.Object,
            _uowMock.Object);
    }

    [Fact]
    public async Task Handle_ValidCommand_ReturnsSuccess()
    {
        var categoryId = Guid.NewGuid();
        var category = Category.Create(CategoryName.Create("Electronics"), Slug.Create("electronics"));

        _categoryRepoMock
            .Setup(r => r.GetByIdAsync(categoryId, default))
            .ReturnsAsync(category);

        var command = new CreateProductCommand(
            Name: "Test Product",
            Price: 29.99m,
            Currency: "USD",
            Sku: "TEST-001",
            CategoryId: categoryId);

        var result = await _handler.Handle(command, default);

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task Handle_ValidCommand_AddsProductToRepository()
    {
        var categoryId = SetupValidCategory();

        var command = new CreateProductCommand("Test", 29.99m, "USD", "T-001", categoryId);
        await _handler.Handle(command, default);

        _repoMock.Verify(r => r.AddAsync(It.IsAny<Product>(), default), Times.Once);
    }

    [Fact]
    public async Task Handle_ValidCommand_SavesViaUnitOfWork()
    {
        var categoryId = SetupValidCategory();

        await _handler.Handle(new CreateProductCommand("Test", 29.99m, "USD", "T-001", categoryId), default);

        _uowMock.Verify(u => u.SaveChangesAsync(default), Times.Once);
    }

    [Fact]
    public async Task Handle_CategoryNotFound_ReturnsFailure()
    {
        _categoryRepoMock
            .Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), default))
            .ReturnsAsync((Category?)null);

        var command = new CreateProductCommand("Test", 29.99m, "USD", "T-001", Guid.NewGuid());
        var result = await _handler.Handle(command, default);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorCodes.Catalog.CategoryNotFound, result.ErrorCode);
    }
}
```

---

## 3. Integration Tests

Integration tests go all the way: HTTP request → controller → MediatR → handler → real database → response.

### Setup: WebApplicationFactory

```csharp
// ECommerce.API.IntegrationTests/CustomWebApplicationFactory.cs

public class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            // Replace real DB with test DB
            var descriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(DbContextOptions<AppDbContext>));
            if (descriptor != null)
                services.Remove(descriptor);

            services.AddDbContext<AppDbContext>(options =>
                options.UseNpgsql(TestDatabase.ConnectionString));
        });
    }
}

// Base class for all integration tests
public abstract class IntegrationTestBase : IClassFixture<CustomWebApplicationFactory>, IAsyncLifetime
{
    protected readonly HttpClient Client;
    protected readonly AppDbContext Db;

    protected IntegrationTestBase(CustomWebApplicationFactory factory)
    {
        Client = factory.CreateClient();
        Db = factory.Services.GetRequiredService<AppDbContext>();
    }

    public async Task InitializeAsync()
    {
        await Db.Database.EnsureCreatedAsync();
        await SeedDataAsync();
    }

    public async Task DisposeAsync()
    {
        // Clean up test data — use transactions to rollback between tests
        await Db.Database.EnsureDeletedAsync();
    }

    protected virtual Task SeedDataAsync() => Task.CompletedTask;
}
```

### Integration test example

```csharp
// ECommerce.API.IntegrationTests/Catalog/ProductsIntegrationTests.cs

public class ProductsIntegrationTests : IntegrationTestBase
{
    private Guid _seededCategoryId;

    public ProductsIntegrationTests(CustomWebApplicationFactory factory) : base(factory) { }

    protected override async Task SeedDataAsync()
    {
        var category = Category.Create(CategoryName.Create("Electronics"), Slug.Create("electronics"));
        Db.Add(category);
        await Db.SaveChangesAsync();
        _seededCategoryId = category.Id;
    }

    [Fact]
    public async Task POST_CreateProduct_Returns201WithId()
    {
        var response = await Client.PostAsJsonAsync("/api/products", new
        {
            name = "iPhone 15",
            price = 999.00m,
            currency = "USD",
            sku = "IPHONE-15",
            categoryId = _seededCategoryId
        });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<ApiResponse<ProductDetailDto>>();
        Assert.NotNull(body?.Data);
        Assert.NotEqual(Guid.Empty, body!.Data.Id);
    }

    [Fact]
    public async Task POST_CreateProduct_DuplicateSku_Returns422()
    {
        // Create first product
        await Client.PostAsJsonAsync("/api/products", new
        {
            name = "First", price = 10m, currency = "USD",
            sku = "DUPE-001", categoryId = _seededCategoryId
        });

        // Try to create with same SKU
        var response = await Client.PostAsJsonAsync("/api/products", new
        {
            name = "Second", price = 20m, currency = "USD",
            sku = "DUPE-001", categoryId = _seededCategoryId  // ← same SKU
        });

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
    }

    [Fact]
    public async Task GET_GetProducts_ReturnsPaginatedList()
    {
        var response = await Client.GetAsync("/api/products?page=1&pageSize=10");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<ApiResponse<PaginatedResult<ProductDto>>>();
        Assert.NotNull(body?.Data);
        Assert.True(body!.Data.TotalCount >= 0);
    }
}
```

---

## 4. Characterization Tests

Characterization tests are the most important test type in this migration. They are written **before deleting the old service** and prove the new handlers preserve existing behavior.

### The workflow

```
Step 1: Write characterization tests AGAINST THE OLD SERVICE
         → Run them → They must PASS
         → These tests document the current behavior

Step 2: Implement the DDD migration (Steps 1-3 of the phase)

Step 3: Run characterization tests AGAINST THE NEW HANDLERS
         → They must still PASS
         → This proves no regression

Step 4: Delete the old service
```

### Characterization test structure

```csharp
// ECommerce.API.IntegrationTests/Catalog/ProductCharacterizationTests.cs
// IMPORTANT: Write this BEFORE the migration. Run against old code first.

public class ProductCharacterizationTests : IntegrationTestBase
{
    // ─── These tests document ALL behaviors of the old ProductService ───

    // Capture happy path: create succeeds with correct response shape
    [Fact]
    public async Task CreateProduct_HappyPath_Returns201()
    {
        // Seed category...
        var response = await Client.PostAsJsonAsync("/api/products", ValidCreateRequest());
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<ApiResponse<ProductDetailDto>>();
        Assert.NotNull(body?.Data?.Id);
        Assert.Equal("Test Product", body!.Data.Name);
    }

    // Capture validation: empty name → 400
    [Fact]
    public async Task CreateProduct_EmptyName_Returns400()
    {
        var request = ValidCreateRequest() with { Name = "" };
        var response = await Client.PostAsJsonAsync("/api/products", request);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    // Capture business rule: duplicate SKU → 422 (NOT 400 — it's a business rejection)
    [Fact]
    public async Task CreateProduct_DuplicateSku_Returns422WithErrorCode()
    {
        await Client.PostAsJsonAsync("/api/products", ValidCreateRequest());
        var response = await Client.PostAsJsonAsync("/api/products", ValidCreateRequest());

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<ApiResponse<object>>();
        Assert.Equal(ErrorCodes.Catalog.SkuAlreadyExists, body?.ErrorCode);
    }

    // Capture authorization: unauthenticated → 401
    [Fact]
    public async Task CreateProduct_Unauthenticated_Returns401()
    {
        // Use a client without auth header
        var unauthClient = _factory.CreateClient();
        var response = await unauthClient.PostAsJsonAsync("/api/products", ValidCreateRequest());
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    // Capture get by slug: 200 with correct data
    [Fact]
    public async Task GetProductBySlug_Exists_Returns200()
    {
        await Client.PostAsJsonAsync("/api/products", ValidCreateRequest() with { Name = "Slug Test" });
        var response = await Client.GetAsync("/api/products/slug-test");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    // Capture get by slug: not found → 404
    [Fact]
    public async Task GetProductBySlug_NotFound_Returns404()
    {
        var response = await Client.GetAsync("/api/products/does-not-exist");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
```

### What to characterize per endpoint

For every endpoint in the context being migrated, write tests for:

1. **Happy path** — success, correct status code, correct response shape
2. **Not found** — resource doesn't exist, returns 404
3. **Validation failure** — missing required field, returns 400
4. **Business rule violation** — duplicate, invalid state, returns 422 with error code
5. **Authorization** — unauthenticated returns 401, wrong role returns 403

This is a minimum of 4–5 tests per endpoint. A context with 6 commands + 4 queries = ~50 characterization tests. That's not too many — it's your safety net.

---

## Test Project Structure

```
src/backend/
├── ECommerce.Catalog.Domain.Tests/           ← Domain unit tests
│   └── Aggregates/
│       ├── ProductTests.cs
│       └── CategoryTests.cs
│   └── ValueObjects/
│       └── MoneyTests.cs
│
├── ECommerce.Catalog.Application.Tests/      ← Handler unit tests
│   └── Commands/
│       └── CreateProductCommandHandlerTests.cs
│   └── Queries/
│       └── GetProductsQueryHandlerTests.cs
│
└── ECommerce.API.IntegrationTests/           ← Integration + characterization
    ├── CustomWebApplicationFactory.cs
    ├── IntegrationTestBase.cs
    └── Catalog/
        ├── ProductCharacterizationTests.cs
        └── ProductsIntegrationTests.cs
```

**One test project per domain project.** The integration tests project is shared across all contexts.

---

## What Tests Tell You

| Test Fails | Meaning |
|-----------|---------|
| Domain unit test | Business rule is wrong or broken |
| Handler unit test | Orchestration logic is broken (wrong repo called, wrong aggregate method) |
| Integration test | Something in the full stack is broken (mapping, EF config, routing) |
| Characterization test | Your migration introduced a regression — behavior changed |

---

## NuGet Packages for Tests

```xml
<!-- Each test project -->
<PackageReference Include="xunit" Version="2.*" />
<PackageReference Include="xunit.runner.visualstudio" Version="2.*" />
<PackageReference Include="Moq" Version="4.*" />                          <!-- mocking -->
<PackageReference Include="FluentAssertions" Version="6.*" />             <!-- readable assertions -->
<PackageReference Include="Microsoft.AspNetCore.Mvc.Testing" Version="10.*" />  <!-- integration tests -->
<PackageReference Include="Testcontainers.PostgreSql" Version="3.*" />    <!-- real DB in Docker -->
```

`Testcontainers.PostgreSql` spins up a real PostgreSQL container per test run. No shared test DB, no flaky tests from state leaking between runs.

---

## Summary

1. **Write domain tests first.** They are fast, easy, and catch the most bugs.
2. **Write characterization tests BEFORE migrating** each context. Run them against the old service. Confirm they pass.
3. **Run characterization tests AFTER migrating**. They must still pass.
4. **Only then delete the old service.**
5. Handler unit tests verify orchestration. Integration tests verify the stack end-to-end.
