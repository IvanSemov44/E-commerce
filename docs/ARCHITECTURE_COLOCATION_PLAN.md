# Storefront Architecture Review: Colocation Architecture Plan

## Executive Summary

This document provides a comprehensive architecture review of the `/storefront` directory and presents a detailed implementation plan for transitioning to a **colocation architecture**. Based on the analysis of 150+ files, I recommend adopting this architecture as it will significantly improve maintainability, developer experience, and code organization.

---

## 1. Current Architecture Analysis

### 1.1 Current Folder Structure

```
src/
├── App.tsx                      # Main application entry
├── config.ts                    # Configuration
├── types.ts                     # Global TypeScript types
├── main.tsx                     # React DOM render
├── index.css                    # Global styles
│
├── components/                  # FLAT - all shared components
│   ├── ui/                      # Base UI components (Button, Card, Input)
│   ├── ProductCard.tsx          # Product display
│   ├── Header.tsx               # Navigation header
│   ├── Footer.tsx               # Footer
│   ├── CategoryFilter.tsx       # Category filtering
│   └── __tests__/               # Tests separated from components
│
├── pages/                       # Page components
│   ├── Home.tsx
│   ├── Products.tsx
│   ├── ProductDetail.tsx
│   ├── Checkout.tsx
│   ├── Cart.tsx
│   ├── Profile.tsx
│   └── __tests__/               # Tests separated from pages
│
├── hooks/                       # GLOBAL hooks directory
│   ├── useCart.ts
│   ├── useAuth.ts
│   ├── useCheckout.ts
│   ├── useProductFilters.ts
│   └── __tests__/               # Tests separated from hooks
│
├── store/                       # Redux store
│   ├── store.ts
│   ├── hooks.ts
│   ├── api/                     # RTK Query APIs
│   │   ├── baseApi.ts
│   │   ├── productApi.ts
│   │   ├── cartApi.ts
│   │   └── ...
│   ├── slices/                  # Redux slices
│   │   ├── authSlice.ts
│   │   ├── cartSlice.ts
│   │   └── ...
│   └── middleware/
│
├── i18n/                        # Internationalization
│   ├── locales/
│   └── index.ts
│
└── utils/                       # Utility functions
    ├── constants.ts
    ├── validation.ts
    └── logger.ts
```

### 1.2 Current Architecture Issues

| Issue | Description | Impact |
|-------|-------------|--------|
| **Flat component structure** | 40+ components in single folder | Difficult to locate related components |
| **Separated tests** | Tests in `__tests__/` subdirectories | Hard to find tests, poor visibility |
| **Global hooks** | All hooks in single `/hooks` folder | Hooks not tied to features they serve |
| **Disconnected slices** | Redux slices separated from API | Hard to understand feature boundaries |
| **No feature boundaries** | Cross-feature imports everywhere | High coupling, difficult refactoring |
| **Scattered types** | Some in `types.ts`, some co-located | Inconsistent organization |

### 1.3 What Works Well

- ✅ CSS Modules co-located with components (`Component.module.css`)
- ✅ RTK Query for API layer (excellent pattern)
- ✅ Lazy loading for routes in [`App.tsx`](src/frontend/storefront/src/App.tsx:17)
- ✅ Good test coverage structure (though separated)
- ✅ TypeScript usage throughout

---

## 2. Colocation Architecture Description

### 2.1 What is Colocation?

**Colocation** means keeping related files together based on the feature or domain they belong to, rather than grouping by file type. The principle: *"Files that change together should stay together."*

### 2.2 Core Principles

1. **Feature-First Organization**: Group by business domain (Products, Cart, Auth)
2. **Self-Contained Modules**: Each feature owns its components, hooks, types, and tests
3. **Explicit Exports**: Clear public API via `index.ts` barrel files
4. **Shared Kernel**: Minimal core shared across features
5. **Barrel Pattern**: Clean imports through index files

### 2.3 Proposed Target Structure

```
src/
├── app/                         # Application shell
│   ├── App.tsx
│   ├── routes.ts
│   └── App.module.css
│
├── features/                    # Feature-based modules
│   ├── products/
│   │   ├── components/
│   │   │   ├── ProductCard/
│   │   │   │   ├── ProductCard.tsx
│   │   │   │   ├── ProductCard.module.css
│   │   │   │   └── ProductCard.test.tsx
│   │   │   ├── ProductGrid/
│   │   │   └── index.ts
│   │   ├── hooks/
│   │   │   ├── useProduct.ts
│   │   │   └── useProductFilters.ts
│   │   ├── api/
│   │   │   └── productApi.ts        # MOVE from store/api
│   │   ├── types/
│   │   │   └── product.ts
│   │   └── pages/
│   │       ├── ProductsPage/
│   │       └── ProductDetailPage/
│   │
│   ├── cart/
│   │   ├── components/
│   │   │   ├── CartItem/
│   │   │   └── CartSummary/
│   │   ├── hooks/
│   │   │   └── useCart.ts           # MOVE from hooks/
│   │   ├── api/
│   │   │   └── cartApi.ts
│   │   ├── slices/
│   │   │   └── cartSlice.ts         # MOVE from store/slices
│   │   └── pages/
│   │       └── CartPage/
│   │
│   ├── checkout/
│   │   ├── components/
│   │   ├── hooks/
│   │   │   └── useCheckout.ts
│   │   ├── api/
│   │   └── pages/
│   │       └── CheckoutPage/
│   │
│   ├── auth/
│   │   ├── components/
│   │   ├── hooks/
│   │   │   └── useAuth.ts
│   │   ├── api/
│   │   │   └── authApi.ts
│   │   ├── slices/
│   │   │   └── authSlice.ts
│   │   └── pages/
│   │       ├── LoginPage/
│   │       └── RegisterPage/
│   │
│   ├── orders/
│   │   ├── components/
│   │   ├── hooks/
│   │   ├── api/
│   │   └── pages/
│   │
│   ├── profile/
│   │   ├── components/
│   │   ├── hooks/
│   │   ├── api/
│   │   └── pages/
│   │
│   └── wishlist/
│
├── shared/                      # Truly shared code
│   ├── components/              # Base UI components (Button, Input, Card)
│   │   ├── ui/
│   │   │   ├── Button/
│   │   │   ├── Input/
│   │   │   ├── Card/
│   │   │   └── index.ts
│   │   ├── layouts/
│   │   │   ├── Header/
│   │   │   └── Footer/
│   │   └── hooks/              # Cross-cutting hooks
│   │       ├── useLocalStorage.ts
│   │       └── useOnlineStatus.ts
│   │
│   ├── lib/                    # Libraries & utilities
│   │   ├── api/
│   │   │   └── baseApi.ts      # RTK Query base
│   │   ├── store/
│   │   │   ├── store.ts
│   │   │   └── hooks.ts
│   │   └── utils/
│   │       ├── constants.ts
│   │       └── validation.ts
│   │
│   ├── types/                  # Shared types
│   │   └── index.ts
│   │
│   └── i18n/                   # Internationalization
│
└── pages/                       # OPTIONAL: Could remain for route mapping
                                 # Or move entirely into features/
```

### 2.4 Key Differences: Before vs After

| Aspect | Current | Colocation |
|--------|---------|------------|
| **Components** | Flat `/components` | `features/*/components` |
| **Hooks** | Global `/hooks` | Co-located with features |
| **API** | Global `/store/api` | `features/*/api` |
| **Slices** | Global `/store/slices` | `features/*/slices` |
| **Tests** | Separate `__tests__/` | Next to source files |
| **Types** | Single `types.ts` | Feature-specific + shared |

---

## 3. Implementation Plan

### 3.1 Migration Strategy

**Phase 1: Foundation (Week 1)**

1. Create new directory structure
2. Move shared/lib infrastructure (baseApi, store)
3. Create barrel exports (`index.ts`) for each feature

**Phase 2: Feature Migration (Weeks 2-4)**

1. Migrate one feature at a time
2. Update imports in App.tsx
3. Keep dual structure during migration

**Phase 3: Cleanup (Week 5)**

1. Remove old directory structure
2. Update build configuration if needed
3. Verify all tests pass

### 3.2 Detailed Migration Steps

#### Step 1: Create Feature Structure

```bash
# Create directories
mkdir -p src/features/products/components
mkdir -p src/features/products/hooks
mkdir -p src/features/products/api
mkdir -p src/features/products/types
mkdir -p src/features/products/pages
```

#### Step 2: Migrate Products Feature

```
CURRENT → TARGET

src/components/ProductCard.tsx           → src/features/products/components/ProductCard/
src/components/ProductCard.module.css    → src/features/products/components/ProductCard/
src/hooks/useProductFilters.ts           → src/features/products/hooks/
src/store/api/productApi.ts              → src/features/products/api/
src/pages/Products.tsx                   → src/features/products/pages/ProductsPage/
src/pages/ProductDetail.tsx              → src/features/products/pages/ProductDetailPage/
```

#### Step 3: Update Imports

```typescript
// BEFORE (current)
import ProductCard from '../components/ProductCard';
import { useGetProductsQuery } from '../store/api/productApi';
import { useProductFilters } from '../hooks/useProductFilters';

// AFTER (colocated)
import { ProductCard } from '../features/products/components';
import { useGetProductsQuery } from '../features/products/api/productApi';
import { useProductFilters } from '../features/products/hooks';
```

#### Step 4: Create Barrel Exports

```typescript
// src/features/products/components/index.ts
export { default as ProductCard } from './ProductCard';
export { default as ProductGrid } from './ProductGrid';
// ...
```

### 3.3 Feature Dependency Graph

```
┌─────────────────────────────────────────────────────────────────┐
│                         SHARED/LIB                             │
│              (baseApi, store, hooks, utils)                    │
└─────────────────────────────────────────────────────────────────┘
                              │
        ┌─────────────────────┼─────────────────────┐
        │                     │                     │
        ▼                     ▼                     ▼
┌───────────────┐    ┌───────────────┐    ┌───────────────┐
│   PRODUCTS    │    │     AUTH      │    │    CART       │
│   (feature)   │    │   (feature)   │    │   (feature)   │
└───────────────┘    └───────────────┘    └───────────────┘
        │                     │                     │
        └─────────────┬───────┴─────────────┬───────┘
                      ▼                     ▼
              ┌───────────────┐    ┌───────────────┐
              │   CHECKOUT    │    │   WISHLIST    │
              │   (feature)   │    │   (feature)   │
              └───────────────┘    └───────────────┘
```

### 3.4 Migration Order (Priority)

| Priority | Feature | Rationale |
|----------|---------|-----------|
| 1 | **Products** | Most complex, highest usage |
| 2 | **Cart** | Core e-commerce flow |
| 3 | **Auth** | Required for checkout |
| 4 | **Checkout** | Critical conversion point |
| 5 | **Orders** | User account section |
| 6 | **Wishlist** | Secondary feature |
| 7 | **Profile** | User management |

### 3.5 Import Path Aliases (Optional Enhancement)

To simplify imports, configure TypeScript path aliases:

```json
// tsconfig.json
{
  "compilerOptions": {
    "baseUrl": ".",
    "paths": {
      "@features/products/*": ["src/features/products/*"],
      "@features/cart/*": ["src/features/cart/*"],
      "@shared/*": ["src/shared/*"]
    }
  }
}
```

---

## 4. Benefits of Colocation Architecture

### 4.1 Developer Experience

| Benefit | Description |
|---------|-------------|
| **Faster navigation** | Related files always next to each other |
| **Better discovery** | Easy to find all code related to a feature |
| **Clear boundaries** | Features have explicit public APIs |
| **Easier onboarding** | New devs understand feature in one place |

### 4.2 Maintainability

| Benefit | Description |
|---------|-------------|
| **Isolated changes** | Modify one feature without breaking others |
| **Better testing** | Tests live with source, easier to maintain |
| **Reduced conflicts** | Git merge conflicts minimized |
| **Code ownership** | Clear ownership boundaries possible |

### 4.3 Technical Benefits

| Benefit | Description |
|---------|-------------|
| **Tree-shaking** | Better bundler optimization |
| **Lazy loading** | Features can be code-split independently |
| **Type safety** | Feature-specific types stay encapsulated |

---

## 5. Risks and Mitigations

| Risk | Mitigation |
|------|------------|
| **Breaking existing imports** | Use barrel exports, update gradually |
| **Merge conflicts during migration** | Migrate one feature at a time |
| **Lost test coverage** | Keep tests next to source, verify coverage |
| **Build issues** | Test build after each feature migration |
| **Team learning curve** | Document patterns, pair during migration |

---

## 6. Recommendations

### 6.1 Immediate Actions

1. **Create feature directories** - Set up the new structure
2. **Start with Products feature** - Highest impact migration
3. **Use barrel exports** - Maintain backward compatibility during transition
4. **Update documentation** - Document new import patterns

### 6.2 Long-term Improvements

1. **Consider micro-frontends** - For very large teams
2. **Add feature flags** - For gradual rollouts
3. **Implement workspace monorepo** - If splitting into packages
4. **Add automated migration scripts** - For future refactoring

---

## Conclusion

The colocation architecture is an excellent choice for this e-commerce storefront. It will:

- ✅ Improve code organization and discoverability
- ✅ Reduce coupling between features
- ✅ Make the codebase more maintainable
- ✅ Enhance developer productivity
- ✅ Support better testing practices

The migration can be completed in 5 weeks with minimal risk by following the phased approach outlined above.

---

*Architecture Review prepared by Senior React Developer*
*Date: 2026-02-28*
