using ECommerce.Core.Exceptions.Base;

namespace ECommerce.Core.Exceptions;

public sealed class EmptyReviewCommentException()
    : BadRequestException("Review comment cannot be empty") { }
