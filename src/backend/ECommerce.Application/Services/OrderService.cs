using ECommerce.Application.Interfaces;
using AutoMapper;
using ECommerce.Application.Configuration;
using ECommerce.Application.DTOs.Orders;
using ECommerce.Application.DTOs.Common;
using ECommerce.Core.Entities;
using ECommerce.Core.Enums;
using ECommerce.Core.Exceptions;
using ECommerce.Core.Interfaces.Repositories;
using ECommerce.Core.Results;
using ECommerce.Core.Constants;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
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
    private readonly BusinessRulesOptions _businessRules;

    private static readonly Dictionary<string, string> CountryCodeMap = new(StringComparer.OrdinalIgnoreCase)
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

    public OrderService(
        IPromoCodeService promoCodeService,
        IInventoryService inventoryService,
        IEmailService emailService,
        IUnitOfWork unitOfWork,
        IMapper mapper,
        ILogger<OrderService> logger,
        IOptions<BusinessRulesOptions> businessRulesOptions)
    {
        _promoCodeService = promoCodeService;
        _inventoryService = inventoryService;
        _emailService = emailService;
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _logger = logger;
        _businessRules = businessRulesOptions.Value;
    }

    public async Task<Result<OrderDetailDto>> CreateOrderAsync(Guid? userId, CreateOrderDto dto, CancellationToken cancellationToken = default)
    {
        var isGuest = userId == null;
        _logger.LogInformation("Creating order. UserId: {UserId}, IsGuest: {IsGuest}", userId, isGuest);

        // Begin database transaction to ensure atomicity
        await using var transaction = await _unitOfWork.BeginTransactionAsync(cancellationToken);

        try
        {
            // Step 1: Validate user or guest
            if (userId.HasValue)
            {
                var user = await _unitOfWork.Users.GetByIdAsync(userId.Value, trackChanges: false, cancellationToken);
                if (user == null)
                {
                    await transaction.RollbackAsync(cancellationToken);
                    return Result<OrderDetailDto>.Fail(ErrorCodes.UserNotFound, "User not found");
                }
            }
            else
            {
                if (string.IsNullOrWhiteSpace(dto.GuestEmail))
                {
                    _logger.LogWarning("Guest checkout attempted without email");
                    await transaction.RollbackAsync(cancellationToken);
                    return Result<OrderDetailDto>.Fail(ErrorCodes.OrderNotFound, "Email is required for guest checkout");
                }
            }

            // Step 2: Create order entity with addresses
            var order = new Order
            {
                OrderNumber = GenerateOrderNumber(),
                UserId = userId,
                Status = OrderStatus.Pending,
                PaymentStatus = PaymentStatus.Pending,
                PaymentMethod = dto.PaymentMethod,
                Currency = "USD"
            };

            // Map shipping address
            if (dto.ShippingAddress != null)
            {
                var shippingAddress = _mapper.Map<Address>(dto.ShippingAddress);
                if (userId.HasValue)
                    shippingAddress.UserId = userId.Value;
                shippingAddress.Type = "Shipping";
                shippingAddress.IsDefault = false;
                shippingAddress.Country = NormalizeCountryCode(dto.ShippingAddress.Country);
                order.ShippingAddress = shippingAddress;
            }

            // Map billing address (or copy from shipping if not provided)
            if (dto.BillingAddress != null)
            {
                var billingAddress = _mapper.Map<Address>(dto.BillingAddress);
                if (userId.HasValue)
                    billingAddress.UserId = userId.Value;
                billingAddress.Type = "Billing";
                billingAddress.IsDefault = false;
                billingAddress.Country = NormalizeCountryCode(dto.BillingAddress.Country);
                order.BillingAddress = billingAddress;
            }
            else if (dto.ShippingAddress != null)
            {
                // Copy shipping address to billing if not provided
                var billingAddress = _mapper.Map<Address>(dto.ShippingAddress);
                if (userId.HasValue)
                    billingAddress.UserId = userId.Value;
                billingAddress.Type = "Billing";
                billingAddress.IsDefault = false;
                billingAddress.Country = NormalizeCountryCode(dto.ShippingAddress.Country);
                order.BillingAddress = billingAddress;
            }

            // Step 3: Process order items and calculate subtotal
            var (items, subtotal, stockCheckItems) = await ProcessOrderItemsAsync(dto.Items, cancellationToken);

            // Step 4: Validate products exist and are active
            foreach (var item in stockCheckItems)
            {
                // ProductId is already a Guid from ProcessOrderItemsAsync
                var product = await _unitOfWork.Products.GetByIdAsync(item.ProductId, false, cancellationToken);
                if (product == null)
                {
                    await transaction.RollbackAsync(cancellationToken);
                    return Result<OrderDetailDto>.Fail(ErrorCodes.ProductNotFound, $"Product not found");
                }

                if (!product.IsActive)
                {
                    await transaction.RollbackAsync(cancellationToken);
                    return Result<OrderDetailDto>.Fail(ErrorCodes.ProductNotAvailable, $"Product '{product.Name}' is not available");
                }
            }

            var stockCheckResponse = await _inventoryService.CheckStockAvailabilityAsync(stockCheckItems);
            if (!stockCheckResponse.IsAvailable)
            {
                await transaction.RollbackAsync(cancellationToken);
                return Result<OrderDetailDto>.Fail(ErrorCodes.InsufficientStock, "Insufficient stock for one or more items");
            }

            // Step 5: Apply promo code if provided
            if (!string.IsNullOrWhiteSpace(dto.PromoCode))
            {
                var promoValidationResult = await _promoCodeService.ValidatePromoCodeAsync(dto.PromoCode, subtotal);
                if (!promoValidationResult.IsValid)
                {
                    await transaction.RollbackAsync(cancellationToken);
                    return Result<OrderDetailDto>.Fail(ErrorCodes.InvalidPromoCode, $"Promo code '{dto.PromoCode}' is invalid");
                }

                order.DiscountAmount = promoValidationResult.DiscountAmount;
                order.PromoCodeId = promoValidationResult.PromoCode?.Id;
            }

            // Step 6: Calculate final totals with business rules
            CalculateOrderTotals(order, subtotal);
            order.Items = items;

            // Step 7: Save order to database
            await _unitOfWork.Orders.AddAsync(order, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            // Step 8: Reduce stock for products (within transaction - will rollback if fails)
            await ReduceProductStockAsync(items, order, userId, cancellationToken);

            // Step 9: Increment promo code usage (within transaction for atomicity)
            await IncrementPromoCodeUsageAsync(order.PromoCodeId, order.OrderNumber, cancellationToken);

            // Step 10: Commit transaction - all operations succeeded
            await transaction.CommitAsync(cancellationToken);

            _logger.LogInformation("Order created successfully: {OrderNumber}", order.OrderNumber);

            // Post-transaction operations (best effort - errors logged but don't fail order)
            await SendOrderConfirmationAsync(dto.GuestEmail, userId, order, cancellationToken);

            return Result<OrderDetailDto>.Ok(_mapper.Map<OrderDetailDto>(order));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating order. Transaction will be rolled back.");
            await transaction.RollbackAsync(cancellationToken);
            return Result<OrderDetailDto>.Fail("ORDER_CREATION_FAILED", ex.Message);
        }
    }

    #region Order Creation Helper Methods

    /// <summary>
    /// Validates that the user exists or guest email is provided.
    /// </summary>
    private async Task ValidateUserOrGuestAsync(Guid? userId, string? guestEmail, CancellationToken cancellationToken)
    {
        if (userId.HasValue)
        {
            var user = await _unitOfWork.Users.GetByIdAsync(userId.Value, trackChanges: false, cancellationToken);
            if (user == null)
                throw new UserNotFoundException(userId.Value);
        }
        else
        {
            if (string.IsNullOrWhiteSpace(guestEmail))
            {
                _logger.LogWarning("Guest checkout attempted without email");
                throw new GuestEmailRequiredException();
            }
        }
    }

    /// <summary>
    /// Creates the order entity with addresses mapped from DTO.
    /// </summary>
    private async Task<Order> CreateOrderEntityAsync(Guid? userId, CreateOrderDto dto, CancellationToken cancellationToken)
    {
        var order = new Order
        {
            OrderNumber = GenerateOrderNumber(),
            UserId = userId,
            Status = OrderStatus.Pending,
            PaymentStatus = PaymentStatus.Pending,
            PaymentMethod = dto.PaymentMethod,
            Currency = "USD"
        };

        // Map shipping address
        if (dto.ShippingAddress != null)
        {
            var shippingAddress = _mapper.Map<Address>(dto.ShippingAddress);
            if (userId.HasValue)
                shippingAddress.UserId = userId.Value;
            shippingAddress.Type = "Shipping";
            shippingAddress.IsDefault = false;
            shippingAddress.Country = NormalizeCountryCode(dto.ShippingAddress.Country);
            order.ShippingAddress = shippingAddress;
        }

        // Map billing address (or copy from shipping if not provided)
        if (dto.BillingAddress != null)
        {
            var billingAddress = _mapper.Map<Address>(dto.BillingAddress);
            if (userId.HasValue)
                billingAddress.UserId = userId.Value;
            billingAddress.Type = "Billing";
            billingAddress.IsDefault = false;
            billingAddress.Country = NormalizeCountryCode(dto.BillingAddress.Country);
            order.BillingAddress = billingAddress;
        }
        else if (dto.ShippingAddress != null)
        {
            var billingAddress = _mapper.Map<Address>(dto.ShippingAddress);
            if (userId.HasValue)
                billingAddress.UserId = userId.Value;
            billingAddress.Type = "Billing";
            billingAddress.IsDefault = false;
            billingAddress.Country = NormalizeCountryCode(dto.ShippingAddress.Country);
            order.BillingAddress = billingAddress;
        }

        return await Task.FromResult(order);
    }

    /// <summary>
    /// Processes order items, calculates subtotal, and creates stock check list.
    /// SECURITY: Uses server-side product lookup to prevent price manipulation attacks.
    /// PERFORMANCE: Batch-loads all products to prevent N+1 queries.
    /// </summary>
    private async Task<(List<OrderItem> items, decimal subtotal, List<ECommerce.Application.DTOs.Inventory.StockCheckItemDto> stockCheckItems)> 
        ProcessOrderItemsAsync(IEnumerable<CreateOrderItemDto>? itemDtos, CancellationToken cancellationToken)
    {
        var items = new List<OrderItem>();
        var subtotal = 0m;
        var stockCheckItems = new List<ECommerce.Application.DTOs.Inventory.StockCheckItemDto>();

        if (itemDtos != null && itemDtos.Any())
        {
            // PERFORMANCE FIX: Batch load all products at once instead of looping with individual queries
            var productIds = itemDtos
                .Select(i => 
                {
                    if (Guid.TryParse(i.ProductId, out var id))
                        return id;
                    throw new ProductNotFoundException(i.ProductId);
                })
                .ToList();

            var products = await _unitOfWork.Products.GetByIdsAsync(productIds, trackChanges: false, cancellationToken);
            var productDict = products.ToDictionary(p => p.Id);

            foreach (var itemDto in itemDtos)
            {
                if (!Guid.TryParse(itemDto.ProductId, out var productId))
                    throw new ProductNotFoundException(itemDto.ProductId);

                // Look up from pre-loaded products
                if (!productDict.TryGetValue(productId, out var product))
                    throw new ProductNotFoundException(productId);

                // Validate product is available for purchase
                if (!product.IsActive)
                    throw new ProductNotAvailableException(product.Name);

                // 🔒 Use database price, not client-provided price
                var orderItem = new OrderItem
                {
                    ProductId = productId,
                    ProductName = product.Name,
                    ProductSku = product.Sku,
                    ProductImageUrl = product.Images.FirstOrDefault(i => i.IsPrimary)?.Url 
                                      ?? product.Images.FirstOrDefault()?.Url,
                    Quantity = itemDto.Quantity,
                    UnitPrice = product.Price,  // ✓ Server-side price
                    TotalPrice = product.Price * itemDto.Quantity
                };

                items.Add(orderItem);

                var itemTotal = orderItem.UnitPrice * orderItem.Quantity;
                subtotal += itemTotal;

                stockCheckItems.Add(new ECommerce.Application.DTOs.Inventory.StockCheckItemDto
                {
                    ProductId = productId,
                    Quantity = itemDto.Quantity
                });
            }
        }

        return (items, subtotal, stockCheckItems);
    }

    /// <summary>
    /// Validates that sufficient stock is available for all items.
    /// Note: Within transaction context, this provides race condition protection.
    /// Future enhancement: Add pessimistic locking with SELECT FOR UPDATE (requires raw SQL).
    /// </summary>
    private async Task ValidateStockAvailabilityAsync(
        List<ECommerce.Application.DTOs.Inventory.StockCheckItemDto> stockCheckItems, 
        CancellationToken cancellationToken)
    {
        if (stockCheckItems.Any())
        {
            var stockCheck = await _inventoryService.CheckStockAvailabilityAsync(stockCheckItems, cancellationToken);
            if (!stockCheck.IsAvailable)
            {
                var firstIssue = stockCheck.Issues.First();
                throw new InsufficientStockException(firstIssue.ProductName, firstIssue.RequestedQuantity, firstIssue.AvailableQuantity);
            }
        }
    }

    /// <summary>
    /// Applies promo code discount if provided and valid.
    /// </summary>
    private async Task ApplyPromoCodeAsync(Order order, string? promoCode, decimal subtotal, CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(promoCode))
        {
            var promoValidation = await _promoCodeService.ValidatePromoCodeAsync(promoCode, subtotal, cancellationToken);
            if (promoValidation.IsValid && promoValidation.PromoCode != null)
            {
                order.DiscountAmount = promoValidation.DiscountAmount;
                order.PromoCodeId = promoValidation.PromoCode.Id;
                _logger.LogInformation("Promo code {Code} applied to order with discount ${Amount}",
                    promoCode, order.DiscountAmount);
            }
            else
            {
                throw new InvalidPromoCodeException(promoCode);
            }
        }
        else
        {
            order.DiscountAmount = 0;
        }
    }

    /// <summary>
    /// Calculates final order totals using business rules configuration.
    /// </summary>
    private void CalculateOrderTotals(Order order, decimal subtotal)
    {
        order.Subtotal = subtotal;
        order.ShippingAmount = subtotal > _businessRules.FreeShippingThreshold 
            ? 0 
            : _businessRules.StandardShippingCost;
        order.TaxAmount = subtotal * _businessRules.TaxRate;
        order.TotalAmount = order.Subtotal + order.ShippingAmount + order.TaxAmount - order.DiscountAmount;
    }

    /// <summary>
    /// Reduces stock for all products in the order using batch operation.
    /// PERFORMANCE FIX: Single transaction for all items instead of N individual transactions.
    /// Throws exception on failure to trigger transaction rollback.
    /// </summary>
    private async Task ReduceProductStockAsync(List<OrderItem> items, Order order, Guid? userId, CancellationToken cancellationToken)
    {
        var itemsToReduce = items
            .Where(i => i.ProductId.HasValue)
            .Select(i => (i.ProductId!.Value, i.Quantity, "sale", (Guid?)order.Id, userId))
            .ToList();

        if (itemsToReduce.Any())
        {
            await _inventoryService.ReduceStockBatchAsync(itemsToReduce, cancellationToken);
            _logger.LogInformation("Batch stock reduction completed for {ItemCount} products in order {OrderNumber}",
                itemsToReduce.Count, order.OrderNumber);
        }
    }

    /// <summary>
    /// Increments promo code usage count (best effort - errors logged but don't fail order).
    /// </summary>
    private async Task IncrementPromoCodeUsageAsync(Guid? promoCodeId, string orderNumber, CancellationToken cancellationToken)
    {
        if (promoCodeId.HasValue)
        {
            try
            {
                await _promoCodeService.IncrementUsedCountAsync(promoCodeId.Value, cancellationToken);
            }
            catch (Exception promoEx)
            {
                _logger.LogError(promoEx, "Failed to increment usage count for promo code {PromoCodeId} in order {OrderNumber}",
                    promoCodeId.Value, orderNumber);
                // Continue - order was created successfully
            }
        }
    }

    /// <summary>
    /// Sends order confirmation email (best effort - errors logged but don't fail order).
    /// </summary>
    private async Task SendOrderConfirmationAsync(string? guestEmail, Guid? userId, Order order, CancellationToken cancellationToken)
    {
        try
        {
            var emailAddress = !string.IsNullOrWhiteSpace(guestEmail) 
                ? guestEmail 
                : userId.HasValue ? await GetUserEmailAsync(userId.Value, cancellationToken) : null;

            if (!string.IsNullOrWhiteSpace(emailAddress))
            {
                await _emailService.SendOrderConfirmationEmailAsync(emailAddress, order);
                _logger.LogInformation("Order confirmation email sent to {Email}", emailAddress);
            }
        }
        catch (Exception emailEx)
        {
            _logger.LogError(emailEx, "Failed to send order confirmation email for order {OrderNumber}", order.OrderNumber);
            // Don't throw - order was created successfully
        }
    }

    #endregion

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

    public async Task<PaginatedResult<OrderDto>> GetUserOrdersAsync(Guid userId, OrderQueryParameters parameters, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Retrieving orders for user {UserId}, page {Page}", userId, parameters.Page);

        var totalCount = await _unitOfWork.Orders.GetUserOrdersCountAsync(userId, cancellationToken: cancellationToken);
        var orders = await _unitOfWork.Orders.GetUserOrdersAsync(userId, parameters.GetSkip(), parameters.PageSize, cancellationToken: cancellationToken);
        var dtos = orders.Select(o => _mapper.Map<OrderDto>(o)).ToList();

        return new PaginatedResult<OrderDto>
        {
            Items = dtos,
            TotalCount = totalCount,
            Page = parameters.Page,
            PageSize = parameters.PageSize
        };
    }

    public async Task<Result<OrderDetailDto>> UpdateOrderStatusAsync(Guid id, string status, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Updating order {OrderId} status to {Status}", id, status);

        var order = await _unitOfWork.Orders.GetByIdAsync(id, trackChanges: true, cancellationToken: cancellationToken);
        if (order == null)
        {
            return Result<OrderDetailDto>.Fail(ErrorCodes.OrderNotFound, $"Order {id} not found");
        }

        // Validate status
        if (!Enum.TryParse<OrderStatus>(status, ignoreCase: true, out var orderStatus))
        {
            return Result<OrderDetailDto>.Fail(ErrorCodes.InvalidOrderStatus, $"Invalid order status: {status}");
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

        return Result<OrderDetailDto>.Ok(_mapper.Map<OrderDetailDto>(order));
    }

    public async Task<Result<Unit>> CancelOrderAsync(Guid id, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Cancelling order {OrderId}", id);

        var order = await _unitOfWork.Orders.GetByIdAsync(id, trackChanges: true, cancellationToken: cancellationToken);
        if (order == null)
        {
            return Result<Unit>.Fail(ErrorCodes.OrderNotFound, $"Order {id} not found");
        }

        // Can't cancel if already shipped or delivered
        if (order.Status == OrderStatus.Shipped || order.Status == OrderStatus.Delivered)
        {
            return Result<Unit>.Fail(ErrorCodes.InvalidOrderStatus, $"Cannot cancel order with status {order.Status}");
        }

        order.Status = OrderStatus.Cancelled;
        order.CancelledAt = DateTime.UtcNow;
        order.UpdatedAt = DateTime.UtcNow;

        await _unitOfWork.Orders.UpdateAsync(order, cancellationToken: cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken: cancellationToken);

        _logger.LogInformation("Order {OrderId} cancelled successfully", id);

        return Result<Unit>.Ok(new Unit());
    }

    public async Task<PaginatedResult<OrderDto>> GetAllOrdersAsync(OrderQueryParameters parameters, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Retrieving all orders, page {Page}", parameters.Page);

        var totalCount = await _unitOfWork.Orders.GetTotalOrdersCountAsync(cancellationToken: cancellationToken);
        var orders = await _unitOfWork.Orders.GetAllOrdersPaginatedAsync(parameters.GetSkip(), parameters.PageSize, cancellationToken: cancellationToken);

        var dtos = orders.Select(o => _mapper.Map<OrderDto>(o)).ToList();

        return new PaginatedResult<OrderDto>
        {
            Items = dtos,
            TotalCount = totalCount,
            Page = parameters.Page,
            PageSize = parameters.PageSize
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

        var countryUpper = country.ToUpperInvariant();

        // If already 2 characters, assume it's a code
        if (countryUpper.Length == 2)
            return countryUpper;

        if (CountryCodeMap.TryGetValue(country, out var code))
            return code;

        // Default: take first 2 characters
        return countryUpper.Substring(0, Math.Min(2, countryUpper.Length));
    }

    private async Task<string?> GetUserEmailAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var user = await _unitOfWork.Users.GetByIdAsync(userId, trackChanges: false, cancellationToken: cancellationToken);
        return user?.Email;
    }
}

