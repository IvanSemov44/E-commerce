using ECommerce.Identity.Domain.Aggregates.User;
using ECommerce.Identity.Domain.Errors;
using ECommerce.Identity.Domain.Events;
using ECommerce.Identity.Domain.Interfaces;
using ECommerce.Identity.Domain.ValueObjects;
using ECommerce.SharedKernel.Enums;

namespace ECommerce.Identity.Tests.Domain;

[TestClass]
public class UserTests
{
    // ── Helpers ───────────────────────────────────────────────────────────────

    private sealed class FakePasswordHasher : IPasswordHasher
    {
        public string Hash(string rawPassword) => $"$2a$12${rawPassword}_hashed";
        public bool Verify(string rawPassword, string hash) => hash == $"$2a$12${rawPassword}_hashed";
    }

    private static readonly FakePasswordHasher Hasher = new();

    private static User MakeUser(string email = "test@example.com", string firstName = "John", string lastName = "Doe", string password = "ValidPass1")
        => User.Register(email, firstName, lastName, password, Hasher).GetDataOrThrow();

    // ── Register ──────────────────────────────────────────────────────────────

    [TestMethod]
    public void Register_ValidInput_CreatesUserWithCustomerRole()
    {
        var userResult = User.Register("test@example.com", "John", "Doe", "ValidPass1", Hasher);

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
        var user = MakeUser();

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
        var user = MakeUser();
        Assert.IsEmpty(user.Addresses);
    }

    [TestMethod]
    public void Register_InvalidEmail_ReturnsFailure()
    {
        var result = User.Register("not-an-email", "John", "Doe", "ValidPass1", Hasher);
        Assert.IsFalse(result.IsSuccess);
    }

    [TestMethod]
    public void Register_EmptyFirstName_ReturnsFailure()
    {
        var result = User.Register("test@example.com", "", "Doe", "ValidPass1", Hasher);
        Assert.IsFalse(result.IsSuccess);
    }

    [TestMethod]
    public void Register_WeakPassword_ReturnsFailure()
    {
        var result = User.Register("test@example.com", "John", "Doe", "weak", Hasher);
        Assert.IsFalse(result.IsSuccess);
    }

    // ── VerifyEmail ───────────────────────────────────────────────────────────

    [TestMethod]
    public void VerifyEmail_CorrectToken_SetsVerifiedAndClearsToken()
    {
        var user = MakeUser();
        string token = user.EmailVerificationToken!;

        var result = user.VerifyEmail(token);

        Assert.IsTrue(result.IsSuccess);
        Assert.IsTrue(user.IsEmailVerified);
        Assert.IsNull(user.EmailVerificationToken);
    }

    [TestMethod]
    public void VerifyEmail_WrongToken_ReturnsFailure()
    {
        var user = MakeUser();

        var result = user.VerifyEmail("wrong-token");

        Assert.IsFalse(result.IsSuccess);
        Assert.AreEqual(IdentityErrors.EmailTokenInvalid.Code, result.GetErrorOrThrow().Code);
        Assert.IsFalse(user.IsEmailVerified);
    }

    [TestMethod]
    public void VerifyEmail_AlreadyVerified_ReturnsFailure()
    {
        var user = MakeUser();
        string token = user.EmailVerificationToken!;
        user.VerifyEmail(token);

        var result = user.VerifyEmail(token);

        Assert.IsFalse(result.IsSuccess);
        Assert.AreEqual(IdentityErrors.EmailAlreadyVerified.Code, result.GetErrorOrThrow().Code);
    }

    [TestMethod]
    public void VerifyEmail_RaisesEmailVerifiedEvent()
    {
        var user = MakeUser();
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
        var user = MakeUser(password: "OldPass1");

        var result = user.ChangePassword("OldPass1", "NewPass1!", Hasher);

        Assert.IsTrue(result.IsSuccess);
        Assert.AreEqual("$2a$12$NewPass1!_hashed", user.PasswordHash.Hash);
    }

    [TestMethod]
    public void ChangePassword_WrongOldPassword_Fails()
    {
        var user = MakeUser(password: "OldPass1");

        var result = user.ChangePassword("wrongpw", "NewPass1!", Hasher);

        Assert.IsFalse(result.IsSuccess);
        Assert.AreEqual(IdentityErrors.InvalidCredentials, result.GetErrorOrThrow());
    }

    [TestMethod]
    public void ChangePassword_RevokesAllRefreshTokens()
    {
        var user = MakeUser(password: "OldPass1");
        user.AddRefreshToken("token-1", DateTime.UtcNow.AddDays(30));
        user.AddRefreshToken("token-2", DateTime.UtcNow.AddDays(30));

        user.ChangePassword("OldPass1", "NewPass1!", Hasher);

        Assert.IsTrue(user.RefreshTokens.All(t => t.IsRevoked));
    }

    [TestMethod]
    public void ChangePassword_RaisesPasswordChangedEvent()
    {
        var user = MakeUser(password: "OldPass1");
        user.ClearDomainEvents();

        user.ChangePassword("OldPass1", "NewPass1!", Hasher);

        Assert.HasCount(1, user.DomainEvents);
        Assert.IsInstanceOfType<PasswordChangedEvent>(user.DomainEvents.First());
    }

    // ── AddAddress ────────────────────────────────────────────────────────────

    [TestMethod]
    public void AddAddress_FirstAddress_BecomesDefaultShippingAndBilling()
    {
        var user = MakeUser();

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
        var user = MakeUser();
        user.AddAddress("123 Main St", "Springfield", "US", "12345");

        user.AddAddress("456 Oak Ave", "Shelbyville", "US", "67890");

        Assert.HasCount(2, user.Addresses);
        Assert.AreEqual(1, user.Addresses.Count(a => a.IsDefaultShipping));
    }

    [TestMethod]
    public void AddAddress_EmptyStreet_ReturnsFailure()
    {
        var user = MakeUser();

        var result = user.AddAddress("", "City", "US", null);

        Assert.IsFalse(result.IsSuccess);
        Assert.AreEqual(IdentityErrors.AddressStreetEmpty.Code, result.GetErrorOrThrow().Code);
    }

    [TestMethod]
    public void AddAddress_EmptyCity_ReturnsFailure()
    {
        var user = MakeUser();

        var result = user.AddAddress("123 Main St", "", "US", null);

        Assert.IsFalse(result.IsSuccess);
        Assert.AreEqual(IdentityErrors.AddressCityEmpty.Code, result.GetErrorOrThrow().Code);
    }

    [TestMethod]
    public void AddAddress_EmptyCountry_ReturnsFailure()
    {
        var user = MakeUser();

        var result = user.AddAddress("123 Main St", "City", "", null);

        Assert.IsFalse(result.IsSuccess);
        Assert.AreEqual(IdentityErrors.AddressCountryEmpty.Code, result.GetErrorOrThrow().Code);
    }

    [TestMethod]
    public void AddAddress_ExceedsLimit_ReturnsFailure()
    {
        var user = MakeUser();
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
        var user = MakeUser();
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
        var user = MakeUser();

        var result = user.SetDefaultShippingAddress(Guid.NewGuid());

        Assert.IsFalse(result.IsSuccess);
        Assert.AreEqual(IdentityErrors.AddressNotFound.Code, result.GetErrorOrThrow().Code);
    }

    // ── AddRefreshToken ───────────────────────────────────────────────────────

    [TestMethod]
    public void AddRefreshToken_AddsTokenToCollection()
    {
        var user = MakeUser();

        user.AddRefreshToken("raw-token", DateTime.UtcNow.AddDays(30));

        Assert.HasCount(1, user.RefreshTokens);
        Assert.AreEqual("raw-token", user.RefreshTokens.First().Token);
    }

    [TestMethod]
    public void AddRefreshToken_WhenFiveActive_RevokesOldest()
    {
        var user = MakeUser();
        for (int i = 0; i < 5; i++)
            user.AddRefreshToken($"token-{i}", DateTime.UtcNow.AddDays(30));

        user.AddRefreshToken("token-new", DateTime.UtcNow.AddDays(30));

        Assert.IsTrue(user.RefreshTokens.First(t => t.Token == "token-0").IsRevoked);
        Assert.IsTrue(user.RefreshTokens.First(t => t.Token == "token-new").IsActive);
    }

    [TestMethod]
    public void GetActiveRefreshToken_ExpiredToken_ReturnsNull()
    {
        var user = MakeUser();
        user.AddRefreshToken("expired-token", DateTime.UtcNow.AddDays(-1));

        var found = user.GetActiveRefreshToken("expired-token");

        Assert.IsNull(found);
    }

    [TestMethod]
    public void GetActiveRefreshToken_UnknownToken_ReturnsNull()
    {
        var user = MakeUser();

        var found = user.GetActiveRefreshToken("nonexistent");

        Assert.IsNull(found);
    }

    // ── RevokeRefreshToken ────────────────────────────────────────────────────

    [TestMethod]
    public void RevokeRefreshToken_ExistingToken_Succeeds()
    {
        var user = MakeUser();
        user.AddRefreshToken("token-to-revoke", DateTime.UtcNow.AddDays(30));

        var result = user.RevokeRefreshToken("token-to-revoke");

        Assert.IsTrue(result.IsSuccess);
        Assert.IsTrue(user.RefreshTokens.First(t => t.Token == "token-to-revoke").IsRevoked);
    }

    [TestMethod]
    public void RevokeRefreshToken_UnknownToken_ReturnsFailure()
    {
        var user = MakeUser();

        var result = user.RevokeRefreshToken("nonexistent");

        Assert.IsFalse(result.IsSuccess);
        Assert.AreEqual(IdentityErrors.TokenInvalid.Code, result.GetErrorOrThrow().Code);
    }

    // ── SetDefaultBillingAddress ──────────────────────────────────────────────

    [TestMethod]
    public void SetDefaultBillingAddress_ValidId_SwitchesDefault()
    {
        var user = MakeUser();
        user.AddAddress("123 Main St", "City", "US", null);
        user.AddAddress("456 Oak Ave", "City", "US", null);

        var second = user.Addresses.Last();
        var result = user.SetDefaultBillingAddress(second.Id);

        Assert.IsTrue(result.IsSuccess);
        Assert.IsTrue(second.IsDefaultBilling);
        Assert.IsFalse(user.Addresses.First().IsDefaultBilling);
    }

    [TestMethod]
    public void SetDefaultBillingAddress_UnknownId_ReturnsFailure()
    {
        var user = MakeUser();

        var result = user.SetDefaultBillingAddress(Guid.NewGuid());

        Assert.IsFalse(result.IsSuccess);
        Assert.AreEqual(IdentityErrors.AddressNotFound.Code, result.GetErrorOrThrow().Code);
    }
}
