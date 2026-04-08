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
        user.Should().NotBeNull();
        user.Id.Should().NotBeEmpty();
        user.Email.Should().NotBeNullOrWhiteSpace();
        user.FirstName.Should().NotBeNullOrWhiteSpace();
        user.LastName.Should().NotBeNullOrWhiteSpace();
        user.PasswordHash.Should().NotBeNullOrWhiteSpace();
        user.Role.Should().Be(UserRole.Customer);
        user.IsEmailVerified.Should().BeTrue();
    }

    [TestMethod]
    public void CreateProduct_ReturnsValidProduct()
    {
        // Act
        var product = TestDataFactory.CreateProduct();

        // Assert
        product.Should().NotBeNull();
        product.Id.Should().NotBeEmpty();
        product.Name.Should().NotBeNullOrWhiteSpace();
        product.Slug.Should().NotBeNullOrWhiteSpace();
        product.Price.Should().BeGreaterThan(0);
        product.StockQuantity.Should().BeGreaterThanOrEqualTo(0);
        product.IsActive.Should().BeTrue();
    }

    [TestMethod]
    public void CreatePromoCode_ReturnsValidPromoCode()
    {
        // Act
        var promoCode = TestDataFactory.CreatePromoCode();

        // Assert
        promoCode.Should().NotBeNull();
        promoCode.Id.Should().NotBeEmpty();
        promoCode.Code.Value.Should().NotBeNullOrWhiteSpace();
        promoCode.Discount.Type.Should().Be(ECommerce.Promotions.Domain.Enums.DiscountType.Percentage);
        promoCode.Discount.Amount.Should().Be(10);
        promoCode.IsActive.Should().BeTrue();
    }

    [TestMethod]
    public void CreateOrder_ReturnsValidOrder()
    {
        // Arrange
        var userId = Guid.NewGuid();

        // Act
        var order = TestDataFactory.CreateOrder(userId);

        // Assert
        order.Should().NotBeNull();
        order.Id.Should().NotBeEmpty();
        order.UserId.Should().Be(userId);
        order.OrderNumber.Should().NotBeNullOrWhiteSpace();
        order.TotalAmount.Should().BeGreaterThan(0);
        order.Status.Should().Be(OrderStatus.Pending);
        order.PaymentStatus.Should().Be(PaymentStatus.Pending);
    }

    [TestMethod]
    public void CreateCategory_ReturnsValidCategory()
    {
        // Act
        var category = TestDataFactory.CreateCategory();

        // Assert
        category.Should().NotBeNull();
        category.Id.Should().NotBeEmpty();
        category.Name.Should().NotBeNullOrWhiteSpace();
        category.Slug.Should().NotBeNullOrWhiteSpace();
        category.IsActive.Should().BeTrue();
    }
}
