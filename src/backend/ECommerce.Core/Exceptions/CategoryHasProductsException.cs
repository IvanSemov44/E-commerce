using ECommerce.Core.Exceptions.Base;

namespace ECommerce.Core.Exceptions;

public sealed class CategoryHasProductsException(Guid categoryId)
    : BadRequestException($"Cannot delete category with ID {categoryId} because it has existing products") { }
