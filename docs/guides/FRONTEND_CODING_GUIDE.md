# Frontend Coding Guide — E-Commerce Platform

Comprehensive coding standards for the React 19 + TypeScript frontend, using Vite, Redux Toolkit, and RTK Query.

**Companion to**: [BACKEND_CODING_GUIDE.md](BACKEND_CODING_GUIDE.md) — This document focuses exclusively on frontend patterns while the backend guide covers the ASP.NET Core API layer.

---

## Tech Stack

- **React**: 19.2+ (functional components only, hooks-based)
- **TypeScript**: 5.9+ (strict mode enforced, no `any` types allowed)
- **Build Tool**: Vite 7+ (ESM-first, fast HMR, environment variables)
- **State Management**: Redux Toolkit 2.11+ + RTK Query (server state vs client state separation)
- **Routing**: React Router 7+ (data loading, lazy routes)  
- **HTTP Client**: RTK Query's fetchBaseQuery (no direct Axios usage)
- **Styling**: CSS Modules (scoped `.module.css` files)
- **Linting**: ESLint 9+ with TypeScript support
- **Testing**: Vitest (preferred) or Jest + React Testing Library

**Frontend-Backend Boundary**: Frontend consumes backend APIs defined in [BACKEND_CODING_GUIDE.md](BACKEND_CODING_GUIDE.md). All DTOs and API contracts are shared — frontend mirrors backend `ApiResponse<T>` structure exactly.

---

## Project Structure

```
src/
├── assets/                  # Images, icons, fonts
├── components/
│   ├── ui/                  # Reusable UI components (Button, Card, Modal, etc.)
│   │   ├── Button.tsx
│   │   ├── Button.module.css
│   │   └── index.ts
│   ├── layout/              # Layout wrappers (Header, Footer, Sidebar)
│   │   ├── Header.tsx
│   │   ├── Header.module.css
│   │   └── index.ts
│   ├── features/            # Feature-specific components (ProductCard, CartItem)
│   │   ├── Product/
│   │   │   ├── ProductCard.tsx
│   │   │   ├── ProductCard.module.css
│   │   │   └── index.ts
│   │   └── Cart/
│   │       ├── CartItem.tsx
│   │       └── index.ts
│   └── index.ts             # Barrel export
├── hooks/                   # Custom React hooks
│   ├── useAsync.ts
│   ├── usePagination.ts
│   └── index.ts
├── pages/                   # Page components (route-level)
│   ├── ProductsPage.tsx
│   ├── ProductDetailPage.tsx
│   ├── CheckoutPage.tsx
│   └── components/          # Page-specific feature components
│       ├── ProductsPage/
│       │   ├── ProductFilter.tsx
│       │   ├── ProductSort.tsx
│       │   ├── ProductGrid.tsx
│       │   └── index.ts
│       ├── ProductDetail/
│       │   ├── ProductImages.tsx
│       │   ├── ProductInfo.tsx
│       │   ├── PriceSection.tsx
│       │   └── index.ts
│       └── Checkout/
│           ├── OrderSummary.tsx
│           ├── ShippingForm.tsx
│           ├── PaymentForm.tsx
│           └── index.ts
├── store/
│   ├── api/                 # RTK Query APIs (one per feature)
│   │   ├── productApi.ts
│   │   ├── cartApi.ts
│   │   ├── authApi.ts
│   │   └── index.ts
│   ├── slices/              # Redux slices (auth, ui, cart)
│   │   ├── authSlice.ts
│   │   ├── uiSlice.ts
│  

## Page Organization Formula (Critical Pattern)

### The Orchestration Rule

**Formula**: `Page Component = Data Fetching + Layout Composition + Event Orchestration`

```
┌─────────────────────────────────────────────────────────────┐
│ Page Component (Thin Layer — 50-100 lines max)             │
│                                                             │
│  ✓ RTK Query hooks (data fetching)                         │
│  ✓ Route-level state (filters, pagination)                 │
│  ✓ Layout structure (composition)                          │
│  ✓ Event handlers (orchestration)                          │
│  ✗ NO business logic                                       │
│  ✗ NO complex JSX (extract to components)                  │
│  ✗ NO direct UI rendering (delegate to children)           │
└─────────────────────────────────────────────────────────────┘
                          │
                          ▼
         ┌────────────────┴────────────────┐
         │                                  │
         ▼                                  ▼
┌─────────────────────┐          ┌─────────────────────┐
│ Page-Specific       │          │ Shared Feature      │
│ Components          │          │ Components          │
│                     │          │                     │
│ pages/components/   │          │ components/         │
│   PageName/         │          │   features/         │
│                     │          │   ui/               │
│ Used by 1 page only │          │ Used by 2+ pages    │
└─────────────────────┘          └─────────────────────┘
```

### Decision Tree: Where Does This Component Go?

```
                    Creating a Component?
                            │
                            ▼
              ┌─────────────────────────────┐
              │ Is it a pure UI element?    │
              │ (Button, Card, Modal, etc.) │
              └─────────────────────────────┘
                    │           │
              Yes   │           │   No
                    ▼           ▼
           components/ui/   ┌─────────────────────────────┐
                            │ Is it used by 2+ pages?     │
                            └─────────────────────────────┘
                                  │           │
                            Yes   │           │   No
                                  ▼           ▼
                      components/features/   ┌─────────────────────────────┐
                                             │ Is it a layout wrapper?     │
                                             │ (Header, Footer, Sidebar)   │
                                             └─────────────────────────────┘
                                                   │           │
                                             Yes   │           │   No
                                                   ▼           ▼
                                        components/layout/   pages/components/
                                                             {PageName}/
```

### Component Classification Rules

| Component Type | Location | Example | Usage Pattern |
|---------------|----------|---------|---------------|
| **Pure UI** | `components/ui/` | Button, Input, Card, Modal, Dropdown | Used everywhere, no business logic |
| **Layout Wrappers** | `components/layout/` | Header, Footer, Sidebar, PageWrapper | Structural, wraps content |
| **Shared Features** | `components/features/{Feature}/` | ProductCard, CartItem, OrderCard | Used by 2+ pages, feature-specific |
| **Page Features** | `pages/components/{PageName}/` | ProductFilter, ShippingForm, OrderSummary | Used by 1 page only |
| **Pages** | `pages/` | ProductsPage, CheckoutPage | Orchestration only, thin layer |

### Size Thresholds (When to Extract)

```typescript
// ❌ BAD — Page component over 150 lines, complex JSX embedded
export default function ProductsPage() {
  const [filters, setFilters] = useState({});
  const { data } = useGetProductsQuery(filters);

  return (
    <main>
      <h1>Products</h1>
      {/* 100+ lines of filter UI code here */}
      <aside>
        <div className={styles.filterSection}>
          <h3>Categories</h3>
          {/* Complex filter logic... */}
        </div>
        <div className={styles.priceRange}>
          {/* Complex price slider... */}
        </div>
      </aside>
      
      {/* 100+ lines of product grid code here */}
      <section>
        <div className={styles.grid}>
          {data?.items.map(product => (
            <div className={styles.card}>
              {/* Complex product card... */}
            </div>
          ))}
        </div>
      </section>
    </main>
  );
}

// ✅ GOOD — Page is orchestration layer only (50 lines)
export default function ProductsPage() {
  const [filters, setFilters] = useState({});
  const { data } = useGetProductsQuery(filters);

  return (
    <main className={styles.page}>
      <h1>Products</h1>
      <div className={styles.layout}>
        <ProductFilter filters={filters} onChange={setFilters} />
        <ProductGrid products={data?.items || []} />
      </div>
    </main>
  );
}
```

**Extraction Triggers**:
- Page component exceeds **100 lines**
- Section of JSX exceeds **30 lines**
- Logic block is reusable or testable in isolation
- Section has its own state management
- Visual complexity makes page hard to scan

### Naming Conventions for Page Components

```typescript
// Pattern: {PageName}{ComponentPurpose}
// Location: pages/components/{PageName}/{ComponentPurpose}.tsx

// ProductsPage components
pages/components/ProductsPage/
├── ProductFilter.tsx       // ✅ Clear: filters for ProductsPage
├── ProductSort.tsx         // ✅ Clear: sorting for ProductsPage
├── ProductGrid.tsx         // ✅ Clear: grid layout for ProductsPage
└── index.ts                // Barrel export

// CheckoutPage components
pages/components/Checkout/
├── ShippingForm.tsx        // ✅ Clear: shipping step
├── PaymentForm.tsx         // ✅ Clear: payment step
├── OrderSummary.tsx        // ✅ Clear: order review
└── index.ts

// ❌ BAD — Generic names lose context
pages/components/ProductsPage/
├── Filter.tsx              // ❌ Too generic
├── Grid.tsx                // ❌ What kind of grid?
├── List.tsx                // ❌ List of what?
```

### Import/Export Pattern for Page Components

```typescript
// pages/components/ProductsPage/index.ts — Barrel export
export { default as ProductFilter } from './ProductFilter';
export { default as ProductSort } from './ProductSort';
export { default as ProductGrid } from './ProductGrid';

// pages/ProductsPage.tsx — Clean imports
import { ProductFilter, ProductSort, ProductGrid } from './components/ProductsPage';

// ✅ Benefits:
// - Single import line
// - Clear component origin
// - Easy to refactor
// - Autocomplete works perfectly
```

### Real-World Example: ProductsPage Refactoring

**Before** (400+ lines, tangled):
```tsx
// pages/ProductsPage.tsx
export default function ProductsPage() {
  const [filters, setFilters] = useState({});
  const [sortBy, setSortBy] = useState('newest');
  const [selectedCategory, setSelectedCategory] = useState('');
  const { data: categories } = useGetCategoriesQuery();
  const { data: products } = useGetProductsQuery({ filters, sortBy });

  // 50 lines of filter logic
  const handleCategoryChange = (id: string) => { /* ... */ };
  const handlePriceChange = (min: number, max: number) => { /* ... */ };
  
  // 50 lines of sort logic
  const handleSortChange = (value: string) => { /* ... */ };
  
  return (
    <main>
      {/* 100 lines of filter JSX */}
      <aside>...</aside>
      
      {/* 100 lines of sort JSX */}
      <div>...</div>
      
      {/* 100 lines of grid JSX */}
      <section>...</section>
    </main>
  );
}
```

**After** (50 lines, clear):
```tsx
// pages/ProductsPage.tsx — ORCHESTRATION ONLY
import { useState } from 'react';
import { useGetProductsQuery } from '../store/api/productApi';
import QueryRenderer from '../components/QueryRenderer';
import { ProductFilter, ProductSort, ProductGrid } from './components/ProductsPage';
import styles from './ProductsPage.module.css';

export default function ProductsPage() {
  const [filters, setFilters] = useState({});
  const [sortBy, setSortBy] = useState('newest');

  const productsQuery = useGetProductsQuery({
    page: 1,
    limit: 20,
    ...filters,
    sortBy,
  });

  return (
    <main className={styles.page}>
      <h1>Products</h1>
      
      <div className={styles.layout}>
        <aside className={styles.sidebar}>
          <ProductFilter filters={filters} onChange={setFilters} />
        </aside>

        <section className={styles.content}>
          <ProductSort value={sortBy} onChange={setSortBy} />
          
          <QueryRenderer query={productsQuery}>
            {(data) => <ProductGrid products={data.items} />}
          </QueryRenderer>
        </section>
      </div>
    </main>
  );
}

// pages/components/ProductsPage/ProductFilter.tsx — EXTRACTED
import { useGetCategoriesQuery } from '../../../store/api/categoriesApi';
import styles from './ProductFilter.module.css';

interface ProductFilterProps {
  filters: {
    category?: string;
    priceRange?: [number, number];
  };
  onChange: (filters: any) => void;
}

export default function ProductFilter({ filters, onChange }: ProductFilterProps) {
  const { data: categories } = useGetCategoriesQuery();

  const handleCategoryChange = (categoryId: string) => {
    onChange({ ...filters, category: categoryId });
  };

  return (
    <div className={styles.filter}>
      <h3>Filter</h3>
      
      <div className={styles.section}>
        <label>Category</label>
        {categories?.map((cat) => (
          <button
            key={cat.id}
            className={filters.category === cat.id ? styles.active : ''}
            onClick={() => handleCategoryChange(cat.id)}
          >
            {cat.name}
          </button>
        ))}
      </div>
      
      {/* Price range controls */}
    </div>
  );
}

// pages/components/ProductsPage/ProductGrid.tsx — EXTRACTED
import ProductCard from '../../../components/features/Product/ProductCard';
import type { Product } from '../../../types/entities';
import styles from './ProductGrid.module.css';

interface ProductGridProps {
  products: Product[];
}

export default function ProductGrid({ products }: ProductGridProps) {
  if (products.length === 0) {
    return <div className={styles.empty}>No products found</div>;
  }

  return (
    <div className={styles.grid}>
      {products.map((product) => (
        <ProductCard
          key={product.id}
          id={product.id}
          name={product.name}
          slug={product.slug}
          price={product.price}
          imageUrl={product.images[0]?.url}
          rating={Math.round(product.averageRating)}
          reviewCount={product.reviewCount}
        />
      ))}
    </div>
  );
}
```

### Benefits Achieved

| Before (Monolithic) | After (Extracted) |
|---------------------|-------------------|
| 400+ line page file | 50-line orchestration file |
| Hard to understand flow | Clear composition pattern |
| Can't test filter in isolation | Each component testable |
| Difficult to reuse logic | Components are reusable |
| Merge conflicts common | Changes isolated to features |
| Poor code discoverability | Clear folder structure |
| Slow IDE performance | Fast syntax highlighting |

### Testing Benefits

```typescript
// ❌ BEFORE — Must test entire page
test('ProductsPage filters work', () => {
  render(<ProductsPage />);
  // Complex: page includes auth, routing, multiple queries
  // Hard to isolate filter behavior
});

// ✅ AFTER — Test component in isolation
test('ProductFilter updates filters', () => {
  const onChange = vi.fn();
  render(<ProductFilter filters={{}} onChange={onChange} />);
  
  fireEvent.click(screen.getByText('Electronics'));
  
  expect(onChange).toHaveBeenCalledWith({ category: 'electronics' });
});
```

### Migration Checklist

When refactoring an existing page:

1. **Identify extraction candidates**:
   - [ ] Sections over 30 lines of JSX
   - [ ] Repeated patterns
   - [ ] Independent state management
   - [ ] Logical groupings (filters, forms, etc.)

2. **Create component structure**:
   - [ ] Create `pages/components/{PageName}/` folder
   - [ ] Move first component
   - [ ] Add `index.ts` barrel export
   - [ ] Update page imports

3. **Define clean interfaces**:
   - [ ] Props are data + handlers only
   - [ ] No prop drilling (use context if needed)
   - [ ] TypeScript interfaces defined

4. **Verify extraction**:
   - [ ] Page file under 100 lines
   - [ ] Each component under 150 lines
   - [ ] Components are independently testable
   - [ ] No circular dependencies

--- │   └── index.ts
│   ├── hooks.ts             # useAppDispatch, useAppSelector (typed)
│   └── store.ts             # Redux store configuration
├── types/
│   ├── api.ts               # API request/response types
│   ├── entities.ts          # Domain entity types
│   └── common.ts            # Shared types (PaginatedResult, ApiResponse)
├── utils/
│   ├── formatters.ts        # Format utilities (currency, date)
│   ├── validators.ts        # Form validation logic
│   └── constants.ts         # App-wide constants
├── App.tsx                  # Root component + routing
├── main.tsx                 # App entry point
└── index.css                # Global styles (minimal)
```

---

## Core Patterns & Conventions

### 1. File Naming (Aligned with Backend)

**Frontend follows C# PascalCase for components but uses camelCase for utilities** (complementing backend's C# conventions):

| Type | Pattern | Example | Backend Equivalent |
|------|---------|---------|-------------------|
| Components | `PascalCase.tsx` | `ProductCard.tsx` | `ProductController.cs` |
| CSS Modules | `ComponentName.module.css` | `ProductCard.module.css` | N/A |
| Hooks | `camelCase.ts` (starts with `use`) | `useProductFilter.ts` | N/A |
| Utils/helpers | `camelCase.ts` | `formatPrice.ts` | Static helper methods |
| Slices | `featureSlice.ts` | `authSlice.ts` | `FeatureService.cs` |
| API files | `featureApi.ts` | `productApi.ts` | `ProductController.cs` |
| Types | `camelCase.ts` | `api.ts`, `entities.ts` | `ProductDto.cs`, `Entities/` |
| Constants | `CONSTANT_CASE` inside files | `DEFAULT_PAGE_SIZE = 20` | `const int DefaultPageSize` |

**Note**: Frontend types mirror backend DTOs exactly — `ProductDto` becomes `Product` interface.

### 2. TypeScript Configuration

**tsconfig.json rules:**
```json
{
  "compilerOptions": {
    "strict": true,
    "noImplicitAny": true,
    "noImplicitThis": true,
    "strictNullChecks": true,
    "strictFunctionTypes": true,
    "esModuleInterop": true,
    "skipLibCheck": true,
    "forceConsistentCasingInFileNames": true,
    "resolveJsonModule": true,
    "noUnusedLocals": true,
    "noUnusedParameters": true,
    "noImplicitReturns": true
  }
}
```

**Key rule**: Never use `any` type. Use union types, generics, or proper interface definitions.

### 3. Import Organization

Group imports in this order:
1. React & external libraries
2. Internal components/hooks/utils
3. Types/interfaces
4. Styles (last)

```tsx
import { useState, useCallback } from 'react';
import { useNavigate } from 'react-router-dom';
import { useAppDispatch, useAppSelector } from '../store/hooks';

import ProductCard from '../components/ProductCard';
import Header from '../components/layout/Header';
import { useProductFilter } from '../hooks/useProductFilter';

import type { Product, PaginatedResult } from '../types/entities';
import type { ProductQueryParams } from '../store/api/productApi';

import styles from './ProductsPage.module.css';
```

---

## React 19 Best Practices

### 1. Functional Components (Only)

Always use functional components with hooks. Class components are legacy.

```tsx
// ✅ GOOD
interface UserProfileProps {
  userId: string;
}

export default function UserProfile({ userId }: UserProfileProps) {
  const [profile, setProfile] = useState<User | null>(null);
  
  return <div>{profile?.name}</div>;
}

// ❌ BAD — Class components
class UserProfile extends Component { }
```

### 2. Props Interface Pattern

Define props explicitly via interfaces placed at the top of the file.

```tsx
interface ProductCardProps {
  id: string;
  name: string;
  price: number;
  imageUrl: string;
  onAddToCart?: (id: string) => void;     // Optional callback
  isLoading?: boolean;
  variant?: 'default' | 'compact';        // Literal union for variants
}

export default function ProductCard(props: ProductCardProps) {
  const { id, name, price, imageUrl, onAddToCart, isLoading = false, variant = 'default' } = props;
  return (
    {/* ... */}
  );
}
```

### 3. Hook Rules (React 19)

- **Hooks only in functional components**, at top level (not inside loops/conditions)
- **Custom hooks** start with `use` prefix
- **Prefer composition** over custom HOCs in React 19
- **Use `useCallback`** for stable function references (especially for callbacks passed to child components)
- **Use `useMemo`** sparingly — only for expensive computations or object identity

```tsx
// ✅ GOOD — Hook at top level
function OrderList() {
  const [filters, setFilters] = useState<OrderFilter>({});
  
  const handleFilterChange = useCallback((newFilters: OrderFilter) => {
    setFilters(newFilters);
  }, []);
  
  return <FilterComponent onChange={handleFilterChange} />;
}

// ❌ BAD — Hook inside condition
function BadComponent() {
  if (someCondition) {
    const [state, setState] = useState(0); // ❌ Hook not at top level
  }
}
```

### 4. Controlled vs Uncontrolled Components

Use **controlled components** for forms (state-driven).

```tsx
// ✅ GOOD — Controlled component
interface SearchProps {
  value: string;
  onChange: (value: string) => void;
}

export function SearchInput({ value, onChange }: SearchProps) {
  return (
    <input
      type="text"
      value={value}
      onChange={(e) => onChange(e.target.value)}
      placeholder="Search products..."
    />
  );
}

// ❌ BAD — Uncontrolled (unless really necessary)
export function SearchInput() {
  const inputRef = useRef<HTMLInputElement>(null);
  // Avoid this pattern unless you need ref access
}
```

### 5. Component Composition

Break down large components into smaller, single-responsibility pieces.

```
ProductDetail.tsx (parent component)
├── ProductImage.tsx
├── ProductInfo.tsx
├── PriceSection.tsx
├── RatingSection.tsx
└── AddToCartSection.tsx
```

Each sub-component should be testable in isolation.

### 6. Children & Slots Pattern

Use React's `children` prop for flexible composition:

```tsx
interface CardProps {
  title: string;
  children: React.ReactNode;
  footer?: React.ReactNode;
}

export function Card({ title, children, footer }: CardProps) {
  return (
    <div className={styles.card}>
      <h2>{title}</h2>
      <div>{children}</div>
      {footer && <div className={styles.footer}>{footer}</div>}
    </div>
  );
}

// Usage
<Card title="Order Details" footer={<button>Proceed</button>}>
  <OrderInfo order={order} />
</Card>
```

---

## TypeScript Patterns

### 1. Type Safety First

Never use `any`. Use union types, generics, or `unknown` (with type guards).

```tsx
// ✅ GOOD
type ApiResponse<T> = {
  success: boolean;
  data?: T;
  error?: string;
};

interface Product {
  id: string;
  name: string;
  price: number;
}

async function fetchProduct(id: string): Promise<ApiResponse<Product>> {
  const response = await fetch(`/api/products/${id}`);
  return response.json();
}

// ❌ BAD
function fetchProduct(id: any): any {
  // ...
}
```

### 2. Generic Types for Reusable Logic

```tsx
// ✅ GOOD — Generic hook for async data fetching
interface AsyncState<T> {
  data: T | null;
  loading: boolean;
  error: Error | null;
}

function useAsync<T>(
  asyncFunction: () => Promise<T>,
  immediate = true
): AsyncState<T> {
  const [state, setState] = useState<AsyncState<T>>({
    data: null,
    loading: false,
    error: null,
  });

  useEffect(() => {
    if (!immediate) return;
    
    asyncFunction()
      .then((data) => setState({ data, loading: false, error: null }))
      .catch((error) => setState({ data: null, loading: false, error }));
  }, []);

  return state;
}

// Usage
const { data: products, loading, error } = useAsync(() => fetchProducts());
```

### 3. Discriminated Unions (over Utility Types)

```tsx
// ✅ GOOD — Discriminated union for async states
type LoadingState = {
  status: 'loading';
};

type SuccessState<T> = {
  status: 'success';
  data: T;
};

type ErrorState = {
  status: 'error';
  error: Error;
};

type AsyncResult<T> = LoadingState | SuccessState<T> | ErrorState;

// Pattern matching with switch
function renderResult<T>(result: AsyncResult<T>) {
  switch (result.status) {
    case 'loading':
      return <Loader />;
    case 'success':
      return <Component data={result.data} />;
    case 'error':
      return <ErrorMessage error={result.error} />;
  }
}
```

### 4. Const Assertions for Literal Types

```tsx
// ✅ GOOD — Using const for literal types
const PRODUCT_SORT_OPTIONS = {
  price_asc: 'Price: Low to High',
  price_desc: 'Price: High to Low',
  newest: 'Newest',
} as const;

type SortOption = keyof typeof PRODUCT_SORT_OPTIONS;

// Now SortOption is 'price_asc' | 'price_desc' | 'newest' (strict)
```

---

## State Management: Redux Toolkit + RTK Query

### 1. RTK Query APIs (store/api/)

**One API file per feature**, with clear request/response types:

```tsx
// src/store/api/productApi.ts

import { createApi, fetchBaseQuery } from '@reduxjs/toolkit/query/react';
import type { Product, PaginatedResult } from '../../types/entities';

interface ProductQueryParams {
  page?: number;
  limit?: number;
  sortBy?: string;
  filterBy?: Record<string, string>;
}

export const productApi = createApi({
  reducerPath: 'productApi',
  baseQuery: fetchBaseQuery({
    baseUrl: import.meta.env.VITE_API_URL || 'http://localhost:5000/api',
    prepareHeaders: (headers, { getState }: any) => {
      const { auth } = getState() as RootState;
      if (auth.token) {
        headers.set('Authorization', `Bearer ${auth.token}`);
      }
      return headers;
    },
  }),
  tagTypes: ['Product'],
  endpoints: (builder) => ({
    getProducts: builder.query<PaginatedResult<Product>, ProductQueryParams>({
      query: (params) => ({
        url: '/products',
        params,
      }),
      transformResponse: (response: ApiResponse<PaginatedResult<Product>>) => response.data,
      providesTags: ['Product'],
    }),

    getProductById: builder.query<Product, string>({
      query: (id) => `/products/${id}`,
      transformResponse: (response: ApiResponse<Product>) => response.data,
      providesTags: (result, error, id) => [{ type: 'Product', id }],
    }),

    addProduct: builder.mutation<Product, Omit<Product, 'id'>>({
      query: (product) => ({
        url: '/products',
        method: 'POST',
        body: product,
      }),
      transformResponse: (response: ApiResponse<Product>) => response.data,
      invalidatesTags: ['Product'],
    }),
  }),
});
 (matching backend ApiResponse structure):**

| Pattern | Purpose | Backend Alignment |
|---------|---------|------------------|
| `transformResponse: (response: ApiResponse<T>) => response.data` | Extract `data` from backend's `ApiResponse<T>` wrapper | Matches backend's consistent response format |
| `providesTags: ['Product']` | Cache invalidation (READ operations) | Aligns with backend entity groupings |
| `invalidatesTags: ['Product']` | Invalidate cache on mutation (WRITE) | Triggers refresh when backend data changes |
| `tagTypes: ['Product', 'Order']` | Define cache tags at API level | Maps to backend entity types |
| `skip: !isAuthenticated` | Conditionally skip query | Respects backend auth requirements |
| `prepareHeaders: (headers) => headers.set('Authorization', 'Bearer ${token}')` | Add auth header | Required for backend JWT validation |

**Critical**: Every API file must handle the backend's `ApiResponse<T>` format:
```typescript
interface ApiResponse<T> {
  success: boolean;
  message: string;
  data?: T;           // ← Extract this in transformResponse
  errors?: string[];
}
```
| `invalidatesTags` | Invalidate cache on mutation (WRITE) |
| `tagTypes` | Define cache tags at API level |
| `skip` | Conditionally skip query (e.g., if not authenticated) |
| `{skip: !isAuthenticated}` | Skip if condition false |

### 2. Redux Slices (store/slices/)

Use Redux Toolkit's `createSlice` for simplified boilerplate:

```tsx
// src/store/slices/authSlice.ts

import { createSlice, PayloadAction } from '@reduxjs/toolkit';

interface AuthState {
  user: User | null;
  token: string | null;
  isAuthenticated: boolean;
  loading: boolean;
  error: string | null;
}

const initialState: AuthState = {
  user: JSON.parse(localStorage.getItem('user') || 'null'),
  token: localStorage.getItem('token'),
  isAuthenticated: !!localStorage.getItem('token'),
  loading: false,
  error: null,
};

const authSlice = createSlice({
  name: 'auth',
  initialState,
  reducers: {
    setUser: (state, action: PayloadAction<User>) => {
      state.user = action.payload;
      state.isAuthenticated = true;
      localStorage.setItem('user', JSON.stringify(action.payload));
    },
    setToken: (state, action: PayloadAction<string>) => {
      state.token = action.payload;
      localStorage.setItem('token', action.payload);
    },
    logout: (state) => {
      state.user = null;
      state.token = null;
      state.isAuthenticated = false;
      localStorage.removeItem('user');
      localStorage.removeItem('token');
    },
  },
  extraReducers: (builder) => {
    // Handle async thunks or other actions if needed
  },
});

export const { setUser, setToken, logout } = authSlice.actions;
export default authSlice.reducer;
```

### 3. Store Configuration (store/store.ts)

```tsx
import { configureStore } from '@reduxjs/toolkit';
import { productApi } from './api/productApi';
import { cartApi } from './api/cartApi';
import authReducer from './slices/authSlice';
import uiReducer from './slices/uiSlice';

export const store = configureStore({
  reducer: {
    auth: authReducer,
    ui: uiReducer,
    [productApi.reducerPath]: productApi.reducer,
    [cartApi.reducerPath]: cartApi.reducer,
  },
  middleware: (getDefaultMiddleware) =>
    getDefaultMiddleware().concat(
      productApi.middleware,
      cartApi.middleware
    ),
});

export type RootState = ReturnType<typeof store.getState>;
export type AppDispatch = typeof store.dispatch;
```

### 4. Typed Hooks (store/hooks.ts)

Always use typed hooks to access Redux state and dispatch:

```tsx
// src/store/hooks.ts

import { useDispatch, useSelector } from 'react-redux';
import ty — Matches your current implementation
const user = useAppSelector((state) => state.auth.user);
const dispatch = useAppDispatch();

// ❌ BAD — Untyped, loses IntelliSense
const user = useSelector((state: any) => state.auth.user);

// ✅ CURRENT PATTERN — Your existing typed hook implementation
export const useAppSelector = <T,>(selector: (state: RootState) => T) => 
  useSelector<RootState, T>(selector);
```

**Note**: This matches your existing implementation in `src/frontend/storefront/src/store/hooks.ts` and `src/frontend/admin/src/store/hooks.ts`.
**Always use these typed versions**, not the raw `useDispatch` / `useSelector`:

```tsx
// ✅ GOOD
const user = useAppSelector((state) => state.auth.user);
const dispatch = useAppDispatch();

---

## Real-World Examples (Based on Current Codebase)

### 1. Current API Pattern (cartApi.ts

---

## Component Examples

### 1. Simple Component (UI)

```tsx
// src/components/ui/Button.tsx

import style from './Button.module.css';

interface ButtonProps extends React.ButtonHTMLAttributes<HTMLButtonElement> {
  variant?: 'primary' | 'secondary' | 'danger';
  isLoading?: boolean;
  children: React.ReactNode;
}

export default function Button({
  variant = 'primary',
  isLoading = false,
  children,
  disabled,
  ...rest
}: ButtonProps) {
  return (
    <button
      className={`${style.button} ${style[variant]}`}
      disabled={disabled || isLoading}
      {...rest}
    >
      {isLoading ? <span className={style.spinner} /> : children}
    </button>
  );
}
```Store Configuration (Following Your Current Pattern)

### 2. Feature Component with Hooks & RTK Query

```tsx
// src/components/features/ProductList.tsx

import { useState } from 'react';
import { useGetProductsQuery } from '../../store/api/productApi';
import ProductCard from './ProductCard';
import type { ProductQueryParams } from '../../store/api/productApi';
import styles from './ProductList.module.css';

interface ProductListProps {
  initialFilters?: ProductQueryParams;
}

export default function ProductList({ initialFilters = {} }: ProductListProps) {
  const [filters, setFilters] = useState<ProductQueryParams>(initialFilters);

  const { data, isLoading, error, isFetching } = useGetProductsQuery(filters);

  if (isLoading) return <div>Loading products...</div>;
  if (error) return <div>Failed to load products</div>;

  return (
    <div className={styles.list}>
      {data?.items.map((product) => (
        <ProductCard key={product.id} product={product} />
      ))}
    </div>
  );
}
```

### 3. Component Using QueryRenderer Pattern

```tsx
// src/components/features/AddProductForm.tsx

import { useState, useCallback } from 'react';
import { useAddProductMutation } from '../../store/api/productApi';
import Button from '../ui/Button';
import type { Product } from '../../types/entities';
import styles from './AddProductForm.module.css';

interface FormData {
  name: string;
  price: number;
  imageUrl: string;
}

const validateForm = (data: Partial<FormData>): string[] => {
  const errors: string[] = [];
  if (!data.name || data.name.trim().length < 3) {
    errors.push('Name must be at least 3 characters');
  }
  if (!data.price || data.price <= 0) {
    errors.push('Price must be greater than 0');
  }
  if (!data.imageUrl) {
    errors.push('Image URL is required');
  }
  return errors;
};

export default function AddProductForm() {
  const [formData, setFormData] = useState<FormData>({
    name: '',
    price: 0,
    imageUrl: '',
  });
  const [validationErrors, setValidationErrors] = useState<string[]>([]);
  
  const [addProduct, { isLoading }] = useAddProductMutation();

  const handleChange = useCallback(
    (e: React.ChangeEvent<HTMLInputElement>) => {
      const { name, value } = e.target;
      setFormData((prev) => ({
        ...prev,
        [name]: name === 'price' ? parseFloat(value) : value,
      }));
    },
    []
  );

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    
    const errors = validateForm(formData);
    if (errors.length > 0) {
      setValidationErrors(errors);
      return;
    }

    try {
      await addProduct(formData).unwrap();
      // Success handling
      setFormData({ name: '', price: 0, imageUrl: '' });
      setValidationErrors([]);
    } catch (error) {
      setValidationErrors([error instanceof Error ? error.message : 'Failed to add product']);
    }
  };

  return (
    <form onSubmit={handleSubmit} className={styles.form}>
      {validationErrors.length > 0 && (
        <div className={styles.errors}>
          {validationErrors.map((error) => (
            <p key={error}>{error}</p>
          ))}
        </div>
      )}
      
      <input
        type="text"
        name="name"
        value={formData.name}
        onChange={handleChange}
        placeholder="Product name"
      />
      
      <input
        type="number"
        name="price"
        value={formData.price}
        onChange={handleChange}
        placeholder="Price"
        step="0.01"
      />
      
      <input
        type="url"
        name="imageUrl"
        value={formData.imageUrl}
        onChange={handleChange}
        placeholder="Image URL"
      />
      
      <Button variant="primary" type="submit" isLoading={isLoading}>
        Add Product
      </Button>
    </form>
  );
}
```

### 4. Current ProductCard Pattern (Your Implementation)

```tsx
// src/components/ProductCard.tsx — Your established pattern

import { Link } from 'react-router-dom';
import { useAppSelector } from '../store/hooks';
import { 
  useAddToWishlistMutation, 
  useRemoveFromWishlistMutation, 
  useCheckInWishlistQuery 
} from '../store/api/wishlistApi';
import Card from './ui/Card';

interface ProductCardProps {
  id: string;
  name: string;
  slug: string;
  price: number;
  compareAtPrice?: number;
  imageUrl: string;
  rating?: number;
  reviewCount?: number;
}

export default function ProductCard({
  id, name, slug, price, compareAtPrice, imageUrl, rating = 0, reviewCount = 0
}: ProductCardProps) {
  const DEFAULT_PRODUCT_IMAGE = 'https://placehold.co/400x400/f1f5f9/64748b?text=Product';
  
  // ✅ Your current auth pattern
  const { isAuthenticated } = useAppSelector((state) => state.auth);
  
  // ✅ Your conditional query pattern
  const { data: isInWishlist, refetch: refetchWishlist } = useCheckInWishlistQuery(id, {
    skip: !isAuthenticated,
  });
  
  const [addToWishlist] = useAddToWishlistMutation();
  const [removeFromWishlist] = useRemoveFromWishlistMutation();

  const handleWishlistClick = async (e: React.MouseEvent) => {
    e.preventDefault();
    e.stopPropagation();

    if (!isAuthenticated) {
      alert('Please sign in to add items to your wishlist');
      return;
    }

    try {
      if (isInWishlist) {
        await removeFromWishlist(id).unwrap();
      } else {
        await addToWishlist(id).unwrap();
      }
      // ✅ Your immediate refetch pattern
      await refetchWishlist();
    } catch {
      // Error handled by mutation state
    }
  };

  return (
    <Link to={`/products/${slug}`}>
      <Card variant="default" padding="sm" style={{ position: 'relative' }}>
        <div style={{ position: 'relative' }}>
          <img
            src={imageUrl || DEFAULT_PRODUCT_IMAGE}
            alt={name}
            onError={(e) => {
              e.currentTarget.src = DEFAULT_PRODUCT_IMAGE;
            }}
          />
          
          {/* ✅ Your conditional wishlist UI pattern */}
          {isAuthenticated && (
            <button
              onClick={handleWishlistClick}
              style={{
                position: 'absolute',
                top: '0.5rem',
                right: '0.5rem',
                background: 'white',
                border: 'none',
                borderRadius: '50%',
                width: '2rem',
                height: '2rem',
                display: 'flex',
                alignItems: 'center',
                justifyContent: 'center',
                cursor: 'pointer',
                fontSize: '1.25rem',
                boxShadow: '0 2px 4px rgba(0,0,0,0.1)',
              }}
              title={isInWishlist ? 'Remove from wishlist' : 'Add to wishlist'}
            >
              {isInWishlist ? '♥' : '♡'}
            </button>
          )}
        </div>
        
        <div>
          <h3>{name}</h3>
          <div>
            {compareAtPrice && (
              <span style={{ textDecoration: 'line-through', color: '#6b7280' }}>
                ${compareAtPrice.toFixed(2)}
              </span>
            )}
            <span style={{ fontWeight: 'bold' }}>${price.toFixed(2)}</span>
          </div>
          {rating > 0 && (
            <div>
              {'★'.repeat(rating)}{'☆'.repeat(5 - rating)} ({reviewCount} reviews)
            </div>
          )}
        </div>
      </Card>
    </Link>
  );
}
```

**Key patterns from your implementation**:
- **Conditional auth UI**: Only show wishlist button if authenticated
- **Skip queries**: Use `skip: !isAuthenticated` to avoid unnecessary API calls
- **Immediate refetch**: Call `refetchWishlist()` after mutations for instant UI updates
- **Default images**: Graceful fallback with placeholder service
- **Event handling**: Prevent navigation when clicking interactive elements

---

## Custom Hooks Pattern

Create reusable logic in custom hooks (placed in `src/hooks/`):

```tsx
// src/hooks/usePaginationParams.ts

import { useState, useCallback } from 'react';

interface PaginationParams {
  page: number;
  limit: number;
}

export function usePaginationParams(initialLimit = 20) {
  const [params, setParams] = useState<PaginationParams>({
    page: 1,
    limit: initialLimit,
  });

  const goToPage = useCallback((page: number) => {
    setParams((prev) => ({ ...prev, page }));
  }, []);

  const setLimit = useCallback((limit: number) => {
    setParams({ page: 1, limit });
  }, []);

  const reset = useCallback(() => {
    setParams({ page: 1, limit: initialLimit });
  }, [initialLimit]);

  return { params, goToPage, setLimit, reset };
}
```

---

## Styling Conventions (CSS Modules)

Use **CSS Modules for scoped styling** — never global classes.

```css
/* src/components/Button.module.css */

.button {
  padding: 0.5rem 1rem;
  border: none;
  border-radius: 4px;
  font-size: 1rem;
  cursor: pointer;
  transition: all 0.2s ease;
}

.button:hover {
  transform: translateY(-2px);
  box-shadow: 0 4px 8px rgba(0, 0, 0, 0.1);
}

.primary {
  background-color: #007bff;
  color: white;
}

.primary:hover {
  background-color: #0056b3;
}

.secondary {
  background-color: #6c757d;
  color: white;
}

.secondary:hover {
  background-color: #545b62;
}

.danger {
  background-color: #dc3545;
  color: white;
}

.danger:hover {
  background-color: #c82333;
}

.spinner {
  display: inline-block;
  animation: spin 1s linear infinite;
}

@keyframes spin {
  from {
    transform: rotate(0deg);
  }
  to {
    transform: rotate(360deg);
  }
}
```

**Usage:**

```tsx
import styles from './Button.module.css';

<button className={`${styles.button} ${styles.primary}`}>
  Click me
</button>
```

---

## Error Handling

### 1. Error Boundary Component

```tsx
// src/components/ErrorBoundary.tsx

import React from 'react';

interface Props {
  children: React.ReactNode;
  fallback?: React.ReactNode;
}

interface State {
  hasError: boolean;
  error: Error | null;
}

export class ErrorBoundary extends React.Component<Props, State> {
  constructor(props: Props) {
    super(props);
    this.state = { hasError: false, error: null };
  }

  static getDerivedStateFromError(error: Error) {
    return { hasError: true, error };
  }

  componentDidCatch(error: Error, errorInfo: React.ErrorInfo) {
    console.error('Error caught by boundary:', error, errorInfo);
    // Log to error tracking service (e.g., Sentry)
  }

  render() {
    if (this.state.hasError) {
      return this.props.fallback || <div>Something went wrong</div>;
    }

    return this.props.children;
  }
}
```

### 2. API Error Handling

```tsx
// src/utils/apiError.ts

interface ApiErrorResponse {
  success: false;
  message: string;
  errors?: Record<string, string[]>;
}

export class ApiError extends Error {
  constructor(
    public status: number,
    message: string,
    public errors?: Record<string, string[]>
  ) {
    super(message);
    this.name = 'ApiError';
  }
}

export function handleApiError(error: unknown): ApiError {
  if (error instanceof ApiError) {
    return error;
  }

  if (error instanceof TypeError) {
    return new ApiError(0, 'Network request failed');
  }

  return new ApiError(500, 'An unexpected error occurred');
}
```

---

## Performance Optimization

### 1. Code Splitting (Lazy Routes)

```tsx
// src/App.tsx

import { lazy, Suspense } from 'react';
import { Routes, Route } from 'react-router-dom';

const HomePage = lazy(() => import('./pages/HomePage'));
const ProductsPage = lazy(() => import('./pages/ProductsPage'));
const CartPage = lazy(() => import('./pages/CartPage'));

export default function App() {
  return (
    <Routes>
      <Route
        path="/"
        element={
          <Suspense fallback={<div>Loading...</div>}>
            <HomePage />
          </Suspense>
        }
      />
      {/* Other routes */}
    </Routes>
  );
}
```

### 2. Memoization (when necessary)

Use `React.memo` **only** for components that receive expensive props or heavy re-renders:

```tsx
interface ProductCardProps {
  product: Product;
  onSelect: (id: string) => void;
}

// Only memoize if the component is expensive or receives callbacks
export default React.memo(function ProductCard({
  product,
  onSelect,
}: ProductCardProps) {
  return (
    <div onClick={() => onSelect(product.id)}>
      {product.name}
    </div>
  );
});
```

### 3. Image Optimization

```tsx
// Always include alt text, use responsive images
<img
  src={imageUrl}
  alt={productName}
  loading="lazy"
  width={400}
  height={400}
  onError={(e) => {
    e.currentTarget.src = DEFAULT_PLACEHOLDER;
  }}
/>
```

---

## Testing Conventions

### 1. Component Tests (Vitest)

```tsx
// src/components/Button.test.tsx

import { describe, it, expect } from 'vitest';
import { render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import Button from './Button';

describe('Button Component', () => {
  it('renders with text', () => {
---

## Frontend-Backend Integration Patterns

### 1. API Response Handling (Match Backend ApiResponse<T>)

```typescript
// ✅ Backend returns this structure (from BACKEND_CODING_GUIDE.md)
interface ApiResponse<T> {
  success: boolean;
  message: string;
  data?: T;
  errors?: string[];
}

// ✅ Frontend handles it consistently
export const productApi = createApi({
  // ...
  endpoints: (builder) => ({
    getProducts: builder.query<PaginatedResult<Product>, ProductQueryParams>({
      query: (params) => ({ url: '/products', params }),
      transformResponse: (response: ApiResponse<PaginatedResult<Product>>) => {
        // Always extract data, provide fallback
        return response.data || { items: [], totalCount: 0, page: 1, pageSize: 20 };
      },
    }),
  }),
});
```

### 2. Error Handling (Mirror Backend Exceptions)

```typescript
// Backend throws typed exceptions → Frontend catches structured errors
const [createProduct, { isLoading }] = useCreateProductMutation();

try {
  await createProduct(productData).unwrap();
} catch (error: any) {
  // Backend's GlobalExceptionMiddleware formats errors consistently
  if (error.status === 404) {
    // NotFoundExceptionHandler
    showNotification('Product not found', 'error');
  } else if (error.status === 400) {
    // ValidationExceptionHandler
    const validationErrors = error.data?.errors || {};
    setFormErrors(validationErrors);
  } else if (error.status === 401) {
    // UnauthorizedExceptionHandler
    dispatch(logout());
    navigate('/login');
  }
}
```

### 3. DTO Mapping (Frontend Types Mirror Backend DTOs)

```typescript
// Backend DTO (from BACKEND_CODING_GUIDE.md):
// public class ProductDto { Id, Name, Price, ImageUrl, ... }

// Frontend interface (mirrors exactly):
interface Product {
  id: string;        // Backend: Guid Id
  name: string;      // Backend: string Name
  price: number;     // Backend: decimal Price
  imageUrl: string;  // Backend: string? ImageUrl
}

// ✅ No mapping needed — direct usage
const { data: products } = useGetProductsQuery();
products?.items.map(product => product.name); // Direct access
```

---

## Common Mistakes to Avoid

| Mistake | ❌ Don't | ✅ Do Instead | Backend Alignment |
|---------|---------|---------------|------------------|
| Using `any` type | `function foo(x: any)` | Define proper types | Match backend DTO types |
| Global CSS classes | `.productCard { }` | CSS Modules: `styles.productCard` | N/A |
| Monolithic page components | 400+ line page files | Extract to `pages/components/{PageName}/` | Like backend extracts to helper methods |
| Lifting state too high | All state at root | Keep state close to usage | Like backend services |
| Not memoizing callbacks | Every function is new | Use `useCallback` for handler props | N/A |
| Ignoring TypeScript errors | `// @ts-ignore` | Fix or use proper types | Backend uses strict C# |
| Hardcoding URLs | `'http://localhost:5000/api'` | Use `VITE_API_URL` env var | Match backend `appsettings.json` |
| Manually creating dates/times | `new Date()` | Use date library (date-fns, Day.js) | Backend uses ISO strings |
| Forgetting alt text on images | `<img src="..." />` | `<img src="..." alt="description" />` | N/A |
| Not handling loading/error states | Show only success | Show loading spinners, error messages | Backend provides structured errors |
| Conditional hook calls | Hooks inside if/loops | Hooks always top-level | N/A |
| Inline complex logic in JSX | Long ternaries (50+ lines) | Extract to components/hooks | Like backend helper methods |
| Mutating Redux state | `state.user.name = 'John'` | Use RTK's Immer: `state.user.name = 'John'` | Immutable like backend entities |
| Not using RTK Query | Manual fetch + useEffect | Use `useGetProductsQuery()` | Matches backend Repository pattern |
| Wrong token storage key | `localStorage.getItem('token')` | `localStorage.getItem('authToken')` | Match your current pattern |
| Inconsistent API base URLs | Different URLs per API file | Single `VITE_API_URL` env var | Match backend base path |
| Not extracting data from ApiResponse | `return response` | `return response.data` | Backend wraps all responses |
### 2. Hook Tests

```tsx
// src/hooks/usePaginationParams.test.ts

import { describe, it, expect } from 'vitest';
import { renderHook, act } from '@testing-library/react';
import { usePaginationParams } from './usePaginationParams';

describe('usePaginationParams', () => {
  it('initializes with correct defaults', () => {
    const { result } = renderHook(() => usePaginationParams(20));
    
    expect(result.current.params.page).toBe(1);
    expect(result.current.params.limit).toBe(20);
  });

  it('updates page on goToPage', () => {
    const { result } = renderHook(() => usePaginationParams());
    
    act(() => {
      result.current.goToPage(3);
    });
    
    expect(result.current.params.page).toBe(3);
  });
});
```

---

## Accessibility (A11y)

### 1. Semantic HTML

```tsx
// ✅ GOOD
<header>
  <nav>
    <a href="/home">Home</a>
  </nav>
</header>
<main>
  <article>
    <h1>Product Details</h1>
  </article>
</main>

// ❌ BAD
<div className="header">
  <div className="nav">
    <div onClick={() => navigate('/home')}>Home</div>
  </div>
</div>
```

### 2. ARIA Attributes

```tsx
<button
  aria-label="Close menu"
  onClick={handleClose}
>
  ✕
</button>

<div role="alert" aria-live="polite">
  {errorMessage}
</div>
```

### 3. Focus Management

```tsx
const inputRef = useRef<HTMLInputElement>(null);

const focusInput = useCallback(() => {
  inputRef.current?.focus();
}, []);

return (
  <>
    <input ref={inputRef} />
    <button onClick={focusInput}>Focus Input</button>
  </>
);
```

---

## Environment Variables

Create `.env` and `.env.example` (commit example, ignore actual):

```bash
# .env.example
VITE_API_URL=http://localhost:5000/api
VITE_APP_NAME=E-Commerce Storefront
VITE_AUTH_TOKEN_KEY=auth_token

# .env (local — .gitignore'd)
VITE_API_URL=http://localhost:5000/api
```

**Access in code:**

```tsx
const apiUrl = import.meta.env.VITE_API_URL || 'http://localhost:5000/api';
```

---

## Common Mistakes to Avoid

| Mistake | ❌ Don't | ✅ Do Instead |
|---------|---------|---------------|
| Using `any` type | `function foo(x: any)` | Define proper types |
| Global CSS classes | `.productCard { }` | CSS Modules: `styles.productCard` |
| Lifting state too high | All state at root | Keep state close to usage, lift only when needed |
| Not memoizing callbacks | Every function is new | Use `useCallback` for handler props |
| Ignoring TypeScript errors | `// @ts-ignore` | Fix or use proper types |
| Hardcoding URLs | `'http://localhost:5000/api'` | Use `VITE_API_URL` env var |
| Manually creating dates/times | `new Date()` | Use date library (date-fns, Day.js) |
| Forgetting alt text on images | `<img src="..." />` | `<img src="..." alt="description" />` |
| Not handling loading/error states | Show only success | Show loading spinners, error messages |
| Block-scoped namespaces (C# carry-over) | ❌ This is JS | Just export directly |
| Conditional hook calls | Hooks inside if/loops | Hooks always top-level |
| Inline complex logic in JSX | Long ternaries | Extract to functions/hooks |
| Mutating Redux state | `state.user.name = 'John'` | `{ ...state, user: { ...state.user, name: 'John' } }` |
| Not using RTK Query | Manual fetch + useEffect | Use `useGetProductsQuery()` |
| Separate API layer in Redux | Old approach | RTK Query handles it all |

---

## ESLint Configuration

Example `.eslintrc.js` for strict quality:

```javascript
import eslint from '@eslint/js';
import tseslint from 'typescript-eslint';
import reactHooks from 'eslint-plugin-react-hooks';
import reactRefresh from 'eslint-plugin-react-refresh';
### Frontend-Specific Checks
- [ ] **TypeScript**: No `any` types, strict mode enabled, matches backend DTO shapes
- [ ] **React**: No class components, hooks at top level, functional patterns only
- [ ] **Redux**: Using typed `useAppSelector`/`useAppDispatch` (your custom hooks)
- [ ] **RTK Query**: Separate API files, `transformResponse` extracts `data` from `ApiResponse<T>`
- [ ] **Props**: Interface defined, prop types strict, optional props have defaults
- [ ] **Naming**: Files in correct case (PascalCase components, camelCase hooks/utils)
- [ ] **Styling**: CSS Modules used, scoped classes, no global styles
- [ ] **Testing**: Components tested, RTK Query mocked, edge cases covered
- [ ] **Accessibility**: Semantic HTML, alt text, ARIA labels, keyboard navigation
- [ ] **Performance**: Code-split routes, `useCallback` for handlers, `useMemo` sparingly
- [ ] **Page Organization**: Pages under 100 lines, complex features extracted to `pages/components/{PageName}/`

### Frontend-Backend Integration Checks  
- [ ] **API Response**: Always handle `ApiResponse<T>` format, extract `data` field
- [ ] **Error Handling**: Match backend error status codes (400/401/404/500)
- [ ] **Authentication**: Use `authToken` key, handle JWT expiration, logout on 401
- [ ] **Environment**: Use `VITE_API_URL` consistently, no hardcoded backend URLs
- [ ] **DTOs**: Frontend types mirror backend DTOs exactly, no manual mapping
- [ ] **Tags**: RTK Query tags align with backend entities for cache invalidation
- [ ] **Headers**: Authorization header format matches backend JWT validation
### Frontend Core Principles
1. **Type Safety**: Embrace TypeScript strictly — no `any`, mirror backend DTO types exactly
2. **Component Isolation**: Small, single-responsibility components using composition patterns
3. **Page Organization**: Pages are thin orchestration layers — extract complexity to `pages/components/{PageName}/`
4. **State Management**: RTK Query for server state, Redux slices for client-only state
5. **Performance**: Code-split routes, lazy-load images, memoize callbacks appropriately
6. **Quality**: Strict linting, comprehensive testing, accessibility-first development
7. **Consistency**: Follow established patterns in your codebase religiously
8. **DRY**: Extract reusable logic to hooks and utilities, avoid duplicate API patterns
9. **Error Handling**: Show loading/error states consistently, graceful degradation
10. **Accessibility**: Semantic HTML, ARIA labels, keyboard navigation support
11. **Documentation**: Comments for "why", not "what" — code should be self-documenting

### Frontend-Backend Alignment Principles  
12. **API Consistency**: Always handle backend's `ApiResponse<T>` format consistently
13. **Error Mapping**: Map backend exception types to appropriate frontend error handling
14. **DTO Mirroring**: Frontend interfaces exactly match backend DTO structure
15. **Authentication Flow**: Follow backend JWT patterns, handle token lifecycle correctly
16. **Cache Invalidation**: RTK Query tags align with backend entity boundaries
17. **Environment Parity**: Use same base URLs and configuration patterns as backend

### Architecture Alignment
18. **Component → Service**: Frontend components mirror backend's Controller → Service pattern (thin orchestration)
19. **Extract Early**: Extract complex JSX (30+ lines) just like backend extracts helper methods
20. **Single Responsibility**: One component = one feature, matching backend's single-responsibility principle

---

## ESLint Configuration
      ],
      '@typescript-eslint/no-explicit-any': 'error',
    },
  },
  eslint.configs.recommended,
  ...tseslint.configs.recommended,
];
```

---

## Vite Configuration

Example `vite.config.ts`:

```typescript
import { defineConfig } from 'vite';
import react from '@vitejs/plugin-react';

export default defineConfig({
  plugins: [react()],
  server: {
    port: 5173,
    strictPort: true,
    proxy: {
      '/api': {
        target: 'http://localhost:5000',
        changeOrigin: true,
      },
    },
  },
  build: {
    outDir: 'dist',
    sourcemap: false,
    rollupOptions: {
      output: {
        manualChunks: {
          'react-vendors': ['react', 'react-dom', 'react-router-dom'],
          'redux-vendors': ['@reduxjs/toolkit', 'react-redux'],
        },
      },
    },
  },
});
```

---

## Code Review Checklist

Before submitting a pull request, ensure:

- [ ] **TypeScript**: No `any` types, strict mode enabled
- [ ] **React**: No class components, hooks at top level
- [ ] **Redux**: Using typed `useAppSelector`/`useAppDispatch`
- [ ] **RTK Query**: Separate API files, transformed responses
- [ ] **Props**: Interface defined, prop types strict
- [ ] **Naming**: Files in correct case (PascalCase components, camelCase hooks/utils)
- [ ] **Styling**: CSS Modules used, scoped classes
- [ ] **Testing**: Components tested, edge cases covered
- [ ] **Accessibility**: Semantic HTML, alt text, ARIA labels where needed
- [ ] **Performance**: Code-split large routes, memoized expensive components
- [ ] **Env Vars**: No hardcoded URLs or secrets
---

## Project-Specific Conventions (Your Established Patterns)

### 1. Auth Token Handling
```typescript
// ✅ Your current pattern (not 'token')
localStorage.getItem('authToken');
localStorage.setItem('authToken', token);
```

### 2. API Base URL Pattern  
```typescript
// ✅ Your current environment variable
const API_URL = import.meta.env.VITE_API_URL || 'http://localhost:5000/api';
```

### 3. Component Patterns You're Using
- **QueryRenderer**: Centralized loading/error state handling
- **Card component**: Consistent card UI with variant props
- **Conditional wishlist UI**: Only show for authenticated users
- **Skip queries**: `{skip: !isAuthenticated}` pattern
- **Page organization**: Extract complex sections to `pages/components/{PageName}/` (see Page Organization Formula above)

### 4. Store Organization (Your Current Structure)
- **Storefront APIs**: `productApi`, `cartApi`, `wishlistApi`, `profileApi`, `authApi`, `ordersApi`, `categoriesApi`, `reviewsApi`
- **Admin APIs**: `productsApi`, `ordersApi`, `dashboardApi`, `customersApi`, `reviewsApi`, `promoCodesApi`, `inventoryApi`, `authApi`

---

## Compatibility with Backend Guide

This document complements [BACKEND_CODING_GUIDE.md](BACKEND_CODING_GUIDE.md):

| Aspect | Frontend (This Doc) | Backend (BACKEND_CODING_GUIDE.md) |
|--------|-------------------|-----------------------------------|
| **Architecture** | Component → Hook → RTK Query → API | Controller → Service → Repository → Entity |
| **State** | Redux slices + RTK Query | Entity Framework + Unit of Work |
| **Validation** | Client-side TypeScript + forms | FluentValidation + DTOs |
| **Error Handling** | RTK Query error states | GlobalExceptionMiddleware |
| **Types** | TypeScript interfaces | C# DTOs + Entities |
| **Auth** | JWT in localStorage + Redux | JWT validation + ICurrentUserService |
| **Testing** | Vitest + React Testing Library | MSTest + Unit/Integration tests |

**Key Integration Points**:
- Frontend types mirror backend DTOs exactly — no mapping layer needed
- RTK Query `transformResponse` always extracts `data` from `ApiResponse<T>`
- Frontend error handling maps to backend exception hierarchy
- Authentication flows match between JWT generation (backend) and consumption (frontend)

---

## References

### Frontend-Specific
- [React 19 Docs](https://react.dev)
- [TypeScript Handbook](https://www.typescriptlang.org/docs/)
- [Redux Toolkit Docs](https://redux-toolkit.js.org/)
- [RTK Query Docs](https://redux-toolkit.js.org/rtk-query/overview)
- [Vite Docs](https://vitejs.dev/)
- [React Router Docs](https://reactrouter.com/)
- [Web Accessibility Guidelines (WCAG)](https://www.w3.org/WAI/WCAG21/quickref/)

### Project-Specific
- [BACKEND_CODING_GUIDE.md](BACKEND_CODING_GUIDE.md) — Backend patterns and API structure this frontend consumes
- [ARCHITECTURE_PLAN.md](ARCHITECTURE_PLAN.md) — Overall system architecture and technology decisions  
- [src/frontend/AUTHENTICATION_GUIDE.md](src/frontend/AUTHENTICATION_GUIDE.md) — Detailed auth flow documentation

---

**Last Updated**: February 5, 2026  
**Version**: 2.1  
**Companion Document**: [BACKEND_CODING_GUIDE.md](BACKEND_CODING_GUIDE.md)
