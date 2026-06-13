using FluentValidation.TestHelper;
using ECommerce.Contracts.DTOs.Auth;
using ECommerce.Contracts.Validators.Auth;

namespace ECommerce.Identity.Tests.Validators;

// ── RegisterDtoValidator ─────────────────────────────────────────────────────

[TestClass]
public class RegisterDtoValidatorTests
{
    private readonly RegisterDtoValidator _v = new();

    [TestMethod]
    public void Email_Empty_HasError() =>
        _v.TestValidate(new RegisterDto { Email = "", Password = "Valid1234", FirstName = "John", LastName = "Doe" })
          .ShouldHaveValidationErrorFor(x => x.Email);

    [TestMethod]
    public void Email_InvalidFormat_HasError() =>
        _v.TestValidate(new RegisterDto { Email = "not-an-email", Password = "Valid1234", FirstName = "John", LastName = "Doe" })
          .ShouldHaveValidationErrorFor(x => x.Email);

    [TestMethod]
    public void Password_Empty_HasError() =>
        _v.TestValidate(new RegisterDto { Email = "a@b.com", Password = "", FirstName = "John", LastName = "Doe" })
          .ShouldHaveValidationErrorFor(x => x.Password);

    [TestMethod]
    public void Password_TooShort_HasError() =>
        _v.TestValidate(new RegisterDto { Email = "a@b.com", Password = "Abc123a", FirstName = "John", LastName = "Doe" })
          .ShouldHaveValidationErrorFor(x => x.Password)
          .WithErrorMessage("Password must be at least 8 characters");

    [TestMethod]
    public void Password_NoUppercase_HasError() =>
        _v.TestValidate(new RegisterDto { Email = "a@b.com", Password = "abcdef12", FirstName = "John", LastName = "Doe" })
          .ShouldHaveValidationErrorFor(x => x.Password)
          .WithErrorMessage("Password must contain an uppercase letter");

    [TestMethod]
    public void Password_NoLowercase_HasError() =>
        _v.TestValidate(new RegisterDto { Email = "a@b.com", Password = "ABCDEF12", FirstName = "John", LastName = "Doe" })
          .ShouldHaveValidationErrorFor(x => x.Password)
          .WithErrorMessage("Password must contain a lowercase letter");

    [TestMethod]
    public void Password_NoDigit_HasError() =>
        _v.TestValidate(new RegisterDto { Email = "a@b.com", Password = "Abcdefgh", FirstName = "John", LastName = "Doe" })
          .ShouldHaveValidationErrorFor(x => x.Password)
          .WithErrorMessage("Password must contain a digit");

    [TestMethod]
    public void FirstName_Empty_HasError() =>
        _v.TestValidate(new RegisterDto { Email = "a@b.com", Password = "Valid1234", FirstName = "", LastName = "Doe" })
          .ShouldHaveValidationErrorFor(x => x.FirstName);

    [TestMethod]
    public void FirstName_TooLong_HasError() =>
        _v.TestValidate(new RegisterDto { Email = "a@b.com", Password = "Valid1234", FirstName = new string('A', 51), LastName = "Doe" })
          .ShouldHaveValidationErrorFor(x => x.FirstName);

    [TestMethod]
    public void LastName_Empty_HasError() =>
        _v.TestValidate(new RegisterDto { Email = "a@b.com", Password = "Valid1234", FirstName = "John", LastName = "" })
          .ShouldHaveValidationErrorFor(x => x.LastName);

    [TestMethod]
    public void LastName_TooLong_HasError() =>
        _v.TestValidate(new RegisterDto { Email = "a@b.com", Password = "Valid1234", FirstName = "John", LastName = new string('A', 51) })
          .ShouldHaveValidationErrorFor(x => x.LastName);

    [TestMethod]
    public void ValidDto_NoErrors() =>
        _v.TestValidate(new RegisterDto { Email = "test@example.com", Password = "Valid1234", FirstName = "John", LastName = "Doe" })
          .ShouldNotHaveAnyValidationErrors();
}

// ── LoginDtoValidator ────────────────────────────────────────────────────────

[TestClass]
public class LoginDtoValidatorTests
{
    private readonly LoginDtoValidator _v = new();

    [TestMethod]
    public void Email_Empty_HasError() =>
        _v.TestValidate(new LoginDto { Email = "", Password = "pass" })
          .ShouldHaveValidationErrorFor(x => x.Email);

    [TestMethod]
    public void Email_InvalidFormat_HasError() =>
        _v.TestValidate(new LoginDto { Email = "not-an-email", Password = "pass" })
          .ShouldHaveValidationErrorFor(x => x.Email);

    [TestMethod]
    public void Password_Empty_HasError() =>
        _v.TestValidate(new LoginDto { Email = "a@b.com", Password = "" })
          .ShouldHaveValidationErrorFor(x => x.Password);

    [TestMethod]
    public void ValidDto_NoErrors() =>
        _v.TestValidate(new LoginDto { Email = "user@example.com", Password = "anypassword" })
          .ShouldNotHaveAnyValidationErrors();
}

// ── ForgotPasswordDtoValidator ───────────────────────────────────────────────

[TestClass]
public class ForgotPasswordDtoValidatorTests
{
    private readonly ForgotPasswordDtoValidator _v = new();

    [TestMethod]
    public void Email_Empty_HasError() =>
        _v.TestValidate(new ForgotPasswordDto { Email = "" })
          .ShouldHaveValidationErrorFor(x => x.Email);

    [TestMethod]
    public void Email_InvalidFormat_HasError() =>
        _v.TestValidate(new ForgotPasswordDto { Email = "not-an-email" })
          .ShouldHaveValidationErrorFor(x => x.Email);

    [TestMethod]
    public void ValidEmail_NoErrors() =>
        _v.TestValidate(new ForgotPasswordDto { Email = "user@example.com" })
          .ShouldNotHaveAnyValidationErrors();
}

// ── ResetPasswordDtoValidator ────────────────────────────────────────────────

[TestClass]
public class ResetPasswordDtoValidatorTests
{
    private readonly ResetPasswordDtoValidator _v = new();

    private static ResetPasswordDto Make(string email = "user@example.com", string token = "validtoken1234", string password = "NewPass1") =>
        new() { Email = email, Token = token, NewPassword = password };

    [TestMethod]
    public void Email_Empty_HasError() =>
        _v.TestValidate(Make(email: ""))
          .ShouldHaveValidationErrorFor(x => x.Email);

    [TestMethod]
    public void Email_InvalidFormat_HasError() =>
        _v.TestValidate(Make(email: "not-an-email"))
          .ShouldHaveValidationErrorFor(x => x.Email);

    [TestMethod]
    public void Token_Empty_HasError() =>
        _v.TestValidate(Make(token: ""))
          .ShouldHaveValidationErrorFor(x => x.Token);

    [TestMethod]
    public void Token_TooShort_HasError() =>
        _v.TestValidate(Make(token: "short123"))
          .ShouldHaveValidationErrorFor(x => x.Token);

    [TestMethod]
    public void NewPassword_Empty_HasError() =>
        _v.TestValidate(Make(password: ""))
          .ShouldHaveValidationErrorFor(x => x.NewPassword);

    [TestMethod]
    public void NewPassword_TooShort_HasError() =>
        _v.TestValidate(Make(password: "Abc123a"))
          .ShouldHaveValidationErrorFor(x => x.NewPassword)
          .WithErrorMessage("Password must be at least 8 characters");

    [TestMethod]
    public void NewPassword_NoUppercase_HasError() =>
        _v.TestValidate(Make(password: "abcdef12"))
          .ShouldHaveValidationErrorFor(x => x.NewPassword)
          .WithErrorMessage("Password must contain at least one uppercase letter");

    [TestMethod]
    public void NewPassword_NoLowercase_HasError() =>
        _v.TestValidate(Make(password: "ABCDEF12"))
          .ShouldHaveValidationErrorFor(x => x.NewPassword)
          .WithErrorMessage("Password must contain at least one lowercase letter");

    [TestMethod]
    public void NewPassword_NoDigit_HasError() =>
        _v.TestValidate(Make(password: "Abcdefgh"))
          .ShouldHaveValidationErrorFor(x => x.NewPassword)
          .WithErrorMessage("Password must contain at least one digit");

    [TestMethod]
    public void ValidDto_NoErrors() =>
        _v.TestValidate(Make()).ShouldNotHaveAnyValidationErrors();
}

// ── RefreshTokenDtoValidator ─────────────────────────────────────────────────

[TestClass]
public class RefreshTokenDtoValidatorTests
{
    private readonly RefreshTokenDtoValidator _v = new();

    [TestMethod]
    public void Token_Null_HasError() =>
        _v.TestValidate(new RefreshTokenDto { Token = null! })
          .ShouldHaveValidationErrorFor(x => x.Token);

    [TestMethod]
    public void Token_ShortNonEmpty_HasError() =>
        // MinimumLength(10) fires only when token is provided but non-empty; "abc12" is non-empty but <10
        _v.TestValidate(new RefreshTokenDto { Token = "abc12" })
          .ShouldHaveValidationErrorFor(x => x.Token);

    [TestMethod]
    public void Token_EmptyString_NoErrors() =>
        // Empty string intentionally passes validator — service returns 401 for invalid tokens
        _v.TestValidate(new RefreshTokenDto { Token = "" })
          .ShouldNotHaveAnyValidationErrors();

    [TestMethod]
    public void ValidToken_NoErrors() =>
        _v.TestValidate(new RefreshTokenDto { Token = "validrefreshtoken" })
          .ShouldNotHaveAnyValidationErrors();
}

// ── VerifyEmailDtoValidator ──────────────────────────────────────────────────

[TestClass]
public class VerifyEmailDtoValidatorTests
{
    private readonly VerifyEmailDtoValidator _v = new();

    [TestMethod]
    public void UserId_Empty_HasError() =>
        _v.TestValidate(new VerifyEmailDto { UserId = Guid.Empty, Token = "validtoken12" })
          .ShouldHaveValidationErrorFor(x => x.UserId);

    [TestMethod]
    public void Token_Empty_HasError() =>
        _v.TestValidate(new VerifyEmailDto { UserId = Guid.NewGuid(), Token = "" })
          .ShouldHaveValidationErrorFor(x => x.Token);

    [TestMethod]
    public void Token_TooShort_HasError() =>
        _v.TestValidate(new VerifyEmailDto { UserId = Guid.NewGuid(), Token = "short123" })
          .ShouldHaveValidationErrorFor(x => x.Token);

    [TestMethod]
    public void ValidDto_NoErrors() =>
        _v.TestValidate(new VerifyEmailDto { UserId = Guid.NewGuid(), Token = "validtoken12" })
          .ShouldNotHaveAnyValidationErrors();
}
