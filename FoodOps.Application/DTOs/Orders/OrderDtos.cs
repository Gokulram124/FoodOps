using FoodOps.Domain.Enums;

namespace FoodOps.Application.DTOs.Orders;

public record OrderItemRequest(int MenuItemId, int Quantity);
public record CreateOrderDto(int RestaurantId, List<OrderItemRequest> Items);
public record OrderItemDto(int MenuItemId, string Name, int Quantity, decimal Price);
public record OrderDto(int Id, int RestaurantId, string RestaurantName, OrderStatus Status,
    decimal TotalAmount, DateTime OrderTime, int? RiderId, List<OrderItemDto> Items);
public record UpdateOrderStatusDto(int OrderId, OrderStatus NewStatus, string? Remarks);
public record OrderStatusLogDto(OrderStatus OldStatus, OrderStatus NewStatus, DateTime ChangedOn, string? Remarks);