
using Microsoft.AspNetCore.Identity;
namespace FoodOps.Infrastructure.Persistence;

public class ApplicationUser : IdentityUser<Guid>
{
    public string FullName { get; set; } = string.Empty;
}