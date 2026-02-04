using ECommerce.Core.Exceptions.Base;

namespace ECommerce.Core.Exceptions;

public class DuplicateCategorySlugException : ConflictException
{
    public DuplicateCategorySlugException(string slug)
        : base($"Category with slug '{slug}' already exists") { }
}
