# Phase 2, Step 6: Identity Handler Unit Tests

**Prerequisite**: Step 2 (`ECommerce.Identity.Application`) is complete and builds. `ECommerce.Identity.Tests` project from step 5 exists.

Write these tests AFTER delivering the Application project. They test command handler orchestration using fake repository and service implementations — no EF, no HTTP, no real bcrypt.

---

## Task: Add Handler Tests to ECommerce.Identity.Tests

Files go in `Identity/ECommerce.Identity.Tests/Application/`.

---

### File: `Identity/ECommerce.Identity.Tests/Application/CommandHandlerTests.cs`

```csharp
using ECommerce.Identity.Application.Commands.Register;
using ECommerce.Identity.Application.Commands.Login;
using ECommerce.Identity.Application.Commands.Logout;
using ECommerce.Identity.Application.Commands.RefreshToken;
using ECommerce.Identity.Application.Commands.ChangePassword;
using ECommerce.Identity.Application.Commands.UpdateProfile;
using ECommerce.Identity.Application.Commands.AddAddress;
using ECommerce.Identity.Application.Commands.SetDefaultAddress;
using ECommerce.Identity.Application.Commands.DeleteAccount;
using ECommerce.Identity.Application.Commands.VerifyEmail;
using ECommerce.Identity.Application.Commands.ForgotPassword;
using ECommerce.Identity.Application.Queries.GetCurrentUser;
using ECommerce.Identity.Application.Interfaces;
using ECommerce.Identity.Domain.Aggregates.User;
using ECommerce.Identity.Domain.Errors;
using ECommerce.Identity.Application.Errors;
using ECommerce.Identity.Domain.Interfaces;
using ECommerce.Identity.Domain.ValueObjects;
using ECommerce.SharedKernel.Interfaces;
using ECommerce.SharedKernel.Results;

namespace ECommerce.Identity.Tests.Application;

[TestClass]
public class CommandHandlerTests
{
    // ── Fakes ─────────────────────────────────────────────────────────────────

    sealed class FakeUserRepository : IUserRepository
    {
        public List<User> Store = new();
        public List<User> Deleted = new();

        public Task<User?> GetByIdAsync(Guid id, CancellationToken ct = default)
            => Task.FromResult(Store.FirstOrDefault(u => u.Id == id));

        public Task<User?> GetByEmailAsync(Email email, CancellationToken ct = default)
            => Task.FromResult(Store.FirstOrDefault(u => u.Email.Value == email.Value));

        public Task<bool> EmailExistsAsync(string email, CancellationToken ct = default)
            => Task.FromResult(Store.Any(u => u.Email.Value == email.ToLowerInvariant()));

        public Task<User?> GetByRefreshTokenAsync(string token, CancellationToken ct = default)
            => Task.FromResult(Store.FirstOrDefault(u => u.RefreshTokens.Any(t => t.Token == token)));

        public Task AddAsync(User user, CancellationToken ct = default)
        {
            Store.Add(user);
            return Task.CompletedTask;
        }

        public Task DeleteAsync(User user, CancellationToken ct = default)
        {
            Store.Remove(user);
            Deleted.Add(user);
            return Task.CompletedTask;
        }
    }

    sealed class FakePasswordHasher : IPasswordHasher
    {
        // In tests: hash = "HASH:" + raw. Verify = stored starts with "HASH:" + raw.
        public string Hash(string raw) => $"HASH:{raw}";
        public bool Verify(string raw, string hash) => hash == $"HASH:{raw}";
    }

    sealed class FakeJwtTokenService : IJwtTokenService
    {
        public string GenerateAccessToken(User user) => $"ACCESS_TOKEN_FOR_{user.Id}";
        public string GenerateRefreshToken() => $"REFRESH_{Guid.NewGuid():N}";
    }

    sealed class FakeUnitOfWork : IUnitOfWork
    {
        public int SaveCount;
        public Task<int> SaveChangesAsync(CancellationToken ct = default) { SaveCount++; return Task.FromResult(1); }
        public Task BeginTransactionAsync(CancellationToken ct = default)    => Task.CompletedTask;
        public Task CommitTransactionAsync(CancellationToken ct = default)   => Task.CompletedTask;
        public Task RollbackTransactionAsync(CancellationToken ct = default) => Task.CompletedTask;
        public bool HasActiveTransaction => false;
        public void Dispose() { }
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static User SeedUser(FakeUserRepository repo, string email = "test@example.com", string rawPassword = "Password1")
    {
        var user = User.Register(
            Email.Create(email).GetDataOrThrow(),
            PersonName.Create("Test", "User").GetDataOrThrow(),
            PasswordHash.FromHash($"HASH:{rawPassword}").GetDataOrThrow());
        repo.Store.Add(user);
        return user;
    }

    // ── RegisterCommandHandler ────────────────────────────────────────────────

    [TestMethod]
    public async Task RegisterHandler_ValidCommand_ReturnsAuthTokenDto()
    {
        var repo   = new FakeUserRepository();
        var hasher = new FakePasswordHasher();
        var jwt    = new FakeJwtTokenService();
        var uow    = new FakeUnitOfWork();
        var handler = new RegisterCommandHandler(repo, hasher, jwt, uow);

        var result = await handler.Handle(
            new RegisterCommand("Jane", "Doe", "jane@example.com", "SecurePass1"),
            CancellationToken.None);

        Assert.IsTrue(result.IsSuccess);
        var dto = result.GetDataOrThrow();
        Assert.IsFalse(string.IsNullOrEmpty(dto.AccessToken));
        Assert.IsFalse(string.IsNullOrEmpty(dto.RefreshToken));
        Assert.AreNotEqual(Guid.Empty, dto.UserId);
    }

    [TestMethod]
    public async Task RegisterHandler_DuplicateEmail_ReturnsEmailTakenError()
    {
        var repo = new FakeUserRepository();
        SeedUser(repo, "jane@example.com");
        var handler = new RegisterCommandHandler(repo, new FakePasswordHasher(), new FakeJwtTokenService(), new FakeUnitOfWork());

        var result = await handler.Handle(
            new RegisterCommand("Jane", "Doe", "jane@example.com", "SecurePass1"),
            CancellationToken.None);

        Assert.IsFalse(result.IsSuccess);
        // EMAIL_TAKEN is an application-layer error (requires EmailExistsAsync repo call)
        Assert.AreEqual(IdentityApplicationErrors.EmailTaken.Code, result.GetErrorOrThrow().Code);
    }

    [TestMethod]
    public async Task RegisterHandler_InvalidEmail_ReturnsFailure()
    {
        var handler = new RegisterCommandHandler(
            new FakeUserRepository(), new FakePasswordHasher(), new FakeJwtTokenService(), new FakeUnitOfWork());

        var result = await handler.Handle(
            new RegisterCommand("Jane", "Doe", "not-an-email", "SecurePass1"),
            CancellationToken.None);

        Assert.IsFalse(result.IsSuccess);
    }

    [TestMethod]
    public async Task RegisterHandler_WeakPassword_ReturnsFailure()
    {
        var handler = new RegisterCommandHandler(
            new FakeUserRepository(), new FakePasswordHasher(), new FakeJwtTokenService(), new FakeUnitOfWork());

        var result = await handler.Handle(
            new RegisterCommand("Jane", "Doe", "jane@example.com", "abc"), // too short
            CancellationToken.None);

        Assert.IsFalse(result.IsSuccess);
    }

    [TestMethod]
    public async Task RegisterHandler_ValidCommand_UserStoredInRepository()
    {
        var repo    = new FakeUserRepository();
        var handler = new RegisterCommandHandler(repo, new FakePasswordHasher(), new FakeJwtTokenService(), new FakeUnitOfWork());

        await handler.Handle(
            new RegisterCommand("Jane", "Doe", "jane@example.com", "SecurePass1"),
            CancellationToken.None);

        Assert.AreEqual(1, repo.Store.Count);
        Assert.AreEqual("jane@example.com", repo.Store[0].Email.Value);
    }

    // ── LoginCommandHandler ───────────────────────────────────────────────────

    [TestMethod]
    public async Task LoginHandler_ValidCredentials_ReturnsAuthTokenDto()
    {
        var repo = new FakeUserRepository();
        SeedUser(repo, "user@example.com", "Password1");
        var handler = new LoginCommandHandler(repo, new FakePasswordHasher(), new FakeJwtTokenService(), new FakeUnitOfWork());

        var result = await handler.Handle(
            new LoginCommand("user@example.com", "Password1"),
            CancellationToken.None);

        Assert.IsTrue(result.IsSuccess);
        Assert.IsFalse(string.IsNullOrEmpty(result.GetDataOrThrow().AccessToken));
    }

    [TestMethod]
    public async Task LoginHandler_WrongPassword_ReturnsInvalidCredentials()
    {
        var repo = new FakeUserRepository();
        SeedUser(repo, "user@example.com", "Password1");
        var handler = new LoginCommandHandler(repo, new FakePasswordHasher(), new FakeJwtTokenService(), new FakeUnitOfWork());

        var result = await handler.Handle(
            new LoginCommand("user@example.com", "WrongPassword1"),
            CancellationToken.None);

        Assert.IsFalse(result.IsSuccess);
        Assert.AreEqual(IdentityErrors.InvalidCredentials.Code, result.GetErrorOrThrow().Code);
    }

    [TestMethod]
    public async Task LoginHandler_UnknownEmail_ReturnsInvalidCredentials()
    {
        var handler = new LoginCommandHandler(
            new FakeUserRepository(), new FakePasswordHasher(), new FakeJwtTokenService(), new FakeUnitOfWork());

        var result = await handler.Handle(
            new LoginCommand("ghost@example.com", "Password1"),
            CancellationToken.None);

        Assert.IsFalse(result.IsSuccess);
        // Must return INVALID_CREDENTIALS — NOT a "user not found" or 404-style error (security requirement)
        Assert.AreEqual(IdentityErrors.InvalidCredentials.Code, result.GetErrorOrThrow().Code);
    }

    [TestMethod]
    public async Task LoginHandler_ValidCredentials_StoresRefreshToken()
    {
        var repo = new FakeUserRepository();
        var user = SeedUser(repo, "user@example.com", "Password1");
        var handler = new LoginCommandHandler(repo, new FakePasswordHasher(), new FakeJwtTokenService(), new FakeUnitOfWork());

        await handler.Handle(new LoginCommand("user@example.com", "Password1"), CancellationToken.None);

        Assert.AreEqual(1, user.RefreshTokens.Count);
    }

    // ── LogoutCommandHandler ──────────────────────────────────────────────────

    [TestMethod]
    public async Task LogoutHandler_ValidToken_RevokesToken()
    {
        var repo = new FakeUserRepository();
        var user = SeedUser(repo);
        user.AddRefreshToken("active-token", DateTime.UtcNow.AddDays(30));
        var handler = new LogoutCommandHandler(repo, new FakeUnitOfWork());

        var result = await handler.Handle(
            new LogoutCommand(user.Id, "active-token"),
            CancellationToken.None);

        Assert.IsTrue(result.IsSuccess);
        Assert.IsTrue(user.RefreshTokens.First(t => t.Token == "active-token").IsRevoked);
    }

    [TestMethod]
    public async Task LogoutHandler_UnknownUser_StillSucceeds()
    {
        // Logout should succeed even if user not found — idempotent, no information leak
        var handler = new LogoutCommandHandler(new FakeUserRepository(), new FakeUnitOfWork());

        var result = await handler.Handle(
            new LogoutCommand(Guid.NewGuid(), "some-token"),
            CancellationToken.None);

        Assert.IsTrue(result.IsSuccess);
    }

    // ── RefreshTokenCommandHandler ────────────────────────────────────────────

    [TestMethod]
    public async Task RefreshTokenHandler_ValidToken_ReturnsNewAuthTokenDto()
    {
        var repo = new FakeUserRepository();
        var user = SeedUser(repo);
        user.AddRefreshToken("valid-refresh", DateTime.UtcNow.AddDays(30));
        var handler = new RefreshTokenCommandHandler(repo, new FakeJwtTokenService(), new FakeUnitOfWork());

        var result = await handler.Handle(
            new RefreshTokenCommand("valid-refresh"),
            CancellationToken.None);

        Assert.IsTrue(result.IsSuccess);
        Assert.IsFalse(string.IsNullOrEmpty(result.GetDataOrThrow().AccessToken));
        Assert.IsFalse(string.IsNullOrEmpty(result.GetDataOrThrow().RefreshToken));
    }

    [TestMethod]
    public async Task RefreshTokenHandler_TokenNotFound_ReturnsTokenInvalid()
    {
        var handler = new RefreshTokenCommandHandler(
            new FakeUserRepository(), new FakeJwtTokenService(), new FakeUnitOfWork());

        var result = await handler.Handle(
            new RefreshTokenCommand("nonexistent-token"),
            CancellationToken.None);

        Assert.IsFalse(result.IsSuccess);
        Assert.AreEqual(IdentityErrors.TokenInvalid.Code, result.GetErrorOrThrow().Code);
    }

    [TestMethod]
    public async Task RefreshTokenHandler_RevokedToken_ReturnsTokenRevoked()
    {
        var repo = new FakeUserRepository();
        var user = SeedUser(repo);
        user.AddRefreshToken("revoked-token", DateTime.UtcNow.AddDays(30));
        var token = user.RefreshTokens.First(t => t.Token == "revoked-token");
        token.Revoke("test revocation"); // Revoke it directly
        var handler = new RefreshTokenCommandHandler(repo, new FakeJwtTokenService(), new FakeUnitOfWork());

        var result = await handler.Handle(
            new RefreshTokenCommand("revoked-token"),
            CancellationToken.None);

        Assert.IsFalse(result.IsSuccess);
        Assert.AreEqual(IdentityErrors.TokenRevoked.Code, result.GetErrorOrThrow().Code);
    }

    [TestMethod]
    public async Task RefreshTokenHandler_ExpiredToken_ReturnsTokenInvalid()
    {
        var repo = new FakeUserRepository();
        var user = SeedUser(repo);
        user.AddRefreshToken("expired-token", DateTime.UtcNow.AddDays(-1)); // already expired
        var handler = new RefreshTokenCommandHandler(repo, new FakeJwtTokenService(), new FakeUnitOfWork());

        var result = await handler.Handle(
            new RefreshTokenCommand("expired-token"),
            CancellationToken.None);

        Assert.IsFalse(result.IsSuccess);
        Assert.AreEqual(IdentityErrors.TokenInvalid.Code, result.GetErrorOrThrow().Code);
    }

    // ── ChangePasswordCommandHandler ──────────────────────────────────────────

    [TestMethod]
    public async Task ChangePasswordHandler_CorrectOldPassword_Succeeds()
    {
        var repo = new FakeUserRepository();
        var user = SeedUser(repo, "user@example.com", "OldPass1");
        var handler = new ChangePasswordCommandHandler(repo, new FakePasswordHasher(), new FakeUnitOfWork());

        var result = await handler.Handle(
            new ChangePasswordCommand(user.Id, "OldPass1", "NewPass1"),
            CancellationToken.None);

        Assert.IsTrue(result.IsSuccess);
        Assert.AreEqual("HASH:NewPass1", user.PasswordHash.Hash);
    }

    [TestMethod]
    public async Task ChangePasswordHandler_WrongOldPassword_ReturnsFailure()
    {
        var repo = new FakeUserRepository();
        var user = SeedUser(repo, "user@example.com", "OldPass1");
        var handler = new ChangePasswordCommandHandler(repo, new FakePasswordHasher(), new FakeUnitOfWork());

        var result = await handler.Handle(
            new ChangePasswordCommand(user.Id, "WrongOld1", "NewPass1"),
            CancellationToken.None);

        Assert.IsFalse(result.IsSuccess);
        Assert.AreEqual(IdentityErrors.InvalidCredentials.Code, result.GetErrorOrThrow().Code);
    }

    [TestMethod]
    public async Task ChangePasswordHandler_UserNotFound_ReturnsUserNotFound()
    {
        var handler = new ChangePasswordCommandHandler(
            new FakeUserRepository(), new FakePasswordHasher(), new FakeUnitOfWork());

        var result = await handler.Handle(
            new ChangePasswordCommand(Guid.NewGuid(), "OldPass1", "NewPass1"),
            CancellationToken.None);

        Assert.IsFalse(result.IsSuccess);
        // USER_NOT_FOUND is an application-layer error (requires repo lookup)
        Assert.AreEqual(IdentityApplicationErrors.UserNotFound.Code, result.GetErrorOrThrow().Code);
    }

    // ── UpdateProfileCommandHandler ───────────────────────────────────────────

    [TestMethod]
    public async Task UpdateProfileHandler_ValidCommand_UpdatesName()
    {
        var repo = new FakeUserRepository();
        var user = SeedUser(repo);
        var handler = new UpdateProfileCommandHandler(repo, new FakeUnitOfWork());

        var result = await handler.Handle(
            new UpdateProfileCommand(user.Id, "Updated", "Name", "+1234567890"),
            CancellationToken.None);

        Assert.IsTrue(result.IsSuccess);
        Assert.AreEqual("Updated", user.Name.First);
        Assert.AreEqual("Name", user.Name.Last);
        Assert.AreEqual("+1234567890", user.PhoneNumber);
    }

    [TestMethod]
    public async Task UpdateProfileHandler_UserNotFound_ReturnsUserNotFound()
    {
        var handler = new UpdateProfileCommandHandler(new FakeUserRepository(), new FakeUnitOfWork());

        var result = await handler.Handle(
            new UpdateProfileCommand(Guid.NewGuid(), "First", "Last", null),
            CancellationToken.None);

        Assert.IsFalse(result.IsSuccess);
        Assert.AreEqual(IdentityApplicationErrors.UserNotFound.Code, result.GetErrorOrThrow().Code);
    }

    // ── AddAddressCommandHandler ──────────────────────────────────────────────

    [TestMethod]
    public async Task AddAddressHandler_ValidCommand_AddsAddress()
    {
        var repo = new FakeUserRepository();
        var user = SeedUser(repo);
        var handler = new AddAddressCommandHandler(repo, new FakeUnitOfWork());

        var result = await handler.Handle(
            new AddAddressCommand(user.Id, "123 Main St", "Springfield", "US", "12345"),
            CancellationToken.None);

        Assert.IsTrue(result.IsSuccess);
        Assert.AreEqual(1, user.Addresses.Count);
        Assert.AreEqual("123 Main St", user.Addresses.First().Street);
    }

    [TestMethod]
    public async Task AddAddressHandler_ExceedsLimit_ReturnsFailure()
    {
        var repo = new FakeUserRepository();
        var user = SeedUser(repo);
        for (int i = 0; i < 5; i++)
            user.AddAddress($"{i} Street", "City", "US", null);

        var handler = new AddAddressCommandHandler(repo, new FakeUnitOfWork());

        var result = await handler.Handle(
            new AddAddressCommand(user.Id, "6th Street", "City", "US", null),
            CancellationToken.None);

        Assert.IsFalse(result.IsSuccess);
        Assert.AreEqual(IdentityErrors.AddressLimit.Code, result.GetErrorOrThrow().Code);
    }

    // ── SetDefaultAddressCommandHandler ──────────────────────────────────────

    [TestMethod]
    public async Task SetDefaultAddressHandler_ValidAddress_SwitchesDefault()
    {
        var repo = new FakeUserRepository();
        var user = SeedUser(repo);
        user.AddAddress("123 Main St", "City", "US", null);
        user.AddAddress("456 Oak Ave", "City", "US", null);
        var secondAddressId = user.Addresses.Last().Id;
        var handler = new SetDefaultAddressCommandHandler(repo, new FakeUnitOfWork());

        var result = await handler.Handle(
            new SetDefaultAddressCommand(user.Id, secondAddressId),
            CancellationToken.None);

        Assert.IsTrue(result.IsSuccess);
        Assert.IsTrue(user.Addresses.First(a => a.Id == secondAddressId).IsDefaultShipping);
    }

    [TestMethod]
    public async Task SetDefaultAddressHandler_UserNotFound_ReturnsUserNotFound()
    {
        var handler = new SetDefaultAddressCommandHandler(new FakeUserRepository(), new FakeUnitOfWork());

        var result = await handler.Handle(
            new SetDefaultAddressCommand(Guid.NewGuid(), Guid.NewGuid()),
            CancellationToken.None);

        Assert.IsFalse(result.IsSuccess);
        Assert.AreEqual(IdentityApplicationErrors.UserNotFound.Code, result.GetErrorOrThrow().Code);
    }

    [TestMethod]
    public async Task SetDefaultAddressHandler_AddressNotInUserList_ReturnsAddressNotFound()
    {
        var repo = new FakeUserRepository();
        var user = SeedUser(repo);
        user.AddAddress("123 Main St", "City", "US", null);
        var handler = new SetDefaultAddressCommandHandler(repo, new FakeUnitOfWork());

        var result = await handler.Handle(
            new SetDefaultAddressCommand(user.Id, Guid.NewGuid()), // nonexistent address id
            CancellationToken.None);

        Assert.IsFalse(result.IsSuccess);
        Assert.AreEqual(IdentityErrors.AddressNotFound.Code, result.GetErrorOrThrow().Code);
    }

    // ── DeleteAccountCommandHandler ───────────────────────────────────────────

    [TestMethod]
    public async Task DeleteAccountHandler_ExistingUser_DeletesAndSucceeds()
    {
        var repo = new FakeUserRepository();
        var user = SeedUser(repo);
        var handler = new DeleteAccountCommandHandler(repo, new FakeUnitOfWork());

        var result = await handler.Handle(
            new DeleteAccountCommand(user.Id),
            CancellationToken.None);

        Assert.IsTrue(result.IsSuccess);
        Assert.AreEqual(0, repo.Store.Count);
        Assert.AreEqual(1, repo.Deleted.Count);
    }

    [TestMethod]
    public async Task DeleteAccountHandler_UserNotFound_ReturnsUserNotFound()
    {
        var handler = new DeleteAccountCommandHandler(new FakeUserRepository(), new FakeUnitOfWork());

        var result = await handler.Handle(
            new DeleteAccountCommand(Guid.NewGuid()),
            CancellationToken.None);

        Assert.IsFalse(result.IsSuccess);
        Assert.AreEqual(IdentityApplicationErrors.UserNotFound.Code, result.GetErrorOrThrow().Code);
    }

    // ── VerifyEmailCommandHandler ─────────────────────────────────────────────

    [TestMethod]
    public async Task VerifyEmailHandler_CorrectToken_Succeeds()
    {
        var repo = new FakeUserRepository();
        var user = SeedUser(repo);
        string token = user.EmailVerificationToken!;
        var handler = new VerifyEmailCommandHandler(repo, new FakeUnitOfWork());

        var result = await handler.Handle(
            new VerifyEmailCommand(user.Id, token),
            CancellationToken.None);

        Assert.IsTrue(result.IsSuccess);
        Assert.IsTrue(user.IsEmailVerified);
    }

    [TestMethod]
    public async Task VerifyEmailHandler_WrongToken_ReturnsFailure()
    {
        var repo = new FakeUserRepository();
        var user = SeedUser(repo);
        var handler = new VerifyEmailCommandHandler(repo, new FakeUnitOfWork());

        var result = await handler.Handle(
            new VerifyEmailCommand(user.Id, "bad-token"),
            CancellationToken.None);

        Assert.IsFalse(result.IsSuccess);
        Assert.AreEqual(IdentityErrors.EmailTokenInvalid.Code, result.GetErrorOrThrow().Code);
    }

    // ── ForgotPasswordCommandHandler ──────────────────────────────────────────

    [TestMethod]
    public async Task ForgotPasswordHandler_KnownEmail_Succeeds()
    {
        var repo = new FakeUserRepository();
        SeedUser(repo, "user@example.com");
        var handler = new ForgotPasswordCommandHandler(repo, new FakeUnitOfWork());

        var result = await handler.Handle(
            new ForgotPasswordCommand("user@example.com"),
            CancellationToken.None);

        // Always succeeds — never reveal whether email is registered
        Assert.IsTrue(result.IsSuccess);
    }

    [TestMethod]
    public async Task ForgotPasswordHandler_UnknownEmail_AlsoSucceeds()
    {
        var handler = new ForgotPasswordCommandHandler(new FakeUserRepository(), new FakeUnitOfWork());

        var result = await handler.Handle(
            new ForgotPasswordCommand("ghost@example.com"),
            CancellationToken.None);

        // Security invariant: must return success even for nonexistent emails
        Assert.IsTrue(result.IsSuccess);
    }

    // ── GetCurrentUserQueryHandler ────────────────────────────────────────────

    [TestMethod]
    public async Task GetCurrentUserHandler_ExistingUser_ReturnsProfile()
    {
        var repo = new FakeUserRepository();
        var user = SeedUser(repo, "user@example.com");
        var handler = new GetCurrentUserQueryHandler(repo);

        var result = await handler.Handle(
            new GetCurrentUserQuery(user.Id),
            CancellationToken.None);

        Assert.IsTrue(result.IsSuccess);
        var profile = result.GetDataOrThrow();
        Assert.AreEqual("user@example.com", profile.Email);
        Assert.AreEqual("Test", profile.FirstName);
        Assert.AreEqual("User",  profile.LastName);
    }

    [TestMethod]
    public async Task GetCurrentUserHandler_UnknownUser_ReturnsUserNotFound()
    {
        var handler = new GetCurrentUserQueryHandler(new FakeUserRepository());

        var result = await handler.Handle(
            new GetCurrentUserQuery(Guid.NewGuid()),
            CancellationToken.None);

        Assert.IsFalse(result.IsSuccess);
        Assert.AreEqual(IdentityApplicationErrors.UserNotFound.Code, result.GetErrorOrThrow().Code);
    }
}
```

---

### 3. Add missing project reference

The test project from step 5 only referenced the Domain. Now add Application:

```bash
cd src/backend
dotnet add Identity/ECommerce.Identity.Tests/ECommerce.Identity.Tests.csproj \
    reference Identity/ECommerce.Identity.Application/ECommerce.Identity.Application.csproj
```

### 4. Run tests

```bash
dotnet test Identity/ECommerce.Identity.Tests/ECommerce.Identity.Tests.csproj --logger "console;verbosity=normal"
```

---

## Acceptance Criteria

- [ ] `Application/CommandHandlerTests.cs` created in `Identity.Tests`
- [ ] `ECommerce.Identity.Application` project reference added to `Identity.Tests`
- [ ] `FakeUserRepository` includes `DeleteAsync` (tracks deleted users in `Deleted` list)
- [ ] Handlers covered: Register, Login, Logout, RefreshToken, ChangePassword, UpdateProfile, AddAddress, SetDefaultAddress, DeleteAccount, VerifyEmail, ForgotPassword, GetCurrentUser
- [ ] Each handler has: happy path + specific error case(s)
- [ ] Login tests confirm both "wrong password" AND "user not found" return `INVALID_CREDENTIALS` (security requirement — never reveal which)
- [ ] ForgotPassword tests confirm both known AND unknown email return success (security invariant — never reveal registration status)
- [ ] RefreshToken tests cover: valid token, nonexistent token (`TOKEN_INVALID`), revoked token (`TOKEN_REVOKED`), expired token (`TOKEN_INVALID`)
- [ ] `IdentityApplicationErrors` (not `IdentityErrors`) used for: `EMAIL_TAKEN`, `USER_NOT_FOUND` — these require a repo lookup and live in the Application layer
- [ ] `IdentityErrors` used for domain aggregate failures: `INVALID_CREDENTIALS`, `TOKEN_INVALID`, `TOKEN_REVOKED`, `ADDRESS_LIMIT`, `ADDRESS_NOT_FOUND`, `EMAIL_TOKEN_INVALID`
- [ ] No EF Core, no HTTP, no database — all in-memory
- [ ] All tests pass
