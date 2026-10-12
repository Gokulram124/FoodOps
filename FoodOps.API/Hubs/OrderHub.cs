using FoodOps.API.Extensions;
using FoodOps.Application.Common;
using FoodOps.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace FoodOps.API.Hubs;

[Authorize]
public class OrderHub : Hub
{
    private readonly IRestaurantService _restaurants;
    public OrderHub(IRestaurantService restaurants) => _restaurants = restaurants;

    public override async Task OnConnectedAsync()
    {
        var user = Context.User!;
        var userId = user.GetUserId();

        // everyone listens to their own orders
        await Groups.AddToGroupAsync(Context.ConnectionId, OrderGroups.User(userId));

        if (user.IsInRole(Roles.Admin))
            await Groups.AddToGroupAsync(Context.ConnectionId, OrderGroups.Admins);

        if (user.IsInRole(Roles.RestaurantOwner))
            foreach (var r in await _restaurants.GetMineAsync(userId))
                await Groups.AddToGroupAsync(Context.ConnectionId, OrderGroups.Restaurant(r.Id));

        await base.OnConnectedAsync();
    }
}