using FoodOps.Domain.Entities;
using FoodOps.Domain.Enums;

namespace FoodOps.Application.OrderRules;

public interface IStatusTransitionRule
{
    bool AppliesTo(string role);
    void Validate(Order order, OrderStatus newStatus, Guid userId, string role);
}