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
        mock.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var mockTransaction = new Mock<IAsyncTransaction>();
        mockTransaction.Setup(t => t.CommitAsync())
            .Returns(Task.CompletedTask);
        mockTransaction.Setup(t => t.RollbackAsync())
            .Returns(Task.CompletedTask);

        mock.Setup(u => u.BeginTransactionAsync(It.IsAny<CancellationToken>()))
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
        mock.Setup(r => r.GetAllAsync(It.IsAny<bool>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((bool _, CancellationToken __) => items.ToList());

        // GetByIdAsync (matches signature with optional trackChanges)
        mock.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Guid id, bool _, CancellationToken __) => items.FirstOrDefault(i => i.Id == id));

        // AddAsync - assign id and add to backing list, return completed task
        mock.Setup(r => r.AddAsync(It.IsAny<T>(), It.IsAny<CancellationToken>()))
            .Callback<T, CancellationToken>((item, _) =>
            {
                if (item.Id == Guid.Empty)
                    item.Id = Guid.NewGuid();
                items.Add(item);
            })
            .Returns(Task.CompletedTask);

        // UpdateAsync
        mock.Setup(r => r.UpdateAsync(It.IsAny<T>(), It.IsAny<CancellationToken>()))
            .Callback<T, CancellationToken>((item, _) =>
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
        mock.Setup(r => r.DeleteAsync(It.IsAny<T>(), It.IsAny<CancellationToken>()))
            .Callback<T, CancellationToken>((item, _) => items.Remove(item))
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

        // Provide mapping for Product -> WishlistItemDto used by wishlist service tests
        mock.Setup(m => m.Map<ECommerce.Application.DTOs.Wishlist.WishlistItemDto>(It.IsAny<object>())).Returns((object src) =>
        {
            if (src == null) return null!;
            var prod = src as ECommerce.Core.Entities.Product;
            if (prod == null) return null!;

            return new ECommerce.Application.DTOs.Wishlist.WishlistItemDto
            {
                Id = Guid.Empty,
                ProductId = prod.Id,
                ProductName = prod.Name,
                ProductImage = prod.Images.FirstOrDefault() != null ? prod.Images.FirstOrDefault()!.Url : null,
                Price = prod.Price,
                CompareAtPrice = prod.CompareAtPrice,
                StockQuantity = prod.StockQuantity,
                IsAvailable = prod.IsActive && prod.StockQuantity > 0,
                AddedAt = DateTime.MinValue
            };
        });

        // Provide mapping for CreateOrderItemDto -> OrderItem used by OrderService tests
        // Note: ProductName, Price, and ImageUrl are now populated by OrderService from Product entity
        mock.Setup(m => m.Map<ECommerce.Core.Entities.OrderItem>(It.IsAny<object>())).Returns((object src) =>
        {
            if (src == null) return null!;
            var dto = src as ECommerce.Application.DTOs.Orders.CreateOrderItemDto;
            if (dto == null) return null!;

            return new ECommerce.Core.Entities.OrderItem
            {
                Id = Guid.Empty,
                ProductId = null,
                ProductName = string.Empty,
                ProductSku = null,
                ProductImageUrl = null,
                Quantity = dto.Quantity,
                UnitPrice = 0m,
                TotalPrice = 0m
            };
        });

        // Provide mapping for AddressDto -> Address used by OrderService tests
        mock.Setup(m => m.Map<ECommerce.Core.Entities.Address>(It.IsAny<object>())).Returns((object src) =>
        {
            if (src == null) return null!;
            var dto = src as ECommerce.Application.DTOs.Common.AddressDto;
            if (dto == null) return null!;

            return new ECommerce.Core.Entities.Address
            {
                Id = Guid.Empty,
                UserId = Guid.Empty,
                Type = string.Empty,
                FirstName = dto.FirstName,
                LastName = dto.LastName,
                Company = dto.Company,
                StreetLine1 = dto.StreetLine1,
                StreetLine2 = dto.StreetLine2,
                City = dto.City,
                State = dto.State,
                PostalCode = dto.PostalCode,
                Country = dto.Country,
                Phone = dto.Phone,
                IsDefault = false
            };
        });

        // Note: No broad fallback mapping is configured so specific setups above
        // will be used. Tests that require other mappings should explicitly
        // configure the mock in their setup.

        // Provide mapping for KeyValuePair<DateTime,int> -> OrderTrendDto
        mock.Setup(m => m.Map<ECommerce.Application.DTOs.Dashboard.OrderTrendDto>(It.IsAny<object>())).Returns((object src) =>
        {
            if (src == null) return null!;
            if (src is KeyValuePair<DateTime, int> kv)
            {
                return new ECommerce.Application.DTOs.Dashboard.OrderTrendDto
                {
                    Date = kv.Key.ToString("yyyy-MM-dd"),
                    Count = kv.Value
                };
            }
            return null!;
        });

        // Provide mapping for KeyValuePair<DateTime,decimal> -> RevenueTrendDto
        mock.Setup(m => m.Map<ECommerce.Application.DTOs.Dashboard.RevenueTrendDto>(It.IsAny<object>())).Returns((object src) =>
        {
            if (src == null) return null!;
            if (src is KeyValuePair<DateTime, decimal> kv)
            {
                return new ECommerce.Application.DTOs.Dashboard.RevenueTrendDto
                {
                    Date = kv.Key.ToString("yyyy-MM-dd"),
                    Amount = kv.Value
                };
            }
            return null!;
        });

        return mock;
    }
}
