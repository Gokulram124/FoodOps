namespace FoodOps.Application.Interfaces;

public interface IStockService
{
    /// Atomically takes stock for every item. Throws if any item has too little. Call inside a transaction.
    Task ReserveAsync(IReadOnlyDictionary<int, int> quantities);

    /// Gives a cancelled/rejected order's quantities back. Returns the item ids it touched.
    Task<IReadOnlyList<int>> ReleaseAsync(int orderId);

    /// Pushes the CURRENT stock of these items to everyone viewing the restaurant's menu. Never throws.
    Task PublishAsync(int restaurantId, IEnumerable<int> menuItemIds);
}