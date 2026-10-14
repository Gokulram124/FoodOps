namespace FoodOps.Application.DTOs.Admin;

public record CreateOwnerDto(string FullName, string Email, string Password);

public record OwnerSummaryDto(Guid Id, string FullName, string Email, int RestaurantCount);