using AutoMapper;
using ECommerce.Core.Common;
using ECommerce.Core.Interfaces.Repositories;
using Microsoft.Extensions.Logging;
using Moq;

namespace ECommerce.Tests.Helpers;

/// <summary>
/// Helper methods for creating mocks in unit tests.
/// </summary>
public static class MockHelpers
{
    /// <summary>
    /// Creates a mock IUnitOfWork with default setup.
    /// </summary>
    public static Mock<IUnitOfWork> CreateMockUnitOfWork()
    {
        var mock = new Mock<IUnitOfWork>();
        mock.Setup(u => u.SaveChangesAsync())
            .ReturnsAsync(1);

        var mockTransaction = new Mock<IAsyncTransaction>();
        mockTransaction.Setup(t => t.CommitAsync())
            .Returns(Task.CompletedTask);
        mockTransaction.Setup(t => t.RollbackAsync())
            .Returns(Task.CompletedTask);

        mock.Setup(u => u.BeginTransactionAsync())
            .ReturnsAsync(mockTransaction.Object);

        return mock;
    }

    /// <summary>
    /// Creates a mock repository with a backing list for testing.
    /// </summary>
    public static Mock<IRepository<T>> CreateMockRepository<T>(List<T>? items = null)
        where T : BaseEntity
    {
        items ??= new List<T>();
        var mock = new Mock<IRepository<T>>();

        // GetAllAsync
        mock.Setup(r => r.GetAllAsync())
            .ReturnsAsync(() => items.ToList());

        // GetByIdAsync
        mock.Setup(r => r.GetByIdAsync(It.IsAny<Guid>()))
            .ReturnsAsync((Guid id) => items.FirstOrDefault(i => i.Id == id));

        // AddAsync - returns the entity
        mock.Setup(r => r.AddAsync(It.IsAny<T>()))
            .ReturnsAsync((T item) =>
            {
                if (item.Id == Guid.Empty)
                    item.Id = Guid.NewGuid();
                items.Add(item);
                return item;
            });

        // UpdateAsync
        mock.Setup(r => r.UpdateAsync(It.IsAny<T>()))
            .Callback<T>((item) =>
            {
                var existing = items.FirstOrDefault(i => i.Id == item.Id);
                if (existing != null)
                {
                    items.Remove(existing);
                    items.Add(item);
                }
            })
            .Returns(Task.CompletedTask);

        // DeleteAsync
        mock.Setup(r => r.DeleteAsync(It.IsAny<T>()))
            .Callback<T>((item) => items.Remove(item))
            .Returns(Task.CompletedTask);

        return mock;
    }

    /// <summary>
    /// Creates a mock logger for testing.
    /// </summary>
    public static Mock<ILogger<T>> CreateMockLogger<T>()
    {
        return new Mock<ILogger<T>>();
    }

    /// <summary>
    /// Creates a basic mock IMapper for testing.
    /// Note: This is a simple mock - for complex mapping scenarios,
    /// tests should set up specific Map calls.
    /// </summary>
    public static Mock<IMapper> CreateMockMapper()
    {
        var mock = new Mock<IMapper>();

        // Setup default behavior to return a new instance of TDestination
        // Tests can override this for specific scenarios
        mock.Setup(m => m.Map<It.IsAnyType>(It.IsAny<object>()))
            .Returns((object source) =>
            {
                // Return null for now - tests will setup specific mappings
                return null!;
            });

        return mock;
    }
}
