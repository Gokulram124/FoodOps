using FoodOps.Domain.Enums;   

namespace FoodOps.Application.Analytics;

public class OrderReportDto
{
    public int Id { get; set; }
    public string RestaurantName { get; set; } = string.Empty;
    public OrderStatus Status { get; set; }
    public DateTime OrderTime { get; set; }
    public decimal TotalAmount { get; set; }
    public decimal DiscountAmount { get; set; }
    public string? CouponCode { get; set; }
    public List<OrderReportLineDto> Items { get; set; } = new();
}

public class OrderReportLineDto
{
    public int LineId { get; set; }
    public int MenuItemId { get; set; }
    public string ItemName { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public decimal Price { get; set; }
}