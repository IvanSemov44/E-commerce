# DDD Migration: Testing Guide

This folder contains practical testing instructions for the DDD/CQRS migration.
Read `theory/07-testing-ddd.md` for the theory. Use the files here to actually do the work.

---

## Related Documents

| Document | Purpose |
|----------|---------|
| `theory/07-testing-ddd.md` | Theory behind each test level — read this first |
| `testing/tester-prompt-template.md` | Ready-to-paste prompts for the tester (4 prompts) |
| `testing/characterization-checklist.md` | Per-endpoint matrix and 5-scenario rule |
| `phases/phase-1-catalog.md` | Phase 1 step plan with tester handoff notes |
| `phases/phase-2-identity.md` | Phase 2 — same structure |
| `phases/phase-3-inventory.md` | Phase 3 |
| `phases/phase-4-shopping.md` | Phase 4 |
| `phases/phase-5-promotions.md` | Phase 5 |
| `phases/phase-6-reviews.md` | Phase 6 |
| `phases/phase-7-ordering.md` | Phase 7 |

Each phase doc marks exactly **when** to hand off to the tester ("Tester handoff after Step X") and lists test checkpoints in its Definition of Done.

---

## Project Structure Decision

Two separate test projects — different speed, different purpose:

```
src/backend/
├── ECommerce.Tests/                    ← HTTP integration tests (slow, ~seconds)
│   └── Integration/
│       ├── ProductsControllerTests.cs  ← characterization tests (HTTP → old service)
│       └── CategoriesControllerTests.cs
│
├── ECommerce.Catalog.Tests/            ← NEW — fast unit tests (ms, no DB, no HTTP)
│   ├── Domain/
│   │   ├── ProductTests.cs             ← aggregate + value object tests
│   │   └── CategoryTests.cs
│   └── Handlers/
│       ├── CreateProductCommandHandlerTests.cs
│       └── GetProductsQueryHandlerTests.cs
│
├── ECommerce.Identity.Tests/           ← Phase 2 (same pattern)
├── ECommerce.Inventory.Tests/          ← Phase 3
└── ...
```

**Rule: never mix fast and slow tests in the same project.**
Fast tests run on every save. Slow tests run before commit.

---

## Three Levels — When to Write Each

### Level 1: Characterization Tests (BEFORE migration starts)
**Project:** `ECommerce.Tests/Integration/`
**When:** Before touching any service — documents current behavior as baseline
**Speed:** Slow (seconds — real HTTP, InMemory DB)

```
Write tests → run → PASS (old service)
     ↓
Programmer migrates to handlers
     ↓
Run same tests → must still PASS (new handlers)
     ↓
Delete old service
```

### Level 2: Domain Unit Tests (AFTER aggregate is built)
**Project:** `ECommerce.Catalog.Tests/Domain/`
**When:** After programmer delivers aggregates and value objects
**Speed:** Fast (milliseconds — pure C#, no mocks, no DB)
**What:** Factory methods, invariants, domain events, value object validation

### Level 3: Handler Unit Tests (AFTER handlers are built)
**Project:** `ECommerce.Catalog.Tests/Handlers/`
**When:** After programmer delivers command/query handlers
**Speed:** Fast (milliseconds — mocked repos only)
**What:** Correct repo called, correct aggregate method, saves via UoW, correct Result returned

---

## What Each Level Catches

| Test Fails | Means |
|-----------|-------|
| Characterization test | Migration introduced a regression — behavior changed |
| Domain unit test | Business rule is wrong or broken |
| Handler unit test | Orchestration logic wrong (wrong method called, wrong return) |

---

## Per-Phase Checklist

Before closing any phase:
- [ ] Characterization tests written and PASSING against OLD service
- [ ] Programmer migrates to handlers
- [ ] Characterization tests still PASSING against NEW handlers
- [ ] Domain unit tests written and PASSING for new aggregates
- [ ] Handler unit tests written and PASSING for new handlers
- [ ] Old service deleted
- [ ] `dotnet test` passes with 0 failures

---

## Test Framework: Key Facts

**ECommerce.Tests (integration):**
- Framework: MSTest — `[TestClass]`, `[TestMethod]`, `[TestInitialize]`, `[TestCleanup]`
- Auth helpers: `_factory.CreateAdminClient()`, `_factory.CreateAuthenticatedClient()`, `_factory.CreateUnauthenticatedClient()`
- Response shape: `ApiResponse<T>` — see `ECommerce.Application/DTOs/Common/ApiResponse.cs`
  - `response.Success` (bool)
  - `response.Data` (T?)
  - `response.ErrorDetails.Code` (string?) ← use this for 422 assertions
- Error codes: `ECommerce.Core.Constants.ErrorCodes` — flat class, e.g. `ErrorCodes.ProductNotFound`

**ECommerce.Catalog.Tests (unit):**
- Framework: MSTest
- Mocking: Moq
- Assertions: FluentAssertions (`.Should().Be()`) or MSTest Assert
- No NuGet for DB — pure C#

---

## Code Style Rules (apply to all test files)

### AAA comments — mandatory in every [TestMethod]
Every test method body must have exactly three section comments:
```csharp
// Arrange
// Act
// Assert
```
If a test has a prerequisite step (e.g. create a category before testing the product), label it:
```csharp
// Arrange — create prerequisite category
...
Assert.AreEqual(HttpStatusCode.Created, catRes.StatusCode);

// Act
HttpResponseMessage res = await client.PostAsync(...);

// Assert
Assert.AreEqual(HttpStatusCode.Created, res.StatusCode);
```
Do NOT add AAA comments to `[TestInitialize]` or `[TestCleanup]`.

### CancellationToken — mandatory on all HTTP calls
Add `public TestContext TestContext { get; set; } = null!;` to each test class.
Pass `TestContext.CancellationToken` to every HTTP method:
```csharp
HttpResponseMessage res = await client.GetAsync("/api/products", TestContext.CancellationToken);
HttpResponseMessage res = await client.PostAsync("/api/products", Serialize(dto), TestContext.CancellationToken);
HttpResponseMessage res = await client.PutAsync($"/api/products/{id}", Serialize(dto), TestContext.CancellationToken);
// DeleteAsync has no CT overload — use SendAsync:
HttpResponseMessage res = await client.SendAsync(
    new HttpRequestMessage(HttpMethod.Delete, $"/api/products/{id}"),
    TestContext.CancellationToken);
```

### Explicit types — no implicit var in test bodies
The analyzer requires explicit types. Replace `var` with the actual type everywhere except
anonymous object initializers (where `var` is the only option):
```csharp
// Allowed (anonymous object — var required)
var create = new { Name = "Test", Price = 9.99m };

// Must be explicit
HttpResponseMessage res = await client.GetAsync(...);
ApiResponse<JsonElement>? api = await Deserialize<JsonElement>(res);
Guid id = Guid.Parse(...);
string body = await res.Content.ReadAsStringAsync();
JsonElement json = JsonSerializer.Deserialize<JsonElement>(body, _jsonOptions);
```

### One class per context — separate files
Never put Products and Categories tests in the same file.
- `ProductsCharacterizationTests.cs` — Products only
- `CategoriesCharacterizationTests.cs` — Categories only
