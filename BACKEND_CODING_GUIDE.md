# Backend Coding Guide — ECommerce Project

This guide documents how to write code in this backend based on the conventions already established
in the codebase, plus areas where current code should be corrected. Every example is drawn from real
files in this project.

---

## Table of Contents

1. [Project Structure & Layer Rules](#1-project-structure--layer-rules)
2. [Entities — Core Layer](#2-entities--core-layer)
3. [DTOs — Application Layer](#3-dtos--application-layer)
4. [Validators](#4-validators)
5. [Repository Interfaces](#5-repository-interfaces)
6. [Repository Implementations](#6-repository-implementations)
7. [Unit of Work](#7-unit-of-work)
8. [Services](#8-services)
9. [Controllers](#9-controllers)
10. [Exception Handling](#10-exception-handling)
11. [Logging](#11-logging)
12. [AutoMapper](#12-automapper)
13. [Configuration & DI Registration](#13-configuration--di-registration)
14. [Known Issues in Current Code](#14-known-issues-in-current-code)

---

## 1. Project Structure & Layer Rules

```
ECommerce.Core            ← Domain: entities, enums, interfaces, exceptions
ECommerce.Application     ← Business logic: services, DTOs, validators, mapping
ECommerce.Infrastructure ← Data access: repositories, DbContext, migrations, seeders
ECommerce.API             ← HTTP layer: controllers, middleware, action filters
ECommerce.Tests           ← Unit & integration tests
```

**Dependency direction** (enforced by project references — never reverse these):

```
API  →  Application  →  Core
 ↓           ↓
Infrastructure  →  Core
```

- **Core** has zero references to other solution projects.
- **Application** references only Core. It defines interfaces — never implementations.
- **Infrastructure** references Core and Application. It implements the interfaces.
- **API** references Application (and Infrastructure only in `Program.cs` for DI wiring).

Every file uses **file-scoped namespaces**:

```csharp
// Correct
namespace ECommerce.Application.Services;

public class OrderService : IOrderService { }
```

```csharp
// Wrong — don't use block-scoped namespaces
namespace ECommerce.Application.Services
{
    public class OrderService : IOrderService { }
}
```

---

## 2. Entities — Core Layer

**Location:** `ECommerce.Core/Entities/`

All entities inherit from `BaseEntity`:

```csharp
// ECommerce.Core/Common/BaseEntity.cs
public abstract class BaseEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
```

Rules for entities:

- **Inherit BaseEntity.** Every table-backed class gets `Id`, `CreatedAt`, `UpdatedAt` for free.
- **Use `null!` for required strings** that are set before save but have no constructor default:
  ```csharp
  public string Name { get; set; } = null!;
  ```
- **Provide defaults for value types where appropriate:**
  ```csharp
  public int LowStockThreshold { get; set; } = 10;
  public bool IsActive { get; set; } = true;
  public string Currency { get; set; } = "USD";
  ```
- **Nullable foreign keys** for optional relationships:
  ```csharp
  public Guid? CategoryId { get; set; }    // product can exist without category
  public Guid? UserId { get; set; }        // order can be guest (no user)
  ```
- **Navigation properties are `virtual`** and collections are initialized:
  ```csharp
  public virtual Category? Category { get; set; }
  public virtual ICollection<OrderItem> Items { get; set; } = new List<OrderItem>();
  ```
- **Enums for status fields**, never magic strings:
  ```csharp
  public OrderStatus Status { get; set; } = OrderStatus.Pending;
  public PaymentStatus PaymentStatus { get; set; } = PaymentStatus.Pending;
  ```
- **Timestamp fields are nullable** when they only get set at a specific event:
  ```csharp
  public DateTime? ShippedAt { get; set; }
  public DateTime? DeliveredAt { get; set; }
  public DateTime? CancelledAt { get; set; }
  ```

Enums live in `ECommerce.Core/Enums/`, one file per enum.

---

## 3. DTOs — Application Layer

**Location:** `ECommerce.Application/DTOs/{Feature}/`

The project uses three categories of DTO:

| Category | Naming | Example | Purpose |
|---|---|---|---|
| Read | `{Entity}Dto` | `OrderDto` | API response (list views) |
| Read (detail) | `{Entity}DetailDto` | `OrderDetailDto` | API response (single item, extends the base) |
| Write | `Create{Entity}Dto` / `Update{Entity}Dto` | `CreateOrderDto` | Request body |
| Query | `{Entity}QueryParameters` | `OrderQueryParameters` | Query string params for pagination/filtering |

**Never expose entities directly.** The DTO is the API contract. If the entity changes, the contract doesn't have to.

Inheritance is used for detail DTOs:

```csharp
public class OrderDto                      // base — used in list responses
{
    public Guid Id { get; set; }
    public string OrderNumber { get; set; } = null!;
    public string Status { get; set; } = null!;
    public decimal TotalAmount { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class OrderDetailDto : OrderDto     // extends base — used in single-item responses
{
    public decimal Subtotal { get; set; }
    public AddressDto? ShippingAddress { get; set; }
    // ... additional detail fields
}
```

**Shared DTOs** (used across features) go in `DTOs/Common/`:

```
DTOs/Common/
├── PaginatedResult<T>.cs      ← pagination wrapper (lives here, not inside another file)
├── ApiResponse<T>.cs          ← response envelope
├── CategoryDto.cs
├── AddressDto.cs
├── ErrorDetails.cs
└── HealthCheckResponseDto.cs
```

`PaginatedResult<T>` provides computed pagination metadata — use it everywhere you paginate:

```csharp
public class PaginatedResult<T>
{
    public List<T> Items { get; set; } = new();
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
    public bool HasPrevious => Page > 1;
    public bool HasNext => Page < TotalPages;
}
```

---

## 4. Validators

**Location:** `ECommerce.Application/Validators/{Feature}/`

One validator file per DTO. Inherit `AbstractValidator<T>`:

```csharp
using FluentValidation;
using ECommerce.Application.DTOs.Auth;

namespace ECommerce.Application.Validators.Auth;

public class RegisterDtoValidator : AbstractValidator<RegisterDto>
{
    public RegisterDtoValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email is required")
            .EmailAddress().WithMessage("Invalid email format");

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("Password is required")
            .MinimumLength(8).WithMessage("Password must be at least 8 characters")
            .Matches("[A-Z]").WithMessage("Password must contain an uppercase letter")
            .Matches("[a-z]").WithMessage("Password must contain a lowercase letter")
            .Matches("[0-9]").WithMessage("Password must contain a digit");
    }
}
```

**For nested object validation**, use `.SetValidator()`:

```csharp
public class CreateOrderDtoValidator : AbstractValidator<CreateOrderDto>
{
    public CreateOrderDtoValidator()
    {
        RuleFor(x => x.Items).NotEmpty().WithMessage("Order must contain at least one item");
        RuleForEach(x => x.Items).SetValidator(new CreateOrderItemDtoValidator());
        RuleFor(x => x.ShippingAddress).NotNull().SetValidator(new AddressDtoValidator());
    }
}
```

Validators are auto-discovered at startup via:

```csharp
builder.Services.AddValidatorsFromAssemblyContaining<AddToCartDtoValidator>();
```

This scans the entire Application assembly. You do **not** need to register validators individually — just place them in the correct folder and they are picked up.

Validation errors are returned automatically by the `[ValidationFilter]` action filter as `422 Unprocessable Entity` with the standard `ApiResponse` format. **Do not add manual validation logic in controllers or services for things FluentValidation already covers.**

---

## 5. Repository Interfaces

**Location:** `ECommerce.Core/Interfaces/Repositories/`

The base interface is generic:

```csharp
public interface IRepository<T> where T : BaseEntity
{
    // Read
    Task<T?> GetByIdAsync(Guid id, bool trackChanges = true, CancellationToken cancellationToken = default);
    Task<IEnumerable<T>> GetAllAsync(bool trackChanges = true, CancellationToken cancellationToken = default);
    IQueryable<T> FindAll(bool trackChanges = false);
    IQueryable<T> FindByCondition(Expression<Func<T, bool>> expression, bool trackChanges = false);

    // Write (none of these call SaveChanges — UnitOfWork does that)
    void Add(T entity);
    Task AddAsync(T entity, CancellationToken cancellationToken = default);
    Task UpdateAsync(T entity, CancellationToken cancellationToken = default);
    Task DeleteAsync(T entity, CancellationToken cancellationToken = default);
    // ... bulk variants

    // Utility
    Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default);
    Task<int> CountAsync(CancellationToken cancellationToken = default);
    Task<int> CountAsync(Expression<Func<T, bool>> predicate, CancellationToken cancellationToken = default);
}
```

When you need queries beyond basic CRUD, create a **specialized interface** that extends the generic one:

```csharp
public interface IProductRepository : IRepository<Product>
{
    Task<Product?> GetBySlugAsync(string slug, bool trackChanges = false, CancellationToken cancellationToken = default);
    Task<(IEnumerable<Product> Items, int TotalCount)> GetProductsWithFiltersAsync(
        int skip, int take, Guid? categoryId = null, /* ... */);
    Task<bool> IsSlugUniqueAsync(string slug, Guid? excludeId = null, CancellationToken cancellationToken = default);
}
```

**Decision rule: generic vs specialized repository.**

| Entity needs... | Use |
|---|---|
| Only basic CRUD | `IRepository<Entity>` directly in UnitOfWork |
| Custom queries (by slug, by user, filtered pagination) | Specialized `I{Entity}Repository` |

Current split in this project:
- **Specialized:** `IProductRepository`, `IOrderRepository`, `IUserRepository`, `ICategoryRepository`, `ICartRepository`, `IReviewRepository`, `IWishlistRepository`
- **Generic only:** `OrderItem`, `CartItem`, `Address`, `PromoCode`, `InventoryLog`, `ProductImage`

All methods follow these conventions:
- **Read methods default `trackChanges = false`** in specialized repos (reads don't need tracking).
- **Write methods do not call `SaveChangesAsync`** — that's the UnitOfWork's job.
- **Every async method takes `CancellationToken cancellationToken = default`** as the last parameter.

---

## 6. Repository Implementations

**Location:** `ECommerce.Infrastructure/Repositories/`

The base `Repository<T>` implements `IRepository<T>`. Specialized repositories inherit from it:

```csharp
public class ProductRepository : Repository<Product>, IProductRepository
{
    public ProductRepository(AppDbContext context) : base(context)
    {
    }

    public async Task<Product?> GetBySlugAsync(string slug, bool trackChanges = false, CancellationToken cancellationToken = default)
    {
        var query = trackChanges ? DbSet : DbSet.AsNoTracking();
        return await query
            .Include(p => p.Category)
            .Include(p => p.Images)
            .Include(p => p.Reviews)
            .FirstOrDefaultAsync(p => p.Slug == slug && p.IsActive, cancellationToken);
    }
}
```

Key patterns:

- **Always check `trackChanges`** and apply `.AsNoTracking()` when false. The base class exposes `DbSet` as `protected`.
- **Include navigation properties explicitly.** EF Core does not lazy-load by default. If your service needs `order.Items`, the repository method must `.Include(o => o.Items)`.
- **Count before paginate.** For paginated endpoints, get the total count *after* filters are applied but *before* `Skip`/`Take`:
  ```csharp
  var totalCount = await query.CountAsync(cancellationToken);   // after WHERE, before Skip/Take
  var items = await query.Skip(skip).Take(take).ToListAsync(cancellationToken);
  return (items, totalCount);
  ```
- **Put complex filtering in the repository, not the service.** The service should pass parameters; the repository builds the query. See `GetProductsWithFiltersAsync` for the pattern.
- **Never call `SaveChangesAsync` inside a repository.** The repository modifies the EF change tracker. `UnitOfWork.SaveChangesAsync()` flushes everything in one transaction.

Use `#region` blocks to organize large repositories the same way the base class does:

```csharp
#region Read Operations
// ...
#endregion

#region Write Operations
// ...
#endregion
```

---

## 7. Unit of Work

**Location:** `ECommerce.Infrastructure/UnitOfWork.cs`

The UnitOfWork is the single entry point for all database access in services. Services inject `IUnitOfWork`, never individual repositories directly.

```csharp
// Correct — inject UnitOfWork
public class OrderService : IOrderService
{
    private readonly IUnitOfWork _unitOfWork;

    public OrderService(IUnitOfWork unitOfWork, /* ... */)
    {
        _unitOfWork = unitOfWork;
    }
}
```

```csharp
// Wrong — do not inject repositories directly into services
public class OrderService
{
    private readonly IOrderRepository _orderRepo;  // don't do this
}
```

Repositories are lazily initialized with `??=`:

```csharp
public IProductRepository Products => _products ??= new ProductRepository(_context);
```

This means `_unitOfWork.Products` is safe to access at any time — it creates the repo on first use.

**Transaction pattern** — use `BeginTransactionAsync` when a single service method needs atomicity across multiple writes:

```csharp
using var transaction = await _unitOfWork.BeginTransactionAsync(cancellationToken);
try
{
    _unitOfWork.Orders.Add(order);
    _unitOfWork.CartItems.DeleteRange(cartItems);
    await _unitOfWork.SaveChangesAsync(cancellationToken);
    await transaction.CommitAsync(cancellationToken);
}
catch
{
    await transaction.RollbackAsync(cancellationToken);
    throw;
}
```

When there is only one logical write operation, skip the explicit transaction — `SaveChangesAsync` is already atomic for a single call:

```csharp
order.Status = OrderStatus.Shipped;
order.ShippedAt = DateTime.UtcNow;
await _unitOfWork.Orders.UpdateAsync(order, cancellationToken: cancellationToken);
await _unitOfWork.SaveChangesAsync(cancellationToken: cancellationToken);
```

---

## 8. Services

**Location:** `ECommerce.Application/Services/`
**Interfaces:** `ECommerce.Application/Interfaces/`

Services contain all business logic. They sit between controllers and repositories.

**Constructor injection — all dependencies listed explicitly:**

```csharp
public class OrderService : IOrderService
{
    private readonly IPromoCodeService _promoCodeService;
    private readonly IInventoryService _inventoryService;
    private readonly IEmailService _emailService;
    private readonly IMapper _mapper;
    private readonly ILogger<OrderService> _logger;
    private readonly IUnitOfWork _unitOfWork;

    public OrderService(
        IPromoCodeService promoCodeService,
        IInventoryService inventoryService,
        IEmailService emailService,
        IUnitOfWork unitOfWork,
        IMapper mapper,
        ILogger<OrderService> logger)
    {
        _promoCodeService = promoCodeService;
        _inventoryService = inventoryService;
        _emailService = emailService;
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _logger = logger;
    }
}
```

Naming conventions:
- Fields: `_camelCase` with underscore prefix
- Parameters: `camelCase` matching the field name without the underscore

**Service methods should throw typed domain exceptions, never generic framework exceptions.** If a condition is not covered by an existing exception, create one in `ECommerce.Core/Exceptions/`. See [Section 10](#10-exception-handling).

**Use `_mapper.Map<T>()` for entity → DTO conversion.** Do not manually construct DTOs from entities unless there is a specific reason. The mapping is defined in `MappingProfile.cs`.

```csharp
// Correct
return _mapper.Map<OrderDetailDto>(order);

// Wrong — manual construction duplicates mapping logic
return new OrderDetailDto { Id = order.Id, Status = order.Status.ToString(), /* ... */ };
```

**Fire-and-forget for non-critical side effects.** Emails that should not block the main operation use `Task.Run`:

```csharp
_ = Task.Run(async () =>
{
    try
    {
        await _emailService.SendOrderConfirmationEmailAsync(email, order);
    }
    catch
    {
        // log but don't throw — email failure must not fail the order
    }
});
```

Note: this pattern has a known limitation (see [Section 14, Item 5](#14-known-issues-in-current-code)). For production, a proper background job queue is preferred.

**Private helper methods** go at the bottom of the class, marked `private`:

```csharp
private string GenerateOrderNumber()
{
    var date = DateTime.UtcNow.ToString("yyyyMMdd");
    var random = Guid.NewGuid().ToString("N").Substring(0, 6).ToUpper();
    return $"ORD-{date}-{random}";
}
```

---

## 9. Controllers

**Location:** `ECommerce.API/Controllers/`

Controllers are thin. They do exactly three things:
1. Receive the HTTP request
2. Call the service
3. Return an `ApiResponse<T>`

**Standard class-level attributes:**

```csharp
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class OrdersController : ControllerBase
```

Add `[Authorize]` at class level if most endpoints require auth, then override with `[AllowAnonymous]` on the exceptions. If most endpoints are public, do the reverse.

**Inject services and logger, nothing else:**

```csharp
private readonly IOrderService _orderService;
private readonly ICurrentUserService _currentUser;
private readonly ILogger<OrdersController> _logger;

public OrdersController(IOrderService orderService, ICurrentUserService currentUser, ILogger<OrdersController> logger)
{
    _orderService = orderService;
    _currentUser = currentUser;
    _logger = logger;
}
```

`ICurrentUserService` gives you the authenticated user:
- `_currentUser.UserId` — throws if not authenticated
- `_currentUser.UserIdOrNull` — returns null if not authenticated (use for endpoints that support guest access)

**Every endpoint needs these attributes:**

```csharp
[HttpGet("{id:guid}")]
[ProducesResponseType(typeof(ApiResponse<OrderDetailDto>), StatusCodes.Status200OK)]
[ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
```

`[ProducesResponseType]` documents the API for Swagger and self-documents the contract. List every status code the endpoint can return.

Add `[ValidationFilter]` on any endpoint that accepts a request body DTO:

```csharp
[HttpPost]
[AllowAnonymous]
[ValidationFilter]
public async Task<IActionResult> CreateOrder([FromBody] CreateOrderDto dto, CancellationToken cancellationToken)
```

**Response construction:**

```csharp
// Success
return Ok(ApiResponse<OrderDetailDto>.Ok(order, "Order retrieved successfully"));

// Created (use CreatedAtAction for POST that creates a resource)
return CreatedAtAction(nameof(GetOrderById), new { id = order.Id },
    ApiResponse<OrderDetailDto>.Ok(order, "Order created successfully"));

// Not found (when you check before calling the service)
return NotFound(ApiResponse<object>.Error("Order not found"));
```

**Do not wrap the entire method in try/catch.** The `GlobalExceptionMiddleware` catches all typed domain exceptions and maps them to the correct HTTP status code automatically. If your service throws `OrderNotFoundException`, the middleware returns 404 with the message. You only need local try/catch for exceptions you want to handle *differently* than the middleware default.

Role-based authorization:

```csharp
[Authorize(Roles = "Admin,SuperAdmin")]   // admin-only endpoints
```

---

## 10. Exception Handling

**Location:** `ECommerce.Core/Exceptions/`

The project uses a typed exception hierarchy. The middleware maps exception base classes to HTTP status codes:

| Base Exception | HTTP Status | Use for |
|---|---|---|
| `NotFoundException` | 404 | Entity not found by ID/slug/number |
| `BadRequestException` | 400 | Invalid input that passes validation but fails business rules |
| `UnauthorizedException` | 401 | Auth token missing/invalid/expired |
| `ConflictException` | 409 | Duplicate unique constraint (email, slug, etc.) |

Base classes use C# 12 primary constructors:

```csharp
public abstract class NotFoundException(string message) : Exception(message) { }
```

Each specific exception is `sealed`, one per file, also using primary constructors:

```csharp
// ECommerce.Core/Exceptions/OrderNotFoundException.cs
using ECommerce.Core.Exceptions.Base;

namespace ECommerce.Core.Exceptions;

public sealed class OrderNotFoundException(Guid orderId)
    : NotFoundException($"Order with ID '{orderId}' was not found.") { }
```

```csharp
// ECommerce.Core/Exceptions/InsufficientStockException.cs
public sealed class InsufficientStockException(string productName, int requestedQuantity, int availableQuantity)
    : BadRequestException($"Insufficient stock for product '{productName}'. Requested: {requestedQuantity}, Available: {availableQuantity}.") { }
```

**When to create a new exception class:**
- You need a distinct error that a client should be able to identify
- The message needs structured data (IDs, names, quantities)

**When NOT to create one:**
- Generic "something went wrong" — let it fall through to the 500 handler
- Validation errors — FluentValidation handles those as 422

**To throw:** just throw. No try/catch needed in the service or controller:

```csharp
var order = await _unitOfWork.Orders.GetByIdAsync(id);
if (order == null)
{
    throw new OrderNotFoundException(id);   // middleware catches this → 404
}
```

---

## 11. Logging

All services and controllers inject `ILogger<T>`:

```csharp
private readonly ILogger<OrderService> _logger;
```

The project uses **structured logging** via Serilog. Use message templates, not string interpolation:

```csharp
// Correct — structured, searchable
_logger.LogInformation("Order created successfully: {OrderNumber}", order.OrderNumber);
_logger.LogWarning("Payment failed for order {OrderId}. Reason: {Message}", dto.OrderId, result.Message);
_logger.LogError(ex, "Failed to reduce stock for product {ProductId} in order {OrderNumber}",
    item.ProductId.Value, order.OrderNumber);

// Wrong — loses structure, not searchable
_logger.LogInformation($"Order created: {order.OrderNumber}");
```

**Log level guidelines:**

| Level | Use for |
|---|---|
| `LogInformation` | Start/end of significant operations (order created, status updated) |
| `LogWarning` | Expected but noteworthy failures (payment failed, email not sent, guest missing email) |
| `LogError` | Unexpected failures with exception objects — always pass `ex` as first arg |

Serilog is configured in `Program.cs` with console + daily file output:

```csharp
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .WriteTo.Console()
    .WriteTo.File("logs/app-.txt", rollingInterval: RollingInterval.Day)
    .CreateLogger();
```

---

## 12. AutoMapper

**Location:** `ECommerce.Application/MappingProfile.cs`

All entity ↔ DTO mappings are in a single `MappingProfile` class. When you add a new entity and its DTOs, add the mapping here.

**Simple mappings** (property names match):

```csharp
CreateMap<Order, OrderDto>().ReverseMap();
CreateMap<CreateProductDto, Product>();
```

**Custom mappings** when property names differ or computed values are needed:

```csharp
CreateMap<Product, ProductDto>()
    .ForMember(dest => dest.AverageRating, opt => opt.MapFrom(src =>
        src.Reviews.Any() ? src.Reviews.Average(r => (decimal)r.Rating) : 0))
    .ForMember(dest => dest.ReviewCount, opt => opt.MapFrom(src => src.Reviews.Count));
```

**Ignore fields that should not be mapped from the source:**

```csharp
CreateMap<UpdateProfileDto, User>()
    .ForMember(dest => dest.Id, opt => opt.Ignore())
    .ForMember(dest => dest.Email, opt => opt.Ignore())
    .ForMember(dest => dest.PasswordHash, opt => opt.Ignore())
    .ForAllMembers(opts => opts.Condition((src, dest, srcMember) => srcMember != null));  // skip nulls on update
```

The `.ForAllMembers` with null condition is the **patch pattern** — only overwrite destination properties when the source value is non-null. Use it on all `Update*Dto` → Entity mappings.

**Use AutoMapper in services, not controllers.** Controllers never touch entities directly.

---

## 13. Configuration & DI Registration

**Location:** `ECommerce.API/Program.cs`

All services are registered as **Scoped** (one instance per HTTP request):

```csharp
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
builder.Services.AddScoped<IOrderService, OrderService>();
builder.Services.AddScoped<IProductService, ProductService>();
// ... one line per service
```

The pattern is `AddScoped<IInterface, Implementation>()`. Do not use Singleton or Transient for services that use `IUnitOfWork` — the DbContext is scoped, and sharing it across scopes causes threading issues.

**Email provider is selected at startup via config:**

```csharp
var emailProvider = configuration["EmailProvider"] ?? "SendGrid";
if (emailProvider.Equals("Smtp", StringComparison.OrdinalIgnoreCase))
    builder.Services.AddScoped<IEmailService, SmtpEmailService>();
else
    builder.Services.AddScoped<IEmailService, SendGridEmailService>();
```

This is the only place in the codebase where a runtime config value switches an implementation. If you add a new swappable provider, follow this exact pattern.

**Middleware order matters.** The current pipeline order in `Program.cs`:

```csharp
app.UseMiddleware<GlobalExceptionMiddleware>();   // must be first — catches everything below
app.UseCors("AllowAll");
app.UseAuthentication();                          // must come before Authorization
app.UseAuthorization();
app.MapControllers();
```

Do not reorder these.

---

## 14. Known Issues in Current Code

These are real issues found during the audit. Fix them when you touch the relevant code, or address them proactively.

### Issue 1 — `OrderService.CreateOrderAsync` wraps everything in a redundant try/catch

**File:** `ECommerce.Application/Services/OrderService.cs`

The outer `try { ... } catch (Exception ex) { _logger.LogError(...); throw; }` adds no value. The `GlobalExceptionMiddleware` already logs and handles all exceptions. The typed exceptions (`UserNotFoundException`, `InsufficientStockException`, etc.) thrown inside are already the correct response. Remove the outer try/catch.

### Issue 2 — `OrdersController.CreateOrder` has local exception handling for framework exceptions

**File:** `ECommerce.API/Controllers/OrdersController.cs`

```csharp
catch (InvalidOperationException ex) { ... }
catch (ArgumentException ex) when (ex.Message.Contains("Guest")) { ... }
```

The service throws `InvalidOperationException` for the guest email check. This should be a domain exception (e.g., a new `GuestEmailRequiredException : BadRequestException`). Once that is done, remove the local catches — the middleware handles it.

### Issue 3 — `GetAllOrdersAsync` loads all orders into memory before paginating

**File:** `ECommerce.Application/Services/OrderService.cs`

```csharp
var allOrders = await _unitOfWork.Orders.GetAllAsync();  // loads every row
var totalCount = allOrders.Count();
var orders = allOrders.OrderByDescending(...).Skip(...).Take(...).ToList();  // in-memory pagination
```

This will not scale. Add a method to `IOrderRepository` that does pagination at the database level (the same way `GetUserOrdersAsync` already does it for a single user), and use it here.

### Issue 4 — Empty `OnActionExecuted` method in `ValidationFilterAttribute`

**File:** `ECommerce.API/ActionFilters/ValidationFilterAttribute.cs`

```csharp
public override void OnActionExecuted(ActionExecutedContext context) { }
```

This override does nothing. Remove it.

### Issue 5 — Fire-and-forget emails use `Task.Run` with no queue

**File:** `ECommerce.Application/Services/AuthService.cs`

```csharp
_ = Task.Run(async () => { await _emailService.SendWelcomeEmailAsync(...); });
```

If the application shuts down before the task completes, the email is lost silently. For a production system, replace with a background job queue (e.g., Hangfire, Azure Service Bus, or even a simple `IHostedService` with a `Channel<T>`). The current pattern is acceptable for development but not for production.

### Issue 6 — `NormalizeCountryCode` creates a new `Dictionary` on every invocation

**File:** `ECommerce.Application/Services/OrderService.cs`

```csharp
private string NormalizeCountryCode(string country)
{
    var countryMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
    {
        { "United States", "US" },
        // ...
    };
}
```

Move the dictionary to a `private static readonly` field. It never changes and should be allocated once.

### Issue 7 — `CartService` has a null-reference bug after reload

**File:** `ECommerce.Application/Services/CartService.cs`, `AddToCartAsync` method

```csharp
cart = await _unitOfWork.Carts.GetCartWithItemsAsync(cart.Id, ...);
if (cart == null)
    throw new CartNotFoundException(cart.Id);  // cart IS null here — this throws NullReferenceException
```

Store `cart.Id` before the reload:

```csharp
var cartId = cart.Id;
cart = await _unitOfWork.Carts.GetCartWithItemsAsync(cartId, ...);
if (cart == null)
    throw new CartNotFoundException(cartId);
```

### Issue 8 — CORS and JWT are open for development but must be locked for production

**File:** `ECommerce.API/Program.cs`

```csharp
builder.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader();  // CORS
ValidateIssuer = false,   // JWT
ValidateAudience = false, // JWT
```

Before deploying to production:
- CORS: replace `AllowAll` with a named policy that lists only your frontend origins
- JWT: set `ValidateIssuer = true` and `ValidateAudience = true`, and populate `Jwt:Issuer` and `Jwt:Audience` in `appsettings.Production.json`

---

*Sources consulted during this guide:*
- *[Clean Architecture Folder Structure — Milan Jovanovic](https://www.milanjovanovic.tech/blog/clean-architecture-folder-structure)*
- *[ASP.NET Core Web API Best Practices — Code Maze](https://code-maze.com/aspnetcore-webapi-best-practices/)*
- *[Web API Design Best Practices — Microsoft Learn](https://learn.microsoft.com/en-us/azure/architecture/best-practices/api-design)*
- *[Top 12 Best Practices for REST APIs using C# .NET — DEV Community](https://dev.to/adrianbailador/top-12-best-practices-for-rest-apis-using-c-net-4kpp)*
- *[Clean Architecture in .NET 10 — DEV Community](https://dev.to/nikhilwagh/clean-architecture-in-net-10-patterns-that-actually-work-in-production-2025-guide-36b0)*
