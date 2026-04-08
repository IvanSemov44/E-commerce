using ECommerce.Identity.Domain.Aggregates.User;
using ECommerce.Identity.Domain.Errors;
using ECommerce.Identity.Domain.Events;
using ECommerce.Identity.Domain.ValueObjects;
using ECommerce.SharedKernel.Enums;

namespace ECommerce.Identity.Tests.Domain;

[TestClass]
public class UserTests
{
    // ── Helpers ───────────────────────────────────────────────────────────────

    private static PasswordHash MakeHash(string hash = "$2a$12$fakehash")
        => PasswordHash.FromHash(hash).GetDataOrThrow();

    // ── Register ──────────────────────────────────────────────────────────────

    [TestMethod]
    public void Register_ValidInput_CreatesUserWithCustomerRole()
    {
        var userResult = User.Register("test@example.com", "John", "Doe", MakeHash());

        Assert.IsTrue(userResult.IsSuccess);
        var user = userResult.GetDataOrThrow();
        Assert.AreEqual(UserRole.Customer, user.Role);
        Assert.AreEqual("test@example.com", user.Email.Value);
        Assert.AreEqual("John", user.Name.First);
        Assert.IsFalse(user.IsEmailVerified);
        Assert.IsNotNull(user.EmailVerificationToken);
    }

    [TestMethod]
    public void Register_RaisesUserRegisteredEvent()
    {
        var userResult = User.Register("test@example.com", "John", "Doe", MakeHash());
        var user = userResult.GetDataOrThrow();

        var events = user.DomainEvents.ToList();
        Assert.HasCount(1, events);
        Assert.IsInstanceOfType<UserRegisteredEvent>(events[0]);

        var evt = (UserRegisteredEvent)events[0];
        Assert.AreEqual(user.Id, evt.UserId);
        Assert.AreEqual("test@example.com", evt.Email);
    }

    [TestMethod]
    public void Register_NewUser_HasNoAddresses()
    {
        var userResult = User.Register("test@example.com", "John", "Doe", MakeHash());
        var user = userResult.GetDataOrThrow();
        Assert.IsEmpty(user.Addresses);
    }

    [TestMethod]
    public void Register_InvalidEmail_ReturnsFailure()
    {
        var result = User.Register("not-an-email", "John", "Doe", MakeHash());
        Assert.IsFalse(result.IsSuccess);
    }

    [TestMethod]
    public void Register_EmptyFirstName_ReturnsFailure()
    {
        var result = User.Register("test@example.com", "", "Doe", MakeHash());
        Assert.IsFalse(result.IsSuccess);
    }

    // ── VerifyEmail ───────────────────────────────────────────────────────────

    [TestMethod]
    public void VerifyEmail_CorrectToken_SetsVerifiedAndClearsToken()
    {
        var userResult = User.Register("test@example.com", "John", "Doe", MakeHash());
        var user = userResult.GetDataOrThrow();
        string token = user.EmailVerificationToken!;

        var result = user.VerifyEmail(token);

        Assert.IsTrue(result.IsSuccess);
        Assert.IsTrue(user.IsEmailVerified);
        Assert.IsNull(user.EmailVerificationToken);
    }

    [TestMethod]
    public void VerifyEmail_WrongToken_ReturnsFailure()
    {
        var userResult = User.Register("test@example.com", "John", "Doe", MakeHash());
        var user = userResult.GetDataOrThrow();

        var result = user.VerifyEmail("wrong-token");

        Assert.IsFalse(result.IsSuccess);
        Assert.AreEqual(IdentityErrors.EmailTokenInvalid.Code, result.GetErrorOrThrow().Code);
        Assert.IsFalse(user.IsEmailVerified);
    }

    [TestMethod]
    public void VerifyEmail_AlreadyVerified_ReturnsFailure()
    {
        var userResult = User.Register("test@example.com", "John", "Doe", MakeHash());
        var user = userResult.GetDataOrThrow();
        string token = user.EmailVerificationToken!;
        user.VerifyEmail(token); // first call succeeds

        var result = user.VerifyEmail(token); // second call

        Assert.IsFalse(result.IsSuccess);
        Assert.AreEqual(IdentityErrors.EmailAlreadyVerified.Code, result.GetErrorOrThrow().Code);
    }

    [TestMethod]
    public void VerifyEmail_RaisesEmailVerifiedEvent()
    {
        var userResult = User.Register("test@example.com", "John", "Doe", MakeHash());
        var user = userResult.GetDataOrThrow();
        user.ClearDomainEvents();
        string token = user.EmailVerificationToken!;

        user.VerifyEmail(token);

        Assert.HasCount(1, user.DomainEvents);
        Assert.IsInstanceOfType<EmailVerifiedEvent>(user.DomainEvents.First());
    }

    // ── ChangePassword ────────────────────────────────────────────────────────

    [TestMethod]
    public void ChangePassword_UpdatesHash()
    {
        var userResult = User.Register("test@example.com", "John", "Doe", MakeHash("$2a$12$oldhash"));
        var user = userResult.GetDataOrThrow();
        var newHash = MakeHash("$2a$12$newhash");

        user.ChangePassword(newHash);

        Assert.AreEqual("$2a$12$newhash", user.PasswordHash.Hash);
    }

    [TestMethod]
    public void ChangePassword_RevokesAllRefreshTokens()
    {
        var userResult = User.Register("test@example.com", "John", "Doe", MakeHash());
        var user = userResult.GetDataOrThrow();
        user.AddRefreshToken("token-1", DateTime.UtcNow.AddDays(30));
        user.AddRefreshToken("token-2", DateTime.UtcNow.AddDays(30));

        user.ChangePassword(MakeHash("$2a$12$newhash"));

        Assert.IsTrue(user.RefreshTokens.All(t => t.IsRevoked));
    }

    [TestMethod]
    public void ChangePassword_RaisesPasswordChangedEvent()
    {
        var userResult = User.Register("test@example.com", "John", "Doe", MakeHash());
        var user = userResult.GetDataOrThrow();
        user.ClearDomainEvents();

        user.ChangePassword(MakeHash("$2a$12$newhash"));

        Assert.HasCount(1, user.DomainEvents);
        Assert.IsInstanceOfType<PasswordChangedEvent>(user.DomainEvents.First());
    }

    // ── AddAddress ────────────────────────────────────────────────────────────

    [TestMethod]
    public void AddAddress_FirstAddress_BecomesDefaultShippingAndBilling()
    {
        var userResult = User.Register("test@example.com", "John", "Doe", MakeHash());
        var user = userResult.GetDataOrThrow();

        var result = user.AddAddress("123 Main St", "Springfield", "US", "12345");

        Assert.IsTrue(result.IsSuccess);
        Assert.HasCount(1, user.Addresses);
        var addr = user.Addresses.First();
        Assert.IsTrue(addr.IsDefaultShipping);
        Assert.IsTrue(addr.IsDefaultBilling);
    }

    [TestMethod]
    public void AddAddress_SecondAddress_DoesNotReplaceDefault()
    {
        var userResult = User.Register("test@example.com", "John", "Doe", MakeHash());
        var user = userResult.GetDataOrThrow();
        user.AddAddress("123 Main St", "Springfield", "US", "12345");

        user.AddAddress("456 Oak Ave", "Shelbyville", "US", "67890");

        Assert.HasCount(2, user.Addresses);
        Assert.AreEqual(1, user.Addresses.Count(a => a.IsDefaultShipping));
    }

    [TestMethod]
    public void AddAddress_EmptyStreet_ReturnsFailure()
    {
        var userResult = User.Register("test@example.com", "John", "Doe", MakeHash());
        var user = userResult.GetDataOrThrow();

        var result = user.AddAddress("", "City", "US", null);

        Assert.IsFalse(result.IsSuccess);
        Assert.AreEqual(IdentityErrors.AddressStreetEmpty.Code, result.GetErrorOrThrow().Code);
    }

    [TestMethod]
    public void AddAddress_EmptyCity_ReturnsFailure()
    {
        var userResult = User.Register("test@example.com", "John", "Doe", MakeHash());
        var user = userResult.GetDataOrThrow();

        var result = user.AddAddress("123 Main St", "", "US", null);

        Assert.IsFalse(result.IsSuccess);
        Assert.AreEqual(IdentityErrors.AddressCityEmpty.Code, result.GetErrorOrThrow().Code);
    }

    [TestMethod]
    public void AddAddress_EmptyCountry_ReturnsFailure()
    {
        var userResult = User.Register("test@example.com", "John", "Doe", MakeHash());
        var user = userResult.GetDataOrThrow();

        var result = user.AddAddress("123 Main St", "City", "", null);

        Assert.IsFalse(result.IsSuccess);
        Assert.AreEqual(IdentityErrors.AddressCountryEmpty.Code, result.GetErrorOrThrow().Code);
    }

    [TestMethod]
    public void AddAddress_ExceedsLimit_ReturnsFailure()
    {
        var userResult = User.Register("test@example.com", "John", "Doe", MakeHash());
        var user = userResult.GetDataOrThrow();
        for (int i = 0; i < 5; i++)
            user.AddAddress($"{i} Street", "City", "US", null);

        var result = user.AddAddress("6th Street", "City", "US", null);

        Assert.IsFalse(result.IsSuccess);
        Assert.AreEqual(IdentityErrors.AddressLimit.Code, result.GetErrorOrThrow().Code);
    }

    // ── SetDefaultShippingAddress ─────────────────────────────────────────────

    [TestMethod]
    public void SetDefaultShippingAddress_ValidId_SwitchesDefault()
    {
        var userResult = User.Register("test@example.com", "John", "Doe", MakeHash());
        var user = userResult.GetDataOrThrow();
        user.AddAddress("123 Main St", "City", "US", null);
        user.AddAddress("456 Oak Ave", "City", "US", null);

        var second = user.Addresses.Last();
        var result = user.SetDefaultShippingAddress(second.Id);

        Assert.IsTrue(result.IsSuccess);
        Assert.IsTrue(second.IsDefaultShipping);
        Assert.IsFalse(user.Addresses.First().IsDefaultShipping);
    }

    [TestMethod]
    public void SetDefaultShippingAddress_UnknownId_ReturnsFailure()
    {
        var userResult = User.Register("test@example.com", "John", "Doe", MakeHash());
        var user = userResult.GetDataOrThrow();

        var result = user.SetDefaultShippingAddress(Guid.NewGuid());

        Assert.IsFalse(result.IsSuccess);
        Assert.AreEqual(IdentityErrors.AddressNotFound.Code, result.GetErrorOrThrow().Code);
    }

    // ── AddRefreshToken ───────────────────────────────────────────────────────

    [TestMethod]
    public void AddRefreshToken_AddsTokenToCollection()
    {
        var userResult = User.Register("test@example.com", "John", "Doe", MakeHash());
        var user = userResult.GetDataOrThrow();

        user.AddRefreshToken("raw-token", DateTime.UtcNow.AddDays(30));

        Assert.HasCount(1, user.RefreshTokens);
        Assert.AreEqual("raw-token", user.RefreshTokens.First().Token);
    }

    [TestMethod]
    public void AddRefreshToken_WhenFiveActive_RevokesOldest()
    {
        var userResult = User.Register("test@example.com", "John", "Doe", MakeHash());
        var user = userResult.GetDataOrThrow();
        for (int i = 0; i < 5; i++)
            user.AddRefreshToken($"token-{i}", DateTime.UtcNow.AddDays(30));

        user.AddRefreshToken("token-new", DateTime.UtcNow.AddDays(30));

        // Oldest (token-0) should be revoked
        Assert.IsTrue(user.RefreshTokens.First(t => t.Token == "token-0").IsRevoked);
        // New token is active
        Assert.IsTrue(user.RefreshTokens.First(t => t.Token == "token-new").IsActive);
    }

    [TestMethod]
    public void GetActiveRefreshToken_ExpiredToken_ReturnsNull()
    {
        var userResult = User.Register("test@example.com", "John", "Doe", MakeHash());
        var user = userResult.GetDataOrThrow();
        user.AddRefreshToken("expired-token", DateTime.UtcNow.AddDays(-1)); // already expired

        var found = user.GetActiveRefreshToken("expired-token");

        Assert.IsNull(found);
    }

    [TestMethod]
    public void GetActiveRefreshToken_UnknownToken_ReturnsNull()
    {
        var userResult = User.Register("test@example.com", "John", "Doe", MakeHash());
        var user = userResult.GetDataOrThrow();

        var found = user.GetActiveRefreshToken("nonexistent");

        Assert.IsNull(found);
    }

    // ── RevokeRefreshToken ────────────────────────────────────────────────────

    [TestMethod]
    public void RevokeRefreshToken_ExistingToken_ReturnsTrue()
    {
        var userResult = User.Register("test@example.com", "John", "Doe", MakeHash());
        var user = userResult.GetDataOrThrow();
        user.AddRefreshToken("token-to-revoke", DateTime.UtcNow.AddDays(30));

        var result = user.RevokeRefreshToken("token-to-revoke");

        Assert.IsTrue(result);
        Assert.IsTrue(user.RefreshTokens.First(t => t.Token == "token-to-revoke").IsRevoked);
    }

    [TestMethod]
    public void RevokeRefreshToken_UnknownToken_ReturnsFalse()
    {
        var userResult = User.Register("test@example.com", "John", "Doe", MakeHash());
        var user = userResult.GetDataOrThrow();

        var result = user.RevokeRefreshToken("nonexistent");

        Assert.IsFalse(result);
    }
}
