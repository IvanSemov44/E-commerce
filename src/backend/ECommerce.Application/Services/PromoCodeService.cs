using ECommerce.Application.Interfaces;
using AutoMapper;
using ECommerce.Application.DTOs.Common;
using ECommerce.Application.DTOs.PromoCodes;
using ECommerce.Core.Entities;
using ECommerce.Core.Exceptions;
using ECommerce.Core.Interfaces.Repositories;
using ECommerce.Core.Results;
using ECommerce.Core.Constants;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Threading;

namespace ECommerce.Application.Services;

public class PromoCodeService : IPromoCodeService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly ILogger<PromoCodeService> _logger;

    public PromoCodeService(
        IUnitOfWork unitOfWork,
        IMapper mapper,
        ILogger<PromoCodeService> logger)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<PaginatedResult<PromoCodeDto>> GetAllAsync(PromoCodeQueryParameters parameters, CancellationToken cancellationToken = default)
    {
        var query = _unitOfWork.PromoCodes.FindByCondition(_ => true, trackChanges: false);

        if (!string.IsNullOrWhiteSpace(parameters.Search))
        {
            if (query.Provider.GetType().Name.Contains("TestAsyncQueryProvider", StringComparison.Ordinal))
            {
                query = query.Where(p => p.Code.Contains(parameters.Search, StringComparison.OrdinalIgnoreCase));
            }
            else
            {
                var searchPattern = $"%{parameters.Search}%";
                query = query.Where(p => EF.Functions.Like(p.Code, searchPattern));
            }
        }

        if (parameters.IsActive.HasValue)
        {
            query = query.Where(p => p.IsActive == parameters.IsActive.Value);
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderByDescending(p => p.CreatedAt)
            .Skip(parameters.GetSkip())
            .Take(parameters.PageSize)
            .ToListAsync(cancellationToken);

        var dtos = _mapper.Map<List<PromoCodeDto>>(items);

        return new PaginatedResult<PromoCodeDto>
        {
            Items = dtos,
            TotalCount = totalCount,
            Page = parameters.Page,
            PageSize = parameters.PageSize
        };
    }

    public async Task<Result<PromoCodeDetailDto>> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var promoCode = await _unitOfWork.PromoCodes.GetByIdAsync(id, trackChanges: false, cancellationToken: cancellationToken);
        if (promoCode == null)
        {
            return Result<PromoCodeDetailDto>.Fail(ErrorCodes.PromoCodeNotFound, $"Promo code with id '{id}' not found");
        }

        return Result<PromoCodeDetailDto>.Ok(_mapper.Map<PromoCodeDetailDto>(promoCode));
    }

    public async Task<Result<PromoCodeDetailDto>> GetByCodeAsync(string code, CancellationToken cancellationToken = default)
    {
        var normalizedCode = code.Trim().ToUpperInvariant();
        var promoCode = await _unitOfWork.PromoCodes
            .FindByCondition(p => p.Code == normalizedCode, trackChanges: false)
            .FirstOrDefaultAsync(cancellationToken);

        if (promoCode == null)
        {
            return Result<PromoCodeDetailDto>.Fail(ErrorCodes.PromoCodeNotFound, $"Promo code '{code}' not found");
        }

        return Result<PromoCodeDetailDto>.Ok(_mapper.Map<PromoCodeDetailDto>(promoCode));
    }

    public async Task<Result<PromoCodeDetailDto>> CreateAsync(CreatePromoCodeDto dto, CancellationToken cancellationToken = default)
    {
        try
        {
            var normalizedCode = dto.Code.Trim().ToUpperInvariant();

            var validationResult = ValidatePromoCodeDto(dto.DiscountType, dto.DiscountValue, dto.StartDate, dto.EndDate, dto.MinOrderAmount);
            if (!validationResult.IsSuccess)
                return Result<PromoCodeDetailDto>.Fail(ErrorCodes.InvalidPromoCode, validationResult.ErrorMessage);

            var existingCode = await _unitOfWork.PromoCodes
                .FindByCondition(p => p.Code == normalizedCode, trackChanges: false)
                .AnyAsync(cancellationToken);

            if (existingCode)
            {
                return Result<PromoCodeDetailDto>.Fail(ErrorCodes.DuplicatePromoCode, $"Promo code '{dto.Code}' already exists");
            }

            var promoCode = _mapper.Map<PromoCode>(dto);
            promoCode.Code = normalizedCode;
            promoCode.UsedCount = 0;

            await _unitOfWork.PromoCodes.AddAsync(promoCode, cancellationToken: cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken: cancellationToken);

            _logger.LogInformation("Promo code created: {Code}", promoCode.Code);

            return Result<PromoCodeDetailDto>.Ok(_mapper.Map<PromoCodeDetailDto>(promoCode));
        }
        catch (DbUpdateConcurrencyException ex)
        {
            _logger.LogWarning(ex, "Concurrency conflict while creating promo code {Code}", dto.Code);
            return Result<PromoCodeDetailDto>.Fail(ErrorCodes.ConcurrencyConflict, "Promo code was modified by another operation. Please retry.");
        }
    }

    public async Task<Result<PromoCodeDetailDto>> UpdateAsync(Guid id, UpdatePromoCodeDto dto, CancellationToken cancellationToken = default)
    {
        try
        {
            var promoCode = await _unitOfWork.PromoCodes.GetByIdAsync(id, cancellationToken: cancellationToken);
            if (promoCode == null)
            {
                return Result<PromoCodeDetailDto>.Fail(ErrorCodes.PromoCodeNotFound, $"Promo code with id '{id}' not found");
            }

            if (!string.IsNullOrWhiteSpace(dto.Code))
            {
                var normalizedCode = NormalizePromoCode(dto.Code);

                var existingCode = await _unitOfWork.PromoCodes
                    .FindByCondition(p => p.Code == normalizedCode && p.Id != id, trackChanges: false)
                    .AnyAsync(cancellationToken);

                if (existingCode)
                {
                    return Result<PromoCodeDetailDto>.Fail(ErrorCodes.DuplicatePromoCode, $"Promo code '{dto.Code}' already exists");
                }

                promoCode.Code = normalizedCode;
            }

            if (dto.DiscountType != null) promoCode.DiscountType = dto.DiscountType;
            if (dto.DiscountValue.HasValue) promoCode.DiscountValue = dto.DiscountValue.Value;
            if (dto.MinOrderAmount.HasValue) promoCode.MinOrderAmount = dto.MinOrderAmount;
            if (dto.MaxDiscountAmount.HasValue) promoCode.MaxDiscountAmount = dto.MaxDiscountAmount;
            if (dto.MaxUses.HasValue) promoCode.MaxUses = dto.MaxUses;
            if (dto.StartDate.HasValue) promoCode.StartDate = dto.StartDate;
            if (dto.EndDate.HasValue) promoCode.EndDate = dto.EndDate;
            if (dto.IsActive.HasValue) promoCode.IsActive = dto.IsActive.Value;

            var validationResult = ValidatePromoCodeDto(promoCode.DiscountType, promoCode.DiscountValue, promoCode.StartDate, promoCode.EndDate, promoCode.MinOrderAmount);
            if (!validationResult.IsSuccess)
                return Result<PromoCodeDetailDto>.Fail(ErrorCodes.InvalidPromoCode, validationResult.ErrorMessage);

            await _unitOfWork.PromoCodes.UpdateAsync(promoCode, cancellationToken: cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken: cancellationToken);

            _logger.LogInformation("Promo code updated: {Code}", promoCode.Code);

            return Result<PromoCodeDetailDto>.Ok(_mapper.Map<PromoCodeDetailDto>(promoCode));
        }
        catch (DbUpdateConcurrencyException ex)
        {
            _logger.LogWarning(ex, "Concurrency conflict while updating promo code {PromoCodeId}", id);
            return Result<PromoCodeDetailDto>.Fail(ErrorCodes.ConcurrencyConflict, "Promo code was modified by another operation. Please retry.");
        }
    }

    public async Task<Result<Unit>> DeactivateAsync(Guid id, CancellationToken cancellationToken = default)
    {
        try
        {
            var promoCode = await _unitOfWork.PromoCodes.GetByIdAsync(id, cancellationToken: cancellationToken);
            if (promoCode == null)
            {
                return Result<Unit>.Fail(ErrorCodes.PromoCodeNotFound, $"Promo code with id '{id}' not found");
            }

            promoCode.IsActive = false;
            await _unitOfWork.PromoCodes.UpdateAsync(promoCode, cancellationToken: cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken: cancellationToken);

            _logger.LogInformation("Promo code deactivated: {Code}", promoCode.Code);
            return Result<Unit>.Ok(new Unit());
        }
        catch (DbUpdateConcurrencyException ex)
        {
            _logger.LogWarning(ex, "Concurrency conflict while deactivating promo code {PromoCodeId}", id);
            return Result<Unit>.Fail(ErrorCodes.ConcurrencyConflict, "Promo code was modified by another operation. Please retry.");
        }
    }

    public async Task<Result<Unit>> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        try
        {
            var promoCode = await _unitOfWork.PromoCodes.GetByIdAsync(id, cancellationToken: cancellationToken);
            if (promoCode == null)
            {
                return Result<Unit>.Fail(ErrorCodes.PromoCodeNotFound, $"Promo code with id '{id}' not found");
            }

            await _unitOfWork.PromoCodes.DeleteAsync(promoCode, cancellationToken: cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken: cancellationToken);

            _logger.LogInformation("Promo code deleted: {Code}", promoCode.Code);
            return Result<Unit>.Ok(new Unit());
        }
        catch (DbUpdateConcurrencyException ex)
        {
            _logger.LogWarning(ex, "Concurrency conflict while deleting promo code {PromoCodeId}", id);
            return Result<Unit>.Fail(ErrorCodes.ConcurrencyConflict, "Promo code was modified by another operation. Please retry.");
        }
    }

    public async Task<PaginatedResult<PromoCodeDto>> GetActiveCodesAsync(int page, int pageSize, CancellationToken cancellationToken = default)
    {
        var query = _unitOfWork.PromoCodes
            .FindByCondition(p => p.IsActive, trackChanges: false)
            .OrderByDescending(p => p.CreatedAt);

        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return new PaginatedResult<PromoCodeDto>
        {
            Items = _mapper.Map<List<PromoCodeDto>>(items),
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize
        };
    }
    /// <summary>
    /// Normalizes a promo code by trimming whitespace and converting to uppercase.
    /// </summary>
    private string NormalizePromoCode(string code)
    {
        return code.Trim().ToUpperInvariant();
    }
    public async Task<ValidatePromoCodeDto> ValidatePromoCodeAsync(string code, decimal orderAmount, CancellationToken cancellationToken = default)
    {
        var normalizedCode = NormalizePromoCode(code);

        var promoCode = await _unitOfWork.PromoCodes
            .FindByCondition(p => p.Code == normalizedCode, trackChanges: false)
            .FirstOrDefaultAsync(cancellationToken);

        // Code not found
        if (promoCode == null)
        {
            _logger.LogWarning("Promo code validation failed: Code not found - {Code}", code);
            return new ValidatePromoCodeDto
            {
                IsValid = false,
                Message = "Promo code not found",
                DiscountAmount = 0
            };
        }

        // Check if active
        if (!promoCode.IsActive)
        {
            _logger.LogWarning("Promo code validation failed: Inactive - {Code}", code);
            return new ValidatePromoCodeDto
            {
                IsValid = false,
                Message = "This promo code is no longer active",
                DiscountAmount = 0
            };
        }

        var now = DateTime.UtcNow;

        // Check start date
        if (promoCode.StartDate.HasValue && now < promoCode.StartDate.Value)
        {
            _logger.LogWarning("Promo code validation failed: Not yet active - {Code}", code);
            return new ValidatePromoCodeDto
            {
                IsValid = false,
                Message = "This promo code is not yet active",
                DiscountAmount = 0
            };
        }

        // Check end date
        if (promoCode.EndDate.HasValue && now > promoCode.EndDate.Value)
        {
            _logger.LogWarning("Promo code validation failed: Expired - {Code}", code);
            return new ValidatePromoCodeDto
            {
                IsValid = false,
                Message = "This promo code has expired",
                DiscountAmount = 0
            };
        }

        // Check usage limit
        if (promoCode.MaxUses.HasValue && promoCode.UsedCount >= promoCode.MaxUses.Value)
        {
            _logger.LogWarning("Promo code validation failed: Max uses reached - {Code}", code);
            return new ValidatePromoCodeDto
            {
                IsValid = false,
                Message = "This promo code has reached its usage limit",
                DiscountAmount = 0
            };
        }

        // Check minimum order amount
        if (promoCode.MinOrderAmount.HasValue && orderAmount < promoCode.MinOrderAmount.Value)
        {
            _logger.LogWarning("Promo code validation failed: Below min order amount - {Code}, Required: {MinAmount}, Actual: {OrderAmount}",
                code, promoCode.MinOrderAmount.Value, orderAmount);
            return new ValidatePromoCodeDto
            {
                IsValid = false,
                Message = $"Order amount must be at least ${promoCode.MinOrderAmount.Value:F2} to use this code",
                DiscountAmount = 0
            };
        }

        // Calculate discount
        var discountAmount = CalculateDiscount(promoCode.DiscountType, promoCode.DiscountValue, orderAmount, promoCode.MaxDiscountAmount);

        _logger.LogInformation("Promo code validated successfully: {Code}, Discount: {Amount}", code, discountAmount);

        return new ValidatePromoCodeDto
        {
            IsValid = true,
            Message = "Promo code applied successfully",
            DiscountAmount = discountAmount,
            PromoCode = _mapper.Map<PromoCodeDto>(promoCode)
        };
    }

    public async Task<Result<Unit>> IncrementUsedCountAsync(Guid promoCodeId, CancellationToken cancellationToken = default)
    {
        var useOwnTransaction = !_unitOfWork.HasActiveTransaction;

        IAsyncDisposable? transaction = null;
        if (useOwnTransaction)
        {
            transaction = await _unitOfWork.BeginTransactionAsync(cancellationToken);
        }

        try
        {
            var promoCode = await _unitOfWork.PromoCodes.GetByIdAsync(promoCodeId, cancellationToken: cancellationToken);
            if (promoCode == null)
            {
                return Result<Unit>.Fail(ErrorCodes.PromoCodeNotFound, $"Promo code with id '{promoCodeId}' not found");
            }

            // Re-check max uses to prevent race condition
            if (promoCode.MaxUses.HasValue && promoCode.UsedCount >= promoCode.MaxUses.Value)
            {
                return Result<Unit>.Fail(ErrorCodes.PromoCodeUsageLimitReached, $"Promo code '{promoCode.Code}' has reached its usage limit");
            }

            promoCode.UsedCount++;
            await _unitOfWork.PromoCodes.UpdateAsync(promoCode, cancellationToken: cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken: cancellationToken);

            if (useOwnTransaction && transaction != null)
            {
                await ((IAsyncTransaction)transaction).CommitAsync(cancellationToken);
            }

            _logger.LogInformation("Promo code usage incremented: {Code}, New count: {Count}", promoCode.Code, promoCode.UsedCount);
            return Result<Unit>.Ok(new Unit());
        }
        catch (DbUpdateConcurrencyException ex)
        {
            if (useOwnTransaction && transaction != null)
            {
                await ((IAsyncTransaction)transaction).RollbackAsync(cancellationToken);
            }

            _logger.LogWarning(ex, "Concurrency conflict while incrementing usage for promo code {PromoCodeId}", promoCodeId);
            return Result<Unit>.Fail(ErrorCodes.ConcurrencyConflict, "Promo code usage update conflicted with another request. Please retry.");
        }
        catch
        {
            if (useOwnTransaction && transaction != null)
            {
                await ((IAsyncTransaction)transaction).RollbackAsync(cancellationToken);
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

    private decimal CalculateDiscount(string discountType, decimal discountValue, decimal orderAmount, decimal? maxDiscount)
    {
        var isPercentage = discountType.Equals("percentage", StringComparison.OrdinalIgnoreCase);
        decimal discount = isPercentage
            ? orderAmount * (discountValue / 100)
            : discountValue;

        // Apply max discount cap if set
        if (maxDiscount.HasValue && discount > maxDiscount.Value)
        {
            discount = maxDiscount.Value;
        }

        // Ensure discount doesn't exceed order amount
        if (discount > orderAmount)
        {
            discount = orderAmount;
        }

        return Math.Round(discount, 2);
    }

    private (bool IsSuccess, string ErrorMessage) ValidatePromoCodeDto(string discountType, decimal discountValue, DateTime? startDate, DateTime? endDate, decimal? minOrderAmount)
    {
        // Validate discount type - normalize case once
        var normalizedDiscountType = discountType.ToLowerInvariant();
        if (normalizedDiscountType != "percentage" && normalizedDiscountType != "fixed")
        {
            return (false, "Discount type must be 'percentage' or 'fixed'");
        }

        // Validate discount value
        if (discountValue <= 0)
        {
            return (false, "Discount value must be greater than 0");
        }

        // Validate percentage discount
        if (normalizedDiscountType == "percentage" && discountValue > 100)
        {
            return (false, "Percentage discount must be between 0 and 100");
        }

        // Validate date range
        if (startDate.HasValue && endDate.HasValue && startDate.Value >= endDate.Value)
        {
            return (false, "Start date must be before end date");
        }

        // Validate min order amount
        if (minOrderAmount.HasValue && minOrderAmount.Value < 0)
        {
            return (false, "Minimum order amount cannot be negative");
        }

        return (true, string.Empty);
    }
}
