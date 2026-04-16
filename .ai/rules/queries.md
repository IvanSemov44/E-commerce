# Query Handler Rules

## Scope

Applies to: MediatR query handlers, read repositories, projections, read models, caching in the query path.

---

## Hard Rules

1. **Queries never modify data.** A query handler does not call `SaveChangesAsync`, does not call aggregate mutation methods, and does not enqueue events or messages.

2. **Queries return DTOs, not entities.** Query handlers return data transfer objects. They never return domain aggregates or EF-tracked entities to callers.

3. **Queries bypass aggregates.** Query handlers use optimized read paths: projections, raw SQL, read-only DbContext, or read repositories. They do not load a full aggregate and then map it.

4. **One handler per query.** Each `IRequest<T>` query has exactly one `IRequestHandler<TQuery, TResponse>`.

5. **Queries also return `Result<T>`.** For consistency with the command side. Return `Result.Fail` for not-found or permission failures; do not throw.

6. **Cache is populated and read at the query handler level.** Handlers check the cache first, call the read path on miss, and populate the cache. Commands never cache their results.

7. **Cache invalidation is done in domain event handlers, not in command handlers.** When an aggregate changes state, an event handler evicts the relevant cache keys. Command handlers do not touch the cache directly.

8. **Cache stores read models (DTOs), not aggregates.** Never serialize and cache a domain aggregate.

---

## Allowed Patterns

- Raw SQL or Dapper for query handlers where ORM query performance is a concern.
- `AsNoTracking()` on any EF query used in a query handler.
- Separate `IProductReadRepository` returning DTOs, used only by query handlers.
- Cache-aside pattern: check cache → on miss, query DB → populate cache → return.

---

## Forbidden Patterns

- Loading a full aggregate to map it to a DTO in a query handler.
- `SaveChangesAsync` or any mutation in a query handler.
- Returning EF-tracked entities from a query handler.
- Caching aggregates or EF entities.
- Cache invalidation inside a command handler (must go through event handlers instead).

---

## Required Tests

- Unit test: query handler returns correct DTO for a given read repository response.
- Unit test: cache hit path returns cached value without hitting the read repository.
- Integration test: query returns expected data from a real database state.

---

## Required Evidence (before merge)

- No `SaveChangesAsync` or mutation in query handler code.
- Cache is populated with a DTO, not a domain entity.
- Cache invalidation wired to a domain event handler, not a command handler.

---

## Definition of Done

All hard rules pass. Required tests are green. Required evidence is attached in the PR.
