# Phase 1: Testing & Verification — Detailed Checklist

**Status**: Starting Phase 1  
**Date**: February 6, 2026  
**Goal**: Verify all 5 previous phases work correctly

---

## A. Toast Notification System Verification

### Code Review ✅
- [x] Redux slice properly created (`toastSlice.ts`)
- [x] useToast hook implemented with all 4 methods
- [x] Toast component renders correctly
- [x] ToastContainer manages multiple toasts
- [x] CSS animations defined (shimmer effect)
- [x] App.tsx integrated with ToastContainer

### Manual Tests Required
- [ ] **TC-T1**: Login page shows success toast on successful login
- [ ] **TC-T2**: Login page shows error toast on failed login
- [ ] **TC-T3**: Register page shows success toast
- [ ] **TC-T4**: Toast auto-dismisses after 3 seconds
- [ ] **TC-T5**: Multiple toasts stack vertically without overlap
- [ ] **TC-T6**: Close button dismisses toast immediately
- [ ] **TC-T7**: All 4 colors display correctly (green, red, orange, blue)
- [ ] **TC-T8**: Mobile: Toasts appear at bottom, full width
- [ ] **TC-T9**: Admin login shows toast (separate app instance)

### Browser DevTools Verification
- [ ] Redux state shows `toast.toasts` array populated
- [ ] No console errors when toasts appear
- [ ] Network tab: No extra API calls

---

## B. Error Boundary Verification

### Code Review ✅
- [x] ErrorBoundary.tsx created as class component
- [x] getDerivedStateFromError implemented
- [x] componentDidCatch logs errors
- [x] ErrorPage fallback UI created
- [x] App.tsx wrapped with ErrorBoundary
- [x] Styling responsive and professional

### Manual Tests Required
- [ ] **TC-E1**: Intentionally break a component (throw error)
- [ ] **TC-E2**: ErrorPage displays instead of white screen
- [ ] **TC-E3**: "Go to Home" button navigates to /
- [ ] **TC-E4**: "Refresh Page" button reloads the page
- [ ] **TC-E5**: Dev mode shows error details in `<details>` tag
- [ ] **TC-E6**: Error details hidden in production (check config)
- [ ] **TC-E7**: Admin ErrorBoundary separate from storefront
- [ ] **TC-E8**: Multiple errors trigger boundary multiple times

### Browser Verification
- [ ] No errors in console before intentional break
- [ ] Error clearly displayed after break
- [ ] Styling visible and readable

---

## C. Loading Skeletons Verification

### Code Review ✅
- [x] Skeleton.tsx base component created
- [x] ProductSkeleton matches product card dimensions
- [x] ProfileSkeleton matches profile form layout
- [x] CartSkeleton matches cart layout
- [x] ProductsGridSkeleton creates grid of 12 items
- [x] Shimmer animation in CSS (2s, gradient shift)
- [x] QueryRenderer updated with custom loading support

### Manual Tests Required (Slow Network)
- [ ] **TC-S1**: Products page shows ProductsGridSkeleton while loading
- [ ] **TC-S2**: Each skeleton has shimmer animation
- [ ] **TC-S3**: ProductDetail shows ProductSkeleton
- [ ] **TC-S4**: Profile page shows ProfileSkeleton
- [ ] **TC-S5**: Cart page shows CartSkeleton
- [ ] **TC-S6**: No layout shift when content loads
- [ ] **TC-S7**: Skeleton disappears smoothly when content arrives
- [ ] **TC-S8**: Mobile: Skeletons stack properly on small screens
- [ ] **TC-S9**: Shimmer animation is smooth (not jittery)

### Performance Check
- [ ] Skeleton loading test with Chrome DevTools throttling (Slow 3G)

---

## D. useAuth Integration Verification

### Code Review ✅
- [x] Login.tsx removed error state, uses useToast
- [x] Register.tsx updated similarly
- [x] ForgotPassword.tsx uses toast for success
- [x] ResetPassword.tsx uses toast for status
- [x] Profile.tsx supports useToast
- [x] Redux token/user properly stored
- [x] localStorage persists token

### Manual Tests Required
- [ ] **TC-A1**: Login with valid credentials → success toast
- [ ] **TC-A2**: Login with invalid credentials → error toast
- [ ] **TC-A3**: Register new account → success toast
- [ ] **TC-A4**: Register with duplicate email → error toast
- [ ] **TC-A5**: Logout clears token and user
- [ ] **TC-A6**: Page reload restores authenticated state
- [ ] **TC-A7**: Invalid token redirected to login
- [ ] **TC-A8**: Protected routes block unauthenticated access
- [ ] **TC-A9**: Admin login also uses new toast system

### Auth State Verification
- [ ] Redux DevTools shows auth slice with user and token
- [ ] localStorage contains `auth` key after login
- [ ] Token sent in Authorization header on API calls

---

## E. Admin Panel Sync Verification

### Code Review ✅
- [x] Admin config.ts created
- [x] Admin toastSlice.ts created
- [x] Admin useToast.ts hook created
- [x] Admin Toast/ToastContainer created
- [x] Admin ErrorBoundary created
- [x] Admin App.tsx has ErrorBoundary + ToastContainer
- [x] Admin Login.tsx uses useToast
- [x] Store updated with toast reducer

### Manual Tests Required
- [ ] **TC-AD1**: Admin login shows success toast
- [ ] **TC-AD2**: Admin login shows error toast on failure
- [ ] **TC-AD3**: Admin ErrorBoundary catches errors
- [ ] **TC-AD4**: Admin app works independently from storefront
- [ ] **TC-AD5**: Toast styling same as storefront (consistent)
- [ ] **TC-AD6**: Admin config values used correctly
- [ ] **TC-AD7**: Protected routes work in admin

---

## Testing Procedure

### 1. Setup for Manual Testing
```bash
# Terminal 1: Start Docker
docker compose up

# Wait for services to start (30-60 seconds)
# Check: API running on http://localhost:5000
#        Storefront on http://localhost:5173
#        Admin on http://localhost:5177
```

### 2. Browser Setup
- Open Chrome DevTools (F12)
- Open Console, Network, Redux DevTools tabs
- Keep browser open for all tests

### 3. Testing Order
1. **Toast Tests (TC-T1 through TC-T9)** — ~15 min
2. **Error Boundary Tests (TC-E1 through TC-E8)** — ~10 min
3. **Skeleton Tests (TC-S1 through TC-S9)** — ~15 min (use Chrome throttling)
4. **Auth Tests (TC-A1 through TC-A9)** — ~20 min
5. **Admin Tests (TC-AD1 through TC-AD7)** — ~10 min
6. **Mobile Responsive Tests** — ~10 min (resize to 375px)

**Total Estimated Time**: ~80 minutes

### 4. Chrome DevTools Throttling (for Skeleton Tests)
1. Open DevTools → Network tab
2. Set throttling to "Slow 3G"
3. Reload page
4. Watch skeletons load, then content

### 5. Mobile Testing (375px)
1. DevTools → Toggle Device Toolbar (Ctrl+Shift+M)
2. Select "iPhone SE" or set width to 375px
3. Test toast position (should be bottom)
4. Test skeleton layout (should not wrap awkwardly)

---

## Expected Results

### ✅ All Tests Pass When:
- Toast notifications appear with correct colors
- Toasts auto-dismiss after 3 seconds
- Multiple toasts don't overlap
- Error boundary catches errors gracefully
- ErrorPage displays with working buttons
- Skeletons appear while loading
- Shimmer animation is smooth
- No layout shift when content loads
- Auth flows work with toast notifications
- Admin app independent but follows same patterns
- No console errors
- Mobile layout responsive

### ❌ Tests Fail If:
- Toast doesn't appear or appears in wrong color
- Toast doesn't auto-dismiss
- Error crashes app (white screen)
- Skeleton doesn't animate
- Skeleton dimensions don't match content
- Auth doesn't persist across reload
- Admin toasts don't work
- Console has TypeScript/runtime errors

---

## Issue Resolution Template

If any test fails:
```markdown
### Issue: [Test Code] - Description
- **Expected**: What should happen
- **Actual**: What happened instead
- **File**: Which file needs fixing
- **Fix**: The code change needed
```

---

## Documentation to Update
After all tests pass:
- [ ] README.md — Add "Features" section mentioning toast, error boundaries, skeletons
- [ ] Add test results summary to NEXT_IMPROVEMENTS_PLAN.md
- [ ] Create TESTING_GUIDE.md for future testing

---

## Ready to Test?

Before running manual tests, confirm:
- ✅ Docker is installed and running
- ✅ All files from Phase 1-5 are committed to git
- ✅ Zero TypeScript errors in IDE
- ✅ No uncommitted changes

**Next Step**: Run `docker compose up` and start testing! 🧪
