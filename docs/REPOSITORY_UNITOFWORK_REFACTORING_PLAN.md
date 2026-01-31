# Repository & UnitOfWork Pattern Refactoring Plan

## Executive Summary

This document outlines the refactoring plan to align the E-commerce project's Repository and UnitOfWork patterns with best practices from "Ultimate ASP.NET Core Web API". The main goals are:

1. **Enforce atomic transactions** - Remove SaveChangesAsync from individual repositories
2. **Improve read performance** - Add trackChanges parameter for AsNoTracking optimization
3. **Clean up dual access pattern** - Standardize on specialized repositories only
4. **Add missing specialized repositories** - Expose all entity-specific repos through UnitOfWork

---

## Current State Analysis

### Architecture Overview

```
ECommerce.Core (Domain Layer)
├── Interfaces/Repositories/
│   ├── IRepository<T>           # Generic interface
│   ├── IUnitOfWork               # Unit of Work interface
│   ├── IProductRepository        # Specialized
│   ├── IOrderRepository          # Specialized
│   ├── IUserRepository           # Specialized
│   ├── ICategoryRepository       # Specialized (NOT in UoW)
│   ├── ICartRepository           # Specialized (NOT in UoW)
│   ├── IReviewRepository         # Specialized (NOT in UoW)
│   └── IWishlistRepository       # Specialized (NOT in UoW)

ECommerce.Infrastructure
├── Repositories/
│   ├── Repository<T>             # Generic implementation
│   ├── ProductRepository
│   ├── OrderRepository
│   ├── UserRepository
│   ├── CategoryRepository
│   ├── CartRepository
│   ├── ReviewRepository
│   └── WishlistRepository
└── UnitOfWork.cs
```

### Issue #1: SaveChangesAsync in Generic Repository 🔴

**Location:** `src/backend/ECommerce.Infrastructure/Repositories/Repository.cs:47-50`

```csharp
// PROBLEM: Each repository can save independently, breaking atomic transactions
public virtual async Task<int> SaveChangesAsync()
{
    return await Context.SaveChangesAsync();
}
```

**Impact:** Services calling `repository.SaveChangesAsync()` bypass the UnitOfWork pattern, making it impossible to:
- Roll back partial changes on failure
- Batch multiple operations into one transaction
- Maintain data consistency

**Example of broken pattern:**
```csharp
// ProductRepository.cs:60-68
public async Task UpdateStockAsync(Guid productId, int quantity)
{
    var product = await DbSet.FirstOrDefaultAsync(p => p.Id == productId);
    if (product != null)
    {
        product.StockQuantity = quantity;
        product.UpdatedAt = DateTime.UtcNow;
        await SaveChangesAsync();  // ❌ Saves immediately, can't be part of transaction
    }
}
```

---

### Issue #2: Missing trackChanges Parameter 🟡

**Location:** `src/backend/ECommerce.Core/Interfaces/Repositories/IRepository.cs`

```csharp
// CURRENT: Always tracks changes
Task<T?> GetByIdAsync(Guid id);
Task<IEnumerable<T>> GetAllAsync();
```

**Book Pattern:**
```csharp
// RECOMMENDED: Optional tracking for read-only optimization
IQueryable<T> FindAll(bool trackChanges);
IQueryable<T> FindByCondition(Expression<Func<T, bool>> expression, bool trackChanges);
```

**Performance Impact:**
- EF Core tracks all entities for potential updates
- Read-only queries waste memory and CPU
- `AsNoTracking()` can improve read performance by 20-50%

---

### Issue #3: Dual Access Pattern in UnitOfWork 🟡

**Location:** `src/backend/ECommerce.Core/Interfaces/Repositories/IUnitOfWork.cs:7-24`

```csharp
// CONFUSION: Two ways to access the same entity
IRepository<Product> Products { get; }      // Generic access
IProductRepository ProductRepository { get; }  // Specialized access

// Which should services use?
```

**Current Usage Analysis:**
| Entity | Generic in UoW | Specialized in UoW | Should Use |
|--------|----------------|-------------------|------------|
| Product | ✅ `Products` | ✅ `ProductRepository` | Specialized only |
| Order | ✅ `Orders` | ✅ `OrderRepository` | Specialized only |
| User | ✅ `Users` | ✅ `UserRepository` | Specialized only |
| Category | ✅ `Categories` | ❌ Missing | Add specialized |
| Cart | ✅ `Carts` | ❌ Missing | Add specialized |
| Review | ✅ `Reviews` | ❌ Missing | Add specialized |
| Wishlist | ✅ `Wishlists` | ❌ Missing | Add specialized |

---

### Issue #4: Missing Specialized Repositories in UnitOfWork 🟡

These specialized repositories exist but aren't exposed through UnitOfWork:

| Repository | File Exists | In UnitOfWork |
|------------|-------------|---------------|
| `ICategoryRepository` | ✅ | ❌ |
| `ICartRepository` | ✅ | ❌ |
| `IReviewRepository` | ✅ | ❌ |
| `IWishlistRepository` | ✅ | ❌ |

---

## Implementation Plan

### Phase 1: Remove SaveChangesAsync from Generic Repository

**Goal:** Enforce that all saves go through UnitOfWork only.

#### Step 1.1: Update IRepository Interface

**Modify:** `src/backend/ECommerce.Core/Interfaces/Repositories/IRepository.cs`

```csharp
using System.Linq.Expressions;
using ECommerce.Core.Common;

namespace ECommerce.Core.Interfaces.Repositories;

public interface IRepository<T> where T : BaseEntity
{
    // Read operations with optional change tracking
    Task<T?> GetByIdAsync(Guid id, bool trackChanges = true);
    Task<IEnumerable<T>> GetAllAsync(bool trackChanges = true);
    IQueryable<T> FindAll(bool trackChanges = false);
    IQueryable<T> FindByCondition(Expression<Func<T, bool>> expression, bool trackChanges = false);

    // Write operations (no SaveChanges - UnitOfWork handles that)
    void Add(T entity);
    Task AddAsync(T entity);
    void AddRange(IEnumerable<T> entities);
    void Update(T entity);
    void Delete(T entity);
    void DeleteRange(IEnumerable<T> entities);

    // Utility
    Task<bool> ExistsAsync(Guid id);
    Task<int> CountAsync();
    Task<int> CountAsync(Expression<Func<T, bool>> predicate);
}
```

#### Step 1.2: Update Repository Implementation

**Modify:** `src/backend/ECommerce.Infrastructure/Repositories/Repository.cs`

```csharp
using System.Linq.Expressions;
using ECommerce.Core.Common;
using ECommerce.Core.Interfaces.Repositories;
using ECommerce.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Infrastructure.Repositories;

public class Repository<T> : IRepository<T> where T : BaseEntity
{
    protected readonly AppDbContext Context;
    protected readonly DbSet<T> DbSet;

    public Repository(AppDbContext context)
    {
        Context = context;
        DbSet = context.Set<T>();
    }

    #region Read Operations

    public virtual async Task<T?> GetByIdAsync(Guid id, bool trackChanges = true)
    {
        var query = trackChanges ? DbSet : DbSet.AsNoTracking();
        return await query.FirstOrDefaultAsync(x => x.Id == id);
    }

    public virtual async Task<IEnumerable<T>> GetAllAsync(bool trackChanges = true)
    {
        var query = trackChanges ? DbSet : DbSet.AsNoTracking();
        return await query.ToListAsync();
    }

    public virtual IQueryable<T> FindAll(bool trackChanges = false)
    {
        return trackChanges ? DbSet : DbSet.AsNoTracking();
    }

    public virtual IQueryable<T> FindByCondition(Expression<Func<T, bool>> expression, bool trackChanges = false)
    {
        return trackChanges
            ? DbSet.Where(expression)
            : DbSet.Where(expression).AsNoTracking();
    }

    #endregion

    #region Write Operations

    public virtual void Add(T entity)
    {
        DbSet.Add(entity);
    }

    public virtual async Task AddAsync(T entity)
    {
        await DbSet.AddAsync(entity);
    }

    public virtual void AddRange(IEnumerable<T> entities)
    {
        DbSet.AddRange(entities);
    }

    public virtual void Update(T entity)
    {
        DbSet.Update(entity);
    }

    public virtual void Delete(T entity)
    {
        DbSet.Remove(entity);
    }

    public virtual void DeleteRange(IEnumerable<T> entities)
    {
        DbSet.RemoveRange(entities);
    }

    #endregion

    #region Utility

    public virtual async Task<bool> ExistsAsync(Guid id)
    {
        return await DbSet.AnyAsync(x => x.Id == id);
    }

    public virtual async Task<int> CountAsync()
    {
        return await DbSet.CountAsync();
    }

    public virtual async Task<int> CountAsync(Expression<Func<T, bool>> predicate)
    {
        return await DbSet.CountAsync(predicate);
    }

    #endregion
}
```

#### Step 1.3: Fix ProductRepository.UpdateStockAsync

**Modify:** `src/backend/ECommerce.Infrastructure/Repositories/ProductRepository.cs`

```csharp
// BEFORE (lines 60-68)
public async Task UpdateStockAsync(Guid productId, int quantity)
{
    var product = await DbSet.FirstOrDefaultAsync(p => p.Id == productId);
    if (product != null)
    {
        product.StockQuantity = quantity;
        product.UpdatedAt = DateTime.UtcNow;
        await SaveChangesAsync();  // ❌ Remove this
    }
}

// AFTER
public async Task UpdateStockAsync(Guid productId, int quantity)
{
    var product = await DbSet.FirstOrDefaultAsync(p => p.Id == productId);
    if (product != null)
    {
        product.StockQuantity = quantity;
        product.UpdatedAt = DateTime.UtcNow;
        // Don't call SaveChangesAsync - let UnitOfWork handle it
    }
}
```

#### Step 1.4: Update IProductRepository Interface

**Modify:** `src/backend/ECommerce.Core/Interfaces/Repositories/IProductRepository.cs`

Update method signatures to use trackChanges where applicable:

```csharp
using ECommerce.Core.Entities;

namespace ECommerce.Core.Interfaces.Repositories;

public interface IProductRepository : IRepository<Product>
{
    Task<Product?> GetBySlugAsync(string slug, bool trackChanges = false);
    Task<IEnumerable<Product>> GetByCategoryAsync(Guid categoryId, bool trackChanges = false);
    Task<IEnumerable<Product>> GetFeaturedAsync(int count, bool trackChanges = false);
    Task<IEnumerable<Product>> GetActiveProductsAsync(int skip, int take, bool trackChanges = false);
    Task<int> GetActiveProductsCountAsync();
    Task<(IEnumerable<Product> Items, int TotalCount)> GetProductsWithFiltersAsync(
        int skip,
        int take,
        Guid? categoryId = null,
        string? searchQuery = null,
        decimal? minPrice = null,
        decimal? maxPrice = null,
        decimal? minRating = null,
        bool? isFeatured = null,
        string? sortBy = null,
        bool trackChanges = false);
    void UpdateStock(Guid productId, int quantity);  // Changed from async Task
    Task<bool> IsSlugUniqueAsync(string slug, Guid? excludeId = null);
}
```

---

### Phase 2: Standardize UnitOfWork with Specialized Repositories Only

**Goal:** Remove generic repository access, expose only specialized repositories.

#### Step 2.1: Update IUnitOfWork Interface

**Modify:** `src/backend/ECommerce.Core/Interfaces/Repositories/IUnitOfWork.cs`

```csharp
namespace ECommerce.Core.Interfaces.Repositories;

public interface IUnitOfWork : IDisposable, IAsyncDisposable
{
    // Specialized repositories only (no generic IRepository<T> access)
    IProductRepository Products { get; }
    IOrderRepository Orders { get; }
    IUserRepository Users { get; }
    ICategoryRepository Categories { get; }
    ICartRepository Carts { get; }
    IReviewRepository Reviews { get; }
    IWishlistRepository Wishlists { get; }

    // For entities without specialized repos, use generic access
    IRepository<OrderItem> OrderItems { get; }
    IRepository<CartItem> CartItems { get; }
    IRepository<Address> Addresses { get; }
    IRepository<PromoCode> PromoCodes { get; }
    IRepository<InventoryLog> InventoryLogs { get; }
    IRepository<ProductImage> ProductImages { get; }

    // Transaction management
    Task<int> SaveChangesAsync();
    Task<IAsyncTransaction> BeginTransactionAsync();
}

public interface IAsyncTransaction : IAsyncDisposable
{
    Task CommitAsync();
    Task RollbackAsync();
}
```

#### Step 2.2: Update UnitOfWork Implementation

**Modify:** `src/backend/ECommerce.Infrastructure/UnitOfWork.cs`

```csharp
using ECommerce.Core.Entities;
using ECommerce.Core.Interfaces.Repositories;
using ECommerce.Infrastructure.Data;
using ECommerce.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore.Storage;

namespace ECommerce.Infrastructure;

public class UnitOfWork : IUnitOfWork
{
    private readonly AppDbContext _context;

    // Specialized repositories
    private IProductRepository? _products;
    private IOrderRepository? _orders;
    private IUserRepository? _users;
    private ICategoryRepository? _categories;
    private ICartRepository? _carts;
    private IReviewRepository? _reviews;
    private IWishlistRepository? _wishlists;

    // Generic repositories for simple entities
    private IRepository<OrderItem>? _orderItems;
    private IRepository<CartItem>? _cartItems;
    private IRepository<Address>? _addresses;
    private IRepository<PromoCode>? _promoCodes;
    private IRepository<InventoryLog>? _inventoryLogs;
    private IRepository<ProductImage>? _productImages;

    public UnitOfWork(AppDbContext context)
    {
        _context = context;
    }

    // Specialized repositories
    public IProductRepository Products => _products ??= new ProductRepository(_context);
    public IOrderRepository Orders => _orders ??= new OrderRepository(_context);
    public IUserRepository Users => _users ??= new UserRepository(_context);
    public ICategoryRepository Categories => _categories ??= new CategoryRepository(_context);
    public ICartRepository Carts => _carts ??= new CartRepository(_context);
    public IReviewRepository Reviews => _reviews ??= new ReviewRepository(_context);
    public IWishlistRepository Wishlists => _wishlists ??= new WishlistRepository(_context);

    // Generic repositories
    public IRepository<OrderItem> OrderItems => _orderItems ??= new Repository<OrderItem>(_context);
    public IRepository<CartItem> CartItems => _cartItems ??= new Repository<CartItem>(_context);
    public IRepository<Address> Addresses => _addresses ??= new Repository<Address>(_context);
    public IRepository<PromoCode> PromoCodes => _promoCodes ??= new Repository<PromoCode>(_context);
    public IRepository<InventoryLog> InventoryLogs => _inventoryLogs ??= new Repository<InventoryLog>(_context);
    public IRepository<ProductImage> ProductImages => _productImages ??= new Repository<ProductImage>(_context);

    public async Task<int> SaveChangesAsync()
    {
        return await _context.SaveChangesAsync();
    }

    public async Task<IAsyncTransaction> BeginTransactionAsync()
    {
        var transaction = await _context.Database.BeginTransactionAsync();
        return new AsyncTransaction(transaction);
    }

    public void Dispose()
    {
        _context?.Dispose();
    }

    public ValueTask DisposeAsync()
    {
        return _context?.DisposeAsync() ?? default;
    }

    private class AsyncTransaction : IAsyncTransaction
    {
        private readonly IDbContextTransaction _transaction;

        public AsyncTransaction(IDbContextTransaction transaction)
        {
            _transaction = transaction;
        }

        public async Task CommitAsync() => await _transaction.CommitAsync();
        public async Task RollbackAsync() => await _transaction.RollbackAsync();
        public async ValueTask DisposeAsync() => await _transaction.DisposeAsync();
    }
}
```

---

### Phase 3: Update Services to Use New Pattern

**Goal:** Update all services to use the refactored repository pattern.

#### Step 3.1: Identify Service Changes Required

| Service | Current Pattern | Required Change |
|---------|-----------------|-----------------|
| `ProductService` | Uses `_unitOfWork.Products` | Update to new interface |
| `OrderService` | Uses `_unitOfWork.Orders` | Update to new interface |
| `InventoryService` | Calls `SaveChangesAsync` on repo | Remove, use UoW save |
| `CartService` | Uses `_unitOfWork.Carts` | Change to `Carts` (specialized) |
| `WishlistService` | Uses `_unitOfWork.Wishlists` | Change to `Wishlists` (specialized) |
| `ReviewService` | Uses `_unitOfWork.Reviews` | Change to `Reviews` (specialized) |
| `CategoryService` | Uses `_unitOfWork.Categories` | Change to `Categories` (specialized) |

#### Step 3.2: Example Service Update Pattern

**Before:**
```csharp
public async Task<ProductDto> CreateProductAsync(CreateProductDto dto)
{
    var product = _mapper.Map<Product>(dto);
    await _unitOfWork.Products.AddAsync(product);  // Old generic method
    await _unitOfWork.Products.SaveChangesAsync();  // ❌ Wrong - bypasses UoW
    return _mapper.Map<ProductDto>(product);
}
```

**After:**
```csharp
public async Task<ProductDto> CreateProductAsync(CreateProductDto dto)
{
    var product = _mapper.Map<Product>(dto);
    await _unitOfWork.Products.AddAsync(product);  // Uses specialized repo
    await _unitOfWork.SaveChangesAsync();  // ✅ Correct - through UoW
    return _mapper.Map<ProductDto>(product);
}
```

#### Step 3.3: Add trackChanges for Read Operations

**Before:**
```csharp
public async Task<IEnumerable<ProductDto>> GetAllProductsAsync()
{
    var products = await _unitOfWork.Products.GetAllAsync();  // Tracks changes
    return _mapper.Map<IEnumerable<ProductDto>>(products);
}
```

**After:**
```csharp
public async Task<IEnumerable<ProductDto>> GetAllProductsAsync()
{
    var products = await _unitOfWork.Products.GetAllAsync(trackChanges: false);  // No tracking
    return _mapper.Map<IEnumerable<ProductDto>>(products);
}
```

---

### Phase 4: Update Specialized Repository Interfaces

**Goal:** Ensure all specialized repository interfaces extend from the new IRepository<T>.

#### Files to Update:

| Interface | Changes Needed |
|-----------|----------------|
| `IProductRepository` | Add trackChanges params, remove SaveChanges calls |
| `IOrderRepository` | Add trackChanges params |
| `IUserRepository` | Add trackChanges params |
| `ICategoryRepository` | Add trackChanges params |
| `ICartRepository` | Add trackChanges params |
| `IReviewRepository` | Add trackChanges params |
| `IWishlistRepository` | Add trackChanges params |

---

## Files Summary

### Files to Modify

| File | Phase | Changes |
|------|-------|---------|
| `IRepository.cs` | 1 | Remove SaveChangesAsync, add trackChanges, add FindAll/FindByCondition |
| `Repository.cs` | 1 | Implement new interface, remove SaveChangesAsync |
| `IProductRepository.cs` | 1 | Add trackChanges params, change UpdateStockAsync signature |
| `ProductRepository.cs` | 1 | Remove SaveChangesAsync call, implement trackChanges |
| `IUnitOfWork.cs` | 2 | Remove duplicate generic access, add missing specialized repos |
| `UnitOfWork.cs` | 2 | Update to match new interface |
| `IOrderRepository.cs` | 4 | Add trackChanges params |
| `OrderRepository.cs` | 4 | Implement trackChanges |
| `IUserRepository.cs` | 4 | Add trackChanges params |
| `UserRepository.cs` | 4 | Implement trackChanges |
| `ICategoryRepository.cs` | 4 | Add trackChanges params |
| `CategoryRepository.cs` | 4 | Implement trackChanges |
| `ICartRepository.cs` | 4 | Add trackChanges params |
| `CartRepository.cs` | 4 | Implement trackChanges |
| `IReviewRepository.cs` | 4 | Add trackChanges params |
| `ReviewRepository.cs` | 4 | Implement trackChanges |
| `IWishlistRepository.cs` | 4 | Add trackChanges params |
| `WishlistRepository.cs` | 4 | Implement trackChanges |

### Services to Update (Phase 3)

| Service | Changes |
|---------|---------|
| `ProductService.cs` | Use UoW.SaveChangesAsync, add trackChanges |
| `OrderService.cs` | Use UoW.SaveChangesAsync, add trackChanges |
| `InventoryService.cs` | Use UoW.SaveChangesAsync, add trackChanges |
| `CartService.cs` | Use specialized repo, add trackChanges |
| `WishlistService.cs` | Use specialized repo, add trackChanges |
| `ReviewService.cs` | Use specialized repo, add trackChanges |
| `CategoryService.cs` | Use specialized repo, add trackChanges |
| `AuthService.cs` | Use UoW.SaveChangesAsync, add trackChanges |
| `PaymentService.cs` | Use UoW.SaveChangesAsync |
| `PromoCodeService.cs` | Use UoW.SaveChangesAsync, add trackChanges |

---

## Verification Plan

### Build Verification
```bash
cd src/backend
dotnet build ECommerce.sln
```

### Test Verification
```bash
dotnet test ECommerce.Tests
```

### Manual Testing Checklist

| Endpoint | Test Case | Expected |
|----------|-----------|----------|
| `POST /api/products` | Create product | Product saved via UoW |
| `PUT /api/products/{id}` | Update product | Changes saved atomically |
| `POST /api/orders` | Create order with items | All items saved in one transaction |
| `PUT /api/inventory/adjust` | Adjust stock | Stock updated, rollback on failure |
| `POST /api/cart/items` | Add to cart | Cart updated via specialized repo |

### Transaction Rollback Test

```csharp
// Test that partial failures rollback completely
[TestMethod]
public async Task CreateOrder_WhenPaymentFails_RollsBackCompletely()
{
    // Arrange: Create order that will fail at payment

    // Act: Attempt to create order

    // Assert: No order items in DB, stock unchanged
}
```

---

## Risk Assessment

| Phase | Risk | Impact | Mitigation |
|-------|------|--------|------------|
| Phase 1 | Breaking changes in IRepository | High | Update all implementations simultaneously |
| Phase 2 | Service compile errors | Medium | Systematic service updates |
| Phase 3 | Runtime errors from missing saves | High | Comprehensive testing |
| Phase 4 | Performance regression | Low | Monitor query performance |

### Rollback Strategy

1. **Git branch:** Create `feature/repository-refactoring` branch
2. **Incremental commits:** Commit after each phase
3. **Test at each phase:** Ensure builds pass before proceeding
4. **Revert capability:** Each phase can be reverted independently

---

## Summary

### Changes by Priority

| Priority | Change | Benefit |
|----------|--------|---------|
| 🔴 High | Remove SaveChangesAsync from repos | Atomic transactions |
| 🟡 Medium | Add trackChanges parameter | 20-50% read perf improvement |
| 🟡 Medium | Standardize on specialized repos | Clearer API, less confusion |
| 🟢 Low | Add missing specialized repos to UoW | Complete repository access |

### Metrics

- **Files to modify:** 20+
- **Interfaces changed:** 9
- **Services updated:** 10
- **Test coverage required:** All service methods with DB operations

---

## Appendix: Book Pattern Reference

### RepositoryBase Pattern (from book)

```csharp
public abstract class RepositoryBase<T> : IRepositoryBase<T> where T : class
{
    protected RepositoryContext RepositoryContext;

    public RepositoryBase(RepositoryContext repositoryContext)
        => RepositoryContext = repositoryContext;

    public IQueryable<T> FindAll(bool trackChanges) =>
        !trackChanges
            ? RepositoryContext.Set<T>().AsNoTracking()
            : RepositoryContext.Set<T>();

    public IQueryable<T> FindByCondition(Expression<Func<T, bool>> expression, bool trackChanges) =>
        !trackChanges
            ? RepositoryContext.Set<T>().Where(expression).AsNoTracking()
            : RepositoryContext.Set<T>().Where(expression);

    public void Create(T entity) => RepositoryContext.Set<T>().Add(entity);
    public void Update(T entity) => RepositoryContext.Set<T>().Update(entity);
    public void Delete(T entity) => RepositoryContext.Set<T>().Remove(entity);
}
```

### RepositoryManager Pattern (from book)

```csharp
public sealed class RepositoryManager : IRepositoryManager
{
    private readonly RepositoryContext _repositoryContext;
    private readonly Lazy<ICompanyRepository> _companyRepository;
    private readonly Lazy<IEmployeeRepository> _employeeRepository;

    public RepositoryManager(RepositoryContext repositoryContext)
    {
        _repositoryContext = repositoryContext;
        _companyRepository = new Lazy<ICompanyRepository>(() =>
            new CompanyRepository(repositoryContext));
        _employeeRepository = new Lazy<IEmployeeRepository>(() =>
            new EmployeeRepository(repositoryContext));
    }

    public ICompanyRepository Company => _companyRepository.Value;
    public IEmployeeRepository Employee => _employeeRepository.Value;
    public void Save() => _repositoryContext.SaveChanges();
}
```

### Key Principle

> "We have the Create, Update, and Delete methods in the RepositoryBase class, but they won't make any change in the database until we call the SaveChanges method. Our repository manager class will handle that as well."
>
> — Ultimate ASP.NET Core Web API, Chapter 3.7
