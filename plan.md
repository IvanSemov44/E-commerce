# E-Commerce Platform - Master Plan

**Last Updated:** February 24, 2026  
**Status:** Active Development  
**Tech Stack:** ASP.NET Core 10 + React 19 + PostgreSQL + Docker

---

## Table of Contents

1. [Project Overview](#project-overview)
2. [Current Status](#current-status)
3. [Completed Features](#completed-features)
4. [Remaining Work](#remaining-work)
5. [Action Plan](#action-plan)
6. [Architecture Reference](#architecture-reference)

---

## Project Overview

Full-stack e-commerce platform with:
- **Storefront**: Customer-facing React application
- **Admin Dashboard**: Management interface for administrators
- **Backend API**: ASP.NET Core Web API with Clean Architecture
- **Database**: PostgreSQL with Entity Framework Core

### Key Technologies
| Layer | Technology |
|-------|------------|
| Frontend | React 19, TypeScript, Vite, Redux Toolkit |
| Backend | ASP.NET Core 10, EF Core, FluentValidation, AutoMapper |
| Database | PostgreSQL |
| Auth | JWT with refresh tokens |
| Container | Docker, Docker Compose |
| Payments | Mock (Stripe/PayPal integration pending) |

---

## Current Status

### Overall Progress: ~75% Complete

| Category | Status | Notes |
|----------|--------|-------|
| Core E-commerce | ✅ Complete | Products, Cart, Orders, Wishlist, Reviews |
| Authentication | ✅ Complete | JWT, Email Verification, Password Reset |
| Admin Dashboard | ✅ Complete | Products, Orders, Inventory, Promo Codes |
| Security | ⚠️ 50% | Middleware done, credentials need rotation |
| Payments | ⚠️ Mock Only | Real Stripe/PayPal integration needed |
| Checkout UX | ⚠️ 60% | Missing payment selection, trust signals |
| Legal Pages | ❌ Missing | Privacy, Terms, Cookie Consent |
| SEO | ❌ Missing | Meta tags, sitemap |

---

## Completed Features

### Backend API

| Feature | Controller | Status |
|---------|------------|--------|
| Products CRUD | [`ProductsController.cs`](src/backend/ECommerce.API/Controllers/ProductsController.cs) | ✅ |
| Categories | [`CategoriesController.cs`](src/backend/ECommerce.API/Controllers/CategoriesController.cs) | ✅ |
| Shopping Cart | [`CartController.cs`](src/backend/ECommerce.API/Controllers/CartController.cs) | ✅ |
| Orders | [`OrdersController.cs`](src/backend/ECommerce.API/Controllers/OrdersController.cs) | ✅ |
| Wishlist | [`WishlistController.cs`](src/backend/ECommerce.API/Controllers/WishlistController.cs) | ✅ |
| Reviews | [`ReviewsController.cs`](src/backend/ECommerce.API/Controllers/ReviewsController.cs) | ✅ |
| Promo Codes | [`PromoCodesController.cs`](src/backend/ECommerce.API/Controllers/PromoCodesController.cs) | ✅ |
| Authentication | [`AuthController.cs`](src/backend/ECommerce.API/Controllers/AuthController.cs) | ✅ |
| User Profile | [`ProfileController.cs`](src/backend/ECommerce.API/Controllers/ProfileController.cs) | ✅ |
| Inventory | [`InventoryController.cs`](src/backend/ECommerce.API/Controllers/InventoryController.cs) | ✅ |
| Dashboard Stats | [`DashboardController.cs`](src/backend/ECommerce.API/Controllers/DashboardController.cs) | ✅ |
| Payments (Mock) | [`PaymentsController.cs`](src/backend/ECommerce.API/Controllers/PaymentsController.cs) | ⚠️ |

### Security Middleware

| Feature | File | Status |
|---------|------|--------|
| Global Exception Handler | [`GlobalExceptionMiddleware.cs`](src/backend/ECommerce.API/Middleware/GlobalExceptionMiddleware.cs) | ✅ |
| Security Headers | [`SecurityHeadersMiddleware.cs`](src/backend/ECommerce.API/Middleware/SecurityHeadersMiddleware.cs) | ✅ |
| CSRF Protection | [`CsrfMiddleware.cs`](src/backend/ECommerce.API/Middleware/CsrfMiddleware.cs) | ✅ |

### Frontend - Storefront

| Page | File | Status |
|------|------|--------|
| Home | [`Home.tsx`](src/frontend/storefront/src/pages/Home.tsx) | ✅ |
| Products | [`Products.tsx`](src/frontend/storefront/src/pages/Products.tsx) | ✅ |
| Product Detail | [`ProductDetail.tsx`](src/frontend/storefront/src/pages/ProductDetail.tsx) | ✅ |
| Cart | [`Cart.tsx`](src/frontend/storefront/src/pages/Cart.tsx) | ✅ |
| Checkout | [`Checkout.tsx`](src/frontend/storefront/src/pages/Checkout.tsx) | ⚠️ |
| Login/Register | [`Login.tsx`](src/frontend/storefront/src/pages/Login.tsx), [`Register.tsx`](src/frontend/storefront/src/pages/Register.tsx) | ✅ |
| Profile | [`Profile.tsx`](src/frontend/storefront/src/pages/Profile.tsx) | ✅ |
| Order History | [`OrderHistory.tsx`](src/frontend/storefront/src/pages/OrderHistory.tsx) | ✅ |
| Order Detail | [`OrderDetail.tsx`](src/frontend/storefront/src/pages/OrderDetail.tsx) | ⚠️ |
| Wishlist | [`Wishlist.tsx`](src/frontend/storefront/src/pages/Wishlist.tsx) | ✅ |

### Frontend - Admin Dashboard

| Page | Status |
|------|--------|
| Dashboard | ✅ |
| Products Management | ✅ |
| Orders Management | ✅ |
| Inventory Management | ✅ |
| Promo Codes | ✅ |
| Reviews Moderation | ✅ |
| Customers | ✅ |

### Infrastructure

| Feature | Status |
|---------|--------|
| Docker Compose | ✅ |
| PostgreSQL Database | ✅ |
| Clean Architecture | ✅ |
| Email Service (SendGrid/SMTP) | ✅ |
| Toast Notifications | ✅ |
| Error Boundaries | ✅ |
| Loading Skeletons | ✅ |

---

## Remaining Work

### Priority 1: Security Fixes (CRITICAL)

- [ ] **Rotate Exposed Credentials**
  - Revoke SendGrid API key
  - Change Gmail app password
  - Generate new JWT secret
  - Update all secrets in environment variables

- [ ] **Clean Git History**
  - Remove sensitive data from git history using BFG or git-filter-repo
  - Force push cleaned repository

- [ ] **Secure Secrets Management**
  - Use .NET User Secrets for development
  - Environment variables for production
  - Update `.gitignore` to prevent future leaks

### Priority 2: Checkout Completion

- [ ] **Payment Method Selection UI**
  - Create payment method selector component
  - Support: Credit Card, PayPal, Apple Pay, Google Pay
  - Backend already supports these via `GET /api/payments/methods`

- [x] **Trust Signals** - [`TrustSignals.tsx`](src/frontend/storefront/src/components/TrustSignals.tsx)
  - Added to checkout page: Secure Checkout, Free Shipping, 30-Day Returns, Buyer Protection
  - Two variants: full (checkout) and compact (product pages)

- [x] **Guest Checkout**
  - Frontend now sends `guestEmail` field
  - Backend already supports it (`Order.GuestEmail` exists)
  - Added account creation prompt for guest orders on success page
  - Form pre-fills for authenticated users
  - Email field prominent for guests

- [x] **Order Status Timeline**
  - Created [`OrderStatusTimeline.tsx`](src/frontend/storefront/src/pages/components/OrderDetail/OrderStatusTimeline.tsx)
  - Visual stepper showing: Pending → Processing → Shipped → Delivered
  - Special handling for Cancelled orders
  - Responsive design for mobile

- [ ] **Tracking Number Support**
  - Add `TrackingNumber` field to `Order` entity
  - Show in OrderDetail page
  - Allow admin to enter when marking as shipped

### Priority 3: Legal Compliance

- [x] **Privacy Policy Page** (GDPR requirement) - [`PrivacyPolicy.tsx`](src/frontend/storefront/src/pages/PrivacyPolicy.tsx)
- [x] **Terms of Service Page** - [`TermsOfService.tsx`](src/frontend/storefront/src/pages/TermsOfService.tsx)
- [x] **Returns Policy Page** - [`ReturnsPolicy.tsx`](src/frontend/storefront/src/pages/ReturnsPolicy.tsx)
- [x] **Cookie Consent Banner** - Already implemented via [`CookieConsent.tsx`](src/frontend/storefront/src/components/CookieConsent.tsx)
- [x] **Fix Footer Links** - All footer links now point to correct pages:
  - Company: About Us, Careers, Press, Blog
  - Support: Help Center, Contact Us, Track Order, Returns
  - Legal: Privacy Policy, Terms of Service, Cookies, Security
- [x] **Additional Pages Created**:
  - [`AboutUs.tsx`](src/frontend/storefront/src/pages/AboutUs.tsx) - Company information
  - [`Careers.tsx`](src/frontend/storefront/src/pages/Careers.tsx) - Job opportunities
  - [`Press.tsx`](src/frontend/storefront/src/pages/Press.tsx) - Media resources
  - [`Blog.tsx`](src/frontend/storefront/src/pages/Blog.tsx) - Blog placeholder
  - [`HelpCenter.tsx`](src/frontend/storefront/src/pages/HelpCenter.tsx) - FAQ and support
  - [`Contact.tsx`](src/frontend/storefront/src/pages/Contact.tsx) - Contact information
  - [`TrackOrder.tsx`](src/frontend/storefront/src/pages/TrackOrder.tsx) - Order tracking info
  - [`CookiePolicy.tsx`](src/frontend/storefront/src/pages/CookiePolicy.tsx) - Cookie policy
  - [`Security.tsx`](src/frontend/storefront/src/pages/Security.tsx) - Security information

### Priority 4: Homepage Enhancement

- [x] **Announcement Bar** (top of page) - [`AnnouncementBar.tsx`](src/frontend/storefront/src/components/AnnouncementBar.tsx)
  - Dismissible banner with customizable message and link
  - Default: "Free shipping on orders over $50!"
- [x] **Trust Signals Section** - Added to [`Home.tsx`](src/frontend/storefront/src/pages/Home.tsx)
  - Uses existing [`TrustSignals.tsx`](src/frontend/storefront/src/components/TrustSignals.tsx) component
  - Displays: Secure Checkout, Free Shipping, 30-Day Returns, Buyer Protection
- [x] **Category Showcase** - Added to [`Home.tsx`](src/frontend/storefront/src/pages/Home.tsx)
  - Uses `useGetTopLevelCategoriesQuery` from [`categoriesApi.ts`](src/frontend/storefront/src/store/api/categoriesApi.ts)
  - Displays up to 6 top-level categories with images or placeholder icons
  - Links to filtered products page
- [x] **Bestsellers Section** - Added to [`Home.tsx`](src/frontend/storefront/src/pages/Home.tsx)
  - Uses `useGetProductsQuery` with `sortBy=reviewCount&sortOrder=desc`
  - Shows top 4 products by review count
  - "View All Bestsellers" link to products page
- [x] **Promotions Section** - Added to [`Home.tsx`](src/frontend/storefront/src/pages/Home.tsx)
  - Filters products with `compareAtPrice` (on sale)
  - Shows up to 4 promotional products
  - "View All Offers" link
- [x] **Working Newsletter Subscription** - Updated [`Footer.tsx`](src/frontend/storefront/src/components/Footer.tsx)
  - Added form state management with email validation
  - Shows success/error toast notifications via `useToast` hook
  - Stores subscribers in localStorage (demo implementation)
  - Prevents duplicate subscriptions

### Priority 5: Payment Integration

- [ ] **Stripe Integration**
  - Install Stripe.NET SDK
  - Create real payment processing
  - Handle webhooks

- [ ] **Apple Pay / Google Pay**
  - Integrate via Stripe Payment Element
  - One-tap payment support

### Priority 6: Product Experience

- [ ] **Product Variants** (size, color)
- [ ] **Quick View Modal**
- [ ] **Recently Viewed Products**
- [x] **Live Search** (debounced, with preview) - Updated [`SearchBar.tsx`](src/frontend/storefront/src/components/SearchBar.tsx)
  - 300ms debounce on search input
  - Shows up to 5 product results with images and prices
  - Keyboard navigation (arrow keys, enter to select)
  - Loading spinner while fetching
  - "View all results" link to full search page
  - Click-outside-to-close behavior

### Priority 7: Order Management

- [ ] **Courier Integration** (Econt/Speedy for Bulgaria)
- [ ] **Abandoned Cart Emails** (backend exists, needs scheduling)
- [ ] **Public Order Tracking Page**

### Priority 8: SEO & Performance

- [ ] **Dynamic Meta Tags** (react-helmet-async)
- [ ] **Sitemap Generator**
- [ ] **PWA Support**
- [ ] **Performance Optimizations**

### Priority 9: Advanced Features

- [ ] **Google Places API** (address autocomplete)
- [ ] **Loyalty Points System**
- [ ] **Video Reviews Support**
- [ ] **Framer Motion Animations**

---

## Action Plan

### Phase 1: Security (Immediate)
```
1. Rotate all exposed credentials
2. Clean git history
3. Set up proper secrets management
```

### Phase 2: Checkout Polish (1-2 weeks)
```
1. Add payment method selection UI
2. Add trust signals to checkout and product pages
3. Wire guest checkout frontend
4. Add order status timeline
5. Add tracking number support
```

### Phase 3: Legal & Trust (1 week)
```
1. Create Privacy Policy page
2. Create Terms of Service page
3. Add cookie consent banner
4. Fix footer links
```

### Phase 4: Homepage (1 week)
```
1. Add announcement bar
2. Add trust signals section
3. Add category showcase
4. Implement newsletter subscription
```

### Phase 5: Real Payments (1-2 weeks)
```
1. Integrate Stripe
2. Add Apple Pay / Google Pay
3. Test payment flows
```

### Phase 6: SEO & PWA (1 week)
```
1. Add dynamic meta tags
2. Generate sitemap
3. Add PWA manifest and service worker
```

---

## Architecture Reference

### Backend Structure (Clean Architecture)

```
src/backend/
├── ECommerce.API/           # Presentation layer (Controllers, Middleware)
├── ECommerce.Application/   # Business logic (Services, DTOs, Interfaces)
├── ECommerce.Core/          # Domain entities, Enums, Interfaces
├── ECommerce.Infrastructure/# Data access (Repositories, EF Core)
└── ECommerce.Tests/         # Unit and integration tests
```

### Frontend Structure

```
src/frontend/
├── storefront/              # Customer-facing app
│   └── src/
│       ├── components/      # Reusable UI components
│       ├── pages/           # Page components
│       ├── hooks/           # Custom React hooks
│       ├── store/           # Redux store, slices, API
│       └── types/           # TypeScript types
│
└── admin/                   # Admin dashboard
    └── src/
        ├── components/
        ├── pages/
        ├── hooks/
        └── store/
```

### Database Entities

| Entity | Description |
|--------|-------------|
| `User` | User accounts with roles |
| `Product` | Products with images, pricing |
| `Category` | Hierarchical categories |
| `Order` | Orders with status tracking |
| `OrderItem` | Line items with price snapshot |
| `Cart` / `CartItem` | Shopping cart |
| `Wishlist` | Saved products |
| `Review` | Product reviews and ratings |
| `PromoCode` | Discount codes |
| `Address` | Shipping/billing addresses |
| `InventoryLog` | Stock change history |

---

## Running the Project

### Using Docker (Recommended)
```bash
docker-compose up --build -d
```

### Access Points
- **Storefront**: http://localhost:5173
- **Admin Dashboard**: http://localhost:5177
- **Backend API**: http://localhost:5000
- **Swagger UI**: http://localhost:5000

### Environment Variables Required
```
# Database
DB_PASSWORD=your-db-password

# JWT
JWT_SECRET=your-jwt-secret-min-32-chars

# SendGrid
SENDGRID_API_KEY=your-sendgrid-api-key

# SMTP (optional)
SMTP_PASSWORD=your-smtp-password
```

---

## Notes

- This plan replaces all previous planning documents
- For implementation details, refer to the source code
- Keep this document updated as features are completed
