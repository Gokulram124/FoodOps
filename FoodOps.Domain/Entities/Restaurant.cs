namespace FoodOps.Domain.Entities;

public class Restaurant
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public decimal Rating { get; set; }
    public bool IsOpen { get; set; } = true;
    public Guid OwnerId { get; set; }
    public ICollection<MenuItem> MenuItems { get; set; } = new List<MenuItem>();
}