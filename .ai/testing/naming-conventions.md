# Naming Conventions

One standard. Applied everywhere. No exceptions.

---

## Method Names

**Pattern:** `Subject_Scenario_ExpectedOutcome`

The subject is what is being called. The scenario is the specific input or state. The outcome is what should happen.

```
// Domain / Application
Create_EmptyName_ReturnsFailure
Create_ValidInputs_RaisesProductCreatedEvent
Handle_ProductNotFound_ReturnsNotFoundError
Handle_ValidCommand_PersistsAggregateAndCommits
GetById_ExistingProduct_ReturnsDto
Deactivate_AlreadyInactive_ReturnsFailure

// Integration / Characterization
POST_ValidProduct_Returns201WithId
POST_MissingName_Returns400
POST_Unauthenticated_Returns401
POST_CustomerRole_Returns403
GET_ExistingProduct_Returns200WithCorrectShape
GET_NonExistentId_Returns404
DELETE_ExistingProduct_Returns204

// Frontend component
renders_WithDefaultProps_ShowsTitle
renders_WhenLoading_ShowsSkeleton
click_AddToCart_DispatchesAddAction
renders_WhenAuthError_ShowsLoginPrompt

// Frontend hook
returnsInitialState_OnMount
updatesCart_WhenAddItemCalled
returnsError_WhenApiFails

// E2E
user_CanAddProductToCart_AndSeeItInCartPage
guest_CannotCheckout_WithoutLogin
admin_CanCreateProduct_AndItAppearsInCatalog
```

---

## Class Names

**Pattern:** `<Subject>Tests` — always plural, always suffix `Tests`

```csharp
// Domain
ProductTests
ReviewTests
EmailValueObjectTests

// Application
CreateProductCommandHandlerTests
GetProductByIdQueryHandlerTests
PlaceOrderCommandHandlerTests

// Integration
ProductsControllerTests
OrdersControllerTests

// Characterization
ProductsCharacterizationTests
OrdersCharacterizationTests

// Projection sync
ReviewsProductProjectionSyncCharacterizationTests
OrderingProductProjectionSyncCharacterizationTests
```

---

## File Names

File name mirrors the class name exactly.

```
ProductTests.cs
CreateProductCommandHandlerTests.cs
ProductsControllerTests.cs
ProductsCharacterizationTests.cs
ReviewsProductProjectionSyncCharacterizationTests.cs
```

Frontend:
```
ProductCard.test.tsx
useCart.test.ts
cartSlice.test.ts
checkout-auth.spec.ts        (E2E UI flow)
api-cart.spec.ts             (E2E API contract)
```

---

## File Location

File location mirrors production namespace / feature path exactly.

**Backend:**
```
Production:  src/backend/Catalog/ECommerce.Catalog.Domain/Aggregates/Product/Product.cs
Test:        src/backend/Catalog/ECommerce.Catalog.Tests/Domain/ProductTests.cs

Production:  src/backend/Catalog/ECommerce.Catalog.Application/Commands/CreateProduct/CreateProductCommandHandler.cs
Test:        src/backend/Catalog/ECommerce.Catalog.Tests/Application/CreateProductCommandHandlerTests.cs
```

**Frontend:**
```
Production:  src/frontend/storefront/src/features/cart/components/CartItem/CartItem.tsx
Test:        src/frontend/storefront/src/features/cart/components/CartItem/CartItem.test.tsx

Production:  src/frontend/storefront/src/features/cart/hooks/useCart.ts
Test:        src/frontend/storefront/src/features/cart/hooks/useCart.test.ts
```

---

## Test Body Structure — AAA

Every test method must have the three sections labelled as comments. No exceptions.

```csharp
[TestMethod]
public void Create_ValidInputs_RaisesProductCreatedEvent()
{
    // Arrange
    string name = "Widget Pro";
    decimal price = 29.99m;

    // Act
    Result<Product> result = Product.Create(name, price);

    // Assert
    Assert.IsTrue(result.IsSuccess);
    Product product = result.GetDataOrThrow();
    Assert.AreEqual(1, product.DomainEvents.Count);
    Assert.IsInstanceOfType<ProductCreatedEvent>(product.DomainEvents[0]);
}
```

Frontend:
```tsx
it('renders_WhenLoading_ShowsSkeleton', () => {
    // Arrange
    const { getByTestId } = renderWithProviders(<ProductCard />, {
        preloadedState: { catalog: { loading: true } },
    });

    // Act — (nothing; render is the act)

    // Assert
    expect(getByTestId('product-skeleton')).toBeInTheDocument();
});
```

When there is no meaningful Arrange or Act, keep the comment but leave the block empty — do not omit the label.

---

## Variable Naming Inside Tests

- Use **explicit types** (not `var`) for all non-trivial assertions in backend tests.
- Anonymous initializer objects are the only exception.

```csharp
// GOOD
Result<Product> result = Product.Create("Widget", 10m);
Product product = result.GetDataOrThrow();
HttpResponseMessage response = await client.GetAsync("/api/products/123");

// BAD
var result = Product.Create("Widget", 10m);
var response = await client.GetAsync("/api/products/123");

// ALLOWED (anonymous object)
var createDto = new { Name = "Widget", Price = 10m };
```

Frontend tests: `const`, `let` everywhere — TypeScript infers correctly.

---

## Region Grouping (backend BC test files only)

Group tests by the method being tested using `#region`:

```csharp
[TestClass]
public class ProductTests
{
    #region Create

    [TestMethod]
    public void Create_ValidInputs_ReturnsProduct() { ... }

    [TestMethod]
    public void Create_EmptyName_ReturnsFailure() { ... }

    #endregion

    #region UpdatePrice

    [TestMethod]
    public void UpdatePrice_ValidAmount_UpdatesAndRaisesEvent() { ... }

    [TestMethod]
    public void UpdatePrice_NegativeAmount_ReturnsFailure() { ... }

    #endregion
}
```
