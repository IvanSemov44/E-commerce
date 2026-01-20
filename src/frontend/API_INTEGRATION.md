# Frontend API Integration Guide

This document describes the RTK Query API setup and usage patterns for both storefront and admin applications.

## Overview

Both applications use **RTK Query** for data fetching, caching, and state management. RTK Query handles:
- Automatic caching
- Request deduplication
- Synchronization across components
- Error handling
- Loading states
- Automatic refetching

## Environment Setup

### 1. Create `.env` files in both applications

**For storefront (`src/frontend/storefront/.env`):**
```env
VITE_API_URL=http://localhost:5000/api/v1
```

**For admin (`src/frontend/admin/.env`):**
```env
VITE_API_URL=http://localhost:5000/api/v1
```

The backend API should be running at `http://localhost:5000` with all endpoints under `/api/v1`.

## API Structure

### Shared Types

All types are defined in `src/frontend/shared/types.ts`:

```typescript
// Core API Response Wrapper
export interface ApiResponse<T> {
  success: boolean;
  message: string;
  data?: T;
  errors?: string[];
}

// Pagination
export interface PaginatedResult<T> {
  items: T[];
  totalCount: number;
  page: number;
  pageSize: number;
}
```

### Base Query Configuration

All API slices use a consistent base query with:
- **Base URL**: From `VITE_API_URL` environment variable
- **Authentication**: Bearer token from localStorage
- **Headers**: Automatically set Content-Type and Authorization

```typescript
// src/frontend/shared/api/baseQuery.ts
const baseQuery = fetchBaseQuery({
  baseUrl: API_URL,
  prepareHeaders: (headers) => {
    const token = localStorage.getItem('authToken');
    if (token) {
      headers.set('Authorization', `Bearer ${token}`);
    }
    return headers;
  },
});
```

## API Slices

### Authentication API

**Location**:
- Storefront: `src/frontend/storefront/src/store/api/authApi.ts`
- Admin: `src/frontend/admin/src/store/api/authApi.ts`

**Endpoints**:
- `POST /auth/login` - Login with email/password
- `POST /auth/register` - Register new user
- `GET /auth/me` - Get current user
- `POST /auth/logout` - Logout

**Usage Example**:
```typescript
import { useLoginMutation, useGetCurrentUserQuery } from '@/store/api/authApi';

function LoginComponent() {
  const [login, { isLoading }] = useLoginMutation();
  const { data: user, isLoading: userLoading } = useGetCurrentUserQuery();

  const handleLogin = async (email, password) => {
    try {
      const result = await login({ email, password }).unwrap();
      localStorage.setItem('authToken', result.token);
      // Redirect to dashboard
    } catch (error) {
      console.error('Login failed:', error);
    }
  };

  return <div>...</div>;
}
```

### Products API

**Location**:
- Storefront: `src/frontend/storefront/src/store/api/productApi.ts`
- Admin: `src/frontend/admin/src/store/api/productsApi.ts`

**Storefront Endpoints**:
- `GET /products` - List products with pagination
- `GET /products/slug/{slug}` - Get product by slug
- `GET /products/{id}` - Get product by ID
- `GET /products/featured?count=10` - Get featured products

**Admin Endpoints**:
- `GET /admin/products` - List all products
- `GET /admin/products/{id}` - Get product details
- `POST /admin/products` - Create product
- `PUT /admin/products/{id}` - Update product
- `DELETE /admin/products/{id}` - Delete product
- `PUT /admin/products/{productId}/stock` - Update stock

**Usage Example**:
```typescript
// Storefront - Get featured products
import { useGetFeaturedProductsQuery } from '@/store/api/productApi';

function FeaturedProducts() {
  const { data: products, isLoading, error } = useGetFeaturedProductsQuery(10);

  if (isLoading) return <div>Loading...</div>;
  if (error) return <div>Error loading products</div>;

  return (
    <div>
      {products?.map(product => (
        <ProductCard key={product.id} product={product} />
      ))}
    </div>
  );
}

// Admin - Get products with search
import { useGetProductsQuery } from '@/store/api/productsApi';

function ProductsPage() {
  const [page, setPage] = useState(1);
  const [search, setSearch] = useState('');

  const { data: result, isLoading } = useGetProductsQuery({
    page,
    pageSize: 20,
    search,
  });

  return (
    <div>
      {/* Render paginated products */}
    </div>
  );
}
```

### Orders API

**Location**:
- Storefront: `src/frontend/storefront/src/store/api/ordersApi.ts`
- Admin: `src/frontend/admin/src/store/api/ordersApi.ts`

**Storefront Endpoints**:
- `POST /checkout` - Create order
- `GET /orders` - Get user's orders
- `GET /orders/{id}` - Get order details

**Admin Endpoints**:
- `GET /admin/orders` - List all orders with filters
- `GET /admin/orders/{id}` - Get order details
- `PUT /admin/orders/{orderId}/status` - Update order status
- `GET /admin/orders/stats` - Get order statistics

**Usage Example**:
```typescript
// Storefront - Create order
import { useCreateOrderMutation } from '@/store/api/ordersApi';

function CheckoutComponent() {
  const [createOrder, { isLoading }] = useCreateOrderMutation();

  const handleCheckout = async (orderData) => {
    try {
      const result = await createOrder(orderData).unwrap();
      // Handle payment or redirect
    } catch (error) {
      console.error('Checkout failed:', error);
    }
  };

  return <div>...</div>;
}

// Admin - Get orders with status filter
import { useGetOrdersQuery, useUpdateOrderStatusMutation } from '@/store/api/ordersApi';

function OrdersPage() {
  const [status, setStatus] = useState('pending');
  const { data: result } = useGetOrdersQuery({ status });
  const [updateStatus] = useUpdateOrderStatusMutation();

  return <div>...</div>;
}
```

### Customers API (Admin Only)

**Location**: `src/frontend/admin/src/store/api/customersApi.ts`

**Endpoints**:
- `GET /admin/customers` - List customers
- `GET /admin/customers/{id}` - Get customer details
- `GET /admin/customers/stats` - Get customer statistics

### Dashboard API (Admin Only)

**Location**: `src/frontend/admin/src/store/api/dashboardApi.ts`

**Endpoints**:
- `GET /admin/dashboard/stats` - Get dashboard statistics (polls every 30s)

## Common Patterns

### Handling Loading and Error States

```typescript
const { data, isLoading, error } = useGetProductsQuery({ page: 1 });

if (isLoading) return <LoadingSkeleton />;
if (error) return <ErrorAlert message="Failed to load products" />;

return (
  <div>
    {data?.items.map(item => (
      <ProductCard key={item.id} product={item} />
    ))}
  </div>
);
```

### Using Mutations

```typescript
const [createProduct, { isLoading, error }] = useCreateProductMutation();

const handleCreate = async (formData) => {
  try {
    const result = await createProduct(formData).unwrap();
    // Success - redirect or show success message
  } catch (error) {
    // Error is automatically handled
    // Show error message
  }
};
```

### Polling Data

The Dashboard API automatically polls every 30 seconds:

```typescript
// Dashboard stats will automatically refresh every 30 seconds
const { data: stats } = useGetDashboardStatsQuery();
```

### Manual Cache Invalidation

```typescript
// After creating/updating, invalidate cache to refetch
const { data } = useGetProductsQuery(
  { page: 1 },
  {
    // Refetch when component mounts
    refetchOnMountOrArgChange: true
  }
);
```

## Authentication Flow

1. **Login**:
   ```typescript
   const { token } = await authApi.login({ email, password });
   localStorage.setItem('authToken', token);
   ```

2. **Auto-Include in Requests**:
   - All API slices automatically include the token in headers
   - No need to manually pass it

3. **Handle 401 Errors**:
   - Currently logs a warning
   - TODO: Implement token refresh or logout redirect

4. **Logout**:
   ```typescript
   await authApi.logout();
   localStorage.removeItem('authToken');
   // Redirect to login
   ```

## Adding New API Endpoints

### Step 1: Define Types

Add types to `src/frontend/shared/types.ts`

### Step 2: Create API Slice

```typescript
// src/frontend/admin/src/store/api/newFeatureApi.ts
import { createApi, fetchBaseQuery } from '@reduxjs/toolkit/query/react';

const API_URL = import.meta.env.VITE_API_URL;

export const newFeatureApi = createApi({
  reducerPath: 'newFeatureApi',
  baseQuery: fetchBaseQuery({
    baseUrl: API_URL,
    prepareHeaders: (headers) => {
      const token = localStorage.getItem('authToken');
      if (token) headers.set('Authorization', `Bearer ${token}`);
      return headers;
    },
  }),
  tagTypes: ['NewFeature'],
  endpoints: (builder) => ({
    getFeatures: builder.query<FeatureType[], void>({
      query: () => '/admin/features',
      providesTags: ['NewFeature'],
    }),
    createFeature: builder.mutation<FeatureType, CreateFeatureRequest>({
      query: (data) => ({
        url: '/admin/features',
        method: 'POST',
        body: data,
      }),
      invalidatesTags: ['NewFeature'],
    }),
  }),
});

export const { useGetFeaturesQuery, useCreateFeatureMutation } = newFeatureApi;
```

### Step 3: Add to Store

```typescript
// src/frontend/admin/src/store/store.ts
import { newFeatureApi } from './api/newFeatureApi';

export const store = configureStore({
  reducer: {
    // ... other reducers
    [newFeatureApi.reducerPath]: newFeatureApi.reducer,
  },
  middleware: (getDefaultMiddleware) =>
    getDefaultMiddleware().concat(
      // ... other middleware
      newFeatureApi.middleware
    ),
});
```

## Testing

### Mock Data

For testing without a backend, you can use mock responses:

```typescript
// Create a mock API response
const mockProducts = {
  items: [
    { id: '1', name: 'Product 1', price: 99.99 },
  ],
  totalCount: 1,
  page: 1,
  pageSize: 20,
};
```

### Using Redux DevTools

RTK includes Redux DevTools integration for debugging:

1. Install Redux DevTools browser extension
2. All API calls will appear as actions in the DevTools
3. You can inspect cache state and dispatch actions

## Performance Optimization

### Request Deduplication

RTK Query automatically deduplicates requests made within a short time window.

### Selective Caching

```typescript
// Refetch only when component mounts or arguments change
useGetProductsQuery(query, {
  refetchOnMountOrArgChange: true
});

// Refetch when window regains focus
useGetProductsQuery(query, {
  refetchOnFocus: true
});
```

### Cache Management

```typescript
// Skip query execution
const { data } = useGetProductsQuery(query, {
  skip: !isReady, // Only fetch when ready
});
```

## Error Handling

All API errors are caught and can be accessed via the `error` property:

```typescript
const { error } = useGetProductsQuery();

if (error) {
  console.error('Error:', error.data?.errors);
  // error.status - HTTP status code
  // error.data - Response data
}
```

## Debugging

Enable Redux Toolkit logging:

```typescript
// In store.ts
import { logger } from 'redux-logger';

const store = configureStore({
  reducer: { /* ... */ },
  middleware: (getDefaultMiddleware) =>
    getDefaultMiddleware()
      .concat(productApi.middleware)
      .concat(logger), // Add logger middleware
});
```

---

For questions or issues, refer to:
- [RTK Query Documentation](https://redux-toolkit.js.org/rtk-query/overview)
- [Redux Documentation](https://redux.js.org/)
