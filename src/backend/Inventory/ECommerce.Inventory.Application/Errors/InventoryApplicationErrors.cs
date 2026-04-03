using ECommerce.SharedKernel.Results;

namespace ECommerce.Inventory.Application.Errors;

public static class InventoryApplicationErrors
{
    public static readonly DomainError InventoryItemNotFound = new("INVENTORY_ITEM_NOT_FOUND", "Inventory item not found for this product.");
}