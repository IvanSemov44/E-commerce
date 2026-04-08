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
    var client = _factory.CreateAdminClient();
    var body = new { Name = "Widget", Price = -5m };
    HttpResponseMessage response = await client.PostAsync("/api/products", Serialize(body));
    Assert.AreEqual(HttpStatusCode.UnprocessableEntity, response.StatusCode);
}

// RIGHT — test it at the domain layer
[TestMethod]
public void Create_NegativePrice_ReturnsFailure()
{
    Result<Product> result = Product.Create("Widget", -5m);
    Assert.IsFalse(result.IsSuccess);
    Assert.AreEqual("PRODUCT_PRICE_NEGATIVE", result.GetErrorOrThrow().Code);
}
// Then add ONE integration test that checks the endpoint returns 422 for bad input
// — not repeating every domain rule, just proving the pipeline wires through.
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
Assert.IsNotNull(body);
Assert.IsTrue(body.Success);
Assert.IsNotNull(body.Data);
Assert.AreEqual("Widget Pro", body.Data.Name);
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
Assert.AreEqual("CATALOG_PRODUCT_NAME_EMPTY", body?.ErrorCode);
```

---

### 5. Shared state between tests

Shared static fields, shared DbContext instances, or shared factory data that tests mutate = flaky tests and ordering dependencies.

```csharp
// WRONG — static product ID shared between tests
private static Guid _productId;

[TestMethod]
public async Task Create_ReturnsId() { _productId = ...; }

[TestMethod]
public async Task Get_ReturnsProduct() { /* depends on _productId */ }

// RIGHT — every test creates its own data
[TestMethod]
public async Task GET_ExistingProduct_Returns200()
{
    // Arrange — create the product inside this test
    HttpResponseMessage created = await client.PostAsync("/api/products", Serialize(new { Name = "Widget" }));
    Guid id = (await Deserialize<ApiResponse<ProductDto>>(created)).Data!.Id;

    // Act
    HttpResponseMessage response = await client.GetAsync($"/api/products/{id}");

    // Assert
    Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
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
mockRepo.Verify(r => r.AddAsync(It.IsAny<Product>(), It.IsAny<CancellationToken>()), Times.Once);

// RIGHT — fake repo, assert on state
var fakeRepo = new FakeProductRepository();
// ... run handler ...
Assert.IsTrue(fakeRepo.Contains(expectedProductId));
Assert.AreEqual(1, fakeUow.SaveChangesCount);
```

---

## Frontend

### 8. Making real HTTP calls in component or hook tests

Real network calls make tests slow and environment-dependent.

```tsx
// WRONG — real RTK Query call fires
renderWithProviders(<ProductList />);
await waitFor(() => expect(screen.getByText('Widget')).toBeInTheDocument());

// RIGHT — mock the API endpoint
vi.mock('@/shared/lib/api/baseApi', () => ({
    useGetProductsQuery: () => ({ data: [{ id: '1', name: 'Widget' }], isLoading: false }),
}));
renderWithProviders(<ProductList />);
expect(screen.getByText('Widget')).toBeInTheDocument();
```

---

### 9. Inline selectors in E2E spec files

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

### 10. Testing implementation details (what a function calls, not what it returns)

Tests that verify internal calls break on every refactor even when behavior is unchanged.

```csharp
// WRONG — testing that a specific method was called
mockRepo.Verify(r => r.AddAsync(It.IsAny<Product>(), default), Times.Once);

// RIGHT — testing the observable outcome
Assert.IsTrue(fakeRepo.All.Any(p => p.Name == "Widget"));
Assert.AreEqual(1, fakeUow.SaveChangesCount);
```
