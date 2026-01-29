namespace ECommerce.Core.Exceptions;

/// <summary>
/// Exception thrown when a category is not found.
/// </summary>
public sealed class CategoryNotFoundException : NotFoundException
{
    public CategoryNotFoundException(Guid categoryId)
        : base($"Category with ID '{categoryId}' was not found.")
    {
    }

    public CategoryNotFoundException(string slug)
        : base($"Category with slug '{slug}' was not found.")
    {
    }
}
