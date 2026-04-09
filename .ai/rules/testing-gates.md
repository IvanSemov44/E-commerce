# Testing Gates

## Scope

Applies to: decisions about which test type to write, required test coverage per layer, evidence required before merge.

---

## Layer-to-Test Assignment

| Layer | Test type | What to cover |
|-------|-----------|---------------|
| Domain (aggregates, value objects) | Unit test — no mocks needed | Aggregate methods, value object validation, domain events raised |
| Command handlers | Unit test with mocked repositories | Correct aggregate method called, correct repo called, result mapped correctly |
| Query handlers | Unit test with mocked read repo | Cache hit returns cached value; cache miss hits read repo and populates cache |
| FluentValidation validators | Unit test | Valid input passes, each invalid case returns the correct error message |
| Repositories + EF mappings | Integration test (real DB) | Full save-and-reload round trip including children and value objects |
| API endpoints | Integration test (real DB, full pipeline) | Valid request → correct response; invalid input → 400; unauthorized → 401/403 |
| Domain event dispatch | Integration test | Commit → event dispatched → consumer effect observed |
| Cross-context projections | Integration test | Source event arrives → projection updated correctly |

---

## Hard Rules

1. **Domain layer tests are pure unit tests.** No EF, no DbContext, no HTTP. Instantiate aggregates and value objects directly.

2. **Handler unit tests mock repositories.** Handlers are tested with `Mock<IRepository>`. Do not use InMemory EF or real databases for handler unit tests.

3. **InMemory EF does not enforce unique constraints.** `HasIndex(...).IsUnique()` is silently ignored by the InMemory provider. Unique constraint validation requires Testcontainers with a real database. Handler existence checks (e.g. `SlugExistsAsync`) must be tested explicitly — do not rely on a DB constraint to catch them.

4. **Integration tests hit a real database.** Use Testcontainers (Postgres) for all integration tests that need to verify persistence behavior, constraint enforcement, or migration correctness.

5. **Tests are not deleted to make a build pass.** If a test fails after a change, fix the code or fix the test — with justification. Deleting a failing test is a blocking PR violation.

6. **Do not test EF configurations directly.** EF configuration correctness is covered by integration tests that save and reload aggregates. A separate test that reads `ModelBuilder` state is not required and adds noise.

7. **Characterization tests are written before migration, not after.** Before migrating a bounded context, integration and E2E characterization tests must be green. They define the behavior contract the migrated code must preserve.

---

## Minimum Coverage Gate (per layer touched by the PR)

| Layer | Minimum |
|-------|---------|
| Aggregate methods | All public domain methods have at least one happy-path and one failure-path unit test |
| Value objects | All `Create` factory methods have at least one invalid input test |
| Command handlers | Happy path + at least one failure path (not-found, unauthorized) |
| Query handlers | Happy path + cache hit path |
| API endpoints | Happy path + validation failure + auth failure |
| Event dispatch | At least one integration test confirming commit-to-event flow |

---

## Required Evidence (before merge)

- All tests in the touched layer are green.
- No tests were deleted as part of the change.
- If InMemory was replaced with Testcontainers: confirm unique constraints are now enforced.
- If a new domain event was added: integration test for commit-to-event flow is present.
- If a new endpoint was added: integration test for happy path and auth failure is present.

---

## Definition of Done

All hard rules pass. Coverage minimums are met. Required evidence is attached in the PR. Build is green.
