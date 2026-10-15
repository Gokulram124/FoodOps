namespace FoodOps.Application.Notifications;

public record StockLevel(int MenuItemId, int? Stock);   // Stock null = unlimited

public interface IOrderNotifier
{
    Task OrderChangedAsync(OrderChange change);
    Task StockChangedAsync(int restaurantId, IReadOnlyList<StockLevel> levels);
}