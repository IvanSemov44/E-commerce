# Remaining Implementation Phases: Quick Reference

## Phase 7-8: Service Layer Refactoring

### Overview
Add CancellationToken support to all service methods and ensure they use the new exception types.

### Pattern to Follow

**Service Interface**:
```csharp
public interface IProductService
{
    Task<ProductDto> GetProductByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<PagedList<ProductDto>> GetProductsAsync(
        ProductRequestParameters parameters, 
        CancellationToken cancellationToken = default);
    Task<ProductDto> CreateProductAsync(
        CreateProductDto dto, 
        CancellationToken cancellationToken = default);
    Task<ProductDto> UpdateProductAsync(
        Guid id, 
        UpdateProductDto dto, 
        CancellationToken cancellationToken = default);
    Task DeleteProductAsync(Guid id, CancellationToken cancellationToken = default);
}
```

**Service Implementation**:
```csharp
public class ProductService : IProductService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public async Task<ProductDto> GetProductByIdAsync(
        Guid id, 
        CancellationToken cancellationToken = default)
    {
        var product = await _unitOfWork.Products.GetByIdAsync(id, cancellationToken: cancellationToken);
        
        if (product == null)
            throw new ProductNotFoundException(id);
        
        return _mapper.Map<ProductDto>(product);
    }

    public async Task<PagedList<ProductDto>> GetProductsAsync(
        ProductRequestParameters parameters, 
        CancellationToken cancellationToken = default)
    {
        var query = _unitOfWork.Products.FindAll()
            .ApplySort(parameters.OrderBy, !parameters.IsDescending)
            .SearchBy(p => p.Name, parameters.SearchTerm);
        
        var (totalCount, items) = await query.GetPagedDataAsync(
            parameters.PageNumber, 
            parameters.PageSize, 
            cancellationToken);
        
        var dtos = _mapper.Map<List<ProductDto>>(items);
        return new PagedList<ProductDto>(dtos, totalCount, parameters.PageNumber, parameters.PageSize);
    }

    public async Task<ProductDto> CreateProductAsync(
        CreateProductDto dto, 
        CancellationToken cancellationToken = default)
    {
        if (await _unitOfWork.Categories.ExistsAsync(dto.CategoryId, cancellationToken: cancellationToken) == false)
            throw new CategoryNotFoundException(dto.CategoryId);
        
        var product = _mapper.Map<Product>(dto);
        
        try
        {
            await _unitOfWork.Products.AddAsync(product, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            throw new BadRequestException($"Failed to create product: {ex.Message}");
        }
        
        return _mapper.Map<ProductDto>(product);
    }

    public async Task<ProductDto> UpdateProductAsync(
        Guid id, 
        UpdateProductDto dto, 
        CancellationToken cancellationToken = default)
    {
        var product = await _unitOfWork.Products.GetByIdAsync(id, trackChanges: true, cancellationToken: cancellationToken);
        
        if (product == null)
            throw new ProductNotFoundException(id);
        
        if (dto.CategoryId != Guid.Empty && 
            await _unitOfWork.Categories.ExistsAsync(dto.CategoryId, cancellationToken: cancellationToken) == false)
            throw new CategoryNotFoundException(dto.CategoryId);
        
        _mapper.Map(dto, product);
        
        await _unitOfWork.Products.UpdateAsync(product, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        
        return _mapper.Map<ProductDto>(product);
    }

    public async Task DeleteProductAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var product = await _unitOfWork.Products.GetByIdAsync(id, trackChanges: true, cancellationToken: cancellationToken);
        
        if (product == null)
            throw new ProductNotFoundException(id);
        
        await _unitOfWork.Products.DeleteAsync(product, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
```

### Services to Update (Priority Order)
1. **ProductService** - Foundational, widely used
2. **CategoryService** - Dependency for products
3. **UserService** - Authentication dependent
4. **OrderService** - Complex, multi-repository
5. **CartService** - Shopping flow dependent
6. **ReviewService** - Secondary feature
7. **PromoCodeService** - Secondary feature
8. **InventoryService** - Inventory management
9. **WishlistService** - Secondary feature
10. **PaymentService** - Payment processing
11. **AuthService** - Authentication
12. **DashboardService** - Analytics/reporting
13. **SendGridEmailService** - Email service
14. **SmtpEmailService** - Email service

### Checklist per Service
- [ ] Add CancellationToken to all async methods
- [ ] Pass CancellationToken through repository calls
- [ ] Replace old exceptions with new specific types
- [ ] Add try-catch for database exceptions
- [ ] Update method signatures in interface and implementation
- [ ] Update all calls to services from other services
- [ ] Add XML documentation

---

## Phase 9: Controller Updates

### Pattern to Follow

**Before**:
```csharp
[HttpPost]
public async Task<IActionResult> CreateProduct([FromBody] CreateProductDto dto)
{
    if (dto == null)
        return BadRequest("Object is null");
    
    if (!ModelState.IsValid)
        return UnprocessableEntity(ModelState);
    
    try
    {
        var result = await _productService.CreateProductAsync(dto);
        return Ok(ApiResponse<ProductDto>.Ok(result));
    }
    catch (Exception ex)
    {
        return BadRequest(ApiResponse<object>.Error(ex.Message));
    }
}
```

**After**:
```csharp
[HttpPost]
[ValidationFilter]  // Automatic validation
public async Task<IActionResult> CreateProduct(
    [FromBody] CreateProductDto dto,
    CancellationToken cancellationToken)  // Cancellation support
{
    var result = await _productService.CreateProductAsync(dto, cancellationToken);
    return Ok(ApiResponse<ProductDto>.Ok(result, "Product created successfully"));
}
```

The `ValidationFilter` attribute handles null checks and ModelState validation automatically. Exception middleware handles errors globally.

### Controllers to Update (Priority Order)
1. **ProductsController**
2. **CategoriesController**
3. **UsersController**
4. **AuthController**
5. **OrdersController**
6. **CartsController**
7. **ReviewsController**
8. **PromoCodeController**
9. **WishlistController**
10. **InventoryController**
11. **DashboardController**

### Changes per Action Method
- [ ] Add `[ValidationFilter]` attribute
- [ ] Add `CancellationToken cancellationToken` parameter
- [ ] Remove manual null checks (handled by attribute)
- [ ] Remove manual ModelState checks (handled by attribute)
- [ ] Remove try-catch blocks (handled by middleware)
- [ ] Pass CancellationToken to service calls
- [ ] Update response format using ApiResponse
- [ ] Add Swagger documentation

### Swagger Documentation Example
```csharp
/// <summary>
/// Retrieves a product by its ID.
/// </summary>
/// <param name="id">The product ID.</param>
/// <param name="cancellationToken">Cancellation token.</param>
/// <returns>The product data if found.</returns>
/// <response code="200">Product found and returned.</response>
/// <response code="404">Product not found.</response>
[HttpGet("{id}")]
[ProducesResponseType(typeof(ApiResponse<ProductDto>), StatusCodes.Status200OK)]
[ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
public async Task<IActionResult> GetProductById(
    Guid id,
    CancellationToken cancellationToken)
{
    var product = await _productService.GetProductByIdAsync(id, cancellationToken);
    return Ok(ApiResponse<ProductDto>.Ok(product));
}
```

---

## Phase 10: Testing & Verification

### Test Categories

**1. Build Verification**
```powershell
dotnet build                    # Full build
dotnet build --no-restore      # Incremental build
dotnet clean && dotnet build   # Clean build
```

**2. Unit Tests**
```powershell
dotnet test
dotnet test --verbosity=detailed
dotnet test --collect:"XPlat Code Coverage"
```

**3. Integration Tests**
```powershell
# Run specific test file
dotnet test ECommerce.Tests/Integration/ProductServiceTests.cs

# Run with filter
dotnet test --filter "Category=Integration"
```

**4. Manual API Testing**
- Start the application
- Open Swagger UI at http://localhost:5000 (adjust port as needed)
- Test each endpoint with different scenarios
- Verify error responses
- Test pagination

### Verification Checklist
- [ ] Build succeeds with 0 errors
- [ ] All unit tests pass
- [ ] All integration tests pass
- [ ] Manual API testing successful
- [ ] CancellationToken flows properly through stack
- [ ] New exception types work correctly
- [ ] ValidationFilter catches validation errors
- [ ] Global middleware catches unhandled exceptions
- [ ] Pagination works with new MetaData
- [ ] Sorting and filtering work correctly

### Documentation to Create/Update
- [ ] Update README with new features
- [ ] Create MIGRATION.md for developers
- [ ] Update API documentation
- [ ] Document exception handling approach
- [ ] Create troubleshooting guide
- [ ] Update architecture documentation

### Performance Benchmarks (Optional)
- Measure pagination performance
- Test large result sets
- Benchmark sorting operations
- Profile memory usage

---

## Automation Scripts

### Phase 7-8 Helper: Service Update Template
```powershell
# Create list of services to update
$services = @(
    "ProductService",
    "CategoryService",
    "UserService",
    "OrderService",
    "CartService",
    "ReviewService",
    "PromoCodeService",
    "InventoryService",
    "WishlistService",
    "PaymentService",
    "AuthService",
    "DashboardService"
)

foreach ($service in $services) {
    Write-Host "TODO: Update $service" -ForegroundColor Yellow
}
```

### Phase 9 Helper: Controller Update Template
```powershell
# List of controllers
$controllers = @(
    "ProductsController",
    "CategoriesController",
    "UsersController",
    "AuthController",
    "OrdersController",
    "CartsController",
    "ReviewsController",
    "PromoCodeController",
    "WishlistController",
    "InventoryController",
    "DashboardController"
)

foreach ($controller in $controllers) {
    Write-Host "TODO: Update $controller" -ForegroundColor Yellow
}
```

### Phase 10 Testing Script
```powershell
Write-Host "Phase 10: Testing & Verification" -ForegroundColor Cyan

Write-Host "`n1. Build Verification..." -ForegroundColor Green
dotnet build
if ($LASTEXITCODE -ne 0) { exit 1 }

Write-Host "`n2. Running Unit Tests..." -ForegroundColor Green
dotnet test
if ($LASTEXITCODE -ne 0) { exit 1 }

Write-Host "`n3. Build succeeded! Check warnings above." -ForegroundColor Green
```

---

## Code Review Checklist

- [ ] All async methods have CancellationToken parameter
- [ ] CancellationToken is passed through to repository calls
- [ ] All exceptions use new specific types
- [ ] ValidationFilter attribute used on all POST/PUT/PATCH actions
- [ ] No manual ModelState validation
- [ ] No try-catch for validation (handled by filter)
- [ ] All public methods have XML documentation
- [ ] Response format uses ApiResponse consistently
- [ ] No hardcoded error messages (use exception types)
- [ ] Pagination uses new PagedList pattern
- [ ] Sorting uses ApplySort extension
- [ ] Filtering uses Where extension
- [ ] Searching uses SearchBy extension

---

## Summary

**Total Services**: 14 (estimated 2-3 hours each)
**Total Controllers**: 11 (estimated 1-2 hours each)
**Estimated Remaining Time**: 35-50 hours

**Recommended Approach**:
1. Start with core services (Product, Category, User)
2. Update dependent controllers
3. Test incrementally
4. Document as you go
5. Final verification and cleanup

---

**Last Updated**: 2025-02-03
**Current Phase**: 7-10 Planning
**Status**: Ready for Execution
