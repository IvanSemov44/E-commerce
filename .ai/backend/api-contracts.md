# Backend API Contracts Standard

Updated: 2026-03-08
Owner: @ivans

## Purpose
Keep response envelopes and endpoint semantics consistent across controllers.

## Core Rules
1. Controllers return consistent API response envelope shape.
2. Keep status code semantics aligned with business outcomes.
3. Paginated endpoints must return page metadata.
4. Keep controller logic thin and delegate business logic to services.

## Status Code Mapping (Baseline)
- `200/201`: successful read/write operation.
- `400`: invalid request parameters or business request state.
- `401`: unauthenticated request.
- `403`: authenticated but forbidden.
- `404`: requested resource not found.
- `409`: conflict (concurrency/idempotency/business conflict).
- `422`: semantic validation failure.
- `500`: unexpected infrastructure/server failure.

## Real Code References
- Controllers: `src/backend/ECommerce.API/Controllers/`
- Response helpers/models: `src/backend/ECommerce.API/Helpers/`
- Pagination normalization: `src/backend/ECommerce.API/Helpers/PaginationRequestNormalizer.cs`

## Common Mistakes
- Returning raw exceptions/messages from controllers.
- Inconsistent success envelope shape across endpoints.
- Performing business decision logic directly in controller actions.

## Controller Contract Checklist
- [ ] Endpoint has explicit `ProducesResponseType` entries for expected outcomes.
- [ ] Status code mapping is consistent with business outcome categories.
- [ ] Controller returns standard response envelope.
- [ ] Validation and authorization behavior are explicit in endpoint metadata.
