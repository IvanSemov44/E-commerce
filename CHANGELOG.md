# Changelog

All notable changes to this project are documented here.
Format follows [Keep a Changelog](https://keepachangelog.com/en/1.0.0/).

---

## [Unreleased]

### In progress
- Stripe payment integration (replaces mocked PaymentService)
- `[ProducesResponseType]` coverage on all endpoints (~87 remaining)

---

## [0.4.0] — 2026-03-18

### Changed
- **Checkout refactor** — removed `CheckoutContext` god-hook; split into three focused hooks:
  - `useCheckoutForm` — form state + localStorage draft persistence with debounced writes
  - `useCheckoutOrder` — order creation + stock validation
  - `useCheckoutPromo` — promo code application and discount calculation
- Memoized order payload calculation in checkout to prevent unnecessary re-renders

### Fixed
- Empty cart check in checkout now guarded behind loading state to prevent false redirects
- Route paths use `ROUTE_PATHS` constants instead of hardcoded strings

---

## [0.3.0] — 2026-03

### Changed
- **Products refactor** — split products god hook into focused hooks; URL-driven filter state; extracted smart filter components; DRY pass on types and naming
- Extracted `OrderTotals` and `OrderTotalsDisplay` to shared layer for reuse across checkout and order detail

### Fixed
- Fixed failing tests after products refactor
- Added `OrderTotalsDisplay` test coverage

---

## [0.2.0] — 2026-02

### Added
- Wishlist feature (backend service + frontend page)
- Promo code validation in checkout flow
- Inventory stock check before order creation
- Dashboard analytics endpoint (admin)
- Integration tests for all 12 controllers
- Architecture tests (NetArchTest) enforcing Clean Architecture dependency rules

### Changed
- Introduced `CheckoutContext` to eliminate prop drilling in checkout (later refactored in 0.4.0)

---

## [0.1.0] — 2026-01

### Added
- Initial project structure with Clean Architecture (Core / Application / Infrastructure / API)
- User authentication: register, login, JWT + refresh token rotation, email verification, password reset
- Product catalog with categories, images, filtering, sorting, pagination
- Shopping cart with optimistic updates and localStorage persistence
- Order management: create, list, detail, cancel, status update
- Reviews: create, update, delete, list per product
- User profile management
- Payment service (mocked — Stripe integration pending)
- Email service: SendGrid + SMTP implementations
- PostgreSQL with EF Core migrations and data seeding
- Docker Compose setup
- React storefront with RTK Query, Redux Toolkit, React Router 7
- React admin panel (scaffold)
- Serilog structured logging
- Rate limiting, CSRF protection, security headers
- Health checks
