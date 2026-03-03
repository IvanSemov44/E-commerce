using ECommerce.Application.Interfaces;
using ECommerce.Application.DTOs.Common;
using ECommerce.Application.DTOs.Inventory;
using AutoMapper;
using Microsoft.EntityFrameworkCore;
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

        var useOwnTransaction = !_unitOfWork.HasActiveTransaction;

        IAsyncTransaction? transaction = null;
        if (useOwnTransaction)
        {
            transaction = await _unitOfWork.BeginTransactionAsync(cancellationToken);
        }

        try
        {
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

            if (useOwnTransaction && transaction != null)
            {
                await transaction.CommitAsync(cancellationToken);
            }

            _logger.LogInformation("Stock reduced for product {ProductId}: {Quantity} units. New stock: {NewStock}",
                productId, quantity, product.StockQuantity);

            await CheckAndSendLowStockAlertsAsync(productId, cancellationToken);
        }
        catch
        {
            if (useOwnTransaction && transaction != null)
            {
                await transaction.RollbackAsync(cancellationToken);
            }
            throw;
        }
        finally
        {
            if (transaction != null)
            {
                await transaction.DisposeAsync();
            }
        }
    }

    /// <summary>
    /// Batch reduces stock for multiple products in a single transaction (prevents N+1 queries and transactions).
    /// PERFORMANCE FIX: Single transaction for all items instead of N individual transactions.
    /// </summary>
    public async Task ReduceStockBatchAsync(List<(Guid ProductId, int Quantity, string Reason, Guid? ReferenceId, Guid? UserId)> items, CancellationToken cancellationToken = default)
    {
        if (!items.Any())
            return;

        IAsyncTransaction? transaction = null;
        var useOwnTransaction = !_unitOfWork.HasActiveTransaction;

        try
        {
            if (useOwnTransaction)
                transaction = await _unitOfWork.BeginTransactionAsync(cancellationToken);

            // Batch load all products to avoid N+1
            var productIds = items.Select(i => i.ProductId).Distinct().ToList();
            var products = await _unitOfWork.Products.GetByIdsAsync(productIds, trackChanges: true, cancellationToken);
            var productDict = products.ToDictionary(p => p.Id);

            var logsToAdd = new List<InventoryLog>();
            var productsToCheck = new HashSet<Guid>();

            // Process all items
            foreach (var (productId, quantity, reason, referenceId, userId) in items)
            {
                if (!productDict.TryGetValue(productId, out var product))
                    throw new ProductNotFoundException(productId);

                if (product.StockQuantity < quantity)
                    throw new InsufficientStockException(product.Name, quantity, product.StockQuantity);

                product.StockQuantity -= quantity;
                product.UpdatedAt = DateTime.UtcNow;

                logsToAdd.Add(new InventoryLog
                {
                    ProductId = productId,
                    QuantityChange = -quantity,
                    Reason = reason,
                    ReferenceId = referenceId,
                    CreatedByUserId = userId
                });

                productsToCheck.Add(productId);
            }

            // Batch update products
            await _unitOfWork.Products.UpdateRangeAsync(productDict.Values.ToList(), cancellationToken);

            // Batch add logs
            await _unitOfWork.InventoryLogs.AddRangeAsync(logsToAdd, cancellationToken);

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            if (useOwnTransaction && transaction != null)
                await transaction.CommitAsync(cancellationToken);

            _logger.LogInformation("Batch stock reduction completed for {ItemCount} products", items.Count);

            // Check low stock alerts for affected products (best effort - don't fail on error)
            foreach (var productId in productsToCheck)
            {
                try
                {
                    await CheckAndSendLowStockAlertsAsync(productId, cancellationToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error checking low stock alert for product {ProductId}", productId);
                }
            }
        }
        catch (Exception ex)
        {
            if (useOwnTransaction && transaction != null)
                await transaction.RollbackAsync(cancellationToken);
            _logger.LogError(ex, "Error in batch stock reduction");
            throw;
        }
        finally
        {
            if (transaction != null)
                await transaction.DisposeAsync();
        }
    }

    public async Task IncreaseStockAsync(Guid productId, int quantity, string reason, Guid? referenceId = null, Guid? userId = null, CancellationToken cancellationToken = default)
    {
        if (quantity <= 0)
            throw new InvalidQuantityException("Quantity must be positive");

        var useOwnTransaction = !_unitOfWork.HasActiveTransaction;

        IAsyncTransaction? transaction = null;
        if (useOwnTransaction)
        {
            transaction = await _unitOfWork.BeginTransactionAsync(cancellationToken);
        }

        try
        {
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

            if (useOwnTransaction && transaction != null)
            {
                await transaction.CommitAsync(cancellationToken);
            }

            _logger.LogInformation("Stock increased for product {ProductId}: {Quantity} units. New stock: {NewStock}",
                productId, quantity, product.StockQuantity);

            if (product.StockQuantity > product.LowStockThreshold)
            {
                _lowStockAlertsSent.Remove(productId);
            }
        }
        catch
        {
            if (useOwnTransaction && transaction != null)
            {
                await transaction.RollbackAsync(cancellationToken);
            }
            throw;
        }
        finally
        {
            if (transaction != null)
            {
                await transaction.DisposeAsync();
            }
        }
    }

    public async Task AdjustStockAsync(Guid productId, int newQuantity, string reason, string? notes = null, Guid? userId = null, CancellationToken cancellationToken = default)
    {
        if (newQuantity < 0)
            throw new InvalidQuantityException("Quantity cannot be negative");

        var useOwnTransaction = !_unitOfWork.HasActiveTransaction;

        IAsyncTransaction? transaction = null;
        if (useOwnTransaction)
        {
            transaction = await _unitOfWork.BeginTransactionAsync(cancellationToken);
        }

        try
        {
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

            if (useOwnTransaction && transaction != null)
            {
                await transaction.CommitAsync(cancellationToken);
            }

            _logger.LogInformation("Stock adjusted for product {ProductId}: {PreviousStock} -> {NewStock}",
                productId, previousStock, newQuantity);

            await CheckAndSendLowStockAlertsAsync(productId, cancellationToken);
        }
        catch
        {
            if (useOwnTransaction && transaction != null)
            {
                await transaction.RollbackAsync(cancellationToken);
            }
            throw;
        }
        finally
        {
            if (transaction != null)
            {
                await transaction.DisposeAsync();
            }
        }
    }

    public async Task<StockCheckResponse> CheckStockAvailabilityAsync(List<StockCheckItemDto> items, CancellationToken cancellationToken = default)
    {
        var issues = new List<StockIssueDto>();

        // Batch-load all products to avoid N+1
        var productIds = items.Select(i => i.ProductId).Distinct().ToList();
        var products = await _unitOfWork.Products
            .FindByCondition(p => productIds.Contains(p.Id), trackChanges: false)
            .ToDictionaryAsync(p => p.Id, cancellationToken);

        foreach (var item in items)
        {
            if (!products.TryGetValue(item.ProductId, out var product))
            {
                issues.Add(new StockIssueDto
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
                issues.Add(new StockIssueDto
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

        return new StockCheckResponse { IsAvailable = issues.Count == 0, Issues = issues };
    }

    public async Task<bool> IsStockAvailableAsync(Guid productId, int quantity, CancellationToken cancellationToken = default)
    {
        var product = await _unitOfWork.Products.GetByIdAsync(productId, cancellationToken: cancellationToken);
        return product != null && product.StockQuantity >= quantity;
    }

    public async Task<PaginatedResult<InventoryDto>> GetAllInventoryAsync(InventoryQueryParameters parameters, CancellationToken cancellationToken = default)
    {
        var query = _unitOfWork.Products.FindByCondition(_ => true, trackChanges: false);

        if (!string.IsNullOrWhiteSpace(parameters.Search))
        {
            var searchPattern = $"%{parameters.Search}%";
            query = query.Where(p => EF.Functions.Like(p.Name, searchPattern) ||
                                   (p.Sku != null && EF.Functions.Like(p.Sku, searchPattern)));
        }

        if (parameters.LowStockOnly == true)
        {
            query = query.Where(p => p.StockQuantity <= p.LowStockThreshold);
        }

        query = query.OrderBy(p => p.StockQuantity).ThenBy(p => p.Name);

        var totalCount = await query.CountAsync(cancellationToken);

        var products = await query
            .Skip(parameters.GetSkip())
            .Take(parameters.PageSize)
            .ToListAsync(cancellationToken);

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
        var lowStockProducts = await _unitOfWork.Products
            .FindByCondition(p => p.StockQuantity <= p.LowStockThreshold && p.IsActive, trackChanges: false)
            .OrderBy(p => p.StockQuantity)
            .ToListAsync(cancellationToken);

        return lowStockProducts.Select(p => _mapper.Map<LowStockAlertDto>(p)).ToList();
    }

    public async Task<List<InventoryLogDto>> GetInventoryHistoryAsync(Guid productId, int page = 1, int pageSize = 50, CancellationToken cancellationToken = default)
    {
        var logs = await _unitOfWork.InventoryLogs
            .FindByCondition(log => log.ProductId == productId, trackChanges: false)
            .OrderByDescending(log => log.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        var product = await _unitOfWork.Products.GetByIdAsync(productId, cancellationToken: cancellationToken);
        var productName = product?.Name ?? "Unknown Product";

        // Batch-load users to avoid N+1
        var userIds = logs.Where(l => l.CreatedByUserId.HasValue).Select(l => l.CreatedByUserId!.Value).Distinct().ToList();
        var users = new Dictionary<Guid, string>();

        if (userIds.Count > 0)
        {
            var userEntities = await _unitOfWork.Users
                .FindByCondition(u => userIds.Contains(u.Id), trackChanges: false)
                .ToListAsync(cancellationToken);

            foreach (var user in userEntities)
            {
                users[user.Id] = $"{user.FirstName} {user.LastName}";
            }
        }

        var currentStock = product?.StockQuantity ?? 0;
        var result = new List<InventoryLogDto>();

        foreach (var log in logs)
        {
            var dto = _mapper.Map<InventoryLogDto>(log);
            var createdByUserName = log.CreatedByUserId.HasValue && users.ContainsKey(log.CreatedByUserId.Value)
                ? users[log.CreatedByUserId.Value]
                : null;

            var finalDto = dto with
            {
                ProductName = productName,
                StockAfterChange = currentStock,
                CreatedByUserName = createdByUserName
            };

            result.Add(finalDto);

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
                // Query only admin users from database instead of filtering in memory
                var admins = await _unitOfWork.Users
                    .FindByCondition(u => u.Role == UserRole.Admin || u.Role == UserRole.SuperAdmin, trackChanges: false)
                    .ToListAsync(cancellationToken);

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

    public async Task<InventoryDto?> GetProductByIdAsync(Guid productId, CancellationToken cancellationToken = default)
    {
        var product = await _unitOfWork.Products.GetByIdAsync(productId, trackChanges: false, cancellationToken);
        if (product == null)
            return null;

        return _mapper.Map<InventoryDto>(product);
    }
}
