using Bogus;
using ECommerce.Core.Entities;
using ECommerce.Core.Enums;

namespace ECommerce.Tests.Helpers;

/// <summary>
/// Factory for creating test data using Bogus.
/// </summary>
public static class TestDataFactory
{
    private static readonly Faker _faker = new();

    public static User CreateUser(
        string? email = null,
        UserRole role = UserRole.Customer,
        bool isEmailVerified = true)
    {
        return new User
        {
            Id = Guid.NewGuid(),
            Email = email ?? _faker.Internet.Email(),
            FirstName = _faker.Name.FirstName(),
            LastName = _faker.Name.LastName(),
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("TestPass123!"),
            Role = role,
            IsEmailVerified = isEmailVerified,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
    }

    public static Product CreateProduct(
        Guid? categoryId = null,
        decimal? price = null,
        int? stock = null,
        bool isActive = true,
        string? name = null,
        string? slug = null)
    {
        var productName = name ?? _faker.Commerce.ProductName();
        var productSlug = slug ?? productName.ToLower().Replace(" ", "-");

        return new Product
        {
            Id = Guid.NewGuid(),
            Name = productName,
            Slug = productSlug,
            Description = _faker.Commerce.ProductDescription(),
            Price = price ?? _faker.Random.Decimal(10, 1000),
            StockQuantity = stock ?? _faker.Random.Int(0, 100),
            CategoryId = categoryId ?? Guid.NewGuid(),
            IsActive = isActive,
            IsFeatured = _faker.Random.Bool(),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
    }

    public static Category CreateCategory(
        string? name = null,
        string? slug = null,
        bool isActive = true)
    {
        var categoryName = name ?? _faker.Commerce.Categories(1)[0];
        var categorySlug = slug ?? categoryName.ToLower().Replace(" ", "-");

        return new Category
        {
            Id = Guid.NewGuid(),
            Name = categoryName,
            Slug = categorySlug,
            Description = _faker.Lorem.Sentence(),
            IsActive = isActive,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
    }

    public static PromoCode CreatePromoCode(
        string? code = null,
        string discountType = "percentage",
        decimal discountValue = 10,
        bool isActive = true,
        DateTime? startDate = null,
        DateTime? endDate = null,
        int? maxUses = null,
        int usedCount = 0,
        decimal? minOrderAmount = null,
        decimal? maxDiscountAmount = null)
    {
        return new PromoCode
        {
            Id = Guid.NewGuid(),
            Code = code ?? _faker.Random.AlphaNumeric(8).ToUpper(),
            DiscountType = discountType,
            DiscountValue = discountValue,
            MinOrderAmount = minOrderAmount,
            MaxDiscountAmount = maxDiscountAmount,
            MaxUses = maxUses,
            UsedCount = usedCount,
            StartDate = startDate,
            EndDate = endDate,
            IsActive = isActive,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
    }

    public static Order CreateOrder(
        Guid userId,
        decimal? totalAmount = null,
        OrderStatus status = OrderStatus.Pending,
        PaymentStatus paymentStatus = PaymentStatus.Pending,
        string? orderNumber = null)
    {
        var amount = totalAmount ?? _faker.Random.Decimal(50, 1000);
        return new Order
        {
            Id = Guid.NewGuid(),
            OrderNumber = orderNumber ?? $"ORD-{DateTime.UtcNow:yyyyMMdd}-{_faker.Random.Int(1000, 9999)}",
            UserId = userId,
            TotalAmount = amount,
            Subtotal = amount,
            Status = status,
            PaymentStatus = paymentStatus,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
    }

    public static OrderItem CreateOrderItem(
        Guid orderId,
        Guid productId,
        int quantity = 1,
        decimal? unitPrice = null)
    {
        var price = unitPrice ?? _faker.Random.Decimal(10, 100);
        return new OrderItem
        {
            Id = Guid.NewGuid(),
            OrderId = orderId,
            ProductId = productId,
            ProductName = "Test Product",
            Quantity = quantity,
            UnitPrice = price,
            TotalPrice = price * quantity,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
    }

    public static Cart CreateCart(Guid? userId = null, string? sessionId = null)
    {
        return new Cart
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            SessionId = sessionId ?? Guid.NewGuid().ToString(),
            Items = new List<CartItem>(),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
    }

    public static CartItem CreateCartItem(
        Guid cartId,
        Guid productId,
        int quantity = 1)
    {
        return new CartItem
        {
            Id = Guid.NewGuid(),
            CartId = cartId,
            ProductId = productId,
            Quantity = quantity,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
    }

    public static Review CreateReview(
        Guid userId,
        Guid productId,
        int rating = 5,
        string? comment = null)
    {
        return new Review
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            ProductId = productId,
            Rating = rating,
            Comment = comment ?? _faker.Lorem.Paragraph(),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
    }

    public static Wishlist CreateWishlistItem(Guid userId, Guid productId)
    {
        return new Wishlist
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            ProductId = productId,
            CreatedAt = DateTime.UtcNow,
            // Product navigation property is not set by default
            // Tests that need Product should set it explicitly or use the overload below
        };
    }

    public static Wishlist CreateWishlistItem(Guid userId, Guid productId, Product product)
    {
        return new Wishlist
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            ProductId = productId,
            CreatedAt = DateTime.UtcNow,
            Product = product
        };
    }

    public static Address CreateAddress(Guid userId)
    {
        return new Address
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Type = "Shipping",
            FirstName = _faker.Name.FirstName(),
            LastName = _faker.Name.LastName(),
            Phone = _faker.Phone.PhoneNumber(),
            StreetLine1 = _faker.Address.StreetAddress(),
            City = _faker.Address.City(),
            State = _faker.Address.State(),
            PostalCode = _faker.Address.ZipCode(),
            Country = _faker.Address.Country(),
            IsDefault = false,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
    }

    public static InventoryLog CreateInventoryLog(
        Guid productId,
        int quantityChange,
        string reason = "Test adjustment")
    {
        return new InventoryLog
        {
            Id = Guid.NewGuid(),
            ProductId = productId,
            QuantityChange = quantityChange,
            Reason = reason,
            CreatedAt = DateTime.UtcNow
        };
    }
}
