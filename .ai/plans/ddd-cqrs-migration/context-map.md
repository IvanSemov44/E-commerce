# Bounded Context Map

**This document identifies every bounded context in our e-commerce system, what belongs in each, and how they communicate.**

---

## What is a Context Map?

A Context Map shows all bounded contexts and their relationships. It answers:
- What contexts exist?
- Which entities/concepts belong to which context?
- How do contexts talk to each other?
- Who depends on whom?

---

## Our Bounded Contexts

### 1. Catalog Context

**Purpose**: Managing the product catalog — what products exist, their descriptions, prices, images, and categories.

**Ubiquitous Language**: Product, Category, Image, Slug, SKU, Featured, Active/Inactive

**Aggregates**:
| Aggregate Root | Child Entities | Value Objects |
|---------------|----------------|---------------|
| **Product** | ProductImage | ProductName, Slug, Money (Price, CompareAtPrice, CostPrice), Sku, Barcode, Weight |
| **Category** | — | CategoryName, Slug |

**Current Entities → Context Mapping**:
| Current Entity | Becomes | Notes |
|---------------|---------|-------|
| `Product` | `Product` aggregate root | Gains domain methods: `UpdatePrice()`, `AddImage()`, `Activate()`, `Deactivate()` |
| `ProductImage` | Child entity of Product | Accessed only through Product, not independently |
| `Category` | `Category` aggregate root | Gains: `Rename()`, `MoveTo(parentId)`, `Activate()` |

**Key Invariants** (business rules the aggregate enforces):
- Product price must be positive
- Product slug must be unique (validated via repository, enforced at DB level)
- Product must have a category
- Product can have max N images
- First image added is automatically primary
- Only one image can be primary at a time
- Category slug must be unique
- Category cannot be its own parent (no circular hierarchy)

**Domain Events**:
- `ProductCreatedEvent` — for search indexing, analytics
- `ProductPriceChangedEvent` — for price-watch notifications
- `ProductDeactivatedEvent` — for removing from cart/wishlist
- `CategoryCreatedEvent`

---

### 2. Identity Context

**Purpose**: User accounts, authentication, authorization. Who the user is.

**Ubiquitous Language**: User, Account, Email, Password, Role, Verification, Address

**Aggregates**:
| Aggregate Root | Child Entities | Value Objects |
|---------------|----------------|---------------|
| **User** | Address, RefreshToken | Email, PersonName, PhoneNumber, Password (hash) |

**Current Entities → Context Mapping**:
| Current Entity | Becomes | Notes |
|---------------|---------|-------|
| `User` | `User` aggregate root | Gains: `ChangeEmail()`, `VerifyEmail()`, `AddAddress()`, `SetDefaultAddress()` |
| `Address` | Child entity of User | Accessed through User. Value-object-like but has identity (user has many). |
| `RefreshToken` | Child entity of User | Token lifecycle managed by User aggregate |

**Key Invariants**:
- Email must be unique (cross-aggregate, validated via repository)
- Email must be valid format (value object enforces)
- Password must meet strength policy
- Maximum N addresses per user
- Exactly one default shipping address and one default billing address
- Refresh tokens: max active tokens, old ones auto-revoked

**Domain Events**:
- `UserRegisteredEvent` — triggers welcome email
- `EmailVerifiedEvent`
- `PasswordResetRequestedEvent` — triggers reset email

**Note on Auth**: Authentication logic (JWT generation, token validation) is an **Application concern**, not a domain concern. The domain knows about Users and their credentials. The Application layer orchestrates the auth flow.

---

### 3. Inventory Context

**Purpose**: Managing stock levels, tracking stock changes, alerting on low stock.

**Ubiquitous Language**: Stock, Quantity, Restock, Adjustment, Low Stock, Threshold

**Aggregates**:
| Aggregate Root | Child Entities | Value Objects |
|---------------|----------------|---------------|
| **InventoryItem** | InventoryLog (history) | StockLevel, Quantity |

**Current Entities → Context Mapping**:
| Current Entity | Becomes | Notes |
|---------------|---------|-------|
| `Product.StockQuantity` + `Product.LowStockThreshold` | `InventoryItem` aggregate root | **Separated from Product!** Inventory is its own context. References ProductId. |
| `InventoryLog` | Child entity of InventoryItem | Stock change history within the aggregate |

**Why separate from Catalog?** In the Catalog context, a Product is about *description and pricing*. In Inventory, a Product is about *stock levels*. These change independently and for different reasons. A marketing manager updates product descriptions; a warehouse worker manages stock. Different people, different rules, different aggregate.

**Key Invariants**:
- Stock cannot go negative (unless explicitly allowed for backorders)
- Every stock change must have a reason and reference
- Low stock threshold must be non-negative

**Domain Events**:
- `StockReducedEvent(ProductId, NewQuantity)` — general notification
- `LowStockDetectedEvent(ProductId, CurrentStock, Threshold)` — triggers alert email
- `StockReplenishedEvent(ProductId, AddedQuantity, NewQuantity)`

**Cross-Context Communication**:
- Listens to: `OrderPlacedEvent` (from Ordering) → reduce stock
- Publishes: `LowStockDetectedEvent` → handled by Notifications

---

### 4. Shopping Context

**Purpose**: Managing the shopping experience — carts and wishlists.

**Ubiquitous Language**: Cart, Cart Item, Wishlist, Add to Cart, Remove, Clear

**Aggregates**:
| Aggregate Root | Child Entities | Value Objects |
|---------------|----------------|---------------|
| **Cart** | CartItem | Quantity |
| **Wishlist** | WishlistItem (just ProductId) | — |

**Current Entities → Context Mapping**:
| Current Entity | Becomes | Notes |
|---------------|---------|-------|
| `Cart` | `Cart` aggregate root | Gains: `AddItem()`, `RemoveItem()`, `UpdateQuantity()`, `Clear()` |
| `CartItem` | Child entity of Cart | No independent access |
| `Wishlist` (entity per product) | `Wishlist` aggregate root with items | One Wishlist per user, items as children |

**Key Invariants**:
- No duplicate products in cart (adding existing product increases quantity)
- Quantity must be positive
- Cart has maximum item count (configurable)
- No duplicate products in wishlist

**Domain Events**:
- `ItemAddedToCartEvent` — for analytics
- `CartClearedEvent`

**Cross-Context Communication**:
- Needs: Product existence and price from Catalog (validates via ID lookup)
- Listens to: `OrderPlacedEvent` → clear cart
- Listens to: `ProductDeactivatedEvent` → remove from cart/wishlist

---

### 5. Promotions Context

**Purpose**: Managing promotional codes and discount calculations.

**Ubiquitous Language**: Promo Code, Discount, Percentage, Fixed Amount, Usage Limit, Valid Period

**Aggregates**:
| Aggregate Root | Child Entities | Value Objects |
|---------------|----------------|---------------|
| **PromoCode** | — | DiscountValue, DateRange, PromoCodeString |

**Current Entities → Context Mapping**:
| Current Entity | Becomes | Notes |
|---------------|---------|-------|
| `PromoCode` | `PromoCode` aggregate root | Gains: `Apply()`, `IsValid()`, `IncrementUsage()` |

**Key Invariants**:
- Code must be unique
- Discount percentage must be 0-100
- Fixed discount must be positive
- StartDate must be before EndDate
- UsedCount cannot exceed MaxUses
- Cannot apply expired or inactive codes

**Domain Services**:
- `DiscountCalculator` — calculates discount amount given a promo code and subtotal (cross-concern: needs order data)

**Domain Events**:
- `PromoCodeAppliedEvent(Code, OrderId, DiscountAmount)`
- `PromoCodeExhaustedEvent(Code)` — when MaxUses reached

---

### 6. Reviews Context

**Purpose**: Product reviews and ratings.

**Ubiquitous Language**: Review, Rating, Verified Purchase, Approval, Edit Window

**Aggregates**:
| Aggregate Root | Child Entities | Value Objects |
|---------------|----------------|---------------|
| **Review** | — | Rating, ReviewContent |

**Current Entities → Context Mapping**:
| Current Entity | Becomes | Notes |
|---------------|---------|-------|
| `Review` | `Review` aggregate root | Gains: `Edit()`, `Approve()`, `MarkVerified()` |

**Key Invariants**:
- Rating must be 1-5
- One review per user per product (enforced at repo + domain level)
- Can only edit within 30 days of creation
- Review content cannot be empty

**Cross-Context Communication**:
- References: ProductId (from Catalog), UserId (from Identity), OrderId (from Ordering) — all by ID only
- Listens to: `OrderDeliveredEvent` → mark review as verified purchase

---

### 7. Ordering Context (Most Complex)

**Purpose**: The order lifecycle — placing orders, status transitions, cancellations.

**Ubiquitous Language**: Order, Order Item, Place, Confirm, Ship, Deliver, Cancel, Payment, Subtotal, Tax, Shipping

**Aggregates**:
| Aggregate Root | Child Entities | Value Objects |
|---------------|----------------|---------------|
| **Order** | OrderItem | OrderNumber, Money (amounts), OrderStatus (state machine), PaymentInfo, ShippingAddress (snapshot), BillingAddress (snapshot) |

**Current Entities → Context Mapping**:
| Current Entity | Becomes | Notes |
|---------------|---------|-------|
| `Order` | `Order` aggregate root | Gains: `Place()`, `Confirm()`, `Ship()`, `Deliver()`, `Cancel()` — state machine |
| `OrderItem` | Child entity of Order | Immutable snapshot of product at order time |

**Key Invariants (State Machine)**:
```
Pending → Confirmed → Processing → Shipped → Delivered
   ↓          ↓           ↓
Cancelled  Cancelled   Cancelled (partial refund?)
```
- Cannot skip states (Pending → Shipped is invalid)
- Cannot cancel after Shipped
- Cannot modify items after Confirmed
- Total must equal sum of items + shipping + tax - discount
- Order must have at least one item

**Why Ordering is last**: It touches almost every other context. It needs:
- Product info from Catalog (snapshot at order time)
- User/Address info from Identity
- Stock reduction from Inventory
- Promo code validation from Promotions
- Cart clearing from Shopping
- Email from Notifications

**Domain Events**:
- `OrderPlacedEvent` — the big one. Triggers: inventory reduction, cart clearing, email, analytics
- `OrderConfirmedEvent`
- `OrderShippedEvent(TrackingNumber)`
- `OrderDeliveredEvent`
- `OrderCancelledEvent(Reason)` — triggers: stock restoration, potential refund

---

## Context Relationship Map

```
                    ┌──────────────┐
                    │   Catalog    │
                    │  (Product,   │
                    │   Category)  │
                    └──────┬───────┘
                           │ ProductId referenced by ↓
          ┌────────────────┼────────────────┬──────────────┐
          │                │                │              │
   ┌──────┴──────┐  ┌─────┴──────┐  ┌─────┴─────┐  ┌────┴─────┐
   │  Inventory  │  │  Shopping   │  │  Reviews   │  │ Ordering │
   │  (Stock)    │  │ (Cart,     │  │ (Review)   │  │ (Order)  │
   │             │  │  Wishlist)  │  │            │  │          │
   └─────────────┘  └────────────┘  └────────────┘  └────┬─────┘
                                                          │
                                              ┌───────────┼──────────┐
                                              │           │          │
                                       ┌──────┴──┐ ┌─────┴────┐ ┌──┴────────┐
                                       │Identity │ │Promotions│ │Inventory  │
                                       │(User,   │ │(Promo    │ │(reduce    │
                                       │Address) │ │Code)     │ │stock)     │
                                       └─────────┘ └──────────┘ └───────────┘
```

**Relationship Types**:

| Upstream | Downstream | Type | Communication |
|----------|------------|------|---------------|
| Catalog | Inventory | Customer-Supplier | Inventory references ProductId |
| Catalog | Shopping | Customer-Supplier | Cart references ProductId + price lookup |
| Catalog | Reviews | Customer-Supplier | Review references ProductId |
| Catalog | Ordering | Customer-Supplier | OrderItem snapshots product data |
| Identity | Ordering | Customer-Supplier | Order references UserId, address snapshot |
| Identity | Reviews | Customer-Supplier | Review references UserId |
| Identity | Shopping | Customer-Supplier | Cart/Wishlist references UserId |
| Promotions | Ordering | Customer-Supplier | Order applies promo code |
| Ordering | Inventory | Event-driven | `OrderPlacedEvent` → reduce stock |
| Ordering | Shopping | Event-driven | `OrderPlacedEvent` → clear cart |
| Ordering | Reviews | Event-driven | `OrderDeliveredEvent` → mark verified |
| Inventory | Notifications | Event-driven | `LowStockDetectedEvent` → send alert |
| Identity | Notifications | Event-driven | `UserRegisteredEvent` → welcome email |

**Shared Kernel**: `ECommerce.SharedKernel` — base classes (Entity, AggregateRoot, ValueObject, IDomainEvent, Result<T>) shared by all contexts.

---

## Migration Impact Summary

| Context | Complexity | New Projects | Entities Moved | Services Replaced |
|---------|-----------|--------------|----------------|-------------------|
| Foundation | Low | 1 (SharedKernel) | 0 | 0 |
| Catalog | Medium | 3 (Domain, App, Infra) | 3 (Product, ProductImage, Category) | 2 (ProductService, CategoryService) |
| Identity | Medium | 3 | 3 (User, Address, RefreshToken) | 3 (AuthService, UserService, CurrentUserService) |
| Inventory | Medium | 3 | 1→2 (InventoryLog, new InventoryItem) | 1 (InventoryService) |
| Shopping | Medium | 3 | 3 (Cart, CartItem, Wishlist) | 2 (CartService, WishlistService) |
| Promotions | Low | 3 | 1 (PromoCode) | 1 (PromoCodeService) |
| Reviews | Low | 3 | 1 (Review) | 1 (ReviewService) |
| Ordering | High | 3 | 2 (Order, OrderItem) | 1 (OrderService) |
| **Total** | — | **22 projects** | **16 entities** | **11 services** |
