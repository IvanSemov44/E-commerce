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
using ECommerce.Core.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.EntityFrameworkCore;
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
            var validationResult = await ValidateUserOrGuestAsync(userId, dto.GuestEmail, cancellationToken);
            if (!validationResult.IsSuccess)
            {
                await transaction.RollbackAsync(cancellationToken);
                var failure = validationResult.GetFailureOrNull();
                return Result<OrderDetailDto>.Fail(failure!.Value.Code, failure.Value.Message);
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
            var itemsResult = await ProcessOrderItemsAsync(dto.Items, cancellationToken);
            if (!itemsResult.IsSuccess)
            {
                await transaction.RollbackAsync(cancellationToken);
                var failure = itemsResult.GetFailureOrNull();
                return Result<OrderDetailDto>.Fail(failure!.Value.Code, failure.Value.Message);
            }

            var (items, subtotal, stockCheckItems) = itemsResult.GetDataOrThrow();

            // Step 4: Validate stock availability
            var stockResult = await ValidateStockAvailabilityAsync(stockCheckItems, cancellationToken);
            if (!stockResult.IsSuccess)
            {
                await transaction.RollbackAsync(cancellationToken);
                var failure = stockResult.GetFailureOrNull();
                return Result<OrderDetailDto>.Fail(failure!.Value.Code, failure.Value.Message);
            }

            // Step 5: Apply promo code if provided
            var promoResult = await ApplyPromoCodeAsync(order, dto.PromoCode, subtotal, cancellationToken);
            if (!promoResult.IsSuccess)
            {
                await transaction.RollbackAsync(cancellationToken);
                var failure = promoResult.GetFailureOrNull();
                return Result<OrderDetailDto>.Fail(failure!.Value.Code, failure.Value.Message);
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
            return Result<OrderDetailDto>.Fail(ErrorCodes.OrderCreationFailed, "Failed to create order");
        }
    }

    #region Order Creation Helper Methods

    /// <summary>
    /// Validates that the user exists or guest email is provided.
    /// </summary>
    private async Task<Result<Core.Results.Unit>> ValidateUserOrGuestAsync(Guid? userId, string? guestEmail, CancellationToken cancellationToken)
    {
        if (userId.HasValue)
        {
            var user = await _unitOfWork.Users.GetByIdAsync(userId.Value, trackChanges: false, cancellationToken);
            if (user == null)
                return Result<Core.Results.Unit>.Fail(ErrorCodes.UserNotFound, $"User with id '{userId.Value}' not found");
        }
        else
        {
            if (string.IsNullOrWhiteSpace(guestEmail))
            {
                _logger.LogWarning("Guest checkout attempted without email");
                return Result<Core.Results.Unit>.Fail(ErrorCodes.OrderNotFound, "Email is required for guest checkout");
            }
        }

        return Result<Core.Results.Unit>.Ok(new Core.Results.Unit());
    }

    /// <summary>
    /// Creates the order entity with addresses mapped from DTO.
    /// </summary>
    private Task<Order> CreateOrderEntityAsync(Guid? userId, CreateOrderDto dto, CancellationToken _)
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

        return Task.FromResult(order);
    }

    /// <summary>
    /// Processes order items, calculates subtotal, and creates stock check list.
    /// SECURITY: Uses server-side product lookup to prevent price manipulation attacks.
    /// PERFORMANCE: Batch-loads all products to prevent N+1 queries.
    /// </summary>
    private async Task<Result<(List<OrderItem> items, decimal subtotal, List<ECommerce.Application.DTOs.Inventory.StockCheckItemDto> stockCheckItems)>>
        ProcessOrderItemsAsync(IEnumerable<CreateOrderItemDto>? itemDtos, CancellationToken cancellationToken)
    {
        var items = new List<OrderItem>();
        var subtotal = 0m;
        var stockCheckItems = new List<ECommerce.Application.DTOs.Inventory.StockCheckItemDto>();

        if (itemDtos != null && itemDtos.Any())
        {
            // Batch-load all products at once to avoid N+1 queries
            var productIds = new List<Guid>();
            foreach (var i in itemDtos)
            {
                if (!Guid.TryParse(i.ProductId, out var id))
                    return Result<(List<OrderItem>, decimal, List<ECommerce.Application.DTOs.Inventory.StockCheckItemDto>)>.Fail(
                        ErrorCodes.ProductNotFound, $"Invalid product ID: {i.ProductId}");
                productIds.Add(id);
            }

            var products = await _unitOfWork.Products.GetByIdsAsync(productIds, trackChanges: false, cancellationToken);
            var productDict = products.ToDictionary(p => p.Id);

            foreach (var itemDto in itemDtos)
            {
                if (!Guid.TryParse(itemDto.ProductId, out var productId))
                    return Result<(List<OrderItem>, decimal, List<ECommerce.Application.DTOs.Inventory.StockCheckItemDto>)>.Fail(
                        ErrorCodes.ProductNotFound, $"Invalid product ID: {itemDto.ProductId}");

                // Look up from pre-loaded products
                if (!productDict.TryGetValue(productId, out var product))
                    return Result<(List<OrderItem>, decimal, List<ECommerce.Application.DTOs.Inventory.StockCheckItemDto>)>.Fail(
                        ErrorCodes.ProductNotFound, $"Product with id '{productId}' not found");

                // Validate product is available for purchase
                if (!product.IsActive)
                    return Result<(List<OrderItem>, decimal, List<ECommerce.Application.DTOs.Inventory.StockCheckItemDto>)>.Fail(
                        ErrorCodes.ProductNotAvailable, $"Product '{product.Name}' is not available for purchase");

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

        return Result<(List<OrderItem>, decimal, List<ECommerce.Application.DTOs.Inventory.StockCheckItemDto>)>.Ok((items, subtotal, stockCheckItems));
    }

    /// <summary>
    /// Validates that sufficient stock is available for all items.
    /// Note: Within transaction context, this provides race condition protection.
    /// Future enhancement: Add pessimistic locking with SELECT FOR UPDATE (requires raw SQL).
    /// </summary>
    private async Task<Result<Core.Results.Unit>> ValidateStockAvailabilityAsync(
        List<ECommerce.Application.DTOs.Inventory.StockCheckItemDto> stockCheckItems,
        CancellationToken cancellationToken)
    {
        if (stockCheckItems.Any())
        {
            var stockCheck = await _inventoryService.CheckStockAvailabilityAsync(stockCheckItems, cancellationToken);
            if (!stockCheck.IsAvailable)
            {
                var firstIssue = stockCheck.Issues.First();
                return Result<Core.Results.Unit>.Fail(ErrorCodes.InsufficientStock,
                    $"Insufficient stock for '{firstIssue.ProductName}': requested {firstIssue.RequestedQuantity}, available {firstIssue.AvailableQuantity}");
            }
        }

        return Result<Core.Results.Unit>.Ok(new Core.Results.Unit());
    }

    /// <summary>
    /// Applies promo code discount if provided and valid.
    /// </summary>
    private async Task<Result<Core.Results.Unit>> ApplyPromoCodeAsync(Order order, string? promoCode, decimal subtotal, CancellationToken cancellationToken)
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
                return Result<Core.Results.Unit>.Fail(ErrorCodes.InvalidPromoCode, $"Promo code '{promoCode}' is invalid or has expired");
            }
        }
        else
        {
            order.DiscountAmount = 0;
        }

        return Result<Core.Results.Unit>.Ok(new Core.Results.Unit());
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
    /// Processes all items in a single transaction for atomicity.
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
                await _emailService.SendOrderConfirmationEmailAsync(emailAddress, order, cancellationToken);
                _logger.LogInformation("Order confirmation email sent to {Email}", emailAddress.MaskEmail());
            }
        }
        catch (Exception emailEx)
        {
            _logger.LogError(emailEx, "Failed to send order confirmation email for order {OrderNumber}", order.OrderNumber);
            // Don't throw - order was created successfully
        }
    }

    #endregion

    public async Task<Result<OrderDetailDto>> GetOrderByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Retrieving order {OrderId}", id);

        var order = await _unitOfWork.Orders.GetWithItemsAsync(id, cancellationToken: cancellationToken);
        if (order == null)
        {
            return Result<OrderDetailDto>.Fail(ErrorCodes.OrderNotFound, "Order not found");
        }

        return Result<OrderDetailDto>.Ok(_mapper.Map<OrderDetailDto>(order));
    }

    public async Task<Result<OrderDetailDto>> GetOrderByIdForUserAsync(Guid id, Guid? userId, bool isAdmin, CancellationToken cancellationToken = default)
    {
        var order = await GetOrderByIdAsync(id, cancellationToken);
        if (order is Result<OrderDetailDto>.Failure orderFailure)
        {
            return Result<OrderDetailDto>.Fail(orderFailure.Code, orderFailure.Message);
        }

        var orderData = ((Result<OrderDetailDto>.Success)order).Data;

        if (!isAdmin && orderData.UserId != userId)
        {
            return Result<OrderDetailDto>.Fail(ErrorCodes.Forbidden, "You do not have permission to access this order");
        }

        return Result<OrderDetailDto>.Ok(orderData);
    }

    public async Task<Result<OrderDetailDto>> GetOrderByNumberAsync(string orderNumber, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Retrieving order {OrderNumber}", orderNumber);

        var order = await _unitOfWork.Orders.GetByOrderNumberAsync(orderNumber, cancellationToken: cancellationToken);
        if (order == null)
        {
            return Result<OrderDetailDto>.Fail(ErrorCodes.OrderNotFound, "Order not found");
        }

        return Result<OrderDetailDto>.Ok(_mapper.Map<OrderDetailDto>(order));
    }

    public async Task<Result<OrderDetailDto>> GetOrderByNumberForUserAsync(string orderNumber, Guid? userId, bool isAdmin, CancellationToken cancellationToken = default)
    {
        var order = await GetOrderByNumberAsync(orderNumber, cancellationToken);
        if (order is Result<OrderDetailDto>.Failure orderFailure)
        {
            return Result<OrderDetailDto>.Fail(orderFailure.Code, orderFailure.Message);
        }

        var orderData = ((Result<OrderDetailDto>.Success)order).Data;

        if (!isAdmin && orderData.UserId != userId)
        {
            return Result<OrderDetailDto>.Fail(ErrorCodes.Forbidden, "You do not have permission to access this order");
        }

        return Result<OrderDetailDto>.Ok(orderData);
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

    public async Task<Result<OrderDetailDto>> UpdateOrderStatusAsync(Guid id, string status, string? trackingNumber = null, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Updating order {OrderId} status to {Status}", id, status);

        try
        {
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
                if (!string.IsNullOrWhiteSpace(trackingNumber))
                {
                    order.TrackingNumber = trackingNumber.Trim();
                }
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
        catch (DbUpdateConcurrencyException ex)
        {
            _logger.LogWarning(ex, "Concurrency conflict while updating order {OrderId} status", id);
            return Result<OrderDetailDto>.Fail(ErrorCodes.ConcurrencyConflict,
                $"Order {id} was modified by another user. Please refresh and try again.");
        }
    }

    public Task<Result<Unit>> CancelOrderAsync(Guid id, CancellationToken cancellationToken = default)
        => CancelOrderAsync(id, userId: null, isAdmin: true, cancellationToken);

    public async Task<Result<Unit>> CancelOrderAsync(Guid id, Guid? userId, bool isAdmin, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Cancelling order {OrderId}", id);

        try
        {
            var order = await _unitOfWork.Orders.GetByIdAsync(id, trackChanges: true, cancellationToken: cancellationToken);
            if (order == null)
            {
                return Result<Unit>.Fail(ErrorCodes.OrderNotFound, $"Order {id} not found");
            }

            if (!isAdmin && order.UserId != userId)
            {
                return Result<Unit>.Fail(ErrorCodes.Forbidden, "You do not have permission to cancel this order");
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
        catch (DbUpdateConcurrencyException ex)
        {
            _logger.LogWarning(ex, "Concurrency conflict while cancelling order {OrderId}", id);
            return Result<Unit>.Fail(ErrorCodes.ConcurrencyConflict,
                $"Order {id} was modified by another user. Please refresh and try again.");
        }
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
    private static string GenerateOrderNumber()
    {
        // Format: ORD-YYYYMMDD-XXXXXX (e.g., ORD-20250120-A1B2C3)
        var date = DateTime.UtcNow.ToString("yyyyMMdd", System.Globalization.CultureInfo.InvariantCulture);
        var random = Guid.NewGuid().ToString("N")[..6].ToUpperInvariant();
        return $"ORD-{date}-{random}";
    }

    /// <summary>
    /// Normalize country name to 2-letter ISO country code.
    /// </summary>
    private static string NormalizeCountryCode(string country)
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

