using ECommerce.Core.Exceptions.Base;

namespace ECommerce.Core.Exceptions;

public sealed class DuplicateCategorySlugException(string slug)
    : ConflictException($"Category with slug '{slug}' already exists") { }
