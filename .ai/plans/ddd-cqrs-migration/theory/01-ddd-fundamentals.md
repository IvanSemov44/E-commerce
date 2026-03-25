# DDD Fundamentals

**Read this before any implementation.**

---

## What is Domain-Driven Design?

DDD is a software design approach that says: **the most important part of your code is the business logic, and the code should mirror how the business thinks and talks**.

Right now in our codebase, if you ask "where is the rule that an order can't be cancelled after it's shipped?", the answer is: somewhere in `OrderService.cs`, buried between database calls and DTO mappings. In DDD, the answer is: `Order.Cancel()` throws because the Order *knows* its own rules.

DDD has two levels:
1. **Strategic DDD** — the big picture (what are the boundaries of our system?)
2. **Tactical DDD** — the code patterns (how do we write the domain model?)

---

## Strategic DDD

### Ubiquitous Language

The team (developers, product owners, domain experts) agrees on a shared vocabulary. If the business says "order", the code has a class called `Order`. If the business says "place an order", the code has a method called `PlaceOrder()`. No translation layer between business speak and code.

**In our codebase**: We already do this mostly. Product, Order, Cart, Category — these match business concepts. But we'll be more intentional about it.

### Bounded Contexts

A **Bounded Context** is a boundary within which a particular model (set of terms and rules) applies. The same word can mean different things in different contexts.

**Example**: "Product" means different things to different parts of the business:
- **Catalog Context**: Product has name, description, images, price, category — it's about *displaying* products
- **Inventory Context**: Product has stock quantity, reorder level, warehouse location — it's about *managing stock*
- **Ordering Context**: Product is just a line item with name, price, quantity — it's a *snapshot* at order time

In our current code, `Product` is ONE entity that serves ALL these purposes. That's why it has 20+ properties. DDD says: break this into separate models per context, each with only what it needs.

### Context Mapping

Bounded contexts don't live in isolation — they need to communicate. The **Context Map** describes how contexts relate:

- **Shared Kernel**: Two contexts share some code (our SharedKernel project with base classes)
- **Customer-Supplier**: One context provides data another needs (Catalog supplies product info to Ordering)
- **Anti-Corruption Layer (ACL)**: A translation layer that prevents one context's model from leaking into another

See `context-map.md` for our specific context map.

---

## Tactical DDD

These are the code-level building blocks. Each one has a specific role.

### Entity

An object with a **unique identity** that persists over time. Two entities are equal if they have the same ID, even if all other properties differ.

```csharp
// Entity: identity matters
var user1 = new User(id: Guid.Parse("abc..."));
var user2 = new User(id: Guid.Parse("abc..."));
// user1 == user2 → true (same identity)
```

**In our codebase**: All our current entities inherit `BaseEntity` with a `Guid Id`. They ARE entities. But they're *anemic* — they have no behavior, just properties.

**In DDD**: Entities contain behavior. A `CartItem` entity doesn't just have `Quantity` — it has `IncreaseQuantity(int amount)` that validates the amount is positive.

### Value Object

An object with **no identity** — defined entirely by its attributes. Two value objects are equal if all their properties are equal. They are **immutable** (once created, never changed — you create a new one instead).

```csharp
// Value Object: value matters, not identity
var price1 = new Money(100, "USD");
var price2 = new Money(100, "USD");
// price1 == price2 → true (same value)

// Immutable: changing creates a new object
var discounted = price1.Apply(discount); // returns NEW Money object
```

**Why Value Objects matter**: They eliminate primitive obsession. Instead of `decimal Price` (which could be negative, have wrong currency, etc.), you have `Money Price` which *guarantees* valid state. You can never accidentally set a price to -50 because `Money` won't allow it.

**Value Objects we'll create**:
- `Money` (amount + currency, always positive)
- `Email` (validated email format)
- `Slug` (URL-safe string, validated)
- `PersonName` (first + last name)
- `PhoneNumber` (validated format)
- `Quantity` (positive integer)
- `Rating` (1-5 integer)
- `Sku` (stock keeping unit format)
- `OrderNumber` (formatted order identifier)
- `DateRange` (start + end, start <= end)

### Aggregate

An **Aggregate** is a cluster of entities and value objects that are treated as a single unit for data changes. Every aggregate has exactly one **Aggregate Root** — the entry point.

**Rules**:
1. External code can ONLY reference the aggregate through its root
2. The root enforces all invariants (business rules) for the entire aggregate
3. One transaction = one aggregate (you save one aggregate at a time)
4. Other aggregates are referenced by ID only, never by navigation property

```
┌─────────────────────────────┐
│  Product Aggregate          │
│                             │
│  ┌───────────────────────┐  │
│  │ Product (Root)        │  │  ← External code talks to this
│  │  - Name               │  │
│  │  - Price (Money VO)   │  │
│  │  - Slug (Slug VO)     │  │
│  │  + AddImage()         │  │  ← Root controls children
│  │  + UpdatePrice()      │  │
│  │  + Activate()         │  │
│  └───────────┬───────────┘  │
│              │ owns          │
│  ┌───────────┴───────────┐  │
│  │ ProductImage (Entity) │  │  ← Can't be accessed directly
│  │  - Url                │  │
│  │  - AltText            │  │
│  │  - IsPrimary          │  │
│  └───────────────────────┘  │
│                             │
└─────────────────────────────┘
```

**Why aggregates matter**: They define consistency boundaries. Everything inside an aggregate is always consistent. The Product root guarantees that there's always exactly one primary image. You can't bypass this by directly modifying a ProductImage — you go through Product.

**In our codebase**: Currently, any service can load and modify any entity independently. CartService can modify CartItems directly. In DDD, Cart is the aggregate root, and you MUST go through `cart.AddItem()`, `cart.RemoveItem()`. The Cart enforces: no duplicate products, quantity > 0, etc.

### Aggregate Root

The top-level entity of an aggregate. It is the ONLY entity that:
- Has a repository (one repo per aggregate root)
- Is referenced from outside the aggregate
- Commits changes (via Unit of Work)
- Raises domain events

```csharp
public class Product : AggregateRoot
{
    private readonly List<ProductImage> _images = new();
    public IReadOnlyCollection<ProductImage> Images => _images.AsReadOnly();

    public Result AddImage(string url, string altText)
    {
        // Root enforces invariant: max 10 images
        // NOTE: In this project we return Result<T>, not throw — see rules.md Rule 9.
        // The throw is shown here for conceptual clarity.
        if (_images.Count >= 10)
            return Result.Fail(CatalogErrors.ProductMaxImages);

        var image = new ProductImage(url, altText);

        // Root enforces invariant: first image is always primary
        if (_images.Count == 0)
            image.SetAsPrimary();

        _images.Add(image);
    }
}
```

### Domain Event

A notification that something meaningful happened in the domain. Named in **past tense** because they represent facts that already occurred.

```csharp
public record OrderPlacedEvent(Guid OrderId, Guid? UserId, decimal TotalAmount) : IDomainEvent;
public record StockReducedEvent(Guid ProductId, int NewQuantity) : IDomainEvent;
public record LowStockDetectedEvent(Guid ProductId, int CurrentStock, int Threshold) : IDomainEvent;
```

**How they work**:
1. An aggregate does something → raises an event internally
2. After saving, a dispatcher publishes the event
3. Event handlers react (send email, update another aggregate, log, etc.)

**Why domain events matter**: They decouple aggregates. Currently, `OrderService.CreateOrderAsync()` directly calls `InventoryService`, `EmailService`, and clears the cart. With domain events:
- Order raises `OrderPlacedEvent`
- `ReduceInventoryHandler` listens → reduces stock
- `SendConfirmationEmailHandler` listens → sends email
- `ClearCartHandler` listens → clears cart

Order knows NOTHING about inventory, email, or carts. Each handler can be tested independently.

### Domain Service

A service that contains business logic that doesn't naturally belong to any single aggregate. It operates on multiple aggregates or requires external information.

```csharp
// This logic doesn't belong to PromoCode or Order alone
public class DiscountCalculator : IDomainService
{
    public Money CalculateDiscount(PromoCode code, Money subtotal)
    {
        return code.DiscountType switch
        {
            DiscountType.Percentage => subtotal.MultiplyBy(code.DiscountValue / 100m),
            DiscountType.Fixed => Money.Create(code.DiscountValue, subtotal.Currency),
        };
    }
}
```

**Rule**: If logic belongs to ONE aggregate, put it in the aggregate. Only use Domain Services for cross-aggregate logic.

### Repository (in DDD)

In DDD, repositories are defined in the **domain layer** (as interfaces) and serve one purpose: load and save aggregates. One repository per aggregate root.

This is different from our current generic repositories. In DDD:
- `IProductRepository.GetByIdAsync(ProductId id)` returns the FULL aggregate (Product + Images)
- You don't have a separate `IProductImageRepository`
- The repository always returns the aggregate in a valid state

---

## Anemic Model vs Rich Model

### Anemic (what we have now)
```csharp
// Entity is a data bag
public class Product : BaseEntity
{
    public string Name { get; set; }
    public decimal Price { get; set; }  // Could be negative!
    public int StockQuantity { get; set; }  // Could be negative!
}

// Service has all the logic
public class ProductService
{
    public Result<ProductDto> UpdatePrice(Guid id, decimal newPrice)
    {
        if (newPrice <= 0)
            return Result<ProductDto>.Fail("INVALID", "Price must be positive");
        var product = await _unitOfWork.Products.GetByIdAsync(id);
        product.Price = newPrice;  // Directly set — no encapsulation
        await _unitOfWork.SaveChangesAsync();
    }
}
```

**Problem**: Anyone can set `product.Price = -50` anywhere in the code. The entity can't protect itself. Validation is scattered across services. Business rules are far from the data they protect.

### Rich Model (what we're building)
```csharp
// Entity enforces its own rules
public class Product : AggregateRoot
{
    public Money Price { get; private set; }  // Value Object, always valid
    public StockLevel Stock { get; private set; }  // Value Object, always valid

    public void UpdatePrice(Money newPrice)
    {
        if (newPrice.Amount <= 0)
            throw new DomainException("Price must be positive");

        var oldPrice = Price;
        Price = newPrice;

        if (newPrice < oldPrice)
            AddDomainEvent(new ProductPriceReducedEvent(Id, oldPrice, newPrice));
    }
}
```

**Benefit**: It's IMPOSSIBLE to create a Product with an invalid price. The domain model is always in a valid state. Business rules are right next to the data they protect. Services become thin coordinators.

---

## How This Fits Together

```
    User Request
         │
         ▼
    ┌─────────┐
    │   API    │  ← Thin controller, dispatches command/query
    └────┬────┘
         │
         ▼
    ┌──────────────┐
    │  Application  │  ← Command/Query Handlers (orchestration only)
    │  (MediatR)    │     Load aggregate, call domain methods, save
    └────┬────┘
         │
         ▼
    ┌─────────┐
    │  Domain  │  ← Rich aggregates, value objects, domain events
    │          │     ALL business rules live here
    └────┬────┘
         │
         ▼
    ┌──────────────┐
    │Infrastructure│  ← Repositories, DbContext, external services
    └──────────────┘
```

The key insight: **the Domain layer depends on NOTHING**. It has no references to EF Core, MediatR, or any framework. It's pure C# with pure business logic. This makes it trivially testable.
