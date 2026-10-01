using System;
using System.Collections.Generic;
using FoodOps.Domain.Enums;


namespace FoodOps.Domain.Entities;

public class Order
{
    public int Id { get; set; }
    public Guid CustomerId { get; set; }
    public int RestaurantId { get; set; }
    public Restaurant? Restaurant { get; set; }
    public int? RiderId { get; set; }
    public Rider? Rider { get; set; }
    public OrderStatus Status { get; set; } = OrderStatus.Placed;
    public decimal TotalAmount { get; set; }
    public DateTime OrderTime { get; set; } = DateTime.UtcNow;
    public ICollection<OrderItem> Items { get; set; } = new List<OrderItem>();
}
    