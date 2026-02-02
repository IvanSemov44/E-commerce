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

        // GetAllAsync (matches signature with optional trackChanges)
        mock.Setup(r => r.GetAllAsync(It.IsAny<bool>()))
            .ReturnsAsync((bool _)=> items.ToList());

        // GetByIdAsync (matches signature with optional trackChanges)
        mock.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<bool>()))
            .ReturnsAsync((Guid id, bool _) => items.FirstOrDefault(i => i.Id == id));

        // AddAsync - assign id and add to backing list, return completed task
        mock.Setup(r => r.AddAsync(It.IsAny<T>()))
            .Callback<T>((T item) =>
            {
                if (item.Id == Guid.Empty)
                    item.Id = Guid.NewGuid();
                items.Add(item);
            })
            .Returns(Task.CompletedTask);

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

        // Provide simple, null-safe mappings for cart-related DTOs so unit tests
        // using the mapper without explicit setups won't receive nulls.
        mock.Setup(m => m.Map<ECommerce.Application.DTOs.Cart.CartDto>(It.IsAny<object>())).Returns((object src) =>
        {
            if (src == null) return null!;
            var cart = src as ECommerce.Core.Entities.Cart;
            if (cart == null) return null!;

            var dto = new ECommerce.Application.DTOs.Cart.CartDto
            {
                Id = cart.Id,
                Items = cart.Items?.Select(i => new ECommerce.Application.DTOs.Cart.CartItemDto
                {
                    Id = i.Id,
                    ProductId = i.Product != null ? i.Product.Id : i.ProductId,
                    ProductName = i.Product != null ? i.Product.Name : string.Empty,
                    ProductImage = i.Product != null && i.Product.Images.FirstOrDefault() != null ? i.Product.Images.FirstOrDefault()!.Url : null,
                    Price = i.Product != null ? i.Product.Price : 0m,
                    Quantity = i.Quantity,
                    Total = (i.Product != null ? i.Product.Price : 0m) * i.Quantity
                }).ToList() ?? new List<ECommerce.Application.DTOs.Cart.CartItemDto>()
            };

            dto.Subtotal = dto.Items.Sum(x => x.Total);
            dto.Total = dto.Subtotal;
            return dto;
        });

        mock.Setup(m => m.Map<ECommerce.Application.DTOs.Cart.CartItemDto>(It.IsAny<object>())).Returns((object src) =>
        {
            if (src == null) return null!;
            var item = src as ECommerce.Core.Entities.CartItem;
            if (item == null) return null!;

            var dto = new ECommerce.Application.DTOs.Cart.CartItemDto
            {
                Id = item.Id,
                ProductId = item.Product != null ? item.Product.Id : item.ProductId,
                ProductName = item.Product != null ? item.Product.Name : string.Empty,
                ProductImage = item.Product != null && item.Product.Images.FirstOrDefault() != null ? item.Product.Images.FirstOrDefault()!.Url : null,
                Price = item.Product != null ? item.Product.Price : 0m,
                Quantity = item.Quantity,
                Total = (item.Product != null ? item.Product.Price : 0m) * item.Quantity
            };

            return dto;
        });

        // Note: No broad fallback mapping is configured so specific setups above
        // will be used. Tests that require other mappings should explicitly
        // configure the mock in their setup.

        return mock;
    }
}
