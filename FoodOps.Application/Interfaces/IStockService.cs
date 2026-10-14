namespace FoodOps.Application.Interfaces;

public interface IStockService
{
    /// Atomically takes stock for every item. Throws if any item has too little.
    /// Call inside a transaction so a failure rolls back the items already taken.
    Task ReserveAsync(IReadOnlyDictionary<int, int> quantities);
}