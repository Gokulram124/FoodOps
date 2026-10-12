using FoodOps.Application.Common;
using FoodOps.Application.OrderRules;
using FoodOps.Domain.Entities;
using FoodOps.Domain.Enums;

namespace FoodOps.Tests.OrderRules;

public class StatusRuleTests
{
    private static readonly Guid Owner = Guid.NewGuid();
    private static readonly Guid Customer = Guid.NewGuid();

    private static Order MakeOrder(OrderStatus status) => new()
    {
        Status = status,
        CustomerId = Customer,
        Restaurant = new Restaurant { OwnerId = Owner }
    };

    // ---------- restaurant side ----------

    [Theory]
    [InlineData(OrderStatus.Placed, OrderStatus.Accepted)]
    [InlineData(OrderStatus.Placed, OrderStatus.Rejected)]
    [InlineData(OrderStatus.Accepted, OrderStatus.Preparing)]
    [InlineData(OrderStatus.Preparing, OrderStatus.ReadyForPickup)]
    public void Owner_can_follow_the_normal_flow(OrderStatus from, OrderStatus to)
    {
        var rule = new RestaurantFlowRule();

        var ex = Record.Exception(() => rule.Validate(MakeOrder(from), to, Owner, Roles.RestaurantOwner));

        Assert.Null(ex);
    }

    [Theory]
    [InlineData(OrderStatus.Placed, OrderStatus.Preparing)]       // skipping a step
    [InlineData(OrderStatus.Accepted, OrderStatus.Delivered)]
    [InlineData(OrderStatus.Delivered, OrderStatus.Placed)]       // going backwards
    public void Owner_cannot_skip_or_reverse_steps(OrderStatus from, OrderStatus to)
    {
        var rule = new RestaurantFlowRule();

        Assert.Throws<InvalidOperationException>(
            () => rule.Validate(MakeOrder(from), to, Owner, Roles.RestaurantOwner));
    }

    [Fact]
    public void Owner_cannot_touch_another_restaurants_order()
    {
        var rule = new RestaurantFlowRule();
        var someoneElse = Guid.NewGuid();

        Assert.Throws<ForbiddenException>(
            () => rule.Validate(MakeOrder(OrderStatus.Placed), OrderStatus.Accepted, someoneElse, Roles.RestaurantOwner));
    }

    [Fact]
    public void Admin_is_not_limited_to_own_restaurant()
    {
        var rule = new RestaurantFlowRule();

        var ex = Record.Exception(
            () => rule.Validate(MakeOrder(OrderStatus.Placed), OrderStatus.Accepted, Guid.NewGuid(), Roles.Admin));

        Assert.Null(ex);
    }

    [Fact]
    public void Rule_only_applies_to_owner_and_admin()
    {
        var rule = new RestaurantFlowRule();

        Assert.True(rule.AppliesTo(Roles.RestaurantOwner));
        Assert.True(rule.AppliesTo(Roles.Admin));
        Assert.False(rule.AppliesTo(Roles.Customer));
    }

    // ---------- customer side ----------

    [Fact]
    public void Customer_can_cancel_own_placed_order()
    {
        var rule = new CustomerCancelRule();

        var ex = Record.Exception(
            () => rule.Validate(MakeOrder(OrderStatus.Placed), OrderStatus.Cancelled, Customer, Roles.Customer));

        Assert.Null(ex);
    }

    [Theory]
    [InlineData(OrderStatus.Accepted)]
    [InlineData(OrderStatus.Preparing)]
    [InlineData(OrderStatus.Delivered)]
    public void Customer_cannot_cancel_after_the_restaurant_accepted(OrderStatus current)
    {
        var rule = new CustomerCancelRule();

        Assert.Throws<InvalidOperationException>(
            () => rule.Validate(MakeOrder(current), OrderStatus.Cancelled, Customer, Roles.Customer));
    }

    [Fact]
    public void Customer_cannot_cancel_someone_elses_order()
    {
        var rule = new CustomerCancelRule();

        Assert.Throws<ForbiddenException>(
            () => rule.Validate(MakeOrder(OrderStatus.Placed), OrderStatus.Cancelled, Guid.NewGuid(), Roles.Customer));
    }

    [Fact]
    public void Customer_cannot_set_any_status_other_than_cancelled()
    {
        var rule = new CustomerCancelRule();

        Assert.Throws<InvalidOperationException>(
            () => rule.Validate(MakeOrder(OrderStatus.Placed), OrderStatus.Delivered, Customer, Roles.Customer));
    }
}