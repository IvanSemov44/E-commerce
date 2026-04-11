# Test Structure

Quick reference for where tests live and what they cover.

---

## Backend Test Projects

| Project | Test Files | Layer Coverage |
|---|---|---|
| `ECommerce.Tests` | 56 | Integration (API controllers), Unit (middleware, filters, architecture) |
| `ECommerce.Catalog.Tests` | 5 | Domain + Application (handlers, queries) |
| `ECommerce.Inventory.Tests` | 5 | Domain + Application (handlers) |
| `ECommerce.Catalog.Tests` | 5 | Domain + Application |
| `ECommerce.Identity.Tests` | 4 | Domain + Application |
| `ECommerce.Ordering.Tests` | 3 | Domain + Application |
| `ECommerce.Shopping.Tests` | 3 | Domain + Application |
| `ECommerce.Promotions.Tests` | 3 | Domain + Application |
| `ECommerce.Reviews.Tests` | 2 | Domain + Application |

**Total Backend Test Files:** 81 across 8 projects

### Test File Types

- **Domain Tests** (`Domain/*Tests.cs`): Aggregate behavior, invariants, value object equality
- **Application Tests** (`Application/*Tests.cs`): Command/query handlers, validation, business rules
- **Integration Tests** (`Integration/*Tests.cs`): API endpoints, HTTP responses, auth flow
- **Unit Tests** (`Unit/*Tests.cs`): Middleware, filters, helpers, architecture rules
- **Characterization Tests** (`*CharacterizationTests.cs`): Existing behavior documentation before refactoring

---

## Frontend Test Files

| Category | Test Files | Tests |
|---|---|---|
| **Storefront** | 108 | 891 (all passing) |

**Test File Breakdown:**

- **Components** — `ProductCard`, `CategoryFilter`, `ProductActions`, `CartItem`, `Header`, etc.
- **Hooks** — `useCart`, `useCartSync`, `useProductData`, `useProductFilters`, `useToast`
- **Pages** — `LoginPage`, `RegisterPage`, `ProductsPage`, `ProductDetailPage`, `WishlistPage`
- **Slices** — `cartSlice`, `authSlice`, `toastSlice`
- **Utilities** — `useLocalStorage`, `useOnlineStatus`, `test-utils`

**Total Frontend Tests:** 891 (all passing)

---

## Quick Reference Map

### Backend

```
src/backend/
├── Catalog/
│   └── ECommerce.Catalog.Tests/
│       ├── Domain/         → Category, Product aggregates
│       └── Application/    → Command & query handlers
├── Identity/
│   └── ECommerce.Identity.Tests/
│       ├── Domain/
│       └── Application/
├── Inventory/
│   └── ECommerce.Inventory.Tests/
├── Ordering/
│   └── ECommerce.Ordering.Tests/
├── Promotions/
│   └── ECommerce.Promotions.Tests/
├── Reviews/
│   └── ECommerce.Reviews.Tests/
├── Shopping/
│   └── ECommerce.Shopping.Tests/
├── ECommerce.Tests/
│   ├── Integration/        → All API controller tests
│   │   ├── AuthControllerTests
│   │   ├── ProductsControllerTests
│   │   ├── OrdersControllerTests
│   │   └── ... (56 total)
│   └── Unit/               → Middleware, filters, architecture
│       ├── Middleware/
│       ├── ActionFilters/
│       └── Architecture/
```

### Frontend

```
src/frontend/storefront/src/
├── app/                     → App-level hooks, components
│   ├── hooks/__tests__/
│   └── layouts/
├── features/
│   ├── auth/
│   │   └── pages/           → Login, Register, ForgotPassword, ResetPassword
│   ├── cart/
│   │   ├── components/      → CartItem, CartSummary
│   │   ├── hooks/           → useCart, useCartSync
│   │   └── slices/         → cartSlice tests
│   ├── checkout/
│   ├── orders/
│   ├── products/
│   │   ├── components/     → ProductCard, ProductActions, CategoryFilter
│   │   ├── pages/          → ProductDetailPage, ProductsPage
│   │   └── hooks/          → useProductData, useCartActions
│   ├── profile/
│   └── wishlist/
├── shared/
│   ├── components/
│   │   └── ui/             → Button, Input, Card, etc.
│   ├── hooks/              → useLocalStorage, useOnlineStatus
│   └── lib/test/           → test-utils, msw-server
```

---

## Running Tests

### Backend

```bash
# All backend tests
dotnet test src/backend/ECommerce.Tests/ECommerce.Tests.csproj

# Specific BC tests
dotnet test src/backend/Catalog/ECommerce.Catalog.Tests/ECommerce.Catalog.Tests.csproj
```

### Frontend

```bash
# All storefront tests
cd src/frontend/storefront && npm test

# Specific test file
npm test -- src/features/auth/pages/LoginPage/__tests__/LoginPage.test.tsx
```

---

## Related Docs

- [taxonomy.md](taxonomy.md) — What test type goes where
- [naming-conventions.md](naming-conventions.md) — File & method naming
- [coverage-targets.md](coverage-targets.md) — Minimum coverage expectations
- [anti-patterns.md](anti-patterns.md) — What NOT to do
- Pattern docs in [patterns/](patterns/) — Layer-specific guidance