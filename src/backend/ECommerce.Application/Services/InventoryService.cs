using ECommerce.Application.Interfaces;
using ECommerce.Application.DTOs.Inventory;
using AutoMapper;
using ECommerce.Core.Entities;
using ECommerce.Core.Enums;
using ECommerce.Core.Exceptions;
using ECommerce.Core.Interfaces.Repositories;
using Microsoft.Extensions.Logging;

namespace ECommerce.Application.Services;

public class InventoryService : IInventoryService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IEmailService _emailService;
    private readonly ILogger<InventoryService> _logger;
    private readonly IMapper _mapper;
    private readonly HashSet<Guid> _lowStockAlertsSent = new();

    public InventoryService(
        IUnitOfWork unitOfWork,
        IEmailService emailService,
        ILogger<InventoryService> logger,
        IMapper mapper)
    {
        _unitOfWork = unitOfWork;
        _emailService = emailService;
        _logger = logger;
        _mapper = mapper;
    }

    public async Task ReduceStockAsync(Guid productId, int quantity, string reason, Guid? referenceId = null, Guid? userId = null)
    {
        if (quantity <= 0)
            throw new InvalidQuantityException("Quantity must be positive");

        await using var transaction = await _unitOfWork.BeginTransactionAsync();

        var product = await _unitOfWork.Products.GetByIdAsync(productId);
        if (product == null)
            throw new ProductNotFoundException(productId);

        if (product.StockQuantity < quantity)
            throw new InsufficientStockException(product.Name, quantity, product.StockQuantity);

        var previousStock = product.StockQuantity;
        product.StockQuantity -= quantity;

        await _unitOfWork.Products.UpdateAsync(product);

        var log = new InventoryLog
        {
            ProductId = productId,
            QuantityChange = -quantity,
            Reason = reason,
            ReferenceId = referenceId,
            Notes = $"Stock reduced from {previousStock} to {product.StockQuantity}",
            CreatedByUserId = userId
        };

        await _unitOfWork.InventoryLogs.AddAsync(log);
        await _unitOfWork.SaveChangesAsync();
        await transaction.CommitAsync();

        _logger.LogInformation("Stock reduced for product {ProductId}: {Quantity} units. New stock: {NewStock}",
            productId, quantity, product.StockQuantity);

        await CheckAndSendLowStockAlertsAsync(productId);
    }

    public async Task IncreaseStockAsync(Guid productId, int quantity, string reason, Guid? referenceId = null, Guid? userId = null)
    {
        if (quantity <= 0)
            throw new InvalidQuantityException("Quantity must be positive");

        await using var transaction = await _unitOfWork.BeginTransactionAsync();

        var product = await _unitOfWork.Products.GetByIdAsync(productId);
        if (product == null)
            throw new ProductNotFoundException(productId);

        var previousStock = product.StockQuantity;
        product.StockQuantity += quantity;

        await _unitOfWork.Products.UpdateAsync(product);

        var log = new InventoryLog
        {
            ProductId = productId,
            QuantityChange = quantity,
            Reason = reason,
            ReferenceId = referenceId,
            Notes = $"Stock increased from {previousStock} to {product.StockQuantity}",
            CreatedByUserId = userId
        };

        await _unitOfWork.InventoryLogs.AddAsync(log);
        await _unitOfWork.SaveChangesAsync();
        await transaction.CommitAsync();

        _logger.LogInformation("Stock increased for product {ProductId}: {Quantity} units. New stock: {NewStock}",
            productId, quantity, product.StockQuantity);

        if (product.StockQuantity > product.LowStockThreshold)
        {
            _lowStockAlertsSent.Remove(productId);
        }
    }

    public async Task AdjustStockAsync(Guid productId, int newQuantity, string reason, string? notes = null, Guid? userId = null)
    {
        if (newQuantity < 0)
            throw new InvalidQuantityException("Quantity cannot be negative");

        await using var transaction = await _unitOfWork.BeginTransactionAsync();

        var product = await _unitOfWork.Products.GetByIdAsync(productId);
        if (product == null)
            throw new ProductNotFoundException(productId);

        var previousStock = product.StockQuantity;
        var quantityChange = newQuantity - previousStock;

        product.StockQuantity = newQuantity;
        await _unitOfWork.Products.UpdateAsync(product);

        var log = new InventoryLog
        {
            ProductId = productId,
            QuantityChange = quantityChange,
            Reason = reason,
            Notes = notes ?? $"Stock adjusted from {previousStock} to {newQuantity}",
            CreatedByUserId = userId
        };

        await _unitOfWork.InventoryLogs.AddAsync(log);
        await _unitOfWork.SaveChangesAsync();
        await transaction.CommitAsync();

        _logger.LogInformation("Stock adjusted for product {ProductId}: {PreviousStock} -> {NewStock}",
            productId, previousStock, newQuantity);

        await CheckAndSendLowStockAlertsAsync(productId);
    }

    public async Task<StockCheckResponse> CheckStockAvailabilityAsync(List<StockCheckItem> items)
    {
        var response = new StockCheckResponse { IsAvailable = true };

        foreach (var item in items)
        {
            var product = await _unitOfWork.Products.GetByIdAsync(item.ProductId);
            if (product == null)
            {
                response.IsAvailable = false;
                response.Issues.Add(new StockIssue
                {
                    ProductId = item.ProductId,
                    ProductName = "Unknown Product",
                    RequestedQuantity = item.Quantity,
                    AvailableQuantity = 0,
                    Message = "Product not found"
                });
                continue;
            }

            if (product.StockQuantity < item.Quantity)
            {
                response.IsAvailable = false;
                response.Issues.Add(new StockIssue
                {
                    ProductId = item.ProductId,
                    ProductName = product.Name,
                    RequestedQuantity = item.Quantity,
                    AvailableQuantity = product.StockQuantity,
                    Message = product.StockQuantity == 0
                        ? "Out of stock"
                        : $"Only {product.StockQuantity} available"
                });
            }
        }

        return response;
    }

    public async Task<bool> IsStockAvailableAsync(Guid productId, int quantity)
    {
        var product = await _unitOfWork.Products.GetByIdAsync(productId);
        return product != null && product.StockQuantity >= quantity;
    }

    public async Task<List<InventoryDto>> GetAllInventoryAsync(int page = 1, int pageSize = 50, string? search = null, bool? lowStockOnly = null)
    {
        var allProducts = await _unitOfWork.Products.GetAllAsync();
        var query = allProducts.AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var searchLower = search.ToLower();
            query = query.Where(p => p.Name.ToLower().Contains(searchLower) ||
                                   (p.Sku != null && p.Sku.ToLower().Contains(searchLower)));
        }

        if (lowStockOnly == true)
        {
            query = query.Where(p => p.StockQuantity <= p.LowStockThreshold);
        }

        query = query.OrderBy(p => p.StockQuantity).ThenBy(p => p.Name);

        var products = query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        return products.Select(p => _mapper.Map<InventoryDto>(p)).ToList();
    }

    public async Task<List<LowStockAlert>> GetLowStockProductsAsync()
    {
        var products = await _unitOfWork.Products.GetAllAsync();

        var lowStockProducts = products
            .Where(p => p.StockQuantity <= p.LowStockThreshold && p.IsActive)
            .OrderBy(p => p.StockQuantity)
            .Select(p => _mapper.Map<LowStockAlert>(p))
            .ToList();

        return lowStockProducts;
    }

    public async Task<List<InventoryLogDto>> GetInventoryHistoryAsync(Guid productId, int page = 1, int pageSize = 50)
    {
        var logsQuery = (await _unitOfWork.InventoryLogs.GetAllAsync())
            .Where(log => log.ProductId == productId)
            .OrderByDescending(log => log.CreatedAt);

        var logs = logsQuery
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        var product = await _unitOfWork.Products.GetByIdAsync(productId);
        var productName = product?.Name ?? "Unknown Product";

        var userIds = logs.Where(l => l.CreatedByUserId.HasValue).Select(l => l.CreatedByUserId!.Value).Distinct();
        var users = new Dictionary<Guid, string>();

        foreach (var userId in userIds)
        {
            var user = await _unitOfWork.Users.GetByIdAsync(userId);
            if (user != null)
            {
                users[userId] = $"{user.FirstName} {user.LastName}";
            }
        }

        var currentStock = product?.StockQuantity ?? 0;
        var result = new List<InventoryLogDto>();

        foreach (var log in logs)
        {
            var dto = _mapper.Map<InventoryLogDto>(log);
            dto.ProductName = productName;
            dto.StockAfterChange = currentStock;
            dto.CreatedByUserName = log.CreatedByUserId.HasValue && users.ContainsKey(log.CreatedByUserId.Value)
                ? users[log.CreatedByUserId.Value]
                : null;

            result.Add(dto);

            currentStock -= log.QuantityChange;
        }

        return result;
    }

    public async Task CheckAndSendLowStockAlertsAsync(Guid productId)
    {
        try
        {
            var product = await _unitOfWork.Products.GetByIdAsync(productId);
            if (product == null) return;

            if (product.StockQuantity <= product.LowStockThreshold && !_lowStockAlertsSent.Contains(productId))
            {
                var admins = (await _unitOfWork.Users.GetAllAsync())
                    .Where(u => u.Role == UserRole.Admin || u.Role == UserRole.SuperAdmin)
                    .ToList();

                foreach (var admin in admins)
                {
                    await _emailService.SendLowStockAlertAsync(
                        admin.Email,
                        admin.FirstName,
                        product.Name,
                        product.StockQuantity,
                        product.LowStockThreshold,
                        product.Sku
                    );
                }

                _lowStockAlertsSent.Add(productId);

                _logger.LogInformation("Low stock alert sent for product {ProductId}: {ProductName} (Stock: {Stock})",
                    productId, product.Name, product.StockQuantity);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending low stock alert for product {ProductId}", productId);
            // Don't throw - this is a background task
        }
    }
}
