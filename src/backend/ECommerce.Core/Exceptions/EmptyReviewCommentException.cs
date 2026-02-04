using ECommerce.Core.Exceptions.Base;

namespace ECommerce.Core.Exceptions;

/// <summary>
/// Exception thrown when a review comment is empty.
/// </summary>
public sealed class EmptyReviewCommentException : BadRequestException
{
    public EmptyReviewCommentException()
        : base("Review comment cannot be empty") { }
}
