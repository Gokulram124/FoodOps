namespace FoodOps.Application.Analytics;

public class RevenueTrendPointDto
{
    public DateTime Period { get; set; }
    public int OrderCount { get; set; }
    public decimal Revenue { get; set; }
    public decimal Discounts { get; set; }
}