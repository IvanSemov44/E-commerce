using ECommerce.SharedKernel.Enums;
using ECommerce.Promotions.Domain.Enums;
using UserRole = ECommerce.SharedKernel.Enums.UserRole;
using ECommerce.Tests.Helpers;

namespace ECommerce.Tests.Unit.Helpers;

/// <summary>
/// Tests for TestDataFactory to verify test infrastructure.
/// </summary>
[TestClass]
public class TestDataFactoryTests
{
    [TestMethod]
    public void CreateUser_ReturnsValidUser()
    {
        // Act
        var user = TestDataFactory.CreateUser();

        // Assert
        user.ShouldNotBeNull();
        user.Id.ShouldNotBe(Guid.Empty);
        user.Email.ShouldNotBeNullOrWhiteSpace();
        user.FirstName.ShouldNotBeNullOrWhiteSpace();
        user.LastName.ShouldNotBeNullOrWhiteSpace();
        user.PasswordHash.ShouldNotBeNullOrWhiteSpace();
        user.Role.ShouldBe(UserRole.Customer);
        user.IsEmailVerified.ShouldBeTrue();
    }

    [TestMethod]
    public void CreateProduct_ReturnsValidProduct()
    {
        // Act
        var product = TestDataFactory.CreateProduct();

        // Assert
        product.ShouldNotBeNull();
        product.Id.ShouldNotBe(Guid.Empty);
        product.Name.ShouldNotBeNullOrWhiteSpace();
        product.Slug.ShouldNotBeNullOrWhiteSpace();
        product.Price.ShouldBeGreaterThan(0);
        product.StockQuantity.ShouldBeGreaterThanOrEqualTo(0);
        product.IsActive.ShouldBeTrue();
    }

    [TestMethod]
    public void CreatePromoCode_ReturnsValidPromoCode()
    {
        // Act
        var promoCode = TestDataFactory.CreatePromoCode();

        // Assert
        promoCode.ShouldNotBeNull();
        promoCode.Id.ShouldNotBe(Guid.Empty);
        promoCode.Code.Value.ShouldNotBeNullOrWhiteSpace();
        promoCode.Discount.Type.ShouldBe(ECommerce.Promotions.Domain.Enums.DiscountType.Percentage);
        promoCode.Discount.Amount.ShouldBe(10);
        promoCode.IsActive.ShouldBeTrue();
    }

    [TestMethod]
    public void CreateOrder_ReturnsValidOrder()
    {
        // Arrange
        var userId = Guid.NewGuid();

        // Act
        var order = TestDataFactory.CreateOrder(userId);

        // Assert
        order.ShouldNotBeNull();
        order.Id.ShouldNotBe(Guid.Empty);
        order.UserId.ShouldBe(userId);
        order.OrderNumber.ShouldNotBeNullOrWhiteSpace();
        order.TotalAmount.ShouldBeGreaterThan(0);
        order.Status.ShouldBe(OrderStatus.Pending);
        order.PaymentStatus.ShouldBe(PaymentStatus.Pending);
    }

    [TestMethod]
    public void CreateCategory_ReturnsValidCategory()
    {
        // Act
        var category = TestDataFactory.CreateCategory();

        // Assert
        category.ShouldNotBeNull();
        category.Id.ShouldNotBe(Guid.Empty);
        category.Name.ShouldNotBeNullOrWhiteSpace();
        category.Slug.ShouldNotBeNullOrWhiteSpace();
        category.IsActive.ShouldBeTrue();
    }
}
