# Database Schema

## Entity Relationship Diagram

```mermaid
erDiagram
    Users {
        uuid Id PK
        varchar Email UK
        varchar PasswordHash
        varchar FirstName
        varchar LastName
        varchar Phone
        int Role
        bool IsEmailVerified
        varchar EmailVerificationToken
        varchar PasswordResetToken
        datetime PasswordResetExpires
        varchar GoogleId
        varchar AvatarUrl
        bytea RowVersion
        datetime CreatedAt
        datetime UpdatedAt
    }

    Addresses {
        uuid Id PK
        uuid UserId FK
        varchar Type
        varchar FirstName
        varchar LastName
        varchar StreetLine1
        varchar StreetLine2
        varchar City
        varchar State
        varchar PostalCode
        varchar Country
        bool IsDefault
        datetime CreatedAt
        datetime UpdatedAt
    }

    RefreshTokens {
        uuid Id PK
        uuid UserId FK
        varchar Token UK
        datetime ExpiresAt
        bool IsRevoked
        datetime CreatedAt
        datetime UpdatedAt
    }

    Categories {
        uuid Id PK
        varchar Name
        varchar Slug UK
        varchar Description
        uuid ParentId FK
        bool IsActive
        int SortOrder
        datetime CreatedAt
        datetime UpdatedAt
    }

    Products {
        uuid Id PK
        varchar Name
        varchar Slug UK
        varchar Description
        decimal Price
        decimal CompareAtPrice
        decimal CostPrice
        varchar Sku
        uuid CategoryId FK
        int StockQuantity
        int LowStockThreshold
        bool IsActive
        bool IsFeatured
        bytea RowVersion
        datetime CreatedAt
        datetime UpdatedAt
    }

    ProductImages {
        uuid Id PK
        uuid ProductId FK
        varchar Url
        varchar AltText
        bool IsPrimary
        int SortOrder
        datetime CreatedAt
        datetime UpdatedAt
    }

    Carts {
        uuid Id PK
        uuid UserId FK "unique"
        varchar SessionId
        bytea RowVersion
        datetime CreatedAt
        datetime UpdatedAt
    }

    CartItems {
        uuid Id PK
        uuid CartId FK
        uuid ProductId FK
        int Quantity
        datetime CreatedAt
        datetime UpdatedAt
    }

    Wishlists {
        uuid Id PK
        uuid UserId FK
        uuid ProductId FK
        datetime CreatedAt
        datetime UpdatedAt
    }

    PromoCodes {
        uuid Id PK
        varchar Code UK
        int DiscountType
        decimal DiscountValue
        decimal MinOrderAmount
        decimal MaxDiscountAmount
        int MaxUses
        int UsedCount
        datetime StartDate
        datetime EndDate
        bool IsActive
        bytea RowVersion
        datetime CreatedAt
        datetime UpdatedAt
    }

    Orders {
        uuid Id PK
        varchar OrderNumber UK
        uuid UserId FK
        varchar GuestEmail
        int Status
        int PaymentStatus
        varchar PaymentMethod
        varchar PaymentIntentId
        decimal Subtotal
        decimal DiscountAmount
        decimal ShippingAmount
        decimal TaxAmount
        decimal TotalAmount
        uuid ShippingAddressId FK
        uuid BillingAddressId FK
        uuid PromoCodeId FK
        varchar TrackingNumber
        datetime ShippedAt
        datetime DeliveredAt
        datetime CancelledAt
        bytea RowVersion
        datetime CreatedAt
        datetime UpdatedAt
    }

    OrderItems {
        uuid Id PK
        uuid OrderId FK
        uuid ProductId FK
        varchar ProductName
        varchar ProductSku
        int Quantity
        decimal UnitPrice
        decimal TotalPrice
        datetime CreatedAt
        datetime UpdatedAt
    }

    Reviews {
        uuid Id PK
        uuid ProductId FK
        uuid UserId FK
        uuid OrderId FK
        int Rating
        varchar Title
        varchar Comment
        bool IsVerified
        bool IsApproved
        datetime CreatedAt
        datetime UpdatedAt
    }

    InventoryLogs {
        uuid Id PK
        uuid ProductId FK
        int QuantityChange
        varchar Reason
        uuid ReferenceId
        varchar Notes
        uuid CreatedByUserId FK
        datetime CreatedAt
        datetime UpdatedAt
    }

    Users ||--o{ Addresses : "has"
    Users ||--o{ RefreshTokens : "has"
    Users ||--o| Carts : "has"
    Users ||--o{ Wishlists : "saves"
    Users ||--o{ Orders : "places"
    Users ||--o{ Reviews : "writes"
    Users ||--o{ InventoryLogs : "created by"

    Categories ||--o{ Categories : "parent of"
    Categories ||--o{ Products : "contains"

    Products ||--o{ ProductImages : "has"
    Products ||--o{ CartItems : "in"
    Products ||--o{ Wishlists : "in"
    Products ||--o{ OrderItems : "sold as"
    Products ||--o{ Reviews : "receives"
    Products ||--o{ InventoryLogs : "tracked by"

    Carts ||--o{ CartItems : "contains"
    Orders ||--o{ OrderItems : "contains"
    Orders }o--|| Addresses : "ships to"
    Orders }o--|| Addresses : "billed to"
    Orders }o--o| PromoCodes : "uses"
    Orders ||--o{ Reviews : "reviewed via"
```

---

## Concurrency Control

Five tables use **optimistic locking** via a `RowVersion` column (PostgreSQL `bytea`). EF Core throws a `DbUpdateConcurrencyException` if two transactions modify the same row simultaneously.

| Table | Why |
|-------|-----|
| `Users` | Profile updates from multiple sessions |
| `Products` | Stock adjustments from concurrent orders |
| `Carts` | Simultaneous add-to-cart from multiple tabs |
| `Orders` | Status updates racing with cancellations |
| `PromoCodes` | `UsedCount` increment under high load |

---

## Delete Behaviors

| Relationship | Behavior | Effect |
|---|---|---|
| User → Addresses | Cascade | Deleting a user removes all addresses |
| User → RefreshTokens | Cascade | Deleting a user revokes all sessions |
| User → Cart | Cascade | Deleting a user removes the cart |
| User → Wishlists | Cascade | Deleting a user clears the wishlist |
| Product → ProductImages | Cascade | Deleting a product removes images |
| Product → CartItems | Cascade | Item removed from all carts |
| Cart → CartItems | Cascade | Clearing a cart removes all items |
| Order → OrderItems | Cascade | Deleting an order removes its lines |
| Order → User | **SetNull** | User deletion keeps order history (audit) |
| Order → PromoCode | **SetNull** | PromoCode deletion keeps order history |
| OrderItem → Product | **SetNull** | Product deletion keeps order history |
| Review → User | **SetNull** | User deletion keeps product reviews |
| Category → Product | **SetNull** | Deleting a category orphans products |
| Category → Category (parent) | **SetNull** | Deleting parent makes subcategory top-level |

---

## Key Indexes

| Table | Index | Purpose |
|-------|-------|---------|
| Users | `Email` (unique) | Login lookup |
| Categories | `Slug` (unique) | URL routing |
| Products | `Slug` (unique) | URL routing |
| Products | `IsActive`, `IsFeatured` | Catalog filtering |
| Products | `(IsActive, Price)` composite | Filtered + sorted browsing |
| Carts | `SessionId` | Guest cart lookup |
| CartItems | `(CartId, ProductId)` unique | Prevent duplicate cart lines |
| Wishlists | `(UserId, ProductId)` unique | Prevent duplicate wishlist entries |
| Orders | `OrderNumber` (unique) | Customer-facing order lookup |
| Orders | `UserId`, `Status`, `CreatedAt` | Order history queries |
| RefreshTokens | `Token` (unique) | Token validation |
| InventoryLogs | `ProductId`, `(ProductId, CreatedAt)` | Stock history queries |
