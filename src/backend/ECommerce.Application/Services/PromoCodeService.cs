using ECommerce.Application.Interfaces;
using AutoMapper;
using ECommerce.Application.DTOs.Common;
using ECommerce.Application.DTOs.PromoCodes;
using ECommerce.Core.Entities;
using ECommerce.Core.Exceptions;
using ECommerce.Core.Interfaces.Repositories;
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

    public async Task<PaginatedResult<PromoCodeDto>> GetAllAsync(int page = 1, int pageSize = 20, string? search = null, bool? isActive = null, CancellationToken cancellationToken = default)
    {
        // Get all promo codes (this is in-memory filtering for simplicity)
        // In a production app with large datasets, you'd want a custom repository method
        var allPromoCodes = await _unitOfWork.PromoCodes.GetAllAsync(cancellationToken: cancellationToken);

        // Apply filters
        var filtered = allPromoCodes.AsEnumerable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var searchTerm = search.ToLowerInvariant();
            filtered = filtered.Where(p => p.Code.ToLower().Contains(searchTerm));
        }

        if (isActive.HasValue)
        {
            filtered = filtered.Where(p => p.IsActive == isActive.Value);
        }

        // Get total count
        var totalCount = filtered.Count();

        // Apply pagination and ordering
        var items = filtered
            .OrderByDescending(p => p.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        var dtos = _mapper.Map<List<PromoCodeDto>>(items);

        return new PaginatedResult<PromoCodeDto>
        {
            Items = dtos,
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize
        };
    }

    public async Task<PromoCodeDetailDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var promoCode = await _unitOfWork.PromoCodes.GetByIdAsync(id, cancellationToken: cancellationToken);
        return promoCode == null ? null : _mapper.Map<PromoCodeDetailDto>(promoCode);
    }

    public async Task<PromoCodeDetailDto?> GetByCodeAsync(string code, CancellationToken cancellationToken = default)
    {
        var normalizedCode = code.Trim().ToUpperInvariant();
        var allPromoCodes = await _unitOfWork.PromoCodes.GetAllAsync(cancellationToken: cancellationToken);
        var promoCode = allPromoCodes.FirstOrDefault(p => p.Code.ToUpper() == normalizedCode);

        return promoCode == null ? null : _mapper.Map<PromoCodeDetailDto>(promoCode);
    }

    public async Task<PromoCodeDetailDto> CreateAsync(CreatePromoCodeDto dto, CancellationToken cancellationToken = default)
    {
        // Normalize code
        var normalizedCode = dto.Code.Trim().ToUpperInvariant();

        // Validate input
        ValidatePromoCodeDto(dto.DiscountType, dto.DiscountValue, dto.StartDate, dto.EndDate, dto.MinOrderAmount);

        // Check for duplicate code
        var allPromoCodes = await _unitOfWork.PromoCodes.GetAllAsync(cancellationToken: cancellationToken);
        var existingCode = allPromoCodes.Any(p => p.Code.ToUpper() == normalizedCode);

        if (existingCode)
        {
            throw new PromoCodeAlreadyExistsException(dto.Code);
        }

        var promoCode = _mapper.Map<PromoCode>(dto);
        promoCode.Code = normalizedCode;
        promoCode.UsedCount = 0;

        await _unitOfWork.PromoCodes.AddAsync(promoCode, cancellationToken: cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken: cancellationToken);

        _logger.LogInformation("Promo code created: {Code}", promoCode.Code);

        return _mapper.Map<PromoCodeDetailDto>(promoCode);
    }

    public async Task<PromoCodeDetailDto> UpdateAsync(Guid id, UpdatePromoCodeDto dto, CancellationToken cancellationToken = default)
    {
        var promoCode = await _unitOfWork.PromoCodes.GetByIdAsync(id, cancellationToken: cancellationToken);
        if (promoCode == null)
        {
            throw new PromoCodeNotFoundException(id);
        }

        // UpdateAsync only provided fields
        if (!string.IsNullOrWhiteSpace(dto.Code))
        {
            var normalizedCode = dto.Code.Trim().ToUpperInvariant();

            // Check for duplicate code (excluding current)
            var allPromoCodes = await _unitOfWork.PromoCodes.GetAllAsync(cancellationToken: cancellationToken);
            var existingCode = allPromoCodes.Any(p => p.Code.ToUpper() == normalizedCode && p.Id != id);

            if (existingCode)
            {
                throw new PromoCodeAlreadyExistsException(dto.Code);
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

        // Validate updated values
        ValidatePromoCodeDto(promoCode.DiscountType, promoCode.DiscountValue, promoCode.StartDate, promoCode.EndDate, promoCode.MinOrderAmount);

        await _unitOfWork.PromoCodes.UpdateAsync(promoCode, cancellationToken: cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken: cancellationToken);

        _logger.LogInformation("Promo code updated: {Code}", promoCode.Code);

        return _mapper.Map<PromoCodeDetailDto>(promoCode);
    }

    public async Task DeactivateAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var promoCode = await _unitOfWork.PromoCodes.GetByIdAsync(id, cancellationToken: cancellationToken);
        if (promoCode == null)
        {
            throw new PromoCodeNotFoundException(id);
        }

        promoCode.IsActive = false;
        await _unitOfWork.PromoCodes.UpdateAsync(promoCode, cancellationToken: cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken: cancellationToken);

        _logger.LogInformation("Promo code deactivated: {Code}", promoCode.Code);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var promoCode = await _unitOfWork.PromoCodes.GetByIdAsync(id, cancellationToken: cancellationToken);
        if (promoCode == null)
        {
            throw new PromoCodeNotFoundException(id);
        }

        await _unitOfWork.PromoCodes.DeleteAsync(promoCode, cancellationToken: cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken: cancellationToken);

        _logger.LogInformation("Promo code deleted: {Code}", promoCode.Code);
    }

    public async Task<List<PromoCodeDto>> GetActiveCodesAsync(CancellationToken cancellationToken = default)
    {
        var allCodes = await _unitOfWork.PromoCodes.GetAllAsync(trackChanges: false, cancellationToken: cancellationToken);
        var activeCodes = allCodes.Where(p => p.IsActive).ToList();
        return _mapper.Map<List<PromoCodeDto>>(activeCodes);
    }

    public async Task<ValidatePromoCodeDto> ValidatePromoCodeAsync(string code, decimal orderAmount, CancellationToken cancellationToken = default)
    {
        var normalizedCode = code.Trim().ToUpperInvariant();

        var allPromoCodes = await _unitOfWork.PromoCodes.GetAllAsync(cancellationToken: cancellationToken);
        var promoCode = allPromoCodes.FirstOrDefault(p => p.Code.ToUpper() == normalizedCode);

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

    public async Task IncrementUsedCountAsync(Guid promoCodeId, CancellationToken cancellationToken = default)
    {
        // Use a transaction to ensure atomic increment and prevent race conditions
        await using var transaction = await _unitOfWork.BeginTransactionAsync();

        try
        {
            var promoCode = await _unitOfWork.PromoCodes.GetByIdAsync(promoCodeId, cancellationToken: cancellationToken);
            if (promoCode == null)
            {
                throw new PromoCodeNotFoundException(promoCodeId);
            }

            // Re-check max uses to prevent race condition
            if (promoCode.MaxUses.HasValue && promoCode.UsedCount >= promoCode.MaxUses.Value)
            {
                throw new PromoCodeUsageLimitReachedException(promoCode.Code);
            }

            promoCode.UsedCount++;
            await _unitOfWork.PromoCodes.UpdateAsync(promoCode, cancellationToken: cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken: cancellationToken);

            await transaction.CommitAsync();

            _logger.LogInformation("Promo code usage incremented: {Code}, New count: {Count}", promoCode.Code, promoCode.UsedCount);
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    private decimal CalculateDiscount(string discountType, decimal discountValue, decimal orderAmount, decimal? maxDiscount)
    {
        decimal discount = discountType.ToLower() == "percentage"
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

    private void ValidatePromoCodeDto(string discountType, decimal discountValue, DateTime? startDate, DateTime? endDate, decimal? minOrderAmount)
    {
        // Validate discount type
        if (discountType.ToLower() != "percentage" && discountType.ToLower() != "fixed")
        {
            throw new InvalidPromoCodeConfigurationException("Discount type must be 'percentage' or 'fixed'");
        }

        // Validate discount value
        if (discountValue <= 0)
        {
            throw new InvalidPromoCodeConfigurationException("Discount value must be greater than 0");
        }

        // Validate percentage discount
        if (discountType.ToLower() == "percentage" && discountValue > 100)
        {
            throw new InvalidPromoCodeConfigurationException("Percentage discount must be between 0 and 100");
        }

        // Validate date range
        if (startDate.HasValue && endDate.HasValue && startDate.Value >= endDate.Value)
        {
            throw new InvalidPromoCodeConfigurationException("Start date must be before end date");
        }

        // Validate min order amount
        if (minOrderAmount.HasValue && minOrderAmount.Value < 0)
        {
            throw new InvalidPromoCodeConfigurationException("Minimum order amount cannot be negative");
        }
    }
}
