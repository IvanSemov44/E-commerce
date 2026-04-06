using ECommerce.Contracts;
using ECommerce.Ordering.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace ECommerce.Tests.Integration;

[TestClass]
public class OrderingAddressProjectionSyncCharacterizationTests
{
    [TestMethod]
    public async Task Publish_WhenProjectionMissing_InsertsAddressProjection()
    {
        var addressId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        await using var scope = CreateScope($"ordering-address-proj-{Guid.NewGuid():N}");
        var db = scope.ServiceProvider.GetRequiredService<OrderingDbContext>();
        var publisher = scope.ServiceProvider.GetRequiredService<IPublisher>();

        var evt = new AddressProjectionUpdatedIntegrationEvent(
            addressId,
            userId,
            "42 Reader St",
            "Testville",
            "US",
            "10001",
            false,
            DateTime.UtcNow);

        await publisher.Publish(evt, CancellationToken.None);

        var projection = await db.Addresses.SingleOrDefaultAsync(x => x.Id == addressId);
        Assert.IsNotNull(projection);
        Assert.AreEqual(userId, projection.UserId);
        Assert.AreEqual("42 Reader St", projection.StreetLine1);
        Assert.AreEqual("Testville", projection.City);
        Assert.AreEqual("US", projection.Country);
        Assert.AreEqual("10001", projection.PostalCode);
    }

    [TestMethod]
    public async Task Publish_WhenProjectionExists_UpdatesAddressProjection()
    {
        var addressId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        await using var scope = CreateScope($"ordering-address-proj-{Guid.NewGuid():N}");
        var db = scope.ServiceProvider.GetRequiredService<OrderingDbContext>();
        var publisher = scope.ServiceProvider.GetRequiredService<IPublisher>();

        db.Addresses.Add(new AddressReadModel
        {
            Id = addressId,
            UserId = userId,
            StreetLine1 = "Old Street",
            City = "Old City",
            Country = "US",
            PostalCode = "90001"
        });
        await db.SaveChangesAsync();

        var evt = new AddressProjectionUpdatedIntegrationEvent(
            addressId,
            userId,
            "New Street",
            "New City",
            "US",
            "10002",
            false,
            DateTime.UtcNow);

        await publisher.Publish(evt, CancellationToken.None);

        var projection = await db.Addresses.SingleOrDefaultAsync(x => x.Id == addressId);
        Assert.IsNotNull(projection);
        Assert.AreEqual("New Street", projection.StreetLine1);
        Assert.AreEqual("New City", projection.City);
        Assert.AreEqual("10002", projection.PostalCode);
    }

    [TestMethod]
    public async Task Publish_WhenDeleted_RemovesAddressProjection()
    {
        var addressId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        await using var scope = CreateScope($"ordering-address-proj-{Guid.NewGuid():N}");
        var db = scope.ServiceProvider.GetRequiredService<OrderingDbContext>();
        var publisher = scope.ServiceProvider.GetRequiredService<IPublisher>();

        db.Addresses.Add(new AddressReadModel
        {
            Id = addressId,
            UserId = userId,
            StreetLine1 = "Delete Street",
            City = "Delete City",
            Country = "US",
            PostalCode = "10003"
        });
        await db.SaveChangesAsync();

        var evt = new AddressProjectionUpdatedIntegrationEvent(
            addressId,
            userId,
            "Delete Street",
            "Delete City",
            "US",
            "10003",
            true,
            DateTime.UtcNow);

        await publisher.Publish(evt, CancellationToken.None);

        var projection = await db.Addresses.SingleOrDefaultAsync(x => x.Id == addressId);
        Assert.IsNull(projection);
    }

    private static AsyncServiceScope CreateScope(string databaseName)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddDbContext<OrderingDbContext>(options => options.UseInMemoryDatabase(databaseName));
        services.AddMediatR(config => config.RegisterServicesFromAssembly(typeof(OrderingDbContext).Assembly));

        var provider = services.BuildServiceProvider();
        return provider.CreateAsyncScope();
    }
}
