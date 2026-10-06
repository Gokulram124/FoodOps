using FoodOps.Domain.Enums;

namespace FoodOps.Application.Notifications;

public class OrderNotificationFactory : IOrderNotificationFactory
{
    public OrderNotification? Create(int orderId, OrderStatus status) => status switch
    {
        OrderStatus.Placed => new OrderNotification($"Order #{orderId} placed", "We have received your order."),
        OrderStatus.Accepted => new OrderNotification($"Order #{orderId} accepted", "The restaurant accepted your order."),
        OrderStatus.Preparing => new OrderNotification($"Order #{orderId} is being prepared", "Your food is being cooked."),
        OrderStatus.ReadyForPickup => new OrderNotification($"Order #{orderId} is ready", "Waiting for a rider to pick it up."),
        OrderStatus.PickedUp => new OrderNotification($"Order #{orderId} is on the way", "Your rider has picked up the order."),
        OrderStatus.Delivered => new OrderNotification($"Order #{orderId} delivered", "Enjoy your meal!"),
        OrderStatus.Rejected => new OrderNotification($"Order #{orderId} rejected", "Sorry, the restaurant could not accept your order."),
        OrderStatus.Cancelled => new OrderNotification($"Order #{orderId} cancelled", "Your order has been cancelled."),
        _ => null
    };
}