# Storefront vs Admin: Complete Comparison

## Overview

| Aspect | Storefront | Admin |
|--------|-----------|-------|
| **Purpose** | Customer-facing e-commerce site | Internal management dashboard |
| **Users** | Regular customers (Customer role) | Staff (Admin/SuperAdmin roles) |
| **Port** | 5175 | 5174 |
| **API URL** | `/api/v1` ❌ (WRONG - needs fix) | `/api` ✅ (FIXED) |
| **Main Functions** | Browse, buy products, manage account | Manage everything |

---

## Feature Comparison

### 1. Authentication

#### Storefront
```typescript
// File: src/frontend/storefront/src/store/api/authApi.ts

Endpoints:
- POST /auth/register     - Customer creates account
- POST /auth/login        - Customer logs in
- POST /auth/refresh-token - Get new token

Uses:
- Customer role only
- Token in localStorage
- Basic login/register forms
```

#### Admin
```typescript
// File: src/frontend/admin/src/store/api/authApi.ts

Endpoints:
- POST /auth/register     - Admin/superadmin registration
- POST /auth/login        - Admin/superadmin login
- POST /auth/refresh-token - Get new token
- GET /auth/me            - Get current user (MISSING ❌)
- POST /auth/logout       - Logout (MISSING ❌)

Uses:
- Admin/SuperAdmin role checking
- Protected routes with role-based access
- Professional admin login page
- User dropdown in header
```

---

### 2. Product Management

#### Storefront
```typescript
// File: src/frontend/storefront/src/store/api/productsApi.ts

Endpoints (READ ONLY):
- GET /products           - Browse all products with pagination
- GET /products/featured  - View featured products
- GET /products/{id}      - Product details
- GET /products/slug/{slug} - Product by URL slug

Purpose:
- Customers can only VIEW products
- Search and filter products
- Read reviews and ratings

No Create/Update/Delete - Customers can't manage products
```

#### Admin
```typescript
// File: src/frontend/admin/src/store/api/productsApi.ts

Endpoints (FULL CRUD):
- GET /products           - List all products (admin view)
- GET /products/{id}      - Product details
- POST /products          - Create new product ❌ MISSING
- PUT /products/{id}      - Edit product ❌ MISSING
- DELETE /products/{id}   - Delete product ❌ MISSING
- PUT /products/{id}/stock - Update inventory ❌ MISSING

Purpose:
- Admins manage entire product catalog
- Add/edit/delete products
- Manage stock levels
- Bulk operations

All CRUD operations require Admin/SuperAdmin role
```

---

### 3. Orders

#### Storefront
```typescript
// File: src/frontend/storefront/src/store/api/ordersApi.ts

Endpoints:
- POST /checkout          - Customer places order (with full address, payment)
- GET /orders             - View their own orders
- GET /orders/{id}        - View specific order details

CreateOrderRequest includes:
- Cart items with quantities
- Shipping address (required)
- Billing address (optional, same as shipping)
- Payment method
- Promo code (optional)

OrderResponse includes:
- Order ID & order number
- Client secret (for Stripe)
- Totals (subtotal, discount, shipping, tax, total)

Purpose:
- Customers complete purchases
- View their own order history
- Track orders
- Payment processing
```

#### Admin
```typescript
// File: src/frontend/admin/src/store/api/ordersApi.ts

Endpoints:
- GET /orders             - View ALL orders (paginated, filtered, searchable)
  Query params: page, pageSize, status, search
- GET /orders/{id}        - View any customer's order
- PUT /orders/{id}/status - Update order status ❌ MISSING
- GET /orders/stats       - Order statistics dashboard ❌ MISSING
  Returns: totalOrders, totalRevenue, ordersToday, pendingOrders

Purpose:
- Admins manage all customer orders
- Filter by status (pending, shipped, delivered, etc.)
- Update order status
- View order metrics for dashboard
- Monitor orders in real-time

All operations require Admin/SuperAdmin role
```

---

### 4. Customers/Users

#### Storefront
```typescript
// No dedicated customers API
- Users self-manage their own account
- Can view/edit their profile
- Can change password
- Can manage addresses (wishlist, shipping addresses)
```

#### Admin
```typescript
// File: src/frontend/admin/src/store/api/customersApi.ts

Endpoints:
- GET /customers          - List all customers (paginated, searchable)
- GET /customers/{id}     - View customer details ❌ MISSING
- GET /customers/stats    - Customer statistics ❌ MISSING
  Returns: totalCustomers, activeCustomers, newCustomersThisMonth

Purpose:
- Admins view all registered customers
- Search/filter customers
- View customer stats (growth, activity)
- Monitor customer base

All operations require Admin/SuperAdmin role
```

---

### 5. Dashboard

#### Storefront
```
No dashboard - Just a shopping site
Homepage shows:
- Featured products
- Product categories
- Search
- Shopping cart
- User menu
```

#### Admin
```typescript
// File: src/frontend/admin/src/store/api/dashboardApi.ts

Endpoints:
- GET /dashboard/stats    - Dashboard statistics ❌ MISSING

Returns:
- totalOrders: number
- totalRevenue: decimal
- totalCustomers: number
- totalProducts: number
- ordersTrend: array (30-day trend)
- revenueTrend: array (30-day trend)

Features:
- Real-time metrics
- Auto-refresh every 30 seconds (polling)
- Charts and graphs
- KPI cards showing key metrics

Purpose:
- Quick business overview
- Monitor sales trends
- Track customer growth
- Track product count
```

---

## Architecture Differences

### Storefront Architecture
```
Frontend (Storefront)
├── Pages
│   ├── Home (Browse products)
│   ├── Product Detail
│   ├── Cart
│   ├── Checkout
│   ├── Account
│   └── Order History
│
├── Components
│   ├── Product Card
│   ├── Cart Item
│   ├── Checkout Form
│   └── Account Settings
│
└── API
    ├── authApi (login/register only)
    ├── productsApi (read-only)
    └── ordersApi (create order, view own orders)

Redux State:
- auth (customer data, token)
- cart (items, quantities)
- ui (modals, notifications)
```

### Admin Architecture
```
Frontend (Admin)
├── Pages
│   ├── Dashboard (Stats overview)
│   ├── Products (Manage catalog)
│   ├── Orders (Manage orders)
│   ├── Customers (View customers)
│   ├── Settings
│   └── Login
│
├── Components
│   ├── Sidebar (Navigation)
│   ├── Header (User info, logout)
│   ├── Tables (Data display)
│   ├── Forms (Create/Edit)
│   └── UI Components (Button, Input, Card)
│
└── API
    ├── authApi (login/register/me/logout)
    ├── productsApi (full CRUD)
    ├── ordersApi (view all, update status)
    ├── customersApi (view all, stats)
    └── dashboardApi (statistics)

Redux State:
- auth (admin user, token, role)
- All RTK Query caches for data
```

---

## API URL Issue ⚠️

### Storefront (Currently Wrong ❌)
```typescript
const API_URL = import.meta.env.VITE_API_URL || 'http://localhost:5000/api/v1';

// Expects endpoints like:
POST /api/v1/auth/login
GET /api/v1/products
```

**Problem:** Backend doesn't have `/v1` prefix. Should be `/api`.

**Solution needed:** Change to:
```typescript
const API_URL = import.meta.env.VITE_API_URL || 'http://localhost:5000/api';
```

### Admin (Now Fixed ✅)
```typescript
const API_URL = import.meta.env.VITE_API_URL || 'http://localhost:5000/api';

// Uses correct endpoints:
POST /api/auth/login
GET /api/products
```

---

## User Roles & Access

### Storefront Users
```
Role: Customer (default)
Can:
✅ Register and login
✅ Browse all products
✅ View featured products
✅ Search products
✅ Add to cart
✅ Checkout and create orders
✅ View own orders
✅ Manage account profile

Cannot:
❌ Create/edit/delete products
❌ View other customers' orders
❌ Access admin dashboard
❌ Manage inventory
```

### Admin Users
```
Role: Admin or SuperAdmin
Can:
✅ Login to admin panel
✅ Create/edit/delete products
✅ Manage inventory/stock
✅ View all orders
✅ Update order status
✅ View all customers
✅ View dashboard/statistics
✅ Access all admin features

Admin-specific:
- Can do most admin tasks
- Cannot access superadmin-only features

SuperAdmin-specific:
- Can do everything
- Access settings/system configuration
- Manage other admin users
```

---

## Key Differences Summary

| Feature | Storefront | Admin |
|---------|-----------|-------|
| **Audience** | Customers | Staff |
| **Auth** | Self-register | Admin only |
| **Products** | Read-only browse | Full CRUD |
| **Orders** | Create & view own | View all, update status |
| **Customers** | None | View all, stats |
| **Dashboard** | Homepage | Analytics dashboard |
| **Data Model** | Single customer | All business data |
| **Pages** | 5-7 (browse, shop) | 5+ (manage all) |
| **Complexity** | Medium | High |
| **Role** | Customer | Admin/SuperAdmin |

---

## What Each App Does

### Storefront = Online Store
- Customers browse products
- Add items to cart
- Checkout and pay
- View order history
- Manage account

### Admin = Business Management
- Manage product catalog
- Monitor orders
- View customer list
- Track revenue/metrics
- Update order status

---

## Deployment Difference

### Storefront
- Public facing
- Accessible to anyone
- User registration enabled
- Payment processing
- High traffic expected

### Admin
- Internal only
- IP restricted or VPN
- Limited users (staff only)
- No payment processing
- Lower traffic

---

## Next Steps

### For Storefront
1. Fix API URL: Change `/api/v1` to `/api`
2. Test product browsing
3. Implement shopping cart
4. Implement checkout flow
5. Implement payment (Stripe)

### For Admin
1. Wait for backend to implement missing endpoints
2. Once backend is ready:
   - Test product management CRUD
   - Test order status updates
   - Test dashboard statistics
   - Test customer viewing

### Backend
Needs to implement:
- Admin product CRUD endpoints
- Order management endpoints
- Customer listing endpoints
- Dashboard statistics endpoint
- User "me" endpoint
