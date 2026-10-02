using System.Security.Claims;
using FoodOps.Application.Common;

namespace FoodOps.API.Extensions;

public static class ClaimsExtensions
{
    public static Guid GetUserId(this ClaimsPrincipal user) =>
        Guid.Parse(user.FindFirstValue(ClaimTypes.NameIdentifier)!);

    public static bool IsAdmin(this ClaimsPrincipal user) => user.IsInRole(Roles.Admin);
    public static string GetRole(this ClaimsPrincipal user) => user.FindFirstValue(ClaimTypes.Role)!;
    public static string GetFullName(this ClaimsPrincipal user) => user.FindFirstValue(ClaimTypes.Name) ?? "Rider";
}