namespace ECommerce.Ordering.Application.Interfaces;

// Transitional composite to keep compatibility while handlers move to focused query ports.
public interface IDbReader : IProductCatalogReader, IPromoCodeLookup, IShippingAddressReader;
