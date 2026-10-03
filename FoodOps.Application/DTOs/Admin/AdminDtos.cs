namespace FoodOps.Application.DTOs.Admin;

public record HourCountDto(int Hour, int Count);

public record DelayedOrderDto(int Id, string RestaurantName, string Status, int ElapsedMinutes, string Risk);

public record AdminStatsDto(
    int TotalOrders,
    int ActiveOrders,
    int DeliveredOrders,
    int CancelledOrders,
    decimal Revenue,
    double? AvgDeliveryMinutes,
    int DelayedCount,
    int AtRiskCount,
    List<HourCountDto> OrdersByHour,
    List<DelayedOrderDto> DelayedOrders);