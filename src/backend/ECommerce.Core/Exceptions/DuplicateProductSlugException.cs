using ECommerce.Core.Exceptions.Base;

namespace ECommerce.Core.Exceptions;

/// <summary>
/// Exception thrown when attempting to create a product with a duplicate slug.
/// </summary>
public sealed class DuplicateProductSlugException : ConflictException
{
    public DuplicateProductSlugException(string slug)
        : base($"A product with slug '{slug}' already exists.")
    {
    }
}
