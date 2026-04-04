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
        return new PromoCode
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
    }

    public Result Update(
        PromoCodeString? code = null,
        DiscountValue? discount = null,
        DateRange? validPeriod = null,
        bool clearValidPeriod = false,
        int? maxUses = null,
        bool clearMaxUses = false,
        decimal? minimumOrderAmount = null,
        bool clearMinimumOrderAmount = false,
        decimal? maxDiscountAmount = null,
        bool clearMaxDiscountAmount = false,
        bool? isActive = null,
        DateTime? updatedAt = null)
    {
        if (code is not null) Code = code;
        if (discount is not null) Discount = discount;

        if (clearValidPeriod) ValidPeriod = null;
        else if (validPeriod is not null) ValidPeriod = validPeriod;

        if (clearMaxUses) MaxUses = null;
        else if (maxUses.HasValue) MaxUses = maxUses;

        if (clearMinimumOrderAmount) MinimumOrderAmount = null;
        else if (minimumOrderAmount.HasValue) MinimumOrderAmount = minimumOrderAmount;

        if (clearMaxDiscountAmount) MaxDiscountAmount = null;
        else if (maxDiscountAmount.HasValue) MaxDiscountAmount = maxDiscountAmount;

        if (isActive.HasValue) IsActive = isActive.Value;

        UpdatedAt = updatedAt ?? DateTime.UtcNow;
        return Result.Ok();
    }
}