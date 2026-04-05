using ECommerce.SharedKernel.Results;

namespace ECommerce.Reviews.Domain.Errors;

public static class ReviewsErrors
{
    public static readonly DomainError ReviewNotFound = new("REVIEW_NOT_FOUND", "Review does not exist");
    public static readonly DomainError DuplicateReview = new("DUPLICATE_REVIEW", "User has already reviewed this product");
    public static readonly DomainError ProductNotFound = new("PRODUCT_NOT_FOUND", "Product does not exist");
    public static readonly DomainError UserNotFound = new("USER_NOT_FOUND", "User does not exist");
    public static readonly DomainError Unauthorized = new("UNAUTHORIZED", "You do not have permission to modify this review");
    public static readonly DomainError RatingRange = new("RATING_RANGE", "Rating must be between 1 and 5");
    public static readonly DomainError ReviewTitleEmpty = new("REVIEW_TITLE_EMPTY", "Review title cannot be empty");
    public static readonly DomainError ReviewTitleLong = new("REVIEW_TITLE_LONG", "Review title must be 100 characters or fewer");
    public static readonly DomainError ReviewBodyEmpty = new("REVIEW_BODY_EMPTY", "Review body cannot be empty");
    public static readonly DomainError ReviewBodyShort = new("REVIEW_BODY_SHORT", "Review body must be at least 10 characters");
    public static readonly DomainError ReviewBodyLong = new("REVIEW_BODY_LONG", "Review body must be 1000 characters or fewer");
    public static readonly DomainError ReviewAlreadyApproved = new("REVIEW_ALREADY_APPROVED", "Approved reviews cannot be edited");
    public static readonly DomainError ReviewUpdateExpired = new("REVIEW_UPDATE_EXPIRED", "Review edit window has expired");
    public static readonly DomainError ConcurrencyConflict = new("CONCURRENCY_CONFLICT", "Review was modified by another user");
}
