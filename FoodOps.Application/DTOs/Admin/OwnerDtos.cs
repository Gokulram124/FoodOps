namespace FoodOps.Application.DTOs.Admin;

public record CreateOwnerDto(string FullName, string Email, string Password);

public record OwnerSummaryDto(Guid Id, string FullName, string Email, int RestaurantCount);
public record AdminCreateRestaurantDto(string Name, string City, Guid OwnerId);

public record AdminRestaurantDto(
    int Id,
    string Name,
    string City,
    bool IsOpen,
    Guid OwnerId,
    string OwnerName,
    int OrdersToday,
    decimal RevenueToday,
    int ActiveNow);