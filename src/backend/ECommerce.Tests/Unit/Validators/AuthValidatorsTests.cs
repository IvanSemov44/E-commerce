using FluentValidation.TestHelper;
using ECommerce.Application.DTOs.Auth;
using ECommerce.Application.Validators.Auth;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ECommerce.Tests.Unit.Validators;

[TestClass]
public class AuthValidatorsTests
{
    private RegisterDtoValidator _registerValidator = null!;
    private LoginDtoValidator _loginValidator = null!;

    [TestInitialize]
    public void Setup()
    {
        _registerValidator = new RegisterDtoValidator();
        _loginValidator = new LoginDtoValidator();
    }

    [TestMethod]
    public void Register_Should_Have_Errors_On_Invalid_Dto()
    {
        var dto = new RegisterDto { Email = "", Password = "short", FirstName = "", LastName = "" };
        var result = _registerValidator.TestValidate(dto);
        result.ShouldHaveValidationErrorFor(x => x.Email);
        result.ShouldHaveValidationErrorFor(x => x.Password);
        result.ShouldHaveValidationErrorFor(x => x.FirstName);
        result.ShouldHaveValidationErrorFor(x => x.LastName);
    }

    [TestMethod]
    public void Login_Should_Have_Error_When_Password_Empty()
    {
        var dto = new LoginDto { Email = "user@example.com", Password = "" };
        var result = _loginValidator.TestValidate(dto);
        result.ShouldHaveValidationErrorFor(x => x.Password);
    }

    [TestMethod]
    public void Register_Should_Pass_Valid_Dto()
    {
        var dto = new RegisterDto { Email = "test@example.com", Password = "Valid1234", FirstName = "John", LastName = "Doe" };
        var result = _registerValidator.TestValidate(dto);
        result.ShouldNotHaveAnyValidationErrors();
    }
}
