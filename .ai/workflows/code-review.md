# Code Review Checklist

Run through this checklist when reviewing any PR. Stop and request changes if any item fails.

---

## Architecture

- [ ] No business logic in controllers — controllers call services and return `ApiResponse<T>`, nothing more
- [ ] No EF Core / database references in `ECommerce.Application` or `ECommerce.Core`
- [ ] Services inject `IUnitOfWork`, not individual repositories
- [ ] New infrastructure code (DB, email, HTTP) goes in `ECommerce.Infrastructure`, not Application
- [ ] Architecture tests still pass: `dotnet test --filter "Category=Architecture"`

## Services & business logic

- [ ] Services return `Result<T>` for all expected outcomes — no throwing custom exceptions for business failures
- [ ] Every new error case uses a constant from `ErrorCodes.cs` — no inline error strings
- [ ] `SaveChangesAsync()` is not called in repositories — only in `UnitOfWork`
- [ ] Multi-step operations use `UnitOfWork.BeginTransactionAsync()` (e.g. order creation touches Products + Orders)
- [ ] Concurrency-sensitive operations (cart, order, promo code) handle `DbUpdateConcurrencyException`

## API layer

- [ ] Every new endpoint has `[ProducesResponseType]` for every possible status code
- [ ] Write endpoints (POST/PUT/PATCH) have `[ValidationFilter]` attribute
- [ ] New DTO has a corresponding FluentValidation validator in `ECommerce.Application/Validators/`
- [ ] All async controller methods accept and pass `CancellationToken`

## Frontend

- [ ] No `fetch()` or `axios` calls directly in React components — use RTK Query
- [ ] Server state (API data) lives in RTK Query, not in Redux slices
- [ ] New RTK Query mutations have `invalidatesTags` set to refresh dependent queries
- [ ] No `any` types introduced — TypeScript strict mode must stay clean
- [ ] New components use CSS Modules, not inline styles

## Tests

- [ ] New service code has unit tests covering happy path + at least one failure path
- [ ] New endpoint has at least one integration test (happy path)
- [ ] New validators have tests for valid + invalid cases
- [ ] No mocked database in integration tests — they use real DB via `TestWebApplicationFactory`

## Security

- [ ] No secrets or credentials in code or config files
- [ ] User-scoped data checks `ICurrentUserService.UserId` (a customer can't read another's orders)
- [ ] No raw SQL strings that could allow injection (use EF Core parameterised queries)
- [ ] New endpoints that should be admin-only have `[Authorize(Roles = "Admin")]`

## Performance

- [ ] New list endpoints accept `PaginationParameters` — no unbounded collections returned
- [ ] Read-only queries use `.AsNoTracking()`
- [ ] Navigation properties loaded with `.Include()`, not accessed lazily in loops (N+1 check)

## Documentation

- [ ] If a new pattern was introduced, the relevant `.ai/` doc is updated in this PR
- [ ] If a new anti-pattern was discovered, it's added to `.ai/reference/common-mistakes.md`
- [ ] `CHANGELOG.md` has an entry under `[Unreleased]` for user-visible changes
- [ ] New environment variables are documented in `docs/environments.md`
- [ ] New error codes are documented in `docs/error-codes-reference.md`

---

## Quick red flags (request changes immediately)

| What you see | Problem |
|---|---|
| `_context.SaveChangesAsync()` in a repository | Violates UnitOfWork pattern |
| `throw new BusinessException(...)` in a service | Use `Result.Failure()` instead |
| `using Microsoft.EntityFrameworkCore` in Application layer | Layer violation |
| `await fetch(...)` in a React component | Use RTK Query |
| A list endpoint with no pagination params | Will break under load |
| `.ToList()` followed by `.Select()` in a loop | Classic N+1 |
| A new `const string errorCode = "..."` inline | Must go in `ErrorCodes.cs` |
| Missing `CancellationToken` on a new async method | Non-cancellable operation |
