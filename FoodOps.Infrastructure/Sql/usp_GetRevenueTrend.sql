CREATE OR ALTER PROCEDURE dbo.usp_GetRevenueTrend
    @FromUtc         datetime2,
    @ToUtc           datetime2,
    @Granularity     varchar(10),
    @TzOffsetMinutes int,
    @Cancelled       int,
    @Rejected        int
AS
BEGIN
    SET NOCOUNT ON;

    IF @Granularity NOT IN ('day', 'week', 'month')
        THROW 50001, 'Granularity must be day, week or month.', 1;

    WITH Local AS (
        SELECT CAST(DATEADD(MINUTE, @TzOffsetMinutes, o.OrderTime) AS date) AS LocalDate,
               o.TotalAmount,
               o.DiscountAmount
        FROM Orders o
        WHERE o.OrderTime >= @FromUtc
          AND o.OrderTime <  @ToUtc
          AND o.Status NOT IN (@Cancelled, @Rejected)
    ),
    Bucketed AS (
        SELECT CASE @Granularity
                   WHEN 'day'   THEN LocalDate
                   WHEN 'month' THEN DATEFROMPARTS(YEAR(LocalDate), MONTH(LocalDate), 1)
                   WHEN 'week'  THEN DATEADD(DAY, -(DATEDIFF(DAY, '19000101', LocalDate) % 7), LocalDate)
               END AS Period,
               TotalAmount,
               DiscountAmount
        FROM Local
    )
    SELECT Period,
           COUNT(*)                          AS OrderCount,
           COALESCE(SUM(TotalAmount), 0)     AS Revenue,
           COALESCE(SUM(DiscountAmount), 0)  AS Discounts
    FROM Bucketed
    GROUP BY Period
    ORDER BY Period;
END