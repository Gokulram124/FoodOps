using FoodOps.Application.DTOs.Orders;
using FoodOps.Domain.Entities;

namespace FoodOps.Infrastructure.Services;

public static class OrderProjections
{
    public static IQueryable<OrderDto> ProjectToDto(this IQueryable<Order> q) =>
        q.Select(o => new OrderDto(
            o.Id, o.RestaurantId, o.Restaurant!.Name, o.Status, o.TotalAmount, o.OrderTime, o.RiderId,
            o.Items.Select(i => new OrderItemDto(i.MenuItemId, i.MenuItem!.Name, i.Quantity, i.Price)).ToList()));
}