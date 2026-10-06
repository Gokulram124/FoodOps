namespace FoodOps.Application.DTOs.Restaurants;

public class RestaurantQuery
{
    public string? Search { get; set; }
    public string? City { get; set; }
    public bool? IsOpen { get; set; }
    public decimal? MinRating { get; set; }
    public string? Filter { get; set; }   
    public string? Sort { get; set; }   
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 10;


}