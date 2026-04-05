namespace ECommerce.Shopping.Application.Interfaces;

// Transitional composite to preserve compatibility while handlers move to focused query ports.
public interface IShoppingDbReader : IShoppingProductReader, IStockAvailabilityReader;
