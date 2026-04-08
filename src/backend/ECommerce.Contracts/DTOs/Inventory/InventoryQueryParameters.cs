using ECommerce.Contracts.DTOs.Common;

namespace ECommerce.Contracts.DTOs.Inventory;

/// <summary>
/// Query parameters for the admin inventory listing endpoint.
/// Inherits page, pageSize, search from RequestParameters.
/// </summary>
public class InventoryQueryParameters : RequestParameters
{
    public bool? LowStockOnly { get; set; }
}

