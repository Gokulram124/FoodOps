using FoodOps.Application.Common;
using FoodOps.Domain.Entities;
using FoodOps.Domain.Enums;

namespace FoodOps.Application.OrderRules;

public class RestaurantFlowRule : IStatusTransitionRule
{
    private static readonly Dictionary<OrderStatus, OrderStatus[]> Flow = new()
    {
        [OrderStatus.Placed] = new[] { OrderStatus.Accepted, OrderStatus.Rejected },
        [OrderStatus.Accepted] = new[] { OrderStatus.Preparing },
        [OrderStatus.Preparing] = new[] { OrderStatus.ReadyForPickup }
    };

    public bool AppliesTo(string role) => role == Roles.RestaurantOwner || role == Roles.Admin;

    public void Validate(Order order, OrderStatus newStatus, Guid userId, string role)
    {
        if (role == Roles.RestaurantOwner && order.Restaurant!.OwnerId != userId)
            throw new ForbiddenException("Not your restaurant's order.");

        if (!Flow.TryGetValue(order.Status, out var allowed) || !allowed.Contains(newStatus))
            throw new InvalidOperationException($"Cannot move order from {order.Status} to {newStatus}.");
    }
}