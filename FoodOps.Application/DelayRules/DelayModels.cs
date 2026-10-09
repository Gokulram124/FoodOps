using FoodOps.Domain.Enums;

namespace FoodOps.Application.DelayRules;

public enum RiskLevel { None = 0, AtRisk = 1, Delayed = 2 }

public record ActiveOrder(int Id, string RestaurantName, OrderStatus Status, DateTime OrderTime, int? RiderId);

public record RuleResult(RiskLevel Level, string Reason);

public record DelayFinding(
    int OrderId,
    string RestaurantName,
    OrderStatus Status,
    int ElapsedMinutes,
    RiskLevel Level,
    IReadOnlyList<string> Reasons);