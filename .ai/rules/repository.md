# Repository Rules

## Scope

Applies to: repository interfaces in the Domain layer, repository implementations in the Infrastructure layer, read repositories used by query handlers.

---

## Hard Rules

1. **Interface in Domain, implementation in Infrastructure.** `IProductRepository` lives in `{Context}.Domain`. `ProductRepository` lives in `{Context}.Infrastructure`. The Domain project has no reference to EF Core.

2. **Repository returns the full aggregate.** `GetByIdAsync` returns the aggregate root plus all children. Not a partial entity, not a DTO.

3. **No `IQueryable<T>` leaking.** Repository interfaces expose specific named methods (`GetByIdAsync`, `GetBySlugAsync`). The caller does not compose the query. Query shape is defined by the interface.

4. **Repositories do not call `SaveChangesAsync`.** The UnitOfWork owns the commit boundary. A repository calling `SaveChangesAsync` directly bypasses the transaction scope and breaks the UoW contract.

5. **No `UpdateAsync` for EF-tracked aggregates.** When a handler loads an aggregate via `GetByIdAsync`, EF tracks it. Domain method calls mutate the tracked entity. `SaveChangesAsync` persists the diff. Calling `UpdateAsync` before `SaveChangesAsync` is a redundant round-trip and risks missing fields in a manual mapping.

   ```csharp
   // Correct
   var product = await _products.GetByIdAsync(id, ct);
   product.UpdatePrice(newPrice);
   await _uow.SaveChangesAsync(ct);

   // Wrong — extra round-trip, risks missing fields
   var product = await _products.GetByIdAsync(id, ct);
   product.UpdatePrice(newPrice);
   await _products.UpdateAsync(product, ct);
   await _uow.SaveChangesAsync(ct);
   ```

6. **One repository per aggregate root.** No `IProductImageRepository`. Images are accessed through `IProductRepository`. Child entities are only accessed through their aggregate root.

7. **Cross-aggregate references are by ID only.** An `Order` references a `Product` by `Guid ProductId`, never by a navigation property `Product Product`.

---

## Allowed Patterns

- Separate read repository (`IProductReadRepository`) that returns DTOs directly for query handlers.
- `AsNoTracking()` in read repositories — acceptable and encouraged for query-only paths.
- Pagination, filtering, and sorting parameters on read repository methods.

---

## Forbidden Patterns

- `IQueryable<T>` returned from any repository method.
- Child entity repositories (`IOrderLineRepository`, `IProductImageRepository`).
- Navigation properties to other aggregates (only ID references allowed).
- `SaveChangesAsync` called inside any repository method.
- `UpdateAsync` called on an EF-tracked aggregate before `SaveChangesAsync`.

---

## Required Tests

- Unit test per command handler confirming the correct repository method is called.
- Integration test confirming the aggregate is saved and reloaded correctly (including children and value objects).

---

## Required Evidence (before merge)

- No `IQueryable` in any repository interface.
- No `SaveChangesAsync` calls inside repository implementations.
- Integration test covers the full save-and-reload round trip.

---

## Definition of Done

All hard rules pass. Required tests are green. Required evidence is attached in the PR.
