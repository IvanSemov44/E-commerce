# Frontend Loading & Skeleton Architecture

Updated: 2026-03-10
Owner: @ivans

## Purpose

Define where loading UI lives, how it is composed, and which component to reach for in each loading scenario.

## Core Rules

1. Skeletons owned by a feature live in that feature's `components/` folder.
2. App-level loading UI (bootstrap, route transitions) lives in `src/app/skeletons/`.
3. The shared `Skeleton` primitive and building blocks (`SkeletonCard`, `SkeletonLabelRow`) live in `shared/components/Skeletons/`.
4. Import skeletons from the barrel, not deep paths.
5. New skeletons always use `variant="rounded"` and `animation="wave"` for visual consistency.

## Where Things Live

### `shared/components/Skeletons/` — primitives and building blocks

| Component | Purpose |
|---|---|
| `Skeleton` | Base primitive. Use to compose all other skeletons. |
| `SkeletonCard` | Image + text lines + optional children. Use for product/content card shapes. |
| `SkeletonLabelRow` | Flex row of skeleton items. Use for label/value pairs and meta rows. |

### `app/skeletons/` — app-level loading UI

| Component | Purpose |
|---|---|
| `TopLoadingBar` | Thin progress bar shown during bootstrap phase 1 (150–900ms). |
| `AppShellSkeleton` | Full header + content placeholder for bootstrap phase 2 (>900ms). |
| `AppBootstrapLoading` | Orchestrates bootstrap phases. Entry point: `AppShell`. |
| `RouteLoadingFallback` | `React.Suspense` fallback for lazy route chunks. Entry point: `App`. |

### Feature folders — feature-owned skeletons

| Skeleton | Location |
|---|---|
| `ProductSkeleton` | `features/products/components/ProductSkeleton/` |
| `ProductsGridSkeleton` | `features/products/components/ProductsGridSkeleton/` |
| `CartSkeleton` | `features/cart/components/CartSkeleton/` |
| `ProfileSkeleton` | `features/profile/components/ProfileSkeleton/` |
| `WishlistSkeleton` | `features/wishlist/components/WishlistSkeleton/` |
| `OrdersListSkeleton` | `features/orders/components/OrdersListSkeleton/` |
| `OrderDetailSkeleton` | `features/orders/components/OrderDetailSkeleton/` |

## Import Rules

Shared primitives and building blocks:
```ts
import { Skeleton, SkeletonCard, SkeletonLabelRow } from '@/shared/components/Skeletons';
```

App-level loading UI:
```ts
import { RouteLoadingFallback, AppBootstrapLoading } from '@/app/skeletons';
```

Feature skeletons — import from the feature barrel:
```ts
import { ProductsGridSkeleton } from '@/features/products/components';
import { CartSkeleton } from '@/features/cart/components';
import { OrdersListSkeleton, OrderDetailSkeleton } from '@/features/orders/components';
```

## Loading Scenarios

### 1. App bootstrap

Entry: `AppShell` → `AppBootstrapLoading`

| Phase | Duration | UI |
|---|---|---|
| 0 | 0–150ms | Nothing |
| 1 | 150–900ms | `TopLoadingBar` |
| 2 | >900ms | `AppShellSkeleton` |

Timing constants live in `shared/lib/utils/constants.ts`:
- `BOOTSTRAP_TOP_BAR_DELAY_MS`
- `BOOTSTRAP_FULL_FALLBACK_DELAY_MS`

### 2. Route chunk loading

Entry: `App` → `React.Suspense` fallback → `RouteLoadingFallback`

### 3. Feature data loading

Pass the feature skeleton via `QueryRenderer`:
```tsx
<QueryRenderer
  isLoading={isLoading}
  loadingSkeleton={{ custom: <ProductsGridSkeleton count={12} /> }}
  ...
/>
```

`QueryRenderer` falls back to `QueryRendererSkeleton` (generic card/text/image) when no `custom` is provided.

## Structure Convention

Each skeleton is colocated in its own folder:
```
ComponentName/
  ComponentName.tsx
  ComponentName.module.css
  ComponentName.test.tsx
  index.ts
```

## Naming Convention

| Scope | Pattern | Example |
|---|---|---|
| Primitive | `Skeleton` | `Skeleton` |
| Building block | `Skeleton<Shape>` | `SkeletonCard`, `SkeletonLabelRow` |
| App-level | Descriptive | `AppShellSkeleton`, `RouteLoadingFallback` |
| Feature | `<Feature>Skeleton` | `ProductSkeleton`, `CartSkeleton` |

## Common Mistakes

- Importing a feature skeleton from `@/shared/components/Skeletons` — it no longer lives there.
- Adding feature-specific CSS classes to `Skeleton/Skeleton.module.css` — each skeleton owns its own CSS module.
- Creating a new card-shaped skeleton manually instead of composing `SkeletonCard`.
- Using magic numbers for timing instead of the constants in `constants.ts`.
