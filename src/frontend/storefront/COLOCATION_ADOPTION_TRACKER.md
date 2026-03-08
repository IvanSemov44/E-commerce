# Co-location Adoption Tracker (One-by-One)

Use this tracker to migrate components gradually without a risky big-bang refactor.

## Migration Policy

- **Touch-it, fix-it**: if a PR changes a component, migrate that component folder to the co-location template in the same PR.
- **No behavior changes** during migration PRs unless explicitly scoped.
- **Small batches**: 1-3 components per migration PR.
- **Done means done**: a migrated component must satisfy all checklist items below.

## Definition of Done (Per Component)

- Folder matches co-location shape (component, styles, tests, index).
- Imports updated to the component folder barrel (`index.ts`) where applicable.
- Props are typed; no `any` introduced.
- Tests pass for touched scope.
- React Compiler guidance respected (manual `memo`/`useCallback` only when justified).

## PR Template Snippet

Copy this into PR descriptions for migration PRs:

```markdown
### Co-location Migration

- [ ] Component moved to colocated folder
- [ ] `index.ts` barrel export added/updated
- [ ] Imports updated
- [ ] Tests updated/passing for touched scope
- [ ] No behavior change (or explicitly documented)
```

## Priority Queue

Prioritize in this order:

1. High-traffic UI (catalog, cart, checkout)
2. High-churn components (frequently edited)
3. Leaf/low-risk components

## Progress Board

Status legend: `Not Started` | `In Progress` | `Migrated` | `Blocked`

Initial status is inferred from folder contents:

- `Migrated`: has `.tsx` + `.module.css` + test file + `index.ts`
- `In Progress`: has `.tsx` + `.module.css` + `index.ts` but no test file
- `Not Started`: missing one or more required files for a colocated component folder

| Component           | Location                                               | Priority | Status       | Owner | Notes                                      |
| ------------------- | ------------------------------------------------------ | -------: | ------------ | ----- | ------------------------------------------ |
| ActiveFilters       | `src/features/products/components/ActiveFilters`       |     High | Not Started  | -     | tsx:1 css:1 test:0 idx:False               |
| CartItem            | `src/features/cart/components/CartItem`                |     High | Not Started  | -     | tsx:1 css:1 test:0 idx:False               |
| CartItemList        | `src/features/cart/components/CartItemList`            |     High | **Migrated** | ✅    | tests moved to `__tests__/`, 5/5 passing   |
| CartSummary         | `src/features/cart/components/CartSummary`             |     High | **Migrated** | ✅    | tests moved to `__tests__/`, 11/11 passing |
| CheckoutAuthBanner  | `src/features/checkout/components/CheckoutAuthBanner`  |     High | In Progress  | -     | tsx:1 css:1 test:0 idx:True                |
| CheckoutForm        | `src/features/checkout/components/CheckoutForm`        |     High | **Migrated** | ✅    | tsx:1 css:0 test:0 idx:True types:True     |
| OrderSuccess        | `src/features/checkout/components/OrderSuccess`        |     High | **Migrated** | ✅    | tsx:1 css:0 test:0 idx:True types:True     |
| OrderSummary        | `src/features/checkout/components/OrderSummary`        |     High | **Migrated** | ✅    | tsx:1 css:0 test:0 idx:True types:True     |
| ProductActions      | `src/features/products/components/ProductActions`      |     High | Not Started  | -     | tsx:1 css:1 test:0 idx:False               |
| ProductCard         | `src/features/products/components/ProductCard`         |     High | **Migrated** | ✅    | tests moved to `__tests__/`, 9/9 passing   |
| ProductFilters      | `src/features/products/components/ProductFilters`      |     High | Not Started  | -     | tsx:1 css:1 test:0 idx:False               |
| ProductGrid         | `src/features/products/components/ProductGrid`         |     High | **Migrated** | ✅    | tests moved to `__tests__/`, 5/5 passing   |
| ProductImageGallery | `src/features/products/components/ProductImageGallery` |     High | Not Started  | -     | tsx:1 css:1 test:0 idx:False               |
| ProductInfo         | `src/features/products/components/ProductInfo`         |     High | Not Started  | -     | tsx:1 css:1 test:0 idx:False               |
| ProductSearchBar    | `src/features/products/components/ProductSearchBar`    |     High | Not Started  | -     | tsx:1 css:1 test:0 idx:False               |
| OrderCard           | `src/features/orders/components/OrderCard`             |   Medium | In Progress  | -     | tsx:1 css:1 test:0 idx:True                |
| OrderHeader         | `src/features/orders/components/OrderHeader`           |   Medium | **Migrated** | ✅    | tsx:1 css:0 test:0 idx:True types:True     |
| OrderItemsList      | `src/features/orders/components/OrderItemsList`        |   Medium | **Migrated** | ✅    | tsx:1 css:0 test:0 idx:True types:True     |
| OrderTotals         | `src/features/orders/components/OrderTotals`           |   Medium | **Migrated** | ✅    | tsx:1 css:0 test:0 idx:True types:True     |
| ShippingAddress     | `src/features/orders/components/ShippingAddress`       |   Medium | **Migrated** | ✅    | tsx:1 css:0 test:0 idx:True types:True     |
| AccountDetails      | `src/features/profile/components/AccountDetails`       |   Medium | **Migrated** | ✅    | tsx:1 css:1 test:0 idx:True types:True     |
| ProfileForm         | `src/features/profile/components/ProfileForm`          |   Medium | **Migrated** | ✅    | tsx:1 css:1 test:0 idx:True types:True     |
| ProfileHeader       | `src/features/profile/components/ProfileHeader`        |   Medium | **Migrated** | ✅    | tsx:1 css:1 test:0 idx:True types:True     |
| ProfileMessages     | `src/features/profile/components/ProfileMessages`      |   Medium | **Migrated** | ✅    | tsx:1 css:1 test:0 idx:True types:True     |

## Flat Components to Split into Folders

These files are currently directly under feature `components/` roots and should be migrated into per-component folders.

| Component       | Current Location                                      | Priority | Target Status              |
| --------------- | ----------------------------------------------------- | -------: | -------------------------- |
| CartItem        | `src/features/cart/components/CartItem.tsx`           |     High | Move to folder ✅ **DONE** |
| OrderSuccess    | `src/features/checkout/components/OrderSuccess.tsx`   |     High | Move to folder ✅ **DONE** |
| OrderSummary    | `src/features/checkout/components/OrderSummary.tsx`   |     High | Move to folder ✅ **DONE** |
| OrderHeader     | `src/features/orders/components/OrderHeader.tsx`      |   Medium | Move to folder ✅ **DONE** |
| OrderItemsList  | `src/features/orders/components/OrderItemsList.tsx`   |   Medium | Move to folder ✅ **DONE** |
| OrderTotals     | `src/features/orders/components/OrderTotals.tsx`      |   Medium | Move to folder ✅ **DONE** |
| ShippingAddress | `src/features/orders/components/ShippingAddress.tsx`  |   Medium | Move to folder ✅ **DONE** |
| AccountDetails  | `src/features/profile/components/AccountDetails.tsx`  |   Medium | Move to folder ✅ **DONE** |
| ProfileForm     | `src/features/profile/components/ProfileForm.tsx`     |   Medium | Move to folder ✅ **DONE** |
| ProfileHeader   | `src/features/profile/components/ProfileHeader.tsx`   |   Medium | Move to folder             |
| ProfileMessages | `src/features/profile/components/ProfileMessages.tsx` |   Medium | Move to folder             |
| WishlistCard    | `src/features/wishlist/components/WishlistCard.tsx`   |   Medium | Move to folder             |

## Weekly Cadence

- Target **3-5 components per sprint**.
- Review tracker in frontend standup once per week.
- After reaching ~70% migrated, switch policy from advisory to strict enforcement for new/modified components.
