# Backend Query Patterns Standard

Updated: 2026-03-08
Owner: @ivans

## Purpose
Keep query behavior predictable, efficient, and aligned across services and controllers.

## Core Rules
1. Normalize pagination input at API boundary.
2. Enforce page-size caps in service layer.
3. Use non-tracking queries for read paths where mutation is not required.
4. Push filtering/sorting/pagination to database queries, not in-memory.
5. Include related data only when required by DTO/view needs.
6. Avoid N+1 query patterns by explicit include/projection strategy.
7. Prefer projection/select for list endpoints to reduce payload and tracking overhead.

## Real Code References
- Pagination normalization helper: `src/backend/ECommerce.API/Helpers/PaginationRequestNormalizer.cs`
- Product query and page-size caps: `src/backend/ECommerce.Application/Services/ProductService.cs`
- Category DB-level pagination and no-tracking reads: `src/backend/ECommerce.Application/Services/CategoryService.cs`
- Repository tracking toggle: `src/backend/ECommerce.Infrastructure/Repositories/Repository.cs`

## Practical Guidance
- Normalize `page` and `pageSize` in controllers before calling services.
- Re-validate caps in services for defense in depth.
- Prefer projections and lean DTO mapping for list endpoints.

## Common Mistakes
- Materializing full tables before pagination.
- Applying sorting/paging after `ToListAsync`.
- Using tracked queries in high-volume read endpoints by default.
- Iterating parent entities and issuing per-item child queries (N+1).
- Returning full aggregate graphs for lightweight list views.

## Checklist
- [ ] Controller normalizes pagination values.
- [ ] Service applies max page-size cap.
- [ ] Query executes with DB-level filter/sort/pagination.
- [ ] Tracking mode matches endpoint intent.
