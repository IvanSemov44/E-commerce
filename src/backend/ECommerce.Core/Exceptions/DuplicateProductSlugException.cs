using ECommerce.Core.Exceptions.Base;

namespace ECommerce.Core.Exceptions;

public sealed class DuplicateProductSlugException(string slug)
    : ConflictException($"A product with slug '{slug}' already exists.") { }
