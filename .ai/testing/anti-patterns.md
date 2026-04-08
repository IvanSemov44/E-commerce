# Anti-Patterns

What NOT to do — with examples showing the wrong way and the right way.

---

## Backend

### 1. Testing domain rules through HTTP

The most expensive mistake. Domain rules tested at the integration layer are slow, hard to read, and duplicate coverage.

```csharp
// WRONG — testing a domain invariant via HTTP
[TestMethod]
public async Task POST_NegativePrice_Returns422()
{
    var body = new { Name = "Widget", Price = -5m };
    HttpResponseMessage response = await _adminClient.PostAsync("/api/products", Serialize(body));
    Assert.AreEqual(HttpStatusCode.UnprocessableEntity, response.StatusCode);
}

// RIGHT — test it at the domain layer
[TestMethod]
public void NegativePrice_ReturnsFailure()
{
    Result<Product> result = Product.Create("Widget", -5m);
    result.IsSuccess.ShouldBeFalse();
    result.GetErrorOrThrow().Code.ShouldBe("PRODUCT_PRICE_NEGATIVE");
}
// Then add ONE integration test proving the pipeline wires through — not repeating every rule.
```

---

### 2. Using a real DbContext in application tests

Application tests must be fast and deterministic. EF Core introduces database state, migrations, and timing.

```csharp
// WRONG
var options = new DbContextOptionsBuilder<CatalogDbContext>()
    .UseInMemoryDatabase("test").Build();
var db = new CatalogDbContext(options);
var handler = new CreateProductCommandHandler(new ProductRepository(db), new UnitOfWork(db));

// RIGHT — use the hand-written fake
var fakeRepo = new FakeProductRepository();
var fakeUow = new FakeUnitOfWork();
var handler = new CreateProductCommandHandler(fakeRepo, fakeUow);
```

---

### 3. Only asserting status codes in integration tests

Status codes alone prove nothing. Always assert the response body.

```csharp
// WRONG
Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);

// RIGHT
Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
ApiResponse<ProductDto>? body = await Deserialize<ApiResponse<ProductDto>>(response);
body.ShouldNotBeNull();
body.Success.ShouldBeTrue();
body.Data.ShouldNotBeNull();
body.Data!.Name.ShouldBe("Widget Pro");
```

---

### 4. Not asserting error codes on business failures

Error codes are the contract. If a migration breaks an error code, no test catches it unless you assert the code explicitly.

```csharp
// WRONG
Assert.AreEqual(HttpStatusCode.UnprocessableEntity, response.StatusCode);

// RIGHT
Assert.AreEqual(HttpStatusCode.UnprocessableEntity, response.StatusCode);
ApiResponse<JsonElement>? body = await Deserialize<ApiResponse<JsonElement>>(response);
body?.ErrorCode.ShouldBe("CATALOG_PRODUCT_NAME_EMPTY");
```

---

### 5. Shared state between tests via shared fakes

Shared fake instances accumulate state across tests in the same class, creating hidden ordering dependencies.

```csharp
// WRONG — _repo accumulates state; test 2 depends on test 1 having run first
[TestClass]
public class CreateProductCommandHandlerTests
{
    private readonly FakeProductRepository _repo = new(); // shared across all tests!
    private readonly FakeUnitOfWork _uow = new();

    [TestMethod]
    public async Task ValidCommand_CreatesProduct() { ... } // seeds _repo

    [TestMethod]
    public async Task DuplicateSku_ReturnsFailure() { ... } // silently relies on above
}

// RIGHT — each test creates its own fakes
[TestClass]
public class CreateProductCommandHandlerTests
{
    [TestClass]
    public class Handle
    {
        [TestMethod]
        public async Task ValidCommand_CreatesProduct()
        {
            FakeProductRepository repo = new();
            FakeUnitOfWork uow = new();
            CreateProductCommandHandler handler = new(repo, uow);
            // ... test is fully self-contained
        }

        [TestMethod]
        public async Task DuplicateSku_ReturnsFailure()
        {
            FakeProductRepository repo = new();
            FakeUnitOfWork uow = new();
            CreateProductCommandHandler handler = new(repo, uow);
            // Seed exactly what this test needs, nothing more
            await repo.AddAsync(Product.Create("Existing", 10m, "SKU-001").GetDataOrThrow());
            // ...
        }
    }
}
```

---

### 6. Writing characterization tests after the refactor

They have no value once the code has changed. The point is: write → verify they pass → change the code → verify they still pass.

```
// WRONG — sequence
1. Migrate the handler
2. Write characterization tests
3. Run — they pass (of course; the code already works)

// RIGHT — sequence
1. Write characterization tests
2. Run — they pass (documents current behavior)
3. Migrate the handler
4. Run — they must still pass (no regression)
```

---

### 7. Using Moq in domain or application tests

Moq in domain tests means you are testing something other than the domain. In application tests, prefer hand-written fakes — they are simpler, faster to read, and catch more bugs.

```csharp
// WRONG in application tests
var mockRepo = new Mock<IProductRepository>();
mockRepo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
        .ReturnsAsync((Product?)null);

// RIGHT — fake repo, assert on observable state
FakeProductRepository fakeRepo = new();
// ... run handler ...
fakeRepo.All.ShouldBeEmpty();
fakeUow.SaveChangesCount.ShouldBe(0);
```

---

### 8. Using InMemory EF for integration tests that depend on relational behaviour

`UseInMemoryDatabase` does not enforce referential integrity, does not support transactions, and behaves differently from real SQL providers. Tests that pass against InMemory can fail against a real database.

```csharp
// RISKY for integration tests — InMemory ignores FK constraints
services.AddDbContext<AppDbContext>(opt =>
    opt.UseInMemoryDatabase("test"));

// BETTER for integration tests — SQLite enforces constraints and is still fast
services.AddDbContext<AppDbContext>(opt =>
    opt.UseSqlite("DataSource=:memory:"));
// Note: SQLite is already in ECommerce.Tests.csproj. Use it.

// InMemory IS acceptable for projection sync tests (Layer 5) — they only test
// MediatR handler read/write paths, not relational constraints.
```

---

## Frontend

### 9. Mocking RTK Query hooks with vi.mock (implementation detail testing)

`vi.mock` at the module level replaces the hook itself, not the network. This means you are testing against a fake that shares no behaviour with the real RTK Query hook — different caching, different loading states, different error shapes. The test passes even if RTK Query is completely broken.

```ts
// WRONG — mocking the hook is testing the mock, not the component
vi.mock('@/features/cart/api/cartApi', () => ({
    useRemoveFromCartMutation: () => [vi.fn(), { isLoading: false }],
}));

// RIGHT — use MSW to intercept at the network level
// The real RTK Query hook fires, real loading state, real cache update
import { http, HttpResponse } from 'msw';
import { server } from '@/shared/lib/test/msw-server';

it('click_Remove_RemovesItemFromCart', async () => {
    server.use(
        http.delete('/api/cart/:itemId', () => HttpResponse.json({ success: true }))
    );

    renderWithProviders(<CartItem id="1" productName="Widget Pro" quantity={2} unitPrice={29.99} />);
    await userEvent.click(screen.getByRole('button', { name: /remove/i }));

    // Assert on what the user sees, not on internals
    await waitFor(() =>
        expect(screen.queryByText('Widget Pro')).not.toBeInTheDocument()
    );
});
```

---

### 10. Inline selectors in E2E spec files

Selectors change. When they live in spec files, you update them in 10 places. When they live in Page Objects, you update once.

```typescript
// WRONG — selector inline in spec
await page.click('button[data-testid="add-to-cart-btn"]');
await page.waitForSelector('[data-testid="cart-count"]');

// RIGHT — Page Object
const cartPage = new CartPage(page);
await cartPage.addToCart();
await cartPage.expectItemCount(1);
```

---

### 11. Testing implementation details (what a function calls, not what it returns)

Tests that verify internal calls break on every refactor even when behavior is unchanged.

```csharp
// WRONG — testing that a specific method was called
mockRepo.Verify(r => r.AddAsync(It.IsAny<Product>(), default), Times.Once);

// RIGHT — testing the observable outcome
fakeRepo.All.ShouldHaveSingleItem();
fakeRepo.All[0].Name.ShouldBe("Widget");
fakeUow.SaveChangesCount.ShouldBe(1);
```
