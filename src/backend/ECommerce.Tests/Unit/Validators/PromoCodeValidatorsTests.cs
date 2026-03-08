using FluentValidation.TestHelper;
using ECommerce.Application.DTOs.PromoCodes;
using ECommerce.Application.Validators.PromoCodes;
using FluentAssertions;
using System.Text.Json;

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

    #region ValidatePromoCodeRequestDto Tests

    /// <summary>
    /// Tests for ValidatePromoCodeRequestDto validator.
    /// Focuses on the JSON deserialization with camelCase property names.
    /// </summary>
    [TestMethod]
    public void ValidatePromoCodeRequestDto_Should_Accept_ZeroOrderAmount()
    {
        // Arrange
        var validator = new ValidatePromoCodeRequestDtoValidator();
        var request = new ValidatePromoCodeRequestDto { Code = "SAVE20", OrderAmount = 0m };

        // Act
        var result = validator.TestValidate(request);

        // Assert
        // OrderAmount >= 0 should be valid now (previously required > 0)
        result.ShouldNotHaveValidationErrorFor(x => x.OrderAmount);
    }

    [TestMethod]
    public void ValidatePromoCodeRequestDto_Should_Accept_PositiveOrderAmount()
    {
        // Arrange
        var validator = new ValidatePromoCodeRequestDtoValidator();
        var request = new ValidatePromoCodeRequestDto { Code = "SAVE20", OrderAmount = 100m };

        // Act
        var result = validator.TestValidate(request);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.OrderAmount);
    }

    [TestMethod]
    public void ValidatePromoCodeRequestDto_Should_Fail_With_NegativeOrderAmount()
    {
        // Arrange
        var validator = new ValidatePromoCodeRequestDtoValidator();
        var request = new ValidatePromoCodeRequestDto { Code = "SAVE20", OrderAmount = -10m };

        // Act
        var result = validator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.OrderAmount);
    }

    [TestMethod]
    public void ValidatePromoCodeRequestDto_Should_Fail_When_Code_Empty()
    {
        // Arrange
        var validator = new ValidatePromoCodeRequestDtoValidator();
        var request = new ValidatePromoCodeRequestDto { Code = "", OrderAmount = 100m };

        // Act
        var result = validator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Code);
    }

    [TestMethod]
    public void ValidatePromoCodeRequestDto_Should_Fail_When_Code_Exceeds_MaxLength()
    {
        // Arrange
        var validator = new ValidatePromoCodeRequestDtoValidator();
        var request = new ValidatePromoCodeRequestDto { Code = new string('A', 51), OrderAmount = 100m };

        // Act
        var result = validator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Code);
    }

    [TestMethod]
    public void ValidatePromoCodeRequestDto_Should_Fail_When_Code_Contains_Lowercase()
    {
        // Arrange
        var validator = new ValidatePromoCodeRequestDtoValidator();
        var request = new ValidatePromoCodeRequestDto { Code = "save20", OrderAmount = 100m };

        // Act
        var result = validator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Code);
    }

    [TestMethod]
    public void ValidatePromoCodeRequestDto_Should_Fail_When_Code_Contains_Special_Chars()
    {
        // Arrange
        var validator = new ValidatePromoCodeRequestDtoValidator();
        var request = new ValidatePromoCodeRequestDto { Code = "SAVE@20", OrderAmount = 100m };

        // Act
        var result = validator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Code);
    }

    [TestMethod]
    [DataRow("SAVE20")]
    [DataRow("CODE-2024")]
    [DataRow("PROMO100")]
    [DataRow("ABC123")]
    public void ValidatePromoCodeRequestDto_Should_Accept_Valid_Code(string code)
    {
        // Arrange
        var validator = new ValidatePromoCodeRequestDtoValidator();
        var request = new ValidatePromoCodeRequestDto { Code = code, OrderAmount = 100m };

        // Act
        var result = validator.TestValidate(request);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.Code);
    }

    [TestMethod]
    public void ValidatePromoCodeRequestDto_Should_Use_Default_OrderAmount_When_Omitted()
    {
        // Arrange
        var validator = new ValidatePromoCodeRequestDtoValidator();
        var request = new ValidatePromoCodeRequestDto { Code = "SAVE20" }; // OrderAmount defaults to 0m

        // Act
        var result = validator.TestValidate(request);

        // Assert
        // Default value of 0m should pass validation
        result.ShouldNotHaveValidationErrorFor(x => x.OrderAmount);
    }

    #endregion

    #region JSON Deserialization Tests

    /// <summary>
    /// Tests for JSON deserialization with [JsonPropertyName] attributes.
    /// Verifies the fix for frontend camelCase JSON compatibility.
    /// </summary>
    [TestMethod]
    public void ValidatePromoCodeRequestDto_Should_Deserialize_CamelCaseJson()
    {
        // Arrange
        var jsonString = "{\"code\":\"SAVE20\",\"orderAmount\":100}";
        var options = new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = false };

        // Act
        var deserialized = System.Text.Json.JsonSerializer.Deserialize<ValidatePromoCodeRequestDto>(jsonString, options);

        // Assert
        Assert.IsNotNull(deserialized, "Should deserialize camelCase JSON successfully");
        Assert.AreEqual("SAVE20", deserialized.Code, "Code should be deserialized correctly");
        Assert.AreEqual(100m, deserialized.OrderAmount, "OrderAmount should be deserialized correctly");
    }

    [TestMethod]
    public void ValidatePromoCodeRequestDto_Should_Deserialize_PartialJson()
    {
        // Arrange
        var jsonString = "{\"code\":\"SAVE20\"}";
        var options = new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = false };

        // Act
        var deserialized = System.Text.Json.JsonSerializer.Deserialize<ValidatePromoCodeRequestDto>(jsonString, options);

        // Assert
        Assert.IsNotNull(deserialized, "Should deserialize partial JSON");
        Assert.AreEqual("SAVE20", deserialized.Code);
        Assert.AreEqual(0m, deserialized.OrderAmount, "OrderAmount should default to 0m");
    }

    #endregion
}
