# Test Taxonomy

The law. Every test in this repository belongs to exactly one layer. If you are unsure which layer, read the definitions — do not invent a new category.

---

## Backend Layers

### Layer 0 — Architecture Tests (optional but recommended)

```
WHERE:   src/backend/ECommerce.Tests/Architecture/
WHAT:    Automated enforcement of Clean Architecture dependency rules
DEPS:    NetArchTest.Rules or ArchUnitNET
SPEED:   < 1 ms each
RUNS ON: Every save (watch mode) — they are just reflection checks
```

Architecture tests assert that the project reference constraints are not violated. They catch circular dependencies and layer violations the moment they are introduced.

```csharp
// Example: Core must not reference Infrastructure
Types.InAssembly(typeof(Product).Assembly)
    .ShouldNot()
    .HaveDependencyOn("ECommerce.Infrastructure")
    .GetResult()
    .IsSuccessful.ShouldBeTrue();
```

Not yet in this repo. Add as a follow-up when a dedicated Architecture test project is created.

---

### Layer 1 — Domain Tests

```
WHERE:   src/backend/<BC>/ECommerce.<BC>.Tests/Domain/
WHAT:    Aggregate factory methods, domain methods, value object validation, domain events
DEPS:    Zero infrastructure. No EF Core. No Moq. No DI. Pure C#.
         Shouldly for assertions.
SPEED:   < 1 ms each
RUNS ON: Every save (watch mode)
```

A domain test proves that the aggregate or value object enforces its invariants correctly regardless of infrastructure. It instantiates the object directly, calls a method, and asserts on the returned `Result<T>` and on raised domain events.

**Rule:** If your test imports EF Core, MediatR, or any DI container, it is NOT a domain test.

---

### Layer 2 — Application Tests

```
WHERE:   src/backend/<BC>/ECommerce.<BC>.Tests/Application/
WHAT:    Command handlers, query handlers — tested against hand-written fake repos
DEPS:    Fake implementations of domain interfaces (Fakes.cs in each BC).
         No EF Core. No web host. Hand-written fakes over Moq.
         Shouldly for assertions.
SPEED:   < 5 ms each
RUNS ON: Every save (watch mode)
```

An application test proves that a handler correctly orchestrates the domain and persists/reads data through its interfaces. It uses `FakeXRepository` (a `List<T>`-backed implementation) and a `FakeUnitOfWork` that counts `SaveChangesAsync` calls.

**Rule:** If your test creates a `DbContext`, it is NOT an application test.

**Fakes live here:** `src/backend/<BC>/ECommerce.<BC>.Tests/Application/Fakes.cs`

---

### Layer 3 — Integration Tests

```
WHERE:   src/backend/ECommerce.Tests/Integration/
WHAT:    HTTP endpoints tested end-to-end through the web host
DEPS:    TestWebApplicationFactory (SQLite in-memory EF Core + ConditionalTestAuthHandler)
         Shouldly for assertions.
SPEED:   < 500 ms each
RUNS ON: Pre-merge, PR pipeline
```

An integration test proves that the full stack (routing → middleware → controller → handler → EF Core → DB) works together for one HTTP scenario. One test per endpoint per scenario: happy path, validation failure, auth failure.

**Use SQLite, not InMemory EF.** `UseInMemoryDatabase` does not enforce referential integrity. SQLite in-memory does and is equally fast. `Microsoft.EntityFrameworkCore.Sqlite` is already in `ECommerce.Tests.csproj`.

```csharp
// In TestWebApplicationFactory — use SQLite, not InMemory
services.AddDbContext<AppDbContext>(opt =>
    opt.UseSqlite("DataSource=:memory:"));
```

**Rule:** Do not test domain invariants here. Domain rules belong in Layer 1.

**Factory:** `src/backend/ECommerce.Tests/Integration/TestWebApplicationFactory.cs`

---

### Layer 4 — Characterization Tests

```
WHERE:   src/backend/ECommerce.Tests/Integration/
WHAT:    Document observable behavior BEFORE a refactor; verify it survives AFTER
DEPS:    Same as integration tests
TIMING:  Written BEFORE the code changes. Never after.
```

A characterization test exists to catch regressions during migration or refactoring. Written first (old code passes), code changes, same test must still pass.

**Rule:** If the code has already changed when you write the test, it is a regular integration test.

**Naming:** `<Context>CharacterizationTests.cs`

---

### Layer 5 — Projection Sync Tests

```
WHERE:   src/backend/ECommerce.Tests/Integration/
WHAT:    Publish an integration event → assert the read model was updated
DEPS:    Minimal DI scope only. MediatR + target DbContext (InMemory is fine here).
         No web host.
SPEED:   < 100 ms each
```

Each event type gets three tests: insert (new projection), update (existing projection), delete (projection removed).

InMemory EF is acceptable here because projection handlers do not depend on relational constraints — they only read and write their own DbSet.

**Minimal scope pattern:**
```csharp
var services = new ServiceCollection();
services.AddLogging();
services.AddDbContext<TargetDbContext>(opt => opt.UseInMemoryDatabase(uniqueName));
services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(TargetDbContext).Assembly));
```

**Naming:** `<BC><Subject>ProjectionSyncCharacterizationTests.cs`

---

### Layer 6 — Unit Tests (Infrastructure / Cross-cutting)

```
WHERE:   src/backend/ECommerce.Tests/Unit/
WHAT:    Middleware, validators, action filters, extension methods, health checks,
         email service implementations, architecture conventions
DEPS:    Moq acceptable here for infrastructure subjects.
         Shouldly for assertions.
SPEED:   < 10 ms each
```

Tests infrastructure and cross-cutting code that lives outside bounded contexts.

---

## Frontend Layers

### Layer F1 — Component Tests

```
WHERE:   src/frontend/storefront/src/**/<ComponentName>.test.tsx  (co-located)
WHAT:    React component rendering, user interactions, conditional display
DEPS:    Vitest 3 + jsdom + @testing-library/react 16
         renderWithProviders() from src/shared/lib/test/test-utils.tsx
         MSW v2 for HTTP interception (replaces vi.mock for the API layer)
         react-router MemoryRouter in test-utils (not BrowserRouter)
SPEED:   < 50 ms each
RUNS ON: Every save (watch mode)
```

**Rule:** Never `vi.mock` RTK Query hooks. Intercept HTTP with MSW instead. See patterns/frontend-unit-tests.md.

---

### Layer F2 — Hook Tests

```
WHERE:   src/frontend/storefront/src/**/<hookName>.test.ts  (co-located)
WHAT:    Custom hook state transitions, derived values, side effects
DEPS:    Vitest + renderHookWithProviders()
         MSW for hooks that fetch data. preloadedState for hooks that read slice state.
SPEED:   < 20 ms each
```

---

### Layer F3 — Slice / Selector Tests

```
WHERE:   src/frontend/storefront/src/features/<feature>/slices/<slice>.test.ts
WHAT:    Redux reducers, selectors — pure input/output
DEPS:    Vitest only — no DOM, no MSW
SPEED:   < 5 ms each
```

---

### Layer F4 — E2E Tests (Playwright)

```
WHERE:   src/frontend/storefront/e2e/
         API contract tests:  e2e/api-*.spec.ts   (no browser, hits API directly)
         UI flow tests:       e2e/*.spec.ts        (full browser journey)
WHAT:    Complete user journeys through running dev server + real backend
DEPS:    Playwright 1.58+ + backend on :5000 + frontend on :5173
SPEED:   2–10 s each
RUNS ON: Pre-merge, nightly — never in watch mode
```

Use Page Object Model (`e2e/pages/`) for all element selectors. No inline selectors in spec files.
