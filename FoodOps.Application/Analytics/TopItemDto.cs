namespace FoodOps.Application.Analytics;

public class TopItemDto
{
    public int MenuItemId { get; set; }
    public string Name { get; set; } = string.Empty;
    public int TotalQuantity { get; set; }
    public decimal Revenue { get; set; }
}