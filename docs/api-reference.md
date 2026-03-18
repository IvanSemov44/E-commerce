# API Reference

Base URL: `http://localhost:5000/api`
Interactive docs: `http://localhost:5000/swagger`

**Auth header:** `Authorization: Bearer <accessToken>`

Legend: 🔓 Public · 🔑 User (JWT required) · 🛡️ Admin only

---

## Auth

| Method | Endpoint | Auth | Description |
|--------|----------|------|-------------|
| POST | `/auth/register` | 🔓 | Register new account |
| POST | `/auth/login` | 🔓 | Login, returns `accessToken` + `refreshToken` |
| POST | `/auth/refresh` | 🔓 | Rotate refresh token, returns new token pair |
| POST | `/auth/logout` | 🔑 | Revoke refresh token |
| GET | `/auth/me` | 🔑 | Get current user info |
| POST | `/auth/forgot-password` | 🔓 | Send password reset email |
| POST | `/auth/reset-password` | 🔓 | Reset password with token |

---

## Products

| Method | Endpoint | Auth | Description |
|--------|----------|------|-------------|
| GET | `/products` | 🔓 | List products (paginated, filterable, sortable) |
| GET | `/products/featured` | 🔓 | Get featured products |
| GET | `/products/:id` | 🔓 | Get product by ID |
| GET | `/products/slug/:slug` | 🔓 | Get product by URL slug |
| GET | `/products/category/:slug` | 🔓 | Get products by category slug |
| GET | `/products/:id/reviews` | 🔓 | Get reviews for a product |
| POST | `/products` | 🛡️ | Create product |
| PUT | `/products/:id` | 🛡️ | Update product |
| DELETE | `/products/:id` | 🛡️ | Delete product |

**Query params for `GET /products`:**

| Param | Type | Example |
|-------|------|---------|
| `page` | int | `1` |
| `pageSize` | int | `20` |
| `search` | string | `"sneakers"` |
| `categoryId` | uuid | `"abc-123"` |
| `minPrice` | decimal | `10.00` |
| `maxPrice` | decimal | `200.00` |
| `minRating` | int | `4` |
| `sortBy` | string | `"price"`, `"name"`, `"createdAt"` |
| `sortDirection` | string | `"asc"`, `"desc"` |
| `isActive` | bool | `true` |
| `isFeatured` | bool | `true` |

---

## Categories

| Method | Endpoint | Auth | Description |
|--------|----------|------|-------------|
| GET | `/categories` | 🔓 | List all categories |
| GET | `/categories/top-level` | 🔓 | Get top-level categories only |
| GET | `/categories/:id` | 🔓 | Get category by ID |
| GET | `/categories/slug/:slug` | 🔓 | Get category by slug |
| POST | `/categories` | 🛡️ | Create category |
| PUT | `/categories/:id` | 🛡️ | Update category |
| DELETE | `/categories/:id` | 🛡️ | Delete category |

---

## Cart

| Method | Endpoint | Auth | Description |
|--------|----------|------|-------------|
| GET | `/cart` | 🔑 | Get current user's cart |
| POST | `/cart/items` | 🔑 | Add item to cart |
| PUT | `/cart/items/:itemId` | 🔑 | Update item quantity |
| DELETE | `/cart/items/:itemId` | 🔑 | Remove item from cart |
| DELETE | `/cart` | 🔑 | Clear entire cart |

---

## Orders

| Method | Endpoint | Auth | Description |
|--------|----------|------|-------------|
| POST | `/orders` | 🔓 | Create order (supports guest with email) |
| GET | `/orders` | 🔑 | Get current user's order history |
| GET | `/orders/:id` | 🔑 | Get order by ID |
| GET | `/orders/number/:orderNumber` | 🔑 | Get order by order number |
| POST | `/orders/:id/cancel` | 🔑 | Cancel an order |
| GET | `/orders/all` | 🛡️ | Get all orders (admin) |
| PUT | `/orders/:id/status` | 🛡️ | Update order status (admin) |
| GET | `/orders/status/:status` | 🛡️ | Get orders by status (admin) |

**Order status values:** `Pending` · `Processing` · `Shipped` · `Delivered` · `Cancelled`

---

## Payments

| Method | Endpoint | Auth | Description |
|--------|----------|------|-------------|
| POST | `/payments/process` | 🔑 | Process payment for an order |
| GET | `/payments/:orderId` | 🔑 | Get payment details for an order |
| POST | `/payments/:orderId/refund` | 🛡️ | Refund a payment |
| POST | `/payments/intent` | 🔑 | Create payment intent (Stripe) |
| POST | `/payments/webhook` | 🔓 | Stripe webhook handler (HMAC verified) |

> **Note:** Payment processing is currently mocked. See [senior-dev-next.md](senior-dev-next.md) — Stripe integration is the #1 production blocker.

---

## Inventory

| Method | Endpoint | Auth | Description |
|--------|----------|------|-------------|
| GET | `/inventory` | 🛡️ | Get inventory overview |
| GET | `/inventory/:productId` | 🛡️ | Get stock for a product |
| POST | `/inventory/:productId/adjust` | 🛡️ | Adjust stock manually |
| POST | `/inventory/bulk-update` | 🛡️ | Bulk stock update |
| GET | `/inventory/:productId/logs` | 🛡️ | Get inventory change history |
| POST | `/inventory/check` | 🔓 | Check availability before order |
| GET | `/inventory/low-stock` | 🛡️ | Get products below threshold |

---

## Reviews

| Method | Endpoint | Auth | Description |
|--------|----------|------|-------------|
| GET | `/reviews/product/:productId` | 🔓 | Get reviews for a product |
| GET | `/reviews/:id` | 🔓 | Get single review |
| GET | `/reviews/my` | 🔑 | Get current user's reviews |
| POST | `/reviews` | 🔑 | Create a review |
| PUT | `/reviews/:id` | 🔑 | Update own review |
| DELETE | `/reviews/:id` | 🔑 | Delete own review |

---

## Wishlist

| Method | Endpoint | Auth | Description |
|--------|----------|------|-------------|
| GET | `/wishlist` | 🔑 | Get user's wishlist |
| POST | `/wishlist` | 🔑 | Add product to wishlist |
| DELETE | `/wishlist/:productId` | 🔑 | Remove product from wishlist |
| GET | `/wishlist/count` | 🔑 | Get wishlist item count |

---

## Profile

| Method | Endpoint | Auth | Description |
|--------|----------|------|-------------|
| GET | `/profile` | 🔑 | Get user profile |
| PUT | `/profile` | 🔑 | Update profile info |
| GET | `/profile/preferences` | 🔑 | Get user preferences |
| PUT | `/profile/preferences` | 🔑 | Update user preferences |

---

## Promo Codes

| Method | Endpoint | Auth | Description |
|--------|----------|------|-------------|
| POST | `/promo-codes/validate` | 🔓 | Validate a promo code |
| GET | `/promo-codes/:code` | 🔑 | Get promo code details |
| GET | `/promo-codes` | 🛡️ | List all promo codes |
| POST | `/promo-codes` | 🛡️ | Create promo code |
| PUT | `/promo-codes/:id` | 🛡️ | Update promo code |
| DELETE | `/promo-codes/:id` | 🛡️ | Delete promo code |

---

## Dashboard

| Method | Endpoint | Auth | Description |
|--------|----------|------|-------------|
| GET | `/dashboard/stats` | 🛡️ | Get aggregate stats (orders, revenue, customers, products, trends) |

---

## Standard response envelope

All responses are wrapped in `ApiResponse<T>`:

```json
{
  "success": true,
  "data": { ... },
  "message": null,
  "errors": null
}
```

**Error response:**
```json
{
  "success": false,
  "data": null,
  "message": "INSUFFICIENT_STOCK",
  "errors": {
    "productId": ["Item is out of stock"]
  }
}
```

**HTTP status codes used:**

| Code | When |
|------|------|
| 200 | Successful GET / update |
| 201 | Resource created |
| 204 | Successful delete |
| 400 | Bad request / business rule failure |
| 401 | Missing or invalid JWT |
| 403 | Authenticated but wrong role |
| 404 | Resource not found |
| 409 | Conflict (e.g. concurrency, duplicate) |
| 422 | Validation failure (field-level errors) |
| 429 | Rate limit exceeded |
| 500 | Unexpected server error |
