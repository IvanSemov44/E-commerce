# DTO and Dead Code Cleanup - 2026-05-27

## Scope
- Slow scan pass focused on backend DTO usage and obvious dead code.
- Conservative rule: remove only symbols with zero in-repo references outside their own definition.
- Verify build on affected projects after cleanup.

## Method
1. Enumerate all C# DTO files under `src/backend/**/DTOs/`.
2. Count symbol usage across backend C# files (excluding `bin/`, `obj/`, `Migrations/`).
3. Manually verify low-usage symbols.
4. Delete only proven-unused code and rebuild affected projects.

## Folder-by-Folder Findings

### 1) `src/backend/ECommerce.Contracts/DTOs/Auth`
- Deleted dead DTOs:
  - `AuthResponseDto.cs`
  - `TokenResponseDto.cs`
- Reason: no C# references in repository.

### 2) `src/backend/ECommerce.Contracts/DTOs/Common`
- Deleted dead DTO:
  - `CategoryDetailDto.cs`
- Reason: no C# references in repository.
- Note: frontend has its own TypeScript `CategoryDetailDto` interface; this is unrelated and unchanged.

### 3) `src/backend/ECommerce.Contracts/DTOs/Emails`
- Deleted dead DTO:
  - `LowStockAlertEmailDto.cs`
- Reason: no C# references in repository.

### 4) `src/backend/Promotions/ECommerce.Promotions.Application/DTOs`
- Removed dead class from existing file:
  - Removed `ValidatePromoCodeDto` from `PromoCodeDto.cs`
- Reason: no C# references in repository.

### 5) `src/backend/Reviews/ECommerce.Reviews.Application/DTOs`
- `ReviewMappingExtensions.cs` was tested as candidate but restored.
- Reason: handlers use `ToDto` and `ToDetailDto` extension methods, even though class name itself is not referenced.

### 6) `src/frontend/storefront`
- Type-check scan performed (`tsc --noEmit`), no immediate dead-code breakages surfaced.
- No deletions in this pass to avoid risky false positives.

### 7) `src/frontend/admin`
- Type-check scan performed (`tsc --noEmit`), no immediate dead-code breakages surfaced.
- No deletions in this pass to avoid risky false positives.

## Validation
- Built affected backend projects:
  - `ECommerce.Contracts.csproj` -> success
  - `ECommerce.Promotions.Application.csproj` -> success
  - `ECommerce.Reviews.Application.csproj` -> success
- Full solution build was attempted during scan and exposed no new errors after restoring review mappings.

## Remaining Low-Usage Candidates (Not Removed Yet)
- `BulkStockUpdateItem` in Inventory DTOs (used only within request DTO shape).
- Several request DTOs with low references (2-4 files) are likely real API boundary types, not dead code.
- `ReviewMappingExtensions` appears low-usage by class name only, but methods are actively used.

## Recommended Next Pass
1. Add an allowlist for boundary DTOs (API contracts, request/response, serialization models).
2. Run dead-code scan per bounded context (`Catalog`, `Ordering`, `Promotions`, `Inventory`, `Identity`, `Reviews`, `Shopping`).
3. For each candidate, verify symbol usages with method-level checks before deletion.
4. After each folder cleanup, run targeted build/tests for that bounded context.
