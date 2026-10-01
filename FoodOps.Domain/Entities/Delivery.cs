using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FoodOps.Domain.Entities;
public class Delivery
{
    public int Id { get; set; }
    public int OrderId { get; set; }
    public int RiderId { get; set; }
    public DateTime? PickupTime { get; set; }
    public DateTime? DeliveredTime { get; set; }
}
