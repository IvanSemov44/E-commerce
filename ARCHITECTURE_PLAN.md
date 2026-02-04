# E-Commerce Platform Architecture Plan

## Executive Summary

This document outlines the architecture for a B2C e-commerce platform built with **React**, **Redux Toolkit (RTK Query)**, **ASP.NET Core 8**, and **PostgreSQL**. The platform follows Clean Architecture on the backend with a feature-complete storefront and admin dashboard. Designed for small-scale operations (<1K daily users), hosted on Render.com.

---

## Table of Contents

1. [System Overview](#1-system-overview)
2. [Technology Stack](#2-technology-stack)
3. [Project Structure](#3-project-structure)
4. [Database Design](#4-database-design)
5. [Backend Architecture](#5-backend-architecture)
6. [Frontend Architecture](#6-frontend-architecture)
7. [Authentication & Authorization](#7-authentication--authorization)
8. [Payment Integration](#8-payment-integration)
9. [Admin Panel](#9-admin-panel)
10. [Email & Notifications](#10-email--notifications)
11. [Deployment Strategy](#11-deployment-strategy)
12. [Security Considerations](#12-security-considerations)

---

## 1. System Overview

### 1.1 High-Level Architecture

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                              CLIENTS                                         │
├─────────────────────┬───────────────────────────────────────────────────────┤
│   Customer Web App  │    Admin Dashboard                                     │
│   (React + Redux)   │    (React + Redux)                                     │
└─────────┬───────────┴───────────┬───────────────────────────────────────────┘
          │                       │
          ▼                       ▼
┌─────────────────────────────────────────────────────────────────────────────┐
│                         RENDER.COM CDN                                       │
│                    (Static Asset Delivery)                                   │
└─────────────────────────────────┬───────────────────────────────────────────┘
                                  │
                                  ▼
┌─────────────────────────────────────────────────────────────────────────────┐
│                      ASP.NET Core Web API                                    │
│  ┌────────┬──────────┬─────────┬─────────┬──────────┬────────┬───────────┐  │
│  │  Auth  │ Products │  Cart   │  Orders │ Payments │ Admin  │  Reviews  │  │
│  └────────┴──────────┴─────────┴─────────┴──────────┴────────┴───────────┘  │
└─────────┬───────────────────────────────────────────────────────────────────┘
          │
          ├──────────────────┬──────────────────┐
          ▼                  ▼                  ▼
┌─────────────────┐ ┌─────────────────┐ ┌─────────────────┐
│   PostgreSQL    │ │   SendGrid /    │ │  Stripe / PayPal│
│   (Database)    │ │   SMTP (Email)  │ │  (Payments)     │
└─────────────────┘ └─────────────────┘ └─────────────────┘
```

### 1.2 Core Features

| Module | Features |
|--------|----------|
| **Storefront** | Product catalog, search, filtering, product details, reviews |
| **Shopping Cart** | Add/remove items, quantity management, server-side cart |
| **Checkout** | Address entry, payment processing, order creation |
| **User Account** | Registration, login, email verification, password reset, profile, order history, wishlist |
| **Orders** | Order creation, status tracking, order history, order detail |
| **Payments** | Stripe/PayPal processing, refunds, webhook handling |
| **Inventory** | Stock tracking, stock adjustments, low stock alerts |
| **Reviews** | Create, update, delete reviews with rating system |
| **Promo Codes** | Create, validate, apply promotional codes to orders |
| **Admin Panel** | Dashboard stats, product/category/order/customer/inventory/promo/review management |

---

## 2. Technology Stack

### 2.1 Frontend

| Technology | Purpose |
|------------|---------|
| React 18 | UI framework |
| Redux Toolkit | State management (auth, cart slices) |
| RTK Query | API data fetching & caching (separate api files per feature) |
| React Router | Client-side routing |
| TypeScript | Type safety |
| CSS Modules | Component-scoped styling (*.module.css) |
| Vite | Build tool |

### 2.2 Backend

| Technology | Purpose |
|------------|---------|
| ASP.NET Core 8 | Web API framework |
| Entity Framework Core 8 | ORM |
| PostgreSQL | Database |
| FluentValidation | Request validation (per-DTO validators) |
| AutoMapper | Entity to DTO mapping (single MappingProfile) |
| Swashbuckle | Swagger API documentation |
| BCrypt | Password hashing |

### 2.3 External Services

| Service | Purpose |
|---------|---------|
| Stripe | Primary payment processor |
| PayPal | Alternative payment method |
| SendGrid | Transactional emails (production) |
| SMTP | Transactional emails (development/fallback) |

---

## 3. Project Structure

### 3.1 Repository Structure

```
E-commerce/
├── src/
│   ├── backend/                      # ASP.NET Core Solution (.sln)
│   │   ├── ECommerce.API/            # Web API entry point
│   │   ├── ECommerce.Core/           # Domain entities, interfaces, enums, exceptions
│   │   ├── ECommerce.Application/    # Business logic, DTOs, validators, services
│   │   ├── ECommerce.Infrastructure/ # Data access, repositories, seeders
│   │   └── ECommerce.Tests/          # Unit & integration tests
│   │
│   └── frontend/                     # React Applications
│       ├── storefront/               # Customer-facing app
│       ├── admin/                    # Admin dashboard app
│       └── shared/                   # Shared types (types.ts)
│
├── scripts/                          # Build & deployment scripts
├── docker-compose.yml                # Local development
├── .gitignore
└── README.md
```

### 3.2 Backend Project Structure

```
ECommerce.API/
├── ActionFilters/
│   └── ValidationFilterAttribute.cs  # FluentValidation error → 400 response
├── Controllers/
│   ├── AuthController.cs             # Register, login, refresh, verify, password reset
│   ├── CartController.cs             # Cart CRUD (authenticated)
│   ├── CategoriesController.cs       # Public read + admin write
│   ├── DashboardController.cs        # Admin dashboard stats
│   ├── InventoryController.cs        # Admin stock management
│   ├── OrdersController.cs           # Order creation + status management
│   ├── PaymentsController.cs         # Payment processing + refunds
│   ├── ProductsController.cs         # Public read + admin write
│   ├── ProfileController.cs          # User profile read/update
│   ├── PromoCodesController.cs       # Promo code CRUD + validation
│   ├── ReviewsController.cs          # Review CRUD
│   └── WishlistController.cs         # Wishlist add/remove
├── Middleware/
│   └── GlobalExceptionMiddleware.cs  # Catches all exceptions → typed HTTP responses
└── Program.cs                        # DI, middleware pipeline, Swagger setup

ECommerce.Core/
├── Common/
│   └── BaseEntity.cs                 # Id (Guid), CreatedAt, UpdatedAt
├── Entities/                         # 14 domain entities
│   ├── User.cs
│   ├── Product.cs
│   ├── ProductImage.cs
│   ├── Category.cs                   # Self-referencing (ParentId)
│   ├── Order.cs                      # Contains payment fields (PaymentIntentId, PaymentStatus)
│   ├── OrderItem.cs
│   ├── Cart.cs
│   ├── CartItem.cs
│   ├── Address.cs
│   ├── Review.cs
│   ├── PromoCode.cs
│   ├── Wishlist.cs
│   └── InventoryLog.cs               # Audit trail for stock changes
├── Enums/
│   ├── OrderStatus.cs
│   ├── PaymentStatus.cs
│   └── UserRole.cs                   # Customer, Admin
├── Interfaces/
│   └── Repositories/                 # Repository contracts
│       ├── IRepository<T>.cs         # Generic CRUD base
│       ├── IUnitOfWork.cs
│       ├── IUserRepository.cs
│       ├── IProductRepository.cs
│       ├── ICategoryRepository.cs
│       ├── ICartRepository.cs
│       ├── IOrderRepository.cs
│       ├── IReviewRepository.cs
│       └── IWishlistRepository.cs
└── Exceptions/                       # 43 exception files (see EXCEPTION_STRUCTURE.md)
    ├── Base/                         # 4 abstract base classes (404, 400, 401, 409)
    └── *.cs                          # Specific sealed exceptions

ECommerce.Application/
├── Interfaces/                       # 14 service contracts
│   ├── IAuthService.cs
│   ├── ICartService.cs
│   ├── ICategoryService.cs
│   ├── ICurrentUserService.cs
│   ├── IDashboardService.cs
│   ├── IEmailService.cs
│   ├── IInventoryService.cs
│   ├── IOrderService.cs
│   ├── IPaymentService.cs
│   ├── IProductService.cs
│   ├── IPromoCodeService.cs
│   ├── IReviewService.cs
│   ├── IUserService.cs
│   └── IWishlistService.cs
├── Services/                         # 15 service implementations
│   ├── AuthService.cs
│   ├── CartService.cs
│   ├── CategoryService.cs
│   ├── CurrentUserService.cs         # Extracts user from JWT claims
│   ├── DashboardService.cs
│   ├── InventoryService.cs
│   ├── OrderService.cs
│   ├── PaymentService.cs
│   ├── ProductService.cs
│   ├── PromoCodeService.cs
│   ├── ReviewService.cs
│   ├── SendGridEmailService.cs       # Production email via SendGrid
│   ├── SmtpEmailService.cs           # Dev/fallback email via SMTP
│   ├── UserService.cs
│   └── WishlistService.cs
├── DTOs/                             # 14 feature folders
│   ├── Auth/                         # AuthDtos.cs, AuthRequestDtos.cs
│   ├── Cart/                         # CartDtos.cs
│   ├── Common/                       # AddressDto, CategoryDto, ErrorDetails, PaginatedResult, etc.
│   ├── Dashboard/                    # DashboardStatsDto.cs
│   ├── Emails/                       # LowStockAlertEmailDto.cs
│   ├── Inventory/                    # InventoryDtos.cs
│   ├── Orders/                       # OrderDtos.cs
│   ├── Payments/                     # PaymentDtos.cs, SupportedPaymentMethodsResponseDto.cs
│   ├── Products/                     # CreateProductDto, ProductDto, ProductQueryDto, etc.
│   ├── PromoCodes/                   # PromoCodeDtos.cs
│   ├── Reviews/                      # ReviewDtos.cs
│   ├── Users/                        # UserProfileDtos.cs
│   └── Wishlist/                     # WishlistDtos.cs
├── Validators/                       # FluentValidation — one validator per DTO
│   ├── Auth/                         # 7 validators (register, login, change/reset/forgot password, etc.)
│   ├── Cart/                         # 2 validators
│   ├── Common/                       # 2 category validators
│   ├── Inventory/                    # 2 validators
│   ├── Orders/                       # 4 validators
│   ├── Payments/                     # 2 validators
│   ├── Products/                     # 3 validators
│   ├── PromoCodes/                   # 3 validators
│   ├── Reviews/                      # 2 validators
│   ├── Users/                        # 1 validator
│   └── Wishlist/                     # 1 validator
└── MappingProfile.cs                 # AutoMapper entity ↔ DTO mappings

ECommerce.Infrastructure/
├── Data/
│   ├── AppDbContext.cs               # EF Core DbContext — all entity registrations
│   ├── DatabaseSeeder.cs             # Orchestrates seeders
│   └── Seeders/                      # Interface + implementation pairs
│       ├── IUserSeeder / UserSeeder
│       ├── ICategorySeeder / CategorySeeder
│       └── IProductSeeder / ProductSeeder
├── Repositories/                     # 8 repository implementations
│   ├── Repository<T>.cs              # Generic base (implements IRepository<T>)
│   ├── CartRepository.cs
│   ├── CategoryRepository.cs
│   ├── OrderRepository.cs
│   ├── ProductRepository.cs
│   ├── ReviewRepository.cs
│   ├── UserRepository.cs
│   └── WishlistRepository.cs
├── Extensions/
│   └── QueryableExtensions.cs        # LINQ helpers for filtering/pagination
├── Migrations/                       # EF Core migrations
└── UnitOfWork.cs                     # Transaction management (implements IUnitOfWork)

ECommerce.Tests/
├── Helpers/                          # Test infrastructure
│   ├── IntegrationTestBase.cs        # Base class for integration tests
│   ├── MockHelpers.cs                # Mock factory helpers
│   ├── TestDataFactory.cs            # Consistent test entity builders
│   └── TestWebApplicationFactory.cs  # In-memory app host for integration tests
├── Integration/                      # 13 controller integration test suites
│   ├── AuthControllerTests.cs
│   ├── CartControllerTests.cs
│   ├── CategoriesControllerTests.cs
│   ├── DashboardControllerTests.cs
│   ├── InventoryControllerTests.cs
│   ├── OrdersControllerTests.cs
│   ├── PaymentsControllerTests.cs
│   ├── ProductsControllerTests.cs
│   ├── ProfileControllerTests.cs
│   ├── PromoCodesControllerTests.cs
│   ├── ReviewsControllerTests.cs
│   ├── WishlistControllerTests.cs
│   └── AddToCartCreateOrderTests.cs  # End-to-end flow test
└── Unit/                             # Unit tests by layer
    ├── ActionFilters/                # ValidationFilterAttribute tests
    ├── Helpers/                      # TestDataFactory tests
    ├── Mappings/                     # AutoMapper config validation
    ├── Middleware/                    # GlobalExceptionMiddleware tests
    ├── Services/                     # 14 service unit test files
    └── Validators/                   # 6 validator test files
```

### 3.3 Frontend Project Structure

```
frontend/storefront/                  # Customer-facing app
├── src/
│   ├── components/                   # Reusable UI components
│   │   ├── ui/                       # Button, Card, Input (with CSS Modules)
│   │   ├── Header.tsx / Footer.tsx
│   │   ├── ProductCard.tsx
│   │   ├── CategoryFilter.tsx
│   │   ├── ReviewForm.tsx / ReviewList.tsx
│   │   ├── CartItem.tsx
│   │   ├── ProtectedRoute.tsx
│   │   ├── ErrorBoundary.tsx
│   │   └── LoadingSkeleton.tsx
│   ├── pages/                        # Route-level page components
│   │   ├── Home.tsx
│   │   ├── Products.tsx
│   │   ├── ProductDetail.tsx
│   │   ├── Cart.tsx
│   │   ├── Checkout.tsx
│   │   ├── Login.tsx / Register.tsx
│   │   ├── ForgotPassword.tsx / ResetPassword.tsx
│   │   ├── Profile.tsx
│   │   ├── OrderHistory.tsx / OrderDetail.tsx
│   │   └── Wishlist.tsx
│   ├── store/
│   │   ├── store.ts                  # Redux store config
│   │   ├── hooks.ts                  # Typed useAppSelector / useAppDispatch
│   │   ├── slices/
│   │   │   ├── authSlice.ts          # Auth state (user, token, isAuthenticated)
│   │   │   └── cartSlice.ts          # Local cart state
│   │   └── api/                      # RTK Query endpoint definitions
│   │       ├── authApi.ts
│   │       ├── cartApi.ts
│   │       ├── categoriesApi.ts
│   │       ├── ordersApi.ts
│   │       ├── productApi.ts
│   │       ├── profileApi.ts
│   │       ├── reviewsApi.ts
│   │       └── wishlistApi.ts
│   ├── utils/
│   │   └── constants.ts
│   ├── App.tsx                       # Router setup + Redux Provider
│   ├── main.tsx                      # React entry point
│   └── index.css                     # Global styles
├── index.html
├── vite.config.ts
├── tsconfig.json
├── Dockerfile
└── package.json

frontend/admin/                       # Admin dashboard app
├── src/
│   ├── components/                   # Layout + UI components
│   │   ├── ui/                       # Badge, Button, Card, Input, Modal, Pagination, Table
│   │   ├── Header.tsx / Sidebar.tsx
│   │   ├── ProductForm.tsx
│   │   ├── PromoCodeForm.tsx
│   │   └── ProtectedRoute.tsx
│   ├── pages/                        # Admin page components
│   │   ├── Dashboard.tsx
│   │   ├── Products.tsx
│   │   ├── Orders.tsx
│   │   ├── Customers.tsx
│   │   ├── Inventory.tsx
│   │   ├── PromoCodes.tsx
│   │   ├── Reviews.tsx
│   │   ├── Settings.tsx
│   │   └── Login.tsx
│   ├── layouts/
│   │   └── AdminLayout.tsx           # Sidebar + Header wrapper
│   ├── store/
│   │   ├── store.ts
│   │   ├── hooks.ts
│   │   ├── slices/
│   │   │   └── authSlice.ts
│   │   └── api/                      # RTK Query endpoint definitions
│   │       ├── authApi.ts
│   │       ├── customersApi.ts
│   │       ├── dashboardApi.ts
│   │       ├── inventoryApi.ts
│   │       ├── ordersApi.ts
│   │       ├── productsApi.ts
│   │       ├── promoCodesApi.ts
│   │       └── reviewsApi.ts
│   ├── types/
│   │   └── index.ts
│   ├── App.tsx
│   ├── main.tsx
│   └── index.css
├── index.html
├── vite.config.ts
├── tsconfig.json
├── Dockerfile
└── package.json

frontend/shared/                      # Shared across apps
├── types.ts                          # Common TypeScript types
└── vite-env.d.ts
```

---

## 4. Database Design

### 4.1 Entity Relationship Diagram

```
┌──────────────────┐       ┌──────────────────┐       ┌──────────────────┐
│      Users       │       │    Categories    │       │    Products      │
├──────────────────┤       ├──────────────────┤       ├──────────────────┤
│ Id (PK)          │       │ Id (PK)          │       │ Id (PK)          │
│ Email            │       │ Name             │       │ Name             │
│ PasswordHash     │       │ Slug             │       │ Slug             │
│ FirstName        │       │ Description      │       │ Description      │
│ LastName         │       │ ImageUrl         │       │ Price            │
│ Phone            │       │ ParentId (FK)    │──┐    │ CompareAtPrice   │
│ Role             │       │ IsActive         │  │    │ SKU              │
│ IsEmailVerified  │       │ SortOrder        │  │    │ CategoryId (FK)  │───┐
│ CreatedAt        │       │ CreatedAt        │  │    │ StockQuantity    │   │
│ UpdatedAt        │       └──────────────────┘  │    │ LowStockThreshold│   │
└────────┬─────────┘              ▲               │    │ IsActive         │   │
         │                        └───────────────┘    │ IsFeatured       │   │
         │                                             │ CreatedAt        │   │
         │                                             │ UpdatedAt        │   │
         │                                             └────────┬─────────┘   │
         │                                                      │             │
         │       ┌──────────────────┐                           │             │
         │       │   ProductImages  │◄──────────────────────────┘             │
         │       ├──────────────────┤                                         │
         │       │ Id (PK)          │                                         │
         │       │ ProductId (FK)   │       ┌──────────────────┐              │
         │       │ Url              │       │     Reviews      │              │
         │       │ AltText          │       ├──────────────────┤              │
         │       │ IsPrimary        │       │ Id (PK)          │              │
         │       │ SortOrder        │       │ ProductId (FK)   │◄─────────────┘
         │       └──────────────────┘       │ UserId (FK)      │◄─────────────┐
         │                                  │ Rating           │              │
         │                                  │ Comment          │              │
         │                                  │ CreatedAt        │              │
         │                                  └──────────────────┘              │
         │                                                                    │
         ▼                                                                    │
┌──────────────────┐       ┌──────────────────┐       ┌──────────────────┐    │
│    Addresses     │       │      Carts       │       │    CartItems     │    │
├──────────────────┤       ├──────────────────┤       ├──────────────────┤    │
│ Id (PK)          │       │ Id (PK)          │       │ Id (PK)          │    │
│ UserId (FK)      │◄──┐   │ UserId (FK)      │◄──┐   │ CartId (FK)      │────┤
│ Street           │   │   │ CreatedAt        │   │   │ ProductId (FK)   │────┤
│ City             │   │   │ UpdatedAt        │   │   │ Quantity         │    │
│ State            │   │   └──────────────────┘   │   └──────────────────┘    │
│ PostalCode       │   │                          │                           │
│ Country          │   │   ┌──────────────────┐   │   ┌──────────────────┐    │
│ CreatedAt        │   │   │     Orders       │   │   │    OrderItems    │    │
└──────────────────┘   │   ├──────────────────┤   │   ├──────────────────┤    │
                       │   │ Id (PK)          │   │   │ Id (PK)          │    │
                       │   │ UserId (FK)      │◄──┼───│ OrderId (FK)     │────┤
                       │   │ Status           │   │   │ ProductId (FK)   │────┘
                       │   │ PaymentStatus    │   │   │ ProductName      │
                       │   │ PaymentMethod    │   │   │ Quantity         │
┌──────────────────┐   │   │ PaymentIntentId  │   │   │ UnitPrice        │
│   PromoCodes     │   │   │ SubTotal         │   │   │ TotalPrice       │
├──────────────────┤   │   │ DiscountAmount   │   │   └──────────────────┘
│ Id (PK)          │   │   │ ShippingAmount   │   │
│ Code             │   │   │ TotalAmount      │   │
│ DiscountType     │   │   │ ShippingAddressId│───┘
│ DiscountValue    │   │   │ BillingAddressId │───┘
│ MinOrderAmount   │   │   │ PromoCodeId (FK) │◄──────┐
│ MaxUses          │   │   │ CreatedAt        │       │
│ UsedCount        │   │   └──────────────────┘       │
│ IsActive         │   │                              │
│ CreatedAt        │   │   ┌──────────────────┐       │
└──────────────────┘───┼───│    Wishlists     │       │
                       │   ├──────────────────┤       │
                       │   │ Id (PK)          │       │
                       │   │ UserId (FK)      │◄──────┤
                       │   │ ProductId (FK)   │       │
                       │   └──────────────────┘       │
                       │                              │
                       │   ┌──────────────────┐       │
                       │   │  InventoryLogs   │       │
                       │   ├──────────────────┤       │
                       │   │ Id (PK)          │       │
                       │   │ ProductId (FK)   │       │
                       │   │ QuantityChange   │       │
                       │   │ Reason           │       │
                       │   │ CreatedBy (FK)   │◄──────┘
                       │   └──────────────────┘
```

---

## 5. Backend Architecture

### 5.1 Clean Architecture Layers

```
┌─────────────────────────────────────────────┐
│              ECommerce.API                   │  ← HTTP layer (controllers, middleware)
│  Controllers │ ActionFilters │ Middleware    │
└─────────────────────┬───────────────────────┘
                      │ depends on
┌─────────────────────▼───────────────────────┐
│           ECommerce.Application              │  ← Business logic
│  Services │ DTOs │ Validators │ Interfaces  │
└─────────────────────┬───────────────────────┘
                      │ depends on
┌─────────────────────▼───────────────────────┐
│             ECommerce.Core                   │  ← Domain (no external deps)
│  Entities │ Interfaces │ Enums │ Exceptions │
└─────────────────────▲───────────────────────┘
                      │ implements
┌─────────────────────┴───────────────────────┐
│          ECommerce.Infrastructure            │  ← Data access
│  Repositories │ DbContext │ Seeders │ UoW   │
└─────────────────────────────────────────────┘
```

### 5.2 Repository + Unit of Work Pattern

- `IRepository<T>` — generic interface: `GetByIdAsync`, `GetAllAsync`, `AddAsync`, `UpdateAsync`, `DeleteAsync`
- Specialized repositories (e.g. `IProductRepository`) extend the generic with domain-specific queries
- `IUnitOfWork` — wraps `DbContext`, exposes typed repository properties, provides `SaveChangesAsync`
- All service methods operate through `IUnitOfWork` — single transaction per request

### 5.3 Request Flow

```
HTTP Request
    → ActionFilters/ValidationFilterAttribute   (FluentValidation — returns 400 if invalid)
    → Controller method                         (extracts params, calls service)
    → Service (Application layer)               (business logic, uses UnitOfWork)
    → Repository (Infrastructure)              (EF Core queries)
    → Database (PostgreSQL)
```

### 5.4 Exception Handling

All exceptions inherit from typed base classes in `Core/Exceptions/Base/`:
- `NotFoundException` → HTTP 404
- `BadRequestException` → HTTP 400
- `UnauthorizedException` → HTTP 401
- `ConflictException` → HTTP 409

`GlobalExceptionMiddleware` catches these and returns a consistent JSON response. All concrete exceptions are `sealed` and use C# 12 primary constructors. See `EXCEPTION_STRUCTURE.md` for the full catalogue.

### 5.5 Authorization Model

- JWT Bearer tokens — issued on login, validated on every authenticated request
- Role-based: `[Authorize(Roles = "Admin")]` on admin-only controller methods
- `CurrentUserService` extracts the authenticated user's ID from JWT claims — injected into services that need ownership checks (e.g. cart belongs to user)

---

## 6. Frontend Architecture

### 6.1 State Management

Each app (storefront + admin) has its own Redux store:

```
store/
├── store.ts          # configureStore — root reducer + RTK Query middleware
├── hooks.ts          # typed useAppSelector, useAppDispatch
├── slices/           # Redux slices (local state)
│   ├── authSlice.ts  # user, token, isAuthenticated
│   └── cartSlice.ts  # (storefront only) local cart items
└── api/              # RTK Query — one file per backend resource
    ├── authApi.ts
    ├── productApi.ts
    └── ...
```

- **RTK Query** handles all server state: fetching, caching, cache invalidation via tags
- **Redux slices** handle client-only state: auth token persistence, UI state
- Each `api/*.ts` file injects endpoints onto a shared base query configured with the API URL and Bearer token header

### 6.2 Styling

CSS Modules (`*.module.css`) — every component that needs styles has a co-located `.module.css` file. No global CSS framework; styles are component-scoped.

### 6.3 Routing

- Storefront: React Router with flat route structure. Auth-guarded pages wrapped in `ProtectedRoute`
- Admin: Same pattern — `ProtectedRoute` gates all admin pages; unauthenticated users redirect to admin login

---

## 7. Authentication & Authorization

### 7.1 Registration & Login Flow

1. **Register** — `POST /api/auth/register` — creates user (unverified), sends verification email
2. **Verify Email** — `POST /api/auth/verify-email` — marks user as verified
3. **Login** — `POST /api/auth/login` — validates credentials, returns JWT + refresh token
4. **Refresh** — `POST /api/auth/refresh` — exchanges refresh token for new JWT
5. **Forgot/Reset Password** — two-step: request reset link via email, then submit new password with token

### 7.2 Token Structure

```json
{
  "sub": "user-uuid",
  "email": "user@example.com",
  "role": "Customer",
  "iat": 1699000000,
  "exp": 1699086400
}
```

### 7.3 Admin Access

Controllers use `[Authorize(Roles = "Admin")]` on endpoints that require admin privileges. The same controllers serve both public and admin operations — no separate admin controller hierarchy. Example: `ProductsController` has public GET endpoints and admin-only POST/PUT/DELETE endpoints.

---

## 8. Payment Integration

### 8.1 Payment Flow

```
Client                          Server                      Payment Provider
  │                                │                              │
  │  POST /payments/process        │                              │
  │  {orderId, method, amount}     │                              │
  ├───────────────────────────────►│                              │
  │                                │  Create intent / order       │
  │                                ├────────────────────────────► │
  │                                │◄────────────────────────────┤
  │                                │                              │
  │  PaymentResponse               │                              │
  │  {status, transactionId}       │                              │
  │◄───────────────────────────────┤                              │
  │                                │                              │
  │                                │  Webhook: payment confirmed  │
  │                                │◄────────────────────────────┤
  │                                │                              │
  │                                │  Update order status → Paid  │
```

### 8.2 Supported Operations

| Endpoint | Description |
|----------|-------------|
| `POST /payments/process` | Process a payment for an order |
| `POST /payments/refund` | Refund a payment |
| `GET /payments/order/{orderId}` | Get payment details for an order |
| `POST /payments/webhook` | Receive payment provider webhooks |
| `GET /payments/supported-methods` | List available payment methods |

### 8.3 Key Design Decisions

- Payment fields live on the `Order` entity (no separate Payment entity)
- `PaymentService` abstracts provider-specific logic behind `IPaymentService`
- Refunds are validated: amount must not exceed original, order must be in a refundable state

---

## 9. Admin Panel

### 9.1 Dashboard

Admin dashboard (`DashboardController` + `DashboardService`) provides aggregate stats. The admin frontend displays these via the `dashboardApi` RTK Query endpoint.

### 9.2 Admin Pages

| Page | Backend Controller | Key Operations |
|------|-------------------|----------------|
| Dashboard | DashboardController | View stats |
| Products | ProductsController | CRUD products |
| Orders | OrdersController | View orders, update status |
| Customers | ProfileController | View customer list/details |
| Inventory | InventoryController | Adjust stock, view logs |
| Promo Codes | PromoCodesController | CRUD promo codes |
| Reviews | ReviewsController | View/manage reviews |
| Settings | — | App configuration |

### 9.3 Authorization

All admin pages are gated by `ProtectedRoute` on the frontend. The backend enforces `[Authorize(Roles = "Admin")]` on mutation endpoints. Read endpoints may be public (products, categories) or admin-only (dashboard, customers).

---

## 10. Email & Notifications

### 10.1 Dual Provider Setup

- **Production**: `SendGridEmailService` — uses SendGrid API
- **Development/Fallback**: `SmtpEmailService` — uses standard SMTP

Both implement `IEmailService`. The active provider is selected via DI configuration.

### 10.2 Email Triggers

| Trigger | Email Sent |
|---------|------------|
| User registers | Verification email with confirmation link |
| User requests password reset | Reset link with expiry token |
| Low stock detected | Alert email to admin (LowStockAlertEmailDto) |

---

## 11. Deployment Strategy

### 11.1 Local Development

`docker-compose.yml` at the repository root provides:
- PostgreSQL database
- API backend
- Frontend apps (storefront + admin) with hot reload

### 11.2 Production (Render.com)

- **Static Sites**: Storefront and Admin deployed as Render static sites
- **Web Service**: ASP.NET Core API deployed as a Render web service
- **Database**: PostgreSQL managed database on Render
- Environment variables configured per service in Render dashboard

---

## 12. Security Considerations

| Area | Approach |
|------|----------|
| Passwords | BCrypt hashing |
| Authentication | JWT with short-lived access tokens + refresh token rotation |
| Input validation | FluentValidation on every request DTO — rejects malformed input before it reaches business logic |
| SQL injection | Prevented by EF Core parameterized queries |
| Authorization | Role-based (`[Authorize]` attributes) + ownership checks via `CurrentUserService` |
| Error responses | Consistent typed responses via `GlobalExceptionMiddleware` — no stack traces leaked to clients |
| CORS | Configured to allow only the frontend origin |
| Secrets | Stored in environment variables, never in code |
