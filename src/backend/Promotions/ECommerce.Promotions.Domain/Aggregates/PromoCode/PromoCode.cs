using ECommerce.SharedKernel.Domain;
using ECommerce.SharedKernel.Results;
using ECommerce.Promotions.Domain.Errors;
using ECommerce.Promotions.Domain.Events;
using ECommerce.Promotions.Domain.ValueObjects;

namespace ECommerce.Promotions.Domain.Aggregates.PromoCode;

public sealed class PromoCode : AggregateRoot
{
    public PromoCodeString Code { get; private set; } = null!;
    public DiscountValue Discount { get; private set; } = null!;
    public DateRange? ValidPeriod { get; private set; }
    public int? MaxUses { get; private set; }
    public int UsedCount { get; private set; }
    public bool IsActive { get; private set; }
    public decimal? MinimumOrderAmount { get; private set; }
    public decimal? MaxDiscountAmount { get; private set; }
    public DateTime? DeletedAt { get; private set; }
    public byte[]? RowVersion { get; private set; }

    private PromoCode() { }

    public static PromoCode Create(
        PromoCodeString code,
        DiscountValue discount,
        DateRange? validPeriod,
        int? maxUses = null,
        decimal? minimumOrderAmount = null,
        decimal? maxDiscountAmount = null,
        DateTime? createdAt = null,
        DateTime? updatedAt = null)
    {
        var now = createdAt ?? DateTime.UtcNow;
        var promo = new PromoCode
        {
            Id = Guid.NewGuid(),
            Code = code,
            Discount = discount,
            ValidPeriod = validPeriod,
            MaxUses = maxUses,
            UsedCount = 0,
            IsActive = true,
            MinimumOrderAmount = minimumOrderAmount,
            MaxDiscountAmount = maxDiscountAmount,
            CreatedAt = now,
            UpdatedAt = updatedAt ?? now
        };
        promo.AddDomainEvent(new PromoCodeChangedEvent(promo.Id, promo.Code.Value, promo.Discount.Amount, promo.IsActive, false));
        return promo;
    }

    public bool IsValidNow(DateTime now)
    {
        if (!IsActive) return false;
        if (ValidPeriod is not null && !ValidPeriod.IsActive(now)) return false;
        if (MaxUses.HasValue && UsedCount >= MaxUses.Value) return false;
        return true;
    }

    public Result RecordUsage(DateTime? now = null)
    {
        now ??= DateTime.UtcNow;
        if (!IsValidNow(now.Value))
            return Result.Fail(PromotionsErrors.PromoNotValid);

        UsedCount++;
        UpdatedAt = now.Value;

        if (MaxUses.HasValue && UsedCount >= MaxUses.Value)
        {
            IsActive = false;
            AddDomainEvent(new PromoCodeExhaustedEvent(Id, Code.Value));
        }
        else
        {
            AddDomainEvent(new PromoCodeAppliedEvent(Id, Code.Value));
        }

        return Result.Ok();
    }

    public void Deactivate(DateTime? now = null)
    {
        IsActive = false;
        UpdatedAt = now ?? DateTime.UtcNow;
        AddDomainEvent(new PromoCodeChangedEvent(Id, Code.Value, Discount.Amount, IsActive, false));
    }

    public void SoftDelete(DateTime? now = null)
    {
        DeletedAt = now ?? DateTime.UtcNow;
        IsActive = false;
        UpdatedAt = DeletedAt.Value;
        AddDomainEvent(new PromoCodeChangedEvent(Id, Code.Value, Discount.Amount, IsActive, true));
    }

    /// <summary>
    /// Called by the application layer with raw primitive inputs.
    /// Builds value objects and validates internally.
    /// </summary>
    public static Result<PromoCode> Create(
        string code,
        string discountType,
        decimal discountValue,
        DateTime? validFrom = null,
        DateTime? validUntil = null,
        int? maxUses = null,
        decimal? minimumOrderAmount = null,
        decimal? maxDiscountAmount = null)
    {
        var codeResult = PromoCodeString.Create(code);
        if (!codeResult.IsSuccess)
            return Result<PromoCode>.Fail(codeResult.GetErrorOrThrow());

        var discountResult = discountType.Trim().ToUpperInvariant() switch
        {
            "PERCENTAGE" => DiscountValue.Percentage(discountValue),
            "FIXED" => DiscountValue.Fixed(discountValue),
            _ => Result<DiscountValue>.Fail(PromotionsErrors.InvalidDiscountType)
        };
        if (!discountResult.IsSuccess)
            return Result<PromoCode>.Fail(discountResult.GetErrorOrThrow());

        DateRange? validPeriod = null;
        if (validFrom.HasValue && validUntil.HasValue)
        {
            var periodResult = DateRange.Create(validFrom.Value, validUntil.Value);
            if (!periodResult.IsSuccess)
                return Result<PromoCode>.Fail(periodResult.GetErrorOrThrow());
            validPeriod = periodResult.GetDataOrThrow();
        }

        return Result<PromoCode>.Ok(Create(
            codeResult.GetDataOrThrow(),
            discountResult.GetDataOrThrow(),
            validPeriod,
            maxUses,
            minimumOrderAmount,
            maxDiscountAmount));
    }

    public Result Update(
        string? discountType = null,
        decimal? discountValue = null,
        DateTime? validFrom = null,
        DateTime? validUntil = null,
        int? maxUses = null,
        bool clearMaxUses = false,
        decimal? minimumOrderAmount = null,
        bool clearMinimumOrderAmount = false,
        decimal? maxDiscountAmount = null,
        bool clearMaxDiscountAmount = false,
        bool? isActive = null,
        DateTime? updatedAt = null)
    {
        if (discountType is not null && discountValue.HasValue)
        {
            var discountResult = discountType.Trim().ToUpperInvariant() switch
            {
                "PERCENTAGE" => DiscountValue.Percentage(discountValue.Value),
                "FIXED" => DiscountValue.Fixed(discountValue.Value),
                _ => Result<DiscountValue>.Fail(PromotionsErrors.InvalidDiscountType)
            };
            if (!discountResult.IsSuccess)
                return Result.Fail(discountResult.GetErrorOrThrow());
            Discount = discountResult.GetDataOrThrow();
        }

        if (validFrom.HasValue && validUntil.HasValue)
        {
            var periodResult = DateRange.Create(validFrom.Value, validUntil.Value);
            if (!periodResult.IsSuccess)
                return Result.Fail(periodResult.GetErrorOrThrow());
            ValidPeriod = periodResult.GetDataOrThrow();
        }

        if (clearMaxUses) MaxUses = null;
        else if (maxUses.HasValue) MaxUses = maxUses;

        if (clearMinimumOrderAmount) MinimumOrderAmount = null;
        else if (minimumOrderAmount.HasValue) MinimumOrderAmount = minimumOrderAmount;

        if (clearMaxDiscountAmount) MaxDiscountAmount = null;
        else if (maxDiscountAmount.HasValue) MaxDiscountAmount = maxDiscountAmount;

        if (isActive.HasValue) IsActive = isActive.Value;

        UpdatedAt = updatedAt ?? DateTime.UtcNow;
        AddDomainEvent(new PromoCodeChangedEvent(Id, Code.Value, Discount.Amount, IsActive, false));
        return Result.Ok();
    }
}
