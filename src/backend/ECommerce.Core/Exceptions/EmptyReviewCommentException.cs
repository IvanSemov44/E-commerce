namespace ECommerce.Core.Exceptions;

/// <summary>
/// Exception thrown when a review comment is empty or whitespace.
/// </summary>
public sealed class EmptyReviewCommentException : BadRequestException
{
    public EmptyReviewCommentException()
        : base("Comment cannot be empty") { }

    public EmptyReviewCommentException(string message)
        : base(message) { }
}
