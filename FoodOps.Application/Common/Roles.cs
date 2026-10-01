namespace FoodOps.Application.Common;

public static class Roles
{
    public const string Customer = "Customer";
    public const string RestaurantOwner = "RestaurantOwner";
    public const string DeliveryPartner = "DeliveryPartner";
    public const string Admin = "Admin";

    public static readonly string[] All = { Customer, RestaurantOwner, DeliveryPartner, Admin };
}