# E-Commerce Platform — AI Coding Agent Instructions

This document guides AI agents in contributing effectively to this full-stack e-commerce platform.

## Architecture Summary

**Backend**: Clean Architecture (4 layers: API → Application → Infrastructure → Core)  
**Frontend**: React 19 + Redux Toolkit + RTK Query  
**Database**: PostgreSQL with Entity Framework Core + Unit of Work pattern  
**Build**: Docker Compose (containerized)

---

## Backend Architecture Essentials

### Layer Dependencies (enforced — never reverse)
```
   API  →  Application  →  Core
    ↓             ↓
Infrastructure  →  Core
```
- **Core** (zero external dependencies): Entities, Enums, Interfaces, Exceptions
- **Application**: Services, DTOs, Validators, Mappings (references only Core)
- **Infrastructure**: Repositories, DbContext, Migrations (references Core + Application)
- **API**: Controllers, Middleware, Action Filters (references Application + Infrastructure for DI)

### Critical BACKEND Patterns

#### Entities (Core/Entities/)
- Inherit `BaseEntity` → auto-get `Id`, `CreatedAt`, `UpdatedAt`
- Use `null!` for required strings, provide defaults for value types
- Navigation properties are `virtual`, collections initialized: `ICollection<T> = new List<T>()`
- **Status fields use enums** (never strings): `OrderStatus`, `PaymentStatus`
- Nullable foreign keys for optional relationships: `Guid?`
- Event timestamps nullable: `DateTime? ShippedAt`, `CancelledAt`

#### DTOs (Application/DTOs/{Feature}/)
- **List/Read**: `{Entity}Dto`  
- **Detail/Read**: `{Entity}DetailDto` (inherits base DTO)
- **Write**: `Create{Entity}Dto` / `Update{Entity}Dto`
- **Query params**: `{Entity}QueryParameters`
- Shared across features → go in `DTOs/Common/` (CategoryDto, AddressDto, PaginatedResult<T>, ApiResponse<T>)
- **Never expose entities directly** — DTOs are the API contract

#### Validation (Application/Validators/{Feature}/)
- One validator per DTO: `AbstractValidator<T>` from FluentValidation
- Validators auto-discovered at startup (placed in correct folder = registered)
- Nested validation: `.RuleForEach().SetValidator(new NestedValidator())`
- **Don't add manual validation logic in services** — let filters handle it

#### Repositories (Core/Interfaces/ + Infrastructure/Repositories/)
- **Specialized repositories** (custom queries): `IProductRepository`, `IOrderRepository`, etc.
  - Extend `IRepository<T>`
  - Method convention: `async Task<Entity?> GetByXAsync(..., CancellationToken ct = default)`
  - **Always check `trackChanges`** → apply `.AsNoTracking()` when false
  - **Explicit `.Include()`** for navigation properties (no lazy loading)
  - Count **before** Skip/Take for pagination
- **Generic only** for simple CRUD (no custom queries): OrderItem, CartItem, Address, PromoCode, etc.
- **Never call `SaveChangesAsync` inside repo** — UnitOfWork does that

#### Unit of Work (Infrastructure/UnitOfWork.cs)
- **Single entry point for all DB access** — services inject `IUnitOfWork`, never individual repos
- Lazy initialization: `public IProductRepository Products => _products ??= new ProductRepository(_context);`
- Supports transactions: `using var tx = await _uow.BeginTransactionAsync(ct); ... await tx.CommitAsync(ct);`

#### Services (Application/Services/ + Interfaces/)
- Inject dependencies explicitly (no service locator pattern)
- Inject `IUnitOfWork`, `IMapper`, `ILogger<T>`, other services — never repos directly
- **Use Result<T> for business logic** (never throw exceptions): `return Result<T>.Fail(ErrorCodes.OrderNotFound, message)`
- **Use `_mapper.Map<DTO>(entity)`** for conversions (never manual construction)
- **Fire-and-forget for side effects**: `_ = Task.Run(async () => { await _emailService.Send(...); })`

#### Controllers (API/Controllers/)
- **Thin layer**: Receive request → Call service → Return `ApiResponse<T>`
- Inject services + logger only
- Use `ICurrentUserService._currentUser.UserId` / `.UserIdOrNull` for auth
- **Every endpoint needs**: `[HttpXxx]`, `[ProducesResponseType]` (all status codes), `[ValidationFilter]` (if accepts DTO)
- Add `[Authorize]` at class level if most endpoints need auth (override with `[AllowAnonymous]`)
- Responses: `Ok()`, `CreatedAtAction()`, `NotFound()` wrapped in `ApiResponse<T>.Ok()` or `.Error()`

#### Error Handling (Result<T> Pattern)
- **Business logic**: Return `Result<T>` with error codes: `Result<T>.Fail(ErrorCodes.OrderNotFound, message)`
- **Infrastructure failures**: Base exception types remain (NotFoundException, BadRequestException, ConflictException, UnauthorizedException) for framework exceptions only
- **GlobalExceptionMiddleware** catches unexpected infrastructure exceptions → serialize to ApiResponse
- **Controllers**: Pattern match on Result<T>.Success / Result<T>.Failure, never throw in services
- **Error codes**: Use `ErrorCodes` constants (ORDER_NOT_FOUND, PRODUCT_NOT_FOUND, INSUFFICIENT_STOCK, etc.)

#### Logging (all services/controllers)
- Inject `ILogger<T>`
- Configured via Serilog → Console + rolling file (logs/ directory)

---

## Frontend Architecture Essentials

### Redux + RTK Query Organization
```
store/
  ├── api/
  │   ├── authApi.ts       (separate createApi per feature)
  │   ├── productApi.ts
  │   └── ...
  ├── slices/
  │   ├── authSlice.ts     (auth state: user, token, isAuthenticated)
  │   ├── cartSlice.ts
  │   └── ...
  └── store.ts             (configureStore with all reducers + middleware)
```

### Critical FRONTEND Patterns

#### RTK Query APIs (store/api/{feature}Api.ts)
- `createApi({ reducerPath, baseQuery, endpoints })`
- Base query: `fetchBaseQuery({ baseUrl, prepareHeaders })` → handle auth header (`Authorization: Bearer ${token}`)
- Endpoints: `builder.query()/mutation()`
- **Separate API file per feature** (not monolithic)
- Response transformation: `transformResponse: (response) => response.data`

#### Redux Slices (store/slices/)
- Store global UI state: `auth` (user, token, loading, error), `cart` (items, quantity)
- Actions: `createSlice({ name, initialState, reducers })`
- Use `authSlice` to set token/user on login, clear on logout
- `localStorage` for token persistence: read on app init, write on login

#### Components & Styling
- **CSS Modules** for scoped styles: `Component.module.css` → import `styles` → `className={styles.className}`
- Page container structure: `pages/` (routes) → `components/` (reusable)
- `react-router` for navigation, `ProtectedRoute` for auth-only pages
- Error boundaries + loading states (use RTK Query `isLoading`)

#### Type Safety
- Define request/response interfaces in API files
- Use `ApiResponse<T>` shape: `{ success, message, data, errors }`
- Component props fully typed

---

## Key Developer Workflows

### Building & Running
```powershell
# Docker (recommended)
docker-compose up --build         # All services: API (5000), Storefront (5173), Admin (5177), DB (5432)

# Local development
# Backend: Visual Studio → Run ECommerce.sln
# Frontend: npm install && npm run dev (in src/frontend/storefront or admin/)
```

### Database Migrations
```powershell
# Add migration
dotnet ef migrations add MigrationName -p ECommerce.Infrastructure -s ECommerce.API

# Apply migration
dotnet ef database update -p ECommerce.Infrastructure -s ECommerce.API

# Migrations live in: ECommerce.Infrastructure/Migrations/
```

### Testing
- Backend tests: `ECommerce.Tests/` (MSTest)
- Frontend: E2E smoke test `scripts/e2e.ps1`

### Configuration
- **Backend secrets** (never commit): `appsettings.Development.json`, `.env` (production)
- **Frontend env**: `VITE_API_URL` (default `http://localhost:5000/api`)
- JWT settings: Issuer, Audience, SecretKey, ExpireMinutes

---

## Project-Specific Conventions

1. **File-scoped namespaces** (not block-scoped): `namespace ECommerce.Core.Entities;`
2. **Private fields**: `_camelCase` with underscore prefix
3. **Pagination wrapper**: Always use `PaginatedResult<T>` (Items, TotalCount, Page, PageSize, TotalPages, HasPrevious, HasNext)
4. **CancellationToken** on all async methods: last parameter = `CancellationToken cancellationToken = default`
5. **Private helper methods** at bottom of class
6. **Region blocks** in large files: `#region Read Operations`, `#region Write Operations`

---

## Common Mistakes to Avoid

- ❌ Injecting repositories directly into services (use UnitOfWork)
- ❌ Calling `SaveChangesAsync()` inside repositories (UnitOfWork's job)
- ❌ Block-scoped namespaces (use file-scoped)
- ❌ Manually constructing DTOs instead of using AutoMapper
- ❌ Throwing exceptions for business logic (use Result<T> pattern)
- ❌ Lazy-loading navigation properties (explicitly `.Include()`)
- ❌ Validation logic in services (FluentValidation handles it)
- ❌ Monolithic API files (separate per feature)
- ❌ Manual URL construction (use environment variables: `VITE_API_URL`)

---

## Navigating the Codebase

**Key documentation**:
- [BACKEND_CODING_GUIDE.md](../BACKEND_CODING_GUIDE.md) — Deep dive on backend patterns, entity rules, repository design, service patterns
- [ARCHITECTURE_PLAN.md](../ARCHITECTURE_PLAN.md) — System overview, feature list, tech stack details
- [docker-compose.yml](../docker-compose.yml) — Service definitions, ports, health checks

**Reference implementations**:
- Product feature: [ProductRepository](src/backend/ECommerce.Infrastructure/Repositories/ProductRepository.cs), [ProductService](src/backend/ECommerce.Application/Services/ProductService.cs), [ProductsController](src/backend/ECommerce.API/Controllers/ProductsController.cs)
- Auth feature: [AuthService](src/backend/ECommerce.Application/Services/AuthService.cs), [authApi.ts](src/frontend/storefront/src/store/api/authApi.ts)
- DTOs: [Products DTO structure](src/backend/ECommerce.Application/DTOs/Products/)

---

## When to Ask for Clarification

- Endpoint design touches new business domain (ask about feature requirements first)
- Adding enum → confirm values with team
- Schema changes → impact on migrations/seeders
- Breaking API contract changes → coordinate with frontend
- New cross-cutting concern (logging, caching, etc.) → discuss architecture first
