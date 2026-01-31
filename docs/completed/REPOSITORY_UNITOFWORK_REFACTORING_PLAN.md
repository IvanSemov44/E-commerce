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
```
