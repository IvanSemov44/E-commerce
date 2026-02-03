using ECommerce.Application.Interfaces;
using AutoMapper;
using ECommerce.Application.DTOs.Orders;
using ECommerce.Application.DTOs.Common;
using ECommerce.Core.Entities;
using ECommerce.Core.Enums;
using ECommerce.Core.Exceptions;
using ECommerce.Core.Interfaces.Repositories;
using Microsoft.Extensions.Logging;
using System.Threading;

namespace ECommerce.Application.Services;

/// <summary>
/// Service for managing orders.
/// </summary>
public class OrderService : IOrderService
{
    private readonly IPromoCodeService _promoCodeService;
    private readonly IInventoryService _inventoryService;
    private readonly IEmailService _emailService;
    private readonly IMapper _mapper;
    private readonly ILogger<OrderService> _logger;
    private readonly IUnitOfWork _unitOfWork;

    public OrderService(
        IPromoCodeService promoCodeService,
        IInventoryService inventoryService,
        IEmailService emailService,
        IUnitOfWork unitOfWork,
        IMapper mapper,
        ILogger<OrderService> logger)
    {
        _promoCodeService = promoCodeService;
        _inventoryService = inventoryService;
        _emailService = emailService;
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<OrderDetailDto> CreateOrderAsync(Guid userId, CreateOrderDto dto, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Creating order for user {UserId}", userId);

        try
        {
            // Verify user exists
            var user = await _unitOfWork.Users.GetByIdAsync(userId, trackChanges: false, cancellationToken: cancellationToken);
            if (user == null)
            {
                throw new UserNotFoundException(userId);
            }

            // Create order entity
            var order = new Order
            {
                OrderNumber = GenerateOrderNumber(),
                UserId = userId,
                Status = OrderStatus.Pending,
                PaymentStatus = PaymentStatus.Pending,
                PaymentMethod = dto.PaymentMethod,
                Currency = "USD"
            };

            // Map shipping and billing addresses via AutoMapper (service still sets user/type)
            if (dto.ShippingAddress != null)
            {
                var shippingAddress = _mapper.Map<Address>(dto.ShippingAddress);
                shippingAddress.UserId = userId;
                shippingAddress.Type = "Shipping";
                shippingAddress.IsDefault = false;
                // Normalize country code
                shippingAddress.Country = NormalizeCountryCode(dto.ShippingAddress.Country);
                order.ShippingAddress = shippingAddress;
            }

            if (dto.BillingAddress != null)
            {
                var billingAddress = _mapper.Map<Address>(dto.BillingAddress);
                billingAddress.UserId = userId;
                billingAddress.Type = "Billing";
                billingAddress.IsDefault = false;
                billingAddress.Country = NormalizeCountryCode(dto.BillingAddress.Country);
                order.BillingAddress = billingAddress;
            }
            else if (dto.ShippingAddress != null)
            {
                var billingAddress = _mapper.Map<Address>(dto.ShippingAddress);
                billingAddress.UserId = userId;
                billingAddress.Type = "Billing";
                billingAddress.IsDefault = false;
                billingAddress.Country = NormalizeCountryCode(dto.ShippingAddress.Country);
                order.BillingAddress = billingAddress;
            }

            // Process order items and validate stock
            var subtotal = 0m;
            var items = new List<OrderItem>();
            var stockCheckItems = new List<ECommerce.Application.DTOs.Inventory.StockCheckItemDto>();

            if (dto.Items != null && dto.Items.Any())
            {
                foreach (var itemDto in dto.Items)
                {
                    if (!Guid.TryParse(itemDto.ProductId, out var productId))
                    {
                        throw new ProductNotFoundException(itemDto.ProductId);
                    }

                    // Map via AutoMapper and then set parsed ProductId
                    var orderItem = _mapper.Map<OrderItem>(itemDto);
                    orderItem.ProductId = productId;
                    orderItem.Quantity = itemDto.Quantity;

                    items.Add(orderItem);

                    var itemTotal = orderItem.UnitPrice * orderItem.Quantity;
                    subtotal += itemTotal;

                    // Add to stock check list
                    stockCheckItems.Add(new ECommerce.Application.DTOs.Inventory.StockCheckItemDto
                    {
                        ProductId = productId,
                        Quantity = itemDto.Quantity
                    });
                }
            }

            // Validate stock availability before creating order
            if (stockCheckItems.Any())
            {
                var stockCheck = await _inventoryService.CheckStockAvailabilityAsync(stockCheckItems);
                if (!stockCheck.IsAvailable)
                {
                    var firstIssue = stockCheck.Issues.First();
                    throw new InsufficientStockException(firstIssue.ProductName, firstIssue.RequestedQuantity, firstIssue.AvailableQuantity);
                }
            }

            // Calculate totals
            order.Subtotal = subtotal;

            // Apply promo code if provided
            if (!string.IsNullOrWhiteSpace(dto.PromoCode))
            {
                var promoValidation = await _promoCodeService.ValidatePromoCodeAsync(dto.PromoCode, subtotal);
                if (promoValidation.IsValid && promoValidation.PromoCode != null)
                {
                    order.DiscountAmount = promoValidation.DiscountAmount;
                    order.PromoCodeId = promoValidation.PromoCode.Id;
                    _logger.LogInformation("Promo code {Code} applied to order with discount ${Amount}",
                        dto.PromoCode, order.DiscountAmount);
                }
                else
                {
                    throw new InvalidPromoCodeException(dto.PromoCode);
                }
            }
            else
            {
                order.DiscountAmount = 0;
            }

            order.ShippingAmount = subtotal > 100 ? 0 : 10.00m;
            order.TaxAmount = subtotal * 0.08m;
            order.TotalAmount = order.Subtotal + order.ShippingAmount + order.TaxAmount - order.DiscountAmount;
            order.Items = items;

            await _unitOfWork.Orders.AddAsync(order, cancellationToken: cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken: cancellationToken);

            // Reduce stock for each product
            foreach (var item in items)
            {
                if (item.ProductId.HasValue)
                {
                    try
                    {
                        await _inventoryService.ReduceStockAsync(
                            item.ProductId.Value,
                            item.Quantity,
                            "sale",
                            order.Id,
                            userId
                        );
                        _logger.LogInformation("Stock reduced for product {ProductId}: {Quantity} units for order {OrderNumber}",
                            item.ProductId.Value, item.Quantity, order.OrderNumber);
                    }
                    catch (Exception stockEx)
                    {
                        _logger.LogError(stockEx, "Failed to reduce stock for product {ProductId} in order {OrderNumber}",
                            item.ProductId.Value, order.OrderNumber);
                        // Continue - order was created, we'll handle stock manually if needed
                    }
                }
            }

            // Increment promo code usage after successful order creation
            if (order.PromoCodeId.HasValue)
            {
                await _promoCodeService.IncrementUsedCountAsync(order.PromoCodeId.Value);
            }

            // Send order confirmation email
            try
            {
                await _emailService.SendOrderConfirmationEmailAsync(user.Email, order);
                _logger.LogInformation("Order confirmation email sent to {Email}", user.Email);
            }
            catch (Exception emailEx)
            {
                _logger.LogError(emailEx, "Failed to send order confirmation email for order {OrderNumber}", order.OrderNumber);
                // Don't throw - order was created successfully
            }

            _logger.LogInformation("Order created successfully: {OrderNumber}", order.OrderNumber);

            return _mapper.Map<OrderDetailDto>(order);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating order for user {UserId}", userId);
            throw;
        }
    }

    public async Task<OrderDetailDto?> GetOrderByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Retrieving order {OrderId}", id);

        var order = await _unitOfWork.Orders.GetWithItemsAsync(id, cancellationToken: cancellationToken);
        return order != null ? _mapper.Map<OrderDetailDto>(order) : null;
    }

    public async Task<OrderDetailDto?> GetOrderByNumberAsync(string orderNumber, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Retrieving order {OrderNumber}", orderNumber);

        var order = await _unitOfWork.Orders.GetByOrderNumberAsync(orderNumber, cancellationToken: cancellationToken);
        return order != null ? _mapper.Map<OrderDetailDto>(order) : null;
    }

    public async Task<PaginatedResult<OrderDto>> GetUserOrdersAsync(Guid userId, int page = 1, int pageSize = 10, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Retrieving orders for user {UserId}, page {Page}", userId, page);

        var totalCount = await _unitOfWork.Orders.GetUserOrdersCountAsync(userId, cancellationToken: cancellationToken);
        var orders = await _unitOfWork.Orders.GetUserOrdersAsync(userId, (page - 1) * pageSize, pageSize, cancellationToken: cancellationToken);
        var dtos = orders.Select(o => _mapper.Map<OrderDto>(o)).ToList();

        return new PaginatedResult<OrderDto>
        {
            Items = dtos,
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize
        };
    }

    public async Task<OrderDetailDto> UpdateOrderStatusAsync(Guid id, string status, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Updating order {OrderId} status to {Status}", id, status);

        var order = await _unitOfWork.Orders.GetByIdAsync(id, trackChanges: true, cancellationToken: cancellationToken);
        if (order == null)
        {
            throw new OrderNotFoundException(id);
        }

        // Validate status
        if (!Enum.TryParse<OrderStatus>(status, ignoreCase: true, out var orderStatus))
        {
            throw new InvalidOrderStatusException(order.Status.ToString(), status);
        }

        order.Status = orderStatus;
        order.UpdatedAt = DateTime.UtcNow;

        // Update timestamps based on status
        if (orderStatus == OrderStatus.Shipped)
        {
            order.ShippedAt = DateTime.UtcNow;
        }
        else if (orderStatus == OrderStatus.Delivered)
        {
            order.DeliveredAt = DateTime.UtcNow;
        }
        else if (orderStatus == OrderStatus.Cancelled)
        {
            order.CancelledAt = DateTime.UtcNow;
        }

        await _unitOfWork.Orders.UpdateAsync(order, cancellationToken: cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken: cancellationToken);

        _logger.LogInformation("Order {OrderId} status updated to {Status}", id, status);

        return _mapper.Map<OrderDetailDto>(order);
    }

    public async Task<bool> CancelOrderAsync(Guid id, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Cancelling order {OrderId}", id);

        var order = await _unitOfWork.Orders.GetByIdAsync(id, trackChanges: true, cancellationToken: cancellationToken);
        if (order == null)
        {
            return false;
        }

        // Can't cancel if already shipped or delivered
        if (order.Status == OrderStatus.Shipped || order.Status == OrderStatus.Delivered)
        {
            throw new InvalidOrderStatusException(order.Status.ToString(), "Cancelled");
        }

        order.Status = OrderStatus.Cancelled;
        order.CancelledAt = DateTime.UtcNow;
        order.UpdatedAt = DateTime.UtcNow;

        await _unitOfWork.Orders.UpdateAsync(order, cancellationToken: cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken: cancellationToken);

        _logger.LogInformation("Order {OrderId} cancelled successfully", id);

        return true;
    }

    public async Task<PaginatedResult<OrderDto>> GetAllOrdersAsync(int page = 1, int pageSize = 20, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Retrieving all orders, page {Page}", page);

        var allOrders = await _unitOfWork.Orders.GetAllAsync(trackChanges: false, cancellationToken: cancellationToken);
        var totalCount = allOrders.Count();

        var orders = allOrders
            .OrderByDescending(o => o.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        var dtos = orders.Select(o => _mapper.Map<OrderDto>(o)).ToList();

        return new PaginatedResult<OrderDto>
        {
            Items = dtos,
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize
        };
    }

    /// <summary>
    /// Generate a unique order number.
    /// </summary>
    private string GenerateOrderNumber()
    {
        // Format: ORD-YYYYMMDD-XXXXXX (e.g., ORD-20250120-A1B2C3)
        var date = DateTime.UtcNow.ToString("yyyyMMdd");
        var random = Guid.NewGuid().ToString("N").Substring(0, 6).ToUpper();
        return $"ORD-{date}-{random}";
    }

    /// <summary>
    /// Normalize country name to 2-letter ISO country code.
    /// </summary>
    private string NormalizeCountryCode(string country)
    {
        if (string.IsNullOrEmpty(country))
            return "US";

        // If already 2 characters, assume it's a code
        if (country.Length == 2)
            return country.ToUpper();

        // Common country name to code mappings
        var countryMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            { "United States", "US" },
            { "USA", "US" },
            { "Canada", "CA" },
            { "United Kingdom", "UK" },
            { "UK", "UK" },
            { "Mexico", "MX" },
            { "Germany", "DE" },
            { "France", "FR" },
            { "Italy", "IT" },
            { "Spain", "ES" },
            { "Japan", "JP" },
            { "China", "CN" },
            { "India", "IN" },
            { "Australia", "AU" },
            { "Brazil", "BR" }
        };

        if (countryMap.TryGetValue(country, out var code))
            return code;

        // Default: take first 2 characters
        return country.Substring(0, Math.Min(2, country.Length)).ToUpper();
    }
}
