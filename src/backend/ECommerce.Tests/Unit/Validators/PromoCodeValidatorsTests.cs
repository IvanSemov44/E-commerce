using FluentValidation.TestHelper;
using ECommerce.Application.DTOs.PromoCodes;
using ECommerce.Application.Validators.PromoCodes;
using FluentAssertions;

namespace ECommerce.Tests.Unit.Validators;

/// <summary>
/// Unit tests for promo code validators.
/// Tests CreatePromoCodeDtoValidator.
/// </summary>
[TestClass]
public class PromoCodeValidatorsTests
{
    private CreatePromoCodeDtoValidator _createValidator = null!;

    [TestInitialize]
    public void Setup()
    {
        _createValidator = new CreatePromoCodeDtoValidator();
    }

    #region Code Validation Tests

    [TestMethod]
    public void CreatePromoCode_Should_Fail_When_Code_Empty()
    {
        // Arrange
        var dto = new CreatePromoCodeDto { Code = "", DiscountType = "percentage", DiscountValue = 10, StartDate = DateTime.UtcNow };

        // Act
        var result = _createValidator.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Code);
    }

    [TestMethod]
    public void CreatePromoCode_Should_Fail_When_Code_Exceeds_MaxLength()
    {
        // Arrange
        var dto = new CreatePromoCodeDto
        {
            Code = new string('A', 51),
            DiscountType = "percentage",
            DiscountValue = 10,
            StartDate = DateTime.UtcNow
        };

        // Act
        var result = _createValidator.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Code);
    }

    [TestMethod]
    public void CreatePromoCode_Should_Fail_When_Code_Contains_Lowercase()
    {
        // Arrange
        var dto = new CreatePromoCodeDto { Code = "CODE2024lower", DiscountType = "percentage", DiscountValue = 10, StartDate = DateTime.UtcNow };

        // Act
        var result = _createValidator.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Code);
    }

    [TestMethod]
    public void CreatePromoCode_Should_Fail_When_Code_Contains_Special_Characters()
    {
        // Arrange
        var dto = new CreatePromoCodeDto { Code = "CODE-2024!", DiscountType = "percentage", DiscountValue = 10, StartDate = DateTime.UtcNow };

        // Act
        var result = _createValidator.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Code);
    }

    [TestMethod]
    [DataRow("CODE2024")]
    [DataRow("PROMO100")]
    [DataRow("SAVE20")]
    [DataRow("ABC123XYZ")]
    public void CreatePromoCode_Should_Pass_With_Valid_Code(string code)
    {
        // Arrange
        var dto = new CreatePromoCodeDto { Code = code, DiscountType = "percentage", DiscountValue = 10, StartDate = DateTime.UtcNow };

        // Act
        var result = _createValidator.TestValidate(dto);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.Code);
    }

    #endregion

    #region Discount Type Validation Tests

    [TestMethod]
    public void CreatePromoCode_Should_Fail_When_DiscountType_Empty()
    {
        // Arrange
        var dto = new CreatePromoCodeDto { Code = "CODE2024", DiscountType = "", DiscountValue = 10, StartDate = DateTime.UtcNow };

        // Act
        var result = _createValidator.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.DiscountType);
    }

    [TestMethod]
    public void CreatePromoCode_Should_Fail_When_DiscountType_Invalid()
    {
        // Arrange
        var dto = new CreatePromoCodeDto { Code = "CODE2024", DiscountType = "invalid", DiscountValue = 10, StartDate = DateTime.UtcNow };

        // Act
        var result = _createValidator.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.DiscountType);
    }

    [TestMethod]
    [DataRow("percentage")]
    [DataRow("fixed")]
    public void CreatePromoCode_Should_Pass_With_Valid_DiscountType(string discountType)
    {
        // Arrange
        var dto = new CreatePromoCodeDto { Code = "CODE2024", DiscountType = discountType, DiscountValue = 10, StartDate = DateTime.UtcNow };

        // Act
        var result = _createValidator.TestValidate(dto);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.DiscountType);
    }

    #endregion

    #region Discount Value Validation Tests

    [TestMethod]
    public void CreatePromoCode_Should_Fail_When_DiscountValue_Zero()
    {
        // Arrange
        var dto = new CreatePromoCodeDto { Code = "CODE2024", DiscountType = "percentage", DiscountValue = 0, StartDate = DateTime.UtcNow };

        // Act
        var result = _createValidator.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.DiscountValue);
    }

    [TestMethod]
    public void CreatePromoCode_Should_Fail_When_DiscountValue_Negative()
    {
        // Arrange
        var dto = new CreatePromoCodeDto { Code = "CODE2024", DiscountType = "percentage", DiscountValue = -5, StartDate = DateTime.UtcNow };

        // Act
        var result = _createValidator.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.DiscountValue);
    }

    [TestMethod]
    public void CreatePromoCode_Should_Fail_When_PercentageDiscount_Exceeds_100()
    {
        // Arrange
        var dto = new CreatePromoCodeDto { Code = "CODE2024", DiscountType = "percentage", DiscountValue = 150, StartDate = DateTime.UtcNow };

        // Act
        var result = _createValidator.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.DiscountValue);
    }

    [TestMethod]
    public void CreatePromoCode_Should_Pass_With_100_Percent_Discount()
    {
        // Arrange
        var dto = new CreatePromoCodeDto { Code = "FREESTUFF", DiscountType = "percentage", DiscountValue = 100, StartDate = DateTime.UtcNow };

        // Act
        var result = _createValidator.TestValidate(dto);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [TestMethod]
    public void CreatePromoCode_Should_Pass_With_Fixed_Discount_Over_100()
    {
        // Arrange
        var dto = new CreatePromoCodeDto { Code = "BIGSAVE", DiscountType = "fixed", DiscountValue = 500, StartDate = DateTime.UtcNow };

        // Act
        var result = _createValidator.TestValidate(dto);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.DiscountValue);
    }

    #endregion

    #region Optional Fields Validation Tests

    [TestMethod]
    public void CreatePromoCode_Should_Fail_When_MinOrderAmount_Negative()
    {
        // Arrange
        var dto = new CreatePromoCodeDto
        {
            Code = "CODE2024",
            DiscountType = "percentage",
            DiscountValue = 10,
            MinOrderAmount = -100,
            StartDate = DateTime.UtcNow
        };

        // Act
        var result = _createValidator.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.MinOrderAmount);
    }

    [TestMethod]
    public void CreatePromoCode_Should_Pass_With_Zero_MinOrderAmount()
    {
        // Arrange
        var dto = new CreatePromoCodeDto
        {
            Code = "CODE2024",
            DiscountType = "percentage",
            DiscountValue = 10,
            MinOrderAmount = 0,
            StartDate = DateTime.UtcNow
        };

        // Act
        var result = _createValidator.TestValidate(dto);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.MinOrderAmount);
    }

    [TestMethod]
    public void CreatePromoCode_Should_Pass_With_Null_MinOrderAmount()
    {
        // Arrange
        var dto = new CreatePromoCodeDto
        {
            Code = "CODE2024",
            DiscountType = "percentage",
            DiscountValue = 10,
            MinOrderAmount = null,
            StartDate = DateTime.UtcNow
        };

        // Act
        var result = _createValidator.TestValidate(dto);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.MinOrderAmount);
    }

    [TestMethod]
    public void CreatePromoCode_Should_Fail_When_MaxDiscountAmount_Zero()
    {
        // Arrange
        var dto = new CreatePromoCodeDto
        {
            Code = "CODE2024",
            DiscountType = "percentage",
            DiscountValue = 50,
            MaxDiscountAmount = 0,
            StartDate = DateTime.UtcNow
        };

        // Act
        var result = _createValidator.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.MaxDiscountAmount);
    }

    [TestMethod]
    public void CreatePromoCode_Should_Fail_When_MaxUses_Zero()
    {
        // Arrange
        var dto = new CreatePromoCodeDto
        {
            Code = "CODE2024",
            DiscountType = "percentage",
            DiscountValue = 10,
            MaxUses = 0,
            StartDate = DateTime.UtcNow
        };

        // Act
        var result = _createValidator.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.MaxUses);
    }

    #endregion

    #region Date Validation Tests

    [TestMethod]
    public void CreatePromoCode_Should_Fail_When_EndDate_Before_StartDate()
    {
        // Arrange
        var now = DateTime.UtcNow;
        var dto = new CreatePromoCodeDto
        {
            Code = "CODE2024",
            DiscountType = "percentage",
            DiscountValue = 10,
            StartDate = now,
            EndDate = now.AddDays(-1)
        };

        // Act
        var result = _createValidator.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.EndDate);
    }

    [TestMethod]
    public void CreatePromoCode_Should_Fail_When_EndDate_Equals_StartDate()
    {
        // Arrange
        var now = DateTime.UtcNow;
        var dto = new CreatePromoCodeDto
        {
            Code = "CODE2024",
            DiscountType = "percentage",
            DiscountValue = 10,
            StartDate = now,
            EndDate = now
        };

        // Act
        var result = _createValidator.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.EndDate);
    }

    [TestMethod]
    public void CreatePromoCode_Should_Pass_With_EndDate_After_StartDate()
    {
        // Arrange
        var now = DateTime.UtcNow;
        var dto = new CreatePromoCodeDto
        {
            Code = "CODE2024",
            DiscountType = "percentage",
            DiscountValue = 10,
            StartDate = now,
            EndDate = now.AddDays(30)
        };

        // Act
        var result = _createValidator.TestValidate(dto);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.EndDate);
    }

    [TestMethod]
    public void CreatePromoCode_Should_Pass_With_Null_EndDate()
    {
        // Arrange
        var dto = new CreatePromoCodeDto
        {
            Code = "CODE2024",
            DiscountType = "percentage",
            DiscountValue = 10,
            StartDate = DateTime.UtcNow,
            EndDate = null
        };

        // Act
        var result = _createValidator.TestValidate(dto);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.EndDate);
    }

    #endregion

    #region Full DTO Validation Tests

    [TestMethod]
    public void CreatePromoCode_Should_Pass_With_All_Valid_Data()
    {
        // Arrange
        var now = DateTime.UtcNow;
        var dto = new CreatePromoCodeDto
        {
            Code = "SAVE20PERCENT",
            DiscountType = "percentage",
            DiscountValue = 20,
            MinOrderAmount = 50,
            MaxDiscountAmount = 100,
            MaxUses = 1000,
            StartDate = now,
            EndDate = now.AddDays(90)
        };

        // Act
        var result = _createValidator.TestValidate(dto);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [TestMethod]
    public void CreatePromoCode_Should_Pass_With_Minimal_Valid_Data()
    {
        // Arrange
        var dto = new CreatePromoCodeDto
        {
            Code = "CODE2024",
            DiscountType = "fixed",
            DiscountValue = 10,
            StartDate = DateTime.UtcNow
        };

        // Act
        var result = _createValidator.TestValidate(dto);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    #endregion
}
