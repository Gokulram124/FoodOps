using FoodOps.Application.Notifications;
using Microsoft.AspNetCore.SignalR;

namespace FoodOps.API.Hubs;

public class SignalROrderNotifier : IOrderNotifier
{
    private readonly IHubContext<OrderHub> _hub;
    public SignalROrderNotifier(IHubContext<OrderHub> hub) => _hub = hub;

    public Task OrderChangedAsync(OrderChange c) =>
        _hub.Clients
            .Groups(OrderGroups.User(c.CustomerId), OrderGroups.Restaurant(c.RestaurantId), OrderGroups.Admins)
            .SendAsync("orderChanged", new
            {
                orderId = c.OrderId,
                restaurantId = c.RestaurantId,
                status = c.Status.ToString(),
                at = c.At
            });
}