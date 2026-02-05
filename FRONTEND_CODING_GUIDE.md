# Frontend Coding Guide — E-Commerce Platform

Comprehensive coding standards for the React 19 + TypeScript frontend, using Vite, Redux Toolkit, and RTK Query.

---

## Tech Stack

- **React**: 19.2+ (latest features, RSC-inspired patterns)
- **TypeScript**: 5.9+ (strict mode enforced)
- **Build Tool**: Vite 7+
- **State Management**: Redux Toolkit 2.11+ + RTK Query
- **Routing**: React Router 7+
- **HTTP Client**: Axios 1.13+ (via RTK Query)
- **Styling**: CSS Modules (scoped styles)
- **Linting**: ESLint 9+ with TypeScript support
- **Testing**: Vitest (recommended) or Jest

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
│   ├── CartPage.tsx
│   └── NotFound.tsx
├── store/
│   ├── api/                 # RTK Query APIs (one per feature)
│   │   ├── productApi.ts
│   │   ├── cartApi.ts
│   │   ├── authApi.ts
│   │   └── index.ts
│   ├── slices/              # Redux slices (auth, ui, cart)
│   │   ├── authSlice.ts
│   │   ├── uiSlice.ts
│   │   └── index.ts
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

### 1. File Naming

| Type | Pattern | Example |
|------|---------|---------|
| Components | `PascalCase.tsx` | `ProductCard.tsx` |
| CSS Modules | `ComponentName.module.css` | `ProductCard.module.css` |
| Hooks | `camelCase.ts` (starts with `use`) | `useProductFilter.ts` |
| Utils/helpers | `camelCase.ts` | `formatPrice.ts` |
| Slices | `featureSlice.ts` | `authSlice.ts` |
| API files | `featureApi.ts` | `productApi.ts` |
| Types | `camelCase.ts` | `api.ts`, `entities.ts` |
| Constants | `CONSTANT_CASE` inside files | `DEFAULT_PAGE_SIZE = 20` |

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

export const { useGetProductsQuery, useGetProductByIdQuery, useAddProductMutation } = productApi;
```

**Key RTK Query patterns:**

| Pattern | Purpose |
|---------|---------|
| `transformResponse` | Extract `data` from `ApiResponse<T>` wrapper |
| `providesTags` | Cache invalidation (READ) |
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
import type { RootState, AppDispatch } from './store';

export const useAppDispatch = () => useDispatch<AppDispatch>();
export const useAppSelector = <T,>(selector: (state: RootState) => T): T =>
  useSelector<RootState, T>(selector);
```

**Always use these typed versions**, not the raw `useDispatch` / `useSelector`:

```tsx
// ✅ GOOD
const user = useAppSelector((state) => state.auth.user);
const dispatch = useAppDispatch();

// ❌ BAD
const user = useSelector((state: any) => state.auth.user);
```

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
```

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

### 3. Form Component with Validation

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
    render(<Button>Click me</Button>);
    expect(screen.getByText('Click me')).toBeInTheDocument();
  });

  it('calls onClick handler when clicked', async () => {
    const handleClick = vi.fn();
    render(<Button onClick={handleClick}>Click</Button>);
    
    await userEvent.click(screen.getByRole('button'));
    expect(handleClick).toHaveBeenCalledOnce();
  });

  it('shows loading state', () => {
    render(<Button isLoading>Loading</Button>);
    expect(screen.queryByText('Loading')).toBeDisabled();
  });
});
```

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

export default [
  {
    ignores: ['dist', 'node_modules'],
  },
  {
    files: ['**/*.{ts,tsx}'],
    languageOptions: {
      ecmaVersion: 'latest',
      sourceType: 'module',
    },
    plugins: {
      'react-hooks': reactHooks,
      'react-refresh': reactRefresh,
    },
    rules: {
      ...reactHooks.configs.recommended.rules,
      'react-refresh/only-export-components': [
        'warn',
        { allowConstantExport: true },
      ],
      'no-unused-vars': 'off',
      '@typescript-eslint/no-unused-vars': [
        'error',
        { argsIgnorePattern: '^_' },
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
- [ ] **Error Handling**: Loading/error states shown, errors logged
- [ ] **DRY**: No duplicated logic, extracted to hooks/utils
- [ ] **Imports**: Organized (React, libraries, internal, types, styles)
- [ ] **Linting**: No ESLint warnings, consistent formatting

---

## Summary of Key Principles

1. **Type Safety**: Embrace TypeScript strictly — no `any`
2. **Component Isolation**: Small, single-responsibility components
3. **State Management**: RTK Query for async data, Redux slices for global UI state
4. **Performance**: Code-split routes, lazy-load images, memoize sparingly
5. **Quality**: Linting, testing, accessibility from the start
6. **Consistency**: Follow naming and structure conventions religiously
7. **DRY**: Extract reusable logic to hooks and utilities
8. **Error Handling**: Show loading/error states, don't swallow errors
9. **Accessibility**: Semantic HTML, ARIA labels, keyboard navigation
10. **Documentation**: Comments for "why", not "what" (code should be self-documenting)

---

## References

- [React 19 Docs](https://react.dev)
- [TypeScript Handbook](https://www.typescriptlang.org/docs/)
- [Redux Toolkit Docs](https://redux-toolkit.js.org/)
- [RTK Query Docs](https://redux-toolkit.js.org/rtk-query/overview)
- [Vite Docs](https://vitejs.dev/)
- [React Router Docs](https://reactrouter.com/)
- [Web Accessibility Guidelines (WCAG)](https://www.w3.org/WAI/WCAG21/quickref/)

---

**Last Updated**: February 5, 2026  
**Version**: 1.0
