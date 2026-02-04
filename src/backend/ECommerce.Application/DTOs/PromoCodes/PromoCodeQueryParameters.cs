using ECommerce.Application.DTOs.Common;

namespace ECommerce.Application.DTOs.PromoCodes;

/// <summary>
/// Query parameters for the admin promo code listing endpoint.
/// Inherits page, pageSize, search from RequestParameters.
/// </summary>
public class PromoCodeQueryParameters : RequestParameters
{
    public bool? IsActive { get; set; }
}
