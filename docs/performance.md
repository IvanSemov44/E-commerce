# Performance Guide

## Backend

### Pagination — always paginate list endpoints

Every list endpoint uses `PaginationParameters`. Never return unbounded collections.

```csharp
// Always accept pagination params
public async Task<Result<PagedResult<ProductDto>>> GetProductsAsync(
    ProductQueryParameters parameters,
    CancellationToken cancellationToken = default)
```

Default page size: 20. Max page size: 100 (enforced in `PaginationConstants`).

---

### N+1 queries — the #1 performance killer

**Bad — loads products, then fires a query per product for images:**
```csharp
var products = await _context.Products.ToListAsync();
foreach (var p in products)
    p.Images = await _context.ProductImages.Where(i => i.ProductId == p.Id).ToListAsync();
```

**Good — single query with eager loading:**
```csharp
var products = await _context.Products
    .Include(p => p.Images)
    .Include(p => p.Category)
    .AsNoTracking()
    .ToListAsync();
```

**Rule:** Always use `.Include()` for navigation properties you know you'll need. Use `.AsNoTracking()` on read-only queries.

---

### Projection — select only what you need

**Bad — loads entire entity, maps in memory:**
```csharp
var products = await _context.Products.ToListAsync();
return products.Select(p => new ProductSummaryDto { Name = p.Name, Price = p.Price });
```

**Good — project at the DB level:**
```csharp
var products = await _context.Products
    .Select(p => new ProductSummaryDto { Name = p.Name, Price = p.Price })
    .AsNoTracking()
    .ToListAsync();
```

Projection avoids transferring columns you never use.

---

### Indexes — know which queries are covered

Key indexes on `Products` table:
- `Slug` (unique) — product detail page lookup
- `IsActive` — all catalog queries filter by this
- `IsFeatured` — homepage featured products
- `(IsActive, Price)` composite — filtered + sorted browsing

If you add a new filter or sort field that will be used in production queries, add an EF Core index in the entity configuration. See `.ai/workflows/database-migrations.md`.

---

### Tracking vs no-tracking

| Use case | Setting |
|----------|---------|
| Read-only query (GET endpoints) | `.AsNoTracking()` — faster, less memory |
| Query then modify in same transaction | Default tracking (required for change detection) |

---

### Cancellation tokens — always pass through

```csharp
// Every async method accepts a CancellationToken
public async Task<Result<OrderDto>> GetOrderAsync(Guid id, CancellationToken cancellationToken = default)
{
    var order = await _uow.Orders.GetByIdAsync(id, cancellationToken);
    // ...
}
```

This allows the runtime to cancel DB queries when the HTTP request is aborted (client navigates away), saving DB resources.

---

### Resilience policies (Polly)

Configured in `ECommerce.Infrastructure/Resilience/ResiliencePolicies.cs`:

| Policy | Applies to | Config |
|--------|-----------|--------|
| Retry | DB calls, external HTTP | 3 retries, exponential backoff |
| Circuit breaker | External HTTP (email, payments) | Opens after 5 failures in 60s |
| Bulkhead | External HTTP | Max 10 concurrent calls |
| Timeout | All DB + external calls | 30s default |

---

## Frontend

### RTK Query caching

RTK Query caches query results by endpoint + argument. Default cache lifetime: 60 seconds (`keepUnusedDataFor`).

```typescript
// Override cache lifetime per endpoint
getProducts: builder.query({
  query: (params) => ({ url: '/products', params }),
  keepUnusedDataFor: 300,  // 5 minutes for product catalog
}),
```

**Cache invalidation** — use `providesTags` / `invalidatesTags`:

```typescript
getCart: builder.query({
  providesTags: ['Cart'],
}),
addToCart: builder.mutation({
  invalidatesTags: ['Cart'],  // refetches cart after mutation
}),
```

Missing `invalidatesTags` is the most common cause of stale UI after mutations.

---

### Optimistic updates — cart mutations

Cart `updateItem` and `removeItem` use optimistic updates to feel instant:

```typescript
updateCartItem: builder.mutation({
  onQueryStarted: async (args, { dispatch, queryFulfilled }) => {
    // Immediately update cache
    const patch = dispatch(cartApi.util.updateQueryData('getCart', undefined, (draft) => {
      const item = draft.items.find(i => i.id === args.itemId)
      if (item) item.quantity = args.quantity
    }))
    try {
      await queryFulfilled
    } catch {
      patch.undo()  // rollback on failure
    }
  },
})
```

---

### Memoization — prevent unnecessary re-renders

Use `useMemo` for expensive calculations:

```typescript
// In useCheckoutForm — debounced localStorage writes
const debouncedSave = useMemo(
  () => debounce((draft) => localStorage.setItem('checkout-draft', JSON.stringify(draft)), 500),
  []
)

// Memoized order payload — only recalculates when inputs change
const orderPayload = useMemo(() => buildOrderPayload(formData, cartItems, promoCode), [formData, cartItems, promoCode])
```

---

### Lazy loading — routes

All routes are lazy-loaded via `React.lazy()`:

```typescript
const ProductsPage = lazy(() => import('@/features/products/pages/ProductsPage'))
```

This splits the bundle so only the code for the current page is downloaded. Each lazy route has a `Suspense` boundary with a skeleton fallback.

---

### Bundle size — what to watch

```bash
cd src/frontend/storefront
npm run build
npm run analyze  # opens bundle visualizer
```

Watch for:
- Large dependencies imported globally (import only what you use from lodash, date-fns)
- Icons imported from entire icon library vs individual icons
- Duplicate packages from different import paths

---

## Performance checklist before PR

- [ ] New list endpoints accept `PaginationParameters`
- [ ] New queries use `.AsNoTracking()` where appropriate
- [ ] Navigation properties are eagerly loaded with `.Include()`, not lazy-loaded in loops
- [ ] New RTK Query mutations have `invalidatesTags` set
- [ ] No `any[]` responses on list endpoints — always paginated
- [ ] New background operations don't block the HTTP response
