namespace FoodOps.Application.DTOs.Orders;

public record ValidateCouponDto(int RestaurantId, string Code, List<OrderItemRequest> Items);