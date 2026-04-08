# Prompt: Backend Application Handler Test

Use this prompt when adding or changing a command or query handler. Paste the handler and its interfaces at the bottom.

---

```
You are writing MSTest application-layer tests for a command or query handler in this DDD/CQRS e-commerce repository.

## Conventions (non-negotiable)

LAYER: Application (Layer 2)
PROJECT: src/backend/<BC>/ECommerce.<BC>.Tests/Application/
DEPS: Hand-written fake repositories only. No EF Core. No Moq. No web host.
NAMING: MethodName_Scenario_ExpectedOutcome
CLASS: <HandlerName>Tests

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

    // Test helpers
    public bool Contains(Guid id) => _store.Any(x => x.Id == id);
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

[TestClass]
public class <HandlerName>Tests
{
    // Fields: one fake instance per dependency
    private readonly Fake<X>Repository _repo = new();
    private readonly FakeUnitOfWork _uow = new();
    private readonly <HandlerName> _handler;

    public <HandlerName>Tests()
    {
        _handler = new <HandlerName>(_repo, _uow);
    }

    #region Handle

    // tests here

    #endregion
}

## What to generate for COMMAND handlers

1. Handle_ValidCommand_<DescribesEffect>: assert the aggregate is in the fake repo AND SaveChangesCount == 1
2. Handle_<ResourceType>NotFound_ReturnsNotFoundError: assert IsSuccess == false, assert error code
3. Handle_<BusinessRule>Violated_ReturnsFailure: one test per distinct business rule the handler enforces

## What to generate for QUERY handlers

1. Handle_ExistingResource_ReturnsDtoWithCorrectShape: assert IsSuccess == true, assert key DTO fields
2. Handle_NonExistentId_ReturnsNotFoundError: assert IsSuccess == false, assert error code

## Assertion patterns

// Verify repo state (GOOD — check the fake, not mock.Verify)
Assert.IsTrue(_repo.Contains(productId));
Assert.AreEqual("Widget", _repo.Find(productId)!.Name.Value);

// Verify commit happened
Assert.AreEqual(1, _uow.SaveChangesCount);

// Verify failure
Assert.IsFalse(result.IsSuccess);
Assert.AreEqual("CATALOG_PRODUCT_NOT_FOUND", result.GetErrorOrThrow().Code);

// Verify DTO shape
ProductDto dto = result.GetDataOrThrow();
Assert.AreEqual(product.Id, dto.Id);
Assert.AreEqual("Widget", dto.Name);

## Rules
- Pass CancellationToken.None to Handle() — not TestContext.CancellationToken (no MSTest context here)
- Seed the fake repo directly for prerequisites — do not run another handler to set up state
- Do not assert on Moq.Verify — prefer asserting on fake state

## After writing
Run: dotnet test src/backend/<BC>/ECommerce.<BC>.Tests/ECommerce.<BC>.Tests.csproj
All tests must PASS.

---

## Code to test

[PASTE THE HANDLER CLASS HERE]

[PASTE THE DOMAIN INTERFACES (IRepository, IUnitOfWork) HERE]

[PASTE EXISTING Fakes.cs IF IT EXISTS — so you can extend rather than duplicate]
```
