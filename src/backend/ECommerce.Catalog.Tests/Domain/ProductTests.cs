using System;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ECommerce.Catalog.Domain.ValueObjects;
using ECommerce.Catalog.Domain.Aggregates.Product;
using ECommerce.Catalog.Domain.Aggregates.Product.Events;
using ECommerce.SharedKernel.Results;

namespace ECommerce.Catalog.Tests.Domain;

[TestClass]
public class ProductTests
{
    private static T Unwrap<T>(Result<T> result) => result.GetDataOrThrow();

    [TestMethod]
    public void ProductName_EmptyString_ReturnsFailure()
    {
        // Arrange
        string raw = "";
        // Act
        var result1 = ProductName.Create(raw);
        // Assert
        Assert.IsFalse(result1.IsSuccess);
    }

    [TestMethod]
    public void ProductName_WhitespaceOnly_ReturnsFailure()
    {
        // Arrange
        string raw = "   ";
        // Act
        var result2 = ProductName.Create(raw);
        // Assert
        Assert.IsFalse(result2.IsSuccess);
    }

    [TestMethod]
    public void ProductName_ExceedsMaxLength_ReturnsFailure()
    {
        // Arrange
        string raw = new string('a', 201);
        // Act
        var result3 = ProductName.Create(raw);
        // Assert
        Assert.IsFalse(result3.IsSuccess);
    }

    [TestMethod]
    public void ProductName_ValidInput_TrimsWhitespace()
    {
        // Arrange
        string raw = "  My Product  ";
        // Act
        ProductName name = Unwrap(ProductName.Create(raw));
        // Assert
        Assert.AreEqual("My Product", name.Value);
    }

    [TestMethod]
    public void Slug_EmptyString_ReturnsFailure()
    {
        // Arrange
        string raw = "";
        // Act
        var slugResult1 = Slug.Create(raw);
        // Assert
        Assert.IsFalse(slugResult1.IsSuccess);
    }

    [TestMethod]
    public void Slug_OnlySpecialChars_ReturnsFailure()
    {
        // Arrange
        string raw = "!!!!";
        // Act
        var slugResult2 = Slug.Create(raw);
        // Assert
        Assert.IsFalse(slugResult2.IsSuccess);
    }

    [TestMethod]
    public void Slug_MixedCase_IsLowercased()
    {
        // Arrange
        string raw = "My Product";
        // Act
        Slug slug = Unwrap(Slug.Create(raw));
        // Assert
        Assert.AreEqual("my-product", slug.Value);
    }

    [TestMethod]
    public void Slug_SpacesAndUnderscores_BecomeHyphens()
    {
        // Arrange
        string raw = "My_Product Name";
        // Act
        Slug slug = Unwrap(Slug.Create(raw));
        // Assert
        Assert.AreEqual("my-product-name", slug.Value);
    }

    [TestMethod]
    public void Slug_MultipleHyphens_Collapsed()
    {
        // Arrange
        string raw = "a__b  c---d";
        // Act
        Slug slug = Unwrap(Slug.Create(raw));
        // Assert
        Assert.AreEqual(-1, slug.Value.IndexOf("--", StringComparison.Ordinal));
    }

    [TestMethod]
    public void Slug_LeadingTrailingHyphens_Stripped()
    {
        // Arrange
        string raw = "  -Hello-World-  ";
        // Act
        Slug slug = Unwrap(Slug.Create(raw));
        // Assert
        Assert.AreEqual("hello-world", slug.Value);
    }

    [TestMethod]
    public void Sku_EmptyString_ReturnsFailure()
    {
        // Arrange
        string raw = "";
        // Act
        var skuRes1 = Sku.Create(raw);
        // Assert
        Assert.IsFalse(skuRes1.IsSuccess);
    }

    [TestMethod]
    public void Sku_ExceedsMaxLength_ReturnsFailure()
    {
        // Arrange
        string raw = new string('x', 101);
        // Act
        var skuRes2 = Sku.Create(raw);
        // Assert
        Assert.IsFalse(skuRes2.IsSuccess);
    }

    [TestMethod]
    public void Sku_ValidInput_TrimsWhitespace()
    {
        // Arrange
        string raw = "  SKU123  ";
        // Act
        Sku sku = Unwrap(Sku.Create(raw));
        // Assert
        Assert.AreEqual("SKU123", sku.Value);
    }

    [TestMethod]
    public void Money_NegativeAmount_ReturnsFailure()
    {
        // Arrange
        decimal amount = -1m;
        // Act
        var moneyRes1 = Money.Create(amount, "USD");
        // Assert
        Assert.IsFalse(moneyRes1.IsSuccess);
    }

    [TestMethod]
    public void Money_InvalidCurrencyCode_ReturnsFailure()
    {
        // Arrange
        decimal amount = 10m;
        // Act
        var moneyRes2 = Money.Create(amount, "US");
        var moneyRes3 = Money.Create(amount, "");
        // Assert
        Assert.IsFalse(moneyRes2.IsSuccess);
        Assert.IsFalse(moneyRes3.IsSuccess);
    }

    [TestMethod]
    public void Money_ValidInput_CurrencyIsUppercased()
    {
        // Arrange
        Money money = Unwrap(Money.Create(5m, "usd"));
        // Act & Assert
        Assert.AreEqual("USD", money.Currency);
    }

    [TestMethod]
    public void Money_Add_SameCurrency_ReturnsSum()
    {
        // Arrange
        var a = Money.Create(5m, "USD").GetDataOrThrow();
        var b = Money.Create(3m, "USD").GetDataOrThrow();
        // Act
        var sum = a.Add(b).GetDataOrThrow();
        // Assert
        Assert.AreEqual(8m, sum.Amount);
        Assert.AreEqual("USD", sum.Currency);
    }

    [TestMethod]
    public void Money_Add_DifferentCurrency_ReturnsFailure()
    {
        // Arrange
        var a = Money.Create(5m, "USD").GetDataOrThrow();
        var b = Money.Create(3m, "EUR").GetDataOrThrow();
        // Act & Assert
        var addRes = a.Add(b);
        Assert.IsFalse(addRes.IsSuccess);
    }

    [TestMethod]
    public void Weight_NegativeValue_ReturnsFailure()
    {
        // Arrange
        decimal value = -0.1m;
        // Act
        var weightRes = Weight.Create(value);
        // Assert
        Assert.IsFalse(weightRes.IsSuccess);
    }

    [TestMethod]
    public void Weight_ZeroValue_IsAllowed()
    {
        // Arrange
        decimal value = 0m;
        // Act
        Weight weight = Unwrap(Weight.Create(value));
        // Assert
        Assert.AreEqual(0m, weight.Value);
    }

    [TestMethod]
    public void StockQuantity_NegativeValue_ReturnsFailure()
    {
        // Arrange
        int value = -1;
        // Act
        var stockRes = StockQuantity.Create(value);
        // Assert
        Assert.IsFalse(stockRes.IsSuccess);
    }

    [TestMethod]
    public void StockQuantity_ZeroValue_IsAllowed()
    {
        // Arrange
        int value = 0;
        // Act
        StockQuantity qty = Unwrap(StockQuantity.Create(value));
        // Assert
        Assert.AreEqual(0, qty.Value);
    }

    [TestMethod]
    public void Create_ValidInputs_ProductIsInDraftStatus()
    {
        // Arrange
        ProductName name = Unwrap(ProductName.Create("Test Prod"));
        Money price = Unwrap(Money.Create(10m, "USD"));
        Sku sku = Unwrap(Sku.Create("SKU1"));
        Guid categoryId = Guid.NewGuid();
        // Act
        Product product = Unwrap(Product.Create(name.Value, price.Amount, price.Currency, sku.Value, categoryId));
        // Assert
        Assert.AreEqual(ProductStatus.Draft, product.Status);
    }

    [TestMethod]
    public void Create_EmptyName_ReturnsFailureWithProductNameEmptyCode()
    {
        // Arrange
        Guid categoryId = Guid.NewGuid();
        // Act
        var result = Product.Create("", 10m, "USD", "SKU-001", categoryId);
        // Assert
        Assert.IsFalse(result.IsSuccess);
        Assert.AreEqual("PRODUCT_NAME_EMPTY", result.GetErrorOrThrow().Code);
    }

    [TestMethod]
    public void Create_NegativePrice_ReturnsFailureWithMoneyNegativeCode()
    {
        // Arrange
        Guid categoryId = Guid.NewGuid();
        // Act
        var result = Product.Create("Valid", -1m, "USD", "SKU-001", categoryId);
        // Assert
        Assert.IsFalse(result.IsSuccess);
        Assert.AreEqual("MONEY_NEGATIVE", result.GetErrorOrThrow().Code);
    }

    [TestMethod]
    public void Create_InvalidCurrency_ReturnsFailureWithMoneyInvalidCurrencyCode()
    {
        // Arrange
        Guid categoryId = Guid.NewGuid();
        // Act
        var result = Product.Create("Valid", 5m, "US", "SKU-001", categoryId);
        // Assert
        Assert.IsFalse(result.IsSuccess);
        Assert.AreEqual("MONEY_INVALID_CURRENCY", result.GetErrorOrThrow().Code);
    }

    [TestMethod]
    public void Create_EmptySku_ReturnsFailureWithSkuEmptyCode()
    {
        // Arrange
        Guid categoryId = Guid.NewGuid();
        // Act
        var result = Product.Create("Valid", 5m, "USD", "", categoryId);
        // Assert
        Assert.IsFalse(result.IsSuccess);
        Assert.AreEqual("SKU_EMPTY", result.GetErrorOrThrow().Code);
    }

    [TestMethod]
    public void Create_ValidInputs_SlugDerivedFromName()
    {
        // Arrange
        ProductName name = Unwrap(ProductName.Create("My Name"));
        Money price = Unwrap(Money.Create(1m, "USD"));
        Sku sku = Unwrap(Sku.Create("S1"));
        Guid categoryId = Guid.NewGuid();
        // Act
        Product product = Unwrap(Product.Create(name.Value, price.Amount, price.Currency, sku.Value, categoryId));
        // Assert
        Slug expected = Unwrap(Slug.Create(name.Value));
        Assert.AreEqual(expected.Value, product.Slug.Value);
    }

    [TestMethod]
    public void Create_ValidInputs_RaisesProductCreatedEvent()
    {
        // Arrange
        ProductName name = Unwrap(ProductName.Create("P"));
        Money price = Unwrap(Money.Create(1m, "USD"));
        Sku sku = Unwrap(Sku.Create("S"));
        Guid categoryId = Guid.NewGuid();
        // Act
        Product product = Unwrap(Product.Create(name.Value, price.Amount, price.Currency, sku.Value, categoryId));
        // Assert
        bool hasEvent = product.DomainEvents.OfType<ProductCreatedEvent>().Any();
        Assert.IsTrue(hasEvent);
    }

    [TestMethod]
    public void Create_ValidInputs_ImagesIsEmpty()
    {
        // Arrange
        ProductName name = Unwrap(ProductName.Create("P"));
        Money price = Unwrap(Money.Create(1m, "USD"));
        Sku sku = Unwrap(Sku.Create("S"));
        Guid categoryId = Guid.NewGuid();
        // Act
        Product product = Unwrap(Product.Create(name.Value, price.Amount, price.Currency, sku.Value, categoryId));
        // Assert
        Assert.IsFalse(product.Images.Any());
    }

    [TestMethod]
    public void Activate_DraftProduct_StatusBecomesActive()
    {
        // Arrange
        ProductName name = Unwrap(ProductName.Create("P"));
        Money price = Unwrap(Money.Create(1m, "USD"));
        Sku sku = Unwrap(Sku.Create("S"));
        Guid categoryId = Guid.NewGuid();
        Product product = Unwrap(Product.Create(name.Value, price.Amount, price.Currency, sku.Value, categoryId));
        // Act
        product.Activate();
        // Assert
        Assert.AreEqual(ProductStatus.Active, product.Status);
    }

    [TestMethod]
    public void Activate_AlreadyActive_IsIdempotent()
    {
        // Arrange
        ProductName name = Unwrap(ProductName.Create("P"));
        Money price = Unwrap(Money.Create(1m, "USD"));
        Sku sku = Unwrap(Sku.Create("S"));
        Guid categoryId = Guid.NewGuid();
        Product product = Unwrap(Product.Create(name.Value, price.Amount, price.Currency, sku.Value, categoryId));
        product.Activate();
        // Act
        product.Activate();
        // Assert
        Assert.AreEqual(ProductStatus.Active, product.Status);
    }

    [TestMethod]
    public void Deactivate_ActiveProduct_StatusBecomesInactive()
    {
        // Arrange
        ProductName name = Unwrap(ProductName.Create("P"));
        Money price = Unwrap(Money.Create(1m, "USD"));
        Sku sku = Unwrap(Sku.Create("S"));
        Guid categoryId = Guid.NewGuid();
        Product product = Unwrap(Product.Create(name.Value, price.Amount, price.Currency, sku.Value, categoryId));
        product.Activate();
        // Act
        product.Deactivate();
        // Assert
        Assert.AreEqual(ProductStatus.Inactive, product.Status);
    }

    [TestMethod]
    public void Deactivate_ActiveProduct_RaisesProductDeactivatedEvent()
    {
        // Arrange
        ProductName name = Unwrap(ProductName.Create("P"));
        Money price = Unwrap(Money.Create(1m, "USD"));
        Sku sku = Unwrap(Sku.Create("S"));
        Guid categoryId = Guid.NewGuid();
        Product product = Unwrap(Product.Create(name.Value, price.Amount, price.Currency, sku.Value, categoryId));
        product.Activate();
        // Act
        product.Deactivate();
        // Assert
        bool hasEvent = product.DomainEvents.OfType<ProductDeactivatedEvent>().Any();
        Assert.IsTrue(hasEvent);
    }

    [TestMethod]
    public void Deactivate_DiscontinuedProduct_ReturnsFailure()
    {
        // Arrange
        ProductName name = Unwrap(ProductName.Create("P"));
        Money price = Unwrap(Money.Create(1m, "USD"));
        Sku sku = Unwrap(Sku.Create("S"));
        Guid categoryId = Guid.NewGuid();
        Product product = Unwrap(Product.Create(name.Value, price.Amount, price.Currency, sku.Value, categoryId));
        System.Reflection.FieldInfo? backing = typeof(Product).GetField("<Status>k__BackingField", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
        if (backing is null) Assert.Inconclusive("Could not locate backing field for Status");
        // Act
        backing!.SetValue(product, ProductStatus.Discontinued);
        // Assert
        var deactivateRes = product.Deactivate();
        Assert.IsFalse(deactivateRes.IsSuccess);
    }

    [TestMethod]
    public void UpdatePrice_NewAmount_RaisesProductPriceChangedEvent()
    {
        // Arrange
        ProductName name = Unwrap(ProductName.Create("P"));
        Money price = Unwrap(Money.Create(5m, "USD"));
        Sku sku = Unwrap(Sku.Create("S"));
        Guid categoryId = Guid.NewGuid();
        Product product = Unwrap(Product.Create(name.Value, price.Amount, price.Currency, sku.Value, categoryId));
        Money newPrice = Unwrap(Money.Create(10m, "USD"));
        // Act
        product.UpdatePrice(newPrice);
        // Assert
        bool hasEvent = product.DomainEvents.OfType<ProductPriceChangedEvent>().Any();
        Assert.IsTrue(hasEvent);
    }

    [TestMethod]
    public void UpdatePrice_NewAmount_EventContainsOldAndNewPrice()
    {
        // Arrange
        ProductName name = Unwrap(ProductName.Create("P"));
        Money price = Unwrap(Money.Create(5m, "USD"));
        Sku sku = Unwrap(Sku.Create("S"));
        Guid categoryId = Guid.NewGuid();
        Product product = Unwrap(Product.Create(name.Value, price.Amount, price.Currency, sku.Value, categoryId));
        Money newPrice = Unwrap(Money.Create(10m, "USD"));
        // Act
        product.UpdatePrice(newPrice);
        // Assert
        ProductPriceChangedEvent? evt = product.DomainEvents.OfType<ProductPriceChangedEvent>().FirstOrDefault();
        Assert.IsNotNull(evt);
        Assert.AreEqual(5m, evt!.OldPrice.Amount);
        Assert.AreEqual(10m, evt.NewPrice.Amount);
    }

    [TestMethod]
    public void UpdatePrice_NewPrice_PriceIsUpdated()
    {
        // Arrange
        var createRes = Product.Create("Valid Name", 5m, "USD", "SKU-001", Guid.NewGuid());
        var product = createRes.GetDataOrThrow();
        var newPrice = Money.Create(20m, "USD").GetDataOrThrow();
        // Act
        product.UpdatePrice(newPrice);
        // Assert
        Assert.AreEqual(20m, product.Price.Amount);
    }

    [TestMethod]
    public void UpdateDetails_NewName_SlugRegeneratedFromNewName()
    {
        // Arrange
        ProductName name = Unwrap(ProductName.Create("Old"));
        Money price = Unwrap(Money.Create(1m, "USD"));
        Sku sku = Unwrap(Sku.Create("S"));
        Guid categoryId = Guid.NewGuid();
        Product product = Unwrap(Product.Create(name.Value, price.Amount, price.Currency, sku.Value, categoryId));
        ProductName newName = Unwrap(ProductName.Create("Brand New"));
        // Act
        product.UpdateDetails(newName, "desc", categoryId);
        // Assert
        Slug expected = Unwrap(Slug.Create(newName.Value));
        Assert.AreEqual(expected.Value, product.Slug.Value);
    }

    [TestMethod]
    public void UpdateDetails_NewName_NameIsUpdated()
    {
        // Arrange
        var createRes = Product.Create("Valid Name", 10m, "USD", "SKU-001", Guid.NewGuid());
        var product = createRes.GetDataOrThrow();
        var newCategoryId = Guid.NewGuid();
        // Act
        product.UpdateDetails(ProductName.Create("New Name").GetDataOrThrow(), "desc", newCategoryId);
        // Assert
        Assert.AreEqual("New Name", product.Name.Value);
    }

    [TestMethod]
    public void UpdateDetails_NewDescription_DescriptionIsUpdated()
    {
        // Arrange
        var createRes = Product.Create("Valid Name", 10m, "USD", "SKU-001", Guid.NewGuid());
        var product = createRes.GetDataOrThrow();
        var newCategoryId = product.CategoryId;
        // Act
        product.UpdateDetails(ProductName.Create(product.Name.Value).GetDataOrThrow(), "new desc", newCategoryId);
        // Assert
        Assert.AreEqual("new desc", product.Description);
    }

    [TestMethod]
    public void UpdateDetails_NewCategoryId_CategoryIdIsUpdated()
    {
        // Arrange
        var createRes = Product.Create("Valid Name", 10m, "USD", "SKU-001", Guid.NewGuid());
        var product = createRes.GetDataOrThrow();
        var newCategoryId = Guid.NewGuid();
        // Act
        product.UpdateDetails(ProductName.Create(product.Name.Value).GetDataOrThrow(), product.Description, newCategoryId);
        // Assert
        Assert.AreEqual(newCategoryId, product.CategoryId);
    }

    [TestMethod]
    public void AddImage_FirstImage_IsMarkedPrimary()
    {
        // Arrange
        ProductName name = Unwrap(ProductName.Create("P"));
        Money price = Unwrap(Money.Create(1m, "USD"));
        Sku sku = Unwrap(Sku.Create("S"));
        Guid categoryId = Guid.NewGuid();
        Product product = Unwrap(Product.Create(name.Value, price.Amount, price.Currency, sku.Value, categoryId));
        // Act
        product.AddImage("http://img", "alt");
        // Assert
        Assert.HasCount(1, product.Images);
        Assert.IsTrue(product.Images.First().IsPrimary);
    }

    [TestMethod]
    public void AddImage_SecondImage_IsNotPrimary()
    {
        // Arrange
        ProductName name = Unwrap(ProductName.Create("P"));
        Money price = Unwrap(Money.Create(1m, "USD"));
        Sku sku = Unwrap(Sku.Create("S"));
        Guid categoryId = Guid.NewGuid();
        Product product = Unwrap(Product.Create(name.Value, price.Amount, price.Currency, sku.Value, categoryId));
        product.AddImage("http://img1", "a1");
        // Act
        product.AddImage("http://img2", "a2");
        // Assert
        Assert.HasCount(2, product.Images);
        Assert.IsFalse(product.Images.Skip(1).First().IsPrimary);
    }

    [TestMethod]
    public void AddImage_MaxImagesReached_ReturnsFailure()
    {
        // Arrange
        ProductName name = Unwrap(ProductName.Create("P"));
        Money price = Unwrap(Money.Create(1m, "USD"));
        Sku sku = Unwrap(Sku.Create("S"));
        Guid categoryId = Guid.NewGuid();
        Product product = Unwrap(Product.Create(name.Value, price.Amount, price.Currency, sku.Value, categoryId));
        for (int i = 0; i < 10; i++) product.AddImage($"http://{i}", null);
        // Act & Assert
        var addRes = product.AddImage("http://too-many", null);
        Assert.IsFalse(addRes.IsSuccess);
    }

    [TestMethod]
    public void SetPrimaryImage_ExistingImageId_SetsCorrectImageAsPrimary()
    {
        // Arrange
        ProductName name = Unwrap(ProductName.Create("P"));
        Money price = Unwrap(Money.Create(1m, "USD"));
        Sku sku = Unwrap(Sku.Create("S"));
        Guid categoryId = Guid.NewGuid();
        Product product = Unwrap(Product.Create(name.Value, price.Amount, price.Currency, sku.Value, categoryId));
        product.AddImage("http://a", null);
        product.AddImage("http://b", null);
        Guid imageId = product.Images.Skip(1).First().Id;
        // Act
        product.SetPrimaryImage(imageId);
        // Assert
        Assert.IsTrue(product.Images.Skip(1).First().IsPrimary);
        Assert.IsFalse(product.Images.First().IsPrimary);
    }

    [TestMethod]
    public void SetPrimaryImage_UnknownImageId_ReturnsFailure()
    {
        // Arrange
        ProductName name = Unwrap(ProductName.Create("P"));
        Money price = Unwrap(Money.Create(1m, "USD"));
        Sku sku = Unwrap(Sku.Create("S"));
        Guid categoryId = Guid.NewGuid();
        Product product = Unwrap(Product.Create(name.Value, price.Amount, price.Currency, sku.Value, categoryId));
        // Act & Assert
        var setRes = product.SetPrimaryImage(Guid.NewGuid());
        Assert.IsFalse(setRes.IsSuccess);
    }

    [TestMethod]
    public void Delete_AnyProduct_IsDeletedIsTrue()
    {
        // Arrange
        ProductName name = Unwrap(ProductName.Create("P"));
        Money price = Unwrap(Money.Create(1m, "USD"));
        Sku sku = Unwrap(Sku.Create("S"));
        Guid categoryId = Guid.NewGuid();
        Product product = Unwrap(Product.Create(name.Value, price.Amount, price.Currency, sku.Value, categoryId));
        // Act
        product.Delete();
        // Assert
        Assert.IsTrue(product.IsDeleted);
    }
}
