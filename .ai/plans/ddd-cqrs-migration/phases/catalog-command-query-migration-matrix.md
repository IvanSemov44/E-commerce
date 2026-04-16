# Catalog Command Ack + Redirect Matrix

Updated: 2026-04-15
Owner: @ivans

## Goal
Move Catalog write endpoints to a stricter CQRS contract:
- Commands return write outcome only (`Result` or minimal ack like `Id`).
- Controllers do not run mediator follow-up queries in write actions.
- Controllers redirect successful write actions to the existing resource GET endpoint.
- Keep DDD invariants inside aggregates/value objects.
- Keep transactional safety: no commit on failed `Result`, no side-effects before failure checks.

## Decision Rules
1. `Create` and `Update` commands should return minimal write outcome, not read DTOs.
2. Controller does `command -> redirect` to canonical GET action (no in-action follow-up query).
3. `Delete/Activate/Deactivate/Stock adjust` stay `Result` unless UI explicitly requires fresh resource shape.
4. Projection publishers are side-effects and must execute only after all failure branches are cleared.

## Current Catalog Command Matrix

| Endpoint | Command | Current Return | Target Return | Controller Success Behavior | Priority |
|---|---|---|---|---|---|
| `POST /api/categories` | `CreateCategoryCommand` | `Result<Guid>` | `Result<Guid>` | `RedirectToAction(GetCategoryById)` | Done |
| `PUT /api/categories/{id}` | `UpdateCategoryCommand` | `Result<Guid>` | `Result<Guid>` | `RedirectToAction(GetCategoryById)` | Done |
| `DELETE /api/categories/{id}` | `DeleteCategoryCommand` | `Result` | Keep `Result` | None | Keep |
| `POST /api/products` | `CreateProductCommand` | `Result<Guid>` | `Result<Guid>` | `RedirectToAction(GetProductById)` | Done |
| `PUT /api/products/{id}` | `UpdateProductCommand` | `Result<Guid>` | `Result<Guid>` | `RedirectToAction(GetProductById)` | Done |
| `PUT /api/products/{id}/price` | `UpdateProductPriceCommand` | `Result<Guid>` | `Result<Guid>` | `RedirectToAction(GetProductById)` | Done |
| `POST /api/products/{id}/images` | `AddProductImageCommand` | `Result<Guid>` | `Result<Guid>` | `RedirectToAction(GetProductById)` | Done |
| `POST /api/products/{id}/images/{imageId}/primary` | `SetPrimaryImageCommand` | `Result<Guid>` | `Result<Guid>` | `RedirectToAction(GetProductById)` | Done |
| `PUT /api/products/{id}/stock` | `UpdateProductStockCommand` | `Result` | Keep `Result` | None | Keep |
| `POST /api/products/{id}/activate` | `ActivateProductCommand` | `Result` | Keep `Result` | None | Keep |
| `POST /api/products/{id}/deactivate` | `DeactivateProductCommand` | `Result` | Keep `Result` | None | Keep |
| `DELETE /api/products/{id}` | `DeleteProductCommand` | `Result` | Keep `Result` | None | Keep |

## Remaining Work From This Plan

### 1) Integration and characterization test alignment (pending)
- `src/backend/ECommerce.Tests/Integration/CategoriesControllerTests.cs`
- `src/backend/ECommerce.Tests/Integration/CategoriesCharacterizationTests.cs`
- `src/backend/ECommerce.Tests/Integration/ProductsControllerTests.cs`
- `src/backend/ECommerce.Tests/Integration/ProductsCharacterizationTests.cs`
- Update expectations for write endpoints to redirect behavior (`302 Found` + `Location` header), instead of inline DTO payload assertions.

### 2) End-to-end contract verification (pending)
- Run focused integration test suites for categories/products after test updates.
- Validate that redirected GET endpoints return expected `ApiResponse<T>` payloads when followed by clients.

### 3) Optional contract refinement (decision)
- Keep raw `Guid` as the ack payload, or introduce `WriteAckDto` for explicit ack semantics.
- Current implementation already uses `Result<Guid>` consistently.

## Test Impact Matrix

### Catalog app/unit tests
- `src/backend/Catalog/ECommerce.Catalog.Tests/Application/CommandHandlerTests.cs`
  - Update expected handler return payloads from DTO to ack.
  - Keep invariant/failure path assertions.

### API integration tests
- `src/backend/ECommerce.Tests/Integration/CategoriesControllerTests.cs`
- `src/backend/ECommerce.Tests/Integration/CategoriesCharacterizationTests.cs`
- `src/backend/ECommerce.Tests/Integration/ProductsControllerTests.cs`
- `src/backend/ECommerce.Tests/Integration/ProductsCharacterizationTests.cs`
  - Update assertions where create/update endpoints currently expect inline DTO payload from write endpoints.
  - Verify redirect status and `Location` target.
  - Optionally follow redirect and verify final resource payload from GET endpoint.

### Regression tests to keep
- Transaction rollback on failed `Result`/`Result<T>`:
  - `src/backend/ECommerce.API/Behaviors/TransactionBehavior.cs` (add dedicated behavior tests if missing).
- No side-effects before late failures:
  - handlers that publish projections after all failure checks.

## Execution Order (Recommended)
1. Update categories integration/characterization write assertions.
2. Update products integration/characterization write assertions.
3. Run focused suites for updated tests.
4. Run wider backend verification before merge.

## Definition Of Done Per Slice
1. Command returns ack/result only (no read DTO from command handler).
2. Controller does command then redirect to canonical GET action.
3. Build passes.
4. Targeted tests pass.
5. Characterization/integration tests for touched endpoints pass.

## AI Pairing Prompts (Copy/Paste)

### Prompt 1: Implement one slice
"Migrate only [Slice Name] to command-ack + controller redirect.
Constraints:
- Do not change unrelated endpoints.
- Keep current HTTP status codes and ApiResponse envelope.
- Update only required tests.
- Run targeted tests and report exact failures."

### Prompt 2: Review after changes
"Review this diff for DDD/CQRS boundary violations.
Focus on:
1) command return contract,
2) handler purity/orchestration,
3) side-effect ordering,
4) rollback behavior on failed Result.
Findings first, ordered by severity."
