# DTO Best Practices & Guidelines

> **Scope:** This document is a universal guide for all DTOs in `ECommerce.Application/DTOs`. Follow these conventions for consistency, maintainability, and clean code across the entire project.
>
> **Based on:** Ultimate ASP.NET Core Web API best practices, Onion Architecture principles, and industry standards.

---

## Table of Contents

1. [Core Principles](#core-principles)
2. [Onion Architecture Alignment](#onion-architecture-alignment)
3. [Folder Structure](#folder-structure)
4. [Naming Conventions](#naming-conventions)
5. [DTO Design Rules](#dto-design-rules)
6. [Request Parameter DTOs](#request-parameter-dtos)
7. [Validation Strategies](#validation-strategies)
8. [AutoMapper Guidelines](#automapper-guidelines)
9. [Service & Controller Patterns](#service--controller-patterns)
10. [Action Filters Integration](#action-filters-integration)
11. [Error Handling DTOs](#error-handling-dtos)
12. [Testing Strategy](#testing-strategy)
13. [Performance & Serialization](#performance--serialization)
14. [Documentation Standards](#documentation-standards)
15. [Migration & Refactoring Checklist](#migration--refactoring-checklist)
16. [Quick Reference](#quick-reference)

---

## Core Principles

| Principle | Description |
|-----------|-------------|
| **Separation** | DTOs are data carriers only — no business logic, no EF navigation, no methods. |
| **Isolation** | Never expose EF entities to controllers or API consumers. Always map to DTOs. |
| **Single Responsibility** | Each DTO serves one purpose: read (response), write (request), or internal transfer. |
| **Explicit over Implicit** | Prefer explicit property declarations and mappings over conventions that hide behavior. |
| **Layer Independence** | DTOs belong to the Application/Service layer, not Domain or Infrastructure. |
| **Abstraction** | Depend on interfaces (contracts), not implementations — enables testing and flexibility. |

---

## Onion Architecture Alignment

Our project follows **Onion Architecture** with these layers:

```
┌─────────────────────────────────────────────────────────────┐
│                    Presentation Layer                        │
│              (ECommerce.API - Controllers)                   │
├─────────────────────────────────────────────────────────────┤
│                    Infrastructure Layer                      │
│     (ECommerce.Infrastructure - Repositories, DbContext)     │
├─────────────────────────────────────────────────────────────┤
│                      Service Layer                           │
│   (ECommerce.Application - Services, DTOs, Validators)       │
├─────────────────────────────────────────────────────────────┤
│                      Domain Layer                            │
│        (ECommerce.Core - Entities, Interfaces)               │
└─────────────────────────────────────────────────────────────┘
```

### Where DTOs Live

| Layer | Contains | Does NOT Contain |
|-------|----------|------------------|
| **ECommerce.Core** | Entities, Enums, Repository interfaces | DTOs, Mapping logic |
| **ECommerce.Application** | **DTOs**, Services, Validators, AutoMapper profiles | DbContext, EF logic |
| **ECommerce.Infrastructure** | Repositories, DbContext, Migrations | DTOs, Business logic |
| **ECommerce.API** | Controllers, Middleware, Filters | DTOs, Business logic |

### Flow of Dependencies

```
Controller → IService (interface) → Service → IRepository (interface) → Repository
     ↓              ↓                   ↓
   DTOs          DTOs             Entities (Domain)
```

**Key Rule:** Controllers and Services work with DTOs. Repositories work with Entities. AutoMapper bridges them.

---

## Folder Structure

```
src/backend/ECommerce.Application/
├── DTOs/
│   ├── Auth/
│   │   ├── AuthDtos.cs          # LoginDto, RegisterDto, AuthResponseDto, UserDto
│   │   └── AuthRequestDtos.cs   # RefreshTokenRequest, ForgotPasswordRequest, etc.
│   ├── Cart/
│   │   └── CartDtos.cs          # CartDto, CartItemDto, AddToCartDto, UpdateCartItemDto
│   ├── Orders/
│   │   └── OrderDtos.cs         # OrderDto, OrderDetailDto, CreateOrderDto, etc.
│   ├── Products/
│   │   ├── ProductDto.cs        # ProductDto, ProductDetailDto, ProductCategoryDto, ProductReviewDto
│   │   └── CreateProductDto.cs  # CreateProductDto, UpdateProductDto
│   ├── Inventory/
│   │   └── InventoryDtos.cs     # InventoryDto, LowStockAlertDto, StockCheckItemDto, etc.
│   ├── Reviews/
│   │   └── ReviewDtos.cs        # ReviewDto, ReviewDetailDto, CreateReviewDto, UpdateReviewDto
│   ├── Users/
│   │   └── UserProfileDtos.cs   # UserProfileDto, UpdateProfileDto
│   ├── Wishlist/
│   │   └── WishlistDtos.cs      # WishlistDto, WishlistItemDto, AddToWishlistDto
│   ├── Payments/
│   │   └── PaymentDtos.cs       # ProcessPaymentDto, PaymentResponseDto, RefundPaymentDto
│   ├── PromoCodes/
│   │   └── PromoCodeDtos.cs     # PromoCodeDto, CreatePromoCodeDto, ValidatePromoCodeDto
│   ├── Dashboard/
│   │   └── DashboardDtos.cs     # DashboardStatsDto, OrderTrendDto, RevenueTrendDto
│   ├── Common/
│   │   ├── ApiResponse.cs       # ApiResponse<T>, PaginatedResult<T>
│   │   └── ErrorDetails.cs      # ErrorDetails
│   └── CategoryDto.cs           # CategoryDto, CategoryDetailDto, CreateCategoryDto
├── Validators/
│   ├── Auth/
│   │   ├── RegisterDtoValidator.cs
│   │   └── LoginDtoValidator.cs
│   ├── Products/
│   │   └── CreateProductDtoValidator.cs
│   ├── Orders/
│   │   └── CreateOrderDtoValidator.cs
│   ├── Cart/
│   │   ├── AddToCartDtoValidator.cs
│   │   └── UpdateCartItemDtoValidator.cs
│   └── PromoCodes/
│       └── CreatePromoCodeDtoValidator.cs
└── MappingProfile.cs            # All AutoMapper configurations
```

### When to Group vs Split

| Scenario | Recommendation |
|----------|----------------|
| < 5 related DTOs, < 150 LOC | **Group** in one file (e.g., `CartDtos.cs`) |
| > 5 DTOs or > 200 LOC | **Split** into multiple files |
| DTO reused across features | **Separate file** for easier imports |
| DTO needs dedicated validator | Consider **separate file** for clarity |

---

## Naming Conventions

| Pattern | Usage | Examples |
|---------|-------|----------|
| `*Dto` | General read/response DTOs | `ProductDto`, `UserDto`, `OrderDto` |
| `*DetailDto` | Extended version with more fields | `ProductDetailDto`, `OrderDetailDto` |
| `Create*Dto` | Input for creating resources | `CreateProductDto`, `CreateOrderDto` |
| `Update*Dto` | Input for updating resources | `UpdateProductDto`, `UpdateProfileDto` |
| `*ForCreationDto` | Alternative create pattern (book style) | `CompanyForCreationDto` |
| `*ForUpdateDto` | Alternative update pattern (book style) | `EmployeeForUpdateDto` |
| `*Request` | Action-specific request payloads | `RefreshTokenRequest`, `AdjustStockRequest` |
| `*Response` | Wrapper/envelope responses | `ApiResponse<T>`, `StockCheckResponse` |
| `*ItemDto` | Child/nested item in a collection | `CartItemDto`, `OrderItemDto` |
| `*Parameters` | Query/request parameters for filtering | `ProductParameters`, `OrderParameters` |

### Avoid

- ❌ No suffix at all (`LowStockAlert` → use `LowStockAlertDto`)
- ❌ Mixed patterns in same feature (`CartRequest` + `AddToCartDto`)
- ❌ Entity names as DTOs (`Product` vs `ProductDto`)
- ❌ Exposing EF entities directly to API consumers

---

## DTO Design Rules

### DO

```csharp
/// <summary>
/// Response DTO for product information.
/// </summary>
public class ProductDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = null!;
    public decimal Price { get; set; }
    public string? Description { get; set; }  // Nullable for optional fields
    public List<ProductImageDto> Images { get; set; } = new();  // Initialize collections
}
```

### DON'T

```csharp
// ❌ Business logic in DTO
public class ProductDto
{
    public decimal Price { get; set; }
    public decimal Tax => Price * 0.08m;  // ❌ Move to service
}

// ❌ EF navigation properties
public class OrderDto
{
    public User User { get; set; }  // ❌ Use UserDto or flatten fields
}

// ❌ Methods in DTOs
public class CartDto
{
    public void AddItem(CartItemDto item) { }  // ❌ DTOs are data only
}
```

### Nullable Reference Types

```csharp
public class CreateProductDto
{
    public string Name { get; set; } = null!;       // Required (validated)
    public string? Description { get; set; }         // Optional
    public decimal? CompareAtPrice { get; set; }     // Optional value type
}
```

---

## Request Parameter DTOs

For **paging, filtering, searching, and sorting**, create dedicated parameter classes:

### Base Parameters Class

```csharp
namespace ECommerce.Application.DTOs.Common;

/// <summary>
/// Base class for all request parameters supporting paging.
/// </summary>
public abstract class RequestParameters
{
    private const int MaxPageSize = 50;
    private int _pageSize = 10;

    public int PageNumber { get; set; } = 1;

    public int PageSize
    {
        get => _pageSize;
        set => _pageSize = value > MaxPageSize ? MaxPageSize : value;
    }

    public string? OrderBy { get; set; }
    public string? Fields { get; set; }  // For data shaping
}
```

### Feature-Specific Parameters

```csharp
namespace ECommerce.Application.DTOs.Products;

/// <summary>
/// Request parameters for product queries with filtering and searching.
/// </summary>
public class ProductParameters : RequestParameters
{
    // Filtering
    public decimal? MinPrice { get; set; }
    public decimal? MaxPrice { get; set; }
    public Guid? CategoryId { get; set; }
    public bool? IsActive { get; set; }

    // Searching
    public string? SearchTerm { get; set; }

    // Validation
    public bool ValidPriceRange => MaxPrice >= MinPrice || MaxPrice == null || MinPrice == null;
}
```

```csharp
namespace ECommerce.Application.DTOs.Orders;

public class OrderParameters : RequestParameters
{
    public string? Status { get; set; }
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
    public Guid? CustomerId { get; set; }
}
```

### Paginated Response Wrapper

```csharp
namespace ECommerce.Application.DTOs.Common;

public class PagedList<T> : List<T>
{
    public int CurrentPage { get; private set; }
    public int TotalPages { get; private set; }
    public int PageSize { get; private set; }
    public int TotalCount { get; private set; }
    
    public bool HasPrevious => CurrentPage > 1;
    public bool HasNext => CurrentPage < TotalPages;

    public PagedList(List<T> items, int count, int pageNumber, int pageSize)
    {
        TotalCount = count;
        PageSize = pageSize;
        CurrentPage = pageNumber;
        TotalPages = (int)Math.Ceiling(count / (double)pageSize);
        AddRange(items);
    }

    public static async Task<PagedList<T>> ToPagedListAsync(
        IQueryable<T> source, int pageNumber, int pageSize)
    {
        var count = await source.CountAsync();
        var items = await source
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
        return new PagedList<T>(items, count, pageNumber, pageSize);
    }
}
```

### Pagination Metadata in Response Headers

```csharp
// In controller
var products = await _service.GetAllAsync(parameters);

var metadata = new
{
    products.TotalCount,
    products.PageSize,
    products.CurrentPage,
    products.TotalPages,
    products.HasNext,
    products.HasPrevious
};

Response.Headers.Add("X-Pagination", JsonSerializer.Serialize(metadata));
return Ok(products);
```

---

## Validation Strategies

### 1. FluentValidation (Preferred)

#### Validator Structure

```csharp
using FluentValidation;

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
            .Matches("[A-Z]").WithMessage("Password must contain uppercase letter")
            .Matches("[a-z]").WithMessage("Password must contain lowercase letter")
            .Matches("[0-9]").WithMessage("Password must contain a number");

        RuleFor(x => x.FirstName)
            .NotEmpty().WithMessage("First name is required")
            .MaximumLength(50);

        RuleFor(x => x.LastName)
            .NotEmpty().WithMessage("Last name is required")
            .MaximumLength(50);
    }
}
```

#### Registration in Program.cs

```csharp
using FluentValidation;
using FluentValidation.AspNetCore;

// Add after builder.Services.AddControllers()
builder.Services.AddFluentValidationAutoValidation();
builder.Services.AddValidatorsFromAssemblyContaining<RegisterDtoValidator>();
```

### 2. Data Annotations (Simple Cases)

```csharp
using System.ComponentModel.DataAnnotations;

public class CreateProductDto
{
    [Required(ErrorMessage = "Name is required")]
    [MaxLength(100, ErrorMessage = "Name can't exceed 100 characters")]
    public string Name { get; set; } = null!;

    [Range(0.01, double.MaxValue, ErrorMessage = "Price must be greater than 0")]
    public decimal Price { get; set; }
}
```

### 3. IValidatableObject (Cross-Property Validation)

```csharp
public class DateRangeDto : IValidatableObject
{
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (EndDate < StartDate)
        {
            yield return new ValidationResult(
                "End date must be after start date",
                new[] { nameof(EndDate) });
        }
    }
}
```

### 4. Custom Validation Attributes

```csharp
public class ValidGuidAttribute : ValidationAttribute
{
    protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
    {
        if (value is Guid guid && guid == Guid.Empty)
        {
            return new ValidationResult("Invalid GUID provided");
        }
        return ValidationResult.Success;
    }
}

// Usage
public class AddToCartDto
{
    [ValidGuid]
    public Guid ProductId { get; set; }
}
```

### Common Validation Rules Reference

| Field Type | Validation |
|------------|------------|
| Email | `NotEmpty`, `EmailAddress` |
| Password | `NotEmpty`, `MinimumLength(8)`, regex for complexity |
| Name/String | `NotEmpty`, `MaximumLength(n)` |
| Price/Amount | `GreaterThan(0)`, `LessThanOrEqualTo(max)` |
| Quantity | `GreaterThanOrEqualTo(1)` |
| GUID | `NotEmpty`, custom `ValidGuid` attribute |
| Enum string | `Must(BeValidEnumValue)` or `IsInEnum()` |
| Nested object | `SetValidator(new NestedValidator())` |
| Collection | `NotEmpty`, `RuleForEach(x => x.Items).SetValidator(...)` |

### Validation in PATCH Requests

For PATCH operations, use nullable properties and validate only non-null values:

```csharp
public class UpdateProductPatchDto
{
    public string? Name { get; set; }
    public decimal? Price { get; set; }
    public string? Description { get; set; }
}

public class UpdateProductPatchDtoValidator : AbstractValidator<UpdateProductPatchDto>
{
    public UpdateProductPatchDtoValidator()
    {
        When(x => x.Name != null, () =>
        {
            RuleFor(x => x.Name).MaximumLength(100);
        });

        When(x => x.Price != null, () =>
        {
            RuleFor(x => x.Price).GreaterThan(0);
        });
    }
}
```

---

## AutoMapper Guidelines

### MappingProfile Location

All mappings in: `src/backend/ECommerce.Application/MappingProfile.cs`

### Mapping Patterns

```csharp
public class MappingProfile : Profile
{
    public MappingProfile()
    {
        // Simple mapping (property names match)
        CreateMap<Product, ProductDto>();

        // With member configuration
        CreateMap<Product, ProductDetailDto>()
            .ForMember(dest => dest.AverageRating, 
                opt => opt.MapFrom(src => src.Reviews.Any() 
                    ? src.Reviews.Average(r => r.Rating) 
                    : 0));

        // Ignore unmapped members
        CreateMap<UpdateProductDto, Product>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
            .ForAllMembers(opts => 
                opts.Condition((src, dest, srcMember) => srcMember != null));

        // Reverse mapping
        CreateMap<Address, AddressDto>().ReverseMap();

        // Renamed properties
        CreateMap<Product, InventoryDto>()
            .ForMember(dest => dest.ProductId, opt => opt.MapFrom(src => src.Id))
            .ForMember(dest => dest.ProductName, opt => opt.MapFrom(src => src.Name));
    }
}
```

### When NOT to Use AutoMapper

| Scenario | Recommendation |
|----------|----------------|
| Response with conditional logic | Build manually in service |
| Aggregated data (dashboards) | Build manually from query results |
| Complex async data fetching | Use custom mapping method |
| Performance-critical projections | Use LINQ `.Select()` directly |

---

## Service & Controller Patterns

### Service Layer

Services should work with DTOs, not entities. The repository handles entity persistence.

```csharp
public interface IProductService
{
    Task<ProductDto> GetByIdAsync(Guid id, bool trackChanges);
    Task<PagedList<ProductDto>> GetAllAsync(ProductParameters parameters, bool trackChanges);
    Task<ProductDto> CreateAsync(CreateProductDto dto);
    Task UpdateAsync(Guid id, UpdateProductDto dto, bool trackChanges);
    Task DeleteAsync(Guid id, bool trackChanges);
}
```

### Service Implementation Pattern

```csharp
public class ProductService : IProductService
{
    private readonly IRepositoryManager _repository;
    private readonly IMapper _mapper;
    private readonly ILoggerManager _logger;

    public ProductService(IRepositoryManager repository, IMapper mapper, ILoggerManager logger)
    {
        _repository = repository;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<ProductDto> GetByIdAsync(Guid id, bool trackChanges)
    {
        var product = await GetProductAndCheckIfItExists(id, trackChanges);
        return _mapper.Map<ProductDto>(product);
    }

    public async Task<ProductDto> CreateAsync(CreateProductDto dto)
    {
        var entity = _mapper.Map<Product>(dto);
        _repository.Product.CreateProduct(entity);
        await _repository.SaveAsync();
        return _mapper.Map<ProductDto>(entity);
    }

    private async Task<Product> GetProductAndCheckIfItExists(Guid id, bool trackChanges)
    {
        var product = await _repository.Product.GetByIdAsync(id, trackChanges);
        if (product is null)
            throw new ProductNotFoundException(id);
        return product;
    }
}
```

### Controller Layer

Controllers should be thin — delegate logic to services.

```csharp
[ApiController]
[Route("api/[controller]")]
public class ProductsController : ControllerBase
{
    private readonly IServiceManager _service;

    public ProductsController(IServiceManager service) => _service = service;

    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<ProductDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll([FromQuery] ProductParameters parameters)
    {
        var pagedResult = await _service.Product.GetAllAsync(parameters, trackChanges: false);
        
        Response.Headers.Add("X-Pagination", JsonSerializer.Serialize(pagedResult.MetaData));
        
        return Ok(pagedResult);
    }

    [HttpGet("{id:guid}", Name = "GetProductById")]
    [ProducesResponseType(typeof(ProductDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id)
    {
        var product = await _service.Product.GetByIdAsync(id, trackChanges: false);
        return Ok(product);
    }

    [HttpPost]
    [ProducesResponseType(typeof(ProductDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ErrorDetails), StatusCodes.Status400BadRequest)]
    [ServiceFilter(typeof(ValidationFilterAttribute))]
    public async Task<IActionResult> Create([FromBody] CreateProductDto dto)
    {
        var product = await _service.Product.CreateAsync(dto);
        return CreatedAtRoute("GetProductById", new { id = product.Id }, product);
    }

    [HttpPut("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ErrorDetails), StatusCodes.Status404NotFound)]
    [ServiceFilter(typeof(ValidationFilterAttribute))]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateProductDto dto)
    {
        await _service.Product.UpdateAsync(id, dto, trackChanges: true);
        return NoContent();
    }

    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ErrorDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id)
    {
        await _service.Product.DeleteAsync(id, trackChanges: false);
        return NoContent();
    }
}
```

---

## Action Filters Integration

Use Action Filters to keep controllers clean and handle cross-cutting concerns.

### Validation Filter

```csharp
namespace ECommerce.API.ActionFilters;

public class ValidationFilterAttribute : IActionFilter
{
    public void OnActionExecuting(ActionExecutingContext context)
    {
        var action = context.RouteData.Values["action"];
        var controller = context.RouteData.Values["controller"];

        // Check for null DTO
        var param = context.ActionArguments
            .SingleOrDefault(x => x.Value?.ToString()?.Contains("Dto") ?? false).Value;

        if (param is null)
        {
            context.Result = new BadRequestObjectResult(
                $"Object is null. Controller: {controller}, action: {action}");
            return;
        }

        // Check ModelState
        if (!context.ModelState.IsValid)
        {
            context.Result = new UnprocessableEntityObjectResult(context.ModelState);
        }
    }

    public void OnActionExecuted(ActionExecutedContext context) { }
}
```

### Registration

```csharp
// In Program.cs
builder.Services.AddScoped<ValidationFilterAttribute>();

// In Controller
[HttpPost]
[ServiceFilter(typeof(ValidationFilterAttribute))]
public async Task<IActionResult> Create([FromBody] CreateProductDto dto)
```

### Entity Exists Filter (for parent-child relationships)

```csharp
public class ValidateCategoryExistsAttribute : IAsyncActionFilter
{
    private readonly IRepositoryManager _repository;

    public ValidateCategoryExistsAttribute(IRepositoryManager repository)
        => _repository = repository;

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var trackChanges = context.HttpContext.Request.Method.Equals("PUT");
        var id = (Guid)context.ActionArguments["categoryId"]!;
        var category = await _repository.Category.GetByIdAsync(id, trackChanges);

        if (category is null)
        {
            context.Result = new NotFoundResult();
            return;
        }

        context.HttpContext.Items.Add("category", category);
        await next();
    }
}
```

---

## Error Handling DTOs

### Standard Error Response

```csharp
namespace ECommerce.Application.DTOs.Common;

public class ErrorDetails
{
    public int StatusCode { get; set; }
    public string? Message { get; set; }
    
    public override string ToString() => JsonSerializer.Serialize(this);
}
```

### Detailed Validation Errors

```csharp
public class ValidationErrorDetails : ErrorDetails
{
    public IDictionary<string, string[]>? Errors { get; set; }
}
```

### Global Exception Handler Middleware

```csharp
public class ExceptionMiddlewareExtensions
{
    public static void ConfigureExceptionHandler(this WebApplication app, ILoggerManager logger)
    {
        app.UseExceptionHandler(appError =>
        {
            appError.Run(async context =>
            {
                context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
                context.Response.ContentType = "application/json";

                var contextFeature = context.Features.Get<IExceptionHandlerFeature>();
                if (contextFeature != null)
                {
                    context.Response.StatusCode = contextFeature.Error switch
                    {
                        NotFoundException => StatusCodes.Status404NotFound,
                        BadRequestException => StatusCodes.Status400BadRequest,
                        _ => StatusCodes.Status500InternalServerError
                    };

                    logger.LogError($"Something went wrong: {contextFeature.Error}");

                    await context.Response.WriteAsync(new ErrorDetails
                    {
                        StatusCode = context.Response.StatusCode,
                        Message = contextFeature.Error.Message
                    }.ToString());
                }
            });
        });
    }
}
```

---

## Testing Strategy

### 1. Validator Tests

```csharp
[TestClass]
public class RegisterDtoValidatorTests
{
    private RegisterDtoValidator _validator = new();

    [TestMethod]
    public void Validate_EmptyEmail_ReturnsError()
    {
        var dto = new RegisterDto { Email = "", Password = "Valid123!", FirstName = "John", LastName = "Doe" };
        var result = _validator.Validate(dto);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Email");
    }

    [TestMethod]
    public void Validate_ValidDto_ReturnsSuccess()
    {
        var dto = new RegisterDto { Email = "test@example.com", Password = "Valid123!", FirstName = "John", LastName = "Doe" };
        var result = _validator.Validate(dto);
        result.IsValid.Should().BeTrue();
    }
}
```

### 2. AutoMapper Configuration Test

```csharp
[TestClass]
public class MappingProfileTests
{
    [TestMethod]
    public void MappingProfile_ConfigurationIsValid()
    {
        var config = new MapperConfiguration(cfg => cfg.AddProfile<MappingProfile>());
        config.AssertConfigurationIsValid();
    }
}
```

### 3. Service Tests (Mock IMapper and IRepository)

```csharp
[TestClass]
public class ProductServiceTests
{
    private Mock<IRepositoryManager> _mockRepository = null!;
    private Mock<IMapper> _mockMapper = null!;
    private Mock<ILoggerManager> _mockLogger = null!;
    private ProductService _service = null!;

    [TestInitialize]
    public void Setup()
    {
        _mockRepository = new Mock<IRepositoryManager>();
        _mockMapper = new Mock<IMapper>();
        _mockLogger = new Mock<ILoggerManager>();
        
        _service = new ProductService(
            _mockRepository.Object, 
            _mockMapper.Object, 
            _mockLogger.Object);
    }

    [TestMethod]
    public async Task GetById_ProductExists_ReturnsProductDto()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var product = new Product { Id = productId, Name = "Test" };
        var productDto = new ProductDto { Id = productId, Name = "Test" };

        _mockRepository.Setup(r => r.Product.GetByIdAsync(productId, false))
            .ReturnsAsync(product);
        _mockMapper.Setup(m => m.Map<ProductDto>(product))
            .Returns(productDto);

        // Act
        var result = await _service.GetByIdAsync(productId, false);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().Be(productId);
    }

    [TestMethod]
    public async Task GetById_ProductNotFound_ThrowsNotFoundException()
    {
        // Arrange
        var productId = Guid.NewGuid();
        _mockRepository.Setup(r => r.Product.GetByIdAsync(productId, false))
            .ReturnsAsync((Product?)null);

        // Act & Assert
        await Assert.ThrowsExceptionAsync<ProductNotFoundException>(
            () => _service.GetByIdAsync(productId, false));
    }
}
```

### 4. Integration Tests

```csharp
[TestClass]
public class ProductsControllerIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public ProductsControllerIntegrationTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient();
    }

    [TestMethod]
    public async Task GetAll_ReturnsSuccessStatusCode()
    {
        var response = await _client.GetAsync("/api/products");
        response.EnsureSuccessStatusCode();
        response.Headers.Should().ContainKey("X-Pagination");
    }
}
```

---

## Performance & Serialization

### Projection at Query Level

```csharp
// ✅ Efficient: project in query (no tracking, minimal data)
public async Task<PagedList<ProductDto>> GetAllAsync(ProductParameters parameters)
{
    var products = await _context.Products
        .FilterByPrice(parameters.MinPrice, parameters.MaxPrice)
        .Search(parameters.SearchTerm)
        .Sort(parameters.OrderBy)
        .Select(p => new ProductDto
        {
            Id = p.Id,
            Name = p.Name,
            Price = p.Price
        })
        .ToPagedListAsync(parameters.PageNumber, parameters.PageSize);

    return products;
}

// ❌ Inefficient: load all entities then map
var entities = await _context.Products.ToListAsync();
var dtos = _mapper.Map<List<ProductDto>>(entities);
```

### Avoid Over-fetching with Summary DTOs

```csharp
// Use summary DTOs for lists
public class ProductSummaryDto  // Lightweight
{
    public Guid Id { get; set; }
    public string Name { get; set; } = null!;
    public decimal Price { get; set; }
    public string? ThumbnailUrl { get; set; }
}

public class ProductDetailDto  // Full details for single item
{
    public Guid Id { get; set; }
    public string Name { get; set; } = null!;
    public decimal Price { get; set; }
    public string? Description { get; set; }
    public List<ProductImageDto> Images { get; set; } = new();
    public List<ProductReviewDto> Reviews { get; set; } = new();
    public ProductCategoryDto? Category { get; set; }
}
```

### Data Shaping

Allow clients to request only specific fields:

```csharp
// Request: GET /api/products?fields=id,name,price
public class DataShaper<T> : IDataShaper<T> where T : class
{
    public PropertyInfo[] Properties { get; set; }

    public DataShaper()
    {
        Properties = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance);
    }

    public IEnumerable<ShapedEntity> ShapeData(IEnumerable<T> entities, string? fieldsString)
    {
        var requiredProperties = GetRequiredProperties(fieldsString);
        return FetchData(entities, requiredProperties);
    }

    private IEnumerable<PropertyInfo> GetRequiredProperties(string? fieldsString)
    {
        if (string.IsNullOrWhiteSpace(fieldsString))
            return Properties;

        var fields = fieldsString.Split(',', StringSplitOptions.RemoveEmptyEntries);
        return Properties.Where(p => fields.Contains(p.Name, StringComparer.InvariantCultureIgnoreCase));
    }
}
```

### Caching Considerations

```csharp
// Add response caching for read-heavy endpoints
[HttpGet]
[ResponseCache(Duration = 60, VaryByQueryKeys = new[] { "pageNumber", "pageSize" })]
public async Task<IActionResult> GetAll([FromQuery] ProductParameters parameters)
```

### Async All the Way

```csharp
// ✅ Correct: async throughout
public async Task<ProductDto> GetByIdAsync(Guid id)
{
    var product = await _repository.Product.GetByIdAsync(id, trackChanges: false);
    return _mapper.Map<ProductDto>(product);
}

// ❌ Avoid: blocking on async (can cause deadlocks)
public ProductDto GetById(Guid id)
{
    var product = _repository.Product.GetByIdAsync(id, false).Result; // Dangerous!
    return _mapper.Map<ProductDto>(product);
}
```

---

## Documentation Standards

### XML Comments on DTOs

```csharp
/// <summary>
/// Request DTO for user registration.
/// </summary>
public class RegisterDto
{
    /// <summary>
    /// User's email address (must be unique).
    /// </summary>
    /// <example>user@example.com</example>
    public string Email { get; set; } = null!;

    /// <summary>
    /// Password (min 8 chars, requires uppercase, lowercase, and number).
    /// </summary>
    public string Password { get; set; } = null!;

    /// <summary>
    /// User's first name.
    /// </summary>
    /// <example>John</example>
    public string FirstName { get; set; } = null!;

    /// <summary>
    /// User's last name.
    /// </summary>
    /// <example>Doe</example>
    public string LastName { get; set; } = null!;
}
```

---

## Migration & Refactoring Checklist

When creating, moving, renaming, or modifying DTOs:

### DTO Changes
- [ ] Update/create DTO class with proper naming and nullable annotations
- [ ] Follow `*Dto`, `Create*Dto`, `Update*Dto`, `*Parameters` conventions
- [ ] Add XML documentation comments
- [ ] Initialize collections to empty (not null)

### Validation
- [ ] Add/update FluentValidation validator for input DTOs
- [ ] Or add Data Annotations for simple cases
- [ ] Register validators in DI container

### Mapping
- [ ] Add/update AutoMapper mapping in `MappingProfile.cs`
- [ ] Test mapping with `AssertConfigurationIsValid()`

### Service Layer
- [ ] Update service interfaces with new DTOs
- [ ] Update service implementations
- [ ] Ensure async/await throughout

### Presentation Layer
- [ ] Update controller action signatures and return types
- [ ] Update `[ProducesResponseType]` attributes
- [ ] Add action filters if needed

### Imports
- [ ] Update `using` statements in all affected files
- [ ] Ensure no circular dependencies between layers

### Testing
- [ ] Add/update unit tests for validators
- [ ] Add/update service tests with mocked IMapper
- [ ] Run integration tests if available

### Verification
- [ ] Run `dotnet build` — fix compile errors
- [ ] Run `dotnet test` — fix test failures
- [ ] Test API endpoints manually or via Postman/HTTP files
- [ ] Update API documentation (Swagger annotations) if applicable

---

## Quick Reference

### File Naming

| Content | File Name |
|---------|-----------|
| Feature DTOs grouped | `{Feature}Dtos.cs` (e.g., `CartDtos.cs`) |
| Single complex DTO | `{DtoName}.cs` (e.g., `ProductDto.cs`) |
| Validators | `{DtoName}Validator.cs` |
| Request parameters | `{Feature}Parameters.cs` |
| Commands (CQRS) | `{Action}{Entity}Command.cs` |
| Queries (CQRS) | `Get{Entity}Query.cs` |

### Import Statements

```csharp
// DTOs
using ECommerce.Application.DTOs.Products;
using ECommerce.Application.DTOs.Orders;
using ECommerce.Application.DTOs.Common;

// Validators
using ECommerce.Application.Validators.Auth;

// AutoMapper
using AutoMapper;

// MediatR (if using CQRS)
using MediatR;
```

### Extension Methods for Clean Code

```csharp
// ServiceExtensions.cs
public static class ServiceExtensions
{
    public static void ConfigureRepositoryManager(this IServiceCollection services)
        => services.AddScoped<IRepositoryManager, RepositoryManager>();

    public static void ConfigureServiceManager(this IServiceCollection services)
        => services.AddScoped<IServiceManager, ServiceManager>();

    public static void ConfigureAutoMapper(this IServiceCollection services)
        => services.AddAutoMapper(typeof(MappingProfile));

    public static void ConfigureFluentValidation(this IServiceCollection services)
    {
        services.AddFluentValidationAutoValidation();
        services.AddValidatorsFromAssemblyContaining<RegisterDtoValidator>();
    }
}

// In Program.cs
builder.Services.ConfigureRepositoryManager();
builder.Services.ConfigureServiceManager();
builder.Services.ConfigureAutoMapper();
builder.Services.ConfigureFluentValidation();
```

### Common Mistakes to Avoid

| Mistake | Fix |
|---------|-----|
| Returning EF entities from API | Always map to DTOs |
| Business logic in DTOs | Move to service layer |
| Missing validation on inputs | Add FluentValidation validators |
| Inconsistent naming | Follow `*Dto`, `Create*Dto`, `*Request` patterns |
| Large nested objects in lists | Use summary DTOs for collections |
| Manual mapping everywhere | Use AutoMapper, except justified cases |
| Blocking on async code | Use `async`/`await` throughout |
| No pagination on list endpoints | Use `PagedList<T>` and `RequestParameters` |
| Exposing internal errors | Use global exception handler with `ErrorDetails` |
| Tight coupling to implementations | Depend on interfaces (IoC/DI) |

### Response Codes Reference

| Action | Success Code | Error Codes |
|--------|--------------|-------------|
| GET (single) | 200 OK | 404 Not Found |
| GET (collection) | 200 OK | - |
| POST | 201 Created | 400 Bad Request, 422 Unprocessable |
| PUT | 204 No Content | 400 Bad Request, 404 Not Found |
| PATCH | 204 No Content | 400 Bad Request, 404 Not Found |
| DELETE | 204 No Content | 404 Not Found |

---

*Last updated: January 2026*  
*Based on: Ultimate ASP.NET Core Web API, Onion Architecture principles*
