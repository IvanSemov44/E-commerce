using ECommerce.Identity.Domain.ValueObjects;
using ECommerce.Identity.Domain.Errors;

namespace ECommerce.Identity.Tests.Domain;

[TestClass]
public class ValueObjectTests
{
    // ── Email ─────────────────────────────────────────────────────────────────

    [TestMethod]
    public void Email_EmptyString_ReturnsFailure()
    {
        var result = Email.Create("");
        Assert.IsFalse(result.IsSuccess);
        Assert.AreEqual(IdentityErrors.EmailEmpty.Code, result.GetErrorOrThrow().Code);
    }

    [TestMethod]
    public void Email_Whitespace_ReturnsFailure()
    {
        var result = Email.Create("   ");
        Assert.IsFalse(result.IsSuccess);
    }

    [TestMethod]
    public void Email_NoAtSign_ReturnsFailure()
    {
        var result = Email.Create("notanemail");
        Assert.IsFalse(result.IsSuccess);
        Assert.AreEqual(IdentityErrors.EmailInvalid.Code, result.GetErrorOrThrow().Code);
    }

    [TestMethod]
    public void Email_NoDotAfterAt_ReturnsFailure()
    {
        var result = Email.Create("user@nodot");
        Assert.IsFalse(result.IsSuccess);
    }

    [TestMethod]
    public void Email_ExceedsMaxLength_ReturnsFailure()
    {
        var result = Email.Create(new string('a', 252) + "@b.com"); // 252 + 5 = 257 > 256
        Assert.IsFalse(result.IsSuccess);
        Assert.AreEqual(IdentityErrors.EmailTooLong.Code, result.GetErrorOrThrow().Code);
    }

    [TestMethod]
    public void Email_ValidInput_NormalizesToLowercase()
    {
        var result = Email.Create("  User@EXAMPLE.COM  ");
        Assert.IsTrue(result.IsSuccess);
        Assert.AreEqual("user@example.com", result.GetDataOrThrow().Value);
    }

    [TestMethod]
    public void Email_SameValueEmails_AreEqual()
    {
        var a = Email.Create("user@example.com").GetDataOrThrow();
        var b = Email.Create("USER@EXAMPLE.COM").GetDataOrThrow();
        Assert.AreEqual(a, b);
    }

    [TestMethod]
    public void Email_DifferentValues_AreNotEqual()
    {
        var a = Email.Create("alice@example.com").GetDataOrThrow();
        var b = Email.Create("bob@example.com").GetDataOrThrow();
        Assert.AreNotEqual(a, b);
    }

    // ── PersonName ────────────────────────────────────────────────────────────

    [TestMethod]
    public void PersonName_EmptyFirstName_ReturnsFailure()
    {
        var result = PersonName.Create("", "Doe");
        Assert.IsFalse(result.IsSuccess);
        Assert.AreEqual(IdentityErrors.NameFirstEmpty.Code, result.GetErrorOrThrow().Code);
    }

    [TestMethod]
    public void PersonName_EmptyLastName_ReturnsFailure()
    {
        var result = PersonName.Create("John", "");
        Assert.IsFalse(result.IsSuccess);
        Assert.AreEqual(IdentityErrors.NameLastEmpty.Code, result.GetErrorOrThrow().Code);
    }

    [TestMethod]
    public void PersonName_FirstNameTooLong_ReturnsFailure()
    {
        var result = PersonName.Create(new string('a', 101), "Doe");
        Assert.IsFalse(result.IsSuccess);
        Assert.AreEqual(IdentityErrors.NameTooLong.Code, result.GetErrorOrThrow().Code);
    }

    [TestMethod]
    public void PersonName_ValidInput_TrimsWhitespace()
    {
        var result = PersonName.Create("  John  ", "  Doe  ");
        Assert.IsTrue(result.IsSuccess);
        var name = result.GetDataOrThrow();
        Assert.AreEqual("John", name.First);
        Assert.AreEqual("Doe",  name.Last);
        Assert.AreEqual("John Doe", name.FullName);
    }

    [TestMethod]
    public void PersonName_SameValues_AreEqual()
    {
        var a = PersonName.Create("John", "Doe").GetDataOrThrow();
        var b = PersonName.Create("john", "doe").GetDataOrThrow(); // case-insensitive equality
        Assert.AreEqual(a, b);
    }

    [TestMethod]
    public void PersonName_DifferentValues_AreNotEqual()
    {
        var a = PersonName.Create("John", "Doe").GetDataOrThrow();
        var b = PersonName.Create("Jane", "Doe").GetDataOrThrow();
        Assert.AreNotEqual(a, b);
    }

    // ── PasswordHash ──────────────────────────────────────────────────────────

    [TestMethod]
    public void PasswordHash_FromEmptyHash_ReturnsFailure()
    {
        var result = PasswordHash.FromHash("");
        Assert.IsFalse(result.IsSuccess);
        Assert.AreEqual(IdentityErrors.PasswordHashEmpty.Code, result.GetErrorOrThrow().Code);
    }

    [TestMethod]
    public void PasswordHash_FromValidHash_Succeeds()
    {
        var result = PasswordHash.FromHash("$2a$12$some.fake.bcrypt.hash.here");
        Assert.IsTrue(result.IsSuccess);
        Assert.AreEqual("$2a$12$some.fake.bcrypt.hash.here", result.GetDataOrThrow().Hash);
    }

    [TestMethod]
    public void PasswordHash_ValidateRawPassword_EmptyFails()
    {
        var result = PasswordHash.ValidateRawPassword("");
        Assert.IsFalse(result.IsSuccess);
        Assert.AreEqual(IdentityErrors.PasswordEmpty.Code, result.GetErrorOrThrow().Code);
    }

    [TestMethod]
    public void PasswordHash_ValidateRawPassword_TooShortFails()
    {
        var result = PasswordHash.ValidateRawPassword("Ab1");
        Assert.IsFalse(result.IsSuccess);
        Assert.AreEqual(IdentityErrors.PasswordTooShort.Code, result.GetErrorOrThrow().Code);
    }

    [TestMethod]
    public void PasswordHash_ValidateRawPassword_NoUppercaseFails()
    {
        var result = PasswordHash.ValidateRawPassword("password1");
        Assert.IsFalse(result.IsSuccess);
        Assert.AreEqual(IdentityErrors.PasswordNoUpper.Code, result.GetErrorOrThrow().Code);
    }

    [TestMethod]
    public void PasswordHash_ValidateRawPassword_NoDigitFails()
    {
        var result = PasswordHash.ValidateRawPassword("PasswordOnly");
        Assert.IsFalse(result.IsSuccess);
        Assert.AreEqual(IdentityErrors.PasswordNoDigit.Code, result.GetErrorOrThrow().Code);
    }

    [TestMethod]
    public void PasswordHash_ValidateRawPassword_ValidPasses()
    {
        var result = PasswordHash.ValidateRawPassword("SecurePass1");
        Assert.IsTrue(result.IsSuccess);
    }
}
