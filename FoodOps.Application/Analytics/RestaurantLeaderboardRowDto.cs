namespace FoodOps.Application.Analytics;

public class RestaurantLeaderboardRowDto
{
    public int RestaurantId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public int Orders { get; set; }
    public decimal Revenue { get; set; }
    public int OverallRank { get; set; }
    public int CityRank { get; set; }
    public decimal SharePercent { get; set; }
}