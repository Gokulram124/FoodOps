namespace FoodOps.Application.Analytics;

public interface IAnalyticsRepository
{
    Task<IReadOnlyList<TopItemDto>> GetTopItemsAsync(int restaurantId, int take, CancellationToken ct);
    Task<IReadOnlyList<RevenueTrendPointDto>> GetRevenueTrendAsync(
    DateTime fromUtc, DateTime toUtc, string granularity, int tzOffsetMinutes, CancellationToken ct);

    Task<IReadOnlyList<RestaurantPerformanceDto>> GetRestaurantPerformanceAsync(
    DateTime fromUtc, DateTime toUtc, CancellationToken ct);

    Task<OrderReportDto?> GetOrderReportAsync(int orderId, CancellationToken ct);

    Task<AnalyticsDashboardDto> GetDashboardAsync(DateTime fromUtc, DateTime toUtc, CancellationToken ct);

    Task<IReadOnlyList<RestaurantLeaderboardRowDto>> GetLeaderboardAsync(
    DateTime fromUtc, DateTime toUtc, CancellationToken ct);
}