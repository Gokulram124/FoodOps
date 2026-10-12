using FoodOps.Domain.Enums;

namespace FoodOps.Application.Notifications;

public record OrderChange(int OrderId, Guid CustomerId, int RestaurantId, OrderStatus Status, DateTime At);