# E-Commerce Storefront Co-location Architecture Migration

> Execution model for ongoing one-by-one migration: `COLOCATION_ADOPTION_TRACKER.md`

## ✅ Completed Work

### Phase 1: API Slice Migration (100% Complete)

All RTK Query API slices have been moved to their respective features with backward-compatible re-exports in `store/api/` for gradual migration:

#### Products Feature
- ✅ `productApi.ts` - already in `features/products/api/`
- ✅ `categoriesApi.ts` - moved from `store/api/` → `features/products/api/`
- ✅ `reviewsApi.ts` - moved from `store/api/` → `features/products/api/`
- ✅ Updated barrel export: `features/products/api/index.ts`

#### Auth Feature
- ✅ `authApi.ts` - already in `features/auth/api/`
- ✅ Barrel export configured

#### Cart Feature
- ✅ `cartApi.ts` - already in `features/cart/api/`
- ✅ Barrel export configured

#### Checkout Feature
- ✅ `inventoryApi.ts` - moved from `store/api/` → `features/checkout/api/`
- ✅ `promoCodeApi.ts` - moved from `store/api/` → `features/checkout/api/`
- ✅ Updated barrel export: `features/checkout/api/index.ts`

#### Orders Feature
- ✅ `ordersApi.ts` - already in `features/orders/api/`
- ✅ Barrel export configured

#### Profile Feature
- ✅ `profileApi.ts` - already in `features/profile/api/`
- ✅ Updated barrel export: `features/profile/api/index.ts`

#### Wishlist Feature
- ✅ `wishlistApi.ts` - already in `features/wishlist/api/`
- ✅ Updated barrel export: `features/wishlist/api/index.ts`

### Backward Compatibility
All legacy imports still work via re-exports in `store/api/`:
```typescript
// OLD (still works)
import { useGetProductsQuery } from '../../api/productApi';

// NEW (preferred)
import { useGetProductsQuery } from '../../features/products/api';
```

### Store Configuration Updated
- ✅ `store/store.ts` now imports API slices from their feature locations
- ✅ All API injections point to correct feature folders
- ✅ Redux reducers already in features (authSlice, cartSlice)

---

## 🔄 Next Steps (Recommended Order)

### Phase 2: Move Feature-Specific Hooks
Move hooks from legacy `/hooks/` → feature-specific `/features/*/hooks/`:

```
Current Structure                Next Structure
/hooks/useAuth.ts         →      /features/auth/hooks/useAuth.ts
/hooks/useCart.ts         →      /features/cart/hooks/useCart.ts
/hooks/useCheckout.ts     →      /features/checkout/hooks/useCheckout.ts
/hooks/useProduct*.ts     →      /features/products/hooks/
/hooks/useProfile*.ts     →      /features/profile/hooks/
```

**Shared/Global Hooks** (keep in `/shared/lib/hooks/`):
- `useLocalStorage.ts`
- `useOnlineStatus.ts`
- `usePerformanceMonitor.ts`
- `useErrorHandler.ts` (or `useApiErrorHandler.ts`)

### Phase 3: Consolidate Shared Components
Move base UI components to `shared/components/ui/`:

```
Current: /components/ui/*        →  /shared/components/ui/*
  - Button.tsx / Button.module.css
  - Input.tsx / Input.module.css
  - Card.tsx / Card.module.css
  - etc.

Layout Components: /shared/components/layouts/
  - Header.tsx (from /components/)
  - Footer.tsx (from /components/)
```

**Feature-Specific Components** (stay in features):
- All components in `/features/*/components/` ✅ (Already organized)

### Phase 4: Migrate Pages to Features
Move remaining pages from legacy `/pages/` → features:

```
/pages/Login.tsx           →  /features/auth/pages/LoginPage/
/pages/Register.tsx        →  /features/auth/pages/RegisterPage/
/pages/ForgotPassword.tsx  →  /features/auth/pages/ForgotPasswordPage/
/pages/ResetPassword.tsx   →  /features/auth/pages/ResetPasswordPage/
```

**Legal/Support Pages** (keep in `/pages/` or move to `shared/pages/`):
- PrivacyPolicy, TermsOfService, ReturnsPolicy, etc.
- These don't belong to any specific feature

### Phase 5: Update All Imports
- Update `App.tsx` to import from feature pages instead of `/pages/`
- Update component imports to use barrel exports
- Update hook imports to feature-specific locations

### Phase 6: Cleanup
- Remove legacy `/hooks/` directory once all moved
- Remove legacy `/components/` directory (except shared ones)
- Keep `/pages/` only for legal/support pages, or create `/shared/pages/`
- Verify `/store/slices/` only contains global slices (language, toast)

---

## 📋 Import Path Examples

### After Migration (Recommended)

```typescript
// API Hooks - from features
import {
  useGetProductsQuery,
  useGetCategoriesQuery,
  useCreateReviewMutation,
} from '@/features/products/api';

// Mutation Hooks - from features
import { useAddToCartMutation } from '@/features/cart/api';

// Feature Hooks - from features
import { useProductFilters } from '@/features/products/hooks';
import { useAuth } from '@/features/auth/hooks';

// Shared/UI Components
import { Button, Input, Card } from '@/shared/components/ui';
import { Header, Footer } from '@/shared/components/layouts';

// Feature Components
import { ProductCard, ProductGrid } from '@/features/products/components';

// Pages
import HomePage from '@/features/home/pages/HomePage';
import LoginPage from '@/features/auth/pages/LoginPage';
```

### Optional: Add TypeScript Path Aliases

Update `tsconfig.json`:
```json
{
  "compilerOptions": {
    "baseUrl": ".",
    "paths": {
      "@/features/*": ["src/features/*"],
      "@/shared/*": ["src/shared/*"],
      "@/store/*": ["src/store/*"]
    }
  }
}
```

---

## 📊 Current Directory Status

### ✅ Well Organized (Keep as-is)
- ✅ `/features/*` - Feature-based modules with co-located code
- ✅ `/store/api/` - Now with re-exports for backward compatibility
- ✅ `/store/slices/` - Global slices (language, toast)
- ✅ `/store/hooks.ts` - Redux hooks (useAppDispatch, useAppSelector)
- ✅ `/store/middleware/` - Custom middleware

### 🔄 In Progress
- 🔄 API slices migration (✅ DONE - all moved to features)
- 🔄 Redux slices (✅ PARTIALLY DONE - auth, cart moved; language, toast global)

### ⏳ To Do
- ⏳ Move feature-specific hooks to features
- ⏳ Consolidate shared UI components in `/shared/components/ui/`
- ⏳ Migrate pages to features
- ⏳ Update imports across app
- ⏳ Remove legacy directories

---

## 🎯 Benefits Already Achieved

✅ **Feature Boundaries Clear**: Each feature owns its API slices  
✅ **Backward Compatible**: Old imports still work via re-exports  
✅ **Type Safety**: No breaking changes during migration  
✅ **Store Configuration**: Already points to feature-based APIs  

---

## 🚀 Getting Started with Phase 2

Want to continue? Start by:

1. Create `/features/auth/hooks/index.ts` barrel file
2. Move `useAuth.ts` from `/hooks/` → `/features/auth/hooks/`
3. Update imports in auth components and pages
4. Repeat for each feature

Need help? Let me know which feature you'd like to tackle next!
