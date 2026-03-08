# Backend Performance Standard

Updated: 2026-03-08
Owner: @ivans

## Purpose
Prevent common performance regressions in queries, mapping, and service orchestration.

## Core Rules
1. Use pagination for list endpoints by default.
2. Cap page size to prevent heavy payloads.
3. Use non-tracking queries for read-only scenarios.
4. Select only required data for high-traffic queries.
5. Avoid N+1 patterns by controlling includes/joins.

## Real Code References
- Product pagination and limits: `src/backend/ECommerce.Application/Services/ProductService.cs`
- Generic repository tracking option: `src/backend/ECommerce.Infrastructure/Repositories/Repository.cs`
- Product repository query patterns: `src/backend/ECommerce.Infrastructure/Repositories/ProductRepository.cs`

## Practical Guidance
- Keep DTOs lean for list pages; fetch details only when required.
- Validate sort/filter combinations and ensure predictable defaults.
- Add targeted indexes through migrations for repeated heavy filters.

## Common Mistakes
- Unbounded list endpoints.
- Returning large object graphs when only summary fields are needed.
- Defaulting to tracked entity queries in read APIs.

## Checklist
- [ ] Endpoint has page/pageSize with a max cap.
- [ ] Read-only queries are non-tracking where possible.
- [ ] Query shape matches UI need (summary vs detail).
- [ ] High-traffic filter fields are indexed.
