using ECommerce.Identity.Application.Commands.Register;
using ECommerce.Identity.Application.Commands.Login;
using ECommerce.Identity.Application.Commands.Logout;
using ECommerce.Identity.Application.Commands.RefreshToken;
using ECommerce.Identity.Application.Commands.ChangePassword;
using ECommerce.Identity.Application.Commands.UpdateProfile;
using ECommerce.Identity.Application.Commands.AddAddress;
using ECommerce.Identity.Application.Commands.SetDefaultAddress;
using ECommerce.Identity.Application.Commands.DeleteAddress;
using ECommerce.Identity.Application.Commands.DeleteAccount;
using ECommerce.Identity.Application.Commands.VerifyEmail;
using ECommerce.Identity.Application.Commands.ForgotPassword;
using ECommerce.Identity.Application.Commands.ResetPassword;
using ECommerce.Identity.Application.Commands.UpdateUserPreferences;
using ECommerce.Identity.Application.Queries.GetCurrentUser;
using ECommerce.Identity.Application.Queries.GetUserPreferences;
using ECommerce.Identity.Application.Interfaces;
using ECommerce.Identity.Domain.Aggregates.User;
using ECommerce.Identity.Domain.Errors;
using ECommerce.Identity.Application.Errors;
using ECommerce.Identity.Domain.Interfaces;
using ECommerce.Identity.Domain.ValueObjects;
using ECommerce.SharedKernel.Enums;
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

        public Task<User?> GetByIdAsync(Guid id, CancellationToken ct = default)
            => Task.FromResult(Store.FirstOrDefault(u => u.Id == id));

        public Task<User?> GetByEmailAsync(Email email, CancellationToken ct = default)
            => Task.FromResult(Store.FirstOrDefault(u => u.Email.Value == email.Value));

        public Task<bool> EmailExistsAsync(string email, CancellationToken ct = default)
            => Task.FromResult(Store.Any(u => string.Equals(u.Email.Value, email, StringComparison.OrdinalIgnoreCase)));

        public Task<User?> GetByRefreshTokenAsync(string token, CancellationToken ct = default)
            => Task.FromResult(Store.FirstOrDefault(u => u.RefreshTokens.Any(t => t.Token == token)));

        public Task<int> GetCustomersCountAsync(CancellationToken ct = default)
            => Task.FromResult(Store.Count(u => u.Role == UserRole.Customer));

        public Task AddAsync(User user, CancellationToken ct = default)
        {
            Store.Add(user);
            return Task.CompletedTask;
        }

        public Task UpdateAsync(User user, CancellationToken ct = default)
            => Task.CompletedTask; // In-memory, already in Store

        public Task DeleteAsync(User user, CancellationToken ct = default)
        {
            Store.Remove(user);
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

    sealed class FakeAddressProjectionEventPublisher : IAddressProjectionEventPublisher
    {
        public Task PublishAddressProjectionUpdatedAsync(
            Guid addressId,
            Guid userId,
            string streetLine1,
            string city,
            string country,
            string postalCode,
            bool isDeleted,
            CancellationToken cancellationToken = default)
            => Task.CompletedTask;
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static User SeedUser(FakeUserRepository repo, string email = "test@example.com", string rawPassword = "Password1")
    {
        var hash = $"HASH:{rawPassword}";
        var userResult = User.Register(email, "Test", "User", PasswordHash.FromHash(hash).GetDataOrThrow());
        var user = userResult.GetDataOrThrow();
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

        Assert.HasCount(1, repo.Store);
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

        Assert.HasCount(1, user.RefreshTokens);
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
    public async Task LogoutHandler_UnknownUser_ReturnsUserNotFound()
    {
        // Actual implementation returns UserNotFound when user not found
        var handler = new LogoutCommandHandler(new FakeUserRepository(), new FakeUnitOfWork());

        var result = await handler.Handle(
            new LogoutCommand(Guid.NewGuid(), "some-token"),
            CancellationToken.None);

        Assert.IsFalse(result.IsSuccess);
        Assert.AreEqual(IdentityApplicationErrors.UserNotFound.Code, result.GetErrorOrThrow().Code);
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
        user.RevokeRefreshToken("revoked-token"); // Use public API to revoke
        var handler = new RefreshTokenCommandHandler(repo, new FakeJwtTokenService(), new FakeUnitOfWork());

        var result = await handler.Handle(
            new RefreshTokenCommand("revoked-token"),
            CancellationToken.None);

        Assert.IsFalse(result.IsSuccess);
        Assert.AreEqual(IdentityErrors.TokenRevoked.Code, result.GetErrorOrThrow().Code);
    }

    [TestMethod]
    public async Task RefreshTokenHandler_ExpiredToken_ReturnsTokenRevoked()
    {
        var repo = new FakeUserRepository();
        var user = SeedUser(repo);
        user.AddRefreshToken("expired-token", DateTime.UtcNow.AddDays(-1)); // already expired
        var handler = new RefreshTokenCommandHandler(repo, new FakeJwtTokenService(), new FakeUnitOfWork());

        var result = await handler.Handle(
            new RefreshTokenCommand("expired-token"),
            CancellationToken.None);

        Assert.IsFalse(result.IsSuccess);
        // Expired tokens are not "active", so GetActiveRefreshToken returns null → TokenRevoked
        Assert.AreEqual(IdentityErrors.TokenRevoked.Code, result.GetErrorOrThrow().Code);
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
        var handler = new AddAddressCommandHandler(repo, new FakeUnitOfWork(), new FakeAddressProjectionEventPublisher());

        var result = await handler.Handle(
            new AddAddressCommand(user.Id, "123 Main St", "Springfield", "US", "12345"),
            CancellationToken.None);

        Assert.IsTrue(result.IsSuccess);
        Assert.HasCount(1, user.Addresses);
        Assert.AreEqual("123 Main St", user.Addresses.First().Street);
    }

    [TestMethod]
    public async Task AddAddressHandler_ExceedsLimit_ReturnsFailure()
    {
        var repo = new FakeUserRepository();
        var user = SeedUser(repo);
        for (int i = 0; i < 5; i++)
            user.AddAddress($"{i} Street", "City", "US", null);

        var handler = new AddAddressCommandHandler(repo, new FakeUnitOfWork(), new FakeAddressProjectionEventPublisher());

        var result = await handler.Handle(
            new AddAddressCommand(user.Id, "6th Street", "City", "US", null),
            CancellationToken.None);

        Assert.IsFalse(result.IsSuccess);
        Assert.AreEqual(IdentityErrors.AddressLimit.Code, result.GetErrorOrThrow().Code);
    }

    [TestMethod]
    public async Task AddAddressHandler_UserNotFound_ReturnsUserNotFound()
    {
        var handler = new AddAddressCommandHandler(new FakeUserRepository(), new FakeUnitOfWork(), new FakeAddressProjectionEventPublisher());

        var result = await handler.Handle(
            new AddAddressCommand(Guid.NewGuid(), "123 Main St", "City", "US", null),
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

    [TestMethod]
    public async Task VerifyEmailHandler_UserNotFound_ReturnsUserNotFound()
    {
        var handler = new VerifyEmailCommandHandler(new FakeUserRepository(), new FakeUnitOfWork());

        var result = await handler.Handle(
            new VerifyEmailCommand(Guid.NewGuid(), "some-token"),
            CancellationToken.None);

        Assert.IsFalse(result.IsSuccess);
        Assert.AreEqual(IdentityApplicationErrors.UserNotFound.Code, result.GetErrorOrThrow().Code);
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

    // ── SetDefaultAddressCommandHandler ──────────────────────────────────────

    [TestMethod]
    public async Task SetDefaultAddressHandler_ValidAddress_SwitchesDefault()
    {
        var repo = new FakeUserRepository();
        var user = SeedUser(repo);
        user.AddAddress("123 Main St", "City", "US", null);
        user.AddAddress("456 Oak Ave", "City", "US", null);
        var secondAddressId = user.Addresses.Last().Id;
        var handler = new SetDefaultAddressCommandHandler(repo, new FakeUnitOfWork(), new FakeAddressProjectionEventPublisher());

        var result = await handler.Handle(
            new SetDefaultAddressCommand(user.Id, secondAddressId),
            CancellationToken.None);

        Assert.IsTrue(result.IsSuccess);
        Assert.IsTrue(user.Addresses.First(a => a.Id == secondAddressId).IsDefaultShipping);
    }

    [TestMethod]
    public async Task SetDefaultAddressHandler_UserNotFound_ReturnsUserNotFound()
    {
        var handler = new SetDefaultAddressCommandHandler(new FakeUserRepository(), new FakeUnitOfWork(), new FakeAddressProjectionEventPublisher());

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
        var handler = new SetDefaultAddressCommandHandler(repo, new FakeUnitOfWork(), new FakeAddressProjectionEventPublisher());

        var result = await handler.Handle(
            new SetDefaultAddressCommand(user.Id, Guid.NewGuid()), // nonexistent address id
            CancellationToken.None);

        Assert.IsFalse(result.IsSuccess);
        Assert.AreEqual(IdentityErrors.AddressNotFound.Code, result.GetErrorOrThrow().Code);
    }

    // ── DeleteAddressCommandHandler ───────────────────────────────────────────

    [TestMethod]
    public async Task DeleteAddressHandler_ExistingAddress_RemovesAddressAndSucceeds()
    {
        var repo = new FakeUserRepository();
        var user = SeedUser(repo);
        user.AddAddress("123 Main St", "City", "US", null);
        var addressId = user.Addresses.First().Id;
        var handler = new DeleteAddressCommandHandler(repo, new FakeUnitOfWork(), new FakeAddressProjectionEventPublisher());

        var result = await handler.Handle(
            new DeleteAddressCommand(user.Id, addressId),
            CancellationToken.None);

        Assert.IsTrue(result.IsSuccess);
        Assert.IsFalse(user.Addresses.Any(a => a.Id == addressId));
    }

    [TestMethod]
    public async Task DeleteAddressHandler_UserNotFound_ReturnsUserNotFound()
    {
        var handler = new DeleteAddressCommandHandler(new FakeUserRepository(), new FakeUnitOfWork(), new FakeAddressProjectionEventPublisher());

        var result = await handler.Handle(
            new DeleteAddressCommand(Guid.NewGuid(), Guid.NewGuid()),
            CancellationToken.None);

        Assert.IsFalse(result.IsSuccess);
        Assert.AreEqual(IdentityApplicationErrors.UserNotFound.Code, result.GetErrorOrThrow().Code);
    }

    [TestMethod]
    public async Task DeleteAddressHandler_AddressNotFound_ReturnsAddressNotFound()
    {
        var repo = new FakeUserRepository();
        var user = SeedUser(repo);
        var handler = new DeleteAddressCommandHandler(repo, new FakeUnitOfWork(), new FakeAddressProjectionEventPublisher());

        var result = await handler.Handle(
            new DeleteAddressCommand(user.Id, Guid.NewGuid()),
            CancellationToken.None);

        Assert.IsFalse(result.IsSuccess);
        Assert.AreEqual(IdentityApplicationErrors.AddressNotFound.Code, result.GetErrorOrThrow().Code);
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
        Assert.IsEmpty(repo.Store);
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

    // ── ResetPasswordCommandHandler ──────────────────────────────────────────

    [TestMethod]
    public async Task ResetPasswordHandler_ValidToken_Succeeds()
    {
        var repo = new FakeUserRepository();
        var user = SeedUser(repo, "user@example.com", "OldPass1");
        user.SetPasswordResetToken("valid-token", DateTime.UtcNow.AddHours(1));
        var handler = new ResetPasswordCommandHandler(repo, new FakePasswordHasher(), new FakeUnitOfWork());

        var result = await handler.Handle(
            new ResetPasswordCommand("user@example.com", "valid-token", "NewPass1"),
            CancellationToken.None);

        Assert.IsTrue(result.IsSuccess);
        Assert.AreEqual("HASH:NewPass1", user.PasswordHash.Hash);
        Assert.IsNull(user.PasswordResetToken);
    }

    [TestMethod]
    public async Task ResetPasswordHandler_InvalidToken_ReturnsInvalidCredentials()
    {
        var repo = new FakeUserRepository();
        var user = SeedUser(repo);
        user.SetPasswordResetToken("valid-token", DateTime.UtcNow.AddHours(1));
        var handler = new ResetPasswordCommandHandler(repo, new FakePasswordHasher(), new FakeUnitOfWork());

        var result = await handler.Handle(
            new ResetPasswordCommand("user@example.com", "wrong-token", "NewPass1"),
            CancellationToken.None);

        Assert.IsFalse(result.IsSuccess);
        Assert.AreEqual(IdentityErrors.InvalidCredentials.Code, result.GetErrorOrThrow().Code);
    }

    [TestMethod]
    public async Task ResetPasswordHandler_UnknownEmail_ReturnsInvalidCredentials()
    {
        var handler = new ResetPasswordCommandHandler(
            new FakeUserRepository(), new FakePasswordHasher(), new FakeUnitOfWork());

        var result = await handler.Handle(
            new ResetPasswordCommand("ghost@example.com", "any-token", "NewPass1"),
            CancellationToken.None);

        Assert.IsFalse(result.IsSuccess);
        Assert.AreEqual(IdentityErrors.InvalidCredentials.Code, result.GetErrorOrThrow().Code);
    }

    [TestMethod]
    public async Task ResetPasswordHandler_WeakPassword_ReturnsFailure()
    {
        var repo = new FakeUserRepository();
        var user = SeedUser(repo);
        user.SetPasswordResetToken("valid-token", DateTime.UtcNow.AddHours(1));
        var handler = new ResetPasswordCommandHandler(repo, new FakePasswordHasher(), new FakeUnitOfWork());

        var result = await handler.Handle(
            new ResetPasswordCommand("user@example.com", "valid-token", "weak"),
            CancellationToken.None);

        Assert.IsFalse(result.IsSuccess);
    }

    // ── GetUserPreferencesQueryHandler ────────────────────────────────────────

    [TestMethod]
    public async Task GetUserPreferencesHandler_ExistingUser_ReturnsDefaults()
    {
        var repo = new FakeUserRepository();
        var user = SeedUser(repo, "user@example.com");
        var handler = new GetUserPreferencesQueryHandler(repo);

        var result = await handler.Handle(
            new GetUserPreferencesQuery(user.Id),
            CancellationToken.None);

        Assert.IsTrue(result.IsSuccess);
        var prefs = result.GetDataOrThrow();
        Assert.IsTrue(prefs.EmailNotifications);
        Assert.IsFalse(prefs.SmsNotifications);
        Assert.AreEqual("en", prefs.Language);
    }

    [TestMethod]
    public async Task GetUserPreferencesHandler_UnknownUser_ReturnsUserNotFound()
    {
        var handler = new GetUserPreferencesQueryHandler(new FakeUserRepository());

        var result = await handler.Handle(
            new GetUserPreferencesQuery(Guid.NewGuid()),
            CancellationToken.None);

        Assert.IsFalse(result.IsSuccess);
        Assert.AreEqual(IdentityApplicationErrors.UserNotFound.Code, result.GetErrorOrThrow().Code);
    }

    // ── UpdateUserPreferencesCommandHandler ───────────────────────────────────

    [TestMethod]
    public async Task UpdateUserPreferencesHandler_ValidCommand_ReturnsUpdatedDto()
    {
        var repo = new FakeUserRepository();
        var user = SeedUser(repo);
        var handler = new UpdateUserPreferencesCommandHandler(repo, new FakeUnitOfWork());

        var result = await handler.Handle(
            new UpdateUserPreferencesCommand(user.Id, false, true, false, "bg", "BGN", true),
            CancellationToken.None);

        Assert.IsTrue(result.IsSuccess);
        var prefs = result.GetDataOrThrow();
        Assert.IsFalse(prefs.EmailNotifications);
        Assert.IsTrue(prefs.SmsNotifications);
        Assert.AreEqual("bg", prefs.Language);
        Assert.AreEqual("BGN", prefs.Currency);
    }

    [TestMethod]
    public async Task UpdateUserPreferencesHandler_UserNotFound_ReturnsUserNotFound()
    {
        var handler = new UpdateUserPreferencesCommandHandler(new FakeUserRepository(), new FakeUnitOfWork());

        var result = await handler.Handle(
            new UpdateUserPreferencesCommand(Guid.NewGuid(), true, false, true, "en", "USD", false),
            CancellationToken.None);

        Assert.IsFalse(result.IsSuccess);
        Assert.AreEqual(IdentityApplicationErrors.UserNotFound.Code, result.GetErrorOrThrow().Code);
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
