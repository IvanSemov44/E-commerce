using ECommerce.Reviews.Domain.Errors;
using ECommerce.SharedKernel.Results;

namespace ECommerce.Reviews.Domain.ValueObjects;

public sealed record ReviewContent
{
    public string? Title { get; }
    public string Body { get; }

    private ReviewContent(string? title, string body)
    {
        Title = string.IsNullOrWhiteSpace(title) ? null : title.Trim();
        Body = body.Trim();
    }

    public static Result<ReviewContent> Create(string? title, string comment)
    {
        string trimmedComment = comment.Trim();
        if (string.IsNullOrWhiteSpace(trimmedComment))
            return Result<ReviewContent>.Fail(ReviewsErrors.ReviewBodyEmpty);

        if (trimmedComment.Length < 10)
            return Result<ReviewContent>.Fail(ReviewsErrors.ReviewBodyShort);

        if (trimmedComment.Length > 1000)
            return Result<ReviewContent>.Fail(ReviewsErrors.ReviewBodyLong);

        if (!string.IsNullOrWhiteSpace(title) && title.Trim().Length > 100)
            return Result<ReviewContent>.Fail(ReviewsErrors.ReviewTitleLong);

        return Result<ReviewContent>.Ok(new ReviewContent(title, trimmedComment));
    }
}
