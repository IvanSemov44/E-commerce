# Priority 3: Implementation Gaps Guide

## Current Status
- **Test Suite:** 451/489 passing (92.2%)
- **Remaining Failures:** 38 tests (9.2%)  
- **Gap to 95%:** Need 9 more passing tests (only 1.8% more!)
- **Build Status:** 0 errors ✅

---

## Failing Tests by Category

### 1. Payment Operations (5 failures) - EASY FIX
These are all in `PaymentsControllerTests`:
- `ProcessPayment_WithValidData_ReturnsOk` - Expects 200 OK, getting error
- `ProcessPayment_WithCreditCard_ReturnsOk` - Missing implementation
- `ProcessPaymentWebhook_WithValidPayload_ReturnsSuccessful` - Webhook not complete
- `RefundPayment_WithValidOrderId_ReturnsSuccessOrNotFound` - Missing refund logic
- `RefundPayment_WithNegativeAmount_ReturnsBadRequest` - Invalid amount handling

**Root Cause:** PaymentsController endpoints need to be completed/fixed

**File to Fix:** 
```
src/backend/ECommerce.API/Controllers/PaymentsController.cs
src/backend/ECommerce.Application/Services/PaymentService.cs
```

### 2. Profile Operations (4 failures) - MEDIUM FIX  
These are in `ProfileControllerTests`:
- `UpdateProfile_Unauthenticated_ReturnsUnauthorized` - Auth check needed
- `ChangePassword_WithCorrectOldPassword_ReturnsOk` - Password change incomplete
- `ChangePassword_WithMismatchedNewPasswords_ReturnsBadRequest` - Validation needed
- `GetPreferences_Unauthenticated_ReturnsUnauthorized` - Auth check needed

**Root Cause:** Profile endpoints missing authorization checks or incomplete implementation

**Files to Fix:**
```
src/backend/ECommerce.API/Controllers/ProfileController.cs
src/backend/ECommerce.Application/Services/ProfileService.cs
```

### 3. Cart Operations (5 failures) - EASY FIX
These are in `CartControllerTests` and tests calling cart operations:
- `GetCart_WithUnauthenticatedUser_ReturnsUnauthorized` - Auth check missing
- `UpdateCartItem_WithZeroQuantity_ReturnsBadRequest` - Validation needed
- `RemoveItemFromCart_WithUnauthenticated_ReturnsUnauthorized` - Auth check missing
- `ClearCart_WithAuthenticatedUser_ReturnsNoContent` - Not implemented
- Guest cart operations - Not fully implemented

**Root Cause:** Cart endpoints need auth decorators and validation

**Files to Fix:**
```
src/backend/ECommerce.API/Controllers/CartController.cs
src/backend/ECommerce.Application/Services/CartService.cs
```

### 4. Promo Code Operations (4 failures) - MEDIUM FIX
These are in `PromoCodesControllerTests`:
- `CreatePromoCode_WithAdminAndValidData_ReturnsCreated` - Missing endpoint or auth
- `ValidatePromoCode_Unauthenticated_ReturnsUnauthorized` - Auth needed
- `GetActiveCodes_ReturnsOk` - Endpoint not returning proper data
- `DeletePromoCode_WithAdminAndExistingCode_ReturnsOkOrNoContent` - Delete logic broken

**Files to Fix:**
```
src/backend/ECommerce.API/Controllers/PromoCodesController.cs
src/backend/ECommerce.Application/Services/PromoCodeService.cs
```

### 5. Wish List Operations (4 failures) - EASY FIX
These are in `WishlistControllerTests`:
- `AddToWishlist_Unauthenticated_ReturnsUnauthorized` - Auth check missing
- `RemoveFromWishlist_Unauthenticated_ReturnsUnauthorized` - Auth check missing
- `CheckItemInWishlist_Unauthenticated_ReturnsUnauthorized` - Auth check missing

**Root Cause:** Missing `[Authorize]` decorators on endpoints

**Files to Fix:**
```
src/backend/ECommerce.API/Controllers/WishlistController.cs
```

### 6. Review Operations (2 failures) - EASY FIX
These are in `ReviewsControllerTests`:
- `CreateReview_Unauthenticated_ReturnsUnauthorized` - Auth check missing
- `DeleteReview_Unauthenticated_ReturnsUnauthorized` - Auth check missing

**Root Cause:** Missing `[Authorize]` decorators

**Files to Fix:**
```
src/backend/ECommerce.API/Controllers/ReviewsController.cs
```

### 7. Dashboard Operations (4 failures) - MEDIUM FIX
These are in `DashboardControllerTests`:
- `GetUserStats_WithCustomerRole_ReturnsForbidden` - Should return Forbidden
- `GetOrderStats_WithCustomerRole_ReturnsForbidden` - Should return Forbidden
- `GetRevenueStats_WithCustomerRole_ReturnsForbidden` - Should return Forbidden
- Missing admin-only decorators

**Root Cause:** Dashboard endpoints need `[Authorize(Roles = "Admin")]`

**Files to Fix:**
```
src/backend/ECommerce.API/Controllers/DashboardController.cs
```

### 8. Inventory Operations (3 failures) - MEDIUM FIX
These are in `InventoryControllerTests`:
- `UpdateProductStock_WithNegativeQuantity_ReturnsBadRequest` - Validation needed
- `UpdateProductStock_WithCustomerRole_ReturnsForbidden` - Role restriction needed
- `BulkUpdateStock_WithCustomerRole_ReturnsForbidden` - Role restriction needed

**Root Cause:** Missing role-based authorization and validation

**Files to Fix:**
```
src/backend/ECommerce.API/Controllers/InventoryController.cs
```

### 9. Authentication Token Issues (7 failures) - HARD FIX
These are in `AuthControllerTests`:
- `Login_WithWrongPassword_ReturnsUnauthorized` - Auth logic issue
- `Login_AllowsAnonymousAccess` - Endpoint access control
- `RefreshToken_WithInvalidToken_ReturnsUnauthorized` - Token validation
- `RefreshToken_WithEmptyToken_ReturnsUnauthorized` - Validation

**Root Cause:** Token validation logic needs review

**Files to Fix:**
```
src/backend/ECommerce.API/Controllers/AuthController.cs
src/backend/ECommerce.Application/Services/AuthService.cs
```

### 10. Orders Operations (3 failures) - MEDIUM FIX
These are in `OrdersControllerTests`:
- `GetOrderById_WithNonexistentOrder_ReturnsNotFound` - Not returning 404
- `GetOrderByNumber_WithNonexistentOrderNumber_ReturnsNotFound` - Not returning 404

**Root Cause:** Missing null checks, returning wrong HTTP codes

**Files to Fix:**
```
src/backend/ECommerce.API/Controllers/OrdersController.cs
```

---

## Fix Strategy - Recommended Order

### Phase A: Quick Wins (10 min, ~8 tests)
Fix missing `[Authorize]` decorators:
1. WishlistController - Add `[Authorize]` to Add/Remove/Check methods
2. ReviewsController - Add `[Authorize]` to Create/Delete methods  
3. CartController - Add `[Authorize]` to protected endpoints

### Phase B: Role-Based Fixes (15 min, ~6 tests)
Add role restrictions:
1. DashboardController - Add `[Authorize(Roles = "Admin")]` to stats endpoints
2. InventoryController - Add `[Authorize(Roles = "Admin")]` to update methods
3. PromoCodesController - Add `[Authorize(Roles = "Admin")]` to create/delete

### Phase C: Response Code Fixes (20 min, ~5 tests)
Ensure correct HTTP responses:
1. OrdersController - Return 404 for nonexistent orders instead of 200
2. CartController - Return proper error codes
3. PromoCodesController - Get active codes should return proper response

### Phase D: Implementation Completion (30+ min, ~15 tests)
Complete partial implementations:
1. PaymentsController - Complete payment processing logic
2. ProfileController - Complete password change, preferences
3. CartService - Guest cart support
4. AuthController - Token refresh and validation

---

## Quick Fix Examples

### Add Authorize Decorator
```csharp
// Before
public async Task<IActionResult> AddToWishlist(Guid productId)
{
    ...
}

// After  
[Authorize]
public async Task<IActionResult> AddToWishlist(Guid productId)
{
    ...
}
```

### Add Role Restriction
```csharp
// Before
[Authorize]
public async Task<IActionResult> CreatePromoCode([FromBody] CreatePromoCodeDto dto)
{
    ...
}

// After
[Authorize(Roles = "Admin")]
public async Task<IActionResult> CreatePromoCode([FromBody] CreatePromoCodeDto dto)
{
    ...
}
```

### Fix Response Codes
```csharp
// Before - Returns 200 with null data
var order = await _orderService.GetOrderByIdAsync(orderId);
return Ok(order); // Returns 200 OK even when null

// After - Returns 404 when not found
var order = await _orderService.GetOrderByIdAsync(orderId);
if (order == null)
    return NotFound();
return Ok(order);
```

---

## Next Session Plan

**Estimated Time to 95%:** 45-90 minutes

### Step 1: Quick Wins (10 min)
- Add `[Authorize]` decorators to WishlistController and ReviewsController
- **Expected Gain:** +6 tests

### Step 2: Role Restrictions (15 min)
- Add `[Authorize(Roles = "Admin")]` to DashboardController and InventoryController
- **Expected Gain:** +4 tests

### Step 3: Response Code Fixes (15 min)
- Fix OrdersController to return 404 for nonexistent orders
- Fix CartController response codes
- **Expected Gain:** +3 tests

**At this point, you should be at 464+/489 (94.9%+)**

### Step 4: Implementation Gaps (30-60 min, if needed)
- Complete PaymentsController
- Complete ProfileController
- Complete CartService guest cart support
- **Expected Gain:** +5-15 tests

---

## Testing Commands

```powershell
# Test everything
dotnet test --settings .runsettings

# Test specific controller
dotnet test --filter "WishlistControllerTests" --settings .runsettings

# Test specific test
dotnet test --filter "WishlistControllerTests.AddToWishlist_Unauthenticated_ReturnsUnauthorized" --settings .runsettings

# Build check
dotnet build
```

---

## Key Files to Modify

### Controllers (Quick Fixes)
- `src/backend/ECommerce.API/Controllers/WishlistController.cs`
- `src/backend/ECommerce.API/Controllers/ReviewsController.cs`
- `src/backend/ECommerce.API/Controllers/CartController.cs`
- `src/backend/ECommerce.API/Controllers/DashboardController.cs`
- `src/backend/ECommerce.API/Controllers/InventoryController.cs`
- `src/backend/ECommerce.API/Controllers/OrdersController.cs`
- `src/backend/ECommerce.API/Controllers/PromoCodesController.cs`
- `src/backend/ECommerce.API/Controllers/ProfileController.cs`

### Services (Medium Fixes)
- `src/backend/ECommerce.Application/Services/CartService.cs`
- `src/backend/ECommerce.Application/Services/ProfileService.cs`
- `src/backend/ECommerce.Application/Services/PaymentService.cs`
- `src/backend/ECommerce.Application/Services/PromoCodeService.cs`

---

## Summary

**Current:** 451/489 (92.2%)  
**Target:** 460+/489 (94%+)  
**Gap:** Only 9 tests needed  
**Effort:** ~45 minutes for Phase A+B+C  

**The finish line is close!** Most remaining issues are simple decorator additions and response code fixes. A few require implementing missing business logic, but these are well-scoped.

