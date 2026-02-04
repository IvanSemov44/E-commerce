# E-Commerce Platform Architecture Plan

## Executive Summary

This document outlines the comprehensive architecture for a B2C e-commerce platform built with **React**, **Redux Toolkit**, **ASP.NET Core**, and **PostgreSQL**. The platform is designed for small-scale operations (<1K daily users) with a feature-complete approach before launch, hosted on Render.com.

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
11. [Third-Party Integrations](#11-third-party-integrations)
12. [Deployment Strategy](#12-deployment-strategy)
13. [Security Considerations](#13-security-considerations)
14. [Development Workflow](#14-development-workflow)
15. [API Documentation](#15-api-documentation)
16. [Future Considerations](#16-future-considerations)

---

## 1. System Overview

### 1.1 High-Level Architecture

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                              CLIENTS                                         │
├─────────────────────┬───────────────────────┬───────────────────────────────┤
│   Customer Web App  │    Admin Dashboard    │      Mobile (Future)          │
│   (React + Redux)   │    (React + Redux)    │                               │
└─────────┬───────────┴───────────┬───────────┴───────────────────────────────┘
          │                       │
          ▼                       ▼
┌─────────────────────────────────────────────────────────────────────────────┐
│                         RENDER.COM CDN                                       │
│                    (Static Asset Delivery)                                   │
└─────────────────────────────────┬───────────────────────────────────────────┘
                                  │
                                  ▼
┌─────────────────────────────────────────────────────────────────────────────┐
│                         API GATEWAY / BACKEND                                │
│                      ASP.NET Core Web API                                    │
│  ┌──────────────┬──────────────┬──────────────┬──────────────┐              │
│  │ Auth Service │ Product API  │  Order API   │  Admin API   │              │
│  └──────────────┴──────────────┴──────────────┴──────────────┘              │
└─────────┬───────────────────────────────────────────────────────────────────┘
          │
          ├──────────────────┬──────────────────┬──────────────────┐
          ▼                  ▼                  ▼                  ▼
┌─────────────────┐ ┌─────────────────┐ ┌─────────────────┐ ┌─────────────────┐
│   PostgreSQL    │ │    SendGrid     │ │  Stripe/PayPal  │ │   Cloudinary    │
│   (Database)    │ │  (Email/SMTP)   │ │   (Payments)    │ │ (Image Storage) │
└─────────────────┘ └─────────────────┘ └─────────────────┘ └─────────────────┘
```

### 1.2 Core Features

| Module | Features |
|--------|----------|
| **Storefront** | Product catalog, search, filtering, product details, reviews |
| **Shopping Cart** | Add/remove items, quantity management, persistent cart |
| **Checkout** | Guest checkout, address management, shipping selection, payment |
| **User Account** | Registration, login (OAuth), profile, order history, wishlist |
| **Orders** | Order creation, status tracking, order history |
| **Inventory** | Stock tracking, low stock alerts |
| **Admin Panel** | Product management, order management, customer management, reports |
| **Marketing** | Email campaigns, abandoned cart recovery, promotional codes |

---

## 2. Technology Stack

### 2.1 Frontend

| Technology | Version | Purpose |
|------------|---------|---------|
| React | 18.x | UI framework |
| Redux Toolkit | 2.x | State management |
| RTK Query | 2.x | API data fetching & caching |
| React Router | 6.x | Client-side routing |
| TypeScript | 5.x | Type safety |
| Tailwind CSS | 3.x | Styling |
| React Hook Form | 7.x | Form management |
| Zod | 3.x | Schema validation |
| Vite | 5.x | Build tool |

### 2.2 Backend

| Technology | Version | Purpose |
|------------|---------|---------|
| ASP.NET Core | 8.x | Web API framework |
| Entity Framework Core | 8.x | ORM |
| PostgreSQL | 15.x | Database |
| FluentValidation | 11.x | Request validation |
| AutoMapper | 12.x | Object mapping |
| Serilog | 3.x | Logging |
| Swashbuckle | 6.x | API documentation |

### 2.3 External Services

| Service | Purpose |
|---------|---------|
| Stripe | Primary payment processor |
| PayPal | Alternative payment method |
| SendGrid | Transactional & marketing emails |
| Cloudinary | Image storage & optimization |
| Google Analytics 4 | Analytics tracking |
| HubSpot (Free) | CRM integration |

---

## 3. Project Structure

### 3.1 Repository Structure (Monorepo)

```
e-commerce/
├── src/
│   ├── backend/                    # ASP.NET Core Solution
│   │   ├── ECommerce.API/          # Web API project
│   │   ├── ECommerce.Core/         # Domain entities & interfaces
│   │   ├── ECommerce.Application/  # Business logic & services
│   │   ├── ECommerce.Infrastructure/ # Data access & external services
│   │   └── ECommerce.Tests/        # Unit & integration tests
│   │
│   ├── frontend/                   # React Application
│   │   ├── apps/
│   │   │   ├── storefront/         # Customer-facing app
│   │   │   └── admin/              # Admin dashboard
│   │   ├── packages/
│   │   │   ├── ui/                 # Shared UI components
│   │   │   ├── api-client/         # Generated API client
│   │   │   └── utils/              # Shared utilities
│   │   └── package.json
│   │
│   └── shared/                     # Shared types & contracts
│       └── api-contracts/          # OpenAPI specs
│
├── docs/                           # Documentation
├── scripts/                        # Build & deployment scripts
├── docker-compose.yml              # Local development
├── render.yaml                     # Render.com configuration
└── README.md
```

### 3.2 Backend Project Structure

```
ECommerce.API/
├── Controllers/
│   ├── AuthController.cs
│   ├── ProductsController.cs
│   ├── CategoriesController.cs
│   ├── CartController.cs
│   ├── OrdersController.cs
│   ├── CheckoutController.cs
│   ├── UsersController.cs
│   └── Admin/
│       ├── AdminProductsController.cs
│       ├── AdminOrdersController.cs
│       ├── AdminCustomersController.cs
│       └── AdminReportsController.cs
├── Middleware/
│   ├── ExceptionHandlingMiddleware.cs
│   ├── RequestLoggingMiddleware.cs
│   └── RateLimitingMiddleware.cs
├── Filters/
│   └── ValidationFilter.cs
└── Program.cs

ECommerce.Core/
├── Entities/
│   ├── User.cs
│   ├── Product.cs
│   ├── Category.cs
│   ├── Order.cs
│   ├── OrderItem.cs
│   ├── Cart.cs
│   ├── CartItem.cs
│   ├── Address.cs
│   ├── Review.cs
│   └── PromoCode.cs
├── Interfaces/
│   ├── Repositories/
│   │   ├── IProductRepository.cs
│   │   ├── IOrderRepository.cs
│   │   └── IUserRepository.cs
│   └── Services/
│       ├── IPaymentService.cs
│       ├── IEmailService.cs
│       └── IInventoryService.cs
├── Enums/
│   ├── OrderStatus.cs
│   ├── PaymentStatus.cs
│   └── UserRole.cs
└── Exceptions/
    ├── NotFoundException.cs
    ├── ValidationException.cs
    └── PaymentException.cs

ECommerce.Application/
├── Services/
│   ├── ProductService.cs
│   ├── OrderService.cs
│   ├── CartService.cs
│   ├── CheckoutService.cs
│   ├── InventoryService.cs
│   └── UserService.cs
├── DTOs/
│   ├── Products/
│   ├── Orders/
│   ├── Cart/
│   └── Users/
├── Validators/
│   ├── CreateProductValidator.cs
│   ├── CreateOrderValidator.cs
│   └── RegisterUserValidator.cs
└── Mappings/
    └── MappingProfile.cs

ECommerce.Infrastructure/
├── Data/
│   ├── AppDbContext.cs
│   ├── Configurations/
│   │   ├── UserConfiguration.cs
│   │   ├── ProductConfiguration.cs
│   │   └── OrderConfiguration.cs
│   └── Migrations/
├── Repositories/
│   ├── ProductRepository.cs
│   ├── OrderRepository.cs
│   └── UserRepository.cs
├── Services/
│   ├── StripePaymentService.cs
│   ├── PayPalPaymentService.cs
│   ├── SendGridEmailService.cs
│   └── CloudinaryImageService.cs
└── Extensions/
    └── ServiceCollectionExtensions.cs
```

### 3.3 Frontend Project Structure

```
frontend/apps/storefront/
├── src/
│   ├── app/
│   │   ├── store.ts                # Redux store configuration
│   │   ├── api.ts                  # RTK Query API definition
│   │   └── hooks.ts                # Typed hooks
│   ├── features/
│   │   ├── auth/
│   │   │   ├── authSlice.ts
│   │   │   ├── authApi.ts
│   │   │   └── components/
│   │   │       ├── LoginForm.tsx
│   │   │       ├── RegisterForm.tsx
│   │   │       └── SocialLogin.tsx
│   │   ├── products/
│   │   │   ├── productsApi.ts
│   │   │   └── components/
│   │   │       ├── ProductCard.tsx
│   │   │       ├── ProductGrid.tsx
│   │   │       ├── ProductDetails.tsx
│   │   │       └── ProductFilters.tsx
│   │   ├── cart/
│   │   │   ├── cartSlice.ts
│   │   │   ├── cartApi.ts
│   │   │   └── components/
│   │   │       ├── CartDrawer.tsx
│   │   │       ├── CartItem.tsx
│   │   │       └── CartSummary.tsx
│   │   ├── checkout/
│   │   │   ├── checkoutSlice.ts
│   │   │   ├── checkoutApi.ts
│   │   │   └── components/
│   │   │       ├── CheckoutForm.tsx
│   │   │       ├── AddressForm.tsx
│   │   │       ├── PaymentForm.tsx
│   │   │       └── OrderSummary.tsx
│   │   └── orders/
│   │       ├── ordersApi.ts
│   │       └── components/
│   │           ├── OrderHistory.tsx
│   │           └── OrderDetails.tsx
│   ├── components/
│   │   ├── layout/
│   │   │   ├── Header.tsx
│   │   │   ├── Footer.tsx
│   │   │   ├── Navigation.tsx
│   │   │   └── Layout.tsx
│   │   ├── common/
│   │   │   ├── Button.tsx
│   │   │   ├── Input.tsx
│   │   │   ├── Modal.tsx
│   │   │   ├── Loading.tsx
│   │   │   └── ErrorBoundary.tsx
│   │   └── seo/
│   │       └── SEOHead.tsx
│   ├── pages/
│   │   ├── HomePage.tsx
│   │   ├── ProductsPage.tsx
│   │   ├── ProductDetailPage.tsx
│   │   ├── CartPage.tsx
│   │   ├── CheckoutPage.tsx
│   │   ├── OrderConfirmationPage.tsx
│   │   ├── AccountPage.tsx
│   │   ├── OrdersPage.tsx
│   │   └── NotFoundPage.tsx
│   ├── routes/
│   │   ├── index.tsx
│   │   └── ProtectedRoute.tsx
│   ├── utils/
│   │   ├── formatters.ts
│   │   ├── validators.ts
│   │   └── constants.ts
│   ├── types/
│   │   └── index.ts
│   ├── styles/
│   │   └── globals.css
│   ├── App.tsx
│   └── main.tsx
├── public/
├── index.html
├── tailwind.config.js
├── tsconfig.json
├── vite.config.ts
└── package.json
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
         │       ┌──────────────────┐                          │             │
         │       │   ProductImages  │◄─────────────────────────┘             │
         │       ├──────────────────┤                                        │
         │       │ Id (PK)          │                                        │
         │       │ ProductId (FK)   │       ┌──────────────────┐             │
         │       │ Url              │       │     Reviews      │             │
         │       │ AltText          │       ├──────────────────┤             │
         │       │ IsPrimary        │       │ Id (PK)          │             │
         │       │ SortOrder        │       │ ProductId (FK)   │◄────────────┘
         │       └──────────────────┘       │ UserId (FK)      │◄────────────┐
         │                                  │ Rating           │             │
         │                                  │ Title            │             │
         │                                  │ Comment          │             │
         │                                  │ IsVerified       │             │
         │                                  │ CreatedAt        │             │
         │                                  └──────────────────┘             │
         │                                                                   │
         ▼                                                                   │
┌──────────────────┐       ┌──────────────────┐       ┌──────────────────┐   │
│    Addresses     │       │      Carts       │       │    CartItems     │   │
├──────────────────┤       ├──────────────────┤       ├──────────────────┤   │
│ Id (PK)          │       │ Id (PK)          │       │ Id (PK)          │   │
│ UserId (FK)      │◄──┐   │ UserId (FK)      │◄──┐   │ CartId (FK)      │───┤
│ Type (Billing/   │   │   │ SessionId        │   │   │ ProductId (FK)   │───┤
│       Shipping)  │   │   │ CreatedAt        │   │   │ Quantity         │   │
│ FirstName        │   │   │ UpdatedAt        │   │   │ AddedAt          │   │
│ LastName         │   │   └──────────────────┘   │   └──────────────────┘   │
│ Street           │   │                          │                          │
│ City             │   │   ┌──────────────────┐   │   ┌──────────────────┐   │
│ State            │   │   │     Orders       │   │   │    OrderItems    │   │
│ PostalCode       │   │   ├──────────────────┤   │   ├──────────────────┤   │
│ Country          │   │   │ Id (PK)          │   │   │ Id (PK)          │   │
│ IsDefault        │   │   │ OrderNumber      │   │   │ OrderId (FK)     │───┤
│ CreatedAt        │   │   │ UserId (FK)      │◄──┼───│ ProductId (FK)   │───┘
└──────────────────┘   │   │ Status           │   │   │ ProductName      │
                       │   │ PaymentStatus    │   │   │ ProductSKU       │
                       │   │ PaymentMethod    │   │   │ Quantity         │
┌──────────────────┐   │   │ PaymentIntentId  │   │   │ UnitPrice        │
│   PromoCodes     │   │   │ SubTotal         │   │   │ TotalPrice       │
├──────────────────┤   │   │ DiscountAmount   │   │   └──────────────────┘
│ Id (PK)          │   │   │ ShippingAmount   │   │
│ Code             │   │   │ TaxAmount        │   │
│ DiscountType     │   │   │ TotalAmount      │   │
│ DiscountValue    │   │   │ ShippingAddressId│───┘
│ MinOrderAmount   │   │   │ BillingAddressId │───┘
│ MaxUses          │   │   │ PromoCodeId (FK) │◄──────┐
│ UsedCount        │   │   │ Notes            │       │
│ StartDate        │   │   │ CreatedAt        │       │
│ EndDate          │   │   │ UpdatedAt        │       │
│ IsActive         │   │   └──────────────────┘       │
│ CreatedAt        │   │                              │
└──────────────────┘───┼──────────────────────────────┘
                       │
┌──────────────────┐   │   ┌──────────────────┐
│    Wishlists     │   │   │  EmailCampaigns  │
├──────────────────┤   │   ├──────────────────┤
│ Id (PK)          │   │   │ Id (PK)          │
│ UserId (FK)      │◄──┘   │ Name             │
│ ProductId (FK)   │       │ Subject          │
│ AddedAt          │       │ HtmlContent      │
└──────────────────┘       │ Type             │
                           │ Status           │
                           │ SentAt           │
                           │ CreatedAt        │
                           └──────────────────┘
```

### 4.2 Database Schema (SQL)

```sql
-- Users table
CREATE TABLE users (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    email VARCHAR(255) NOT NULL UNIQUE,
    password_hash VARCHAR(255),
    first_name VARCHAR(100),
    last_name VARCHAR(100),
    phone VARCHAR(20),
    role VARCHAR(20) NOT NULL DEFAULT 'Customer',
    is_email_verified BOOLEAN NOT NULL DEFAULT FALSE,
    email_verification_token VARCHAR(255),
    password_reset_token VARCHAR(255),
    password_reset_expires TIMESTAMP,
    google_id VARCHAR(255),
    facebook_id VARCHAR(255),
    avatar_url VARCHAR(500),
    created_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP
);

-- Categories table (self-referencing for hierarchy)
CREATE TABLE categories (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    name VARCHAR(100) NOT NULL,
    slug VARCHAR(100) NOT NULL UNIQUE,
    description TEXT,
    image_url VARCHAR(500),
    parent_id UUID REFERENCES categories(id) ON DELETE SET NULL,
    is_active BOOLEAN NOT NULL DEFAULT TRUE,
    sort_order INT NOT NULL DEFAULT 0,
    created_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP
);

-- Products table
CREATE TABLE products (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    name VARCHAR(255) NOT NULL,
    slug VARCHAR(255) NOT NULL UNIQUE,
    description TEXT,
    short_description VARCHAR(500),
    price DECIMAL(10, 2) NOT NULL,
    compare_at_price DECIMAL(10, 2),
    cost_price DECIMAL(10, 2),
    sku VARCHAR(100) UNIQUE,
    barcode VARCHAR(100),
    category_id UUID REFERENCES categories(id) ON DELETE SET NULL,
    stock_quantity INT NOT NULL DEFAULT 0,
    low_stock_threshold INT NOT NULL DEFAULT 10,
    weight DECIMAL(8, 2),
    is_active BOOLEAN NOT NULL DEFAULT TRUE,
    is_featured BOOLEAN NOT NULL DEFAULT FALSE,
    meta_title VARCHAR(255),
    meta_description VARCHAR(500),
    created_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP
);

-- Product Images table
CREATE TABLE product_images (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    product_id UUID NOT NULL REFERENCES products(id) ON DELETE CASCADE,
    url VARCHAR(500) NOT NULL,
    alt_text VARCHAR(255),
    is_primary BOOLEAN NOT NULL DEFAULT FALSE,
    sort_order INT NOT NULL DEFAULT 0,
    created_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP
);

-- Addresses table
CREATE TABLE addresses (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    user_id UUID REFERENCES users(id) ON DELETE CASCADE,
    type VARCHAR(20) NOT NULL, -- 'shipping' or 'billing'
    first_name VARCHAR(100) NOT NULL,
    last_name VARCHAR(100) NOT NULL,
    company VARCHAR(100),
    street_line1 VARCHAR(255) NOT NULL,
    street_line2 VARCHAR(255),
    city VARCHAR(100) NOT NULL,
    state VARCHAR(100) NOT NULL,
    postal_code VARCHAR(20) NOT NULL,
    country VARCHAR(2) NOT NULL,
    phone VARCHAR(20),
    is_default BOOLEAN NOT NULL DEFAULT FALSE,
    created_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP
);

-- Carts table
CREATE TABLE carts (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    user_id UUID REFERENCES users(id) ON DELETE CASCADE,
    session_id VARCHAR(255),
    created_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT cart_user_or_session CHECK (user_id IS NOT NULL OR session_id IS NOT NULL)
);

-- Cart Items table
CREATE TABLE cart_items (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    cart_id UUID NOT NULL REFERENCES carts(id) ON DELETE CASCADE,
    product_id UUID NOT NULL REFERENCES products(id) ON DELETE CASCADE,
    quantity INT NOT NULL DEFAULT 1,
    added_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    UNIQUE(cart_id, product_id)
);

-- Promo Codes table
CREATE TABLE promo_codes (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    code VARCHAR(50) NOT NULL UNIQUE,
    discount_type VARCHAR(20) NOT NULL, -- 'percentage' or 'fixed'
    discount_value DECIMAL(10, 2) NOT NULL,
    min_order_amount DECIMAL(10, 2),
    max_discount_amount DECIMAL(10, 2),
    max_uses INT,
    used_count INT NOT NULL DEFAULT 0,
    start_date TIMESTAMP,
    end_date TIMESTAMP,
    is_active BOOLEAN NOT NULL DEFAULT TRUE,
    created_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP
);

-- Orders table
CREATE TABLE orders (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    order_number VARCHAR(20) NOT NULL UNIQUE,
    user_id UUID REFERENCES users(id) ON DELETE SET NULL,
    guest_email VARCHAR(255),
    status VARCHAR(30) NOT NULL DEFAULT 'pending',
    payment_status VARCHAR(30) NOT NULL DEFAULT 'pending',
    payment_method VARCHAR(30),
    payment_intent_id VARCHAR(255),
    subtotal DECIMAL(10, 2) NOT NULL,
    discount_amount DECIMAL(10, 2) NOT NULL DEFAULT 0,
    shipping_amount DECIMAL(10, 2) NOT NULL DEFAULT 0,
    tax_amount DECIMAL(10, 2) NOT NULL DEFAULT 0,
    total_amount DECIMAL(10, 2) NOT NULL,
    currency VARCHAR(3) NOT NULL DEFAULT 'USD',
    shipping_address_id UUID REFERENCES addresses(id),
    billing_address_id UUID REFERENCES addresses(id),
    promo_code_id UUID REFERENCES promo_codes(id),
    notes TEXT,
    shipped_at TIMESTAMP,
    delivered_at TIMESTAMP,
    cancelled_at TIMESTAMP,
    created_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT order_user_or_guest CHECK (user_id IS NOT NULL OR guest_email IS NOT NULL)
);

-- Order Items table
CREATE TABLE order_items (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    order_id UUID NOT NULL REFERENCES orders(id) ON DELETE CASCADE,
    product_id UUID REFERENCES products(id) ON DELETE SET NULL,
    product_name VARCHAR(255) NOT NULL,
    product_sku VARCHAR(100),
    product_image_url VARCHAR(500),
    quantity INT NOT NULL,
    unit_price DECIMAL(10, 2) NOT NULL,
    total_price DECIMAL(10, 2) NOT NULL,
    created_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP
);

-- Reviews table
CREATE TABLE reviews (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    product_id UUID NOT NULL REFERENCES products(id) ON DELETE CASCADE,
    user_id UUID REFERENCES users(id) ON DELETE SET NULL,
    order_id UUID REFERENCES orders(id) ON DELETE SET NULL,
    rating INT NOT NULL CHECK (rating >= 1 AND rating <= 5),
    title VARCHAR(255),
    comment TEXT,
    is_verified BOOLEAN NOT NULL DEFAULT FALSE,
    is_approved BOOLEAN NOT NULL DEFAULT FALSE,
    created_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP
);

-- Wishlists table
CREATE TABLE wishlists (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    user_id UUID NOT NULL REFERENCES users(id) ON DELETE CASCADE,
    product_id UUID NOT NULL REFERENCES products(id) ON DELETE CASCADE,
    added_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    UNIQUE(user_id, product_id)
);

-- Inventory Log table (for tracking)
CREATE TABLE inventory_logs (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    product_id UUID NOT NULL REFERENCES products(id) ON DELETE CASCADE,
    quantity_change INT NOT NULL,
    reason VARCHAR(50) NOT NULL, -- 'sale', 'restock', 'adjustment', 'return'
    reference_id UUID, -- order_id or other reference
    notes TEXT,
    created_by UUID REFERENCES users(id),
    created_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP
);

-- Indexes for performance
CREATE INDEX idx_products_category ON products(category_id);
CREATE INDEX idx_products_slug ON products(slug);
CREATE INDEX idx_products_active ON products(is_active);
CREATE INDEX idx_orders_user ON orders(user_id);
CREATE INDEX idx_orders_status ON orders(status);
CREATE INDEX idx_orders_created ON orders(created_at DESC);
CREATE INDEX idx_cart_items_cart ON cart_items(cart_id);
CREATE INDEX idx_reviews_product ON reviews(product_id);
CREATE INDEX idx_addresses_user ON addresses(user_id);
```

---

## 5. Backend Architecture

### 5.1 API Structure

The API follows RESTful conventions with consistent patterns:

```
Base URL: /api/v1

Public Endpoints (No Auth):
├── POST   /auth/register
├── POST   /auth/login
├── POST   /auth/google
├── POST   /auth/facebook
├── POST   /auth/forgot-password
├── POST   /auth/reset-password
├── GET    /products
├── GET    /products/{slug}
├── GET    /products/{id}/reviews
├── GET    /categories
├── GET    /categories/{slug}
├── POST   /cart (guest cart)
├── GET    /cart/{sessionId}
├── POST   /checkout/guest

Protected Endpoints (Auth Required):
├── GET    /auth/me
├── PUT    /auth/profile
├── POST   /auth/change-password
├── POST   /auth/logout
├── GET    /cart
├── POST   /cart/items
├── PUT    /cart/items/{id}
├── DELETE /cart/items/{id}
├── POST   /checkout
├── GET    /orders
├── GET    /orders/{id}
├── POST   /reviews
├── GET    /wishlist
├── POST   /wishlist
├── DELETE /wishlist/{productId}
├── GET    /addresses
├── POST   /addresses
├── PUT    /addresses/{id}
├── DELETE /addresses/{id}

Admin Endpoints (Admin Role Required):
├── GET    /admin/dashboard
├── GET    /admin/products
├── POST   /admin/products
├── PUT    /admin/products/{id}
├── DELETE /admin/products/{id}
├── POST   /admin/products/{id}/images
├── DELETE /admin/products/{id}/images/{imageId}
├── GET    /admin/categories
├── POST   /admin/categories
├── PUT    /admin/categories/{id}
├── DELETE /admin/categories/{id}
├── GET    /admin/orders
├── GET    /admin/orders/{id}
├── PUT    /admin/orders/{id}/status
├── GET    /admin/customers
├── GET    /admin/customers/{id}
├── GET    /admin/inventory
├── PUT    /admin/inventory/{productId}
├── GET    /admin/promo-codes
├── POST   /admin/promo-codes
├── PUT    /admin/promo-codes/{id}
├── DELETE /admin/promo-codes/{id}
├── GET    /admin/reviews
├── PUT    /admin/reviews/{id}/approve
├── DELETE /admin/reviews/{id}
├── GET    /admin/reports/sales
├── GET    /admin/reports/inventory
├── GET    /admin/reports/customers

Webhook Endpoints:
├── POST   /webhooks/stripe
├── POST   /webhooks/paypal
```

### 5.2 Service Layer Pattern

```csharp
// Example: IProductService interface
public interface IProductService
{
    Task<PagedResult<ProductDto>> GetProductsAsync(ProductQueryParams queryParams);
    Task<ProductDetailDto?> GetProductBySlugAsync(string slug);
    Task<ProductDto> CreateProductAsync(CreateProductDto dto);
    Task<ProductDto> UpdateProductAsync(Guid id, UpdateProductDto dto);
    Task DeleteProductAsync(Guid id);
    Task<ProductImageDto> AddProductImageAsync(Guid productId, IFormFile file);
    Task DeleteProductImageAsync(Guid productId, Guid imageId);
}

// Example: ProductService implementation
public class ProductService : IProductService
{
    private readonly IProductRepository _productRepository;
    private readonly IImageService _imageService;
    private readonly IMapper _mapper;
    private readonly ILogger<ProductService> _logger;

    public ProductService(
        IProductRepository productRepository,
        IImageService imageService,
        IMapper mapper,
        ILogger<ProductService> logger)
    {
        _productRepository = productRepository;
        _imageService = imageService;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<PagedResult<ProductDto>> GetProductsAsync(ProductQueryParams queryParams)
    {
        var products = await _productRepository.GetPagedAsync(queryParams);
        return _mapper.Map<PagedResult<ProductDto>>(products);
    }

    // ... other methods
}
```

### 5.3 Repository Pattern

```csharp
// Generic Repository Interface
public interface IRepository<T> where T : BaseEntity
{
    Task<T?> GetByIdAsync(Guid id);
    Task<IEnumerable<T>> GetAllAsync();
    Task<T> AddAsync(T entity);
    Task UpdateAsync(T entity);
    Task DeleteAsync(T entity);
}

// Product-specific Repository
public interface IProductRepository : IRepository<Product>
{
    Task<Product?> GetBySlugAsync(string slug);
    Task<PagedResult<Product>> GetPagedAsync(ProductQueryParams queryParams);
    Task<IEnumerable<Product>> GetFeaturedAsync(int count);
    Task<IEnumerable<Product>> GetByCategoryAsync(Guid categoryId);
    Task UpdateStockAsync(Guid productId, int quantity);
}
```

### 5.4 Dependency Injection Setup

```csharp
// Program.cs
var builder = WebApplication.CreateBuilder(args);

// Database
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// Repositories
builder.Services.AddScoped<IProductRepository, ProductRepository>();
builder.Services.AddScoped<IOrderRepository, OrderRepository>();
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<ICategoryRepository, CategoryRepository>();
builder.Services.AddScoped<ICartRepository, CartRepository>();

// Services
builder.Services.AddScoped<IProductService, ProductService>();
builder.Services.AddScoped<IOrderService, OrderService>();
builder.Services.AddScoped<ICartService, CartService>();
builder.Services.AddScoped<ICheckoutService, CheckoutService>();
builder.Services.AddScoped<IInventoryService, InventoryService>();
builder.Services.AddScoped<IAuthService, AuthService>();

// External Services
builder.Services.AddScoped<IPaymentService, StripePaymentService>();
builder.Services.AddScoped<IEmailService, SendGridEmailService>();
builder.Services.AddScoped<IImageService, CloudinaryImageService>();

// AutoMapper
builder.Services.AddAutoMapper(typeof(MappingProfile));

// FluentValidation
builder.Services.AddValidatorsFromAssemblyContaining<CreateProductValidator>();

// Authentication
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(builder.Configuration["Jwt:SecretKey"]!))
        };
    })
    .AddGoogle(options =>
    {
        options.ClientId = builder.Configuration["Google:ClientId"]!;
        options.ClientSecret = builder.Configuration["Google:ClientSecret"]!;
    });

// CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.WithOrigins(builder.Configuration["Frontend:Url"]!)
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
    });
});

// Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "E-Commerce API", Version = "v1" });
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });
});

var app = builder.Build();

// Middleware pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseMiddleware<ExceptionHandlingMiddleware>();
app.UseMiddleware<RequestLoggingMiddleware>();

app.UseCors("AllowFrontend");
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
```

---

## 6. Frontend Architecture

### 6.1 Redux Store Configuration

```typescript
// app/store.ts
import { configureStore } from '@reduxjs/toolkit';
import { setupListeners } from '@reduxjs/toolkit/query';
import { api } from './api';
import authReducer from '../features/auth/authSlice';
import cartReducer from '../features/cart/cartSlice';
import uiReducer from '../features/ui/uiSlice';

export const store = configureStore({
  reducer: {
    [api.reducerPath]: api.reducer,
    auth: authReducer,
    cart: cartReducer,
    ui: uiReducer,
  },
  middleware: (getDefaultMiddleware) =>
    getDefaultMiddleware().concat(api.middleware),
});

setupListeners(store.dispatch);

export type RootState = ReturnType<typeof store.getState>;
export type AppDispatch = typeof store.dispatch;
```

### 6.2 RTK Query API Definition

```typescript
// app/api.ts
import { createApi, fetchBaseQuery } from '@reduxjs/toolkit/query/react';
import type { RootState } from './store';

export const api = createApi({
  reducerPath: 'api',
  baseQuery: fetchBaseQuery({
    baseUrl: import.meta.env.VITE_API_URL,
    prepareHeaders: (headers, { getState }) => {
      const token = (getState() as RootState).auth.token;
      if (token) {
        headers.set('Authorization', `Bearer ${token}`);
      }
      return headers;
    },
  }),
  tagTypes: ['Product', 'Category', 'Cart', 'Order', 'User', 'Review'],
  endpoints: () => ({}),
});

// features/products/productsApi.ts
import { api } from '../../app/api';
import type { Product, ProductsResponse, ProductQueryParams } from '../../types';

export const productsApi = api.injectEndpoints({
  endpoints: (builder) => ({
    getProducts: builder.query<ProductsResponse, ProductQueryParams>({
      query: (params) => ({
        url: '/products',
        params,
      }),
      providesTags: (result) =>
        result
          ? [
              ...result.items.map(({ id }) => ({ type: 'Product' as const, id })),
              { type: 'Product', id: 'LIST' },
            ]
          : [{ type: 'Product', id: 'LIST' }],
    }),
    getProductBySlug: builder.query<Product, string>({
      query: (slug) => `/products/${slug}`,
      providesTags: (result) =>
        result ? [{ type: 'Product', id: result.id }] : [],
    }),
    // ... more endpoints
  }),
});

export const { useGetProductsQuery, useGetProductBySlugQuery } = productsApi;
```

### 6.3 Auth Slice

```typescript
// features/auth/authSlice.ts
import { createSlice, PayloadAction } from '@reduxjs/toolkit';
import type { User } from '../../types';

interface AuthState {
  user: User | null;
  token: string | null;
  isAuthenticated: boolean;
  isLoading: boolean;
}

const initialState: AuthState = {
  user: null,
  token: localStorage.getItem('token'),
  isAuthenticated: false,
  isLoading: true,
};

const authSlice = createSlice({
  name: 'auth',
  initialState,
  reducers: {
    setCredentials: (
      state,
      action: PayloadAction<{ user: User; token: string }>
    ) => {
      state.user = action.payload.user;
      state.token = action.payload.token;
      state.isAuthenticated = true;
      state.isLoading = false;
      localStorage.setItem('token', action.payload.token);
    },
    logout: (state) => {
      state.user = null;
      state.token = null;
      state.isAuthenticated = false;
      state.isLoading = false;
      localStorage.removeItem('token');
    },
    setLoading: (state, action: PayloadAction<boolean>) => {
      state.isLoading = action.payload;
    },
  },
});

export const { setCredentials, logout, setLoading } = authSlice.actions;
export default authSlice.reducer;
```

### 6.4 Cart Slice (with persistence)

```typescript
// features/cart/cartSlice.ts
import { createSlice, PayloadAction } from '@reduxjs/toolkit';
import type { CartItem } from '../../types';

interface CartState {
  items: CartItem[];
  sessionId: string | null;
}

const loadCartFromStorage = (): CartState => {
  try {
    const saved = localStorage.getItem('cart');
    if (saved) {
      return JSON.parse(saved);
    }
  } catch (e) {
    console.error('Failed to load cart from storage');
  }
  return {
    items: [],
    sessionId: crypto.randomUUID(),
  };
};

const initialState: CartState = loadCartFromStorage();

const cartSlice = createSlice({
  name: 'cart',
  initialState,
  reducers: {
    addItem: (state, action: PayloadAction<CartItem>) => {
      const existingIndex = state.items.findIndex(
        (item) => item.productId === action.payload.productId
      );
      if (existingIndex >= 0) {
        state.items[existingIndex].quantity += action.payload.quantity;
      } else {
        state.items.push(action.payload);
      }
      localStorage.setItem('cart', JSON.stringify(state));
    },
    updateQuantity: (
      state,
      action: PayloadAction<{ productId: string; quantity: number }>
    ) => {
      const item = state.items.find(
        (i) => i.productId === action.payload.productId
      );
      if (item) {
        item.quantity = action.payload.quantity;
      }
      localStorage.setItem('cart', JSON.stringify(state));
    },
    removeItem: (state, action: PayloadAction<string>) => {
      state.items = state.items.filter((i) => i.productId !== action.payload);
      localStorage.setItem('cart', JSON.stringify(state));
    },
    clearCart: (state) => {
      state.items = [];
      state.sessionId = crypto.randomUUID();
      localStorage.setItem('cart', JSON.stringify(state));
    },
    syncCart: (state, action: PayloadAction<CartItem[]>) => {
      state.items = action.payload;
      localStorage.setItem('cart', JSON.stringify(state));
    },
  },
});

export const { addItem, updateQuantity, removeItem, clearCart, syncCart } =
  cartSlice.actions;
export default cartSlice.reducer;

// Selectors
export const selectCartItems = (state: { cart: CartState }) => state.cart.items;
export const selectCartTotal = (state: { cart: CartState }) =>
  state.cart.items.reduce((sum, item) => sum + item.price * item.quantity, 0);
export const selectCartCount = (state: { cart: CartState }) =>
  state.cart.items.reduce((sum, item) => sum + item.quantity, 0);
```

### 6.5 Routing Structure

```typescript
// routes/index.tsx
import { createBrowserRouter, RouterProvider } from 'react-router-dom';
import { lazy, Suspense } from 'react';
import Layout from '../components/layout/Layout';
import ProtectedRoute from './ProtectedRoute';
import Loading from '../components/common/Loading';

const HomePage = lazy(() => import('../pages/HomePage'));
const ProductsPage = lazy(() => import('../pages/ProductsPage'));
const ProductDetailPage = lazy(() => import('../pages/ProductDetailPage'));
const CartPage = lazy(() => import('../pages/CartPage'));
const CheckoutPage = lazy(() => import('../pages/CheckoutPage'));
const OrderConfirmationPage = lazy(() => import('../pages/OrderConfirmationPage'));
const LoginPage = lazy(() => import('../pages/LoginPage'));
const RegisterPage = lazy(() => import('../pages/RegisterPage'));
const AccountPage = lazy(() => import('../pages/AccountPage'));
const OrdersPage = lazy(() => import('../pages/OrdersPage'));
const OrderDetailPage = lazy(() => import('../pages/OrderDetailPage'));
const WishlistPage = lazy(() => import('../pages/WishlistPage'));
const NotFoundPage = lazy(() => import('../pages/NotFoundPage'));

const router = createBrowserRouter([
  {
    path: '/',
    element: <Layout />,
    children: [
      { index: true, element: <HomePage /> },
      { path: 'products', element: <ProductsPage /> },
      { path: 'products/:slug', element: <ProductDetailPage /> },
      { path: 'category/:slug', element: <ProductsPage /> },
      { path: 'cart', element: <CartPage /> },
      { path: 'checkout', element: <CheckoutPage /> },
      { path: 'order-confirmation/:orderId', element: <OrderConfirmationPage /> },
      { path: 'login', element: <LoginPage /> },
      { path: 'register', element: <RegisterPage /> },
      {
        path: 'account',
        element: (
          <ProtectedRoute>
            <AccountPage />
          </ProtectedRoute>
        ),
      },
      {
        path: 'orders',
        element: (
          <ProtectedRoute>
            <OrdersPage />
          </ProtectedRoute>
        ),
      },
      {
        path: 'orders/:id',
        element: (
          <ProtectedRoute>
            <OrderDetailPage />
          </ProtectedRoute>
        ),
      },
      {
        path: 'wishlist',
        element: (
          <ProtectedRoute>
            <WishlistPage />
          </ProtectedRoute>
        ),
      },
      { path: '*', element: <NotFoundPage /> },
    ],
  },
]);

export default function AppRouter() {
  return (
    <Suspense fallback={<Loading />}>
      <RouterProvider router={router} />
    </Suspense>
  );
}
```

---

## 7. Authentication & Authorization

### 7.1 Auth Flow Diagram

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                         AUTHENTICATION FLOWS                                 │
└─────────────────────────────────────────────────────────────────────────────┘

1. EMAIL/PASSWORD REGISTRATION:
┌──────────┐    POST /auth/register    ┌──────────┐    Verify Email    ┌──────────┐
│  Client  │ ──────────────────────► │  Server  │ ──────────────────► │  User    │
│          │ {email, password, name}   │          │  (SendGrid)         │  Inbox   │
└──────────┘                           └──────────┘                     └──────────┘
                                            │
                                            ▼
                                    ┌──────────────┐
                                    │ Create User  │
                                    │ (unverified) │
                                    └──────────────┘

2. EMAIL/PASSWORD LOGIN:
┌──────────┐    POST /auth/login       ┌──────────┐
│  Client  │ ──────────────────────► │  Server  │
│          │ {email, password}         │          │
└──────────┘                           └──────────┘
     ▲                                      │
     │                                      ▼
     │                              ┌──────────────┐
     │                              │ Validate     │
     │                              │ Credentials  │
     │                              └──────────────┘
     │                                      │
     │     {token, user}                    ▼
     └────────────────────────────── Generate JWT

3. OAUTH (GOOGLE/FACEBOOK):
┌──────────┐    Redirect to Provider   ┌──────────┐    Callback        ┌──────────┐
│  Client  │ ──────────────────────► │  OAuth   │ ──────────────────► │  Server  │
│          │                           │ Provider │  (with code)        │          │
└──────────┘                           └──────────┘                     └──────────┘
     ▲                                                                       │
     │                                                                       ▼
     │                                                              ┌──────────────┐
     │                                                              │ Exchange code│
     │                                                              │ for user info│
     │                                                              └──────────────┘
     │                                                                       │
     │     {token, user}                                                     ▼
     └──────────────────────────────────────────────────────────── Create/Update User
                                                                    Generate JWT

4. GUEST CHECKOUT:
┌──────────┐    POST /checkout/guest   ┌──────────┐
│  Client  │ ──────────────────────► │  Server  │
│          │ {email, cart, address,    │          │
│          │  payment}                 │          │
└──────────┘                           └──────────┘
     ▲                                      │
     │                                      ▼
     │                              ┌──────────────┐
     │                              │ Process order│
     │                              │ (no account) │
     │                              └──────────────┘
     │                                      │
     │     {orderId, confirmation}          ▼
     └────────────────────────────── Send confirmation email
```

### 7.2 JWT Token Structure

```json
{
  "header": {
    "alg": "HS256",
    "typ": "JWT"
  },
  "payload": {
    "sub": "user-uuid",
    "email": "user@example.com",
    "name": "John Doe",
    "role": "Customer",
    "iat": 1699000000,
    "exp": 1699086400
  }
}
```

### 7.3 Role-Based Authorization

```csharp
// Roles enum
public enum UserRole
{
    Customer,
    Admin,
    SuperAdmin
}

// Authorization policies
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("RequireAdmin", policy =>
        policy.RequireRole("Admin", "SuperAdmin"));

    options.AddPolicy("RequireSuperAdmin", policy =>
        policy.RequireRole("SuperAdmin"));
});

// Controller usage
[Authorize(Policy = "RequireAdmin")]
[ApiController]
[Route("api/v1/admin/products")]
public class AdminProductsController : ControllerBase
{
    // Admin-only endpoints
}
```

---

## 8. Payment Integration

### 8.1 Payment Flow

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                           PAYMENT FLOW                                       │
└─────────────────────────────────────────────────────────────────────────────┘

STRIPE CHECKOUT:
┌──────────┐   1. Create PaymentIntent   ┌──────────┐   2. Create Intent    ┌──────────┐
│  Client  │ ───────────────────────► │  Server  │ ───────────────────► │  Stripe  │
│          │   {amount, currency}        │          │                       │          │
└──────────┘                             └──────────┘                       └──────────┘
     │                                        ▲                                  │
     │                                        │ 3. client_secret                 │
     │                                        └──────────────────────────────────┘
     │
     │   4. Confirm payment (Stripe.js)
     │   ┌──────────────────────────────────────────────────────────────────────┐
     │   │                                                                      │
     ▼   ▼                                                                      │
┌──────────┐   5. Webhook: payment_intent.succeeded    ┌──────────┐            │
│  Stripe  │ ─────────────────────────────────────► │  Server  │            │
│ Elements │                                          │          │            │
└──────────┘                                          └──────────┘            │
                                                           │                   │
                                                           ▼                   │
                                                    ┌──────────────┐           │
                                                    │ Update Order │           │
                                                    │ Status       │           │
                                                    └──────────────┘           │
                                                           │                   │
                                                           ▼                   │
                                                    ┌──────────────┐           │
                                                    │ Send         │           │
                                                    │ Confirmation │           │
                                                    └──────────────┘           │

PAYPAL CHECKOUT:
┌──────────┐   1. Create Order         ┌──────────┐   2. Create Order    ┌──────────┐
│  Client  │ ─────────────────────► │  Server  │ ─────────────────► │  PayPal  │
│          │   {items, amount}         │          │                     │          │
└──────────┘                           └──────────┘                     └──────────┘
     │                                      ▲                                │
     │                                      │ 3. orderID                     │
     │                                      └────────────────────────────────┘
     │
     │   4. PayPal Checkout popup
     ▼
┌──────────┐   5. onApprove callback    ┌──────────┐
│  PayPal  │ ─────────────────────► │  Server  │
│  Popup   │   {orderID}               │          │
└──────────┘                           └──────────┘
                                            │
                                            ▼
                                     ┌──────────────┐
                                     │ Capture      │
                                     │ Payment      │
                                     └──────────────┘
```

### 8.2 Payment Service Implementation

```csharp
// IPaymentService.cs
public interface IPaymentService
{
    Task<PaymentIntentResult> CreatePaymentIntentAsync(CreatePaymentRequest request);
    Task<PaymentResult> ConfirmPaymentAsync(string paymentIntentId);
    Task<RefundResult> RefundPaymentAsync(string paymentIntentId, decimal amount);
}

// StripePaymentService.cs
public class StripePaymentService : IPaymentService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<StripePaymentService> _logger;

    public StripePaymentService(IConfiguration configuration, ILogger<StripePaymentService> logger)
    {
        _configuration = configuration;
        _logger = logger;
        StripeConfiguration.ApiKey = _configuration["Stripe:SecretKey"];
    }

    public async Task<PaymentIntentResult> CreatePaymentIntentAsync(CreatePaymentRequest request)
    {
        var options = new PaymentIntentCreateOptions
        {
            Amount = (long)(request.Amount * 100), // Convert to cents
            Currency = request.Currency.ToLower(),
            AutomaticPaymentMethods = new PaymentIntentAutomaticPaymentMethodsOptions
            {
                Enabled = true,
            },
            Metadata = new Dictionary<string, string>
            {
                { "orderId", request.OrderId.ToString() }
            }
        };

        var service = new PaymentIntentService();
        var paymentIntent = await service.CreateAsync(options);

        return new PaymentIntentResult
        {
            PaymentIntentId = paymentIntent.Id,
            ClientSecret = paymentIntent.ClientSecret,
            Status = paymentIntent.Status
        };
    }

    // ... other methods
}
```

### 8.3 Webhook Handler

```csharp
[ApiController]
[Route("api/v1/webhooks")]
public class WebhooksController : ControllerBase
{
    private readonly IOrderService _orderService;
    private readonly IConfiguration _configuration;
    private readonly ILogger<WebhooksController> _logger;

    [HttpPost("stripe")]
    public async Task<IActionResult> StripeWebhook()
    {
        var json = await new StreamReader(HttpContext.Request.Body).ReadToEndAsync();
        var stripeSignature = Request.Headers["Stripe-Signature"];

        try
        {
            var stripeEvent = EventUtility.ConstructEvent(
                json,
                stripeSignature,
                _configuration["Stripe:WebhookSecret"]
            );

            switch (stripeEvent.Type)
            {
                case Events.PaymentIntentSucceeded:
                    var paymentIntent = stripeEvent.Data.Object as PaymentIntent;
                    await HandlePaymentSucceededAsync(paymentIntent);
                    break;

                case Events.PaymentIntentPaymentFailed:
                    var failedPayment = stripeEvent.Data.Object as PaymentIntent;
                    await HandlePaymentFailedAsync(failedPayment);
                    break;
            }

            return Ok();
        }
        catch (StripeException e)
        {
            _logger.LogError(e, "Stripe webhook error");
            return BadRequest();
        }
    }

    private async Task HandlePaymentSucceededAsync(PaymentIntent paymentIntent)
    {
        var orderId = Guid.Parse(paymentIntent.Metadata["orderId"]);
        await _orderService.UpdatePaymentStatusAsync(orderId, PaymentStatus.Paid);
        await _orderService.UpdateOrderStatusAsync(orderId, OrderStatus.Processing);
    }
}
```

---

## 9. Admin Panel

### 9.1 Admin Dashboard Features

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                         ADMIN DASHBOARD                                      │
├─────────────────────────────────────────────────────────────────────────────┤
│                                                                             │
│  ┌─────────────┐  ┌─────────────┐  ┌─────────────┐  ┌─────────────┐       │
│  │   Revenue   │  │   Orders    │  │  Customers  │  │  Products   │       │
│  │   $24,500   │  │    156      │  │    1,234    │  │    89       │       │
│  │   ↑ 12%     │  │   ↑ 8%      │  │   ↑ 15%     │  │   ↓ 3%      │       │
│  └─────────────┘  └─────────────┘  └─────────────┘  └─────────────┘       │
│                                                                             │
│  ┌─────────────────────────────────┐  ┌─────────────────────────────────┐  │
│  │      Sales Chart (30 days)      │  │      Top Selling Products       │  │
│  │  ▃▄▅▆▇█▆▅▄▃▂▁▂▃▄▅▆▇█▆▅▄▃▂▁▂▃▄ │  │  1. Product A - 234 units       │  │
│  │                                 │  │  2. Product B - 189 units       │  │
│  │                                 │  │  3. Product C - 156 units       │  │
│  └─────────────────────────────────┘  └─────────────────────────────────┘  │
│                                                                             │
│  ┌─────────────────────────────────┐  ┌─────────────────────────────────┐  │
│  │       Recent Orders             │  │      Low Stock Alerts           │  │
│  │  #1234 - John D. - $125.00     │  │  ⚠ Product X - 3 left           │  │
│  │  #1233 - Jane S. - $89.50      │  │  ⚠ Product Y - 5 left           │  │
│  │  #1232 - Bob M. - $234.00      │  │  ⚠ Product Z - 8 left           │  │
│  └─────────────────────────────────┘  └─────────────────────────────────┘  │
│                                                                             │
└─────────────────────────────────────────────────────────────────────────────┘
```

### 9.2 Admin Module Structure

```typescript
// Admin app structure (frontend/apps/admin/)
admin/
├── src/
│   ├── app/
│   │   ├── store.ts
│   │   └── adminApi.ts
│   ├── features/
│   │   ├── dashboard/
│   │   │   ├── DashboardPage.tsx
│   │   │   └── components/
│   │   │       ├── StatsCard.tsx
│   │   │       ├── SalesChart.tsx
│   │   │       ├── RecentOrders.tsx
│   │   │       └── LowStockAlerts.tsx
│   │   ├── products/
│   │   │   ├── ProductsPage.tsx
│   │   │   ├── ProductFormPage.tsx
│   │   │   └── components/
│   │   │       ├── ProductsTable.tsx
│   │   │       ├── ProductForm.tsx
│   │   │       └── ImageUploader.tsx
│   │   ├── categories/
│   │   │   ├── CategoriesPage.tsx
│   │   │   └── components/
│   │   │       ├── CategoryTree.tsx
│   │   │       └── CategoryForm.tsx
│   │   ├── orders/
│   │   │   ├── OrdersPage.tsx
│   │   │   ├── OrderDetailPage.tsx
│   │   │   └── components/
│   │   │       ├── OrdersTable.tsx
│   │   │       ├── OrderStatusBadge.tsx
│   │   │       └── OrderTimeline.tsx
│   │   ├── customers/
│   │   │   ├── CustomersPage.tsx
│   │   │   ├── CustomerDetailPage.tsx
│   │   │   └── components/
│   │   │       ├── CustomersTable.tsx
│   │   │       └── CustomerOrders.tsx
│   │   ├── inventory/
│   │   │   ├── InventoryPage.tsx
│   │   │   └── components/
│   │   │       └── InventoryTable.tsx
│   │   ├── promo-codes/
│   │   │   ├── PromoCodesPage.tsx
│   │   │   └── components/
│   │   │       ├── PromoCodesTable.tsx
│   │   │       └── PromoCodeForm.tsx
│   │   ├── reviews/
│   │   │   └── ReviewsPage.tsx
│   │   └── reports/
│   │       ├── SalesReportPage.tsx
│   │       ├── InventoryReportPage.tsx
│   │       └── CustomerReportPage.tsx
│   ├── components/
│   │   ├── layout/
│   │   │   ├── AdminLayout.tsx
│   │   │   ├── Sidebar.tsx
│   │   │   └── Header.tsx
│   │   └── common/
│   │       ├── DataTable.tsx
│   │       ├── Pagination.tsx
│   │       ├── SearchInput.tsx
│   │       └── ConfirmDialog.tsx
│   └── routes/
│       └── index.tsx
```

### 9.3 Admin API Endpoints

```typescript
// adminApi.ts
export const adminApi = api.injectEndpoints({
  endpoints: (builder) => ({
    // Dashboard
    getDashboardStats: builder.query<DashboardStats, void>({
      query: () => '/admin/dashboard',
    }),

    // Products
    getAdminProducts: builder.query<PagedResponse<Product>, ProductQueryParams>({
      query: (params) => ({ url: '/admin/products', params }),
      providesTags: ['Product'],
    }),
    createProduct: builder.mutation<Product, CreateProductDto>({
      query: (body) => ({ url: '/admin/products', method: 'POST', body }),
      invalidatesTags: ['Product'],
    }),
    updateProduct: builder.mutation<Product, { id: string; data: UpdateProductDto }>({
      query: ({ id, data }) => ({
        url: `/admin/products/${id}`,
        method: 'PUT',
        body: data,
      }),
      invalidatesTags: ['Product'],
    }),
    deleteProduct: builder.mutation<void, string>({
      query: (id) => ({ url: `/admin/products/${id}`, method: 'DELETE' }),
      invalidatesTags: ['Product'],
    }),

    // Orders
    getAdminOrders: builder.query<PagedResponse<Order>, OrderQueryParams>({
      query: (params) => ({ url: '/admin/orders', params }),
      providesTags: ['Order'],
    }),
    updateOrderStatus: builder.mutation<Order, { id: string; status: OrderStatus }>({
      query: ({ id, status }) => ({
        url: `/admin/orders/${id}/status`,
        method: 'PUT',
        body: { status },
      }),
      invalidatesTags: ['Order'],
    }),

    // Reports
    getSalesReport: builder.query<SalesReport, ReportParams>({
      query: (params) => ({ url: '/admin/reports/sales', params }),
    }),

    // ... more endpoints
  }),
});
```

---

## 10. Email & Notifications

### 10.1 Email Templates

| Template | Trigger | Content |
|----------|---------|---------|
| Welcome | User registration | Welcome message, verify email link |
| Email Verification | Registration, email change | Verification link |
| Password Reset | Forgot password request | Reset link (expires in 1 hour) |
| Order Confirmation | Order placed | Order details, items, total |
| Order Shipped | Order status → Shipped | Tracking info, estimated delivery |
| Order Delivered | Order status → Delivered | Delivery confirmation, review request |
| Abandoned Cart | Cart inactive 24h | Cart items, checkout link |
| Low Stock Alert | Admin notification | Product details, current stock |
| Newsletter | Marketing campaign | Promotional content |

### 10.2 Email Service Implementation

```csharp
// IEmailService.cs
public interface IEmailService
{
    Task SendWelcomeEmailAsync(string email, string name, string verificationLink);
    Task SendPasswordResetEmailAsync(string email, string resetLink);
    Task SendOrderConfirmationAsync(string email, Order order);
    Task SendShippingNotificationAsync(string email, Order order, string trackingNumber);
    Task SendAbandonedCartEmailAsync(string email, Cart cart);
    Task SendMarketingEmailAsync(string email, string subject, string htmlContent);
}

// SendGridEmailService.cs
public class SendGridEmailService : IEmailService
{
    private readonly ISendGridClient _client;
    private readonly IConfiguration _configuration;
    private readonly ILogger<SendGridEmailService> _logger;

    public SendGridEmailService(
        IConfiguration configuration,
        ILogger<SendGridEmailService> logger)
    {
        _configuration = configuration;
        _logger = logger;
        _client = new SendGridClient(configuration["SendGrid:ApiKey"]);
    }

    public async Task SendOrderConfirmationAsync(string email, Order order)
    {
        var msg = new SendGridMessage
        {
            From = new EmailAddress(
                _configuration["SendGrid:FromEmail"],
                _configuration["SendGrid:FromName"]
            ),
            Subject = $"Order Confirmation - #{order.OrderNumber}",
            TemplateId = _configuration["SendGrid:Templates:OrderConfirmation"]
        };

        msg.AddTo(new EmailAddress(email));
        msg.SetTemplateData(new
        {
            order_number = order.OrderNumber,
            order_date = order.CreatedAt.ToString("MMMM dd, yyyy"),
            items = order.Items.Select(i => new
            {
                name = i.ProductName,
                quantity = i.Quantity,
                price = i.UnitPrice.ToString("C"),
                total = i.TotalPrice.ToString("C"),
                image_url = i.ProductImageUrl
            }),
            subtotal = order.Subtotal.ToString("C"),
            shipping = order.ShippingAmount.ToString("C"),
            tax = order.TaxAmount.ToString("C"),
            total = order.TotalAmount.ToString("C"),
            shipping_address = new
            {
                name = $"{order.ShippingAddress.FirstName} {order.ShippingAddress.LastName}",
                street = order.ShippingAddress.StreetLine1,
                city = order.ShippingAddress.City,
                state = order.ShippingAddress.State,
                postal_code = order.ShippingAddress.PostalCode,
                country = order.ShippingAddress.Country
            }
        });

        var response = await _client.SendEmailAsync(msg);

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError("Failed to send order confirmation email: {StatusCode}",
                response.StatusCode);
        }
    }

    // ... other methods
}
```

### 10.3 Abandoned Cart Recovery

```csharp
// Background job for abandoned cart emails
public class AbandonedCartJob : IHostedService
{
    private readonly IServiceProvider _serviceProvider;
    private Timer? _timer;

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _timer = new Timer(ProcessAbandonedCarts, null, TimeSpan.Zero, TimeSpan.FromHours(1));
        return Task.CompletedTask;
    }

    private async void ProcessAbandonedCarts(object? state)
    {
        using var scope = _serviceProvider.CreateScope();
        var cartRepository = scope.ServiceProvider.GetRequiredService<ICartRepository>();
        var emailService = scope.ServiceProvider.GetRequiredService<IEmailService>();

        var abandonedCarts = await cartRepository.GetAbandonedCartsAsync(
            olderThan: TimeSpan.FromHours(24),
            notNotifiedWithin: TimeSpan.FromDays(7)
        );

        foreach (var cart in abandonedCarts)
        {
            if (!string.IsNullOrEmpty(cart.User?.Email))
            {
                await emailService.SendAbandonedCartEmailAsync(cart.User.Email, cart);
                await cartRepository.MarkAsNotifiedAsync(cart.Id);
            }
        }
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _timer?.Change(Timeout.Infinite, 0);
        return Task.CompletedTask;
    }
}
```

---

## 11. Third-Party Integrations

### 11.1 Integration Architecture

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                      THIRD-PARTY INTEGRATIONS                                │
└─────────────────────────────────────────────────────────────────────────────┘

┌──────────────────────────────────────────────────────────────────────────────┐
│                              FRONTEND                                         │
│  ┌─────────────────┐  ┌─────────────────┐  ┌─────────────────┐              │
│  │ Google Analytics│  │   Facebook      │  │    Hotjar       │              │
│  │     (GA4)       │  │     Pixel       │  │  (Optional)     │              │
│  └─────────────────┘  └─────────────────┘  └─────────────────┘              │
└──────────────────────────────────────────────────────────────────────────────┘

┌──────────────────────────────────────────────────────────────────────────────┐
│                              BACKEND                                          │
│  ┌─────────────────┐  ┌─────────────────┐  ┌─────────────────┐              │
│  │    SendGrid     │  │   Cloudinary    │  │    HubSpot      │              │
│  │    (Email)      │  │   (Images)      │  │    (CRM)        │              │
│  └─────────────────┘  └─────────────────┘  └─────────────────┘              │
│                                                                               │
│  ┌─────────────────┐  ┌─────────────────┐                                   │
│  │     Stripe      │  │     PayPal      │                                   │
│  │   (Payments)    │  │   (Payments)    │                                   │
│  └─────────────────┘  └─────────────────┘                                   │
└──────────────────────────────────────────────────────────────────────────────┘
```

### 11.2 Google Analytics 4 Integration

```typescript
// utils/analytics.ts
declare global {
  interface Window {
    gtag: (...args: any[]) => void;
    dataLayer: any[];
  }
}

export const GA_TRACKING_ID = import.meta.env.VITE_GA_TRACKING_ID;

export const pageview = (url: string) => {
  window.gtag('config', GA_TRACKING_ID, {
    page_path: url,
  });
};

export const event = ({ action, category, label, value }: {
  action: string;
  category: string;
  label?: string;
  value?: number;
}) => {
  window.gtag('event', action, {
    event_category: category,
    event_label: label,
    value: value,
  });
};

// E-commerce specific events
export const trackProductView = (product: Product) => {
  window.gtag('event', 'view_item', {
    currency: 'USD',
    value: product.price,
    items: [{
      item_id: product.id,
      item_name: product.name,
      category: product.category?.name,
      price: product.price,
    }],
  });
};

export const trackAddToCart = (product: Product, quantity: number) => {
  window.gtag('event', 'add_to_cart', {
    currency: 'USD',
    value: product.price * quantity,
    items: [{
      item_id: product.id,
      item_name: product.name,
      category: product.category?.name,
      price: product.price,
      quantity: quantity,
    }],
  });
};

export const trackPurchase = (order: Order) => {
  window.gtag('event', 'purchase', {
    transaction_id: order.orderNumber,
    value: order.totalAmount,
    currency: order.currency,
    shipping: order.shippingAmount,
    tax: order.taxAmount,
    items: order.items.map(item => ({
      item_id: item.productId,
      item_name: item.productName,
      price: item.unitPrice,
      quantity: item.quantity,
    })),
  });
};
```

### 11.3 HubSpot CRM Integration

```csharp
// IHubSpotService.cs
public interface IHubSpotService
{
    Task<string> CreateOrUpdateContactAsync(HubSpotContact contact);
    Task CreateDealAsync(Order order);
    Task TrackEventAsync(string email, string eventName, Dictionary<string, object> properties);
}

// HubSpotService.cs
public class HubSpotService : IHubSpotService
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;
    private readonly ILogger<HubSpotService> _logger;

    public HubSpotService(
        IHttpClientFactory httpClientFactory,
        IConfiguration configuration,
        ILogger<HubSpotService> logger)
    {
        _httpClient = httpClientFactory.CreateClient("HubSpot");
        _configuration = configuration;
        _logger = logger;

        _httpClient.BaseAddress = new Uri("https://api.hubapi.com/");
        _httpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", configuration["HubSpot:AccessToken"]);
    }

    public async Task<string> CreateOrUpdateContactAsync(HubSpotContact contact)
    {
        var payload = new
        {
            properties = new
            {
                email = contact.Email,
                firstname = contact.FirstName,
                lastname = contact.LastName,
                phone = contact.Phone,
                lifecyclestage = "customer"
            }
        };

        var response = await _httpClient.PostAsJsonAsync(
            $"crm/v3/objects/contacts",
            payload
        );

        if (response.IsSuccessStatusCode)
        {
            var result = await response.Content.ReadFromJsonAsync<HubSpotContactResponse>();
            return result!.Id;
        }

        // Handle duplicate - search and update
        var searchResponse = await SearchContactByEmailAsync(contact.Email);
        if (searchResponse != null)
        {
            await UpdateContactAsync(searchResponse.Id, payload);
            return searchResponse.Id;
        }

        throw new Exception("Failed to create or update HubSpot contact");
    }

    public async Task CreateDealAsync(Order order)
    {
        var payload = new
        {
            properties = new
            {
                dealname = $"Order #{order.OrderNumber}",
                amount = order.TotalAmount,
                dealstage = "closedwon",
                pipeline = "default"
            }
        };

        await _httpClient.PostAsJsonAsync("crm/v3/objects/deals", payload);
    }
}
```

### 11.4 Cloudinary Image Service

```csharp
// CloudinaryImageService.cs
public class CloudinaryImageService : IImageService
{
    private readonly Cloudinary _cloudinary;
    private readonly ILogger<CloudinaryImageService> _logger;

    public CloudinaryImageService(IConfiguration configuration, ILogger<CloudinaryImageService> logger)
    {
        _logger = logger;

        var account = new Account(
            configuration["Cloudinary:CloudName"],
            configuration["Cloudinary:ApiKey"],
            configuration["Cloudinary:ApiSecret"]
        );

        _cloudinary = new Cloudinary(account);
    }

    public async Task<ImageUploadResult> UploadImageAsync(IFormFile file, string folder)
    {
        using var stream = file.OpenReadStream();

        var uploadParams = new ImageUploadParams
        {
            File = new FileDescription(file.FileName, stream),
            Folder = $"ecommerce/{folder}",
            Transformation = new Transformation()
                .Quality("auto")
                .FetchFormat("auto")
        };

        var result = await _cloudinary.UploadAsync(uploadParams);

        return new ImageUploadResult
        {
            Url = result.SecureUrl.ToString(),
            PublicId = result.PublicId,
            Width = result.Width,
            Height = result.Height
        };
    }

    public async Task DeleteImageAsync(string publicId)
    {
        var deleteParams = new DeletionParams(publicId);
        await _cloudinary.DestroyAsync(deleteParams);
    }

    public string GetOptimizedUrl(string publicId, int width, int height)
    {
        return _cloudinary.Api.UrlImgUp
            .Transform(new Transformation()
                .Width(width)
                .Height(height)
                .Crop("fill")
                .Quality("auto")
                .FetchFormat("auto"))
            .BuildUrl(publicId);
    }
}
```

---

## 12. Deployment Strategy

### 12.1 Render.com Configuration

```yaml
# render.yaml
services:
  # Backend API
  - type: web
    name: ecommerce-api
    env: docker
    dockerfilePath: ./src/backend/Dockerfile
    envVars:
      - key: ASPNETCORE_ENVIRONMENT
        value: Production
      - key: DATABASE_URL
        fromDatabase:
          name: ecommerce-db
          property: connectionString
      - key: JWT_SECRET_KEY
        generateValue: true
      - key: STRIPE_SECRET_KEY
        sync: false
      - key: STRIPE_WEBHOOK_SECRET
        sync: false
      - key: SENDGRID_API_KEY
        sync: false
      - key: CLOUDINARY_URL
        sync: false
      - key: GOOGLE_CLIENT_ID
        sync: false
      - key: GOOGLE_CLIENT_SECRET
        sync: false
      - key: HUBSPOT_ACCESS_TOKEN
        sync: false
    healthCheckPath: /health
    autoDeploy: true

  # Customer Storefront
  - type: web
    name: ecommerce-storefront
    env: static
    buildCommand: cd src/frontend/apps/storefront && npm install && npm run build
    staticPublishPath: ./src/frontend/apps/storefront/dist
    headers:
      - path: /*
        name: Cache-Control
        value: public, max-age=31536000
    routes:
      - type: rewrite
        source: /*
        destination: /index.html
    envVars:
      - key: VITE_API_URL
        value: https://ecommerce-api.onrender.com/api/v1
      - key: VITE_STRIPE_PUBLIC_KEY
        sync: false
      - key: VITE_GA_TRACKING_ID
        sync: false

  # Admin Dashboard
  - type: web
    name: ecommerce-admin
    env: static
    buildCommand: cd src/frontend/apps/admin && npm install && npm run build
    staticPublishPath: ./src/frontend/apps/admin/dist
    routes:
      - type: rewrite
        source: /*
        destination: /index.html
    envVars:
      - key: VITE_API_URL
        value: https://ecommerce-api.onrender.com/api/v1

databases:
  - name: ecommerce-db
    databaseName: ecommerce
    user: ecommerce_user
    plan: free
```

### 12.2 Docker Configuration

```dockerfile
# src/backend/Dockerfile
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy csproj files and restore
COPY ["ECommerce.API/ECommerce.API.csproj", "ECommerce.API/"]
COPY ["ECommerce.Core/ECommerce.Core.csproj", "ECommerce.Core/"]
COPY ["ECommerce.Application/ECommerce.Application.csproj", "ECommerce.Application/"]
COPY ["ECommerce.Infrastructure/ECommerce.Infrastructure.csproj", "ECommerce.Infrastructure/"]
RUN dotnet restore "ECommerce.API/ECommerce.API.csproj"

# Copy everything else and build
COPY . .
WORKDIR "/src/ECommerce.API"
RUN dotnet build "ECommerce.API.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "ECommerce.API.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=publish /app/publish .

# Health check
HEALTHCHECK --interval=30s --timeout=3s --start-period=5s --retries=3 \
  CMD curl --fail http://localhost:80/health || exit 1

EXPOSE 80
ENTRYPOINT ["dotnet", "ECommerce.API.dll"]
```

### 12.3 Environment Variables

```bash
# Backend (.env.production)
ASPNETCORE_ENVIRONMENT=Production
DATABASE_URL=postgresql://user:pass@host:5432/ecommerce

# JWT
JWT_SECRET_KEY=your-256-bit-secret-key
JWT_ISSUER=ecommerce-api
JWT_AUDIENCE=ecommerce-client
JWT_EXPIRY_HOURS=24

# Stripe
STRIPE_SECRET_KEY=sk_live_...
STRIPE_WEBHOOK_SECRET=whsec_...

# PayPal
PAYPAL_CLIENT_ID=...
PAYPAL_CLIENT_SECRET=...
PAYPAL_MODE=live

# SendGrid
SENDGRID_API_KEY=SG....
SENDGRID_FROM_EMAIL=noreply@yourdomain.com
SENDGRID_FROM_NAME=Your Store

# Cloudinary
CLOUDINARY_CLOUD_NAME=...
CLOUDINARY_API_KEY=...
CLOUDINARY_API_SECRET=...

# OAuth
GOOGLE_CLIENT_ID=...
GOOGLE_CLIENT_SECRET=...
FACEBOOK_APP_ID=...
FACEBOOK_APP_SECRET=...

# HubSpot
HUBSPOT_ACCESS_TOKEN=...

# Frontend URLs
FRONTEND_URL=https://yourdomain.com
ADMIN_URL=https://admin.yourdomain.com
```

### 12.4 CI/CD Pipeline (GitHub Actions)

```yaml
# .github/workflows/deploy.yml
name: Deploy to Render

on:
  push:
    branches: [main]
  pull_request:
    branches: [main]

jobs:
  test-backend:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4

      - name: Setup .NET
        uses: actions/setup-dotnet@v4
        with:
          dotnet-version: '8.0.x'

      - name: Restore dependencies
        run: dotnet restore src/backend/ECommerce.API/ECommerce.API.csproj

      - name: Build
        run: dotnet build src/backend/ECommerce.API/ECommerce.API.csproj --no-restore

      - name: Test
        run: dotnet test src/backend/ECommerce.Tests/ECommerce.Tests.csproj --no-build --verbosity normal

  test-frontend:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4

      - name: Setup Node.js
        uses: actions/setup-node@v4
        with:
          node-version: '20'
          cache: 'npm'
          cache-dependency-path: src/frontend/package-lock.json

      - name: Install dependencies
        run: npm ci
        working-directory: src/frontend

      - name: Lint
        run: npm run lint
        working-directory: src/frontend

      - name: Type check
        run: npm run type-check
        working-directory: src/frontend

      - name: Test
        run: npm run test
        working-directory: src/frontend

  deploy:
    needs: [test-backend, test-frontend]
    runs-on: ubuntu-latest
    if: github.ref == 'refs/heads/main'
    steps:
      - name: Deploy to Render
        run: |
          curl -X POST ${{ secrets.RENDER_DEPLOY_HOOK_URL }}
```

---

## 13. Security Considerations

### 13.1 Security Checklist

| Area | Implementation |
|------|----------------|
| **Authentication** | JWT with secure secret, short expiry, refresh tokens |
| **Password Storage** | BCrypt hashing with salt |
| **HTTPS** | Enforced on all endpoints (Render provides SSL) |
| **CORS** | Restricted to known frontend domains |
| **Input Validation** | FluentValidation on all inputs |
| **SQL Injection** | Parameterized queries via EF Core |
| **XSS** | React auto-escaping, CSP headers |
| **CSRF** | SameSite cookies, anti-forgery tokens |
| **Rate Limiting** | AspNetCoreRateLimit middleware |
| **Secrets** | Environment variables, never in code |
| **Dependencies** | Regular updates, Dependabot alerts |
| **Logging** | Serilog with sensitive data filtering |

### 13.2 Security Headers

```csharp
// Middleware for security headers
app.Use(async (context, next) =>
{
    context.Response.Headers.Add("X-Content-Type-Options", "nosniff");
    context.Response.Headers.Add("X-Frame-Options", "DENY");
    context.Response.Headers.Add("X-XSS-Protection", "1; mode=block");
    context.Response.Headers.Add("Referrer-Policy", "strict-origin-when-cross-origin");
    context.Response.Headers.Add("Content-Security-Policy",
        "default-src 'self'; " +
        "script-src 'self' https://js.stripe.com https://www.google-analytics.com; " +
        "style-src 'self' 'unsafe-inline'; " +
        "img-src 'self' data: https://res.cloudinary.com; " +
        "connect-src 'self' https://api.stripe.com https://www.google-analytics.com;");

    await next();
});
```

### 13.3 Rate Limiting Configuration

```csharp
// Rate limiting setup
builder.Services.AddMemoryCache();
builder.Services.Configure<IpRateLimitOptions>(options =>
{
    options.EnableEndpointRateLimiting = true;
    options.StackBlockedRequests = false;
    options.GeneralRules = new List<RateLimitRule>
    {
        new RateLimitRule
        {
            Endpoint = "*",
            Period = "1m",
            Limit = 60
        },
        new RateLimitRule
        {
            Endpoint = "POST:/api/v1/auth/*",
            Period = "1m",
            Limit = 10
        },
        new RateLimitRule
        {
            Endpoint = "POST:/api/v1/checkout/*",
            Period = "1m",
            Limit = 5
        }
    };
});
builder.Services.AddInMemoryRateLimiting();
builder.Services.AddSingleton<IRateLimitConfiguration, RateLimitConfiguration>();
```

---

## 14. Development Workflow

### 14.1 Local Development Setup

```bash
# 1. Clone repository
git clone https://github.com/your-org/ecommerce.git
cd ecommerce

# 2. Start PostgreSQL with Docker
docker-compose up -d postgres

# 3. Backend setup
cd src/backend
dotnet restore
dotnet ef database update --project ECommerce.Infrastructure --startup-project ECommerce.API
dotnet run --project ECommerce.API

# 4. Frontend setup (new terminal)
cd src/frontend
npm install
npm run dev:storefront  # Customer app on :3000
npm run dev:admin       # Admin app on :3001
```

### 14.2 Docker Compose for Local Development

```yaml
# docker-compose.yml
version: '3.8'

services:
  postgres:
    image: postgres:15-alpine
    container_name: ecommerce-db
    environment:
      POSTGRES_USER: postgres
      POSTGRES_PASSWORD: postgres
      POSTGRES_DB: ecommerce
    ports:
      - "5432:5432"
    volumes:
      - postgres_data:/var/lib/postgresql/data

  api:
    build:
      context: ./src/backend
      dockerfile: Dockerfile.dev
    container_name: ecommerce-api
    ports:
      - "5000:80"
    environment:
      - ASPNETCORE_ENVIRONMENT=Development
      - ConnectionStrings__DefaultConnection=Host=postgres;Database=ecommerce;Username=postgres;Password=postgres
    depends_on:
      - postgres
    volumes:
      - ./src/backend:/app
      - /app/bin
      - /app/obj

  storefront:
    build:
      context: ./src/frontend
      dockerfile: Dockerfile.dev
      args:
        APP: storefront
    container_name: ecommerce-storefront
    ports:
      - "3000:3000"
    environment:
      - VITE_API_URL=http://localhost:5000/api/v1
    volumes:
      - ./src/frontend:/app
      - /app/node_modules

  admin:
    build:
      context: ./src/frontend
      dockerfile: Dockerfile.dev
      args:
        APP: admin
    container_name: ecommerce-admin
    ports:
      - "3001:3000"
    environment:
      - VITE_API_URL=http://localhost:5000/api/v1
    volumes:
      - ./src/frontend:/app
      - /app/node_modules

volumes:
  postgres_data:
```

### 14.3 Git Workflow

```
main (production)
  │
  └── develop (staging)
        │
        ├── feature/user-auth
        ├── feature/product-catalog
        ├── feature/checkout-flow
        ├── bugfix/cart-calculation
        └── hotfix/payment-error
```

**Branch Naming Convention:**
- `feature/*` - New features
- `bugfix/*` - Bug fixes
- `hotfix/*` - Urgent production fixes
- `refactor/*` - Code refactoring
- `docs/*` - Documentation updates

### 14.4 Code Quality Tools

```json
// Frontend: package.json scripts
{
  "scripts": {
    "lint": "eslint . --ext .ts,.tsx",
    "lint:fix": "eslint . --ext .ts,.tsx --fix",
    "type-check": "tsc --noEmit",
    "format": "prettier --write \"src/**/*.{ts,tsx,css}\"",
    "test": "vitest",
    "test:coverage": "vitest --coverage"
  }
}
```

```xml
<!-- Backend: .editorconfig -->
root = true

[*.cs]
indent_style = space
indent_size = 4
end_of_line = lf
charset = utf-8
trim_trailing_whitespace = true
insert_final_newline = true

# Naming conventions
dotnet_naming_rule.private_fields_with_underscore.symbols = private_fields
dotnet_naming_rule.private_fields_with_underscore.style = prefix_underscore
dotnet_naming_rule.private_fields_with_underscore.severity = suggestion
```

---

## 15. API Documentation

### 15.1 Swagger/OpenAPI Setup

The API documentation is automatically generated using Swashbuckle and available at `/swagger` in development.

### 15.2 Sample API Responses

**GET /api/v1/products**
```json
{
  "items": [
    {
      "id": "550e8400-e29b-41d4-a716-446655440000",
      "name": "Premium Wireless Headphones",
      "slug": "premium-wireless-headphones",
      "shortDescription": "High-quality wireless headphones with noise cancellation",
      "price": 199.99,
      "compareAtPrice": 249.99,
      "images": [
        {
          "url": "https://res.cloudinary.com/.../headphones-1.jpg",
          "altText": "Headphones front view",
          "isPrimary": true
        }
      ],
      "category": {
        "id": "...",
        "name": "Electronics",
        "slug": "electronics"
      },
      "stockQuantity": 45,
      "isActive": true,
      "isFeatured": true,
      "averageRating": 4.5,
      "reviewCount": 128
    }
  ],
  "totalCount": 89,
  "page": 1,
  "pageSize": 20,
  "totalPages": 5
}
```

**POST /api/v1/checkout**
```json
// Request
{
  "shippingAddress": {
    "firstName": "John",
    "lastName": "Doe",
    "streetLine1": "123 Main St",
    "city": "New York",
    "state": "NY",
    "postalCode": "10001",
    "country": "US",
    "phone": "+1234567890"
  },
  "billingAddress": null,  // Same as shipping
  "paymentMethod": "stripe",
  "promoCode": "SAVE10"
}

// Response
{
  "orderId": "550e8400-e29b-41d4-a716-446655440001",
  "orderNumber": "ORD-2024-001234",
  "clientSecret": "pi_xxx_secret_xxx",  // Stripe PaymentIntent
  "totals": {
    "subtotal": 399.98,
    "discount": 39.99,
    "shipping": 9.99,
    "tax": 32.00,
    "total": 401.98
  }
}
```

---

## 16. Future Considerations

### 16.1 Scaling Path

When traffic grows beyond the initial scale:

1. **Database**: Migrate from Render PostgreSQL to managed service (AWS RDS, Azure Database)
2. **Caching**: Add Redis for session storage and API response caching
3. **CDN**: Implement Cloudflare or AWS CloudFront for static assets
4. **Search**: Add Elasticsearch or Algolia for advanced product search
5. **Queue**: Add message queue (RabbitMQ, AWS SQS) for async processing

### 16.2 Feature Roadmap

| Phase | Features |
|-------|----------|
| **Phase 2** | Product variants, advanced filtering, related products |
| **Phase 3** | Wishlist sharing, gift cards, loyalty points |
| **Phase 4** | Multi-language, multi-currency support |
| **Phase 5** | Mobile app (React Native), PWA support |
| **Phase 6** | AI recommendations, personalization |

### 16.3 Performance Optimization Targets

| Metric | Target |
|--------|--------|
| Time to First Byte (TTFB) | < 200ms |
| Largest Contentful Paint (LCP) | < 2.5s |
| First Input Delay (FID) | < 100ms |
| Cumulative Layout Shift (CLS) | < 0.1 |
| API Response Time (p95) | < 500ms |

---

## Appendix A: Estimated Service Costs (Render.com Free Tier)

| Service | Free Tier Limits | Estimated Monthly Cost |
|---------|------------------|------------------------|
| Web Service (API) | 750 hours | $0 (free) |
| Static Site (Storefront) | Unlimited | $0 (free) |
| Static Site (Admin) | Unlimited | $0 (free) |
| PostgreSQL | 1GB storage | $0 (free) |
| **Total** | | **$0/month** |

**Note**: Free tier has limitations (services sleep after 15 mins of inactivity). For production with consistent uptime, consider Render's paid plans starting at $7/month per service.

### External Services (Free Tiers)

| Service | Free Tier |
|---------|-----------|
| Stripe | Pay per transaction (2.9% + $0.30) |
| SendGrid | 100 emails/day |
| Cloudinary | 25 credits/month |
| HubSpot CRM | Free forever (limited features) |
| Google Analytics | Free |

---

## Appendix B: Checklist for Launch

- [ ] SSL certificates configured
- [ ] Environment variables set in Render
- [ ] Database migrations applied
- [ ] Stripe webhook endpoint configured
- [ ] SendGrid sender verification complete
- [ ] OAuth credentials (Google, Facebook) configured
- [ ] Admin user created
- [ ] Sample products added
- [ ] Email templates tested
- [ ] Payment flow tested (test mode)
- [ ] Error monitoring setup (Sentry optional)
- [ ] Analytics tracking verified
- [ ] Performance audit passed
- [ ] Security headers verified
- [ ] Backup strategy documented

---

*Document Version: 1.0*
*Last Updated: January 2026*
*Author: Senior Architect*
