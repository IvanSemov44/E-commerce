# API Documentation Technical Debt

**Created**: March 7, 2026  
**Status**: Tracked - Not Blocking  
**Priority**: Medium (P2)  
**Estimated Effort**: 6-8 hours

---

## Overview

Controllers are missing comprehensive `[ProducesResponseType]` attributes required by BACKEND_CODING_GUIDE.md. Current compliance: ~35%.

**Impact**: 
- Swagger/OpenAPI documentation incomplete
- Client code generators may miss error handling paths
- API contract not fully explicit

**Not Blocking**: 
- ✅ Build passes (0 errors, 0 warnings)
- ✅ Tests passing
- ✅ Functionality works correctly
- ✅ All endpoints have basic response documentation

---

## Audit Summary

**Date**: March 7, 2026  
**Controllers Reviewed**: 12  
**Issues Found**: 87+ missing status code attributes  
**Compliance Rate**: ~35%

### Required by Guide

Every endpoint must document ALL possible status codes:
- `200/201` - Success
- `400` - Bad Request (invalid parameters)
- `401` - Unauthorized (missing/invalid JWT)
- `403` - Forbidden (insufficient permissions)
- `404` - Not Found (resource doesn't exist)
- `409` - Conflict (concurrency/duplicate/state conflict)
- `422` - Unprocessable Entity (validation failed)
- `500` - Internal Server Error (unhandled exception)

---

## Controllers Prioritized by Impact

### 🔴 Critical Priority (7+ endpoints each)

1. **ReviewsController** - 7 endpoints missing multiple status codes
   - Most endpoints missing: 401, 403, 409, 422, 500
   - File: `src/backend/ECommerce.API/Controllers/ReviewsController.cs`

2. **CategoriesController** - 7 endpoints incomplete
   - Most endpoints missing: 401, 403, 404, 409, 422
   - File: `src/backend/ECommerce.API/Controllers/CategoriesController.cs`

3. **ProductsController** - 7 endpoints incomplete
   - Most endpoints missing: 400, 401, 403, 404, 409, 422
   - File: `src/backend/ECommerce.API/Controllers/ProductsController.cs`

### 🟡 High Priority (5-6 endpoints each)

4. **CartController** - 6 endpoints + authorization structure issue
   - Missing class-level `[Authorize]` attribute
   - Endpoints missing: 401, 409, 422
   - File: `src/backend/ECommerce.API/Controllers/CartController.cs`

5. **PromoCodesController** - 6 endpoints incomplete
   - Most endpoints missing: 401, 403, 404, 409, 422
   - File: `src/backend/ECommerce.API/Controllers/PromoCodesController.cs`

6. **InventoryController** - 6 endpoints + authorization conflicts
   - `[AllowAnonymous]` on endpoints in `[Authorize(Roles)]` controller
   - Missing: 400, 401, 404, 422
   - File: `src/backend/ECommerce.API/Controllers/InventoryController.cs`

7. **ProfileController** - 5 endpoints incomplete
   - Most endpoints missing: 422, 500
   - File: `src/backend/ECommerce.API/Controllers/ProfileController.cs`

8. **WishlistController** - 5 endpoints incomplete
   - Most endpoints missing: 401, 422, 500
   - File: `src/backend/ECommerce.API/Controllers/WishlistController.cs`

### 🟢 Medium Priority (3-4 endpoints each)

9. **AuthController** - 3 endpoints incomplete
   - Missing: 422, 500 on POST endpoints
   - File: `src/backend/ECommerce.API/Controllers/AuthController.cs`

10. **OrdersController** - 3 endpoints incomplete
    - Missing: 400, 403, 404, 409, 422
    - File: `src/backend/ECommerce.API/Controllers/OrdersController.cs`

11. **DashboardController** - 4 endpoints incomplete
    - All missing: 500
    - File: `src/backend/ECommerce.API/Controllers/DashboardController.cs`

### ✅ Low Priority (nearly complete)

12. **PaymentsController** - Only 1 endpoint missing 403
    - Most complete controller
    - File: `src/backend/ECommerce.API/Controllers/PaymentsController.cs`

---

## Common Missing Patterns

### Pattern 1: Missing 422 Unprocessable Entity
**Affected**: 10+ controllers  
**Why Required**: Validation errors via `[ValidationFilter]` return 422, not 400  
**Fix**: Add to all POST/PUT/PATCH endpoints with `[ValidationFilter]`

### Pattern 2: Missing 500 Internal Server Error
**Affected**: Most controllers  
**Why Required**: All endpoints can throw unhandled exceptions  
**Fix**: Add to every endpoint

### Pattern 3: Missing 409 Conflict
**Affected**: Write endpoints (POST/PUT/DELETE)  
**Why Required**: Concurrency conflicts, duplicate keys, state conflicts  
**Fix**: Add to endpoints that modify state

### Pattern 4: Incomplete Security Documentation
**Affected**: CartController, InventoryController  
**Why Required**: Authorization behavior should be explicit  
**Fix**: Use class-level `[Authorize]` with `[AllowAnonymous]` overrides

---

## Implementation Plan

### Phase 1: High-Impact Controllers (3-4 hours)
- [ ] ReviewsController (7 endpoints)
- [ ] CategoriesController (7 endpoints)
- [ ] ProductsController (7 endpoints)
- [ ] CartController (6 endpoints + auth fix)

### Phase 2: Medium-Impact Controllers (2-3 hours)
- [ ] PromoCodesController (6 endpoints)
- [ ] InventoryController (6 endpoints + auth conflicts)
- [ ] ProfileController (5 endpoints)
- [ ] WishlistController (5 endpoints)

### Phase 3: Low-Impact Controllers (1 hour)
- [ ] AuthController (3 endpoints)
- [ ] OrdersController (3 endpoints)
- [ ] DashboardController (4 endpoints)
- [ ] PaymentsController (1 endpoint)

---

## Template for Fixes

Based on BACKEND_CODING_GUIDE.md Template 2:

```csharp
/// <summary>Brief description of what endpoint does</summary>
/// <param name="id">Description of parameter</param>
/// <param name="ct">Cancellation token</param>
/// <remarks>
/// Additional context if needed
/// </remarks>
[HttpGet("{id:guid}")]
[ProducesResponseType(typeof(ApiResponse<EntityDto>), StatusCodes.Status200OK)]
[ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
[ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
[ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status403Forbidden)]
[ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
[ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status409Conflict)]
[ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status422UnprocessableEntity)]
[ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status500InternalServerError)]
[Authorize]  // or [AllowAnonymous]
[Tags("FeatureName")]
public async Task<ActionResult> GetEntity(Guid id, CancellationToken ct) 
{ 
    // ...
}
```

---

## Acceptance Criteria

When this issue is resolved:

- [ ] All controllers have 100% ProducesResponseType coverage
- [ ] Swagger UI shows complete response documentation for every endpoint
- [ ] All endpoints document: 200/201, 400, 401, 403, 404, 409, 422, 500 (as applicable)
- [ ] Authorization attributes consistent (class-level `[Authorize]` with overrides)
- [ ] XML documentation includes parameter descriptions for query/route params
- [ ] CI/CD gate added to enforce ProducesResponseType on new endpoints (optional)

---

## Related Documents

- [BACKEND_CODING_GUIDE.md](../BACKEND_CODING_GUIDE.md) - Section "API Documentation (OpenAPI/Swagger)"
- [BACKEND_CODING_GUIDE.md](../BACKEND_CODING_GUIDE.md) - Section "Template 2: Controller (HTTP Transport)"
- [BACKEND_CODING_GUIDE.md](../BACKEND_CODING_GUIDE.md) - Section "HTTP Status Codes Mapping"

---

## Notes

- **Not blocking production**: API functionality is correct, only documentation incomplete
- **Low regression risk**: Changes are purely additive (attributes only)
- **Can be done incrementally**: Fix high-traffic controllers first
- **Testing not required**: No behavior changes, pure documentation
