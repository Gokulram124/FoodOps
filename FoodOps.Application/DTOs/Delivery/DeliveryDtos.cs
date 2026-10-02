using FoodOps.Domain.Enums;

namespace FoodOps.Application.DTOs.Delivery;

public record SetAvailabilityDto(bool IsOnline, string? Phone);
public record RiderDto(int Id, string Name, string Phone, bool IsOnline);
public record UpdateDeliveryStatusDto(int OrderId, OrderStatus NewStatus);
public record DeliveryDto(int Id, int OrderId, int RiderId, DateTime? PickupTime,
    DateTime? DeliveredTime, OrderStatus OrderStatus);