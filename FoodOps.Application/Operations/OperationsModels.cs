using FoodOps.Application.DelayRules;

namespace FoodOps.Application.Operations;

public enum AlertSeverity { Warning = 1, Critical = 2 }

public record OpsAlert(string Type, AlertSeverity Severity, string Subject, string Message);

public record RestaurantLoad(int RestaurantId, string RestaurantName, int KitchenOrders);

public record OpsSnapshot(
    IReadOnlyList<RestaurantLoad> Restaurants,
    int WaitingForRider,
    int UpcomingPickups,
    int OnlineRiders,
    int BusyRiders)
{
    public int AvailableRiders => Math.Max(0, OnlineRiders - BusyRiders);
}

public record OpsReport(
    DateTime ScannedAtUtc,
    IReadOnlyList<DelayFinding> Delays,
    IReadOnlyList<OpsAlert> Alerts);