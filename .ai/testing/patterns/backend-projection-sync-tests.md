# Pattern: Backend Projection Sync Tests

Layer 5. Publish an integration event → assert read model state. Minimal DI scope. No web host.

---

## Purpose

Proves that an `INotificationHandler<TIntegrationEvent>` correctly maintains a read model when events fire. Required for every handler that syncs a projection from another bounded context.

---

## File naming

```
src/backend/ECommerce.Tests/Integration/
  <BC><Subject>ProjectionSyncCharacterizationTests.cs
```

Examples:
```
ReviewsProductProjectionSyncCharacterizationTests.cs
OrderingProductProjectionSyncCharacterizationTests.cs
OrderingPromoProjectionSyncCharacterizationTests.cs
```

---

## Minimal DI scope — the key pattern

Never use `TestWebApplicationFactory` for these tests. Build only the minimum needed.

```csharp
private static AsyncServiceScope CreateScope(string databaseName)
{
    var services = new ServiceCollection();
    services.AddLogging();
    services.AddDbContext<ReviewsDbContext>(opt =>
        opt.UseInMemoryDatabase(databaseName));
    services.AddMediatR(cfg =>
        cfg.RegisterServicesFromAssembly(typeof(ReviewsDbContext).Assembly));

    ServiceProvider provider = services.BuildServiceProvider();
    return provider.CreateAsyncScope();
}
```

Use a unique database name per test to prevent cross-test interference:
```csharp
await using AsyncServiceScope scope =
    CreateScope($"reviews-product-{Guid.NewGuid():N}");
```

---

## Full three-path template

```csharp
[TestClass]
public class ReviewsProductProjectionSyncCharacterizationTests
{
    // ── Insert path ───────────────────────────────────────────────────────────

    [TestMethod]
    public async Task Publish_NewProduct_InsertsProjection()
    {
        // Arrange
        Guid productId = Guid.NewGuid();
        await using AsyncServiceScope scope = CreateScope($"reviews-insert-{Guid.NewGuid():N}");
        ReviewsDbContext db = scope.ServiceProvider.GetRequiredService<ReviewsDbContext>();
        IPublisher publisher = scope.ServiceProvider.GetRequiredService<IPublisher>();

        // Act
        await publisher.Publish(
            new ProductProjectionUpdatedIntegrationEvent(
                productId, "Widget", 19.99m, isDeleted: false, DateTime.UtcNow),
            CancellationToken.None);

        // Assert
        ProductReadModel? projection = await db.Products.SingleOrDefaultAsync(x => x.Id == productId);
        Assert.IsNotNull(projection);
        Assert.IsTrue(projection.IsActive);
    }

    // ── Update path ───────────────────────────────────────────────────────────

    [TestMethod]
    public async Task Publish_ExistingProduct_UpdatesProjection()
    {
        // Arrange — seed existing projection
        Guid productId = Guid.NewGuid();
        await using AsyncServiceScope scope = CreateScope($"reviews-update-{Guid.NewGuid():N}");
        ReviewsDbContext db = scope.ServiceProvider.GetRequiredService<ReviewsDbContext>();
        IPublisher publisher = scope.ServiceProvider.GetRequiredService<IPublisher>();

        db.Products.Add(new ProductReadModel { Id = productId, IsActive = true, UpdatedAt = DateTime.UtcNow.AddDays(-1) });
        await db.SaveChangesAsync();

        DateTime updatedAt = DateTime.UtcNow;

        // Act
        await publisher.Publish(
            new ProductProjectionUpdatedIntegrationEvent(
                productId, "Widget Updated", 29.99m, isDeleted: false, updatedAt),
            CancellationToken.None);

        // Assert
        ProductReadModel? projection = await db.Products.SingleOrDefaultAsync(x => x.Id == productId);
        Assert.IsNotNull(projection);
        Assert.IsTrue(projection.IsActive);
        Assert.AreEqual(updatedAt, projection.UpdatedAt);
    }

    // ── Delete path ───────────────────────────────────────────────────────────

    [TestMethod]
    public async Task Publish_DeletedProduct_RemovesProjection()
    {
        // Arrange — seed existing projection
        Guid productId = Guid.NewGuid();
        await using AsyncServiceScope scope = CreateScope($"reviews-delete-{Guid.NewGuid():N}");
        ReviewsDbContext db = scope.ServiceProvider.GetRequiredService<ReviewsDbContext>();
        IPublisher publisher = scope.ServiceProvider.GetRequiredService<IPublisher>();

        db.Products.Add(new ProductReadModel { Id = productId, IsActive = true, UpdatedAt = DateTime.UtcNow });
        await db.SaveChangesAsync();

        // Act
        await publisher.Publish(
            new ProductProjectionUpdatedIntegrationEvent(
                productId, string.Empty, 0m, isDeleted: true, DateTime.UtcNow),
            CancellationToken.None);

        // Assert
        Assert.IsNull(await db.Products.SingleOrDefaultAsync(x => x.Id == productId));
    }

    // ── Scope builder ─────────────────────────────────────────────────────────

    private static AsyncServiceScope CreateScope(string databaseName)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddDbContext<ReviewsDbContext>(opt => opt.UseInMemoryDatabase(databaseName));
        services.AddMediatR(cfg =>
            cfg.RegisterServicesFromAssembly(typeof(ReviewsDbContext).Assembly));

        return services.BuildServiceProvider().CreateAsyncScope();
    }
}
```

---

## Rules

1. **Three tests per handler:** insert (projection missing), update (projection exists), delete (projection removed). All three are required.

2. **Unique DB name per test** — use `Guid.NewGuid():N` suffix. Shared databases between tests produce false results.

3. **Seed state directly via DbContext** for the update and delete paths — do not publish a create event to set up state.

4. **Dispose the scope** — use `await using`. The InMemory provider holds a reference; leaking scopes causes cross-test contamination.

5. **Register only what the handler needs** — `AddLogging` + the target `DbContext` + `AddMediatR` pointing at the handler's assembly. Do not add the full application DI.

6. **Assert on the DbContext directly** — not via a service or API call. The point is to verify the database state the handler produced.
