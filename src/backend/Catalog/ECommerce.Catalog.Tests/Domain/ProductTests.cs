using System;
using System.Linq;
using System.Reflection;
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

    private static Product CreateProduct(string name = "P", decimal price = 1m, string currency = "USD", string sku = "S")
        => Unwrap(Product.Create(name, price, currency, Guid.NewGuid(), sku));

    [TestMethod]
    public void ApiContract_UpdateDetails_UsesValueObjectOverloadOnly()
    {
        MethodInfo? valueObjectMethod = typeof(Product).GetMethod(
            "UpdateDetails",
            [typeof(ProductName), typeof(string), typeof(Guid)]);

        MethodInfo? primitiveMethod = typeof(Product).GetMethod(
            "UpdateDetails",
            [typeof(string), typeof(string), typeof(Guid)]);

        Assert.IsNotNull(valueObjectMethod);
        Assert.IsNull(primitiveMethod);
    }

    [TestMethod]
    public void ApiContract_UpdatePrice_UsesValueObjectOverloadOnly()
    {
        MethodInfo? valueObjectMethod = typeof(Product).GetMethod(
            "UpdatePrice",
            [typeof(Money)]);

        MethodInfo? primitiveMethod = typeof(Product).GetMethod(
            "UpdatePrice",
            [typeof(decimal), typeof(string)]);

        Assert.IsNotNull(valueObjectMethod);
        Assert.IsNull(primitiveMethod);
    }

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
        // Act
        Product product = CreateProduct("Test Prod", sku: "SKU1");
        // Assert
        Assert.AreEqual(ProductStatus.Draft, product.Status);
    }

    [TestMethod]
    public void Create_EmptyName_ReturnsFailureWithProductNameEmptyCode()
    {
        // Act
        var result = Product.Create("", 10m, "USD", Guid.NewGuid(), "SKU-001");
        // Assert
        Assert.IsFalse(result.IsSuccess);
        Assert.AreEqual("PRODUCT_NAME_EMPTY", result.GetErrorOrThrow().Code);
    }

    [TestMethod]
    public void Create_NegativePrice_ReturnsFailureWithMoneyNegativeCode()
    {
        // Act
        var result = Product.Create("Valid", -1m, "USD", Guid.NewGuid(), "SKU-001");
        // Assert
        Assert.IsFalse(result.IsSuccess);
        Assert.AreEqual("MONEY_NEGATIVE", result.GetErrorOrThrow().Code);
    }

    [TestMethod]
    public void Create_InvalidCurrency_ReturnsFailureWithMoneyInvalidCurrencyCode()
    {
        // Act
        var result = Product.Create("Valid", 5m, "US", Guid.NewGuid(), "SKU-001");
        // Assert
        Assert.IsFalse(result.IsSuccess);
        Assert.AreEqual("MONEY_INVALID_CURRENCY", result.GetErrorOrThrow().Code);
    }

    [TestMethod]
    public void Create_EmptySku_SucceedsWithNullSku()
    {
        // Arrange — Sku is now optional; empty/null means "no SKU assigned yet"
        // Act
        var result = Product.Create("Valid", 5m, "USD", Guid.NewGuid(), "");
        // Assert
        Assert.IsTrue(result.IsSuccess);
        Assert.IsNull(result.GetDataOrThrow().Sku);
    }

    [TestMethod]
    public void Create_ValidInputs_SlugDerivedFromName()
    {
        // Arrange
        const string nameRaw = "My Name";
        // Act
        Product product = CreateProduct(nameRaw, sku: "S1");
        // Assert
        Slug expected = Unwrap(Slug.Create(nameRaw));
        Assert.AreEqual(expected.Value, product.Slug.Value);
    }

    [TestMethod]
    public void Create_ValidInputs_RaisesProductCreatedEvent()
    {
        // Act
        Product product = CreateProduct();
        // Assert
        Assert.IsTrue(product.DomainEvents.OfType<ProductCreatedEvent>().Any());
    }

    [TestMethod]
    public void Create_ValidInputs_ImagesIsEmpty()
    {
        // Act
        Product product = CreateProduct();
        // Assert
        Assert.IsFalse(product.Images.Any());
    }

    [TestMethod]
    public void Activate_DraftProduct_StatusBecomesActive()
    {
        // Arrange
        Product product = CreateProduct();
        // Act
        product.Activate();
        // Assert
        Assert.AreEqual(ProductStatus.Active, product.Status);
    }

    [TestMethod]
    public void Activate_AlreadyActive_IsIdempotent()
    {
        // Arrange
        Product product = CreateProduct();
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
        Product product = CreateProduct();
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
        Product product = CreateProduct();
        product.Activate();
        // Act
        product.Deactivate();
        // Assert
        Assert.IsTrue(product.DomainEvents.OfType<ProductDeactivatedEvent>().Any());
    }

    [TestMethod]
    public void Deactivate_DiscontinuedProduct_ReturnsFailure()
    {
        // Arrange
        Product product = CreateProduct();
        product.Discontinue();
        // Act
        var deactivateRes = product.Deactivate();
        // Assert
        Assert.IsFalse(deactivateRes.IsSuccess);
    }

    [TestMethod]
    public void UpdatePrice_NewAmount_RaisesProductPriceChangedEvent()
    {
        // Arrange
        Product product = CreateProduct(price: 5m);
        Money newPrice = Unwrap(Money.Create(10m, "USD"));
        // Act
        product.UpdatePrice(newPrice);
        // Assert
        Assert.IsTrue(product.DomainEvents.OfType<ProductPriceChangedEvent>().Any());
    }

    [TestMethod]
    public void UpdatePrice_NewAmount_EventContainsOldAndNewPrice()
    {
        // Arrange
        Product product = CreateProduct(price: 5m);
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
        Product product = CreateProduct(price: 5m, sku: "SKU-001");
        Money newPrice = Unwrap(Money.Create(20m, "USD"));
        // Act
        product.UpdatePrice(newPrice);
        // Assert
        Assert.AreEqual(20m, product.Price.Amount);
    }

    [TestMethod]
    public void UpdateDetails_NewName_SlugRegeneratedFromNewName()
    {
        // Arrange
        Product product = CreateProduct("Old");
        ProductName newName = Unwrap(ProductName.Create("Brand New"));
        // Act
        product.UpdateDetails(newName, "desc", product.CategoryId);
        // Assert
        Slug expected = Unwrap(Slug.Create(newName.Value));
        Assert.AreEqual(expected.Value, product.Slug.Value);
    }

    [TestMethod]
    public void UpdateDetails_NewName_NameIsUpdated()
    {
        // Arrange
        Product product = CreateProduct("Valid Name", sku: "SKU-001");
        Guid newCategoryId = Guid.NewGuid();
        // Act
        product.UpdateDetails(ProductName.Create("New Name").GetDataOrThrow(), "desc", newCategoryId);
        // Assert
        Assert.AreEqual("New Name", product.Name.Value);
    }

    [TestMethod]
    public void UpdateDetails_NewDescription_DescriptionIsUpdated()
    {
        // Arrange
        Product product = CreateProduct("Valid Name", sku: "SKU-001");
        // Act
        product.UpdateDetails(ProductName.Create(product.Name.Value).GetDataOrThrow(), "new desc", product.CategoryId);
        // Assert
        Assert.AreEqual("new desc", product.Description);
    }

    [TestMethod]
    public void UpdateDetails_NewCategoryId_CategoryIdIsUpdated()
    {
        // Arrange
        Product product = CreateProduct("Valid Name", sku: "SKU-001");
        Guid newCategoryId = Guid.NewGuid();
        // Act
        product.UpdateDetails(ProductName.Create(product.Name.Value).GetDataOrThrow(), product.Description, newCategoryId);
        // Assert
        Assert.AreEqual(newCategoryId, product.CategoryId);
    }

    [TestMethod]
    public void AddImage_FirstImage_IsMarkedPrimary()
    {
        // Arrange
        Product product = CreateProduct();
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
        Product product = CreateProduct();
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
        Product product = CreateProduct();
        for (int i = 0; i < 10; i++) product.AddImage($"http://{i}", null);
        // Act & Assert
        var addRes = product.AddImage("http://too-many", null);
        Assert.IsFalse(addRes.IsSuccess);
    }

    [TestMethod]
    public void SetPrimaryImage_ExistingImageId_SetsCorrectImageAsPrimary()
    {
        // Arrange
        Product product = CreateProduct();
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
        Product product = CreateProduct();
        // Act & Assert
        var setRes = product.SetPrimaryImage(Guid.NewGuid());
        Assert.IsFalse(setRes.IsSuccess);
    }

    [TestMethod]
    public void Delete_AnyProduct_SetsStatusInactive()
    {
        // Arrange
        Product product = CreateProduct();
        // Act
        product.Delete();
        // Assert
        Assert.AreEqual(ProductStatus.Inactive, product.Status);
    }
}
