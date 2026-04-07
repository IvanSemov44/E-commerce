using ECommerce.Identity.Application.Queries.GetUserStats;
using ECommerce.Identity.Domain.Aggregates.User;
using ECommerce.Identity.Domain.Interfaces;
using ECommerce.Identity.Domain.ValueObjects;

namespace ECommerce.Identity.Tests.Application;

[TestClass]
public class QueryHandlerTests
{
    private sealed class FakeUserRepository : IUserRepository
    {
        private readonly List<User> _store = new();

        public Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
            => Task.FromResult(_store.FirstOrDefault(x => x.Id == id));

        public Task<User?> GetByEmailAsync(Email email, CancellationToken cancellationToken = default)
            => Task.FromResult(_store.FirstOrDefault(x => x.Email.Value == email.Value));

        public Task<bool> EmailExistsAsync(string email, CancellationToken cancellationToken = default)
            => Task.FromResult(_store.Any(x => x.Email.Value == email));

        public Task<User?> GetByRefreshTokenAsync(string token, CancellationToken cancellationToken = default)
            => Task.FromResult(_store.FirstOrDefault(x => x.RefreshTokens.Any(t => t.Token == token)));

        public Task<int> GetCustomersCountAsync(CancellationToken cancellationToken = default)
            => Task.FromResult(_store.Count(x => x.Role == UserRole.Customer));

        public Task AddAsync(User user, CancellationToken cancellationToken = default)
        {
            _store.Add(user);
            return Task.CompletedTask;
        }

        public Task UpdateAsync(User user, CancellationToken cancellationToken = default)
            => Task.CompletedTask;

        public Task DeleteAsync(User user, CancellationToken cancellationToken = default)
            => Task.CompletedTask;
    }

    [TestMethod]
    public async Task Handle_ReturnsOnlyCustomerCount()
    {
        var repo = new FakeUserRepository();
        await repo.AddAsync(CreateUser("customer1@test.com", UserRole.Customer));
        await repo.AddAsync(CreateUser("customer2@test.com", UserRole.Customer));

        var handler = new GetUserStatsQueryHandler(repo);
        var result = await handler.Handle(new GetUserStatsQuery(), CancellationToken.None);

        Assert.IsTrue(result.IsSuccess);
        Assert.AreEqual(2, result.GetDataOrThrow().TotalCustomers);
    }

    private static User CreateUser(string emailValue, UserRole role)
    {
        var email = Email.Create(emailValue).GetDataOrThrow();
        var name = PersonName.Create("Test", "User").GetDataOrThrow();
        var password = PasswordHash.FromHash("HASH:Password123").GetDataOrThrow();

        var user = User.Register(email.Value, name.First, name.Last, password).GetDataOrThrow();
        _ = role;

        return user;
    }
}
