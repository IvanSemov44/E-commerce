# DTO Organization & AutoMapper Consistency - Refactoring Plan

## Executive Summary

This document outlines a comprehensive refactoring plan to address DTO organization issues and AutoMapper inconsistencies in the E-Commerce application backend.

**Problems Identified:**
1. **Scattered DTOs** - 9 DTOs defined in controllers instead of the DTOs folder
2. **Duplicate DTOs** - CategoryDto and ReviewDto defined in multiple locations
3. **Inconsistent AutoMapper usage** - 4 services use manual mapping instead of AutoMapper
4. **Naming inconsistencies** - Mixed use of `*Dto`, `*Request`, and no suffix
5. **No validation** - FluentValidation installed but not implemented

**Estimated Effort:** 3-4 hours

---

## Table of Contents

1. [Current State Analysis](#current-state-analysis)
2. [Phase 1: Move DTOs from Controllers](#phase-1-move-dtos-from-controllers)
3. [Phase 2: Resolve Duplicate DTOs](#phase-2-resolve-duplicate-dtos)
4. [Phase 3: AutoMapper Consistency](#phase-3-automapper-consistency)
5. [Phase 4: Naming Conventions](#phase-4-naming-conventions)
6. [Phase 5: FluentValidation](#phase-5-fluentvalidation)
7. [Files Summary](#files-summary)
8. [Verification Plan](#verification-plan)
9. [Risk Assessment](#risk-assessment)

---

## Current State Analysis

### 1. DTOs in Controllers (9 DTOs to move)

**AuthController.cs** (lines 140-182):
```csharp
#region DTOs
public class RefreshTokenRequest { ... }
public class TokenResponseDto { ... }
public class ForgotPasswordRequest { ... }
public class ForgotPasswordResponseDto { ... }
public class ResetPasswordRequest { ... }
public class ChangePasswordRequest { ... }
public class VerifyEmailRequest { ... }
#endregion
```

**OrdersController.cs** (lines 119-122):
```csharp
public class UpdateOrderStatusDto { ... }
```

### 2. Duplicate DTOs

| DTO | Location 1 (Proper) | Location 2 (Duplicate) |
|-----|---------------------|------------------------|
| `CategoryDto` | `DTOs/CategoryDto.cs` (full version with ProductCount, IsActive) | `DTOs/Products/ProductDto.cs:36-42` (simplified for embedding) |
| `ReviewDto` | `DTOs/Reviews/ReviewDtos.cs` (ReviewDetailDto) | `DTOs/Products/ProductDto.cs:44-52` (simplified for embedding) |

### 3. Services Without AutoMapper

| Service | Issue | Lines | Recommendation |
|---------|-------|-------|----------------|
| `AuthService.cs` | Manual `MapToUserDto()` method | 259-271 | Refactor to use IMapper |
| `InventoryService.cs` | Inline LINQ mapping to InventoryDto, LowStockAlert | 212-221, 231-238 | Refactor to use IMapper |
| `PaymentService.cs` | Inline DTO creation with business logic | 71-81, 86-101 | **Keep as-is** (justified) |
| `DashboardService.cs` | Inline mapping from aggregated data | 36-54 | **Keep as-is** (justified) |

### 4. Naming Inconsistencies

| Pattern | Examples |
|---------|----------|
| `*Request` suffix | `AdjustStockRequest`, `StockCheckRequest`, `RefreshTokenRequest` |
| `*Dto` suffix | `CreateProductDto`, `UpdateOrderDto`, `PaymentDetailsDto` |
| No suffix | `LowStockAlert`, `StockIssue`, `StockCheckItem` |

### 5. Validation Status

- **FluentValidation 12.1.1** is installed in `ECommerce.Application.csproj`
- **No validators exist** - package is completely unused
- **Only `UserProfileDtos.cs`** uses Data Annotations (`[Required]`, `[StringLength]`)
- **All other DTOs** have no validation

---

## Phase 1: Move DTOs from Controllers

### Step 1.1: Create Auth Request DTOs File

**Create:** `src/backend/ECommerce.Application/DTOs/Auth/AuthRequestDtos.cs`

```csharp
namespace ECommerce.Application.DTOs.Auth;

/// <summary>
/// Request DTO for refreshing authentication tokens.
/// </summary>
public class RefreshTokenRequest
{
    public string Token { get; set; } = null!;
}

/// <summary>
/// Response DTO containing a new authentication token.
/// </summary>
public class TokenResponseDto
{
    public string Token { get; set; } = null!;
}

/// <summary>
/// Request DTO for initiating password reset.
/// </summary>
public class ForgotPasswordRequest
{
    public string Email { get; set; } = null!;
}

/// <summary>
/// Response DTO for password reset initiation.
/// </summary>
public class ForgotPasswordResponseDto
{
    public string? Token { get; set; }
    public string? Message { get; set; }
}

/// <summary>
/// Request DTO for resetting password with token.
/// </summary>
public class ResetPasswordRequest
{
    public string Email { get; set; } = null!;
    public string Token { get; set; } = null!;
    public string NewPassword { get; set; } = null!;
}

/// <summary>
/// Request DTO for changing password (authenticated users).
/// </summary>
public class ChangePasswordRequest
{
    public string OldPassword { get; set; } = null!;
    public string NewPassword { get; set; } = null!;
}

/// <summary>
/// Request DTO for email verification.
/// </summary>
public class VerifyEmailRequest
{
    public Guid UserId { get; set; }
    public string Token { get; set; } = null!;
}
```

### Step 1.2: Modify AuthController

**File:** `src/backend/ECommerce.API/Controllers/AuthController.cs`

**Changes:**
1. Remove entire `#region DTOs` section (lines 140-182)
2. Ensure using statement exists: `using ECommerce.Application.DTOs.Auth;`

### Step 1.3: Move UpdateOrderStatusDto

**Modify:** `src/backend/ECommerce.Application/DTOs/Orders/OrderDtos.cs`

Add at end of file:
```csharp
/// <summary>
/// Request DTO for updating order status.
/// </summary>
public class UpdateOrderStatusDto
{
    public string Status { get; set; } = null!;
}
```

### Step 1.4: Modify OrdersController

**File:** `src/backend/ECommerce.API/Controllers/OrdersController.cs`

**Changes:**
1. Remove `UpdateOrderStatusDto` class (lines 119-122)
2. Using statement already exists for Orders DTOs

---

## Phase 2: Resolve Duplicate DTOs

### Step 2.1: Rename Embedded CategoryDto

The duplicate `CategoryDto` in `ProductDto.cs` serves a different purpose - it's a simplified version for embedding in product responses. Rename it to clarify this distinction.

**Modify:** `src/backend/ECommerce.Application/DTOs/Products/ProductDto.cs`

```csharp
// BEFORE (lines 36-42):
public class CategoryDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = null!;
    public string Slug { get; set; } = null!;
    public string? ImageUrl { get; set; }
}

// AFTER:
/// <summary>
/// Simplified category DTO for embedding in product responses.
/// For full category details, use DTOs.CategoryDto.
/// </summary>
public class ProductCategoryDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = null!;
    public string Slug { get; set; } = null!;
    public string? ImageUrl { get; set; }
}
```

**Update ProductDto (line 14):**
```csharp
// BEFORE:
public CategoryDto? Category { get; set; }

// AFTER:
public ProductCategoryDto? Category { get; set; }
```

### Step 2.2: Rename Embedded ReviewDto

Similarly, rename the embedded `ReviewDto` to distinguish from the full review DTOs.

**Modify:** `src/backend/ECommerce.Application/DTOs/Products/ProductDto.cs`

```csharp
// BEFORE (lines 44-52):
public class ReviewDto
{
    public Guid Id { get; set; }
    public string? Title { get; set; }
    public string? Comment { get; set; }
    public int Rating { get; set; }
    public string? UserName { get; set; }
    public DateTime CreatedAt { get; set; }
}

// AFTER:
/// <summary>
/// Simplified review DTO for embedding in product detail responses.
/// For full review operations, use DTOs.Reviews.ReviewDetailDto.
/// </summary>
public class ProductReviewDto
{
    public Guid Id { get; set; }
    public string? Title { get; set; }
    public string? Comment { get; set; }
    public int Rating { get; set; }
    public string? UserName { get; set; }
    public DateTime CreatedAt { get; set; }
}
```

**Update ProductDetailDto (line 25):**
```csharp
// BEFORE:
public List<ReviewDto> Reviews { get; set; } = new();

// AFTER:
public List<ProductReviewDto> Reviews { get; set; } = new();
```

### Step 2.3: Update MappingProfile

**Modify:** `src/backend/ECommerce.Application/MappingProfile.cs`

Find and update the Category and Review mappings:

```csharp
// BEFORE (around line 23):
CreateMap<Category, DTOs.Products.CategoryDto>()
    .ForMember(dest => dest.ImageUrl, opt => opt.MapFrom(src => src.ImageUrl));

// AFTER:
CreateMap<Category, ProductCategoryDto>()
    .ForMember(dest => dest.ImageUrl, opt => opt.MapFrom(src => src.ImageUrl));

// BEFORE (around line 32):
CreateMap<Review, ReviewDto>()
    .ForMember(dest => dest.UserName,
        opt => opt.MapFrom(src => $"{src.User.FirstName} {src.User.LastName}"));

// AFTER:
CreateMap<Review, ProductReviewDto>()
    .ForMember(dest => dest.UserName,
        opt => opt.MapFrom(src => $"{src.User.FirstName} {src.User.LastName}"));
```

**Add using statement:**
```csharp
using ECommerce.Application.DTOs.Products;
```

---

## Phase 3: AutoMapper Consistency

### Step 3.1: Refactor AuthService

**Modify:** `src/backend/ECommerce.Application/Services/AuthService.cs`

**Change 1: Add IMapper to constructor**

```csharp
// BEFORE:
private readonly IUserRepository _userRepository;
private readonly IConfiguration _configuration;
private readonly IEmailService _emailService;

public AuthService(
    IUserRepository userRepository,
    IConfiguration configuration,
    IEmailService emailService)
{
    _userRepository = userRepository;
    _configuration = configuration;
    _emailService = emailService;
}

// AFTER:
private readonly IUserRepository _userRepository;
private readonly IConfiguration _configuration;
private readonly IEmailService _emailService;
private readonly IMapper _mapper;

public AuthService(
    IUserRepository userRepository,
    IConfiguration configuration,
    IEmailService emailService,
    IMapper mapper)
{
    _userRepository = userRepository;
    _configuration = configuration;
    _emailService = emailService;
    _mapper = mapper;
}
```

**Add using statement:**
```csharp
using AutoMapper;
```

**Change 2: Replace MapToUserDto calls**

Find all occurrences of `MapToUserDto(user)` and replace with `_mapper.Map<UserDto>(user)`:

```csharp
// Line ~64 in RegisterAsync:
User = _mapper.Map<UserDto>(user),

// Line ~84 in LoginAsync:
User = _mapper.Map<UserDto>(user),
```

**Change 3: Delete MapToUserDto method**

Remove the entire private method (lines 259-271):
```csharp
// DELETE THIS:
private UserDto MapToUserDto(User user)
{
    return new UserDto
    {
        Id = user.Id,
        Email = user.Email,
        FirstName = user.FirstName,
        LastName = user.LastName,
        Phone = user.Phone,
        Role = user.Role.ToString(),
        AvatarUrl = user.AvatarUrl
    };
}
```

### Step 3.2: Add Inventory Mappings to MappingProfile

**Modify:** `src/backend/ECommerce.Application/MappingProfile.cs`

Add new mappings at end of constructor:

```csharp
// Product -> InventoryDto (for inventory management views)
CreateMap<Product, InventoryDto>()
    .ForMember(dest => dest.ProductId, opt => opt.MapFrom(src => src.Id))
    .ForMember(dest => dest.ProductName, opt => opt.MapFrom(src => src.Name))
    .ForMember(dest => dest.ImageUrl, opt => opt.MapFrom(src =>
        src.Images.FirstOrDefault() != null ? src.Images.FirstOrDefault()!.Url : null));

// Product -> LowStockAlertDto (for low stock notifications)
CreateMap<Product, LowStockAlertDto>()
    .ForMember(dest => dest.ProductId, opt => opt.MapFrom(src => src.Id))
    .ForMember(dest => dest.ProductName, opt => opt.MapFrom(src => src.Name))
    .ForMember(dest => dest.CurrentStock, opt => opt.MapFrom(src => src.StockQuantity));
```

**Add using statement:**
```csharp
using ECommerce.Application.DTOs.Inventory;
```

### Step 3.3: Refactor InventoryService

**Modify:** `src/backend/ECommerce.Application/Services/InventoryService.cs`

**Change 1: Add IMapper to constructor**

```csharp
// BEFORE:
private readonly IUnitOfWork _unitOfWork;

public InventoryService(IUnitOfWork unitOfWork)
{
    _unitOfWork = unitOfWork;
}

// AFTER:
private readonly IUnitOfWork _unitOfWork;
private readonly IMapper _mapper;

public InventoryService(IUnitOfWork unitOfWork, IMapper mapper)
{
    _unitOfWork = unitOfWork;
    _mapper = mapper;
}
```

**Add using statement:**
```csharp
using AutoMapper;
```

**Change 2: Update GetAllInventoryAsync (lines 212-221)**

```csharp
// BEFORE:
return products.Select(p => new InventoryDto
{
    ProductId = p.Id,
    ProductName = p.Name,
    Sku = p.Sku,
    StockQuantity = p.StockQuantity,
    LowStockThreshold = p.LowStockThreshold,
    ImageUrl = p.Images.FirstOrDefault()?.Url,
    Price = p.Price
}).ToList();

// AFTER:
return _mapper.Map<List<InventoryDto>>(products);
```

**Change 3: Update GetLowStockProductsAsync (lines 228-242)**

```csharp
// BEFORE:
var lowStockProducts = products
    .Where(p => p.StockQuantity <= p.LowStockThreshold && p.IsActive)
    .OrderBy(p => p.StockQuantity)
    .Select(p => new LowStockAlert
    {
        ProductId = p.Id,
        ProductName = p.Name,
        Sku = p.Sku,
        CurrentStock = p.StockQuantity,
        LowStockThreshold = p.LowStockThreshold
    })
    .ToList();

return lowStockProducts;

// AFTER:
var lowStockProducts = products
    .Where(p => p.StockQuantity <= p.LowStockThreshold && p.IsActive)
    .OrderBy(p => p.StockQuantity)
    .ToList();

return _mapper.Map<List<LowStockAlertDto>>(lowStockProducts);
```

### Step 3.4: Services Kept As-Is (Justified)

The following services will **NOT** be refactored to use AutoMapper:

#### PaymentService.cs
**Reason:** Builds response DTOs with conditional business logic (success/failure states, dynamic metadata). AutoMapper is designed for entity-to-DTO mapping, not response building.

```csharp
// Add documentation comment:
/// <summary>
/// Note: Manual DTO creation is used in this service because response DTOs
/// are built with conditional business logic (success/failure states, dynamic metadata),
/// not direct entity mapping.
/// </summary>
```

#### DashboardService.cs
**Reason:** Maps from aggregated Dictionary data from repository queries, not from entities. AutoMapper provides no benefit for this pattern.

```csharp
// Add documentation comment:
/// <summary>
/// Note: Manual DTO creation is used in this service because data comes from
/// aggregated repository queries (Dictionary), not direct entity mapping.
/// </summary>
```

#### CartService.cs & WishlistService.cs
**Reason:** These services already inject IMapper but use custom async mapping methods (`MapCartToDtoAsync`, `MapWishlistToDtoAsync`) because they require async product fetching per item. This is an acceptable pattern.

---

## Phase 4: Naming Conventions

### Naming Convention Standard

| Pattern | Usage | Examples |
|---------|-------|----------|
| `*Dto` | Output/response DTOs, general DTOs | `ProductDto`, `UserDto`, `OrderDto` |
| `Create*Dto` | Input DTOs for creation | `CreateProductDto`, `CreateOrderDto` |
| `Update*Dto` | Input DTOs for updates | `UpdateProductDto`, `UpdateProfileDto` |
| `*Request` | API request payloads (actions) | `RefreshTokenRequest`, `AdjustStockRequest` |
| `*Response` | API response wrappers | `ApiResponse<T>`, `PaginatedResult<T>` |

### Step 4.1: Rename Inventory Types

**Modify:** `src/backend/ECommerce.Application/DTOs/Inventory/InventoryDtos.cs`

| Old Name | New Name |
|----------|----------|
| `LowStockAlert` | `LowStockAlertDto` |
| `StockIssue` | `StockIssueDto` |
| `StockCheckItem` | `StockCheckItemDto` |

```csharp
// BEFORE:
public class LowStockAlert { ... }
public class StockIssue { ... }
public class StockCheckItem { ... }

// AFTER:
public class LowStockAlertDto { ... }
public class StockIssueDto { ... }
public class StockCheckItemDto { ... }
```

### Step 4.2: Update All References

**Files to update:**

1. `src/backend/ECommerce.Application/Services/InventoryService.cs`
   - Change return types and variable types

2. `src/backend/ECommerce.Application/Services/OrderService.cs`
   - Update references to `StockIssueDto`, `StockCheckItemDto`

3. `src/backend/ECommerce.API/Controllers/InventoryController.cs`
   - Update method return types

4. `src/backend/ECommerce.Application/MappingProfile.cs`
   - Update mapping target types

---

## Phase 5: FluentValidation

### Step 5.1: Install FluentValidation.AspNetCore

**Modify:** `src/backend/ECommerce.API/ECommerce.API.csproj`

```xml
<PackageReference Include="FluentValidation.AspNetCore" Version="11.3.0" />
```

> Note: Using 11.3.0 for ASP.NET Core auto-validation integration

### Step 5.2: Create Validators Directory Structure

```
src/backend/ECommerce.Application/
  Validators/
    Auth/
      RegisterDtoValidator.cs
      LoginDtoValidator.cs
    Products/
      CreateProductDtoValidator.cs
    Orders/
      CreateOrderDtoValidator.cs
    PromoCodes/
      CreatePromoCodeDtoValidator.cs
```

### Step 5.3: Create Validators

#### RegisterDtoValidator

**Create:** `src/backend/ECommerce.Application/Validators/Auth/RegisterDtoValidator.cs`

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
            .Matches("[A-Z]").WithMessage("Password must contain at least one uppercase letter")
            .Matches("[a-z]").WithMessage("Password must contain at least one lowercase letter")
            .Matches("[0-9]").WithMessage("Password must contain at least one number");

        RuleFor(x => x.FirstName)
            .NotEmpty().WithMessage("First name is required")
            .MaximumLength(50).WithMessage("First name cannot exceed 50 characters");

        RuleFor(x => x.LastName)
            .NotEmpty().WithMessage("Last name is required")
            .MaximumLength(50).WithMessage("Last name cannot exceed 50 characters");
    }
}
```

#### LoginDtoValidator

**Create:** `src/backend/ECommerce.Application/Validators/Auth/LoginDtoValidator.cs`

```csharp
using FluentValidation;
using ECommerce.Application.DTOs.Auth;

namespace ECommerce.Application.Validators.Auth;

public class LoginDtoValidator : AbstractValidator<LoginDto>
{
    public LoginDtoValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email is required")
            .EmailAddress().WithMessage("Invalid email format");

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("Password is required");
    }
}
```

#### CreateProductDtoValidator

**Create:** `src/backend/ECommerce.Application/Validators/Products/CreateProductDtoValidator.cs`

```csharp
using FluentValidation;
using ECommerce.Application.DTOs.Products;

namespace ECommerce.Application.Validators.Products;

public class CreateProductDtoValidator : AbstractValidator<CreateProductDto>
{
    public CreateProductDtoValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Product name is required")
            .MaximumLength(200).WithMessage("Product name cannot exceed 200 characters");

        RuleFor(x => x.Slug)
            .NotEmpty().WithMessage("Slug is required")
            .MaximumLength(200).WithMessage("Slug cannot exceed 200 characters")
            .Matches("^[a-z0-9-]+$").WithMessage("Slug must contain only lowercase letters, numbers, and hyphens");

        RuleFor(x => x.Price)
            .GreaterThan(0).WithMessage("Price must be greater than 0")
            .LessThanOrEqualTo(1000000).WithMessage("Price cannot exceed 1,000,000");

        RuleFor(x => x.StockQuantity)
            .GreaterThanOrEqualTo(0).WithMessage("Stock quantity cannot be negative");

        RuleFor(x => x.LowStockThreshold)
            .GreaterThanOrEqualTo(0).WithMessage("Low stock threshold cannot be negative");

        RuleFor(x => x.CompareAtPrice)
            .GreaterThan(x => x.Price)
            .When(x => x.CompareAtPrice.HasValue)
            .WithMessage("Compare at price must be greater than the regular price");
    }
}
```

#### CreateOrderDtoValidator

**Create:** `src/backend/ECommerce.Application/Validators/Orders/CreateOrderDtoValidator.cs`

```csharp
using FluentValidation;
using ECommerce.Application.DTOs.Orders;

namespace ECommerce.Application.Validators.Orders;

public class CreateOrderDtoValidator : AbstractValidator<CreateOrderDto>
{
    public CreateOrderDtoValidator()
    {
        RuleFor(x => x.PaymentMethod)
            .NotEmpty().WithMessage("Payment method is required");

        RuleFor(x => x.ShippingAddress)
            .NotNull().WithMessage("Shipping address is required")
            .SetValidator(new AddressDtoValidator()!);

        RuleFor(x => x.Items)
            .NotEmpty().WithMessage("Order must contain at least one item");

        RuleForEach(x => x.Items)
            .SetValidator(new CreateOrderItemDtoValidator());
    }
}

public class AddressDtoValidator : AbstractValidator<AddressDto>
{
    public AddressDtoValidator()
    {
        RuleFor(x => x.FirstName)
            .NotEmpty().WithMessage("First name is required")
            .MaximumLength(50);

        RuleFor(x => x.LastName)
            .NotEmpty().WithMessage("Last name is required")
            .MaximumLength(50);

        RuleFor(x => x.StreetLine1)
            .NotEmpty().WithMessage("Street address is required")
            .MaximumLength(200);

        RuleFor(x => x.City)
            .NotEmpty().WithMessage("City is required")
            .MaximumLength(100);

        RuleFor(x => x.State)
            .NotEmpty().WithMessage("State is required")
            .MaximumLength(100);

        RuleFor(x => x.PostalCode)
            .NotEmpty().WithMessage("Postal code is required")
            .MaximumLength(20);

        RuleFor(x => x.Country)
            .NotEmpty().WithMessage("Country is required")
            .Length(2).WithMessage("Country must be a 2-letter ISO code");
    }
}

public class CreateOrderItemDtoValidator : AbstractValidator<CreateOrderItemDto>
{
    public CreateOrderItemDtoValidator()
    {
        RuleFor(x => x.ProductId)
            .NotEmpty().WithMessage("Product ID is required");

        RuleFor(x => x.Quantity)
            .GreaterThan(0).WithMessage("Quantity must be at least 1");
    }
}
```

#### CreatePromoCodeDtoValidator

**Create:** `src/backend/ECommerce.Application/Validators/PromoCodes/CreatePromoCodeDtoValidator.cs`

```csharp
using FluentValidation;
using ECommerce.Application.DTOs.PromoCodes;

namespace ECommerce.Application.Validators.PromoCodes;

public class CreatePromoCodeDtoValidator : AbstractValidator<CreatePromoCodeDto>
{
    public CreatePromoCodeDtoValidator()
    {
        RuleFor(x => x.Code)
            .NotEmpty().WithMessage("Promo code is required")
            .MaximumLength(50).WithMessage("Promo code cannot exceed 50 characters")
            .Matches("^[A-Z0-9]+$").WithMessage("Promo code must contain only uppercase letters and numbers");

        RuleFor(x => x.DiscountType)
            .NotEmpty().WithMessage("Discount type is required")
            .Must(x => x == "percentage" || x == "fixed")
            .WithMessage("Discount type must be 'percentage' or 'fixed'");

        RuleFor(x => x.DiscountValue)
            .GreaterThan(0).WithMessage("Discount value must be greater than 0");

        RuleFor(x => x.DiscountValue)
            .LessThanOrEqualTo(100)
            .When(x => x.DiscountType == "percentage")
            .WithMessage("Percentage discount cannot exceed 100%");

        RuleFor(x => x.MinOrderAmount)
            .GreaterThanOrEqualTo(0)
            .When(x => x.MinOrderAmount.HasValue)
            .WithMessage("Minimum order amount cannot be negative");

        RuleFor(x => x.MaxDiscountAmount)
            .GreaterThan(0)
            .When(x => x.MaxDiscountAmount.HasValue)
            .WithMessage("Maximum discount amount must be greater than 0");

        RuleFor(x => x.MaxUses)
            .GreaterThan(0)
            .When(x => x.MaxUses.HasValue)
            .WithMessage("Maximum uses must be greater than 0");

        RuleFor(x => x.EndDate)
            .GreaterThan(x => x.StartDate)
            .When(x => x.StartDate.HasValue && x.EndDate.HasValue)
            .WithMessage("End date must be after start date");
    }
}
```

### Step 5.4: Register Validators in DI

**Modify:** `src/backend/ECommerce.API/Program.cs`

Add after `builder.Services.AddControllers()`:

```csharp
using FluentValidation;
using FluentValidation.AspNetCore;
using ECommerce.Application.Validators.Auth;

// Add FluentValidation with automatic validation
builder.Services.AddFluentValidationAutoValidation();
builder.Services.AddValidatorsFromAssemblyContaining<RegisterDtoValidator>();
```

### Step 5.5: Validators Summary

| Validator | DTO | Key Validation Rules |
|-----------|-----|---------------------|
| `RegisterDtoValidator` | `RegisterDto` | Email format, password strength (8+ chars, uppercase, lowercase, number), name lengths |
| `LoginDtoValidator` | `LoginDto` | Email format, password required |
| `CreateProductDtoValidator` | `CreateProductDto` | Name/slug required, slug format, price > 0, stock >= 0 |
| `CreateOrderDtoValidator` | `CreateOrderDto` | Payment method required, address validation, items not empty |
| `CreatePromoCodeDtoValidator` | `CreatePromoCodeDto` | Code format (uppercase alphanumeric), discount type valid, date logic |

---

## Files Summary

### Files to Create (7)

| File | Purpose |
|------|---------|
| `DTOs/Auth/AuthRequestDtos.cs` | Auth request/response DTOs moved from controller |
| `Validators/Auth/RegisterDtoValidator.cs` | Registration input validation |
| `Validators/Auth/LoginDtoValidator.cs` | Login input validation |
| `Validators/Products/CreateProductDtoValidator.cs` | Product creation validation |
| `Validators/Orders/CreateOrderDtoValidator.cs` | Order creation validation (includes AddressDtoValidator, CreateOrderItemDtoValidator) |
| `Validators/PromoCodes/CreatePromoCodeDtoValidator.cs` | Promo code creation validation |

### Files to Modify (10)

| File | Changes |
|------|---------|
| `Controllers/AuthController.cs` | Remove `#region DTOs` section (lines 140-182) |
| `Controllers/OrdersController.cs` | Remove `UpdateOrderStatusDto` (lines 119-122) |
| `DTOs/Orders/OrderDtos.cs` | Add `UpdateOrderStatusDto` |
| `DTOs/Products/ProductDto.cs` | Rename `CategoryDto` → `ProductCategoryDto`, `ReviewDto` → `ProductReviewDto` |
| `DTOs/Inventory/InventoryDtos.cs` | Add `Dto` suffix to `LowStockAlert`, `StockIssue`, `StockCheckItem` |
| `Services/AuthService.cs` | Add IMapper dependency, replace manual mapping, delete `MapToUserDto()` |
| `Services/InventoryService.cs` | Add IMapper dependency, use AutoMapper for `GetAllInventoryAsync` and `GetLowStockProductsAsync` |
| `MappingProfile.cs` | Update renamed DTOs, add `Product` → `InventoryDto` and `LowStockAlertDto` mappings |
| `ECommerce.API.csproj` | Add `FluentValidation.AspNetCore` package |
| `Program.cs` | Register FluentValidation services |

---

## Verification Plan

### After Each Phase

1. **Build solution:** `dotnet build`
2. **Run tests:** `dotnet test`
3. **Search for orphaned references** to old type names

### Final Verification

#### API Endpoint Testing

| Endpoint | DTO Changes | Test |
|----------|-------------|------|
| `POST /api/auth/register` | Uses RegisterDto with validation | Send invalid data, expect 400 with validation errors |
| `POST /api/auth/login` | Uses LoginDto with validation | Send invalid email, expect validation error |
| `GET /api/products` | Uses ProductCategoryDto | Verify category data in response |
| `GET /api/products/{id}` | Uses ProductReviewDto | Verify reviews data in response |
| `GET /api/inventory` | Uses InventoryDto via AutoMapper | Verify inventory list returns correctly |
| `GET /api/inventory/low-stock` | Uses LowStockAlertDto via AutoMapper | Verify low stock alerts return correctly |
| `PUT /api/orders/{id}/status` | Uses UpdateOrderStatusDto | Verify order status can be updated |

#### Validation Testing

```bash
# Test invalid registration
curl -X POST http://localhost:5000/api/auth/register \
  -H "Content-Type: application/json" \
  -d '{"email": "invalid", "password": "123", "firstName": "", "lastName": ""}'

# Expected: 400 Bad Request
{
  "errors": {
    "Email": ["Invalid email format"],
    "Password": ["Password must be at least 8 characters"],
    "FirstName": ["First name is required"],
    "LastName": ["Last name is required"]
  }
}
```

#### Unit Tests

Run existing unit tests to ensure no regressions:
```bash
cd src/backend/ECommerce.Tests
dotnet test
```

Expected: All 88 existing tests should pass.

---

## Risk Assessment

| Phase | Risk Level | Potential Impact | Mitigation Strategy |
|-------|------------|------------------|---------------------|
| Phase 1: Move DTOs | Low | None - just file reorganization | Verify using statements compile |
| Phase 2: Rename DTOs | Medium | Type name changes break references | Update all references, search for old names |
| Phase 3: AutoMapper | Medium | Service constructor changes | Verify DI registration, test affected endpoints |
| Phase 4: Naming | Low | Simple renames | Global search/replace |
| Phase 5: Validation | Low-Medium | May reject previously accepted data | Test with edge cases, document new requirements |

### Rollback Strategy

Each phase can be reverted independently:
- Phase 1: Move DTOs back to controllers
- Phase 2: Rename DTOs back to original names
- Phase 3: Revert service changes, remove IMapper
- Phase 4: Rename types back
- Phase 5: Remove validators and DI registration

---

## Appendix: Current DTO Inventory

### Properly Organized DTOs (61 total)

| Location | DTOs |
|----------|------|
| `Auth/AuthDtos.cs` | RegisterDto, LoginDto, AuthResponseDto, UserDto, GoogleOAuthDto, FacebookOAuthDto |
| `Cart/CartDtos.cs` | CartDto, CartItemDto, AddToCartDto, UpdateCartItemDto |
| `CategoryDto.cs` | CategoryDto, CategoryDetailDto, CreateCategoryDto, UpdateCategoryDto |
| `Orders/OrderDtos.cs` | OrderDto, OrderDetailDto, OrderItemDto, CreateOrderDto, CreateOrderItemDto, AddressDto |
| `Payments/PaymentDtos.cs` | ProcessPaymentDto, PaymentResponseDto, PaymentDetailsDto, RefundPaymentDto, RefundResponseDto |
| `Products/ProductDto.cs` | ProductDto, ProductDetailDto, ProductImageDto |
| `Products/CreateProductDto.cs` | CreateProductDto, UpdateProductDto |
| `Reviews/ReviewDtos.cs` | CreateReviewDto, UpdateReviewDto, ReviewDetailDto |
| `Wishlist/WishlistDtos.cs` | WishlistItemDto, WishlistDto, AddToWishlistDto |
| `Users/UserProfileDtos.cs` | UserProfileDto, UpdateProfileDto |
| `Dashboard/DashboardStatsDto.cs` | DashboardStatsDto, OrderTrendDto, RevenueTrendDto |
| `Inventory/InventoryDtos.cs` | InventoryDto, InventoryLogDto, AdjustStockRequest, StockCheckRequest, StockCheckItem, StockCheckResponse, StockIssue, LowStockAlert |
| `PromoCodes/PromoCodeDtos.cs` | PromoCodeDto, PromoCodeDetailDto, CreatePromoCodeDto, UpdatePromoCodeDto, ValidatePromoCodeRequest, ValidatePromoCodeDto |
| `Common/PaginatedResult.cs` | PaginatedResult<T>, ApiResponse<T>, PagedRequest |
| `Common/ErrorDetails.cs` | ErrorDetails |

### DTOs to Move (9 total)

| Current Location | DTOs to Move |
|------------------|--------------|
| `AuthController.cs` | RefreshTokenRequest, TokenResponseDto, ForgotPasswordRequest, ForgotPasswordResponseDto, ResetPasswordRequest, ChangePasswordRequest, VerifyEmailRequest |
| `OrdersController.cs` | UpdateOrderStatusDto |

---

*Document created: January 2026*
*Last updated: January 2026*
