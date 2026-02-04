using ECommerce.Core.Exceptions.Base;

namespace ECommerce.Core.Exceptions;

public class CategoryHasProductsException : BadRequestException
{
    public CategoryHasProductsException(Guid categoryId)
        : base($"Cannot delete category with ID {categoryId} because it has existing products") { }
}
