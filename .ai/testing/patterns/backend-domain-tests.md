# Pattern: Backend Domain Tests

Layer 1. Pure C#. No infrastructure. Fastest tests in the repository.

---

## Project structure

```
src/backend/<BC>/ECommerce.<BC>.Tests/
└── Domain/
    ├── <AggregateName>Tests.cs
    └── <ValueObjectName>Tests.cs
```

---

## Standard template

```csharp
[TestClass]
public class ProductTests
{
    // ── Helpers ───────────────────────────────────────────────────────────────

    private static Product CreateValidProduct(
        string name = "Widget Pro",
        decimal price = 29.99m,
        string sku = "SKU-001")
    {
        return Product.Create(name, price, sku).GetDataOrThrow();
    }

    // ── Create ────────────────────────────────────────────────────────────────

    #region Create

    [TestMethod]
    public void Create_ValidInputs_ReturnsProduct()
    {
        // Arrange
        string name = "Widget Pro";
        decimal price = 29.99m;

        // Act
        Result<Product> result = Product.Create(name, price, "SKU-001");

        // Assert
        Assert.IsTrue(result.IsSuccess);
        Product product = result.GetDataOrThrow();
        Assert.AreEqual(name, product.Name.Value);
        Assert.AreEqual(price, product.Price.Amount);
    }

    [TestMethod]
    public void Create_ValidInputs_RaisesProductCreatedEvent()
    {
        // Act
        Result<Product> result = Product.Create("Widget", 10m, "SKU-001");

        // Assert
        Product product = result.GetDataOrThrow();
        Assert.AreEqual(1, product.DomainEvents.Count);
        Assert.IsInstanceOfType<ProductCreatedEvent>(product.DomainEvents[0]);
    }

    [TestMethod]
    public void Create_EmptyName_ReturnsFailure()
    {
        // Act
        Result<Product> result = Product.Create("", 10m, "SKU-001");

        // Assert
        Assert.IsFalse(result.IsSuccess);
        Assert.AreEqual("CATALOG_PRODUCT_NAME_EMPTY", result.GetErrorOrThrow().Code);
    }

    [TestMethod]
    public void Create_NegativePrice_ReturnsFailure()
    {
        // Act
        Result<Product> result = Product.Create("Widget", -1m, "SKU-001");

        // Assert
        Assert.IsFalse(result.IsSuccess);
        Assert.AreEqual("CATALOG_PRODUCT_PRICE_NEGATIVE", result.GetErrorOrThrow().Code);
    }

    #endregion

    // ── UpdatePrice ───────────────────────────────────────────────────────────

    #region UpdatePrice

    [TestMethod]
    public void UpdatePrice_ValidAmount_UpdatesPriceAndRaisesEvent()
    {
        // Arrange
        Product product = CreateValidProduct();
        decimal newPrice = 49.99m;

        // Act
        Result result = product.UpdatePrice(newPrice);

        // Assert
        Assert.IsTrue(result.IsSuccess);
        Assert.AreEqual(newPrice, product.Price.Amount);
        Assert.IsTrue(product.DomainEvents.OfType<ProductPriceChangedEvent>().Any());
    }

    [TestMethod]
    public void UpdatePrice_NegativeAmount_ReturnsFailure()
    {
        // Arrange
        Product product = CreateValidProduct();

        // Act
        Result result = product.UpdatePrice(-1m);

        // Assert
        Assert.IsFalse(result.IsSuccess);
        Assert.AreEqual("CATALOG_PRODUCT_PRICE_NEGATIVE", result.GetErrorOrThrow().Code);
    }

    #endregion
}
```

---

## Rules

1. **One factory helper** per test class — `CreateValid<AggregateName>()` with sensible defaults. Override only what the test cares about.

2. **Every `Result.Fail` branch = one test.** Count the `return Result.Fail(...)` calls in the production code and match them 1:1.

3. **Assert the error code, not the error message.** Messages can change; codes are the contract.

4. **Assert domain events were raised** for every method that is supposed to raise one. Check type and key property:
   ```csharp
   ProductPriceChangedEvent evt = product.DomainEvents.OfType<ProductPriceChangedEvent>().Single();
   Assert.AreEqual(49.99m, evt.NewPrice);
   ```

5. **Do not assert on properties that are not the subject of the test.** If you are testing `UpdatePrice`, do not also assert that `Name` is unchanged — that is noise.

6. **No `[TestInitialize]`** for domain tests. Use the factory helper instead.

---

## Value object template

```csharp
[TestClass]
public class EmailValueObjectTests
{
    #region Create

    [TestMethod]
    public void Create_ValidEmail_ReturnsEmail()
    {
        Result<Email> result = Email.Create("user@example.com");
        Assert.IsTrue(result.IsSuccess);
        Assert.AreEqual("user@example.com", result.GetDataOrThrow().Value);
    }

    [TestMethod]
    public void Create_EmptyString_ReturnsFailure()
    {
        Result<Email> result = Email.Create("");
        Assert.IsFalse(result.IsSuccess);
        Assert.AreEqual("IDENTITY_EMAIL_EMPTY", result.GetErrorOrThrow().Code);
    }

    [TestMethod]
    public void Create_InvalidFormat_ReturnsFailure()
    {
        Result<Email> result = Email.Create("not-an-email");
        Assert.IsFalse(result.IsSuccess);
        Assert.AreEqual("IDENTITY_EMAIL_INVALID_FORMAT", result.GetErrorOrThrow().Code);
    }

    [TestMethod]
    public void Create_ExceedsMaxLength_ReturnsFailure()
    {
        string tooLong = new string('a', 250) + "@example.com";
        Result<Email> result = Email.Create(tooLong);
        Assert.IsFalse(result.IsSuccess);
        Assert.AreEqual("IDENTITY_EMAIL_TOO_LONG", result.GetErrorOrThrow().Code);
    }

    #endregion
}
```
