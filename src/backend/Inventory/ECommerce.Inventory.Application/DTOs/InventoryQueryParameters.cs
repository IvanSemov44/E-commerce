using ECommerce.Contracts.DTOs.Common;

namespace ECommerce.Inventory.Application.DTOs;

public class InventoryQueryParameters : RequestParameters
{
    public bool? LowStockOnly { get; set; }
}
