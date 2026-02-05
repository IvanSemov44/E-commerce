# E-Commerce UX & Trust Improvements Plan

> **Purpose:** Actionable implementation plan for UX/trust gaps identified via best-practice
> audit. Ordered by impact. Every code block matches the live codebase conventions exactly.
> Any AI or developer can pick up mid-way and execute from checkboxes alone.

---

## Conventions Reference

| Rule | Detail |
|------|--------|
| CSS | CSS Modules (`*.module.css`). Never global styles or inline `style={}` for layout. |
| Color palette | Dark `#0f172a`, Slate `#475569`, Blue primary `#1976d2`, Green `#16a34a`, Red `#ef4444`, Light bg `#f8fafc`, Border `#e2e8f0` |
| Icons | Heroicons outline, `viewBox="0 0 24 24"`, `strokeWidth={2}`, `fill="none"` |
| Components | Reuse `Button`, `Card`, `Input`, `ErrorAlert`, `LoadingSkeleton`, `EmptyState`, `PageHeader` |
| C# namespaces | File-scoped (`namespace X.Y.Z;`) |
| RTK Query | Per-feature `*Api.ts` files. `transformResponse` unwraps `ApiResponse<T>`. |
| Auth state | `useAppSelector((state) => state.auth)` gives `{ isAuthenticated, user, token }` |

---

## Current-State Summary

### What already works
- JWT auth, refresh tokens, email verification, password reset
- Product images gallery (thumbnails), ratings/reviews, wishlists, compare-at price
- Cart: local + backend sync, unauthenticated add-to-cart supported
- Orders: create (`[AllowAnonymous]`), history, detail, cancel. `Order.GuestEmail` exists.
- `CreateOrderDto.GuestEmail` added — backend guest checkout is ready
- Payments: `GET /api/payments/methods` returns supported list (`[AllowAnonymous]`). `ProcessPaymentDto.PaymentMethod` accepts any string
- Categories: hierarchical, slugs
- Emails: confirmation, shipped (accepts `trackingNumber` param), delivered, abandoned cart
- Admin: Dashboard, Products, Orders (status select per row), Reviews, Customers, PromoCodes, Inventory, Settings
- Responsive: mobile hamburger header, `@media` queries in 15 CSS files
- Loading/error: `LoadingSkeleton` + `ErrorAlert` used across all pages

### Gaps (this plan fixes)
1. Checkout hardcodes `paymentMethod: 'card'` — backend supports 6 methods, UI shows none
2. No trust signals anywhere near payment or Add-to-Cart buttons
3. Guest checkout: backend ready, but frontend never sends `guestEmail`
4. Order status shown as colored text only — no timeline/stepper
5. `Order` entity has no `TrackingNumber` field — email service accepts one but can't store/display it
6. Products page sidebar is fixed 280 px inline style — breaks on mobile
7. All 10 footer links (`to="/"`) are dead — About, Help, Track Order, Returns, Privacy, Terms, etc.
8. Newsletter subscribe form is a no-op (`e.preventDefault()` only)

---

## Phase 1 — Trust Signals on Checkout & Product Pages

**Goal:** Add a trust bar (lock + returns + shipping) above Place Order, and a smaller trust
strip below Add to Cart on ProductDetail. Zero backend changes.

### [ ] 1.1 — `Checkout.module.css`: add trust-bar styles

Add at the **end** of the file:

```css
/* Trust Signals */
.trustBar {
  display: flex;
  justify-content: center;
  gap: 24px;
  flex-wrap: wrap;
  padding: 16px 0;
  margin-bottom: 16px;
  border-top: 1px solid #e2e8f0;
  border-bottom: 1px solid #e2e8f0;
}

.trustItem {
  display: flex;
  align-items: center;
  gap: 8px;
  font-size: 0.875rem;
  color: #475569;
  font-weight: 500;
}

.trustIcon {
  width: 20px;
  height: 20px;
  color: #16a34a;
  flex-shrink: 0;
}
```

### [ ] 1.2 — `Checkout.tsx`: insert trust bar before Place Order button

Inside the `<form>`, replace:

```tsx
                <Button
                  type="submit"
                  size="lg"
                  className={styles.actionButton}
                >
                  Place Order
                </Button>
```

with:

```tsx
                {/* Trust Signals */}
                <div className={styles.trustBar}>
                  <div className={styles.trustItem}>
                    <svg className={styles.trustIcon} fill="none" stroke="currentColor" viewBox="0 0 24 24">
                      <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M9 12l2 2 4-4m5.618-4.016A11.955 11.955 0 0112 2.944a11.955 11.955 0 01-8.618 3.04A12.02 12.02 0 003 9c0 5.591 3.824 10.29 9 11.622 5.176-1.332 9-6.03 9-11.622 0-1.042-.133-2.052-.382-3.016z" />
                    </svg>
                    <span>Secure Checkout</span>
                  </div>
                  <div className={styles.trustItem}>
                    <svg className={styles.trustIcon} fill="none" stroke="currentColor" viewBox="0 0 24 24">
                      <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M4 4v5h.582m15.356 2A8.001 8.001 0 004.582 9m0 0H9m11 11v-5h-.581m0 0a8.003 8.003 0 01-15.357-2m15.357 2H15" />
                    </svg>
                    <span>30-Day Returns</span>
                  </div>
                  <div className={styles.trustItem}>
                    <svg className={styles.trustIcon} fill="none" stroke="currentColor" viewBox="0 0 24 24">
                      <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M13 16V6a1 1 0 00-1-1H4a1 1 0 00-1 1v10a1 1 0 001 1h1m8-1a1 1 0 01-1 1H9m4-1V8a1 1 0 011-1h2.586a1 1 0 01.707.293l3.414 3.414a1 1 0 01.293.707V16a1 1 0 01-1 1h-1m-6-1a1 1 0 001 1h1M5 17a2 2 0 104 0m-4 0a2 2 0 114 0m6 0a2 2 0 104 0m-4 0a2 2 0 114 0" />
                    </svg>
                    <span>Free Shipping over $100</span>
                  </div>
                </div>

                <Button
                  type="submit"
                  size="lg"
                  className={styles.actionButton}
                >
                  Place Order
                </Button>
```

### [ ] 1.3 — `ProductDetail.module.css`: add trust-strip styles

Add at the **end** of the file:

```css
/* Trust Strip */
.trustStrip {
  display: flex;
  justify-content: center;
  gap: 20px;
  flex-wrap: wrap;
  padding: 16px 0;
  margin-top: 16px;
  border-top: 1px solid #e2e8f0;
}

.trustStripItem {
  display: flex;
  align-items: center;
  gap: 6px;
  font-size: 0.8125rem;
  color: #475569;
}

.trustStripIcon {
  width: 16px;
  height: 16px;
  color: #16a34a;
  flex-shrink: 0;
}
```

### [ ] 1.4 — `ProductDetail.tsx`: insert trust strip after the actions div

After the closing `</div>` of `<div className={styles.actions}>`, add:

```tsx
              {/* Trust Strip */}
              <div className={styles.trustStrip}>
                <span className={styles.trustStripItem}>
                  <svg className={styles.trustStripIcon} fill="none" stroke="currentColor" viewBox="0 0 24 24">
                    <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M9 12l2 2 4-4m5.618-4.016A11.955 11.955 0 0112 2.944a11.955 11.955 0 01-8.618 3.04A12.02 12.02 0 003 9c0 5.591 3.824 10.29 9 11.622 5.176-1.332 9-6.03 9-11.622 0-1.042-.133-2.052-.382-3.016z" />
                  </svg>
                  Secure Payment
                </span>
                <span className={styles.trustStripItem}>
                  <svg className={styles.trustStripIcon} fill="none" stroke="currentColor" viewBox="0 0 24 24">
                    <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M4 4v5h.582m15.356 2A8.001 8.001 0 004.582 9m0 0H9m11 11v-5h-.581m0 0a8.003 8.003 0 01-15.357-2m15.357 2H15" />
                  </svg>
                  30-Day Returns
                </span>
                <span className={styles.trustStripItem}>
                  <svg className={styles.trustStripIcon} fill="none" stroke="currentColor" viewBox="0 0 24 24">
                    <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M13 16V6a1 1 0 00-1-1H4a1 1 0 00-1 1v10a1 1 0 001 1h1m8-1a1 1 0 01-1 1H9m4-1V8a1 1 0 011-1h2.586a1 1 0 01.707.293l3.414 3.414a1 1 0 01.293.707V16a1 1 0 01-1 1h-1m-6-1a1 1 0 001 1h1M5 17a2 2 0 104 0m-4 0a2 2 0 114 0m6 0a2 2 0 104 0m-4 0a2 2 0 114 0" />
                  </svg>
                  Free Shipping $100+
                </span>
              </div>
```

**Tests:** No backend tests affected. Manual: verify trust bar renders on both pages.

---

## Phase 2 — Payment Method Selection

**Goal:** Replace hardcoded `paymentMethod: 'card'` with a visual selector. Backend already
has `GET /api/payments/methods` (`[AllowAnonymous]`).

### [ ] 2.1 — `storefront/src/store/api/paymentsApi.ts` — create new file

```ts
import { createApi, fetchBaseQuery } from '@reduxjs/toolkit/query/react';

const API_URL = import.meta.env.VITE_API_URL || 'http://localhost:5000/api';

export interface SupportedPaymentMethodsResponse {
  methods: string[];
}

export const paymentsApi = createApi({
  reducerPath: 'paymentsApi',
  baseQuery: fetchBaseQuery({ baseUrl: API_URL }),
  endpoints: (builder) => ({
    getSupportedPaymentMethods: builder.query<string[], void>({
      query: () => '/payments/methods',
      transformResponse: (response: { success: boolean; data: SupportedPaymentMethodsResponse }) =>
        response.data?.methods || [],
    }),
  }),
});

export const { useGetSupportedPaymentMethodsQuery } = paymentsApi;
```

### [ ] 2.2 — Register `paymentsApi` in the Redux store

Open the storefront store configuration file (where `ordersApi` reducer is added) and add:

```ts
import { paymentsApi } from './api/paymentsApi';

// in configureStore reducers:
[paymentsApi.reducerPath]: paymentsApi.reducer,

// in middleware:
.getDefaultMiddleware().concat(paymentsApi.middleware)
```

### [ ] 2.3 — `Checkout.module.css`: add payment selector styles

Add at the **end** of the file:

```css
/* Payment Method Selector */
.paymentSection {
  margin-bottom: 16px;
}

.paymentTitle {
  font-size: 1rem;
  font-weight: 600;
  color: #0f172a;
  margin-bottom: 12px;
}

.paymentOptions {
  display: grid;
  grid-template-columns: repeat(2, 1fr);
  gap: 10px;
}

.paymentOption {
  display: flex;
  align-items: center;
  gap: 10px;
  padding: 12px 14px;
  border: 2px solid #e2e8f0;
  border-radius: 8px;
  background: #ffffff;
  cursor: pointer;
  font-size: 0.9rem;
  font-weight: 500;
  color: #0f172a;
  transition: border-color 0.15s ease, background-color 0.15s ease;
  text-align: left;
  width: 100%;
}

.paymentOption:hover {
  border-color: #cbd5e1;
  background-color: #f8fafc;
}

.paymentOptionActive {
  border-color: #1976d2;
  background-color: #eff6ff;
}

.paymentOptionIcon {
  width: 24px;
  height: 24px;
  color: #475569;
  flex-shrink: 0;
}
```

### [ ] 2.4 — `Checkout.tsx`: add payment method state + UI

**Add import** at the top:

```tsx
import { useGetSupportedPaymentMethodsQuery } from '../store/api/paymentsApi';
```

**Add state** after the existing `promoCodeValidation` state block:

```tsx
  const [paymentMethod, setPaymentMethod] = useState('credit_card');
  useGetSupportedPaymentMethodsQuery(); // keeps the query warm; list is static for now
```

**Add the payment selector UI** inside the `<form>`, directly before the trust bar (Phase 1
block):

```tsx
                {/* Payment Method */}
                <div className={styles.paymentSection}>
                  <h3 className={styles.paymentTitle}>Payment Method</h3>
                  <div className={styles.paymentOptions}>
                    <button
                      type="button"
                      className={`${styles.paymentOption} ${paymentMethod === 'credit_card' ? styles.paymentOptionActive : ''}`}
                      onClick={() => setPaymentMethod('credit_card')}
                    >
                      <svg className={styles.paymentOptionIcon} fill="none" stroke="currentColor" viewBox="0 0 24 24">
                        <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M3 10h18M3 14h9m-9 4h12m-5-8v8m-4-8v8" />
                      </svg>
                      Credit / Debit Card
                    </button>
                    <button
                      type="button"
                      className={`${styles.paymentOption} ${paymentMethod === 'paypal' ? styles.paymentOptionActive : ''}`}
                      onClick={() => setPaymentMethod('paypal')}
                    >
                      <svg className={styles.paymentOptionIcon} fill="none" stroke="currentColor" viewBox="0 0 24 24">
                        <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M12 8c-1.657 0-3 .895-3 2s1.343 2 3 2 3 .895 3 2-1.343 2-3 2m0-8c1.11 0 2.08.402 2.599 1M12 8V7m0 1v8m0 0v1m0-1c-1.11 0-2.08-.402-2.599-1M21 12a9 9 0 11-18 0 9 9 0 0118 0z" />
                      </svg>
                      PayPal
                    </button>
                    <button
                      type="button"
                      className={`${styles.paymentOption} ${paymentMethod === 'apple_pay' ? styles.paymentOptionActive : ''}`}
                      onClick={() => setPaymentMethod('apple_pay')}
                    >
                      <svg className={styles.paymentOptionIcon} fill="none" stroke="currentColor" viewBox="0 0 24 24">
                        <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M12 22s8-4 8-10V5l-8-3-8 3v7c0 6 8 10 8 10z" />
                      </svg>
                      Apple Pay
                    </button>
                    <button
                      type="button"
                      className={`${styles.paymentOption} ${paymentMethod === 'google_pay' ? styles.paymentOptionActive : ''}`}
                      onClick={() => setPaymentMethod('google_pay')}
                    >
                      <svg className={styles.paymentOptionIcon} fill="none" stroke="currentColor" viewBox="0 0 24 24">
                        <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M9 19v-6a2 2 0 00-2-2H5a2 2 0 00-2 2v6a2 2 0 002 2h2a2 2 0 002-2zm0 0V9a2 2 0 012-2h2a2 2 0 012 2v10m-6 0a2 2 0 002 2h2a2 2 0 002-2m0 0V5a2 2 0 012-2h2a2 2 0 012 2v14a2 2 0 01-2 2h-2a2 2 0 01-2-2z" />
                      </svg>
                      Google Pay
                    </button>
                  </div>
                </div>
```

**Wire the selection** — in `handleSubmit`, change:

```tsx
        paymentMethod: 'card',
```

to:

```tsx
        paymentMethod,
```

**Tests:** No backend tests affected. Manual: select each method, verify `paymentMethod`
value changes in network tab on order creation.

---

## Phase 3 — Wire Guest Checkout

**Goal:** Backend is fully ready (`CreateOrderDto.GuestEmail`, `Order.GuestEmail`,
`[AllowAnonymous]` on POST). Frontend just needs to send the email.

### [ ] 3.1 — `storefront/src/store/api/ordersApi.ts`: add `guestEmail` to request type

In `CreateOrderRequest`, add after `promoCode`:

```ts
  promoCode?: string;
  guestEmail?: string;   // ← add this line
}
```

### [ ] 3.2 — `Checkout.tsx`: read auth state and send `guestEmail`

**Add** after the existing `useAppSelector` calls (line ~21):

```tsx
  const { isAuthenticated } = useAppSelector((state) => state.auth);
```

**In `handleSubmit`**, change the `orderData` object — add after `promoCode`:

```tsx
        promoCode: promoCodeValidation?.isValid ? promoCode : undefined,
        guestEmail: !isAuthenticated ? formData.email : undefined,
```

**Tests:** Integration test `CartControllerTests.AddItemToCart_WithUnauthenticated_AllowedForGuests`
already covers the cart side. Add a manual test: clear auth, add item, checkout with email —
verify order is created with `GuestEmail` populated.

---

## Phase 4 — Order Status Timeline

**Goal:** Replace the plain colored-text status in OrderDetail with a horizontal stepper
showing Pending → Processing → Shipped → Delivered with real timestamps.

### [ ] 4.1 — `OrderDetail.module.css`: add timeline styles

Add at the **end** of the file:

```css
/* Order Status Timeline */
.timeline {
  display: flex;
  align-items: flex-start;
  padding: 24px 0 8px;
  position: relative;
}

.timelineStep {
  flex: 1;
  display: flex;
  flex-direction: column;
  align-items: center;
  position: relative;
}

.timelineStep:not(:last-child)::after {
  content: '';
  position: absolute;
  top: 15px;
  left: 50%;
  right: -50%;
  height: 3px;
  background-color: #e2e8f0;
  z-index: 0;
}

.timelineStep.completed:not(:last-child)::after {
  background-color: #16a34a;
}

.timelineDot {
  width: 30px;
  height: 30px;
  border-radius: 9999px;
  border: 3px solid #e2e8f0;
  background-color: #ffffff;
  display: flex;
  align-items: center;
  justify-content: center;
  position: relative;
  z-index: 1;
  margin-bottom: 10px;
  transition: border-color 0.2s ease, background-color 0.2s ease;
}

.timelineDotCompleted {
  border-color: #16a34a;
  background-color: #16a34a;
}

.timelineDotCurrent {
  border-color: #1976d2;
  background-color: #1976d2;
}

.timelineDotIcon {
  width: 14px;
  height: 14px;
  color: #ffffff;
}

.timelineLabel {
  font-size: 0.8125rem;
  font-weight: 600;
  color: #94a3b8;
  text-align: center;
}

.timelineLabelCompleted {
  color: #16a34a;
}

.timelineLabelCurrent {
  color: #1976d2;
}

.timelineDate {
  font-size: 0.75rem;
  color: #94a3b8;
  text-align: center;
  margin-top: 4px;
}

.cancelledBanner {
  display: flex;
  align-items: center;
  gap: 8px;
  padding: 12px 16px;
  background-color: #fee2e2;
  border: 1px solid #fca5a5;
  border-radius: 8px;
  color: #991b1b;
  font-weight: 600;
  font-size: 0.9rem;
  margin-top: 8px;
}

.cancelledBannerIcon {
  width: 20px;
  height: 20px;
  flex-shrink: 0;
}

@media (max-width: 480px) {
  .timelineLabel {
    font-size: 0.6875rem;
  }

  .timelineDot {
    width: 24px;
    height: 24px;
  }

  .timelineStep:not(:last-child)::after {
    top: 11px;
  }
}
```

### [ ] 4.2 — `OrderDetail.tsx`: replace status text with timeline

Replace the entire **Order Header** `<Card>` block (the one with Order Number / Date / Status / Cancel)
with this implementation. The card stays — only the interior changes:

```tsx
          {/* Order Header */}
          <Card variant="elevated" padding="lg">
            <div
              style={{
                display: 'grid',
                gridTemplateColumns: 'repeat(auto-fit, minmax(200px, 1fr))',
                gap: '2rem',
              }}
            >
              <div>
                <p style={{ margin: 0, fontSize: '0.875rem', color: '#666' }}>
                  Order Number
                </p>
                <p style={{ margin: '0.5rem 0 0 0', fontSize: '1.25rem', fontWeight: 600 }}>
                  {order.orderNumber}
                </p>
              </div>

              <div>
                <p style={{ margin: 0, fontSize: '0.875rem', color: '#666' }}>
                  Date
                </p>
                <p style={{ margin: '0.5rem 0 0 0', fontSize: '1rem', fontWeight: 500 }}>
                  {new Date(order.createdAt).toLocaleDateString()}{' '}
                  {new Date(order.createdAt).toLocaleTimeString()}
                </p>
              </div>

              {canCancel && (
                <div style={{ display: 'flex', alignItems: 'flex-end' }}>
                  <Button
                    variant="secondary"
                    onClick={handleCancel}
                    disabled={isCancelling}
                  >
                    Cancel Order
                  </Button>
                </div>
              )}
            </div>

            {/* Status Timeline */}
            {(() => {
              const steps = [
                { key: 'Pending',    label: 'Order Placed', date: order.createdAt },
                { key: 'Processing', label: 'Processing',   date: order.createdAt },
                { key: 'Shipped',    label: 'Shipped',      date: order.shippedAt },
                { key: 'Delivered',  label: 'Delivered',    date: order.deliveredAt },
              ];
              const statusOrder = ['Pending', 'Processing', 'Shipped', 'Delivered'];
              const currentIndex = order.status === 'Cancelled' ? -1 : statusOrder.indexOf(order.status);

              return (
                <>
                  <div className={styles.timeline}>
                    {steps.map((step, index) => {
                      const isCompleted = currentIndex > index;
                      const isCurrent   = currentIndex === index;

                      return (
                        <div
                          key={step.key}
                          className={`${styles.timelineStep} ${isCompleted ? styles.completed : ''}`}
                        >
                          <div className={`${styles.timelineDot} ${isCompleted ? styles.timelineDotCompleted : isCurrent ? styles.timelineDotCurrent : ''}`}>
                            {(isCompleted || isCurrent) && (
                              <svg className={styles.timelineDotIcon} fill="none" stroke="currentColor" viewBox="0 0 24 24">
                                <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={3} d="M5 13l4 4L19 7" />
                              </svg>
                            )}
                          </div>
                          <span className={`${styles.timelineLabel} ${isCompleted ? styles.timelineLabelCompleted : isCurrent ? styles.timelineLabelCurrent : ''}`}>
                            {step.label}
                          </span>
                          {step.date && (isCompleted || isCurrent) && (
                            <span className={styles.timelineDate}>
                              {new Date(step.date).toLocaleDateString()}
                            </span>
                          )}
                        </div>
                      );
                    })}
                  </div>

                  {order.status === 'Cancelled' && (
                    <div className={styles.cancelledBanner}>
                      <svg className={styles.cancelledBannerIcon} fill="none" stroke="currentColor" viewBox="0 0 24 24">
                        <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M6 18L18 6M6 6l12 12" />
                      </svg>
                      This order has been cancelled.
                    </div>
                  )}
                </>
              );
            })()}
          </Card>
```

**Tests:** No backend changes. Manual: check each order status renders the correct
active step. Verify Cancelled shows the red banner.

---

## Phase 5 — Tracking Number Support

**Goal:** Store tracking number on orders. Show it in storefront OrderDetail. Let admin
enter it when marking an order as Shipped.

### [ ] 5.1 — `Order.cs`: add property

After `public DateTime? CancelledAt { get; set; }` add:

```csharp
    public string? TrackingNumber { get; set; }
```

### [ ] 5.2 — Run EF migration

```bash
cd src\backend
dotnet ef migrations add AddTrackingNumberToOrder --project ECommerce.Infrastructure --startup-project ECommerce.API
dotnet ef database update --project ECommerce.Infrastructure --startup-project ECommerce.API
```

### [ ] 5.3 — `OrderDtos.cs`: expose in response

In `OrderDetailDto`, after `public DateTime? DeliveredAt { get; set; }` add:

```csharp
    public string? TrackingNumber { get; set; }
```

In `UpdateOrderStatusDto`, after `public string Status { get; set; } = null!;` add:

```csharp
    /// <summary>
    /// Tracking number. Required when status is set to Shipped.
    /// </summary>
    public string? TrackingNumber { get; set; }
```

### [ ] 5.4 — `IOrderService.cs`: update signature

Change:

```csharp
    Task<OrderDetailDto> UpdateOrderStatusAsync(Guid id, string status, CancellationToken cancellationToken = default);
```

to:

```csharp
    Task<OrderDetailDto> UpdateOrderStatusAsync(Guid id, string status, string? trackingNumber = null, CancellationToken cancellationToken = default);
```

### [ ] 5.5 — `OrderService.cs`: persist tracking number

In `UpdateOrderStatusAsync`, after the line `order.Status = orderStatus;` add:

```csharp
        if (trackingNumber != null)
        {
            order.TrackingNumber = trackingNumber;
        }
```

Change the method signature to match the interface:

```csharp
    public async Task<OrderDetailDto> UpdateOrderStatusAsync(Guid id, string status, string? trackingNumber = null, CancellationToken cancellationToken = default)
```

### [ ] 5.6 — `OrdersController.cs`: pass tracking number through

In `UpdateOrderStatus`, change:

```csharp
            var order = await _orderService.UpdateOrderStatusAsync(id, statusUpdate.Status, cancellationToken: cancellationToken);
```

to:

```csharp
            var order = await _orderService.UpdateOrderStatusAsync(id, statusUpdate.Status, statusUpdate.TrackingNumber, cancellationToken: cancellationToken);
```

### [ ] 5.7 — `storefront/src/store/api/ordersApi.ts`: add field to Order type

In the `Order` interface, after `deliveredAt?: string;` add:

```ts
  trackingNumber?: string;
```

### [ ] 5.8 — `OrderDetail.tsx`: show tracking number

After the **Shipping Address** card, add a new card:

```tsx
          {/* Tracking Number */}
          {order.trackingNumber && (
            <Card variant="elevated" padding="lg">
              <h2 style={{ marginTop: 0 }}>Tracking Information</h2>
              <div style={{ display: 'flex', alignItems: 'center', gap: '12px' }}>
                <svg style={{ width: '24px', height: '24px', color: '#1976d2' }} fill="none" stroke="currentColor" viewBox="0 0 24 24">
                  <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M13 16V6a1 1 0 00-1-1H4a1 1 0 00-1 1v10a1 1 0 001 1h1m8-1a1 1 0 01-1 1H9m4-1V8a1 1 0 011-1h2.586a1 1 0 01.707.293l3.414 3.414a1 1 0 01.293.707V16a1 1 0 01-1 1h-1m-6-1a1 1 0 001 1h1M5 17a2 2 0 104 0m-4 0a2 2 0 114 0m6 0a2 2 0 104 0m-4 0a2 2 0 114 0" />
                </svg>
                <div>
                  <p style={{ margin: 0, fontSize: '0.875rem', color: '#666' }}>Tracking Number</p>
                  <p style={{ margin: '0.25rem 0 0 0', fontWeight: 600, fontSize: '1.05rem', color: '#0f172a' }}>
                    {order.trackingNumber}
                  </p>
                </div>
              </div>
            </Card>
          )}
```

### [ ] 5.9 — Admin `ordersApi.ts`: add `trackingNumber` to mutation

Change the `updateOrderStatus` mutation type and body:

```ts
    updateOrderStatus: builder.mutation<
      Order,
      { orderId: string; status: OrderStatus; trackingNumber?: string }
    >({
      query: ({ orderId, status, trackingNumber }) => ({
        url: `/orders/${orderId}/status`,
        method: 'PUT',
        body: { status, trackingNumber },
      }),
      invalidatesTags: ['Order'],
    }),
```

### [ ] 5.10 — Admin `Orders.tsx`: tracking number input when shipping

Replace `handleStatusChange`:

```tsx
  const [trackingInputs, setTrackingInputs] = useState<Record<string, string>>({});

  const handleStatusChange = async (orderId: string, newStatus: OrderStatus) => {
    if (newStatus === 'shipped' && !trackingInputs[orderId]?.trim()) {
      alert('Please enter a tracking number before marking as Shipped.');
      return;
    }

    try {
      await updateOrderStatus({
        orderId,
        status: newStatus,
        trackingNumber: newStatus === 'shipped' ? trackingInputs[orderId] : undefined,
      }).unwrap();
      setTrackingInputs((prev) => ({ ...prev, [orderId]: '' }));
    } catch {
      alert('Failed to update order status');
    }
  };
```

In the **Actions column** accessor, replace the plain `<select>` with:

```tsx
      accessor: (order: Order) => (
        <div style={{ display: 'flex', flexDirection: 'column', gap: '6px' }}>
          <select
            value={order.status}
            onChange={(e) => handleStatusChange(order.id, e.target.value as OrderStatus)}
            className={styles.statusSelect}
          >
            <option value="pending">Pending</option>
            <option value="processing">Processing</option>
            <option value="shipped">Shipped</option>
            <option value="delivered">Delivered</option>
            <option value="cancelled">Cancelled</option>
          </select>
          {order.status !== 'shipped' && order.status !== 'delivered' && (
            <input
              type="text"
              placeholder="Tracking #"
              value={trackingInputs[order.id] || ''}
              onChange={(e) => setTrackingInputs((prev) => ({ ...prev, [order.id]: e.target.value }))}
              style={{
                padding: '4px 8px',
                fontSize: '0.8rem',
                border: '1px solid #e2e8f0',
                borderRadius: '4px',
                width: '100%',
              }}
            />
          )}
        </div>
      ),
```

**Tests:**
- Unit: update `OrderServiceTests` — add a test calling `UpdateOrderStatusAsync` with a
  tracking number, assert `order.TrackingNumber` is set.
- Integration: existing `UpdateOrderStatus` tests pass; add one with `TrackingNumber` in body,
  verify it appears in the response `OrderDetailDto`.

---

## Phase 6 — Mobile-Responsive Products Sidebar

**Goal:** Products page sidebar (filters) is currently `width: 280px; flexShrink: 0` via
inline style. On mobile it overflows. Move layout to CSS module and toggle sidebar on
screens < 768 px.

### [ ] 6.1 — `Products.module.css`: add layout + mobile styles

Add at the **end** of the file:

```css
/* Page layout */
.layout {
  display: flex;
  gap: 2rem;
  max-width: 1400px;
  margin: 0 auto;
  padding: 0 1rem;
}

.sidebar {
  width: 280px;
  flex-shrink: 0;
}

.mainContent {
  flex: 1;
  min-width: 0;
}

/* Mobile filter toggle button */
.filterToggle {
  display: none;
  align-items: center;
  gap: 8px;
  padding: 10px 18px;
  background-color: #ffffff;
  border: 2px solid #1976d2;
  border-radius: 8px;
  color: #1976d2;
  font-weight: 600;
  font-size: 0.9rem;
  cursor: pointer;
  margin-bottom: 16px;
  width: fit-content;
}

.filterToggle:hover {
  background-color: #eff6ff;
}

.filterToggleIcon {
  width: 20px;
  height: 20px;
}

@media (max-width: 767px) {
  .layout {
    flex-direction: column;
  }

  .sidebar {
    width: 100%;
    display: none;
  }

  .sidebarOpen {
    display: block;
  }

  .filterToggle {
    display: flex;
  }
}
```

### [ ] 6.2 — `Products.tsx`: replace inline layout with CSS classes + toggle

**Add state** at the top of the component:

```tsx
  const [sidebarOpen, setSidebarOpen] = useState(false);
```

**Replace** the outer layout `<div style={{ display: 'flex', ... }}>` and its two children
wrappers with:

```tsx
      {/* Mobile filter toggle */}
      <button
        className={styles.filterToggle}
        onClick={() => setSidebarOpen((prev) => !prev)}
        aria-expanded={sidebarOpen}
      >
        <svg className={styles.filterToggleIcon} fill="none" stroke="currentColor" viewBox="0 0 24 24">
          <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M3 4a1 1 0 011-1h16a1 1 0 011 1v2a1 1 0 01-.293.707L13 13.414V19a1 1 0 01-.553.894l-4 2A1 1 0 017 21v-7.586L3.293 6.707A1 1 0 013 6V4z" />
        </svg>
        {sidebarOpen ? 'Hide Filters' : 'Show Filters'}
      </button>

      <div className={styles.layout}>
        {/* Sidebar */}
        <div className={`${styles.sidebar} ${sidebarOpen ? styles.sidebarOpen : ''}`}>
          {/* ... existing CategoryFilter + Price + Rating + Featured blocks unchanged ... */}
        </div>

        {/* Main Content */}
        <div className={styles.mainContent}>
          {/* ... existing search bar, results, pagination unchanged ... */}
        </div>
      </div>
```

Remove the inline `style` attributes from the two wrapper divs (`style={{ display: 'flex'... }}`
and `style={{ width: '280px'... }}`). The CSS module handles it now.

**Tests:** No backend changes. Manual: resize browser below 768 px, verify sidebar is hidden
and "Show Filters" button appears. Tap button, verify sidebar shows.

---

## Phase 7 — Stub Pages for Dead Footer Links

**Goal:** Footer has 10 links all pointing to `/`. Create real pages for the trust-critical
ones (Returns, FAQ) and lightweight placeholders for the rest. Wire routes.

### [ ] 7.1 — Create `src/frontend/storefront/src/pages/Returns.tsx`

```tsx
import PageHeader from '../components/PageHeader';
import Card from '../components/ui/Card';
import styles from './StaticPage.module.css';

export default function Returns() {
  return (
    <div className={styles.container}>
      <PageHeader title="Return Policy" />
      <Card variant="elevated" padding="lg">
        <div className={styles.body}>
          <h2>30-Day Return Policy</h2>
          <p>We want you to be completely satisfied with your purchase. If you are not happy
          with your order for any reason, you may return most items within <strong>30 days</strong>
          of the delivery date for a full refund.</p>

          <h3>What Can Be Returned</h3>
          <ul>
            <li>Items in original, unopened packaging</li>
            <li>Items that have not been worn, washed, or altered</li>
            <li>Items with all original tags still attached</li>
          </ul>

          <h3>What Cannot Be Returned</h3>
          <ul>
            <li>Final sale or clearance items (marked at checkout)</li>
            <li>Items damaged by customer misuse</li>
            <li>Gift cards</li>
          </ul>

          <h3>How to Initiate a Return</h3>
          <ol>
            <li>Log into your account and go to <strong>Orders</strong></li>
            <li>Select the order containing the item(s) you wish to return</li>
            <li>Click <strong>Request Return</strong> and follow the instructions</li>
            <li>Ship the item back using the provided label</li>
          </ol>

          <h3>Refund Timeline</h3>
          <p>Once we receive and inspect your return, refunds are processed within
          <strong>5–7 business days</strong> to your original payment method.</p>

          <h3>Questions?</h3>
          <p>Contact our support team at <strong>support@eshop.com</strong> or visit the
          <a href="/faq">FAQ page</a>.</p>
        </div>
      </Card>
    </div>
  );
}
```

### [ ] 7.2 — Create `src/frontend/storefront/src/pages/Faq.tsx`

```tsx
import { useState } from 'react';
import PageHeader from '../components/PageHeader';
import Card from '../components/ui/Card';
import styles from './StaticPage.module.css';

const questions = [
  {
    q: 'How do I track my order?',
    a: 'Once your order ships, you will receive an email with a tracking number. You can also view tracking information in your Order Detail page under your account.',
  },
  {
    q: 'Can I change or cancel my order?',
    a: 'Orders can be cancelled while in Pending or Processing status. Once an order is Shipped it can no longer be cancelled. To cancel, go to Orders → select the order → click Cancel Order.',
  },
  {
    q: 'What payment methods do you accept?',
    a: 'We accept Credit/Debit Cards, PayPal, Apple Pay, and Google Pay.',
  },
  {
    q: 'Do you offer free shipping?',
    a: 'Yes! Orders over $100 qualify for free standard shipping.',
  },
  {
    q: 'How do I return an item?',
    a: 'See our Return Policy page for full details. In short: most items can be returned within 30 days in original condition.',
  },
  {
    q: 'How do I reset my password?',
    a: 'Click "Sign In" → "Forgot Password" → enter your email. You will receive a password reset link.',
  },
  {
    q: 'Is my payment information secure?',
    a: 'Yes. All transactions are processed over HTTPS with SSL encryption. We never store raw card numbers.',
  },
  {
    q: 'Can I shop without creating an account?',
    a: 'Yes. Guest checkout is supported — simply provide your email at checkout.',
  },
];

export default function Faq() {
  const [openIndex, setOpenIndex] = useState<number | null>(null);

  return (
    <div className={styles.container}>
      <PageHeader title="Frequently Asked Questions" />
      <Card variant="elevated" padding="lg">
        <div className={styles.body}>
          {questions.map((item, index) => (
            <div key={index} className={styles.accordionItem}>
              <button
                type="button"
                className={styles.accordionButton}
                onClick={() => setOpenIndex(openIndex === index ? null : index)}
                aria-expanded={openIndex === index}
              >
                <span>{item.q}</span>
                <svg style={{ width: '20px', height: '20px', flexShrink: 0, transition: 'transform 0.2s', transform: openIndex === index ? 'rotate(180deg)' : 'rotate(0deg)' }} fill="none" stroke="currentColor" viewBox="0 0 24 24">
                  <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M19 9l-7 7-7-7" />
                </svg>
              </button>
              {openIndex === index && (
                <p className={styles.accordionAnswer}>{item.a}</p>
              )}
            </div>
          ))}
        </div>
      </Card>
    </div>
  );
}
```

### [ ] 7.3 — Create lightweight placeholder pages

Each follows the same pattern. Create these 5 files:

- `About.tsx`
- `Contact.tsx`
- `Privacy.tsx`
- `Terms.tsx`
- `TrackOrder.tsx`

Template for each (replace `TITLE` and `DESCRIPTION`):

```tsx
import PageHeader from '../components/PageHeader';
import Card from '../components/ui/Card';
import styles from './StaticPage.module.css';

export default function PAGENAME() {
  return (
    <div className={styles.container}>
      <PageHeader title="TITLE" />
      <Card variant="elevated" padding="lg">
        <div className={styles.body}>
          <p style={{ color: '#475569' }}>DESCRIPTION</p>
        </div>
      </Card>
    </div>
  );
}
```

Suggested content per page:

| File | title | Description placeholder |
|------|-------|-------------------------|
| `About.tsx` | About Us | "Our story and mission coming soon." |
| `Contact.tsx` | Contact Us | "Email us at support@eshop.com. We reply within 24 hours." |
| `Privacy.tsx` | Privacy Policy | "Full privacy policy coming soon. We never sell your data." |
| `Terms.tsx` | Terms of Service | "Full terms coming soon. By using this site you agree to our policies." |
| `TrackOrder.tsx` | Track Order | "Log in and visit Orders to see your tracking information." |

### [ ] 7.4 — Create `StaticPage.module.css`

```css
.container {
  min-height: 100vh;
  background-color: #f8fafc;
  padding: 48px 0;
}

.body {
  max-width: 720px;
  margin: 0 auto;
  color: #0f172a;
  line-height: 1.7;
}

.body h2 {
  font-size: 1.375rem;
  font-weight: 700;
  color: #0f172a;
  margin-top: 24px;
  margin-bottom: 8px;
}

.body h3 {
  font-size: 1.1rem;
  font-weight: 600;
  color: #0f172a;
  margin-top: 20px;
  margin-bottom: 8px;
}

.body p {
  margin: 0 0 16px;
  color: #475569;
}

.body ul,
.body ol {
  margin: 0 0 16px;
  padding-left: 24px;
  color: #475569;
}

.body li {
  margin-bottom: 6px;
}

.body a {
  color: #1976d2;
  text-decoration: none;
}

.body a:hover {
  text-decoration: underline;
}

/* FAQ Accordion */
.accordionItem {
  border-bottom: 1px solid #e2e8f0;
}

.accordionButton {
  width: 100%;
  display: flex;
  justify-content: space-between;
  align-items: center;
  gap: 12px;
  padding: 16px 0;
  background: none;
  border: none;
  text-align: left;
  font-size: 1rem;
  font-weight: 600;
  color: #0f172a;
  cursor: pointer;
}

.accordionButton:hover {
  color: #1976d2;
}

.accordionAnswer {
  padding: 0 0 16px;
  margin: 0;
  color: #475569;
  font-size: 0.9375rem;
  line-height: 1.6;
}
```

### [ ] 7.5 — `App.tsx` (storefront): add routes

Add imports:

```tsx
import About from './pages/About';
import Contact from './pages/Contact';
import Returns from './pages/Returns';
import Faq from './pages/Faq';
import Privacy from './pages/Privacy';
import Terms from './pages/Terms';
import TrackOrder from './pages/TrackOrder';
```

Add routes inside `<Routes>` (after the existing `/reset-password` route):

```tsx
          <Route path="/about" element={<About />} />
          <Route path="/contact" element={<Contact />} />
          <Route path="/returns" element={<Returns />} />
          <Route path="/faq" element={<Faq />} />
          <Route path="/privacy" element={<Privacy />} />
          <Route path="/terms" element={<Terms />} />
          <Route path="/track-order" element={<TrackOrder />} />
```

### [ ] 7.6 — `Footer.tsx`: wire links to real routes

Replace the Support and Legal sections:

```tsx
          {/* Support */}
          <div className={styles.section}>
            <h3>Support</h3>
            <ul className={styles.links}>
              <li><Link to="/faq">Help Center</Link></li>
              <li><Link to="/contact">Contact Us</Link></li>
              <li><Link to="/track-order">Track Order</Link></li>
              <li><Link to="/returns">Returns</Link></li>
            </ul>
          </div>

          {/* Legal */}
          <div className={styles.section}>
            <h3>Legal</h3>
            <ul className={styles.links}>
              <li><Link to="/privacy">Privacy Policy</Link></li>
              <li><Link to="/terms">Terms of Service</Link></li>
              <li><Link to="/">Cookies</Link></li>
              <li><Link to="/">Security</Link></li>
            </ul>
          </div>
```

Also wire the Company section:

```tsx
          {/* Company */}
          <div className={styles.section}>
            <h3>Company</h3>
            <ul className={styles.links}>
              <li><Link to="/about">About Us</Link></li>
              <li><Link to="/">Careers</Link></li>
              <li><Link to="/">Press</Link></li>
              <li><Link to="/">Blog</Link></li>
            </ul>
          </div>
```

**Tests:** No backend changes. Manual: click every footer link, verify no link goes to home
page unexpectedly. Verify Returns and FAQ have real content.

---

## Phase 8 — Newsletter Subscription

**Goal:** Wire the footer newsletter form to an actual backend endpoint instead of a no-op.

> **Note:** This phase requires a new backend endpoint. A minimal approach is used —
> no new entity or migration needed yet. The endpoint logs the subscription and can be
> extended later to persist to a database table.

### [ ] 8.1 — `NewsletterController.cs` — create new controller

File: `src/backend/ECommerce.API/Controllers/NewsletterController.cs`

```csharp
using Microsoft.AspNetCore.Mvc;
using ECommerce.Application.DTOs.Common;
using Microsoft.Extensions.Logging;

namespace ECommerce.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class NewsletterController : ControllerBase
{
    private readonly ILogger<NewsletterController> _logger;

    public NewsletterController(ILogger<NewsletterController> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Subscribe an email address to the newsletter.
    /// </summary>
    /// <param name="request">Email address to subscribe.</param>
    /// <returns>Subscription confirmation.</returns>
    [HttpPost("subscribe")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    public IActionResult Subscribe([FromBody] NewsletterSubscribeDto request)
    {
        if (string.IsNullOrWhiteSpace(request.Email) || !request.Email.Contains('@'))
        {
            return BadRequest(ApiResponse<object>.Error("A valid email address is required."));
        }

        _logger.LogInformation("Newsletter subscription request: {Email}", request.Email);

        // TODO: Persist to database when NewsletterSubscription entity is added.
        // For now, the subscription is logged. Extend this method to save to DB.

        return Ok(ApiResponse<object>.Ok(new object(), "Thank you for subscribing!"));
    }
}
```

### [ ] 8.2 — Create `NewsletterSubscribeDto`

File: `src/backend/ECommerce.Application/DTOs/Common/NewsletterSubscribeDto.cs`

```csharp
namespace ECommerce.Application.DTOs.Common;

public class NewsletterSubscribeDto
{
    public string Email { get; set; } = null!;
}
```

### [ ] 8.3 — `Footer.tsx`: wire the subscribe form

Replace the newsletter form:

```tsx
          {/* Newsletter */}
          <div className={styles.newsletter}>
            <h3>Newsletter</h3>
            <p className={styles.newsletterText}>Subscribe to get special offers and updates</p>
            <form className={styles.form} onSubmit={async (e) => {
              e.preventDefault();
              const emailInput = e.currentTarget.querySelector('input[type="email"]') as HTMLInputElement;
              const email = emailInput?.value?.trim();
              if (!email) return;

              try {
                const API_URL = import.meta.env.VITE_API_URL || 'http://localhost:5000/api';
                await fetch(`${API_URL}/newsletter/subscribe`, {
                  method: 'POST',
                  headers: { 'Content-Type': 'application/json' },
                  body: JSON.stringify({ email }),
                });
                emailInput.value = '';
                emailInput.placeholder = 'Thank you!';
                setTimeout(() => { emailInput.placeholder = 'Your email'; }, 3000);
              } catch {
                emailInput.placeholder = 'Try again later';
                setTimeout(() => { emailInput.placeholder = 'Your email'; }, 3000);
              }
            }}>
              <input
                type="email"
                placeholder="Your email"
                className={styles.emailInput}
                required
              />
              <button type="submit" className={styles.subscribeButton}>
                Subscribe
              </button>
            </form>
          </div>
```

**Tests:** Manual: enter a valid email in footer, click Subscribe, verify placeholder
changes to "Thank you!". Enter invalid email, verify browser validation blocks submission.

---

## Execution Order & Build Checkpoints

| After completing | Action |
|-----------------|--------|
| Phase 1 | Visual check only — no build impact |
| Phase 2 | `npm run build` in storefront — verify no TS errors from new `paymentsApi` |
| Phase 3 | Visual + manual checkout test |
| Phase 4 | Visual check only |
| Phase 5 | **`dotnet build`** in backend — migration + DTO + service changes. Run `dotnet test` |
| Phase 6 | Visual check at mobile viewport |
| Phase 7 | Visual check all footer links |
| Phase 8 | **`dotnet build`** — new controller. Manual test |

---

## Files Changed Summary

### Backend
| File | Change |
|------|--------|
| `Core/Entities/Order.cs` | Add `TrackingNumber` property |
| `Application/DTOs/Orders/OrderDtos.cs` | Add `TrackingNumber` to `OrderDetailDto` and `UpdateOrderStatusDto` |
| `Application/DTOs/Common/NewsletterSubscribeDto.cs` | **New file** |
| `Application/Interfaces/IOrderService.cs` | Add `trackingNumber` param to `UpdateOrderStatusAsync` |
| `Application/Services/OrderService.cs` | Persist `TrackingNumber` in `UpdateOrderStatusAsync` |
| `API/Controllers/OrdersController.cs` | Pass `TrackingNumber` from DTO to service |
| `API/Controllers/NewsletterController.cs` | **New file** |
| New migration | `AddTrackingNumberToOrder` |

### Storefront
| File | Change |
|------|--------|
| `pages/Checkout.tsx` | Trust bar, payment selector, guest email |
| `pages/Checkout.module.css` | Trust + payment styles |
| `pages/ProductDetail.tsx` | Trust strip |
| `pages/ProductDetail.module.css` | Trust strip styles |
| `pages/OrderDetail.tsx` | Status timeline, tracking number card |
| `pages/OrderDetail.module.css` | Timeline styles |
| `pages/Products.tsx` | Mobile sidebar toggle |
| `pages/Products.module.css` | Layout + mobile styles |
| `pages/StaticPage.module.css` | **New file** — shared styles for static pages |
| `pages/Returns.tsx` | **New file** |
| `pages/Faq.tsx` | **New file** |
| `pages/About.tsx` | **New file** |
| `pages/Contact.tsx` | **New file** |
| `pages/Privacy.tsx` | **New file** |
| `pages/Terms.tsx` | **New file** |
| `pages/TrackOrder.tsx` | **New file** |
| `components/Footer.tsx` | Wire all links + newsletter form |
| `store/api/ordersApi.ts` | Add `guestEmail` to request, `trackingNumber` to Order type |
| `store/api/paymentsApi.ts` | **New file** |
| Store config | Register `paymentsApi` reducer + middleware |
| `App.tsx` | 7 new routes |

### Admin
| File | Change |
|------|--------|
| `store/api/ordersApi.ts` | Add `trackingNumber` to `updateOrderStatus` mutation |
| `pages/Orders.tsx` | Tracking number input + guarded status change |

---

## Troubleshooting

| Symptom | Likely cause | Fix |
|---------|-------------|-----|
| `paymentsApi` not found | Not registered in store | Add reducer + middleware in store config |
| Migration fails | `Order.cs` change not saved | Verify `TrackingNumber` property is in the file before running `dotnet ef` |
| Footer links still go to `/` | Forgot to update `Footer.tsx` | Re-check the `<Link to=` values |
| Sidebar still visible on mobile | CSS not applied | Verify class names match exactly (`styles.sidebar`, `styles.sidebarOpen`) |
| Newsletter returns 404 | Controller not discovered | Verify file is in `ECommerce.API/Controllers/` and class is `public` |
| Timeline dots all grey | `currentIndex` is `-1` | Only happens for Cancelled — that's correct, the red banner shows instead |
