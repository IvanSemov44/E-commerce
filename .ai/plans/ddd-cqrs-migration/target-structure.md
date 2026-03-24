# Target Project & Folder Structure

**This is the final state after all phases are complete.**

---

## Solution Structure

```
src/backend/
│
├── ECommerce.SharedKernel/                    # Phase 0 — DDD building blocks
│   ├── ECommerce.SharedKernel.csproj
│   ├── Domain/
│   │   ├── Entity.cs                          # Base entity with Id, CreatedAt, UpdatedAt
│   │   ├── AggregateRoot.cs                   # Entity + domain events collection
│   │   ├── ValueObject.cs                     # Base for value objects (equality by value)
│   │   ├── IDomainEvent.cs                    # Marker interface for domain events
│   │   ├── IDomainEventDispatcher.cs          # Dispatches events after save
│   │   └── DomainException.cs                 # Base exception for domain rule violations
│   ├── Results/
│   │   ├── Result.cs                          # Moved from Core (used by all contexts)
│   │   └── Unit.cs                            # Void return type
│   ├── Constants/
│   │   └── ErrorCodes.cs                      # Shared error codes (or per-context later)
│   └── Interfaces/
│       └── IUnitOfWork.cs                     # Base unit of work interface
│
├── Catalog/                                   # Phase 1
│   ├── ECommerce.Catalog.Domain/
│   │   ├── ECommerce.Catalog.Domain.csproj    # References: SharedKernel only
│   │   ├── Aggregates/
│   │   │   ├── Product/
│   │   │   │   ├── Product.cs                 # Aggregate root
│   │   │   │   ├── ProductImage.cs            # Child entity
│   │   │   │   └── Events/
│   │   │   │       ├── ProductCreatedEvent.cs
│   │   │   │       ├── ProductPriceChangedEvent.cs
│   │   │   │       └── ProductDeactivatedEvent.cs
│   │   │   └── Category/
│   │   │       ├── Category.cs                # Aggregate root
│   │   │       └── Events/
│   │   │           └── CategoryCreatedEvent.cs
│   │   ├── ValueObjects/
│   │   │   ├── ProductName.cs
│   │   │   ├── Slug.cs
│   │   │   ├── Money.cs
│   │   │   ├── Sku.cs
│   │   │   ├── Barcode.cs
│   │   │   └── Weight.cs
│   │   ├── Exceptions/
│   │   │   └── CatalogDomainException.cs
│   │   └── Interfaces/
│   │       ├── IProductRepository.cs
│   │       └── ICategoryRepository.cs
│   │
│   ├── ECommerce.Catalog.Application/
│   │   ├── ECommerce.Catalog.Application.csproj  # References: Catalog.Domain, SharedKernel
│   │   ├── Commands/
│   │   │   ├── CreateProduct/
│   │   │   │   ├── CreateProductCommand.cs
│   │   │   │   ├── CreateProductCommandHandler.cs
│   │   │   │   └── CreateProductCommandValidator.cs
│   │   │   ├── UpdateProduct/
│   │   │   │   ├── UpdateProductCommand.cs
│   │   │   │   ├── UpdateProductCommandHandler.cs
│   │   │   │   └── UpdateProductCommandValidator.cs
│   │   │   ├── DeleteProduct/
│   │   │   │   └── ...
│   │   │   ├── CreateCategory/
│   │   │   │   └── ...
│   │   │   ├── UpdateCategory/
│   │   │   │   └── ...
│   │   │   └── DeleteCategory/
│   │   │       └── ...
│   │   ├── Queries/
│   │   │   ├── GetProducts/
│   │   │   │   ├── GetProductsQuery.cs
│   │   │   │   └── GetProductsQueryHandler.cs
│   │   │   ├── GetProductById/
│   │   │   │   └── ...
│   │   │   ├── GetProductBySlug/
│   │   │   │   └── ...
│   │   │   ├── GetFeaturedProducts/
│   │   │   │   └── ...
│   │   │   ├── GetCategories/
│   │   │   │   └── ...
│   │   │   ├── GetCategoryById/
│   │   │   │   └── ...
│   │   │   └── GetCategoryBySlug/
│   │   │       └── ...
│   │   ├── DTOs/
│   │   │   ├── ProductDto.cs
│   │   │   ├── ProductDetailDto.cs
│   │   │   ├── CategoryDto.cs
│   │   │   └── CategoryDetailDto.cs
│   │   └── EventHandlers/
│   │       └── (domain event handlers specific to Catalog)
│   │
│   └── ECommerce.Catalog.Infrastructure/
│       ├── ECommerce.Catalog.Infrastructure.csproj  # References: Catalog.Domain, Catalog.Application
│       ├── Repositories/
│       │   ├── ProductRepository.cs
│       │   └── CategoryRepository.cs
│       ├── Configurations/
│       │   ├── ProductConfiguration.cs
│       │   ├── ProductImageConfiguration.cs
│       │   └── CategoryConfiguration.cs
│       └── ReadModels/
│           └── ProductReadRepository.cs   # Optimized query-side reads
│
├── Identity/                              # Phase 2
│   ├── ECommerce.Identity.Domain/
│   │   ├── Aggregates/
│   │   │   └── User/
│   │   │       ├── User.cs
│   │   │       ├── Address.cs
│   │   │       ├── RefreshToken.cs
│   │   │       └── Events/
│   │   ├── ValueObjects/
│   │   │   ├── Email.cs
│   │   │   ├── PersonName.cs
│   │   │   ├── PhoneNumber.cs
│   │   │   └── Password.cs
│   │   ├── Exceptions/
│   │   └── Interfaces/
│   │
│   ├── ECommerce.Identity.Application/
│   │   ├── Commands/  (Register, Login, RefreshToken, ForgotPassword, ResetPassword, VerifyEmail, UpdateProfile, DeleteAccount)
│   │   ├── Queries/   (GetUser, GetUserById)
│   │   ├── DTOs/
│   │   └── EventHandlers/
│   │
│   └── ECommerce.Identity.Infrastructure/
│       ├── Repositories/
│       ├── Configurations/
│       └── Services/
│           └── JwtTokenService.cs     # Infrastructure concern, not domain
│
├── Inventory/                             # Phase 3
│   ├── ECommerce.Inventory.Domain/
│   │   ├── Aggregates/
│   │   │   └── InventoryItem/
│   │   │       ├── InventoryItem.cs    # New aggregate! References ProductId
│   │   │       ├── InventoryLog.cs     # Child entity
│   │   │       └── Events/
│   │   ├── ValueObjects/
│   │   │   ├── StockLevel.cs
│   │   │   └── Quantity.cs
│   │   ├── Exceptions/
│   │   └── Interfaces/
│   │
│   ├── ECommerce.Inventory.Application/
│   │   ├── Commands/  (ReduceStock, IncreaseStock, AdjustStock, BulkUpdate)
│   │   ├── Queries/   (GetInventory, GetLowStock)
│   │   ├── DTOs/
│   │   └── EventHandlers/
│   │       └── ReduceStockOnOrderPlacedHandler.cs  # Listens to OrderPlacedEvent
│   │
│   └── ECommerce.Inventory.Infrastructure/
│
├── Shopping/                              # Phase 4
│   ├── ECommerce.Shopping.Domain/
│   │   ├── Aggregates/
│   │   │   ├── Cart/
│   │   │   │   ├── Cart.cs
│   │   │   │   ├── CartItem.cs
│   │   │   │   └── Events/
│   │   │   └── Wishlist/
│   │   │       ├── Wishlist.cs
│   │   │       └── Events/
│   │   ├── ValueObjects/
│   │   │   └── Quantity.cs             # May share with SharedKernel
│   │   ├── Exceptions/
│   │   └── Interfaces/
│   │
│   ├── ECommerce.Shopping.Application/
│   │   ├── Commands/  (AddToCart, RemoveFromCart, UpdateCartItem, ClearCart, AddToWishlist, RemoveFromWishlist)
│   │   ├── Queries/   (GetCart, GetWishlist)
│   │   ├── DTOs/
│   │   └── EventHandlers/
│   │       └── ClearCartOnOrderPlacedHandler.cs
│   │
│   └── ECommerce.Shopping.Infrastructure/
│
├── Promotions/                            # Phase 5
│   ├── ECommerce.Promotions.Domain/
│   │   ├── Aggregates/
│   │   │   └── PromoCode/
│   │   ├── ValueObjects/
│   │   │   ├── DiscountValue.cs
│   │   │   ├── DateRange.cs
│   │   │   └── PromoCodeString.cs
│   │   ├── Services/
│   │   │   └── DiscountCalculator.cs   # Domain service
│   │   ├── Exceptions/
│   │   └── Interfaces/
│   │
│   ├── ECommerce.Promotions.Application/
│   └── ECommerce.Promotions.Infrastructure/
│
├── Reviews/                               # Phase 6
│   ├── ECommerce.Reviews.Domain/
│   │   ├── Aggregates/
│   │   │   └── Review/
│   │   ├── ValueObjects/
│   │   │   ├── Rating.cs
│   │   │   └── ReviewContent.cs
│   │   ├── Exceptions/
│   │   └── Interfaces/
│   │
│   ├── ECommerce.Reviews.Application/
│   └── ECommerce.Reviews.Infrastructure/
│
├── Ordering/                              # Phase 7
│   ├── ECommerce.Ordering.Domain/
│   │   ├── Aggregates/
│   │   │   └── Order/
│   │   │       ├── Order.cs             # State machine for status transitions
│   │   │       ├── OrderItem.cs
│   │   │       └── Events/
│   │   │           ├── OrderPlacedEvent.cs
│   │   │           ├── OrderConfirmedEvent.cs
│   │   │           ├── OrderShippedEvent.cs
│   │   │           ├── OrderDeliveredEvent.cs
│   │   │           └── OrderCancelledEvent.cs
│   │   ├── ValueObjects/
│   │   │   ├── OrderNumber.cs
│   │   │   ├── Money.cs               # May share via SharedKernel
│   │   │   ├── OrderStatus.cs          # Value object with transition logic
│   │   │   ├── PaymentInfo.cs
│   │   │   └── ShippingAddress.cs      # Snapshot value object (not entity)
│   │   ├── Services/
│   │   │   └── OrderTotalCalculator.cs  # Domain service
│   │   ├── Exceptions/
│   │   └── Interfaces/
│   │
│   ├── ECommerce.Ordering.Application/
│   │   ├── Commands/  (PlaceOrder, ConfirmOrder, ShipOrder, DeliverOrder, CancelOrder)
│   │   ├── Queries/   (GetOrders, GetOrderById, GetUserOrders)
│   │   ├── DTOs/
│   │   └── EventHandlers/
│   │       ├── SendConfirmationOnOrderPlacedHandler.cs
│   │       └── (other cross-context event handlers)
│   │
│   └── ECommerce.Ordering.Infrastructure/
│
├── ECommerce.API/                         # Stays (updated incrementally)
│   ├── Controllers/                       # Updated to use MediatR
│   ├── Behaviors/                         # MediatR pipeline behaviors
│   │   ├── LoggingBehavior.cs
│   │   ├── ValidationBehavior.cs
│   │   ├── TransactionBehavior.cs
│   │   └── PerformanceBehavior.cs
│   └── Program.cs                         # Registers all contexts
│
├── ECommerce.Core/                        # SHRINKS over time → eventually deleted
│   └── (entities/interfaces not yet migrated)
│
├── ECommerce.Application/                 # SHRINKS over time → eventually deleted
│   └── (services/DTOs not yet migrated)
│
└── ECommerce.Infrastructure/              # SHRINKS over time → eventually deleted
    ├── Data/
    │   └── AppDbContext.cs                # Shared DbContext (Phase 8 splits this)
    └── (repos/configs not yet migrated)
```

---

## Project Dependency Graph

```
ECommerce.SharedKernel          ← depends on NOTHING
        ↑
        │ referenced by all Domain projects
        │
ECommerce.{Context}.Domain      ← depends on SharedKernel only
        ↑
        │
ECommerce.{Context}.Application ← depends on {Context}.Domain, SharedKernel
        ↑
        │
ECommerce.{Context}.Infrastructure ← depends on {Context}.Domain, {Context}.Application
        ↑
        │
ECommerce.API                   ← depends on all Application + Infrastructure projects
```

**Critical rule**: Domain projects NEVER reference Infrastructure, API, or EF Core. They are pure C#.

---

## NuGet Package Distribution

| Project | Packages |
|---------|----------|
| SharedKernel | None (pure C#) |
| {Context}.Domain | None (pure C#) |
| {Context}.Application | MediatR, FluentValidation, AutoMapper (optional) |
| {Context}.Infrastructure | EF Core, Npgsql |
| API | MediatR, FluentValidation, all Infrastructure projects |

---

## DbContext Strategy

**During migration (Phases 1-7)**: One shared `AppDbContext` in `ECommerce.Infrastructure`. All context Infrastructure projects register their EF configurations into this shared context.

**After migration (Phase 8)**: Each bounded context gets its own `DbContext` that only knows about its own entities. The shared `AppDbContext` is deleted.

```csharp
// Phase 1-7: Shared context, configurations from all bounded contexts
public class AppDbContext : DbContext
{
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Applies configurations from ALL infrastructure assemblies
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ProductConfiguration).Assembly);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(UserConfiguration).Assembly);
        // ...
    }
}

// Phase 8: Each context has its own
public class CatalogDbContext : DbContext { /* only Product, Category, ProductImage */ }
public class OrderingDbContext : DbContext { /* only Order, OrderItem */ }
```
