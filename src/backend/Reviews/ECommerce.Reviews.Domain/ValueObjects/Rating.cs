using ECommerce.Reviews.Domain.Errors;
using ECommerce.SharedKernel.Results;

namespace ECommerce.Reviews.Domain.ValueObjects;

public sealed record Rating
{
    public int Value { get; }

    private Rating(int value)
    {
        Value = value;
    }

    public static Result<Rating> Create(int value)
    {
        if (value < 1 || value > 5)
            return Result<Rating>.Fail(ReviewsErrors.RatingRange);

        return Result<Rating>.Ok(new Rating(value));
    }
}
