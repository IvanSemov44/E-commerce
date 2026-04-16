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

    // Test helpers — assert on state, not on method calls
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

Each test creates its own fakes. No shared state. No ordering dependencies.

```csharp
[TestClass]
public class CreateProductCommandHandlerTests
{
    // Helper — reduces boilerplate inside each test
    private static (FakeProductRepository repo, FakeUnitOfWork uow, CreateProductCommandHandler handler) Build()
    {
        FakeProductRepository repo = new();
        FakeUnitOfWork uow = new();
        return (repo, uow, new CreateProductCommandHandler(repo, uow));
    }

    [TestClass]
    public class Handle
    {
        [TestMethod]
        public async Task ValidCommand_CreatesProductAndCommits()
        {
            // Arrange
            var (repo, uow, handler) = Build();
            CreateProductCommand command = new("Widget Pro", 29.99m, "SKU-001", categoryId: Guid.NewGuid());

            // Act
            Result<Guid> result = await handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.ShouldBeTrue();
            Guid productId = result.GetDataOrThrow();
            repo.Contains(productId).ShouldBeTrue();
            uow.SaveChangesCount.ShouldBe(1);
        }

        [TestMethod]
        public async Task DuplicateSku_ReturnsSkuAlreadyExistsError()
        {
            // Arrange — seed existing product directly via fake (not via another command)
            var (repo, uow, handler) = Build();
            Product existing = Product.Create("Existing", 10m, "SKU-001").GetDataOrThrow();
            await repo.AddAsync(existing);

            CreateProductCommand command = new("Another", 20m, "SKU-001", Guid.NewGuid());

            // Act
            Result<Guid> result = await handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.ShouldBeFalse();
            result.GetErrorOrThrow().Code.ShouldBe("CATALOG_SKU_ALREADY_EXISTS");
            uow.SaveChangesCount.ShouldBe(0); // nothing committed
        }

        [TestMethod]
        public async Task CategoryNotFound_ReturnsNotFoundError()
        {
            // Arrange — empty category repo
            FakeProductRepository repo = new();
            FakeCategoryRepository categoryRepo = new(); // empty
            FakeUnitOfWork uow = new();
            CreateProductCommandHandler handler = new(repo, categoryRepo, uow);

            CreateProductCommand command = new("Widget", 10m, "SKU-001", Guid.NewGuid());

            // Act
            Result<Guid> result = await handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.ShouldBeFalse();
            result.GetErrorOrThrow().Code.ShouldBe("CATALOG_CATEGORY_NOT_FOUND");
            uow.SaveChangesCount.ShouldBe(0);
        }
    }
}
```

---

## Query handler test template

```csharp
[TestClass]
public class GetProductByIdQueryHandlerTests
{
    private static (FakeProductRepository repo, GetProductByIdQueryHandler handler) Build()
    {
        FakeProductRepository repo = new();
        return (repo, new GetProductByIdQueryHandler(repo));
    }

    [TestClass]
    public class Handle
    {
        [TestMethod]
        public async Task ExistingProduct_ReturnsDto()
        {
            // Arrange
            var (repo, handler) = Build();
            Product product = Product.Create("Widget", 29.99m, "SKU-001").GetDataOrThrow();
            await repo.AddAsync(product);

            GetProductByIdQuery query = new(product.Id);

            // Act
            Result<ProductDto> result = await handler.Handle(query, CancellationToken.None);

            // Assert
            result.IsSuccess.ShouldBeTrue();
            ProductDto dto = result.GetDataOrThrow();
            dto.Id.ShouldBe(product.Id);
            dto.Name.ShouldBe("Widget");
        }

        [TestMethod]
        public async Task NonExistentId_ReturnsNotFoundError()
        {
            // Arrange
            var (_, handler) = Build();
            GetProductByIdQuery query = new(Guid.NewGuid());

            // Act
            Result<ProductDto> result = await handler.Handle(query, CancellationToken.None);

            // Assert
            result.IsSuccess.ShouldBeFalse();
            result.GetErrorOrThrow().Code.ShouldBe("CATALOG_PRODUCT_NOT_FOUND");
        }
    }
}
```

---

## Parameterized handler tests

Use `[DataTestMethod]` + `[DataRow]` when multiple invalid inputs map to the same error:

```csharp
[TestClass]
public class CreateProductCommandHandlerTests
{
    [TestClass]
    public class Handle
    {
        [DataTestMethod]
        [DataRow("")]
        [DataRow("   ")]
        [DataRow(null)]
        public async Task EmptyOrWhitespaceName_ReturnsValidationError(string? name)
        {
            // Arrange
            var (_, _, handler) = Build();
            CreateProductCommand command = new(name!, 10m, "SKU-001", Guid.NewGuid());

            // Act
            Result<Guid> result = await handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.ShouldBeFalse();
            result.GetErrorOrThrow().Code.ShouldBe("CATALOG_PRODUCT_NAME_EMPTY");
        }
    }
}
```

---

## Rules

1. **Each test creates its own fakes** — use a private static `Build()` helper or inline construction. No shared instance fields. Shared instances create ordering dependencies.

2. **Seed state directly via the fake** — do not run another command to create prerequisites:
   ```csharp
   // GOOD — direct fake seeding
   await repo.AddAsync(existingProduct);
   // BAD — running another command to create the product first
   await _createHandler.Handle(createCommand, ct);
   ```

3. **Always assert `SaveChangesCount`** on command tests. Commands must commit exactly once on success and zero times on failure.

4. **Assert on fake state, not on method calls:**
   ```csharp
   // GOOD
   repo.Contains(productId).ShouldBeTrue();
   // BAD
   mockRepo.Verify(r => r.AddAsync(It.IsAny<Product>(), default), Times.Once);
   ```

5. **Use `CancellationToken.None`** in application tests — `TestContext.CancellationToken` is only for integration tests using `HttpClient`.

6. **Use Shouldly for assertions** — not raw MSTest `Assert.*`. Shouldly gives better failure messages:
   ```csharp
   // MSTest — poor failure: "Assert.AreEqual failed. Expected: 1. Actual: 0."
   Assert.AreEqual(1, uow.SaveChangesCount);

   // Shouldly — clear failure: "uow.SaveChangesCount should be 1 but was 0"
   uow.SaveChangesCount.ShouldBe(1);
   ```

7. **Use nested `[TestClass]` to group by method** — not `#region`. See naming-conventions.md.
