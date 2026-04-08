# Pattern: Backend Application Tests

Layer 2. Tests command and query handlers with hand-written fake repositories. No EF Core. No web host.

---

## Project structure

```
src/backend/<BC>/ECommerce.<BC>.Tests/
└── Application/
    ├── Fakes.cs                                    ← all fakes for this BC
    ├── <CommandName>CommandHandlerTests.cs
    └── <QueryName>QueryHandlerTests.cs
```

---

## Fakes.cs — standard structure

Every BC test project has one `Fakes.cs` with implementations of all domain interfaces used by handlers.

```csharp
// src/backend/Catalog/ECommerce.Catalog.Tests/Application/Fakes.cs

internal sealed class FakeProductRepository : IProductRepository
{
    private readonly List<Product> _store = new();

    public Task<Product?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => Task.FromResult(_store.FirstOrDefault(p => p.Id == id));

    public Task<Product?> GetBySkuAsync(string sku, CancellationToken ct = default)
        => Task.FromResult(_store.FirstOrDefault(p => p.Sku.Value == sku));

    public Task AddAsync(Product product, CancellationToken ct = default)
    {
        _store.Add(product);
        return Task.CompletedTask;
    }

    public Task UpdateAsync(Product product, CancellationToken ct = default)
        => Task.CompletedTask; // EF tracks in-place; no-op in fake

    public Task DeleteAsync(Product product, CancellationToken ct = default)
    {
        _store.Remove(product);
        return Task.CompletedTask;
    }

    // Test helpers
    public bool Contains(Guid id) => _store.Any(p => p.Id == id);
    public Product? Find(Guid id) => _store.FirstOrDefault(p => p.Id == id);
    public IReadOnlyList<Product> All => _store.AsReadOnly();
}

internal sealed class FakeUnitOfWork : IUnitOfWork
{
    public int SaveChangesCount { get; private set; }

    public Task<int> SaveChangesAsync(CancellationToken ct = default)
    {
        SaveChangesCount++;
        return Task.FromResult(1);
    }
}
```

---

## Command handler test template

```csharp
[TestClass]
public class CreateProductCommandHandlerTests
{
    private readonly FakeProductRepository _repo = new();
    private readonly FakeUnitOfWork _uow = new();
    private readonly CreateProductCommandHandler _handler;

    public CreateProductCommandHandlerTests()
    {
        _handler = new CreateProductCommandHandler(_repo, _uow);
    }

    #region Handle

    [TestMethod]
    public async Task Handle_ValidCommand_CreatesProductAndCommits()
    {
        // Arrange
        CreateProductCommand command = new("Widget Pro", 29.99m, "SKU-001", categoryId: Guid.NewGuid());

        // Act
        Result<Guid> result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.IsTrue(result.IsSuccess);
        Guid productId = result.GetDataOrThrow();
        Assert.IsTrue(_repo.Contains(productId));
        Assert.AreEqual(1, _uow.SaveChangesCount);
    }

    [TestMethod]
    public async Task Handle_DuplicateSku_ReturnsFailure()
    {
        // Arrange — seed existing product with same SKU
        CreateProductCommand first = new("Existing", 10m, "SKU-001", Guid.NewGuid());
        await _handler.Handle(first, CancellationToken.None);

        CreateProductCommand duplicate = new("Another", 20m, "SKU-001", Guid.NewGuid());

        // Act
        Result<Guid> result = await _handler.Handle(duplicate, CancellationToken.None);

        // Assert
        Assert.IsFalse(result.IsSuccess);
        Assert.AreEqual("CATALOG_SKU_ALREADY_EXISTS", result.GetErrorOrThrow().Code);
        Assert.AreEqual(1, _uow.SaveChangesCount); // Only the first command committed
    }

    [TestMethod]
    public async Task Handle_CategoryNotFound_ReturnsFailure()
    {
        // Arrange — fake category repo returns null
        FakeCategoryRepository categoryRepo = new(); // empty — no categories
        CreateProductCommandHandler handlerWithEmptyCategories =
            new(_repo, categoryRepo, _uow);

        CreateProductCommand command = new("Widget", 10m, "SKU-001", Guid.NewGuid());

        // Act
        Result<Guid> result = await handlerWithEmptyCategories.Handle(command, CancellationToken.None);

        // Assert
        Assert.IsFalse(result.IsSuccess);
        Assert.AreEqual("CATALOG_CATEGORY_NOT_FOUND", result.GetErrorOrThrow().Code);
        Assert.AreEqual(0, _uow.SaveChangesCount);
    }

    #endregion
}
```

---

## Query handler test template

```csharp
[TestClass]
public class GetProductByIdQueryHandlerTests
{
    private readonly FakeProductRepository _repo = new();
    private readonly GetProductByIdQueryHandler _handler;

    public GetProductByIdQueryHandlerTests()
    {
        _handler = new GetProductByIdQueryHandler(_repo);
    }

    #region Handle

    [TestMethod]
    public async Task Handle_ExistingProduct_ReturnsDto()
    {
        // Arrange
        Product product = Product.Create("Widget", 29.99m, "SKU-001").GetDataOrThrow();
        await _repo.AddAsync(product);

        GetProductByIdQuery query = new(product.Id);

        // Act
        Result<ProductDto> result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.IsTrue(result.IsSuccess);
        ProductDto dto = result.GetDataOrThrow();
        Assert.AreEqual(product.Id, dto.Id);
        Assert.AreEqual("Widget", dto.Name);
    }

    [TestMethod]
    public async Task Handle_NonExistentId_ReturnsNotFoundError()
    {
        // Arrange
        GetProductByIdQuery query = new(Guid.NewGuid());

        // Act
        Result<ProductDto> result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.IsFalse(result.IsSuccess);
        Assert.AreEqual("CATALOG_PRODUCT_NOT_FOUND", result.GetErrorOrThrow().Code);
    }

    #endregion
}
```

---

## Rules

1. **One fake repo instance per test class** — created in the field initializer, not in `[TestInitialize]`. Tests share the same instance to let them seed state naturally.

2. **Always assert `SaveChangesCount`** on command tests. Commands must commit exactly once on success and zero times on failure.

3. **Assert the fake repo state**, not that a method was called:
   ```csharp
   // GOOD
   Assert.IsTrue(_repo.Contains(productId));
   // BAD
   mockRepo.Verify(r => r.AddAsync(...), Times.Once);
   ```

4. **Seed state directly via the fake** — do not run another command to create prerequisites:
   ```csharp
   // GOOD — direct fake seeding
   await _repo.AddAsync(existingProduct);
   // BAD — running another command to create the product first
   await _createHandler.Handle(createCommand, ct);
   ```

5. **No `CancellationToken` from MSTest** in application tests — pass `CancellationToken.None`. The `TestContext.CancellationToken` is only for integration tests using `HttpClient`.
