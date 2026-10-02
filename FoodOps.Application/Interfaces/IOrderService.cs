using FoodOps.Application.DTOs.Orders;
using FoodOps.Domain.Enums;

namespace FoodOps.Application.Interfaces;

public interface IOrderService
{
    Task<OrderDto> PlaceOrderAsync(CreateOrderDto dto, Guid customerId);
    Task<List<OrderDto>> GetMyOrdersAsync(Guid customerId);
    Task<List<OrderDto>> GetRestaurantOrdersAsync(Guid userId, bool isAdmin, OrderStatus? status);
    Task<OrderDto> UpdateStatusAsync(UpdateOrderStatusDto dto, Guid userId, string role);
    Task<List<OrderStatusLogDto>> GetTimelineAsync(int orderId, Guid userId, string role);
}