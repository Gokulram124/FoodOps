namespace FoodOps.Application.Analytics;

public class RestaurantPerformanceDto
{
    public int RestaurantId { get; set; }
    public string Name { get; set; } = string.Empty;
    public int TotalOrders { get; set; }
    public int CancelledOrders { get; set; }        // cancelled + rejected
    public decimal CancelRatePercent { get; set; }
    public decimal Revenue { get; set; }
    public decimal AvgOrderValue { get; set; }
}