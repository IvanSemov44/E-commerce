# Prompt: Backend Projection Sync Test

Use this prompt when adding a new integration event handler that maintains a read model.

---

```
You are writing projection sync characterization tests for a new integration event handler in this DDD/CQRS e-commerce repository.

## STEP 1 — Extract before generating (mandatory)

Before writing any test, read the pasted handler and list:
- The INotificationHandler<TEvent> type name
- The integration event constructor parameters (exact names and types)
- The read model class name and its settable properties
- The DbSet property name on the target DbContext
- What condition triggers insert vs. update vs. delete in the handler logic

If any of these are missing from the pasted code, write "MISSING: [item]" and stop.

## Purpose
These tests verify that an INotificationHandler<TEvent> correctly maintains its read model
when an integration event fires. Three paths must be covered: insert, update, delete.

## Conventions (non-negotiable)

LAYER: Projection Sync (Layer 5)
PROJECT: src/backend/ECommerce.Tests/
FILE: src/backend/ECommerce.Tests/Integration/<BC><Subject>ProjectionSyncCharacterizationTests.cs
NAMING: Publish_<Scenario>_<ExpectedOutcome>
CLASS: <BC><Subject>ProjectionSyncCharacterizationTests

## Minimal DI scope pattern (use exactly this — no web host, no TestWebApplicationFactory)

private static AsyncServiceScope CreateScope(string databaseName)
{
    var services = new ServiceCollection();
    services.AddLogging();
    services.AddDbContext<[TargetDbContext]>(opt =>
        opt.UseInMemoryDatabase(databaseName));
    services.AddMediatR(cfg =>
        cfg.RegisterServicesFromAssembly(typeof([TargetDbContext]).Assembly));

    return services.BuildServiceProvider().CreateAsyncScope();
}

Use a unique DB name per test: $"[prefix]-{Guid.NewGuid():N}"

## Three required tests — generate all three

### Test 1: Insert path (projection does not exist → gets created)

[TestMethod]
public async Task Publish_New<Subject>_InsertsProjection()
{
    // Arrange
    Guid id = Guid.NewGuid();
    await using AsyncServiceScope scope = CreateScope($"[prefix]-insert-{Guid.NewGuid():N}");
    [TargetDbContext] db = scope.ServiceProvider.GetRequiredService<[TargetDbContext]>();
    IPublisher publisher = scope.ServiceProvider.GetRequiredService<IPublisher>();

    // Act
    await publisher.Publish(new [EventName]([args]), CancellationToken.None);

    // Assert
    [ReadModel]? projection = await db.[DbSet].SingleOrDefaultAsync(x => x.Id == id);
    Assert.IsNotNull(projection);
    // assert key fields were set correctly
}

### Test 2: Update path (projection exists → gets updated)

[TestMethod]
public async Task Publish_Existing<Subject>_UpdatesProjection()
{
    // Arrange — seed the existing projection directly via DbContext
    Guid id = Guid.NewGuid();
    await using AsyncServiceScope scope = CreateScope($"[prefix]-update-{Guid.NewGuid():N}");
    [TargetDbContext] db = scope.ServiceProvider.GetRequiredService<[TargetDbContext]>();
    IPublisher publisher = scope.ServiceProvider.GetRequiredService<IPublisher>();

    db.[DbSet].Add(new [ReadModel] { Id = id, /* initial state */ });
    await db.SaveChangesAsync();

    // Act — publish with changed data
    await publisher.Publish(new [EventName]([args with changed values]), CancellationToken.None);

    // Assert — verify the updated fields
    [ReadModel]? projection = await db.[DbSet].SingleOrDefaultAsync(x => x.Id == id);
    Assert.IsNotNull(projection);
    // assert updated fields
}

### Test 3: Delete path (projection exists → gets removed)

[TestMethod]
public async Task Publish_Deleted<Subject>_RemovesProjection()
{
    // Arrange — seed the projection
    Guid id = Guid.NewGuid();
    await using AsyncServiceScope scope = CreateScope($"[prefix]-delete-{Guid.NewGuid():N}");
    [TargetDbContext] db = scope.ServiceProvider.GetRequiredService<[TargetDbContext]>();
    IPublisher publisher = scope.ServiceProvider.GetRequiredService<IPublisher>();

    db.[DbSet].Add(new [ReadModel] { Id = id, /* state */ });
    await db.SaveChangesAsync();

    // Act — publish delete signal
    await publisher.Publish(new [EventName]([args indicating deletion]), CancellationToken.None);

    // Assert
    Assert.IsNull(await db.[DbSet].SingleOrDefaultAsync(x => x.Id == id));
}

## Rules
- Unique DB name per test (Guid suffix) — no shared state
- Seed state directly via DbContext — do not publish a create event to set up update/delete tests
- Always await using on scope — prevents memory leaks
- Assert on DbContext state directly — not via API or service call

## NEVER do these
- Do NOT share a DbContext instance between tests — create a new scope per test
- Do NOT reuse the same database name between tests — always use Guid suffix
- Do NOT publish a create event to set up the update/delete test — seed directly via DbContext
- Do NOT add XML doc comments
- Do NOT invent read model property names — use only what is in the pasted code

## After writing
Run: dotnet test src/backend/ECommerce.Tests/ECommerce.Tests.csproj --filter "[BC][Subject]ProjectionSync"
All 3 tests must PASS.

---

## Handler to test

[PASTE THE INotificationHandler CLASS HERE]

## Integration event

[PASTE THE INTEGRATION EVENT RECORD/CLASS HERE]

## Read model

[PASTE THE READ MODEL CLASS HERE]

## Target DbContext (the DbSet the handler writes to)

[PASTE THE RELEVANT PART OF THE DBCONTEXT HERE]
```
