# The API Layer: Controllers, Result Pattern, and Supporting Infrastructure

**Read this after `04-value-types-and-dtos.md`.**

---

## Controllers — Where, Why, and How

### Where they live

```
ECommerce.API/
└── Controllers/
    ├── ProductsController.cs     ← Catalog
    ├── CategoriesController.cs   ← Catalog
    ├── UsersController.cs        ← Identity
    ├── CartController.cs         ← Shopping
    └── OrdersController.cs       ← Ordering
```

One controller per primary resource. They stay in the **API project only** — never in Application or Domain.

### Why controllers exist

Controllers are the HTTP surface of your application. Their only jobs are:
1. Receive an HTTP request
2. Extract data (from body, route, query string)
3. Dispatch to MediatR
4. Convert the `Result<T>` to an HTTP response

That's it. Controllers contain **zero business logic**.

### What a controller looks like after migration

```csharp
[ApiController]
[Route("api/products")]
public class ProductsController : ControllerBase
{
    private readonly IMediator _mediator;

    public ProductsController(IMediator mediator)
        => _mediator = mediator;

    // GET api/products?page=1&pageSize=20&search=shirt
    [HttpGet]
    public async Task<IActionResult> GetProducts(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? search = null,
        [FromQuery] Guid? categoryId = null,
        CancellationToken ct = default)
    {
        var result = await _mediator.Send(
            new GetProductsQuery(page, pageSize, search, categoryId), ct);

        return result.ToActionResult();
    }

    // GET api/products/{slug}
    [HttpGet("{slug}")]
    public async Task<IActionResult> GetBySlug(string slug, CancellationToken ct)
    {
        var result = await _mediator.Send(new GetProductBySlugQuery(slug), ct);
        return result.ToActionResult();
    }

    // POST api/products
    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Create(
        [FromBody] CreateProductCommand command,
        CancellationToken ct)
    {
        var result = await _mediator.Send(command, ct);
        return result.ToCreatedActionResult($"api/products/{result.Value?.Slug}");
    }

    // PUT api/products/{id}
    [HttpPut("{id:guid}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Update(
        Guid id,
        [FromBody] UpdateProductRequest request,
        CancellationToken ct)
    {
        // Route param + body merged into one command
        var result = await _mediator.Send(
            new UpdateProductCommand(id, request.Name, request.Description, request.Price), ct);

        return result.ToActionResult();
    }
}
```

**Notice:**
- No `try/catch` — errors are handled by the Result pattern and the GlobalExceptionHandler
- No `if (result.IsSuccess)` scattered everywhere — `ToActionResult()` extension handles it
- No business logic — no "if product exists do X"
- The controller doesn't know what `CreateProductCommand` does internally

---

## The Result Pattern — How It Works

### The problem it solves

Without Result<T>, every method that can fail has two options:
1. Throw an exception — but exceptions for expected outcomes (item not found) are expensive and noisy
2. Return null — but null doesn't tell you WHY it failed

Result<T> is a **discriminated union** — it's either a success with a value, or a failure with errors. You can't accidentally use the value without checking if it succeeded.

### What Result<T> looks like

```csharp
// Simplified version — your project likely has something similar
public class Result<T>
{
    public bool IsSuccess { get; }
    public bool IsFailure => !IsSuccess;
    public T? Value { get; }
    public string ErrorCode { get; }
    public string ErrorMessage { get; }
    public Dictionary<string, string[]>? ValidationErrors { get; }

    private Result(bool isSuccess, T? value, string errorCode, string errorMessage,
                   Dictionary<string, string[]>? validationErrors = null)
    {
        IsSuccess = isSuccess;
        Value = value;
        ErrorCode = errorCode;
        ErrorMessage = errorMessage;
        ValidationErrors = validationErrors;
    }

    public static Result<T> Ok(T value)
        => new(true, value, string.Empty, string.Empty);

    public static Result<T> Fail(string errorCode, string message)
        => new(false, default, errorCode, message);

    public static Result<T> NotFound(string message = "Resource not found.")
        => Fail(ErrorCodes.NotFound, message);

    public static Result<T> Unauthorized(string message = "Unauthorized.")
        => Fail(ErrorCodes.Unauthorized, message);
}

// Non-generic version for commands that return nothing
public class Result : Result<Unit>
{
    public static Result Ok() => new Result(true, Unit.Value, string.Empty, string.Empty);
    public static new Result Fail(string errorCode, string message)
        => new Result(false, default, errorCode, message);
}

// Placeholder for "no value"
public record Unit { public static readonly Unit Value = new(); }
```

### How handlers use Result<T>

```csharp
public async Task<Result<ProductDetailDto>> Handle(
    CreateProductCommand command, CancellationToken ct)
{
    // Authorization check — fast fail before any domain work
    if (!_currentUser.IsInRole("Admin"))
        return Result<ProductDetailDto>.Unauthorized();

    // Expected business failure — use Result.Fail
    var existingProduct = await _repository.GetBySkuAsync(command.Sku, ct);
    if (existingProduct != null)
        return Result<ProductDetailDto>.Fail(ErrorCodes.Conflict, $"SKU '{command.Sku}' already exists.");

    // Domain invariant violation — let the aggregate throw DomainException
    // (caught by GlobalExceptionHandler → 422)
    var product = Product.Create(
        ProductName.Create(command.Name),   // throws if name is empty
        Money.Create(command.Price, "USD"), // throws if price < 0
        Sku.Create(command.Sku),
        command.CategoryId
    );

    await _repository.AddAsync(product, ct);
    await _unitOfWork.SaveChangesAsync(ct);

    return Result<ProductDetailDto>.Ok(product.ToDetailDto());
}
```

---

## Removing Boilerplate: The ToActionResult() Extension

Without an extension method, every controller action has this noise:
```csharp
// BAD — repeated in every action
var result = await _mediator.Send(command, ct);
if (result.IsSuccess)
    return Ok(new ApiResponse<ProductDetailDto>(result.Value));
else if (result.ErrorCode == ErrorCodes.NotFound)
    return NotFound(new ApiResponse<ProductDetailDto>(result.ErrorMessage));
else
    return BadRequest(new ApiResponse<ProductDetailDto>(result.ErrorMessage));
```

Replace it with extension methods:

```csharp
// In ECommerce.API/Extensions/ResultExtensions.cs
public static class ResultExtensions
{
    public static IActionResult ToActionResult<T>(this Result<T> result)
    {
        if (result.IsSuccess)
            return new OkObjectResult(ApiResponse<T>.Success(result.Value!));

        return result.ErrorCode switch
        {
            ErrorCodes.NotFound    => new NotFoundObjectResult(ApiResponse<T>.Fail(result.ErrorMessage)),
            ErrorCodes.Unauthorized => new UnauthorizedObjectResult(ApiResponse<T>.Fail(result.ErrorMessage)),
            ErrorCodes.Conflict    => new ConflictObjectResult(ApiResponse<T>.Fail(result.ErrorMessage)),
            _                      => new BadRequestObjectResult(ApiResponse<T>.Fail(result.ErrorMessage))
        };
    }

    public static IActionResult ToCreatedActionResult<T>(this Result<T> result, string location)
    {
        if (result.IsSuccess)
            return new CreatedResult(location, ApiResponse<T>.Success(result.Value!));

        return result.ToActionResult();
    }
}
```

Now every controller action is:
```csharp
var result = await _mediator.Send(command, ct);
return result.ToActionResult();  // ← one line, no if/else
```

---

## ApiResponse<T> — The Consistent HTTP Envelope

Every HTTP response from this API has the same shape. Clients can always expect the same structure.

```csharp
// In ECommerce.API/Models/ApiResponse.cs  (or ECommerce.Core)
public class ApiResponse<T>
{
    public bool Success { get; }
    public T? Data { get; }
    public string? Error { get; }
    public string? ErrorCode { get; }

    private ApiResponse(bool success, T? data, string? error, string? errorCode)
    {
        Success = success;
        Data = data;
        Error = error;
        ErrorCode = errorCode;
    }

    public static ApiResponse<T> Success(T data)
        => new(true, data, null, null);

    public static ApiResponse<T> Fail(string error, string? errorCode = null)
        => new(false, default, error, errorCode);
}
```

HTTP responses always look like one of these:

```json
// Success
{ "success": true, "data": { "id": "...", "name": "Widget" } }

// Failure
{ "success": false, "error": "SKU already exists.", "errorCode": "CONFLICT" }
```

Clients never need to parse HTTP status codes alone — they can always read `success` and `errorCode`.

---

## GlobalExceptionHandler — The Safety Net

Even with Result<T> everywhere, things can still throw:
- `DomainException` from aggregate invariant violations
- `ValidationException` from the MediatR pipeline behavior
- Unexpected exceptions (null reference, timeout, etc.)

The GlobalExceptionHandler catches ALL of these and converts them to proper HTTP responses:

```csharp
// In ECommerce.API/Middleware/GlobalExceptionHandler.cs
public class GlobalExceptionHandler : IExceptionHandler
{
    private readonly ILogger<GlobalExceptionHandler> _logger;

    public GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger)
        => _logger = logger;

    public async ValueTask<bool> TryHandleAsync(
        HttpContext context,
        Exception exception,
        CancellationToken ct)
    {
        var (statusCode, errorCode, message) = exception switch
        {
            ValidationException ve => (
                StatusCodes.Status400BadRequest,
                "VALIDATION_ERROR",
                FormatValidationErrors(ve)),

            DomainException de => (
                StatusCodes.Status422UnprocessableEntity,  // 422 = "valid request, domain rejected it"
                de.Code,
                de.Message),

            UnauthorizedAccessException => (
                StatusCodes.Status401Unauthorized,
                "UNAUTHORIZED",
                "Authentication required."),

            _ => (
                StatusCodes.Status500InternalServerError,
                "INTERNAL_ERROR",
                "An unexpected error occurred.")
        };

        if (statusCode == 500)
            _logger.LogError(exception, "Unhandled exception");

        context.Response.StatusCode = statusCode;
        await context.Response.WriteAsJsonAsync(
            ApiResponse<object>.Fail(message, errorCode), ct);

        return true;
    }

    private static string FormatValidationErrors(ValidationException ve)
        => string.Join("; ", ve.Errors.Select(e => $"{e.PropertyName}: {e.ErrorMessage}"));
}
```

Register in Program.cs:
```csharp
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();

// ...

app.UseExceptionHandler();
```

**Why this matters**: With GlobalExceptionHandler in place:
- `DomainException` from aggregates → 422 (domain rejected it, not a bug)
- `ValidationException` from pipeline → 400 (client sent malformed data)
- Everything else → 500 with logging (real bug)

Controllers never need `try/catch`. The handler guarantees consistent error responses.

---

## ErrorCodes — Consistent Business Error Constants

Scatter `"PRODUCT_NOT_FOUND"` strings across 20 handlers and you'll have typos. Centralize them:

```csharp
// In ECommerce.Core (shared, no dependencies)
// or in ECommerce.SharedKernel/Constants/ErrorCodes.cs
public static class ErrorCodes
{
    // Generic
    public const string NotFound     = "NOT_FOUND";
    public const string Unauthorized = "UNAUTHORIZED";
    public const string Forbidden    = "FORBIDDEN";
    public const string Conflict     = "CONFLICT";
    public const string Validation   = "VALIDATION_ERROR";

    // Catalog
    public static class Catalog
    {
        public const string ProductNotFound   = "PRODUCT_NOT_FOUND";
        public const string SkuAlreadyExists  = "SKU_ALREADY_EXISTS";
        public const string CategoryNotFound  = "CATEGORY_NOT_FOUND";
        public const string InvalidPrice      = "INVALID_PRICE";
    }

    // Ordering
    public static class Ordering
    {
        public const string OrderNotFound          = "ORDER_NOT_FOUND";
        public const string CannotCancelShipped    = "CANNOT_CANCEL_SHIPPED_ORDER";
        public const string InsufficientStock      = "INSUFFICIENT_STOCK";
    }
}
```

Usage:
```csharp
return Result<ProductDetailDto>.Fail(ErrorCodes.Catalog.SkuAlreadyExists, $"SKU '{command.Sku}' already exists.");
```

---

## ICurrentUserService — Getting the Logged-in User in Handlers

Handlers need to know who is calling them (for authorization and audit). Don't inject `IHttpContextAccessor` directly into handlers — that couples application logic to HTTP.

Instead, use an abstraction:

```csharp
// In ECommerce.SharedKernel/Interfaces/ICurrentUserService.cs
public interface ICurrentUserService
{
    Guid? UserId { get; }
    string? Email { get; }
    bool IsAuthenticated { get; }
    bool IsInRole(string role);
}
```

Implementation in API (reads from `HttpContext`):
```csharp
// In ECommerce.API/Services/CurrentUserService.cs
public class CurrentUserService : ICurrentUserService
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CurrentUserService(IHttpContextAccessor httpContextAccessor)
        => _httpContextAccessor = httpContextAccessor;

    public Guid? UserId
    {
        get
        {
            var claim = _httpContextAccessor.HttpContext?.User
                .FindFirstValue(ClaimTypes.NameIdentifier);
            return Guid.TryParse(claim, out var id) ? id : null;
        }
    }

    public string? Email
        => _httpContextAccessor.HttpContext?.User.FindFirstValue(ClaimTypes.Email);

    public bool IsAuthenticated
        => _httpContextAccessor.HttpContext?.User.Identity?.IsAuthenticated == true;

    public bool IsInRole(string role)
        => _httpContextAccessor.HttpContext?.User.IsInRole(role) == true;
}
```

Register:
```csharp
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();
```

Handlers inject it:
```csharp
public class DeleteProductCommandHandler : IRequestHandler<DeleteProductCommand, Result>
{
    private readonly ICurrentUserService _currentUser;

    public async Task<Result> Handle(DeleteProductCommand command, CancellationToken ct)
    {
        // Rule 38: fail fast before any domain work
        if (!_currentUser.IsInRole("Admin"))
            return Result.Unauthorized();

        // ... rest of handler
    }
}
```

---

## Pagination — PaginatedResult<T>

Lists always need pagination. One shared model for all paginated responses:

```csharp
// In ECommerce.SharedKernel (used by all query handlers)
public record PaginatedResult<T>(
    IReadOnlyList<T> Items,
    int TotalCount,
    int Page,
    int PageSize
)
{
    public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
    public bool HasNextPage => Page < TotalPages;
    public bool HasPreviousPage => Page > 1;
}
```

Query handler usage:
```csharp
var items = await _db.Products
    .AsNoTracking()
    .Where(...)
    .Skip((query.Page - 1) * query.PageSize)
    .Take(query.PageSize)
    .Select(p => new ProductDto(...))
    .ToListAsync(ct);

var total = await _db.Products.Where(...).CountAsync(ct);

return Result<PaginatedResult<ProductDto>>.Ok(
    new PaginatedResult<ProductDto>(items, total, query.Page, query.PageSize)
);
```

---

## Soft Deletes — IsDeleted Flag

E-commerce systems almost never hard-delete records. You need order history to reference products even after they're discontinued. The pattern is a soft delete: mark as deleted, filter it out in queries.

**In Entity (SharedKernel):**
```csharp
public abstract class Entity
{
    // ... existing properties ...
    public bool IsDeleted { get; private set; }

    protected void SoftDelete() => IsDeleted = true;
}
```

**Aggregate root method:**
```csharp
// In Product aggregate
public void Discontinue()
{
    if (Status == ProductStatus.Discontinued)
        throw new CatalogDomainException("ALREADY_DISCONTINUED", "Product is already discontinued.");

    Status = ProductStatus.Discontinued;
    SoftDelete();
    AddDomainEvent(new ProductDiscontinuedEvent(Id));
}
```

**EF Core global query filter** (filter out deleted records automatically):
```csharp
// In DbContext.OnModelCreating — applies to ALL queries on Product
modelBuilder.Entity<Product>().HasQueryFilter(p => !p.IsDeleted);
```

With this filter, `_db.Products.Where(...)` automatically excludes soft-deleted products. You only see deleted records when you explicitly call `.IgnoreQueryFilters()`.

---

## What's in the API Layer — Complete Picture

```
ECommerce.API/
├── Controllers/           ← Thin HTTP entry points, dispatch via MediatR
├── Behaviors/             ← MediatR pipeline (Logging, Performance, Validation, Transaction)
├── Middleware/
│   └── GlobalExceptionHandler.cs  ← Catches DomainException, ValidationException, etc.
├── Extensions/
│   ├── ResultExtensions.cs        ← ToActionResult(), ToCreatedActionResult()
│   └── ServiceCollectionExtensions.cs  ← DI registration helpers
├── Models/
│   └── ApiResponse.cs             ← Consistent HTTP response envelope
├── Services/
│   └── CurrentUserService.cs      ← Reads logged-in user from HttpContext
└── Program.cs             ← Composition root: registers everything
```

---

## The Full Request Lifecycle (Putting It All Together)

```
1. Client sends: POST /api/products
   { "name": "Widget", "price": 19.99, "sku": "WGT-001", "categoryId": "..." }

2. ProductsController.Create receives it
   ↓ Constructs CreateProductCommand from body
   ↓ Calls _mediator.Send(command, ct)

3. MediatR pipeline:
   → LoggingBehavior: logs "Handling CreateProductCommand"
   → PerformanceBehavior: starts timer
   → ValidationBehavior: runs CreateProductCommandValidator
        if invalid → throws ValidationException
   → TransactionBehavior: begins DB transaction (ITransactionalCommand)
   → CreateProductCommandHandler.Handle()

4. Handler:
   → ICurrentUserService: checks caller is Admin
   → IProductRepository: checks SKU doesn't exist
   → Product.Create(...): creates aggregate, raises ProductCreatedEvent
   → _repository.AddAsync(product)
   → _unitOfWork.SaveChangesAsync()
       → stamps UpdatedAt
       → base.SaveChangesAsync() persists to DB
       → DomainEventDispatcher dispatches ProductCreatedEvent
           → UpdateSearchIndexOnProductCreatedHandler runs
   → returns Result<ProductDetailDto>.Ok(product.ToDetailDto())

5. TransactionBehavior: commits transaction
6. PerformanceBehavior: checks elapsed time, warns if > 500ms
7. LoggingBehavior: logs "Handled CreateProductCommand in 42ms"

8. Controller receives Result<ProductDetailDto>
   → result.ToCreatedActionResult("api/products/widget-wgt-001")
   → returns HTTP 201 Created with:
     { "success": true, "data": { "id": "...", "name": "Widget", ... } }
```

If any step throws `DomainException` or `ValidationException` → `GlobalExceptionHandler` catches it → returns proper 400/422 response. The controller never sees the exception.
