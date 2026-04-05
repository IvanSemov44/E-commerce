using ECommerce.SharedKernel.Domain;
using ECommerce.SharedKernel.Results;
using ECommerce.Promotions.Domain.Errors;

namespace ECommerce.Promotions.Domain.ValueObjects;

public class DateRange : ValueObject
{
    public DateTime Start { get; private set; }
    public DateTime End { get; private set; }

    private DateRange() { }

    private DateRange(DateTime start, DateTime end)
    {
        Start = start;
        End = end;
    }

    public static Result<DateRange> Create(DateTime start, DateTime end)
    {
        if (start >= end)
            return Result<DateRange>.Fail(PromotionsErrors.DateRangeInvalid);

        return Result<DateRange>.Ok(new DateRange(start, end));
    }

    public bool IsActive(DateTime now) => now >= Start && now <= End;

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Start;
        yield return End;
    }
}