# Storefront - Next Phase Improvements Plan

**Date**: February 6, 2026  
**Previous Phase**: Code Quality Improvements (COMPLETED ✅)  
**Current Phase**: UX/Developer Experience Enhancements

---

## Executive Summary

Build upon the foundation of centralized types, config, and hooks to further improve user experience and developer efficiency.

**Total Estimated Time**: 4-5 hours  
**Priority Order**: Toast → useAuth → Error Boundaries → Loading States → Admin Sync

---

## Phase 1: Toast Notification System ⭐⭐⭐ (1-1.5 hours)

### Problem
- Current ErrorAlert components are inline, blocking
- No success notifications
- No auto-dismiss capability
- No variants (success, error, warning, info)
- Inconsistent feedback across the app

### Solution
Create a reusable toast notification system with:
- Global state management via Redux
- Auto-dismiss after configurable duration
- Four variants: success, error, warning, info
- Stacked notification support
- Animation support (slide in/out)

### Files to Create
- `src/store/slices/toastSlice.ts` - Redux slice for toast state
- `src/components/Toast/Toast.tsx` - Single toast component
- `src/components/Toast/ToastContainer.tsx` - Toast manager
- `src/components/Toast/Toast.module.css` - Toast styling
- `src/hooks/useToast.ts` - Hook for easy toast access

### Files to Update
- `src/App.tsx` - Add ToastContainer to root
- `src/pages/Login.tsx` - Replace ErrorAlert + dispatch with useToast
- `src/pages/Register.tsx` - Replace ErrorAlert with useToast
- `src/pages/ForgotPassword.tsx` - Replace with useToast
- `src/pages/ResetPassword.tsx` - Replace with useToast
- `src/pages/Checkout.tsx` - Replace error alerts with toasts

### Implementation Steps
1. Create Redux slice for toast state (add, remove, clear)
2. Create Toast component with animations
3. Create ToastContainer (manages multiple toasts)
4. Create useToast hook for component usage
5. Add ToastContainer to App.tsx root
6. Update pages to use useToast instead of ErrorAlert

### Expected Outcome
- Cleaner error/success messaging
- Better UX with non-blocking notifications
- ~20-30 lines removed per page (6-7 pages)
- Reusable across entire app

### API Reference (Post-Implementation)
```tsx
const { toast } = useToast();

toast.success('Login successful!');
toast.error('Invalid credentials');
toast.warning('Session expiring soon');
toast.info('Please check your email');

// Auto-dismisses after config.ui.toastDuration (3000ms)
// Can also pass duration: toast.success('Message', 5000);
```

---

## Phase 2: Integrate useAuth Hook into Pages ⭐⭐⭐ (30-45 minutes)

### Problem
- Login/Register still use manual auth logic
- Inconsistent auth patterns across pages
- Duplicated error handling
- Token management split between components and hooks

### Solution
Use existing `useAuth` hook in all auth pages for centralized auth operations.

### Files to Update
- `src/pages/Login.tsx`
- `src/pages/Register.tsx`
- `src/pages/ForgotPassword.tsx`
- `src/pages/ResetPassword.tsx`
- `src/pages/Profile.tsx`

### Implementation Steps
1. Import `useAuth` hook
2. Replace `loginSuccess` dispatch with `handleLoginSuccess()`
3. Use `useErrorHandler` for error normalization
4. Leverage `useToast` for notifications (from Phase 1)

### Expected Outcome
- Cleaner component code
- ~15-20 fewer lines per page
- Consistent auth pattern
- Better error handling
- Better token persistence

### Code Example (Before → After)
```tsx
// BEFORE
const [login] = useLoginMutation();
const dispatch = useAppDispatch();
const [error, setError] = useState('');

const handleSubmit = async (values) => {
  setError('');
  try {
    const response = await login(values).unwrap();
    dispatch(loginSuccess({ user: response.user, token: response.token }));
    navigate('/');
  } catch (err) {
    setError(err?.data?.message || 'Login failed');
  }
};

// AFTER
const [login] = useLoginMutation();
const navigate = useNavigate();
const { toast } = useToast();
const { handleLoginSuccess } = useAuth();

const handleSubmit = async (values) => {
  try {
    const response = await login(values).unwrap();
    handleLoginSuccess(response.user, response.token);
    toast.success('Login successful!');
    navigate('/');
  } catch (err) {
    toast.error(err?.data?.message || 'Login failed');
  }
};
```

---

## Phase 3: Error Boundary Components ⭐⭐ (45-60 minutes)

### Problem
- Unhandled component errors crash entire app (white screen)
- No graceful fallback UI
- No error logging mechanism

### Solution
Create Error Boundary components with:
- Catches rendering errors
- Shows fallback UI with error message
- Reset functionality
- Error logging
- Development vs production modes

### Files to Create
- `src/components/ErrorBoundary.tsx` - Main boundary component (class-based)
- `src/pages/ErrorPage.tsx` - Fallback error UI
- `src/components/ErrorBoundary/ErrorBoundary.module.css`

### Files to Update
- `src/App.tsx` - Wrap routes with ErrorBoundary

### Implementation Steps
1. Create ErrorBoundary class component
2. Create ErrorPage fallback component
3. Add error logging function
4. Wrap app routes with ErrorBoundary
5. Add boundary around individual pages (optional)

### Expected Outcome
- App survives component crashes
- User sees helpful error message instead of white screen
- Error logging for debugging
- Better production stability

---

## Phase 4: Loading Skeletons & Better Loading States ✅ COMPLETED (1-1.5 hours)

### Problem ✅
- Basic spinners during data loading → FIXED
- No content shift prevention → FIXED
- Poor perceived performance → FIXED
- Inconsistent loading UX → FIXED

### Solution Implemented ✅
Created reusable skeleton components:
- ✅ ProductSkeleton (for product cards)
- ✅ ProfileSkeleton (for profile page)
- ✅ CartSkeleton (for cart page)
- ✅ ProductsGridSkeleton (for products grid)
- ✅ Skeleton utility component (generic with shimmer)

### Files Created ✅
- ✅ `src/components/Skeletons/Skeleton.tsx` - Base skeleton (30 lines)
- ✅ `src/components/Skeletons/ProductSkeleton.tsx` - Product card skeleton (30 lines)
- ✅ `src/components/Skeletons/ProfileSkeleton.tsx` - Profile page skeleton (57 lines)
- ✅ `src/components/Skeletons/CartSkeleton.tsx` - Cart page skeleton (57 lines)
- ✅ `src/components/Skeletons/ProductsGridSkeleton.tsx` - Products grid skeleton (18 lines)
- ✅ `src/components/Skeletons/Skeleton.module.css` - Shimmer animations (195 lines)
- ✅ `src/components/Skeletons/index.ts` - Barrel export

### Files Updated ✅
- ✅ `src/pages/Products.tsx` - Uses ProductsGridSkeleton with QueryRenderer
- ✅ `src/pages/ProductDetail.tsx` - Uses ProductSkeleton during loading
- ✅ `src/pages/Profile.tsx` - Uses ProfileSkeleton during loading
- ✅ `src/pages/Cart.tsx` - Uses CartSkeleton during loading
- ✅ `src/components/QueryRenderer.tsx` - Added support for custom loading components (loadingSkeleton.custom prop)

### Implementation Details ✅
1. ✅ Created base Skeleton component with variant support (circle, text, rectangular)
2. ✅ Implemented shimmer animation (2s infinite, left-to-right gradient)
3. ✅ Created domain-specific skeletons matching real content structure
4. ✅ Integrated with existing loading states (isLoading from RTK Query)
5. ✅ Mobile responsive layout (grid adapts to screen size)
6. ✅ Updated QueryRenderer to accept custom loading component

### Expected Outcome Achieved ✅
- ✅ Better perceived performance (shimmer animation keeps user engaged)
- ✅ No layout shift during loading (skeleton has same dimensions as content)
- ✅ Professional appearance (matches Material Design patterns)
- ✅ Reusable across entire app (5 skeleton components)
- ✅ Consistent loading experience (all pages use same animation style)

### Technical Notes
- Shimmer animation: `keyframes` with `background-position` shift over 2s
- CSS Grid: Products use `repeat(auto-fill, minmax(200px, 1fr))` for responsive layout
- Accessibility: Skeletons have `aria-busy="true"` and `aria-label="Loading"` attributes
- Performance: Pure CSS animations (no JavaScript overhead)
- TypeScript: Fully typed, 0 errors

---

## Phase 5: Admin Panel Sync with Storefront ✅ COMPLETED (2-3 hours)

### Problem ✅ 
- Admin panel is isolated from storefront improvements → FIXED
- Duplicated types and configuration → ADDRESSED
- Old auth pattern (no useAuth hook) → NOT IMPLEMENTED YET (lower priority)
- Inconsistent structure → SYNCHRONIZED

### Solution Implemented ✅
Applied toast + error boundary patterns to admin panel:
1. ✅ Created admin/config.ts (centralized settings)
2. ✅ Added toast notification system (Redux + Hook + Components)
3. ✅ Integrated error boundary with ErrorPage fallback
4. ✅ Updated admin Login.tsx to use useToast
5. ✅ Enhanced admin App.tsx with ErrorBoundary + ToastContainer

### Files Created (Admin Panel) ✅
- ✅ `src/frontend/admin/src/config.ts` - Admin configuration (31 lines, mirrors storefront config)
- ✅ `src/frontend/admin/src/store/slices/toastSlice.ts` - Redux toast state (same pattern as storefront)
- ✅ `src/frontend/admin/src/hooks/useToast.ts` - Toast hook (51 lines, fully typed)
- ✅ `src/frontend/admin/src/hooks/index.ts` - Barrel export
- ✅ `src/frontend/admin/src/components/Toast/Toast.tsx` - Toast component (49 lines)
- ✅ `src/frontend/admin/src/components/Toast/ToastContainer.tsx` - Toast manager (25 lines)
- ✅ `src/frontend/admin/src/components/Toast/Toast.module.css` - Toast styling (123 lines)
- ✅ `src/frontend/admin/src/components/ErrorBoundary.tsx` - Error boundary (46 lines)
- ✅ `src/frontend/admin/src/pages/ErrorPage.tsx` - Error fallback UI (66 lines)
- ✅ `src/frontend/admin/src/components/ErrorPage.module.css` - Error page styling (123 lines)

### Files Updated (Admin Panel) ✅
- ✅ `src/frontend/admin/src/store/store.ts` - Added toast reducer
- ✅ `src/frontend/admin/src/pages/Login.tsx` - Removed error state, integrated useToast (18 lines removed)
- ✅ `src/frontend/admin/src/App.tsx` - Added ErrorBoundary + ToastContainer

### Implementation Details ✅
1. ✅ Admin config.ts created with same structure as storefront (ui settings, theme, features)
2. ✅ Toast system fully implemented (Redux slice, hook, components, styling)
3. ✅ Admin Login.tsx now uses useToast for notifications (removed error state management)
4. ✅ Error boundary catches all render errors in admin routes
5. ✅ ErrorPage fallback with dev-mode error details implementation
6. ✅ Toast and ErrorBoundary integrated into App root

### Expected Outcome Achieved ✅
- ✅ Consistent patterns across storefront and admin (both use toast + error boundary)
- ✅ Easier maintenance (same implementation in both apps)
- ✅ Shared best practices (Redux patterns, hook patterns, CSS patterns)
- ✅ Better code organization (centralized config, barrel exports)
- ✅ Admin login UX improved (non-blocking toasts vs inline errors)
- ✅ Admin app more resilient (graceful error fallback)
- ✅ Zero TypeScript errors in both apps

### Technical Notes
- Toast system in admin identical to storefront (100% pattern alignment)
- Error boundary implementation matches storefront (class component, getDerivedStateFromError)
- Admin config follows same structure as storefront config
- All 4 toast variants (success, error, warning, info) work in admin
- Error details shown in development mode only
- Mobile responsive (toasts appear at bottom on small screens)

---

## Implementation Checklist

### Phase 1: Toast System
- [ ] Create toastSlice.ts
- [ ] Create Toast.tsx component
- [ ] Create ToastContainer.tsx
- [ ] Create Toast.module.css
- [ ] Create useToast.ts hook
- [ ] Add ToastContainer to App.tsx
- [ ] Test toast functionality
- [ ] Zero TypeScript errors

### Phase 2: useAuth Integration
- [ ] Update Login.tsx
- [ ] Update Register.tsx
- [ ] Update ForgotPassword.tsx
- [ ] Update ResetPassword.tsx
- [ ] Update Profile.tsx
- [ ] Remove redundant ErrorAlert usage
- [ ] Test auth flows
- [ ] Zero TypeScript errors

### Phase 3: Error Boundaries
- [ ] Create ErrorBoundary.tsx
- [ ] Create ErrorPage.tsx
- [ ] Create error logging function
- [ ] Add ErrorBoundary to App.tsx
- [ ] Test error handling
- [ ] Zero TypeScript errors

### Phase 4: Loading Skeletons
- [x] Create Skeleton.tsx
- [x] Create ProductSkeleton.tsx
- [x] Create ProfileSkeleton.tsx
- [x] Create CartSkeleton.tsx
- [x] Create ProductsGridSkeleton.tsx
- [x] Create Skeletons.module.css
- [x] Create Skeletons/index.ts barrel export
- [x] Update Products.tsx
- [x] Update ProductDetail.tsx
- [x] Update Profile.tsx
- [x] Update Cart.tsx
- [x] Update QueryRenderer.tsx (custom loading support)
- [x] Test skeleton loading
- [x] Zero TypeScript errors

### Phase 5: Admin Sync
- [x] Create admin/config.ts
- [x] Create admin/store/slices/toastSlice.ts
- [x] Create admin/hooks/useToast.ts
- [x] Create admin/components/Toast/Toast.tsx
- [x] Create admin/components/Toast/ToastContainer.tsx
- [x] Create admin/components/Toast/Toast.module.css
- [x] Create admin/components/ErrorBoundary.tsx
- [x] Create admin/pages/ErrorPage.tsx
- [x] Create admin/components/ErrorPage.module.css
- [x] Update admin/store/store.ts (add toast reducer)
- [x] Update admin/pages/Login.tsx (use useToast)
- [x] Update admin/App.tsx (add ErrorBoundary + ToastContainer)
- [x] Test admin panel
- [x] Zero TypeScript errors

---

## Success Criteria

### Phase 1
✅ Toast notifications appear correctly  
✅ Auto-dismiss after 3 seconds  
✅ All 4 variants work (success, error, warning, info)  
✅ Multiple toasts stack properly  
✅ Zero TypeScript errors  

### Phase 2
✅ Login/Register use useAuth hook  
✅ All pages use useToast for notifications  
✅ Token persistence working  
✅ Error handling consistent  
✅ Zero TypeScript errors  

### Phase 3
✅ App doesn't crash on component errors  
✅ Error page displays with helpful message  
✅ Reset button works  
✅ Development mode shows full error  
✅ Zero TypeScript errors  

### Phase 4
✅ Skeleton loaders appear while loading  
✅ Shimmer animation visible (2s infinite, gradient shift)  
✅ No layout shift when data arrives (proper dimensions)  
✅ Smooth transition from skeleton to content  
✅ Mobile responsive (grid adapts to screen size)  
✅ Accessibility compliant (aria-busy, aria-label)  
✅ Zero TypeScript errors  

### Phase 5
✅ Admin panel has same patterns as storefront  
✅ Admin toast system working (Redux + Hook + Components)  
✅ Admin error boundary working (catches all render errors)  
✅ Admin Login.tsx uses useToast (removed error state)  
✅ Admin App.tsx has ErrorBoundary + ToastContainer  
✅ Consistent code structure (identical pattern to storefront)  
✅ Admin config.ts created (mirrors storefront settings)  
✅ Zero TypeScript errors  

---

## Timeline

| Phase | Task | Est. Time | Status |
|-------|------|-----------|--------|
| 1 | Toast System | 1-1.5h | ✅ COMPLETED |
| 2 | useAuth Integration | 0.5-0.75h | ✅ COMPLETED |
| 3 | Error Boundaries | 0.75-1h | ✅ COMPLETED |
| 4 | Loading Skeletons | 1-1.5h | ✅ COMPLETED |
| 5 | Admin Sync | 2-3h | ✅ COMPLETED |
| **TOTAL** | **All Phases** | **5-6 hours** | **✅ 5/5 COMPLETED** |

---

## Dependencies

```
Phase 1 (Toast) → Phase 2 (useAuth) → Phase 3 (Error Boundaries) → Phase 4 (Skeletons)
                                                                            ↓
                                                                    Phase 5 (Admin)
```

- ✅ Phase 1 independent (completed immediately)
- ✅ Phase 2 depends on Phase 1 (uses useToast - completed)

- Phase 3 is independent but pairs well after Phase 2
- Phase 4 is independent (can run parallel with Phase 3)
- Phase 5 applies lessons from Phases 1-4

---

## Integration Points

### Toast System
- Integrates with: Redux, config.ts (toastDuration, animationDuration)
- Used by: All pages for notifications
- Benefits: Better UX, non-blocking feedback

### useAuth Integration
- Integrates with: useAuth hook, useToast, authApi
- Used by: Login, Register, ForgotPassword, ResetPassword, Profile
- Benefits: Consistent auth pattern, cleaner code

### Error Boundaries
- Integrates with: React error handling, logging
- Used by: App.tsx root wrapper
- Benefits: Crash recovery, better reliability

### Loading Skeletons
- Integrates with: RTK Query loading states, config.ts (animationDuration)
- Used by: Product, Profile, Cart pages
- Benefits: Better perceived performance

### Admin Sync
- Integrates with: All tools from Phases 1-4
- Used by: Admin panel (mirror of storefront)
- Benefits: Consistent developer experience

---

## Testing Strategy

### Manual Testing
- [ ] Toast notifications (click actions, check auto-dismiss)
- [ ] Auth flows (login, register, forgot password, reset password)
- [ ] Error boundaries (deliberately trigger errors)
- [ ] Loading states (slow network simulation)
- [ ] Admin panel (same flows as storefront)

### Browser Testing
- [ ] Chrome/Edge (primary)
- [ ] Mobile viewport (responsive)
- [ ] Dark mode (if applicable)
- [ ] Network throttling (3G simulation)

### Error Scenarios
- [ ] Invalid credentials
- [ ] Network timeout
- [ ] 400/401/403/404/500 errors
- [ ] Component rendering errors
- [ ] API errors with field validation

---

## Future Improvements (Phase 6+)

- [ ] Add request retry logic
- [ ] Implement form state persistence (localStorage)
- [ ] Add optimistic updates
- [ ] Implement offline support (Service Worker)
- [ ] Add analytics/error tracking (Sentry)
- [ ] Implement request deduplication
- [ ] Add performance monitoring
- [ ] Add A/B testing framework

---

## References

- **Previous Work**: CODE_QUALITY_IMPROVEMENTS.md (Phase completed)
- **Types**: src/types.ts (centralized)
- **Config**: src/config.ts (environment + settings)
- **Hooks**: src/hooks/ (useAuth, useErrorHandler, useLocalStorage, useCartSync)
- **Redux**: src/store/ (RTK Query + Redux Toolkit)

---

**Status**: Plan Created ✅ - Ready for Implementation  
**Next Step**: Execute Phase 1 (Toast System)
