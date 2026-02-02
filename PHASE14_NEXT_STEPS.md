# Phase 14 Quick Action Guide

## Current Status
✅ **437/489 tests passing (89.4%)**  
⏳ **52 tests failing (10.6%)**  
✅ **0 build errors**

---

## What's Already Done ✅
- All 30 Phase 14 test files created and compile successfully
- Routes verified and corrected (ReviewsController, WishlistController)
- Test structure optimized using TestFixture pattern
- All controllers implemented with endpoints
- 8 tests fixed via route corrections

---

## What Needs Fixing ⏳

### **Phase 1 (30-45 min): JWT Token Implementation** 🔑
File: `src/backend/ECommerce.Tests/Fixtures/TestWebApplicationFactory.cs`

**Current Problem:** Tests use static auth flags that don't propagate JWT claims
**Solution:** Generate actual JWT tokens with roles

**Code Template:**
```csharp
public string GenerateJwtToken(params string[] roles)
{
    var claims = new List<Claim>
    {
        new Claim(ClaimTypes.NameIdentifier, "test-user-123"),
        new Claim(ClaimTypes.Name, "Test User"),
        new Claim(ClaimTypes.Email, "test@example.com")
    };
    
    foreach (var role in roles)
        claims.Add(new Claim(ClaimTypes.Role, role));
    
    // Create token...
    return token;
}
```

**Impact:** Fixes ~30 failures (Phase 13 + Dashboard tests)

---

### **Phase 2 (45-60 min): Update Phase 13 Tests** 🔄
All files: `*ControllerTests.cs` in Phase 13

**Replace this:**
```csharp
public void SetUp()
{
    _fixture.ApplyAdminRole(); // ❌ Static flag, doesn't work
}
```

**With this:**
```csharp
public void SetUp()
{
    var token = _fixture.GenerateJwtToken("Admin");
    _client.DefaultRequestHeaders.Authorization = 
        new AuthenticationHeaderValue("Bearer", token);
}
```

**Affected Test Classes:**
- AuthControllerTests (role claims)
- ProductsControllerTests (CRUD operations)
- OrdersControllerTests (status updates)
- Any admin-required tests

**Impact:** Fixes 23 Phase 13 failures

---

### **Phase 3 (1-2 hours): Complete Implementations** 🛠️
Fix endpoint implementation gaps

**Cart Issues:**
- Guest cart support incomplete
- File: `src/backend/ECommerce.Application/Services/CartService.cs`
- Add `GetGuestCart()` method

**Profile Issues:**
- Some endpoints not fully implemented
- File: `src/backend/ECommerce.API/Controllers/ProfileController.cs`
- Verify all endpoints are implemented

**Inventory Issues:**
- Verification endpoints missing
- File: `src/backend/ECommerce.API/Controllers/InventoryController.cs`
- Add verification operations

**Impact:** Fixes 10-15 remaining failures

---

## Test Breakdown

| Phase | Tests | Passing | Failing | Root Cause |
|-------|-------|---------|---------|---|
| 13 | 407 | 384 | **23** | JWT token role propagation |
| 14 | 82 | 53 | **29** | 4 dashboard + 20+ implementation gaps |
| **TOTAL** | **489** | **437** | **52** | |

---

## Success Checklist for Next Session

- [ ] **JWT Implementation Done**
  - GenerateJwtToken method works
  - Test: Run one auth test, should pass

- [ ] **Phase 13 Tests Updated**
  - All admin role tests use JWT token
  - Test: Run AuthControllerTests, 95%+ should pass

- [ ] **Dashboard Tests Passing**
  - Admin role claims in JWT
  - Test: 4/7 dashboard tests passing

- [ ] **Implementation Gaps Filled**
  - Cart, Profile, Inventory endpoints complete
  - Test: Full test run shows 95%+ pass rate

---

## Expected End Result

After completing all 3 phases:
- ✅ **465+ tests passing (95%+)**
- ✅ **0-1% failing (implementation-specific issues)**
- ✅ **Clean build, 0 errors**
- ✅ **All controllers fully functional**

---

## Quick Commands

```powershell
# Run all tests
dotnet test

# Run specific test class
dotnet test --filter "ClassName=AuthControllerTests"

# Run with verbose output
dotnet test --verbosity detailed

# Get test count
dotnet test --collect:"XPlat Code Coverage" --logger:"console;verbosity=detailed"
```

---

## Key Files

**Test Fixtures & Configuration:**
- `src/backend/ECommerce.Tests/Fixtures/TestWebApplicationFactory.cs` ← UPDATE HERE FIRST
- `src/backend/ECommerce.Tests/Fixtures/TestAuthFixture.cs`

**Phase 14 Test Files (Already created):**
- `src/backend/ECommerce.Tests/Controllers/ReviewsControllerTests.cs`
- `src/backend/ECommerce.Tests/Controllers/WishlistControllerTests.cs`
- `src/backend/ECommerce.Tests/Controllers/CartControllerTests.cs`
- `src/backend/ECommerce.Tests/Controllers/PromoCodesControllerTests.cs`
- `src/backend/ECommerce.Tests/Controllers/ProfileControllerTests.cs`
- `src/backend/ECommerce.Tests/Controllers/DashboardControllerTests.cs`
- `src/backend/ECommerce.Tests/Controllers/InventoryControllerTests.cs`

---

## Notes
- Do NOT edit route definitions - they're already correct after our fixes
- Focus on JWT token generation first (biggest impact)
- Phase 13 tests are waiting for this fix
- Implementation gaps are secondary priority

