# Prompt: Backend Application Handler Test

Use this prompt when adding or changing a command or query handler. Paste the handler and its interfaces at the bottom.

---

```
You are writing MSTest application-layer tests for a command or query handler in this DDD/CQRS e-commerce repository.

## Conventions (non-negotiable)

LAYER: Application (Layer 2)
PROJECT: src/backend/<BC>/ECommerce.<BC>.Tests/Application/
DEPS: Hand-written fake repositories only. No EF Core. No Moq. No web host.
NAMING: Scenario_ExpectedOutcome  (no method-name prefix — you are already inside the handler test class)
CLASS: <HandlerName>Tests with nested [TestClass] public class Handle { ... }
ASSERTIONS: Shouldly  (result.IsSuccess.ShouldBeTrue(), not Assert.IsTrue)

## Fakes pattern

Each BC has a Fakes.cs with hand-written in-memory implementations. If a fake for a needed interface does not exist, create it following this pattern:

internal sealed class Fake<Name>Repository : I<Name>Repository
{
    private readonly List<TAggregateRoot> _store = new();

    public Task<TAggregateRoot?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => Task.FromResult(_store.FirstOrDefault(x => x.Id == id));

    public Task AddAsync(TAggregateRoot entity, CancellationToken ct = default)
    {
        _store.Add(entity);
        return Task.CompletedTask;
    }

    // Test helpers — assert on state, not on method calls
    public bool Contains(Guid id) => _store.Any(x => x.Id == id);
    public TAggregateRoot? Find(Guid id) => _store.FirstOrDefault(x => x.Id == id);
    public IReadOnlyList<TAggregateRoot> All => _store.AsReadOnly();
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

## Test class structure

Each test MUST create its own fakes. No shared state — shared fakes create ordering dependencies.
Use a private static Build() helper to reduce boilerplate.

[TestClass]
public class <HandlerName>Tests
{
    private static (Fake<X>Repository repo, FakeUnitOfWork uow, <HandlerName> handler) Build()
    {
        Fake<X>Repository repo = new();
        FakeUnitOfWork uow = new();
        return (repo, uow, new <HandlerName>(repo, uow));
    }

    [TestClass]
    public class Handle
    {
        // tests here
    }
}

## What to generate for COMMAND handlers

1. ValidCommand_<DescribesEffect>: assert aggregate is in the fake repo AND uow.SaveChangesCount == 1
2. <ResourceType>NotFound_ReturnsNotFoundError: assert IsSuccess == false, assert error code
3. <BusinessRule>Violated_ReturnsFailure: one test per distinct business rule the handler enforces

## What to generate for QUERY handlers

1. ExistingResource_ReturnsDtoWithCorrectShape: assert IsSuccess == true, assert key DTO fields
2. NonExistentId_ReturnsNotFoundError: assert IsSuccess == false, assert error code

## Assertion patterns (Shouldly — mandatory)

// Verify repo state
var (repo, uow, handler) = Build();
repo.Contains(productId).ShouldBeTrue();
repo.Find(productId)!.Name.Value.ShouldBe("Widget");

// Verify commit happened
uow.SaveChangesCount.ShouldBe(1);

// Verify failure
result.IsSuccess.ShouldBeFalse();
result.GetErrorOrThrow().Code.ShouldBe("CATALOG_PRODUCT_NOT_FOUND");

// Verify DTO shape
ProductDto dto = result.GetDataOrThrow();
dto.Id.ShouldBe(product.Id);
dto.Name.ShouldBe("Widget");

## Rules
- Create fakes inside each test via Build() — never as shared class fields
- Pass CancellationToken.None to Handle() — TestContext.CancellationToken is for integration tests only
- Seed the fake repo directly for prerequisites — do not run another handler to set up state
- Use nested [TestClass] public class Handle { } — not #region
- Use Shouldly for all assertions — not Assert.AreEqual / Assert.IsTrue

## After writing
Run: dotnet test src/backend/<BC>/ECommerce.<BC>.Tests/ECommerce.<BC>.Tests.csproj
All tests must PASS.

---

## Code to test

[PASTE THE HANDLER CLASS HERE]

[PASTE THE DOMAIN INTERFACES (IRepository, IUnitOfWork) HERE]

[PASTE EXISTING Fakes.cs IF IT EXISTS — so you can extend rather than duplicate]
```
