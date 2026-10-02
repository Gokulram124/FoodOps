using FoodOps.Application.DTOs.Delivery;
using FoodOps.Application.DTOs.Orders;

namespace FoodOps.Application.Interfaces;

public interface IDeliveryService
{
    Task<RiderDto> SetAvailabilityAsync(Guid userId, string fullName, SetAvailabilityDto dto);
    Task<List<OrderDto>> GetAvailableOrdersAsync();
    Task<OrderDto> AssignAsync(int orderId, Guid userId);
    Task<OrderDto> UpdateStatusAsync(UpdateDeliveryStatusDto dto, Guid userId);
    Task<List<DeliveryDto>> GetMyDeliveriesAsync(Guid userId);
}