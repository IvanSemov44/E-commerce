# Naming Conventions

One standard. Applied everywhere. No exceptions.

---

## Method Names

**Pattern:** `Scenario_ExpectedOutcome` for handler/service tests. `Subject_Scenario_ExpectedOutcome` when the subject adds meaning (domain tests, integration tests).

The rule: if you are already inside `CreateProductCommandHandlerTests`, the `Handle_` prefix on every method is noise. Drop it. Keep the subject when two methods on the same type do different things (domain aggregates, integration tests).

```
// Domain tests — keep Subject because the aggregate has many methods
Create_EmptyName_ReturnsFailure
Create_ValidInputs_RaisesProductCreatedEvent
UpdatePrice_NegativeAmount_ReturnsFailure
UpdatePrice_ValidAmount_UpdatesAndRaisesEvent
Deactivate_AlreadyInactive_ReturnsFailure

// Application handler tests — drop Subject (you are already in the handler class)
ValidCommand_CreatesProductAndCommits
DuplicateSku_ReturnsSkuAlreadyExistsError
CategoryNotFound_ReturnsNotFoundError

// Integration / Characterization — keep HTTP verb as the Subject
POST_ValidProduct_Returns201WithId
POST_MissingName_Returns400
POST_Unauthenticated_Returns401
POST_CustomerRole_Returns403
GET_ExistingProduct_Returns200WithCorrectShape
GET_NonExistentId_Returns404
DELETE_ExistingProduct_Returns204

// Projection sync
Publish_NewProduct_InsertsProjection
Publish_ExistingProduct_UpdatesProjection
Publish_DeletedProduct_RemovesProjection

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

// Application — one class per handler
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
    result.IsSuccess.ShouldBeTrue();
    Product product = result.GetDataOrThrow();
    product.DomainEvents.ShouldHaveSingleItem();
    product.DomainEvents[0].ShouldBeOfType<ProductCreatedEvent>();
}
```

Frontend:
```tsx
it('renders_WhenLoading_ShowsSkeleton', () => {
    // Arrange
    server.use(http.get('/api/products', () => new HttpResponse(null, { status: 200 })));

    // Act
    renderWithProviders(<ProductCard productId="1" />);

    // Assert
    expect(screen.getByTestId('product-skeleton')).toBeInTheDocument();
});
```

When there is no meaningful Arrange or Act, keep the comment but leave the block empty — do not omit the label.

---

## Grouping tests by method — nested classes, not #region

`#region` hides structure and is discouraged in modern C#. Use **nested `[TestClass]`** to group tests by the method under test. MSTest 4 supports nested classes fully.

```csharp
// WRONG — #region is a 2010 pattern
[TestClass]
public class ProductTests
{
    #region Create
    [TestMethod]
    public void Create_ValidInputs_ReturnsProduct() { }
    [TestMethod]
    public void Create_EmptyName_ReturnsFailure() { }
    #endregion

    #region UpdatePrice
    [TestMethod]
    public void UpdatePrice_ValidAmount_UpdatesAndRaisesEvent() { }
    #endregion
}

// RIGHT — nested test classes
[TestClass]
public class ProductTests
{
    [TestClass]
    public class Create
    {
        [TestMethod]
        public void ValidInputs_ReturnsProduct() { }

        [TestMethod]
        public void EmptyName_ReturnsFailure() { }

        [TestMethod]
        public void NegativePrice_ReturnsFailure() { }
    }

    [TestClass]
    public class UpdatePrice
    {
        [TestMethod]
        public void ValidAmount_UpdatesAndRaisesEvent() { }

        [TestMethod]
        public void NegativeAmount_ReturnsFailure() { }
    }

    [TestClass]
    public class Deactivate
    {
        [TestMethod]
        public void AlreadyInactive_ReturnsFailure() { }

        [TestMethod]
        public void Active_DeactivatesAndRaisesEvent() { }
    }
}
```

Benefits: test runner shows `ProductTests > Create > EmptyName_ReturnsFailure`. No folding. Proper class isolation. Navigable.

---

## Parameterized Tests

Use `[DataTestMethod]` + `[DataRow]` for boundary and equivalence class testing on value objects. Avoids copy-paste test methods that differ only in the input.

```csharp
[TestClass]
public class ProductTests
{
    [TestClass]
    public class Create
    {
        [DataTestMethod]
        [DataRow(0)]
        [DataRow(-0.01)]
        [DataRow(-100)]
        public void NonPositivePrice_ReturnsFailure(decimal price)
        {
            // Arrange + Act
            Result<Product> result = Product.Create("Widget", price);

            // Assert
            result.IsSuccess.ShouldBeFalse();
        }

        [DataTestMethod]
        [DataRow(0.01)]
        [DataRow(1)]
        [DataRow(9999.99)]
        public void PositivePrice_Succeeds(decimal price)
        {
            Result<Product> result = Product.Create("Widget", price);
            result.IsSuccess.ShouldBeTrue();
        }
    }
}
```

---

## Variable Naming Inside Tests

- Use **explicit types** (not `var`) for all assertion subjects in backend tests. This makes the type visible in code review and prevents accidental `object` inference.
- Anonymous initializer objects are the only exception.

```csharp
// GOOD — type is explicit for assertion subjects
Result<Product> result = Product.Create("Widget", 10m);
Product product = result.GetDataOrThrow();
HttpResponseMessage response = await client.GetAsync("/api/products/123");
ApiResponse<ProductDto>? body = await Deserialize<ApiResponse<ProductDto>>(response);

// OK — anonymous objects used only as request payloads
var createDto = new { Name = "Widget", Price = 10m };

// BAD — type invisible at point of assertion
var result = Product.Create("Widget", 10m);
var response = await client.GetAsync("/api/products/123");
```

Frontend tests: `const`, `let` everywhere — TypeScript infers correctly.
