using Dapper;
using FoodOps.Application.Abstractions;
using FoodOps.Application.Analytics;
using FoodOps.Domain.Enums;
using System.Data;

namespace FoodOps.Infrastructure.Analytics;

public class AnalyticsRepository : IAnalyticsRepository
{
    private readonly IDbConnectionFactory _factory;

    public AnalyticsRepository(IDbConnectionFactory factory) => _factory = factory;

    public async Task<IReadOnlyList<TopItemDto>> GetTopItemsAsync(int restaurantId, int take, CancellationToken ct)
    {
        const string sql = """
        SELECT TOP (@Take)
               mi.Id                           AS MenuItemId,
               mi.Name                         AS Name,
               SUM(oi.Quantity)                AS TotalQuantity,
               SUM(oi.Quantity * oi.Price)     AS Revenue
        FROM OrderItems oi
        INNER JOIN Orders    o  ON o.Id  = oi.OrderId
        INNER JOIN MenuItems mi ON mi.Id = oi.MenuItemId
        WHERE o.RestaurantId = @RestaurantId
          AND o.Status NOT IN @Excluded
        GROUP BY mi.Id, mi.Name
        ORDER BY TotalQuantity DESC;
        """;

        var excluded = new[] { (int)OrderStatus.Cancelled, (int)OrderStatus.Rejected };

        using var connection = _factory.CreateConnection();

        var rows = await connection.QueryAsync<TopItemDto>(
            new CommandDefinition(
                sql,
                new { RestaurantId = restaurantId, Take = take, Excluded = excluded },
                cancellationToken: ct));

        return rows.ToList();
    }

    public async Task<IReadOnlyList<RevenueTrendPointDto>> GetRevenueTrendAsync(
    DateTime fromUtc, DateTime toUtc, string granularity, int tzOffsetMinutes, CancellationToken ct)
    {
        using var connection = _factory.CreateConnection();

        var rows = await connection.QueryAsync<RevenueTrendPointDto>(
            new CommandDefinition(
                "dbo.usp_GetRevenueTrend",
                new
                {
                    FromUtc = fromUtc,
                    ToUtc = toUtc,
                    Granularity = granularity,
                    TzOffsetMinutes = tzOffsetMinutes,
                    Cancelled = (int)OrderStatus.Cancelled,
                    Rejected = (int)OrderStatus.Rejected
                },
                commandType: CommandType.StoredProcedure,
                cancellationToken: ct));

        return rows.ToList();
    }

    public async Task<IReadOnlyList<RestaurantPerformanceDto>> GetRestaurantPerformanceAsync(
    DateTime fromUtc, DateTime toUtc, CancellationToken ct)
    {
        const string sql = """
        SELECT r.Id   AS RestaurantId,
               r.Name AS Name,
               COUNT(o.Id) AS TotalOrders,
               COALESCE(SUM(CASE WHEN o.Status IN (@Cancelled, @Rejected) THEN 1 ELSE 0 END), 0)
                   AS CancelledOrders,
               COALESCE(CAST(100.0 *
                   SUM(CASE WHEN o.Status IN (@Cancelled, @Rejected) THEN 1 ELSE 0 END)
                   / NULLIF(COUNT(o.Id), 0) AS decimal(5,2)), 0)
                   AS CancelRatePercent,
               COALESCE(SUM(CASE WHEN o.Status NOT IN (@Cancelled, @Rejected)
                                 THEN o.TotalAmount END), 0) AS Revenue,
               COALESCE(AVG(CASE WHEN o.Status NOT IN (@Cancelled, @Rejected)
                                 THEN o.TotalAmount END), 0) AS AvgOrderValue
        FROM Restaurants r
        LEFT JOIN Orders o
               ON o.RestaurantId = r.Id
              AND o.OrderTime >= @FromUtc
              AND o.OrderTime <  @ToUtc
        GROUP BY r.Id, r.Name
        ORDER BY Revenue DESC;
        """;

        using var connection = _factory.CreateConnection();

        var rows = await connection.QueryAsync<RestaurantPerformanceDto>(
            new CommandDefinition(
                sql,
                new
                {
                    FromUtc = fromUtc,
                    ToUtc = toUtc,
                    Cancelled = (int)OrderStatus.Cancelled,
                    Rejected = (int)OrderStatus.Rejected
                },
                cancellationToken: ct));

        return rows.ToList();
    }

    public async Task<OrderReportDto?> GetOrderReportAsync(int orderId, CancellationToken ct)
    {
        const string sql = """
        SELECT o.Id, r.Name AS RestaurantName, o.Status, o.OrderTime,
               o.TotalAmount, o.DiscountAmount, o.CouponCode,
               oi.Id AS LineId, mi.Id AS MenuItemId, mi.Name AS ItemName,
               oi.Quantity, oi.Price
        FROM Orders o
        INNER JOIN Restaurants r ON r.Id  = o.RestaurantId
        INNER JOIN OrderItems oi ON oi.OrderId = o.Id
        INNER JOIN MenuItems mi  ON mi.Id = oi.MenuItemId
        WHERE o.Id = @OrderId
        ORDER BY oi.Id;
        """;

        ct.ThrowIfCancellationRequested();
        using var connection = _factory.CreateConnection();

        var orders = new Dictionary<int, OrderReportDto>();

        await connection.QueryAsync<OrderReportDto, OrderReportLineDto, OrderReportDto>(
            sql,
            (order, line) =>
            {
                if (!orders.TryGetValue(order.Id, out var existing))
                {
                    existing = order;
                    orders.Add(order.Id, existing);
                }
                existing.Items.Add(line);
                return existing;
            },
            new { OrderId = orderId },
            splitOn: "LineId");

        return orders.Values.FirstOrDefault();
    }

    public async Task<AnalyticsDashboardDto> GetDashboardAsync(
    DateTime fromUtc, DateTime toUtc, CancellationToken ct)
    {
        const string sql = """
        -- 1) Summary
        SELECT COUNT(*) AS TotalOrders,
               COALESCE(SUM(CASE WHEN Status IN (@Cancelled, @Rejected) THEN 1 ELSE 0 END), 0)
                   AS CancelledOrders,
               COALESCE(SUM(CASE WHEN Status NOT IN (@Cancelled, @Rejected) THEN TotalAmount END), 0)
                   AS Revenue,
               COALESCE(AVG(CASE WHEN Status NOT IN (@Cancelled, @Rejected) THEN TotalAmount END), 0)
                   AS AvgOrderValue
        FROM Orders
        WHERE OrderTime >= @FromUtc AND OrderTime < @ToUtc;

        -- 2) Top 3 restaurants by revenue
        SELECT TOP (3)
               r.Id AS RestaurantId, r.Name AS Name,
               COUNT(*) AS Orders, SUM(o.TotalAmount) AS Revenue
        FROM Orders o
        INNER JOIN Restaurants r ON r.Id = o.RestaurantId
        WHERE o.OrderTime >= @FromUtc AND o.OrderTime < @ToUtc
          AND o.Status NOT IN (@Cancelled, @Rejected)
        GROUP BY r.Id, r.Name
        ORDER BY Revenue DESC;

        -- 3) Orders per status
        SELECT Status, COUNT(*) AS Orders
        FROM Orders
        WHERE OrderTime >= @FromUtc AND OrderTime < @ToUtc
        GROUP BY Status
        ORDER BY Status;
        """;

        using var connection = _factory.CreateConnection();

        using var grid = await connection.QueryMultipleAsync(
            new CommandDefinition(
                sql,
                new
                {
                    FromUtc = fromUtc,
                    ToUtc = toUtc,
                    Cancelled = (int)OrderStatus.Cancelled,
                    Rejected = (int)OrderStatus.Rejected
                },
                cancellationToken: ct));

        // Read in the SAME order as the SELECT statements above.
        var summary = await grid.ReadSingleAsync<DashboardSummaryDto>();
        var top = (await grid.ReadAsync<DashboardTopRestaurantDto>()).ToList();
        var statuses = (await grid.ReadAsync<DashboardStatusCountDto>()).ToList();

        return new AnalyticsDashboardDto
        {
            Summary = summary,
            TopRestaurants = top,
            StatusBreakdown = statuses
        };
    }

    public async Task<IReadOnlyList<RestaurantLeaderboardRowDto>> GetLeaderboardAsync(
    DateTime fromUtc, DateTime toUtc, CancellationToken ct)
    {
        const string sql = """
        WITH Agg AS (
            SELECT r.Id AS RestaurantId, r.Name AS Name, r.City AS City,
                   COUNT(o.Id) AS Orders,
                   COALESCE(SUM(o.TotalAmount), 0) AS Revenue
            FROM Restaurants r
            LEFT JOIN Orders o
                   ON o.RestaurantId = r.Id
                  AND o.OrderTime >= @FromUtc
                  AND o.OrderTime <  @ToUtc
                  AND o.Status NOT IN (@Cancelled, @Rejected)
            GROUP BY r.Id, r.Name, r.City
        )
        SELECT RestaurantId, Name, City, Orders, Revenue,
               CAST(RANK() OVER (ORDER BY Revenue DESC) AS int)                    AS OverallRank,
               CAST(RANK() OVER (PARTITION BY City ORDER BY Revenue DESC) AS int)  AS CityRank,
               CAST(CASE WHEN SUM(Revenue) OVER () = 0 THEN 0
                         ELSE 100.0 * Revenue / SUM(Revenue) OVER () END
                    AS decimal(5,2))                                               AS SharePercent
        FROM Agg
        ORDER BY OverallRank, Name;
        """;

        using var connection = _factory.CreateConnection();

        var rows = await connection.QueryAsync<RestaurantLeaderboardRowDto>(
            new CommandDefinition(
                sql,
                new
                {
                    FromUtc = fromUtc,
                    ToUtc = toUtc,
                    Cancelled = (int)OrderStatus.Cancelled,
                    Rejected = (int)OrderStatus.Rejected
                },
                cancellationToken: ct));

        return rows.ToList();
    }
}