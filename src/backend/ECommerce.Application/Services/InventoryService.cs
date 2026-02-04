using ECommerce.Application.Interfaces;
using ECommerce.Application.DTOs.Common;
using ECommerce.Application.DTOs.Inventory;
using AutoMapper;
using ECommerce.Core.Entities;
using ECommerce.Core.Enums;
using ECommerce.Core.Exceptions;
using ECommerce.Core.Interfaces.Repositories;
using Microsoft.Extensions.Logging;
using System.Threading;

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

    public async Task ReduceStockAsync(Guid productId, int quantity, string reason, Guid? referenceId = null, Guid? userId = null, CancellationToken cancellationToken = default)
    {
        if (quantity <= 0)
            throw new InvalidQuantityException("Quantity must be positive");

        await using var transaction = await _unitOfWork.BeginTransactionAsync();

        var product = await _unitOfWork.Products.GetByIdAsync(productId, cancellationToken: cancellationToken);
        if (product == null)
            throw new ProductNotFoundException(productId);

        if (product.StockQuantity < quantity)
            throw new InsufficientStockException(product.Name, quantity, product.StockQuantity);

        var previousStock = product.StockQuantity;
        product.StockQuantity -= quantity;

        await _unitOfWork.Products.UpdateAsync(product, cancellationToken: cancellationToken);

        var log = new InventoryLog
        {
            ProductId = productId,
            QuantityChange = -quantity,
            Reason = reason,
            ReferenceId = referenceId,
            Notes = $"Stock reduced from {previousStock} to {product.StockQuantity}",
            CreatedByUserId = userId
        };

        await _unitOfWork.InventoryLogs.AddAsync(log, cancellationToken: cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken: cancellationToken);
        await transaction.CommitAsync();

        _logger.LogInformation("Stock reduced for product {ProductId}: {Quantity} units. New stock: {NewStock}",
            productId, quantity, product.StockQuantity);

        await CheckAndSendLowStockAlertsAsync(productId, cancellationToken);
    }

    public async Task IncreaseStockAsync(Guid productId, int quantity, string reason, Guid? referenceId = null, Guid? userId = null, CancellationToken cancellationToken = default)
    {
        if (quantity <= 0)
            throw new InvalidQuantityException("Quantity must be positive");

        await using var transaction = await _unitOfWork.BeginTransactionAsync();

        var product = await _unitOfWork.Products.GetByIdAsync(productId, cancellationToken: cancellationToken);
        if (product == null)
            throw new ProductNotFoundException(productId);

        var previousStock = product.StockQuantity;
        product.StockQuantity += quantity;

        await _unitOfWork.Products.UpdateAsync(product, cancellationToken: cancellationToken);

        var log = new InventoryLog
        {
            ProductId = productId,
            QuantityChange = quantity,
            Reason = reason,
            ReferenceId = referenceId,
            Notes = $"Stock increased from {previousStock} to {product.StockQuantity}",
            CreatedByUserId = userId
        };

        await _unitOfWork.InventoryLogs.AddAsync(log, cancellationToken: cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken: cancellationToken);
        await transaction.CommitAsync();

        _logger.LogInformation("Stock increased for product {ProductId}: {Quantity} units. New stock: {NewStock}",
            productId, quantity, product.StockQuantity);

        if (product.StockQuantity > product.LowStockThreshold)
        {
            _lowStockAlertsSent.Remove(productId);
        }
    }

    public async Task AdjustStockAsync(Guid productId, int newQuantity, string reason, string? notes = null, Guid? userId = null, CancellationToken cancellationToken = default)
    {
        if (newQuantity < 0)
            throw new InvalidQuantityException("Quantity cannot be negative");

        await using var transaction = await _unitOfWork.BeginTransactionAsync();

        var product = await _unitOfWork.Products.GetByIdAsync(productId, cancellationToken: cancellationToken);
        if (product == null)
            throw new ProductNotFoundException(productId);

        var previousStock = product.StockQuantity;
        var quantityChange = newQuantity - previousStock;

        product.StockQuantity = newQuantity;
        await _unitOfWork.Products.UpdateAsync(product, cancellationToken: cancellationToken);

        var log = new InventoryLog
        {
            ProductId = productId,
            QuantityChange = quantityChange,
            Reason = reason,
            Notes = notes ?? $"Stock adjusted from {previousStock} to {newQuantity}",
            CreatedByUserId = userId
        };

        await _unitOfWork.InventoryLogs.AddAsync(log, cancellationToken: cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken: cancellationToken);
        await transaction.CommitAsync();

        _logger.LogInformation("Stock adjusted for product {ProductId}: {PreviousStock} -> {NewStock}",
            productId, previousStock, newQuantity);

        await CheckAndSendLowStockAlertsAsync(productId, cancellationToken);
    }

    public async Task<StockCheckResponse> CheckStockAvailabilityAsync(List<StockCheckItemDto> items, CancellationToken cancellationToken = default)
    {
        var response = new StockCheckResponse { IsAvailable = true };

        foreach (var item in items)
        {
            var product = await _unitOfWork.Products.GetByIdAsync(item.ProductId, cancellationToken: cancellationToken);
            if (product == null)
            {
                response.IsAvailable = false;
                response.Issues.Add(new StockIssueDto
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
                response.Issues.Add(new StockIssueDto
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

    public async Task<bool> IsStockAvailableAsync(Guid productId, int quantity, CancellationToken cancellationToken = default)
    {
        var product = await _unitOfWork.Products.GetByIdAsync(productId, cancellationToken: cancellationToken);
        return product != null && product.StockQuantity >= quantity;
    }

    public async Task<PaginatedResult<InventoryDto>> GetAllInventoryAsync(InventoryQueryParameters parameters, CancellationToken cancellationToken = default)
    {
        var allProducts = await _unitOfWork.Products.GetAllAsync(cancellationToken: cancellationToken);
        var query = allProducts.AsQueryable();

        if (!string.IsNullOrWhiteSpace(parameters.Search))
        {
            var searchLower = parameters.Search.ToLower();
            query = query.Where(p => p.Name.ToLower().Contains(searchLower) ||
                                   (p.Sku != null && p.Sku.ToLower().Contains(searchLower)));
        }

        if (parameters.LowStockOnly == true)
        {
            query = query.Where(p => p.StockQuantity <= p.LowStockThreshold);
        }

        query = query.OrderBy(p => p.StockQuantity).ThenBy(p => p.Name);

        var totalCount = query.Count();

        var products = query
            .Skip(parameters.GetSkip())
            .Take(parameters.PageSize)
            .ToList();

        var dtos = products.Select(p => _mapper.Map<InventoryDto>(p)).ToList();

        return new PaginatedResult<InventoryDto>
        {
            Items = dtos,
            TotalCount = totalCount,
            Page = parameters.Page,
            PageSize = parameters.PageSize
        };
    }

    public async Task<List<LowStockAlertDto>> GetLowStockProductsAsync(CancellationToken cancellationToken = default)
    {
        var products = await _unitOfWork.Products.GetAllAsync(cancellationToken: cancellationToken);

        var lowStockProducts = products
            .Where(p => p.StockQuantity <= p.LowStockThreshold && p.IsActive)
            .OrderBy(p => p.StockQuantity)
            .Select(p => _mapper.Map<LowStockAlertDto>(p))
            .ToList();

        return lowStockProducts;
    }

    public async Task<List<InventoryLogDto>> GetInventoryHistoryAsync(Guid productId, int page = 1, int pageSize = 50, CancellationToken cancellationToken = default)
    {
        var logsQuery = (await _unitOfWork.InventoryLogs.GetAllAsync(cancellationToken: cancellationToken))
            .Where(log => log.ProductId == productId)
            .OrderByDescending(log => log.CreatedAt);

        var logs = logsQuery
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        var product = await _unitOfWork.Products.GetByIdAsync(productId, cancellationToken: cancellationToken);
        var productName = product?.Name ?? "Unknown Product";

        var userIds = logs.Where(l => l.CreatedByUserId.HasValue).Select(l => l.CreatedByUserId!.Value).Distinct();
        var users = new Dictionary<Guid, string>();

        foreach (var userId in userIds)
        {
            var user = await _unitOfWork.Users.GetByIdAsync(userId, cancellationToken: cancellationToken);
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

    public async Task CheckAndSendLowStockAlertsAsync(Guid productId, CancellationToken cancellationToken = default)
    {
        try
        {
            var product = await _unitOfWork.Products.GetByIdAsync(productId, cancellationToken: cancellationToken);
            if (product == null) return;

            if (product.StockQuantity <= product.LowStockThreshold && !_lowStockAlertsSent.Contains(productId))
            {
                var admins = (await _unitOfWork.Users.GetAllAsync(cancellationToken: cancellationToken))
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
                        product.Sku,
                        cancellationToken
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
