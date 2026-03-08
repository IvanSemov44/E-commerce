# Test Errors Analysis - March 5, 2026

## Summary

**Total**: 322 tests | **Passed**: 262 | **Failed**: 60  
**Test Files**: 37 total | 19 passed | 18 failed

---

## Error Categories

### 1. RTK Query Middleware Missing (23 failures)

**Root Cause**: Test stores do not include RTK Query middleware

**Affected Test Files**:

- `useCart.test.tsx` - 5 failures
- `useCartSync.test.tsx` - 2 failures
- `useCheckout.test.tsx` - 6 failures
- `useProfileForm.test.tsx` - 5 failures
- `useProductDetails.test.tsx` - 3 failures
- `ProductActions.test.tsx` - 1 failure
- `AccountDetails.test.tsx` - 1 failure

**Error Message**:

```
Warning: Middleware for RTK-Query API at reducerPath "api" has not been added to the store.
You must add the middleware for RTK-Query to function correctly!
```

**Fix Required**: Update test utilities to include RTK Query middleware in mock store configuration.

---

### 2. React Router Context Missing (5 failures)

**Root Cause**: Components use `<Link>` or router hooks without `<BrowserRouter>` wrapper

**Affected Test Files**:

- `OrderSuccess.test.tsx` - 5 failures

**Error Message**:

```
TypeError: Cannot destructure property 'basename' of 'React.useContext(...)' as it is null.
```

**Fix Required**: Wrap component in `<BrowserRouter>` in test rendering.

---

### 3. Redux Context Missing (18 failures)

**Root Cause**: Components use Redux hooks without `<Provider>` wrapper

**Affected Test Files**:

- `CheckoutAuthBanner.test.tsx` - 14 failures
- `ProductGrid.test.tsx` - 4 failures

**Error Message**:

```
Error: could not find react-redux context value; please ensure the component is wrapped in a <Provider>
```

**Fix Required**: Wrap components in Redux `<Provider>` with mock store in tests.

---

### 4. Component API Mismatches (9 failures)

**Root Cause**: Tests expect features/props that don't exist in actual components

#### 4.1 OrderSummary - Promo Code Features (3 failures)

- ❌ `displays totals breakdown` - Multiple elements with text "total"
- ❌ `calls onPromoCodeChange when promo code input changes` - No promo code input exists
- ❌ `applies promo code when apply button clicked` - No apply button exists

**Fix Required**: Either:

- Remove tests for non-existent promo code feature, OR
- Implement promo code feature in OrderSummary component

#### 4.2 OrderCard - Items Count Display (2 failures)

- ❌ `renders items count for multiple items` - Cannot find "3 items" text
- ❌ `renders items count for single item` - Cannot find "1 item" text

**Fix Required**: Check if OrderCard displays items count or update test expectations.

#### 4.3 OrderHeader - Cancel Button (2 failures)

- ❌ `shows cancel button when canCancel is true` - No cancel button exists
- ❌ `hides cancel button when canCancel is false` - No cancel button exists

**Fix Required**: Either:

- Remove tests for non-existent cancel feature, OR
- Implement cancel button in OrderHeader component

#### 4.4 ProfileForm - Save Button Click (1 failure)

- ❌ `calls onSave when save button is clicked` - onSave callback not triggered

**Fix Required**: Verify form submission logic or test expectations.

#### 4.5 OrderTotals - Tax Display (1 failure)

- ❌ `displays tax amount` - Tax information not in component

**Fix Required**: Update test or add tax display to component.

---

### 5. CSS Class Name Mismatches (3 failures)

**Root Cause**: Tests check for CSS class names that don't match CSS Modules naming

**Affected Test File**: `ProfileMessages.test.tsx`

- ❌ `applies success styling to success message` - Expects class "success"
- ❌ `applies error styling to error message` - Expects class "error"
- ❌ `renders in container with proper structure` - Expects class "container"

**Fix Required**: Update tests to use CSS Modules hashed class names or check `className` contains substring.

---

### 6. Multiple Elements Found (1 failure)

**Root Cause**: Query finds multiple matching elements

**Affected Test File**: `CartSummary.test.tsx`

- ❌ `displays free shipping when shipping is zero` - Multiple elements with text "/free/i"

**Fix Required**: Use more specific query (e.g., `getByRole`, `getByTestId`) or scope query to container.

---

### 7. Authentication Test Logic Error (1 failure)

**Affected Test File**: `useAuth.test.tsx`

- ❌ `should handle login failure` - Error message mismatch

**Error**:

```
AssertionError: expected 'An unknown error occurred' to be 'Invalid credentials'
```

**Fix Required**: Mock API error response correctly or update test expectation.

---

## Recommended Fix Priority

### Priority 1: Infrastructure Fixes (Quick Wins)

These are test setup issues, not component bugs:

1. **Add RTK Query middleware to test store** → Fixes 23 tests
2. **Add BrowserRouter wrapper to router tests** → Fixes 5 tests
3. **Add Redux Provider to component tests** → Fixes 18 tests

**Impact**: 46/60 failures (77%) can be fixed with test setup improvements.

### Priority 2: Test Cleanup (Documentation Issues)

Remove or update tests for non-existent features:

1. OrderSummary promo code tests (3 tests)
2. OrderHeader cancel button tests (2 tests)
3. OrderCard items count tests (2 tests)
4. OrderTotals tax display test (1 test)
5. ProfileMessages CSS class tests (3 tests)

**Impact**: 11/60 failures fixed.

### Priority 3: Component/Test Bugs

Fix actual component or test logic issues:

1. CartSummary multiple elements query (1 test)
2. ProfileForm onSave callback (1 test)
3. useAuth error message mock (1 test)

**Impact**: 3/60 failures fixed.

---

## Files Needing Updates

### Test Utilities

- `src/tests/test-utils.tsx` or similar - Add RTK Query middleware to mock store

### Hook Tests Needing Redux Provider + Middleware

- `src/features/cart/hooks/__tests__/useCart.test.tsx`
- `src/features/cart/hooks/__tests__/useCartSync.test.tsx`
- `src/features/checkout/hooks/__tests__/useCheckout.test.tsx`
- `src/features/profile/hooks/__tests__/useProfileForm.test.tsx`
- `src/features/products/hooks/__tests__/useProductDetails.test.tsx`

### Component Tests Needing Router/Redux Context

- `src/features/checkout/components/OrderSuccess/OrderSuccess.test.tsx`
- `src/features/checkout/components/CheckoutAuthBanner/CheckoutAuthBanner.test.tsx`
- `src/features/products/components/ProductGrid/ProductGrid.test.tsx`

### Tests Needing Expectations Updated

- `src/features/checkout/components/OrderSummary/OrderSummary.test.tsx`
- `src/features/orders/components/OrderCard/OrderCard.test.tsx`
- `src/features/orders/components/OrderHeader/OrderHeader.test.tsx`
- `src/features/orders/components/OrderTotals/OrderTotals.test.tsx`
- `src/features/profile/components/ProfileMessages/ProfileMessages.test.tsx`
- `src/features/profile/components/ProfileForm/ProfileForm.test.tsx`
- `src/features/cart/components/CartSummary/CartSummary.test.tsx`
- `src/features/auth/hooks/__tests__/useAuth.test.tsx`

---

---

## TypeScript Compilation Errors in Tests

### Missing Testing Library Matchers Import

**Issue**: Many test files use `toBeInTheDocument()`, `toHaveClass()`, `toBeDisabled()` but don't import from `@testing-library/jest-dom`.

**Affected Files** (199 errors across 7 files):

- `ProfileMessages.test.tsx` - 7 errors
- `ProfileHeader.test.tsx` - 5 errors
- `CheckoutForm.test.tsx` - 11 errors
- `OrderSuccess.test.tsx` - 6 errors
- `OrderSummary.test.tsx` - 8 errors
- `OrderHeader.test.tsx` - 6 errors
- `OrderItemsList.test.tsx` - 5 errors
- `OrderTotals.test.tsx` - 1 error

**Fix Required**: Add to each file:

```typescript
import '@testing-library/jest-dom';
```

Or configure in Vitest setup file to import globally.

### Circular Import in OrderCard

**File**: `src/features/orders/components/OrderCard.tsx`

**Error**:

```
Circular definition of import alias 'default'.
```

**Fix Required**: Check OrderCard component file structure - likely has incorrect export/import pattern.

---

## Next Steps

1. **Fix TypeScript errors first** (prevents build issues):
   - Add `@testing-library/jest-dom` import to 8 test files OR
   - Configure Vitest setup to import globally
   - Fix OrderCard circular import

2. **Create shared test utility** with proper Redux store + RTK Query middleware

3. **Update all hook tests** to use new test utility

4. **Add router/redux wrappers** to component tests that need them

5. **Clean up tests** for non-existent features

6. **Fix remaining logic/query issues** (3 tests)

7. **Re-run full test suite** to verify all 322 tests pass
