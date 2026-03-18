# Error Codes Reference

All error codes are defined in `src/backend/ECommerce.Core/Constants/ErrorCodes.cs`.

When a service returns `Result.Failure(errorCode)`, the controller maps it to an HTTP response using `ApiResponse<T>`. The frontend receives the `message` field containing the error code string.

---

## Cart

| Code | HTTP | When it occurs |
|------|------|----------------|
| `CART_NOT_FOUND` | 404 | Cart does not exist for the current user/session |
| `CART_ITEM_NOT_FOUND` | 404 | Item ID does not exist in the cart |

---

## Products

| Code | HTTP | When it occurs |
|------|------|----------------|
| `PRODUCT_NOT_FOUND` | 404 | Product ID or slug does not exist |
| `PRODUCT_NOT_AVAILABLE` | 400 | Product exists but `IsActive = false` |
| `DUPLICATE_PRODUCT_SLUG` | 409 | A product with this slug already exists |

---

## Inventory

| Code | HTTP | When it occurs |
|------|------|----------------|
| `INSUFFICIENT_STOCK` | 409 | Requested quantity exceeds available stock |
| `INVALID_QUANTITY` | 422 | Quantity is zero or negative |

---

## Orders

| Code | HTTP | When it occurs |
|------|------|----------------|
| `ORDER_NOT_FOUND` | 404 | Order ID or number does not exist |
| `ORDER_ALREADY_PROCESSED` | 409 | Attempt to cancel or modify an order that is past `Pending` |
| `INVALID_ORDER_STATE` | 400 | Transition from current status is not allowed |
| `INVALID_ORDER_STATUS` | 400 | The requested status value is not a valid `OrderStatus` enum value |
| `ORDER_CREATION_FAILED` | 500 | Unexpected failure during order transaction |

---

## Categories

| Code | HTTP | When it occurs |
|------|------|----------------|
| `CATEGORY_NOT_FOUND` | 404 | Category ID or slug does not exist |
| `CATEGORY_ALREADY_EXISTS` | 409 | Category name already exists at same parent level |
| `DUPLICATE_CATEGORY_SLUG` | 409 | Slug is already taken by another category |
| `CATEGORY_HAS_PRODUCTS` | 409 | Attempt to delete a category that still has products |

---

## Promo Codes

| Code | HTTP | When it occurs |
|------|------|----------------|
| `INVALID_PROMO_CODE` | 400 | Code not found, expired, inactive, or order below minimum amount |
| `DUPLICATE_PROMO_CODE` | 409 | A promo code with this code string already exists |
| `PROMO_CODE_NOT_FOUND` | 404 | Promo code ID does not exist |
| `PROMO_CODE_USAGE_LIMIT_REACHED` | 409 | Code has been used the maximum number of times |

---

## Reviews

| Code | HTTP | When it occurs |
|------|------|----------------|
| `REVIEW_NOT_FOUND` | 404 | Review ID does not exist |
| `INVALID_RATING` | 422 | Rating is outside the 1–5 range |
| `EMPTY_REVIEW_COMMENT` | 422 | Comment field is required but empty |
| `DUPLICATE_REVIEW` | 409 | User has already reviewed this product |
| `REVIEW_UPDATE_EXPIRED` | 400 | Review edit window (e.g. 30 days) has passed |

---

## Wishlist

| Code | HTTP | When it occurs |
|------|------|----------------|
| `DUPLICATE_WISHLIST_ITEM` | 409 | Product is already in the user's wishlist |

---

## Pagination

| Code | HTTP | When it occurs |
|------|------|----------------|
| `INVALID_PAGINATION` | 422 | `page` < 1 or `pageSize` outside allowed range |

---

## Users

| Code | HTTP | When it occurs |
|------|------|----------------|
| `USER_NOT_FOUND` | 404 | User ID does not exist |

---

## Payments

| Code | HTTP | When it occurs |
|------|------|----------------|
| `UNSUPPORTED_PAYMENT_METHOD` | 400 | Payment method string is not in the supported list |
| `PAYMENT_AMOUNT_MISMATCH` | 400 | Payment amount does not match order total |
| `NO_PAYMENT_FOUND` | 404 | No payment record exists for this order |
| `PAYMENT_INTENT_NOT_FOUND` | 404 | Stripe payment intent ID not found |
| `INVALID_REFUND` | 400 | Refund amount exceeds original payment or order is not in a refundable state |

---

## Authentication & Authorization

| Code | HTTP | When it occurs |
|------|------|----------------|
| `UNAUTHORIZED` | 401 | No JWT token provided or token is invalid/expired |
| `FORBIDDEN` | 403 | Valid JWT but insufficient role (e.g. Customer accessing Admin endpoint) |
| `INVALID_CREDENTIALS` | 401 | Email/password combination does not match |
| `INVALID_TOKEN` | 400 | Refresh token, email verification token, or password reset token is invalid or expired |
| `DUPLICATE_EMAIL` | 409 | Registration attempted with an email that already exists |

---

## Concurrency

| Code | HTTP | When it occurs |
|------|------|----------------|
| `CONCURRENCY_CONFLICT` | 409 | Two concurrent requests modified the same row (optimistic lock failure). Client should retry. |

---

## Frontend handling

```typescript
// RTK Query error shape
{
  status: 409,
  data: {
    success: false,
    message: "CONCURRENCY_CONFLICT",
    errors: null
  }
}

// Pattern for mapping codes to user-facing messages
const ERROR_MESSAGES: Record<string, string> = {
  INSUFFICIENT_STOCK: "Sorry, this item is out of stock.",
  DUPLICATE_EMAIL: "An account with this email already exists.",
  INVALID_CREDENTIALS: "Incorrect email or password.",
  CONCURRENCY_CONFLICT: "Something changed while you were checking out. Please review your cart.",
  // ...
}
```

Validation errors (`422`) return field-level errors in the `errors` object instead of a single `message` code:

```json
{
  "success": false,
  "message": null,
  "errors": {
    "email": ["Email is required", "Email must be valid"],
    "password": ["Password must be at least 8 characters"]
  }
}
```
