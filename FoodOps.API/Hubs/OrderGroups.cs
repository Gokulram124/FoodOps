namespace FoodOps.API.Hubs;

public static class OrderGroups
{
    public const string Admins = "admins";
    public static string User(Guid id) => $"user-{id}";
    public static string Restaurant(int id) => $"restaurant-{id}";
    public static string Menu(int restaurantId) => $"menu-{restaurantId}";
}