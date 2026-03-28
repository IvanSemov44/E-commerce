# Phase 2, Step 5: Identity Domain Unit Tests

**Prerequisite**: Step 1 (`ECommerce.Identity.Domain`) is complete and builds.

Write these tests AFTER delivering the domain project. They test value objects and the User aggregate in isolation — no EF, no HTTP, no database.

---

## Task: Create ECommerce.Identity.Tests Project

### 1. Create the test project

```bash
cd src/backend
dotnet new mstest -n ECommerce.Identity.Tests -f net10.0 -o Identity/ECommerce.Identity.Tests
dotnet sln ../../ECommerce.sln add Identity/ECommerce.Identity.Tests/ECommerce.Identity.Tests.csproj

dotnet add Identity/ECommerce.Identity.Tests/ECommerce.Identity.Tests.csproj \
    reference Identity/ECommerce.Identity.Domain/ECommerce.Identity.Domain.csproj
dotnet add Identity/ECommerce.Identity.Tests/ECommerce.Identity.Tests.csproj \
    reference ECommerce.SharedKernel/ECommerce.SharedKernel.csproj

# Delete auto-generated test file
rm Identity/ECommerce.Identity.Tests/UnitTest1.cs
```

### 2. Create domain unit tests

**File: `Identity/ECommerce.Identity.Tests/Domain/ValueObjectTests.cs`**

```csharp
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
        var result = Email.Create(new string('a', 250) + "@b.com");
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
```

---

**File: `Identity/ECommerce.Identity.Tests/Domain/UserTests.cs`**

```csharp
using ECommerce.Identity.Domain.Aggregates.User;
using ECommerce.Identity.Domain.Errors;
using ECommerce.Identity.Domain.Events;
using ECommerce.Identity.Domain.ValueObjects;

namespace ECommerce.Identity.Tests.Domain;

[TestClass]
public class UserTests
{
    // ── Helpers ───────────────────────────────────────────────────────────────

    private static Email MakeEmail(string raw = "test@example.com")
        => Email.Create(raw).GetDataOrThrow();

    private static PersonName MakeName(string first = "John", string last = "Doe")
        => PersonName.Create(first, last).GetDataOrThrow();

    private static PasswordHash MakeHash(string hash = "$2a$12$fakehash")
        => PasswordHash.FromHash(hash).GetDataOrThrow();

    // ── Register ──────────────────────────────────────────────────────────────

    [TestMethod]
    public void Register_ValidInput_CreatesUserWithCustomerRole()
    {
        var user = User.Register(MakeEmail(), MakeName(), MakeHash());

        Assert.AreEqual(UserRole.Customer, user.Role);
        Assert.AreEqual("test@example.com", user.Email.Value);
        Assert.AreEqual("John", user.Name.First);
        Assert.IsFalse(user.IsEmailVerified);
        Assert.IsNotNull(user.EmailVerificationToken);
    }

    [TestMethod]
    public void Register_RaisesUserRegisteredEvent()
    {
        var user = User.Register(MakeEmail(), MakeName(), MakeHash());

        var events = user.DomainEvents.ToList();
        Assert.AreEqual(1, events.Count);
        Assert.IsInstanceOfType<UserRegisteredEvent>(events[0]);

        var evt = (UserRegisteredEvent)events[0];
        Assert.AreEqual(user.Id, evt.UserId);
        Assert.AreEqual("test@example.com", evt.Email);
    }

    [TestMethod]
    public void Register_NewUser_HasNoAddresses()
    {
        var user = User.Register(MakeEmail(), MakeName(), MakeHash());
        Assert.AreEqual(0, user.Addresses.Count);
    }

    // ── VerifyEmail ───────────────────────────────────────────────────────────

    [TestMethod]
    public void VerifyEmail_CorrectToken_SetsVerifiedAndClearsToken()
    {
        var user  = User.Register(MakeEmail(), MakeName(), MakeHash());
        string token = user.EmailVerificationToken!;

        var result = user.VerifyEmail(token);

        Assert.IsTrue(result.IsSuccess);
        Assert.IsTrue(user.IsEmailVerified);
        Assert.IsNull(user.EmailVerificationToken);
    }

    [TestMethod]
    public void VerifyEmail_WrongToken_ReturnsFailure()
    {
        var user = User.Register(MakeEmail(), MakeName(), MakeHash());

        var result = user.VerifyEmail("wrong-token");

        Assert.IsFalse(result.IsSuccess);
        Assert.AreEqual(IdentityErrors.EmailTokenInvalid.Code, result.GetErrorOrThrow().Code);
        Assert.IsFalse(user.IsEmailVerified);
    }

    [TestMethod]
    public void VerifyEmail_AlreadyVerified_ReturnsFailure()
    {
        var user  = User.Register(MakeEmail(), MakeName(), MakeHash());
        string token = user.EmailVerificationToken!;
        user.VerifyEmail(token); // first call succeeds

        var result = user.VerifyEmail(token); // second call

        Assert.IsFalse(result.IsSuccess);
        Assert.AreEqual(IdentityErrors.EmailAlreadyVerified.Code, result.GetErrorOrThrow().Code);
    }

    [TestMethod]
    public void VerifyEmail_RaisesEmailVerifiedEvent()
    {
        var user  = User.Register(MakeEmail(), MakeName(), MakeHash());
        user.ClearDomainEvents();
        string token = user.EmailVerificationToken!;

        user.VerifyEmail(token);

        Assert.AreEqual(1, user.DomainEvents.Count);
        Assert.IsInstanceOfType<EmailVerifiedEvent>(user.DomainEvents.First());
    }

    // ── ChangePassword ────────────────────────────────────────────────────────

    [TestMethod]
    public void ChangePassword_UpdatesHash()
    {
        var user    = User.Register(MakeEmail(), MakeName(), MakeHash("$2a$12$oldhash"));
        var newHash = MakeHash("$2a$12$newhash");

        user.ChangePassword(newHash);

        Assert.AreEqual("$2a$12$newhash", user.PasswordHash.Hash);
    }

    [TestMethod]
    public void ChangePassword_RevokesAllRefreshTokens()
    {
        var user = User.Register(MakeEmail(), MakeName(), MakeHash());
        user.AddRefreshToken("token-1", DateTime.UtcNow.AddDays(30));
        user.AddRefreshToken("token-2", DateTime.UtcNow.AddDays(30));

        user.ChangePassword(MakeHash("$2a$12$newhash"));

        Assert.IsTrue(user.RefreshTokens.All(t => t.IsRevoked));
    }

    [TestMethod]
    public void ChangePassword_RaisesPasswordChangedEvent()
    {
        var user = User.Register(MakeEmail(), MakeName(), MakeHash());
        user.ClearDomainEvents();

        user.ChangePassword(MakeHash("$2a$12$newhash"));

        Assert.AreEqual(1, user.DomainEvents.Count);
        Assert.IsInstanceOfType<PasswordChangedEvent>(user.DomainEvents.First());
    }

    // ── AddAddress ────────────────────────────────────────────────────────────

    [TestMethod]
    public void AddAddress_FirstAddress_BecomesDefaultShippingAndBilling()
    {
        var user = User.Register(MakeEmail(), MakeName(), MakeHash());

        var result = user.AddAddress("123 Main St", "Springfield", "US", "12345");

        Assert.IsTrue(result.IsSuccess);
        Assert.AreEqual(1, user.Addresses.Count);
        var addr = user.Addresses.First();
        Assert.IsTrue(addr.IsDefaultShipping);
        Assert.IsTrue(addr.IsDefaultBilling);
    }

    [TestMethod]
    public void AddAddress_SecondAddress_DoesNotReplaceDefault()
    {
        var user = User.Register(MakeEmail(), MakeName(), MakeHash());
        user.AddAddress("123 Main St", "Springfield", "US", "12345");

        user.AddAddress("456 Oak Ave", "Shelbyville", "US", "67890");

        Assert.AreEqual(2, user.Addresses.Count);
        Assert.AreEqual(1, user.Addresses.Count(a => a.IsDefaultShipping));
    }

    [TestMethod]
    public void AddAddress_EmptyStreet_ReturnsFailure()
    {
        var user = User.Register(MakeEmail(), MakeName(), MakeHash());

        var result = user.AddAddress("", "City", "US", null);

        Assert.IsFalse(result.IsSuccess);
        Assert.AreEqual(IdentityErrors.AddressStreetEmpty.Code, result.GetErrorOrThrow().Code);
    }

    [TestMethod]
    public void AddAddress_ExceedsLimit_ReturnsFailure()
    {
        var user = User.Register(MakeEmail(), MakeName(), MakeHash());
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
        var user = User.Register(MakeEmail(), MakeName(), MakeHash());
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
        var user = User.Register(MakeEmail(), MakeName(), MakeHash());

        var result = user.SetDefaultShippingAddress(Guid.NewGuid());

        Assert.IsFalse(result.IsSuccess);
        Assert.AreEqual(IdentityErrors.AddressNotFound.Code, result.GetErrorOrThrow().Code);
    }

    // ── AddRefreshToken ───────────────────────────────────────────────────────

    [TestMethod]
    public void AddRefreshToken_AddsTokenToCollection()
    {
        var user = User.Register(MakeEmail(), MakeName(), MakeHash());

        user.AddRefreshToken("raw-token", DateTime.UtcNow.AddDays(30));

        Assert.AreEqual(1, user.RefreshTokens.Count);
        Assert.AreEqual("raw-token", user.RefreshTokens.First().Token);
    }

    [TestMethod]
    public void AddRefreshToken_WhenFiveActive_RevokesOldest()
    {
        var user = User.Register(MakeEmail(), MakeName(), MakeHash());
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
        var user = User.Register(MakeEmail(), MakeName(), MakeHash());
        user.AddRefreshToken("expired-token", DateTime.UtcNow.AddDays(-1)); // already expired

        var found = user.GetActiveRefreshToken("expired-token");

        Assert.IsNull(found);
    }

    [TestMethod]
    public void GetActiveRefreshToken_UnknownToken_ReturnsNull()
    {
        var user = User.Register(MakeEmail(), MakeName(), MakeHash());

        var found = user.GetActiveRefreshToken("nonexistent");

        Assert.IsNull(found);
    }
}
```

---

### 3. Run tests

```bash
cd src/backend
dotnet test Identity/ECommerce.Identity.Tests/ECommerce.Identity.Tests.csproj --logger "console;verbosity=normal"
```

---

## Acceptance Criteria

- [ ] `ECommerce.Identity.Tests` project created and added to solution
- [ ] `ValueObjectTests.cs` — covers Email, PersonName, PasswordHash validation and equality
- [ ] `UserTests.cs` — covers Register, VerifyEmail, ChangePassword, AddAddress, SetDefaultShippingAddress, AddRefreshToken domain behavior
- [ ] Each error case checks the specific `DomainError.Code` (not just `IsSuccess == false`)
- [ ] No EF Core, no HTTP, no database in any test — pure in-memory
- [ ] All tests pass (`dotnet test`)
