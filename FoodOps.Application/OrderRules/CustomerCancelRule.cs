using FoodOps.Application.Common;
using FoodOps.Domain.Entities;
using FoodOps.Domain.Enums;

namespace FoodOps.Application.OrderRules;

public class CustomerCancelRule : IStatusTransitionRule
{
    public bool AppliesTo(string role) => role == Roles.Customer;

    public void Validate(Order order, OrderStatus newStatus, Guid userId, string role)
    {
        if (order.CustomerId != userId)
            throw new ForbiddenException("Not your order.");
        if (newStatus != OrderStatus.Cancelled || order.Status != OrderStatus.Placed)
            throw new InvalidOperationException("You can only cancel an order that is still in Placed status.");
    }
}