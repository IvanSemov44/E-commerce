# Test Taxonomy

The law. Every test in this repository belongs to exactly one layer. If you are unsure which layer, read the definitions — do not invent a new category.

---

## Backend Layers

### Layer 1 — Domain Tests

```
WHERE:   src/backend/<BC>/ECommerce.<BC>.Tests/Domain/
WHAT:    Aggregate factory methods, domain methods, value object validation, domain events
DEPS:    Zero infrastructure. No EF Core. No Moq. No DI. Pure C#.
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
         No EF Core. No web host. Prefer hand-written fakes over Moq.
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
DEPS:    TestWebApplicationFactory (InMemory EF Core + ConditionalTestAuthHandler)
SPEED:   < 500 ms each
RUNS ON: Pre-merge, PR pipeline
```

An integration test proves that the full stack (routing → middleware → controller → handler → EF Core → InMemory DB) works together for one HTTP scenario. One test per endpoint per scenario: happy path, validation failure, auth failure.

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
DEPS:    Minimal DI scope only. MediatR + target DbContext (InMemory). No web host.
SPEED:   < 100 ms each
```

Each event type gets three tests: insert (new projection), update (existing projection), delete (projection removed).

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
DEPS:    Moq acceptable here for infrastructure subjects
SPEED:   < 10 ms each
```

Tests infrastructure and cross-cutting code that lives outside bounded contexts.

---

## Frontend Layers

### Layer F1 — Component Tests

```
WHERE:   src/frontend/storefront/src/**/<ComponentName>.test.tsx  (co-located)
WHAT:    React component rendering, user interactions, conditional display
DEPS:    Vitest + jsdom + @testing-library/react
         renderWithProviders() from src/shared/lib/test/test-utils.tsx
         API layer mocked via vi.mock (never real HTTP)
SPEED:   < 50 ms each
RUNS ON: Every save (watch mode)
```

**Rule:** Never make real HTTP calls. Mock RTK Query endpoints or use MSW.

---

### Layer F2 — Hook Tests

```
WHERE:   src/frontend/storefront/src/**/<hookName>.test.ts  (co-located)
WHAT:    Custom hook state transitions, derived values, side effects
DEPS:    Vitest + renderHookWithProviders() from src/shared/lib/test/test-utils.tsx
SPEED:   < 20 ms each
```

RTK Query hooks are tested by injecting state via `preloadedState`.

---

### Layer F3 — Slice / Selector Tests

```
WHERE:   src/frontend/storefront/src/features/<feature>/slices/<slice>.test.ts
WHAT:    Redux reducers, selectors, RTK Query cache behaviour
DEPS:    Vitest only — no DOM
SPEED:   < 5 ms each
```

---

### Layer F4 — E2E Tests (Playwright)

```
WHERE:   src/frontend/storefront/e2e/
         API contract tests:  e2e/api-*.spec.ts   (no browser, hits API directly)
         UI flow tests:       e2e/*.spec.ts        (full browser journey)
WHAT:    Complete user journeys through running dev server + real backend
DEPS:    Playwright + backend on :5000 + frontend on :5173
SPEED:   2–10 s each
RUNS ON: Pre-merge, nightly — never in watch mode
```

Use Page Object Model (`e2e/pages/`) for all element selectors. No inline selectors in spec files.
