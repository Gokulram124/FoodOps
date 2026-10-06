using FoodOps.Domain.Enums;

namespace FoodOps.Application.Notifications;

public interface IOrderNotificationFactory
{
    OrderNotification? Create(int orderId, OrderStatus status);
}