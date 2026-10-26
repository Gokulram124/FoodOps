using Asp.Versioning;
using FoodOps.Application.Analytics;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FoodOps.API.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/analytics")]
[Authorize(Roles = "Admin")]
public class AnalyticsController : ControllerBase
{
    private readonly IAnalyticsRepository _analytics;

    public AnalyticsController(IAnalyticsRepository analytics) => _analytics = analytics;

    [HttpGet("restaurants/{restaurantId:int}/top-items")]
    public async Task<ActionResult<IReadOnlyList<TopItemDto>>> TopItems(
        int restaurantId, [FromQuery] int take = 5, CancellationToken ct = default)
    {
        take = Math.Clamp(take, 1, 50);
        return Ok(await _analytics.GetTopItemsAsync(restaurantId, take, ct));
    }

    private static readonly string[] Granularities = ["day", "week", "month"];

    [HttpGet("revenue-trend")]
    public async Task<ActionResult<IReadOnlyList<RevenueTrendPointDto>>> RevenueTrend(
        [FromQuery] DateTime? from, [FromQuery] DateTime? to,
        [FromQuery] string granularity = "day",
        [FromQuery] int tzOffsetMinutes = 0,
        CancellationToken ct = default)
    {
        granularity = granularity.ToLowerInvariant();
        if (!Granularities.Contains(granularity))
            return BadRequest(new { message = "granularity must be day, week or month." });

        var toUtc = to ?? DateTime.UtcNow;
        var fromUtc = from ?? toUtc.AddDays(-30);
        if (fromUtc >= toUtc)
            return BadRequest(new { message = "'from' must be earlier than 'to'." });

        return Ok(await _analytics.GetRevenueTrendAsync(fromUtc, toUtc, granularity, tzOffsetMinutes, ct));
    }

    [HttpGet("restaurant-performance")]
    public async Task<ActionResult<IReadOnlyList<RestaurantPerformanceDto>>> RestaurantPerformance(
    [FromQuery] DateTime? from, [FromQuery] DateTime? to, CancellationToken ct = default)
    {
        var toUtc = to ?? DateTime.UtcNow;
        var fromUtc = from ?? toUtc.AddDays(-30);
        if (fromUtc >= toUtc)
            return BadRequest(new { message = "'from' must be earlier than 'to'." });

        return Ok(await _analytics.GetRestaurantPerformanceAsync(fromUtc, toUtc, ct));
    }

    [HttpGet("orders/{orderId:int}")]
    public async Task<ActionResult<OrderReportDto>> OrderReport(int orderId, CancellationToken ct)
    {
        var report = await _analytics.GetOrderReportAsync(orderId, ct);
        return report is null ? NotFound(new { message = "Order not found." }) : Ok(report);
    }

    [HttpGet("dashboard")]
    public async Task<ActionResult<AnalyticsDashboardDto>> Dashboard(
    [FromQuery] DateTime? from, [FromQuery] DateTime? to, CancellationToken ct = default)
    {
        var toUtc = to ?? DateTime.UtcNow;
        var fromUtc = from ?? toUtc.AddDays(-30);
        if (fromUtc >= toUtc)
            return BadRequest(new { message = "'from' must be earlier than 'to'." });

        return Ok(await _analytics.GetDashboardAsync(fromUtc, toUtc, ct));
    }
}