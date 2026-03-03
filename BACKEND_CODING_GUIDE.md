# Backend Coding Guide - E-Commerce Platform

**Last Updated**: March 2026 | **Status**: Production-Ready | **Database**: PostgreSQL

---

## 🚀 Quick Start: 10 Mandatory Rules (Read This First)

Every backend contribution **MUST** follow these 10 rules. When in doubt, reference the detailed sections below.

### **1. Feature Delivery Order (MUST)**
Follow this sequence **always**:
1. Domain (Core) — entities, enums, exceptions
2. Contracts (Application) — DTOs, validators
3. Persistence (Infrastructure) — repositories, migrations
4. Business Logic (Service) — inject `IUnitOfWork`, `IMapper`, `ILogger<T>`
5. Transport (Controller) — validate, call service, return `ApiResponse<T>`

### **2. Response Shape (MUST)**
All responses use this structure:
```csharp
public record ApiResponse<T>
{
    public bool Success { get; init; }
    public T? Data { get; init; }
    public ErrorResponse? Error { get; init; }
    public string? TraceId { get; init; }
}
```

Errors use:
```csharp
public record ErrorResponse
{
    public required string Message { get; init; }
    public string? Code { get; init; }  // e.g., "PRODUCT_NOT_FOUND"
    public Dictionary<string, string[]>? Errors { get; init; }  // Validation errors
    public string? TraceId { get; init; }
}
```

**All endpoints return this shape. No exceptions.**

### **3. Pagination (MUST)**
Every list endpoint **must** paginate. Hard caps: default 20, max 100.
```csharp
[HttpGet]
public async Task<ActionResult> GetProducts(
    [FromQuery] int pageNumber = 1,
    [FromQuery] int pageSize = 20,
    CancellationToken ct = default)
{
    if (pageNumber < 1) pageNumber = 1;
    if (pageSize < 1 || pageSize > 100) pageSize = 20;  // Enforce bounds

    var result = await _service.GetProductsAsync(pageNumber, pageSize, ct);
    return Ok(ApiResponse<PaginatedResponse<ProductDto>>.Success(result));
}
```

### **4. User Context Injection (MUST)**
Never pass `userId` as parameter. Always inject:
```csharp
public class OrderService
{
    public OrderService(IUnitOfWork uow, ICurrentUserContext currentUser, IMapper mapper)
    {
        _unitOfWork = uow;
        _currentUser = currentUser;  // ← Injected, not passed
    }

    public async Task<OrderDto> CreateOrderAsync(CreateOrderDto dto, CancellationToken ct)
    {
        var order = new Order { UserId = _currentUser.UserId };  // ✅ Always use injected
        // ...
    }
}
```

### **5. Validation in 3 Layers (MUST)**
1. **Controller parameter bounds** — `pageNumber >= 1`, Guid not empty
2. **FluentValidation** — DTO shape, format, ranges (add `[ValidationFilter]` to endpoint)
3. **Service layer** — business rules, uniqueness, ownership checks

```csharp
// Controller: Parameter validation
if (id == Guid.Empty) return BadRequest("Invalid ID");

// DTO: FluentValidation
public class CreateProductDtoValidator : AbstractValidator<CreateProductDto>
{
    public CreateProductDtoValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(255);
        RuleFor(x => x.Price).GreaterThan(0);
    }
}

// Service: Business rules
var existingProduct = await _unitOfWork.Products.GetBySlugAsync(dto.Slug, ct);
if (existingProduct != null) throw new DuplicateProductSlugException(dto.Slug);
```

### **6. Structured Logging (MUST)**
Use named placeholders, never string interpolation. Log semantic context.
```csharp
// ✅ GOOD
_logger.LogInformation("Order {OrderId} created by {UserId}, total {OrderTotal:C}",
    order.Id, order.UserId, order.TotalAmount);

// ❌ BAD
_logger.LogInformation($"Order {order.Id} created");  // Lost structure
```

**Never log**: passwords, tokens, credit cards, emails (as plaintext), API keys.

### **7. Error Handling (MUST)**
Throw typed exceptions in services, catch in `GlobalExceptionMiddleware`, map to HTTP status codes.
```csharp
// Service
if (order == null) throw new OrderNotFoundException(orderId);
if (order.UserId != _currentUser.UserId) throw new ForbiddenException("Not your order");

// Middleware catches and returns:
// 404 for NotFoundException
// 403 for ForbiddenException
// 409 for DbUpdateConcurrencyException
// 422 for ValidationException
```

### **8. Authorization (MUST)**
- Class-level `[Authorize]` with `[AllowAnonymous]` overrides
- Role checks: `[Authorize(Roles = "Admin,SuperAdmin")]`
- Ownership checks in service layer (never controller)

```csharp
[Authorize]
public class OrdersController : ControllerBase
{
    [AllowAnonymous]
    [HttpPost]
    public async Task<ActionResult> CreateOrder(...) { }  // Guest allowed

    [HttpGet("{id}")]
    public async Task<ActionResult> GetOrder(Guid id, CancellationToken ct)
    {
        var result = await _service.GetOrderAsync(id, ct);  // Service checks ownership
        return Ok(ApiResponse<OrderDto>.Success(result));
    }
}
```

### **9. Concurrency Safety (MUST for frequently-updated entities)**
Add `[Timestamp]` to Order, Cart, and similar entities. Catch `DbUpdateConcurrencyException`:
```csharp
public class Order
{
    [Timestamp]
    public byte[]? RowVersion { get; set; }  // PostgreSQL: xmin
}

// Service
try
{
    order.Status = dto.NewStatus;
    await _unitOfWork.SaveChangesAsync(ct);
}
catch (DbUpdateConcurrencyException)
{
    _unitOfWork.Orders.Detach(order);
    var current = await _unitOfWork.Orders.GetByIdAsync(orderId, ct);
    throw new OrderConcurrencyException($"Modified by another user. Status: {current?.Status}");
}

// Controller
catch (OrderConcurrencyException ex)
{
    return Conflict(new { error = ex.Message, TraceId = HttpContext.TraceIdentifier });
}
```

### **10. Migrations are Immutable (MUST)**
After `dotnet ef migrations add` and push, **never edit** the migration file. Always create a new one.
```csharp
// ✅ DO THIS
// Migration 1: AddProductTable
// Migration 2: AlterProductAddBarcodeColumn (fix for Migration 1)

// ❌ NEVER THIS
// Edit the original AddProductTable migration after it's pushed
```

---

## 📋 PR Checklist (Maps to CI Gates)

Use this for every feature PR. Check off before pushing:

- [ ] **Code Quality**
  - [ ] Build passes: `dotnet build` (no errors, no new warnings)
  - [ ] No layer dependency violations (Core stays dependency-light)
  - [ ] No `ReverseMap()` on entity→DTO mappings (DTOs map FROM entities, not back)
  - [ ] No N+1 queries (profile with EF Core logging)
  - [ ] No unbounded `GetAll()` queries (use pagination)

- [ ] **Mandatory Rules**
  - [ ] Feature follows 1→5 delivery order (Domain → Contracts → Persistence → Service → Controller)
  - [ ] All list endpoints paginate with bounds (max 100 items)
  - [ ] Response shape is `ApiResponse<T>` with `Success`, `Data`, `Error`, `TraceId`
  - [ ] Error handling uses typed exceptions + middleware mapping
  - [ ] `[Authorize]` or `[AllowAnonymous]` explicitly set on all endpoints
  - [ ] Ownership checks in service layer (not controller)

- [ ] **Validation & Security**
  - [ ] DTOs have FluentValidation validators
  - [ ] `[ValidationFilter]` on all POST/PUT/PATCH endpoints
  - [ ] Three-layer validation: controller bounds → FluentValidation → service business rules
  - [ ] No unbounded `GetAll()` queries — all have pagination or admin guard
  - [ ] No sensitive data in logs (passwords, tokens, cards)

- [ ] **Data & Concurrency**
  - [ ] Migration generated and reviewed locally (`dotnet ef migrations add`)
  - [ ] Migration has `Up()` and `Down()` methods for rollback
  - [ ] Frequently-updated entities have `[Timestamp]` RowVersion (Order, Cart)
  - [ ] Concurrency conflicts caught with `DbUpdateConcurrencyException` and return 409
  - [ ] Distributed locks used for financial operations (Idempotency-Key + cache)
  - [ ] Retry policies configured for transient failures (HttpClient, Polly)

- [ ] **Testing**
  - [ ] Service unit tests for happy path + all exception cases
  - [ ] Repository tests with in-memory database
  - [ ] Test naming: `MethodName_Scenario_ExpectedResult`
  - [ ] All validators tested (100% coverage)

- [ ] **API Documentation**
  - [ ] `///` XML comments on all controller methods
  - [ ] `[ProducesResponseType]` for all status codes: 200, 400, 401, 403, 404, 409, 422, 500
  - [ ] `[Tags("FeatureName")]` on controller or endpoint
  - [ ] Swagger UI shows all endpoints with descriptions (test locally: `/swagger`)

---

## 🔍 Response & Error Contracts (Single Source of Truth)

### **Success Response**
```csharp
public record ApiResponse<T>
{
    public required bool Success { get; init; }
    public T? Data { get; init; }
    public ErrorResponse? Error { get; init; }
    public string? TraceId { get; init; }

    public static ApiResponse<T> Success(T data) =>
        new() { Success = true, Data = data };

    public static ApiResponse<T> Failure(ErrorResponse error, string traceId) =>
        new() { Success = false, Error = error, TraceId = traceId };
}
```

### **Error Response**
```csharp
public record ErrorResponse
{
    /// <summary>User-friendly message (never expose internals)</summary>
    public required string Message { get; init; }

    /// <summary>Semantic error code for client logic: "PRODUCT_NOT_FOUND", "INSUFFICIENT_INVENTORY"</summary>
    public string? Code { get; init; }

    /// <summary>Validation errors: { "Email": ["Invalid format"], "Name": ["Required"] }</summary>
    public Dictionary<string, string[]>? Errors { get; init; }

    /// <summary>Trace ID for support tickets (correlate logs)</summary>
    public string? TraceId { get; init; }
}
```

### **Paginated Response**
```csharp
public record PaginatedResponse<T>
{
    public required IReadOnlyList<T> Items { get; init; }
    public required int PageNumber { get; init; }
    public required int PageSize { get; init; }
    public required int TotalItems { get; init; }
    public required int TotalPages { get; init; }

    public bool HasNextPage => PageNumber < TotalPages;
    public bool HasPreviousPage => PageNumber > 1;
}
```

### **HTTP Status Codes Mapping**
| Status | Code | When | Example |
|--------|------|------|---------|
| 200 | OK | Successful GET, successful business operation | Product retrieved, order created |
| 201 | Created | Resource created | `POST /orders` returns 201 |
| 400 | Bad Request | Invalid parameters, failed business rule | Invalid Guid, negative price |
| 401 | Unauthorized | Missing/invalid JWT | No Authorization header |
| 403 | Forbidden | Resource ownership violation, insufficient role | User accessing another's order |
| 404 | Not Found | Resource doesn't exist | Product ID not found |
| 409 | Conflict | Concurrency conflict, duplicate key, state conflict | Two requests updating same order |
| 422 | Unprocessable Entity | Validation failed (semantic error) | Required field missing, format invalid |
| 500 | Internal Server Error | Unhandled exception | Database connection failed |

---

## 🏗️ Result Pattern (Functional Error Handling)

Instead of throwing exceptions for business logic failures, use a **Result<T>** pattern for explicit control flow. This is especially powerful for:
- API endpoints with multiple failure paths
- Saga patterns and distributed transactions
- Testability (no exception setup needed)

### **Result<T> Base Types**

```csharp
public abstract record Result<T>
{
    public sealed record Success(T Data) : Result<T>;
    public sealed record Failure(string Code, string Message) : Result<T>;
    public sealed record ValidationFailure(Dictionary<string, string[]> Errors) : Result<T>;
}

// Usage
public record struct Result<T>
{
    private readonly T? _data;
    private readonly string? _error;
    public bool IsSuccess { get; private init; }
    public T Data => IsSuccess ? _data! : throw new InvalidOperationException("Result is not successful");
    public string Error => _error ?? string.Empty;

    public static Result<T> Ok(T data) => new() { IsSuccess = true, _data = data };
    public static Result<T> Fail(string error) => new() { IsSuccess = false, _error = error };

    // Pattern matching helper
    public TResult Match<TResult>(
        Func<T, TResult> onSuccess,
        Func<string, TResult> onFailure) =>
        IsSuccess ? onSuccess(_data!) : onFailure(_error!);
}
```

### **Service with Result Pattern**

```csharp
public class OrderService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<OrderService> _logger;

    // Key pattern: No exception throwing for business logic
    public async Task<Result<OrderDto>> CreateOrderAsync(
        CreateOrderDto dto,
        CancellationToken ct = default)
    {
        // Validate inventory
        var inventoryCheck = await _unitOfWork.Products
            .GetByIdAsync(dto.ProductId, ct);
        
        if (inventoryCheck == null)
            return Result<OrderDto>.Fail("PRODUCT_NOT_FOUND");
        
        if (inventoryCheck.Stock < dto.Quantity)
            return Result<OrderDto>.Fail("INSUFFICIENT_INVENTORY");

        // Process order
        var order = new Order { ProductId = dto.ProductId, Quantity = dto.Quantity };
        await _unitOfWork.Orders.AddAsync(order, ct);
        await _unitOfWork.SaveChangesAsync(ct);

        return Result<OrderDto>.Ok(new OrderDto { Id = order.Id });
    }
}
```

### **Controller Mapping Result to HTTP**

```csharp
[HttpPost]
public async Task<ActionResult> CreateOrder(
    [FromBody] CreateOrderDto dto,
    CancellationToken ct = default)
{
    var result = await _service.CreateOrderAsync(dto, ct);
    
    return result.Match(
        onSuccess: order => CreatedAtAction(nameof(GetOrder), new { id = order.Id },
            ApiResponse<OrderDto>.Success(order)),
        onFailure: error => error switch
        {
            "PRODUCT_NOT_FOUND" => NotFound(ApiResponse<OrderDto>.Failure(
                new ErrorResponse { Code = error, Message = "Product not found" }, "")),
            "INSUFFICIENT_INVENTORY" => BadRequest(ApiResponse<OrderDto>.Failure(
                new ErrorResponse { Code = error, Message = "Not enough inventory" }, "")),
            _ => StatusCode(500, ApiResponse<OrderDto>.Failure(
                new ErrorResponse { Code = "UNKNOWN_ERROR", Message = error }, ""))
        }
    );
}
```

**When to use Result<T>:**
- ✅ Multiple business logic failure paths (inventory, validation, state machine)
- ✅ Saga patterns with compensating transactions
- ❌ Infrastructure errors (DB connection failed) — still throw exceptions
- ❌ Programming errors (null reference) — let them fail fast

---

## 🔄 Advanced Concurrency Patterns

### **Optimistic Locking with Conflict Resolution**

When concurrent updates conflict, resolve intelligently instead of failing:

```csharp
public class OrderService
{
    public async Task<Result<OrderDto>> UpdateOrderStatusAsync(
        Guid orderId,
        OrderStatus newStatus,
        byte[]? expectedRowVersion = null,
        CancellationToken ct = default)
    {
        // Load current state
        var order = await _unitOfWork.Orders.GetByIdAsync(orderId, ct)
            ?? throw new OrderNotFoundException(orderId);

        // If client provided RowVersion (from earlier GET), verify not stale
        if (expectedRowVersion != null && !AreRowVersionsEqual(order.RowVersion, expectedRowVersion))
        {
            // Another user modified. Return current state so client can re-apply their change
            _logger.LogWarning("Stale update attempt for Order {OrderId}", orderId);
            return Result<OrderDto>.Fail(
                new { Code = "CONFLICT", CurrentStatus = order.Status, RowVersion = order.RowVersion });
        }

        // Validate state machine: can only transition from certain states
        if (!order.CanTransitionTo(newStatus))
            return Result<OrderDto>.Fail("INVALID_STATE_TRANSITION");

        order.Status = newStatus;
        order.UpdatedAt = DateTime.UtcNow;

        try
        {
            await _unitOfWork.SaveChangesAsync(ct);
            return Result<OrderDto>.Ok(_mapper.Map<OrderDto>(order));
        }
        catch (DbUpdateConcurrencyException ex)
        {
            // Conflict: another user changed it between our read and write
            var currentOrder = await _unitOfWork.Orders.GetByIdAsync(orderId, ct);
            _logger.LogWarning("Concurrency conflict for Order {OrderId}, current status: {Status}",
                orderId, currentOrder?.Status);
            
            // Reload and try merge instead of fail
            return await ResolveConflictAsync(orderId, newStatus, ct);
        }
    }

    private async Task<Result<OrderDto>> ResolveConflictAsync(
        Guid orderId,
        OrderStatus requestedStatus,
        CancellationToken ct)
    {
        // Strategy: If both want to move forward, allow. If conflict, client retries.
        var current = await _unitOfWork.Orders.GetByIdAsync(orderId, ct);
        
        if (current?.Status.Level < requestedStatus.Level)
        {
            // Both trying to progress, allow it
            current.Status = requestedStatus;
            await _unitOfWork.SaveChangesAsync(ct);
            return Result<OrderDto>.Ok(_mapper.Map<OrderDto>(current));
        }

        // Otherwise conflict — return failure with current state
        return Result<OrderDto>.Fail($"CONFLICT_CURRENT_STATUS:{current?.Status}");
    }
}
```

### **Distributed Lock Pattern (Redis)**

For critical sections across multiple instances:

```csharp
public class PaymentService
{
    private readonly IDistributedLockProvider _lockProvider;
    private readonly IUnitOfWork _unitOfWork;

    public async Task<Result<PaymentDto>> ProcessPaymentAsync(
        Guid orderId,
        decimal amount,
        CancellationToken ct = default)
    {
        // Acquire lock on the order to prevent double-charge
        var lockKey = $"payment:lock:{orderId}";
        var lockValue = Guid.NewGuid().ToString();
        var lockLeaseTime = TimeSpan.FromSeconds(30);

        using var @lock = await _lockProvider.AcquireLockAsync(
            lockKey, lockValue, lockLeaseTime, ct);

        if (@lock == null)
        {
            _logger.LogWarning("Failed to acquire lock for Order {OrderId}, contention", orderId);
            return Result<PaymentDto>.Fail("PAYMENT_IN_PROGRESS");
        }

        try
        {
            // Double-check order state inside lock
            var order = await _unitOfWork.Orders.GetByIdAsync(orderId, ct);
            if (order?.Status != OrderStatus.Pending)
                return Result<PaymentDto>.Fail("ORDER_ALREADY_PAID_OR_CANCELLED");

            // Process payment (calls external API)
            var paymentResult = await _paymentGateway.ChargeAsync(
                order.UserId, amount, orderId.ToString(), ct);

            if (!paymentResult.IsSuccess)
                return Result<PaymentDto>.Fail(paymentResult.ErrorCode);

            // Update order status
            order.Status = OrderStatus.Confirmed;
            order.PaymentId = paymentResult.TransactionId;
            await _unitOfWork.SaveChangesAsync(ct);

            return Result<PaymentDto>.Ok(new PaymentDto { Id = paymentResult.TransactionId });
        }
        finally
        {
            // Lock automatically released when disposed
        }
    }
}
```

### **Retry with Exponential Backoff & Circuit Breaker**

```csharp
// In DI configuration
services.AddHttpClient<IPaymentGateway, PaymentGateway>()
    .AddTransientHttpErrorPolicy(p => p
        .WaitAndRetryAsync(new[]
        {
            TimeSpan.FromMilliseconds(100),
            TimeSpan.FromMilliseconds(200),
            TimeSpan.FromMilliseconds(400),
        }))  // Exponential backoff: 100ms, 200ms, 400ms
    .AddTransientHttpErrorPolicy(p => p
        .CircuitBreakerAsync(
            handledEventsAllowedBeforeBreaking: 5,
            durationOfBreak: TimeSpan.FromSeconds(30)))  // Break for 30s after 5 failures
    .ConfigureHttpClient(http => http.Timeout = TimeSpan.FromSeconds(10));
```

---

## ⚡ Query Optimization Patterns

### **N+1 Query Prevention**

```csharp
// ❌ BAD — Causes N+1 queries
public async Task<List<OrderDto>> GetOrdersWithItemsAsync(CancellationToken ct)
{
    var orders = await _context.Orders.ToListAsync(ct);  // Query 1
    foreach (var order in orders)
    {
        // Query 2, 3, 4... (N+1 total)
        var items = await _context.OrderItems
            .Where(oi => oi.OrderId == order.Id)
            .ToListAsync(ct);
        order.Items = items;
    }
    return orders;
}

// ✅ GOOD — Single query with eager load
public async Task<List<OrderDto>> GetOrdersWithItemsAsync(CancellationToken ct)
{
    return await _context.Orders
        .Include(o => o.Items)  // Eager load in single query
        .ThenInclude(oi => oi.Product)  // Nested include
        .AsNoTracking()
        .ToListAsync(ct);
}

// ✅ BETTER — Projection (only get fields you need)
public async Task<List<OrderSummaryDto>> GetOrdersWithItemsAsync(CancellationToken ct)
{
    return await _context.Orders
        .Select(o => new OrderSummaryDto
        {
            Id = o.Id,
            Total = o.Total,
            ItemCount = o.Items.Count,
            ItemNames = o.Items.Select(oi => oi.Product.Name).ToList()
        })
        .AsNoTracking()
        .ToListAsync(ct);
}
```

### **Query Profiling & Analysis**

```csharp
// Register logging and profiling in development
if (app.Environment.IsDevelopment())
{
    // Log all queries with execution time
    services.AddQueryTrackingInterceptor((sql, duration) =>
    {
        if (duration.TotalMilliseconds > 100)
            _logger.LogWarning("Slow query ({Duration}ms): {Sql}", duration.TotalMilliseconds, sql);
    });
}

// Use EF Core's DebugView to inspect query plans
#if DEBUG
var query = _context.Orders.Include(o => o.Items);
var logs = query.ToQueryString();  // Inspect SQL
Console.WriteLine(logs);
#endif
```

### **Pagination Best Practices**

```csharp
// ❌ BAD — Skipping large offsets is expensive
public async Task<List<Product>> GetProductsAsync(int page, int pageSize, CancellationToken ct)
{
    var skip = (page - 1) * pageSize;  // Skip 999,000 rows for page 1000!
    return await _context.Products
        .Skip(skip)
        .Take(pageSize)
        .ToListAsync(ct);
}

// ✅ GOOD — Use "keyset pagination" for large datasets
public async Task<List<Product>> GetProductsKeysetAsync(
    Guid? afterId = null,
    int pageSize = 20,
    CancellationToken ct = default)
{
    var query = _context.Products.AsNoTracking().OrderBy(p => p.Id);
    
    if (afterId != null)
        query = query.Where(p => p.Id > afterId);  // Use indexed column as cursor
    
    return await query.Take(pageSize + 1).ToListAsync(ct);
}
```

Returned DTO includes `HasNext` so frontend knows if more rows exist.

---

## 📅 Background Job Patterns (Hangfire)

For long-running or scheduled operations (email, reports, cleanup):

```csharp
// Register Hangfire
services.AddHangfire(config => config
    .UseSimpleAssemblyNameTypeSerializer()
    .UseRecommendedSerializerSettings()
    .UsePostgresqlStorage(connectionString, new PostgreSqlStorageOptions
    {
        SchemaName = "hangfire",
        InvisibilityTimeout = TimeSpan.FromMinutes(30),
    }));
services.AddHangfireServer();

// Service with background jobs
public class OrderService
{
    private readonly IBackgroundJobClient _backgroundJobs;
    private readonly IEmailService _emailService;

    public async Task<OrderDto> CreateOrderAsync(CreateOrderDto dto, CancellationToken ct = default)
    {
        // ... create order ...
        var order = new Order { /* ... */ };
        await _unitOfWork.SaveChangesAsync(ct);

        // Enqueue email send (returns immediately, executes later)
        _backgroundJobs.Enqueue(() => _emailService.SendOrderConfirmationAsync(order.Id));

        // Schedule report generation for 2 minutes later
        _backgroundJobs.Schedule(
            () => _reportService.GenerateOrderReportAsync(order.Id),
            TimeSpan.FromMinutes(2));

        return _mapper.Map<OrderDto>(order);
    }
}

// Recurring jobs
public class JobSetup
{
    [Obsolete("For Hangfire", true)]  // Signals this is background job entry point
    public async Task DailyOrderReconciliation(CancellationToken ct = default)
    {
        // Runs daily at 2 AM
        await _reconciliationService.ReconcilePendingOrdersAsync(ct);
    }
}

// In startup configuration
RecurringJob.AddOrUpdate(
    "daily-order-reconciliation",
    typeof(JobSetup),
    nameof(JobSetup.DailyOrderReconciliation),
    "0 2 * * *");  // Cron: 2 AM daily
```

---

## 🔀 Distributed Transactions & Saga Pattern

For operations spanning multiple microservices or databases:

```csharp
// Saga coordinator — orchestrates distributed transaction
public class OrderSaga
{
    private readonly IPaymentService _paymentService;
    private readonly IInventoryService _inventoryService;
    private readonly IShippingService _shippingService;
    private readonly ILogger<OrderSaga> _logger;

    public async Task<Result<OrderDto>> ExecuteAsync(
        CreateOrderDto dto,
        CancellationToken ct = default)
    {
        var sagaId = Guid.NewGuid();
        var compensations = new Stack<Func<Task>>();  // Rollback steps

        try
        {
            // Step 1: Reserve inventory
            var inventoryResult = await _inventoryService.ReserveAsync(
                dto.ProductId, dto.Quantity, ct);
            if (!inventoryResult.IsSuccess)
                return Result<OrderDto>.Fail(inventoryResult.Error);
            
            compensations.Push(async () =>
            {
                _logger.LogInformation("[Saga {SagaId}] Compensating inventory release", sagaId);
                await _inventoryService.ReleaseReservationAsync(
                    inventoryResult.ReservationId, ct);
            });

            // Step 2: Process payment
            var paymentResult = await _paymentService.ChargeAsync(
                dto.UserId, dto.Amount, ct);
            if (!paymentResult.IsSuccess)
                return Result<OrderDto>.Fail(paymentResult.Error);
            
            compensations.Push(async () =>
            {
                _logger.LogInformation("[Saga {SagaId}] Compensating payment refund", sagaId);
                await _paymentService.RefundAsync(paymentResult.TransactionId, ct);
            });

            // Step 3: Create shipment
            var shipmentResult = await _shippingService.CreateShipmentAsync(
                dto.UserId, inventoryResult.ReservationId, ct);
            if (!shipmentResult.IsSuccess)
                return Result<OrderDto>.Fail(shipmentResult.Error);

            _logger.LogInformation("[Saga {SagaId}] Order creation succeeded", sagaId);
            return Result<OrderDto>.Ok(shipmentResult.OrderDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[Saga {SagaId}] Unexpected error, executing compensations", sagaId);
            
            // Execute compensations in reverse order
            while (compensations.Count > 0)
            {
                var compensation = compensations.Pop();
                try
                {
                    await compensation();
                }
                catch (Exception compEx)
                {
                    _logger.LogError(compEx, "[Saga {SagaId}] Compensation step failed", sagaId);
                    // Log for manual intervention
                }
            }
            
            return Result<OrderDto>.Fail($"SAGA_FAILED: {ex.Message}");
        }
    }
}

// Usage in controller
[HttpPost]
public async Task<ActionResult> CreateOrder(
    [FromBody] CreateOrderDto dto,
    CancellationToken ct = default)
{
    var result = await _orderSaga.ExecuteAsync(dto, ct);
    return result.Match(
        onSuccess: order => CreatedAtAction(nameof(GetOrder), new { id = order.Id },
            ApiResponse<OrderDto>.Success(order)),
        onFailure: error => StatusCode(500,
            ApiResponse<OrderDto>.Failure(new ErrorResponse { Message = error }, ""))
    );
}
```

---

## 🗄️ Caching Strategy with Redis

### **Multi-Layer Cache**

```csharp
public class ProductRepository : IProductRepository
{
    private readonly AppDbContext _context;
    private readonly IDistributedCache _cache;
    private readonly IMemoryCache _localCache;
    private readonly ILogger<ProductRepository> _logger;
    private const string CacheKeyPrefix = "product:";
    private const int CacheExpirationMinutes = 60;

    // Caching hierarchy: Local → Distributed (Redis) → Database
    public async Task<Product?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var cacheKey = $"{CacheKeyPrefix}{id}";

        // Layer 1: Local memory (fast, per-instance)
        if (_localCache.TryGetValue(cacheKey, out Product? localCached))
        {
            _logger.LogDebug("Cache hit (local) for Product {ProductId}", id);
            return localCached;
        }

        // Layer 2: Distributed Redis (fast, shared across instances)
        var cachedJson = await _cache.GetStringAsync(cacheKey, ct);
        if (cachedJson != null)
        {
            var product = JsonSerializer.Deserialize<Product>(cachedJson);
            _localCache.Set(cacheKey, product, TimeSpan.FromMinutes(5));  // Refresh local
            _logger.LogDebug("Cache hit (distributed) for Product {ProductId}", id);
            return product;
        }

        // Layer 3: Database (slow)
        var dbProduct = await _context.Products.FirstOrDefaultAsync(
            p => p.Id == id, cancellationToken: ct);

        if (dbProduct != null)
        {
            // Populate both caches
            var json = JsonSerializer.Serialize(dbProduct);
            await _cache.SetStringAsync(
                cacheKey,
                json,
                new DistributedCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(CacheExpirationMinutes)
                },
                ct);
            _localCache.Set(cacheKey, dbProduct, TimeSpan.FromMinutes(5));

            _logger.LogInformation("Product {ProductId} cached (db miss)", id);
        }

        return dbProduct;
    }

    // Cache invalidation on write
    public async Task AddAsync(Product product, CancellationToken ct = default)
    {
        await _context.Products.AddAsync(product, ct);
        // Note: Don't save here (UnitOfWork does that)
    }

    public async Task UpdateAsync(Product product, CancellationToken ct = default)
    {
        _context.Products.Update(product);
        // Cache invalidation deferred to UnitOfWork.SaveChangesAsync
    }
}

// UnitOfWork invalidates caches on successful save
public class UnitOfWork : IUnitOfWork
{
    private readonly IDistributedCache _cache;

    public async Task SaveChangesAsync(CancellationToken ct = default)
    {
        var changedEntities = _context.ChangeTracker
            .Entries()
            .Where(e => e.State != EntityState.Unchanged)
            .Select(e => e.Entity)
            .ToList();

        // Save to database
        await _context.SaveChangesAsync(ct);

        // Invalidate relevant caches
        foreach (var entity in changedEntities)
        {
            var cacheKey = entity switch
            {
                Product p => $"product:{p.Id}",
                Cart c => $"cart:{c.UserId}",
                Order o => $"order:{o.Id}",
                _ => null
            };

            if (cacheKey != null)
            {
                await _cache.RemoveAsync(cacheKey, ct);
                _logger.LogInformation("Cache invalidated: {CacheKey}", cacheKey);
            }
        }
    }
}
```

### **Cache-Aside Pattern for Lists**

```csharp
public async Task<List<CategoryDto>> GetCategoriesAsync(CancellationToken ct = default)
{
    const string cacheKey = "categories:all";

    // Try cache first
    var cachedJson = await _cache.GetStringAsync(cacheKey, ct);
    if (cachedJson != null)
        return JsonSerializer.Deserialize<List<CategoryDto>>(cachedJson) ?? new();

    // Cache miss — fetch from database
    var categories = await _context.Categories
        .OrderBy(c => c.Name)
        .Select(c => new CategoryDto { Id = c.Id, Name = c.Name })
        .ToListAsync(ct);

    // Store in cache
    var json = JsonSerializer.Serialize(categories);
    await _cache.SetStringAsync(
        cacheKey,
        json,
        new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(1),
            SlidingExpiration = TimeSpan.FromMinutes(30)  // Reset if accessed
        },
        ct);

    return categories;
}
```

---

## 📝 Copy-Paste Templates Appendix

Ready-to-use templates for the most common files. Copy, fill in blanks, done.

### **Template 1: Service (Business Logic)**
```csharp
using ECommerce.Application.Interfaces;
using ECommerce.Core.Entities;
using ECommerce.Core.Exceptions;
using AutoMapper;
using Microsoft.Extensions.Logging;

namespace ECommerce.Application.Services;

public class ProductService : IProductService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserContext _currentUser;
    private readonly IMapper _mapper;
    private readonly ILogger<ProductService> _logger;

    public ProductService(
        IUnitOfWork unitOfWork,
        ICurrentUserContext currentUser,
        IMapper mapper,
        ILogger<ProductService> logger)
    {
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<ProductDto> GetProductByIdAsync(Guid productId, CancellationToken ct = default)
    {
        var product = await _unitOfWork.Products.GetByIdAsync(productId, ct)
            ?? throw new ProductNotFoundException(productId);

        _logger.LogInformation("Product {ProductId} retrieved", productId);
        return _mapper.Map<ProductDto>(product);
    }

    public async Task<PaginatedResponse<ProductDto>> GetProductsAsync(
        int pageNumber = 1,
        int pageSize = 20,
        CancellationToken ct = default)
    {
        pageNumber = Math.Max(1, pageNumber);
        pageSize = Math.Max(1, Math.Min(pageSize, 100));

        var skip = (pageNumber - 1) * pageSize;
        var totalItems = await _unitOfWork.Products.CountAsync(p => p.IsActive, ct);

        var products = await _unitOfWork.Products
            .GetQuery()
            .Where(p => p.IsActive)
            .OrderByDescending(p => p.CreatedAt)
            .Skip(skip)
            .Take(pageSize)
            .ToListAsync(ct);

        var items = _mapper.Map<List<ProductDto>>(products);
        var totalPages = (int)Math.Ceiling(totalItems / (decimal)pageSize);

        return new PaginatedResponse<ProductDto>
        {
            Items = items,
            PageNumber = pageNumber,
            PageSize = pageSize,
            TotalItems = totalItems,
            TotalPages = totalPages
        };
    }

    public async Task<ProductDto> CreateProductAsync(CreateProductDto dto, CancellationToken ct = default)
    {
        // Business rule: slug must be unique
        var existingProduct = await _unitOfWork.Products.GetBySlugAsync(dto.Slug, ct);
        if (existingProduct != null)
            throw new DuplicateProductSlugException(dto.Slug);

        var product = new Product
        {
            Name = dto.Name,
            Slug = dto.Slug,
            Price = dto.Price,
            Description = dto.Description,
            IsActive = true
        };

        await _unitOfWork.Products.AddAsync(product, ct);
        await _unitOfWork.SaveChangesAsync(ct);

        _logger.LogInformation("Product {ProductId} created with slug {ProductSlug}",
            product.Id, product.Slug);

        return _mapper.Map<ProductDto>(product);
    }
}
```

### **Template 2: Controller (HTTP Transport)**
```csharp
using ECommerce.API.Filters;
using ECommerce.Application.DTOs.Products;
using ECommerce.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ECommerce.API.Controllers;

/// <summary>Product management endpoints</summary>
[ApiController]
[Route("api/v1/[controller]")]
[Tags("Products")]
public class ProductsController : ControllerBase
{
    private readonly IProductService _service;

    public ProductsController(IProductService service) => _service = service;

    /// <summary>Retrieve a product by ID</summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<ProductDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
    [AllowAnonymous]
    public async Task<ActionResult> GetProductById(
        [FromRoute] Guid id,
        CancellationToken ct = default)
    {
        if (id == Guid.Empty)
            return BadRequest(new ErrorResponse { Message = "Invalid product ID", Code = "INVALID_ID" });

        var result = await _service.GetProductByIdAsync(id, ct);
        return Ok(ApiResponse<ProductDto>.Success(result));
    }

    /// <summary>Retrieve paginated list of active products</summary>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<PaginatedResponse<ProductDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
    [AllowAnonymous]
    public async Task<ActionResult> GetProducts(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        if (pageNumber < 1) pageNumber = 1;
        if (pageSize < 1 || pageSize > 100) pageSize = 20;

        var result = await _service.GetProductsAsync(pageNumber, pageSize, ct);
        return Ok(ApiResponse<PaginatedResponse<ProductDto>>.Success(result));
    }

    /// <summary>Create a new product (Admin only)</summary>
    [HttpPost]
    [Authorize(Roles = "Admin,SuperAdmin")]
    [ProducesResponseType(typeof(ApiResponse<ProductDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status422UnprocessableEntity)]
    [ValidationFilter]
    public async Task<ActionResult> CreateProduct(
        [FromBody] CreateProductDto dto,
        CancellationToken ct = default)
    {
        var result = await _service.CreateProductAsync(dto, ct);
        return CreatedAtAction(nameof(GetProductById), new { id = result.Id },
            ApiResponse<ProductDto>.Success(result));
    }
}
```

### **Template 3: FluentValidation Validator**
```csharp
using FluentValidation;
using ECommerce.Application.DTOs.Products;

namespace ECommerce.Application.Validators.Products;

public class CreateProductDtoValidator : AbstractValidator<CreateProductDto>
{
    public CreateProductDtoValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Name is required")
            .MaximumLength(255).WithMessage("Name must not exceed 255 characters");

        RuleFor(x => x.Slug)
            .NotEmpty().WithMessage("Slug is required")
            .Matches(@"^[a-z0-9]+(?:-[a-z0-9]+)*$").WithMessage("Slug must be lowercase alphanumeric with hyphens only");

        RuleFor(x => x.Price)
            .GreaterThan(0).WithMessage("Price must be greater than 0")
            .LessThanOrEqualTo(999999.99m).WithMessage("Price must not exceed 999,999.99");

        RuleFor(x => x.Description)
            .MaximumLength(2000).WithMessage("Description must not exceed 2000 characters");
    }
}
```

### **Template 4: Repository Query Method**
```csharp
// In ProductRepository.cs
public async Task<Product?> GetBySlugAsync(string slug, CancellationToken ct = default)
{
    return await _context.Products
        .AsNoTracking()  // For read-only queries
        .FirstOrDefaultAsync(p => p.Slug == slug, cancellationToken: ct);
}

public async Task<IReadOnlyList<Product>> GetActiveByCategoryAsync(
    Guid categoryId,
    int skip = 0,
    int take = 20,
    CancellationToken ct = default)
{
    return await _context.Products
        .AsNoTracking()
        .Where(p => p.CategoryId == categoryId && p.IsActive)
        .OrderByDescending(p => p.CreatedAt)
        .Skip(skip)
        .Take(take)
        .ToListAsync(ct);
}

public async Task<int> CountAsync(Expression<Func<Product, bool>>? predicate = null, CancellationToken ct = default)
{
    var query = _context.Products.AsQueryable();
    if (predicate != null)
        query = query.Where(predicate);
    return await query.CountAsync(ct);
}
```

### **Template 5: Unit Test (Service with Mocks)**
```csharp
using ECommerce.Application.Services;
using ECommerce.Core.Entities;
using ECommerce.Core.Exceptions;
using Moq;
using FluentAssertions;
using AutoMapper;

namespace ECommerce.Tests.Unit.Services;

[TestClass]
public class ProductServiceTests
{
    private Mock<IUnitOfWork> _mockUnitOfWork = null!;
    private Mock<ICurrentUserContext> _mockCurrentUser = null!;
    private Mock<IMapper> _mockMapper = null!;
    private Mock<ILogger<ProductService>> _mockLogger = null!;
    private ProductService _service = null!;

    [TestInitialize]
    public void Setup()
    {
        _mockUnitOfWork = new Mock<IUnitOfWork>();
        _mockCurrentUser = new Mock<ICurrentUserContext>();
        _mockMapper = new Mock<IMapper>();
        _mockLogger = new Mock<ILogger<ProductService>>();

        _service = new ProductService(_mockUnitOfWork.Object, _mockCurrentUser.Object, _mockMapper.Object, _mockLogger.Object);
    }

    [TestMethod]
    public async Task GetProductByIdAsync_ExistingProduct_ReturnsProduct()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var product = TestDataFactory.CreateProduct(id: productId, name: "Laptop");
        var productDto = new ProductDto { Id = productId, Name = "Laptop" };

        _mockUnitOfWork
            .Setup(u => u.Products.GetByIdAsync(productId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(product);

        _mockMapper
            .Setup(m => m.Map<ProductDto>(product))
            .Returns(productDto);

        // Act
        var result = await _service.GetProductByIdAsync(productId);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().Be(productId);
        result.Name.Should().Be("Laptop");
    }

    [TestMethod]
    public async Task GetProductByIdAsync_NonExistingProduct_ThrowsNotFoundException()
    {
        // Arrange
        var productId = Guid.NewGuid();
        _mockUnitOfWork
            .Setup(u => u.Products.GetByIdAsync(productId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Product?)null);

        // Act
        Func<Task> act = async () => await _service.GetProductByIdAsync(productId);

        // Assert
        await act.Should().ThrowAsync<ProductNotFoundException>();
    }

    [TestMethod]
    public async Task CreateProductAsync_DuplicateSlug_ThrowsException()
    {
        // Arrange
        var dto = new CreateProductDto { Name = "Laptop", Slug = "laptop", Price = 999.99m };
        var existingProduct = TestDataFactory.CreateProduct(slug: "laptop");

        _mockUnitOfWork
            .Setup(u => u.Products.GetBySlugAsync("laptop", It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingProduct);

        // Act
        Func<Task> act = async () => await _service.CreateProductAsync(dto);

        // Assert
        await act.Should().ThrowAsync<DuplicateProductSlugException>();
    }
}
```

### **Template 6: Result Pattern in Service**
```csharp
public class PaymentService : IPaymentService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IPaymentGateway _gateway;
    private readonly ILogger<PaymentService> _logger;

    // No exceptions for predictable business logic
    public async Task<Result<PaymentDto>> ProcessPaymentAsync(
        Guid orderId,
        PaymentMethodDto method,
        CancellationToken ct = default)
    {
        // Validate order exists and status
        var order = await _unitOfWork.Orders.GetByIdAsync(orderId, ct);
        if (order == null)
            return Result<PaymentDto>.Fail("ORDER_NOT_FOUND");

        if (order.Status != OrderStatus.Pending)
            return Result<PaymentDto>.Fail("ORDER_NOT_PENDING");

        // Validate payment method
        if (!IsValidPaymentMethod(method))
            return Result<PaymentDto>.Fail("INVALID_PAYMENT_METHOD");

        // Process with gateway
        try
        {
            var gatewayResult = await _gateway.ChargeAsync(
                method, order.Total, orderId.ToString(), ct);

            if (!gatewayResult.IsSuccess)
                return Result<PaymentDto>.Fail(gatewayResult.ErrorCode);

            // Record payment
            var payment = new Payment
            {
                OrderId = orderId,
                TransactionId = gatewayResult.TransactionId,
                Amount = order.Total,
                Status = PaymentStatus.Success
            };
            await _unitOfWork.Payments.AddAsync(payment, ct);
            order.Status = OrderStatus.Paid;
            await _unitOfWork.SaveChangesAsync(ct);

            _logger.LogInformation("Payment {PaymentId} processed for Order {OrderId}",
                payment.Id, orderId);

            return Result<PaymentDto>.Ok(new PaymentDto { Id = payment.Id });
        }
        catch (HttpRequestException ex)
        {
            // Infrastructure error — log and return retry hint
            _logger.LogError(ex, "Gateway connection failed for Order {OrderId}", orderId);
            return Result<PaymentDto>.Fail("GATEWAY_UNAVAILABLE_RETRY");
        }
    }
}
```

---

## 📚 Deep Reference

### **1. Feature Delivery Workflow**

**MUST** follow this order for every feature:

1. **Domain (Core)** — Add/extend entity, enum, exception
2. **Contracts (Application)** — DTOs, validators
3. **Persistence (Infrastructure)** — Repository queries, migrations
4. **Business Logic (Service)** — Inject `IUnitOfWork`, `IMapper`, `ILogger<T>`
5. **Transport (Controller)** — Validate params, call service, return `ApiResponse<T>`
6. **Cross-cutting** — Mappings, tests, logging, migrations

### **2. C# Naming Conventions**

| Element | Convention | Example |
|---------|-----------|---------|
| **Private fields** | `_camelCase` | `private readonly IMapper _mapper;` |
| **Local variables** | `camelCase` | `var productDto = ...;` |
| **Parameters** | `camelCase` | `(Guid productId, CancellationToken ct)` |
| **Public properties** | `PascalCase` | `public string Name { get; set; }` |
| **Methods** | `PascalCase` + `Async` suffix | `CreateProductAsync(...)` |
| **Interfaces** | `I` prefix | `IProductService`, `IUnitOfWork` |
| **Classes** | `PascalCase` | `ProductService`, `OrderRepository` |
| **Constants** | `PascalCase` | `private const int MaxPageSize = 100;` |
| **Enums** | Singular `PascalCase` | `OrderStatus`, not `OrderStatuses` |
| **Namespaces** | `Company.Project.Layer.Feature` | `ECommerce.Application.Services` |

**MUST** rules:
- Explicit visibility: `private string _foo;` not `string _foo;`
- File-scoped namespaces: `namespace ECommerce.Core.Entities;`
- One class per file (except small DTOs)
- Allman style braces: opening brace on new line

### **3. Security (PostgreSQL-Specific)**

#### **Authentication: JWT + httpOnly Cookies**

**Configuration**: `ECommerce.API/Configuration/JwtOptions.cs`

```csharp
public class JwtOptions
{
    public const string SectionName = "Jwt";
    public string SecretKey { get; set; } = null!;  // MIN 32 chars, validated at startup
    public string Issuer { get; set; } = null!;
    public string Audience { get; set; } = null!;
    public int ExpirationMinutes { get; set; } = 60;
    public int RefreshTokenExpirationDays { get; set; } = 7;
    public int ClockSkewSeconds { get; set; } = 0;   // Strict — no drift tolerance
}
```

**MUST Rules**:
- ✅ Secret key 32+ characters, validated at startup
- ✅ Never store JWT in `localStorage` — use httpOnly cookies only
- ✅ `ClockSkew = 0` — strict expiry
- ✅ Tokens delivered via `Authorization: Bearer <token>` header OR httpOnly cookie

#### **Authorization**

**MUST** rules:
- Class-level `[Authorize]` with `[AllowAnonymous]` overrides
- Role-based: `[Authorize(Roles = "Admin,SuperAdmin")]`
- Ownership checks in service layer (not controller)
- Never trust client-provided user IDs — always use `ICurrentUserContext`

#### **User Context Injection**

```csharp
public interface ICurrentUserContext
{
    Guid UserId { get; }
    string Email { get; }
    IReadOnlyList<string> Roles { get; }
    bool IsAuthenticated { get; }
}

// Register: services.AddScoped<ICurrentUserContext, CurrentUserContext>();
// Usage: Inject into services, never pass userId as parameter
```

#### **Rate Limiting**

| Policy | Limit | Window | Applied To |
|--------|-------|--------|------------|
| `Global` | 100 req (prod) / 1000 (dev) | 60 seconds | All endpoints (by IP) |
| `AuthLimit` | 5 req (prod) / 50 (dev) | 60 seconds | Login, Register |
| `PasswordResetLimit` | 3 req (prod) / 30 (dev) | 15 minutes | Password reset |

**MUST** apply `[EnableRateLimiting("AuthLimit")]` on login/register endpoints.

#### **CSRF Protection**

**MUST** rules:
- Double-submit cookie pattern
- `XSRF-TOKEN` cookie is `HttpOnly: false` (readable by JS)
- `X-XSRF-TOKEN` header validated on POST/PUT/DELETE/PATCH
- Auth endpoints (login/register) excluded

#### **Input Validation**

**MUST** use three layers:
1. **Controller** — parameter bounds (Guid.Empty, pageSize > 100)
2. **FluentValidation** — DTO shape, format, ranges
3. **Service** — business rules, uniqueness, ownership

**MUST** use whitelist for enums (not blacklist):
```csharp
private static readonly HashSet<string> AllowedMethods = new(StringComparer.OrdinalIgnoreCase)
{
    "stripe", "paypal", "credit_card", "card", "debit_card", "apple_pay", "google_pay"
};
```

### **4. Database & Migrations (PostgreSQL)**

#### **Migration Naming**

**MUST** use descriptive names:
```bash
dotnet ef migrations add AddProductTable
dotnet ef migrations add AlterProductAddBarcodeColumn
dotnet ef migrations add CreateProductSlugUniqueIndex
```

**MUST** include `Down()` for rollback:
```csharp
protected override void Up(MigrationBuilder mb)
{
    mb.CreateTable("Products", ...);
    mb.CreateIndex("IX_Products_Slug", "Products", "Slug", unique: true);
}

protected override void Down(MigrationBuilder mb)
{
    mb.DropTable("Products");
}
```

#### **RowVersion & Concurrency (PostgreSQL xmin)**

PostgreSQL uses implicit `xmin` column for optimistic locking:

```csharp
public class Order
{
    [Timestamp]
    public byte[]? RowVersion { get; set; }  // Maps to xmin in PostgreSQL
}

// EF Core handles concurrency automatically on SaveChangesAsync()
// Catch DbUpdateConcurrencyException and handle conflict
```

#### **Indexes**

**MUST** add indexes for:
- `WHERE` clauses: `Product.Slug`, `Order.UserId`
- `ORDER BY` columns: `CreatedAt`
- Foreign keys: `Product.CategoryId`
- Composite filters: `(CategoryId, IsActive)` for multi-column WHERE

```csharp
modelBuilder.Entity<Product>()
    .HasIndex(p => p.Slug)
    .IsUnique();

modelBuilder.Entity<Product>()
    .HasIndex(p => new { p.CategoryId, p.IsActive });
```

### **5. Logging & Observability**

**MUST** use structured logging with named placeholders:

```csharp
// ✅ GOOD
_logger.LogInformation("Order {OrderId} created by {UserId}, total {OrderTotal:C}",
    order.Id, order.UserId, order.TotalAmount);

// ❌ BAD
_logger.LogInformation($"Order {order.Id} created");
```

**Log Levels**:
| Level | Use For |
|-------|---------|
| `Trace` | Method entry (dev only) |
| `Debug` | State changes, cache misses (dev only) |
| `Information` | Business events (login, order created) |
| `Warning` | Unexpected but recoverable (rate limit, retry) |
| `Error` | Failures needing investigation (payment failed) |
| `Fatal` | App cannot continue (DB unavailable after retries) |

**MUST NOT log**: passwords, tokens, credit cards, API keys, emails (plaintext).

**Correlation ID**: All logs in request automatically include `X-Correlation-ID` for tracing.

### **6. Testing**

**MUST** test structure:

```
ECommerce.Tests/
├── Helpers/
│   ├── TestDataFactory.cs       ← Bogus-based factory
│   ├── MockHelpers.cs
│   └── TestAsyncQueryProvider.cs
├── Unit/
│   ├── Repositories/            ← In-memory DB tests
│   ├── Services/                ← Mocked dependency tests
│   ├── Validators/              ← Validation rule tests
│   └── Middleware/
└── Integration/                 ← WebApplicationFactory tests
```

**MUST** test naming: `MethodName_Scenario_ExpectedResult`

```csharp
[TestMethod]
public async Task CreateProductAsync_DuplicateSlug_ThrowsException() { }

[TestMethod]
public async Task GetProductsAsync_InvalidPageNumber_SetsToOne() { }
```

**MUST** use Arrange-Act-Assert pattern:

```csharp
// Arrange
var product = TestDataFactory.CreateProduct(slug: "test");
_mockRepo.Setup(r => r.GetBySlugAsync("test"))
    .ReturnsAsync(product);

// Act
var result = await _service.GetProductBySlugAsync("test");

// Assert
result.Should().NotBeNull();
```

**Code Coverage Thresholds**:
| Layer | Minimum |
|-------|---------|
| Services | 80% |
| Repositories | 70% |
| Controllers | 60% |
| Validators | 100% |
| Entities | 50% |

### **7. API Documentation (OpenAPI/Swagger)**

**MUST** include on every endpoint:
- `///` XML summary and parameter descriptions
- `[ProducesResponseType]` for all codes: 200, 201, 400, 401, 403, 404, 409, 422, 500
- `[Authorize]` or `[AllowAnonymous]`
- `[Tags("FeatureName")]`

```csharp
/// <summary>Retrieve a product by ID</summary>
/// <param name="id">The unique product identifier</param>
/// <param name="ct">Cancellation token</param>
/// <remarks>
/// **Caching**: Results cached for 1 hour
/// 
/// **Authorization**: Not required — public data
/// </remarks>
/// <response code="200">Product found</response>
/// <response code="404">Product not found</response>
[HttpGet("{id:guid}")]
[ProducesResponseType(typeof(ApiResponse<ProductDto>), StatusCodes.Status200OK)]
[ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
[ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
[AllowAnonymous]
[Tags("Products")]
public async Task<ActionResult> GetProductById(Guid id, CancellationToken ct) { }
```

### **8. Idempotency for Financial Operations**

**MUST** for payment/order endpoints: require `Idempotency-Key` header (UUID format).

```csharp
[HttpPost]
public async Task<ActionResult> CreateOrder(
    [FromBody] CreateOrderDto dto,
    [FromHeader(Name = "Idempotency-Key")] string? idempotencyKey,
    CancellationToken ct = default)
{
    if (string.IsNullOrWhiteSpace(idempotencyKey) || !Guid.TryParse(idempotencyKey, out _))
        return BadRequest(new { error = "Idempotency-Key required, must be UUID" });

    // Check cache for duplicate
    var cached = await _cache.GetAsync<OrderDto>($"idempotent:{idempotencyKey}", ct);
    if (cached != null) return CreatedAtAction(nameof(GetOrder), cached);

    // Process
    var result = await _service.CreateOrderAsync(dto, ct);
    await _cache.SetAsync($"idempotent:{idempotencyKey}", result, TimeSpan.FromHours(24), ct);

    return CreatedAtAction(nameof(GetOrder), ApiResponse<OrderDto>.Success(result));
}
```

**MUST NOT** cache failed results — allow retry with same key.

### **9. Configuration & Environment Management**

**MUST** use Options Pattern with validation:

```csharp
// Define with data annotation validation
public class JwtOptions
{
    public const string SectionName = "Jwt";
    
    [Required(ErrorMessage = "SecretKey required")]
    [StringLength(int.MaxValue, MinimumLength = 32, ErrorMessage = "SecretKey must be 32+ chars")]
    public string SecretKey { get; set; } = null!;
    
    [Required]
    public string Issuer { get; set; } = null!;
    
    [Range(5, 1440)]
    public int ExpirationMinutes { get; set; } = 60;
}

// Register with validation
services.Configure<JwtOptions>(options => 
    configuration.GetSection(JwtOptions.SectionName).Bind(options))
    .ValidateDataAnnotations()  // Validates on startup
    .ValidateOnStart();         // Fail if invalid

// Inject
public MyService(IOptionsSnapshot<JwtOptions> options)  // Snapshot for reloading
{
    _options = options.Value;
}
```

**MUST** never read `IConfiguration` directly in services.

**appsettings Structure**:
- `appsettings.json` — committed, no secrets (empty placeholders)
- `appsettings.Development.json` — gitignored, local secrets
- Environment variables — production secrets via `Jwt__SecretKey` convention
- `.env` file (for Docker) — see [DEPLOYMENT.md](./DEPLOYMENT.md)

**Startup Validation**:
```csharp
builder.Configuration.ValidateRequiredConfiguration();  // Fail fast on missing secrets

// Also validate custom rules
var jwtOptions = new JwtOptions();
configuration.GetSection(JwtOptions.SectionName).Bind(jwtOptions);
if (jwtOptions.SecretKey.Length < 32)
    throw new InvalidOperationException("JWT SecretKey must be 32+ characters");
```

---

## 🎯 API Versioning Strategy

### **Semantic Versioning (Major.Minor.Patch)**

```csharp
// Route by version in URL (clearest for REST)
[ApiController]
[Route("api/v{version:apiVersion}/[controller]")]
[ApiVersion("1.0")]
[ApiVersion("1.1")]
public class ProductsController : ControllerBase
{
    // Available in v1.0 and v1.1
    [HttpGet("{id}")]
    [MapToApiVersion("1.0")]
    [MapToApiVersion("1.1")]
    public Task<ActionResult> GetProduct_V1(Guid id) { }

    // Only in v1.1 (new field)
    [HttpGet("{id}/enriched")]
    [MapToApiVersion("1.1")]
    public Task<ActionResult> GetProduct_V1_1_Enriched(Guid id) { }

    // v2.0 breaking change (different shape)
    [HttpGet("{id}")]
    [MapToApiVersion("2.0")]
    public Task<ActionResult> GetProduct_V2(Guid id) { }
}
```

**Versioning Rules**:
- **Major (v1→v2)**: Breaking changes (removed fields, incompatible types)
- **Minor (v1.0→v1.1)**: Backward-compatible (new fields, new endpoints)
- **Patch (v1.0.0→v1.0.1)**: Bug fixes only

**Deprecation Path**:
```csharp
[Obsolete("Use v1.1 instead. Will be removed 2027-03-31.", false)]
[MapToApiVersion("1.0")]
public Task<ActionResult> GetProduct_V1(Guid id) { }
```

---

## 📊 Audit Trail & Soft Deletes

### **Track Who Changed What When**

```csharp
public class Order : BaseEntity
{
    public required string UserId { get; set; }
    public required decimal Total { get; set; }

    // Audit fields
    public Guid? DeletedBy { get; set; }
    public DateTime? DeletedAt { get; set; }
    public bool IsDeleted { get; set; }

    // Status history
    [NotMapped]
    public List<OrderStatusHistory> StatusHistory { get; set; } = new();
}

public class OrderStatusHistory
{
    public Guid Id { get; set; }
    public Guid OrderId { get; set; }
    public OrderStatus OldStatus { get; set; }
    public OrderStatus NewStatus { get; set; }
    public string ChangedBy { get; set; } = null!;  // Email or UserId
    public DateTime ChangedAt { get; set; }
    public string? Reason { get; set; }
}

// Soft delete on SaveChanges
public override int SaveChanges()
{
    foreach (var entry in ChangeTracker.Entries())
    {
        if (entry.State == EntityState.Deleted && entry.Entity is BaseEntity entity)
        {
            entry.State = EntityState.Modified;
            entity.IsDeleted = true;
            entity.DeletedAt = DateTime.UtcNow;
        }
    }
    return base.SaveChanges();
}

// Query filters: automatically exclude deleted
public DbSet<Order> Orders { get; set; }

protected override void OnModelCreating(ModelBuilder mb)
{
    mb.Entity<Order>().HasQueryFilter(o => !o.IsDeleted);
}

// To query including deleted:
public async Task<Order?> GetByIdIncludingDeletedAsync(Guid id, CancellationToken ct)
{
    return await _context.Orders.IgnoreQueryFilters()
        .FirstOrDefaultAsync(o => o.Id == id, ct);
}
```

### **Audit Log Middleware**

```csharp
public class AuditMiddleware
{
    public async Task InvokeAsync(HttpContext context, IServiceProvider services)
    {
        var startTime = DateTime.UtcNow;
        var originalBody = context.Response.Body;

        try
        {
            using var memoryStream = new MemoryStream();
            context.Response.Body = memoryStream;

            await _next(context);

            var duration = DateTime.UtcNow - startTime;
            var statusCode = context.Response.StatusCode;
            var method = context.Request.Method;
            var path = context.Request.Path.Value;

            using var scope = services.CreateScope();
            var auditService = scope.ServiceProvider.GetRequiredService<IAuditService>();
            var currentUser = scope.ServiceProvider.GetRequiredService<ICurrentUserContext>();

            await auditService.LogRequestAsync(new AuditLog
            {
                UserId = currentUser.IsAuthenticated ? currentUser.UserId : null,
                Method = method,
                Path = path,
                StatusCode = statusCode,
                DurationMs = (int)duration.TotalMilliseconds,
                Timestamp = DateTime.UtcNow
            });

            await memoryStream.CopyToAsync(originalBody);
        }
        finally
        {
            context.Response.Body = originalBody;
        }
    }
}

// Register in Program.cs
app.UseMiddleware<AuditMiddleware>();
```

---

## 📞 Support & Feedback

- **Question?** Check the **Quick Start** first (10 MUST rules)
- **Implementing a feature?** Follow the **PR Checklist**
- **Need templates?** Copy from **Templates Appendix**
- **Design pattern?** See **Result Pattern**, **Concurrency**, **Caching** sections
- **Performance tuning?** Check **Query Optimization Patterns**
- **Inconsistency found?** Update this guide in the same PR — it's a living document

**Last Updated**: March 2026 | **Database**: PostgreSQL | **Status**: Production-Ready
