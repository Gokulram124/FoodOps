using FoodOps.Domain.Enums;  

namespace FoodOps.Application.Analytics;

public class AnalyticsDashboardDto
{
    public DashboardSummaryDto Summary { get; set; } = new();
    public List<DashboardTopRestaurantDto> TopRestaurants { get; set; } = new();
    public List<DashboardStatusCountDto> StatusBreakdown { get; set; } = new();
}

public class DashboardSummaryDto
{
    public int TotalOrders { get; set; }
    public int CancelledOrders { get; set; }
    public decimal Revenue { get; set; }
    public decimal AvgOrderValue { get; set; }
}

public class DashboardTopRestaurantDto
{
    public int RestaurantId { get; set; }
    public string Name { get; set; } = string.Empty;
    public int Orders { get; set; }
    public decimal Revenue { get; set; }
}

public class DashboardStatusCountDto
{
    public OrderStatus Status { get; set; }
    public int Orders { get; set; }
}