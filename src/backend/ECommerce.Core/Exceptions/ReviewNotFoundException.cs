using ECommerce.Core.Exceptions.Base;

namespace ECommerce.Core.Exceptions;

public sealed class ReviewNotFoundException(Guid reviewId)
    : NotFoundException($"Review with ID '{reviewId}' was not found.") { }
