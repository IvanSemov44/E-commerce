# Phase 4: Shopping Bounded Context

**Prerequisite**: Phase 3 complete.

**Learn**: Aggregate with child entities, transaction boundaries, idempotent domain methods, cross-context ID validation.

---

## What's New in This Phase

The Cart aggregate is the first example of an aggregate with **business-meaningful child entities** that you actively mutate. In Phase 1, `ProductImage` children were simpler (add, set primary). Cart is more complex:

- `AddItem` is **idempotent**: adding a product already in the cart increases quantity, it doesn't add a duplicate
- `UpdateQuantity` must enforce minimum/maximum bounds
- The cart as a whole enforces its own consistency: max items, no duplicates
- The aggregate root is the gatekeeper for ALL child mutations

---

## Old Service → New Handler Mapping

| Old Method | New Handler |
|-----------|-------------|
| `CartService.GetCartAsync(userId)` | `GetCartQuery` |
| `CartService.AddItemAsync(userId, productId, qty)` | `AddToCartCommand` |
| `CartService.RemoveItemAsync(userId, cartItemId)` | `RemoveFromCartCommand` |
| `CartService.UpdateQuantityAsync(userId, cartItemId, qty)` | `UpdateCartItemQuantityCommand` |
| `CartService.ClearCartAsync(userId)` | `ClearCartCommand` |
| `WishlistService.GetWishlistAsync(userId)` | `GetWishlistQuery` |
| `WishlistService.AddToWishlistAsync(userId, productId)` | `AddToWishlistCommand` |
| `WishlistService.RemoveFromWishlistAsync(userId, productId)` | `RemoveFromWishlistCommand` |

---

## Step 1: Domain Project

### Cart aggregate

```csharp
// Aggregates/Cart/Cart.cs
public class Cart : AggregateRoot
{
    public Guid UserId { get; private set; }

    private readonly List<CartItem> _items = new();
    public IReadOnlyCollection<CartItem> Items => _items.AsReadOnly();

    public int ItemCount => _items.Sum(i => i.Quantity);
    public bool IsEmpty => _items.Count == 0;

    private Cart() { }

    public static Cart Create(Guid userId)
    {
        return new Cart
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
    }

    public void AddItem(Guid productId, int quantity, decimal unitPrice, string currency)
    {
        if (quantity <= 0)
            throw new ShoppingDomainException("QUANTITY_INVALID", "Quantity must be positive.");

        // Idempotency: if product already in cart, increase quantity
        var existing = _items.FirstOrDefault(i => i.ProductId == productId);
        if (existing is not null)
        {
            existing.IncreaseQuantity(quantity);
            AddDomainEvent(new CartItemQuantityUpdatedEvent(Id, productId, existing.Quantity));
            return;
        }

        if (_items.Count >= 50)
            throw new ShoppingDomainException("CART_FULL", "Cart cannot contain more than 50 distinct items.");

        _items.Add(CartItem.Create(Guid.NewGuid(), Id, productId, quantity, unitPrice, currency));
        AddDomainEvent(new ItemAddedToCartEvent(Id, productId, quantity));
    }

    public void UpdateItemQuantity(Guid cartItemId, int newQuantity)
    {
        if (newQuantity <= 0)
            throw new ShoppingDomainException("QUANTITY_INVALID", "Quantity must be positive.");

        var item = _items.FirstOrDefault(i => i.Id == cartItemId)
            ?? throw new ShoppingDomainException("CART_ITEM_NOT_FOUND", "Cart item not found.");

        item.SetQuantity(newQuantity);
        AddDomainEvent(new CartItemQuantityUpdatedEvent(Id, item.ProductId, newQuantity));
    }

    public void RemoveItem(Guid cartItemId)
    {
        var item = _items.FirstOrDefault(i => i.Id == cartItemId)
            ?? throw new ShoppingDomainException("CART_ITEM_NOT_FOUND", "Cart item not found.");

        _items.Remove(item);
    }

    public void Clear()
    {
        _items.Clear();
        AddDomainEvent(new CartClearedEvent(Id, UserId));
    }
}
```

### CartItem child entity

```csharp
// Aggregates/Cart/CartItem.cs
public class CartItem : Entity
{
    public Guid CartId { get; private set; }
    public Guid ProductId { get; private set; }   // ID only — no navigation to Product
    public int Quantity { get; private set; }
    public decimal UnitPrice { get; private set; } // Snapshot of price at add time
    public string Currency { get; private set; } = null!;

    private CartItem() { }

    internal static CartItem Create(Guid id, Guid cartId, Guid productId, int quantity, decimal unitPrice, string currency)
    {
        return new CartItem
        {
            Id = id,
            CartId = cartId,
            ProductId = productId,
            Quantity = quantity,
            UnitPrice = unitPrice,
            Currency = currency,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
    }

    internal void IncreaseQuantity(int amount) => Quantity += amount;
    internal void SetQuantity(int quantity) => Quantity = quantity;
}
```

**`UnitPrice` is a snapshot**: When a product's price changes in Catalog, the cart item retains the price from when it was added. This prevents surprising total changes mid-session. The application can choose to refresh prices on cart load (a query concern) — but the domain stores the snapshot.

### AddToCartCommand — cross-context product validation

The Cart aggregate cannot verify that `ProductId` exists in the Catalog. Rule 7: no service injection in aggregates. The handler must validate:

```csharp
// Commands/AddToCart/AddToCartCommandHandler.cs
public class AddToCartCommandHandler : IRequestHandler<AddToCartCommand, Result<CartDto>>
{
    private readonly ICartRepository _carts;
    private readonly AppDbContext _db;   // for cross-context product lookup (same DB during migration)
    private readonly ICurrentUserService _currentUser;
    private readonly IUnitOfWork _uow;

    public async Task<Result<CartDto>> Handle(AddToCartCommand command, CancellationToken ct)
    {
        var userId = _currentUser.UserId
            ?? return Result<CartDto>.Unauthorized();

        // Cross-context validation: product must exist
        // During Phases 1-7 we use the shared DB. In Phase 8 this becomes an API call.
        var product = await _db.Products.AsNoTracking()
            .Where(p => p.Id == command.ProductId && !p.IsDeleted && p.Status == "Active")
            .Select(p => new { p.Id, Price = p.Price.Amount, Currency = p.Price.Currency })
            .FirstOrDefaultAsync(ct);

        if (product is null)
            return Result<CartDto>.Fail(ErrorCodes.Shopping.ProductNotFound, "Product not found or inactive.");

        // Load or create cart
        var cart = await _carts.GetByUserIdAsync(userId, ct)
            ?? Cart.Create(userId);

        cart.AddItem(command.ProductId, command.Quantity, product.Price, product.Currency);

        await _carts.UpsertAsync(cart, ct);
        await _uow.SaveChangesAsync(ct);

        return Result<CartDto>.Ok(cart.ToDto());
    }
}
```

**The `// TODO Phase 8` comment** is important: cross-context queries via shared DB is acceptable during migration. Document it explicitly so it's not cargo-culted into a microservices world.

---

## Step 2: Wishlist aggregate

Wishlist is simpler — it's just a list of product IDs for a user. No prices, no quantities:

```csharp
public class Wishlist : AggregateRoot
{
    public Guid UserId { get; private set; }

    private readonly List<Guid> _productIds = new();  // Just IDs, not child entities
    public IReadOnlyCollection<Guid> ProductIds => _productIds.AsReadOnly();

    private Wishlist() { }

    public static Wishlist Create(Guid userId) => new()
    {
        Id = Guid.NewGuid(),
        UserId = userId,
        CreatedAt = DateTime.UtcNow,
        UpdatedAt = DateTime.UtcNow
    };

    public void AddProduct(Guid productId)
    {
        if (_productIds.Contains(productId)) return;  // idempotent
        if (_productIds.Count >= 100)
            throw new ShoppingDomainException("WISHLIST_FULL", "Wishlist cannot exceed 100 items.");
        _productIds.Add(productId);
    }

    public void RemoveProduct(Guid productId)
    {
        _productIds.Remove(productId);  // no-op if not present
    }
}
```

**Why are WishlistItems just GUIDs, not child entities?** They have no properties beyond the ID — there's nothing to track about them. Using a `List<Guid>` directly is the simplest correct model.

**EF Core for the Guid list**: EF Core 8+ supports `primitive collections` natively. For EF 7 and below, create a `WishlistItem` EF entity purely for persistence (infrastructure concern, not domain):

```csharp
// In WishlistConfiguration.cs:
builder.OwnsMany(w => w.ProductIds, id =>  // EF 8: primitive collection
{
    id.ToTable("WishlistItems");
    id.Property<Guid>("ProductId").IsRequired();
});
```

---

## Event Handlers from Other Contexts

When an order is placed, the cart must be cleared. This handler lives in Shopping.Application:

```csharp
// EventHandlers/ClearCartOnOrderPlacedHandler.cs
// Stub — implement fully in Phase 7 when OrderPlacedEvent exists
// public class ClearCartOnOrderPlacedHandler : INotificationHandler<OrderPlacedEvent>
// {
//     // Load cart by UserId from event, call cart.Clear(), save
// }
```

**Tester handoff after Step 2:** Once both aggregates (`Cart` and `Wishlist`) and their handlers are delivered, the tester writes domain unit tests and handler unit tests in `ECommerce.Shopping.Tests/`. See `.ai/plans/ddd-cqrs-migration/testing/tester-prompt-template.md` → Prompt 2 (domain) and Prompt 3 (handlers).

---

## Definition of Done

Full testing guide: `.ai/plans/ddd-cqrs-migration/testing/README.md`

**Characterization (integration — slow):**
- [ ] Characterization tests written and PASSING against OLD service (before any migration)
- [ ] Characterization tests still PASSING after cutover to new handlers

**Domain unit tests (fast — written after Step 2):**
- [ ] `ECommerce.Shopping.Tests/Domain/CartTests.cs` written and PASSING
- [ ] `ECommerce.Shopping.Tests/Domain/WishlistTests.cs` written and PASSING
- Covers: AddItem idempotency, quantity limits, CartItem snapshot price, Wishlist no-op remove

**Handler unit tests (fast — written after handlers are delivered):**
- [ ] `ECommerce.Shopping.Tests/Handlers/` tests written and PASSING
- Covers: AddToCartCommand, RemoveFromCartCommand, AddToWishlistCommand orchestration

**Code:**
- [ ] `Cart` aggregate with `AddItem` (idempotent), `UpdateItemQuantity`, `RemoveItem`, `Clear`
- [ ] `CartItem` as child entity with snapshot unit price
- [ ] `Wishlist` aggregate with idempotent `AddProduct` and no-op `RemoveProduct`
- [ ] Cross-context product existence check in handler (not in aggregate)
- [ ] Event stubs for `ClearCartOnOrderPlacedHandler` with `// Phase 7` comment
- [ ] Old `CartService` and `WishlistService` deleted after tests pass

## What You Learned in Phase 4

- Aggregate methods should be idempotent where business logic allows (adding duplicate → increase qty)
- Child entities are created only through the aggregate root, with `internal` constructors
- The aggregate enforces consistency boundaries (max items, no duplicates) — not the handler
- Cross-context data access uses shared DB during migration and must be commented as such
- When a domain concept is just an ID with no properties, model it as a primitive, not an entity
