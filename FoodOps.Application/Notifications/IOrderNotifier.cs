namespace FoodOps.Application.Notifications;

public interface IOrderNotifier
{
    Task OrderChangedAsync(OrderChange change);
}