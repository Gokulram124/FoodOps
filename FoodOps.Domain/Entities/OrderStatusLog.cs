using FoodOps.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FoodOps.Domain.Entities;
public class OrderStatusLog
{
    public int Id { get; set; }
    public int OrderId { get; set; }
    public OrderStatus OldStatus { get; set; }
    public OrderStatus NewStatus { get; set; }
    public Guid ChangedBy { get; set; }
    public DateTime ChangedOn { get; set; } = DateTime.UtcNow;
    public string? Remarks { get; set; }
}