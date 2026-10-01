using FoodOps.Application.Common;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using FoodOps.Domain.Entities;

namespace FoodOps.Infrastructure.Persistence;

public static class DbSeeder
{
    public static async Task SeedAsync(IServiceProvider sp)
    {
        using var scope = sp.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await db.Database.MigrateAsync();

        var roleMgr = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole<Guid>>>();
        foreach (var role in Roles.All)
            if (!await roleMgr.RoleExistsAsync(role))
                await roleMgr.CreateAsync(new IdentityRole<Guid>(role));

        var userMgr = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        const string adminEmail = "admin@foodops.com";
        if (await userMgr.FindByEmailAsync(adminEmail) == null)
        {
            var admin = new ApplicationUser
            {
                Id = Guid.NewGuid(),
                UserName = adminEmail,
                Email = adminEmail,
                FullName = "System Admin",
                EmailConfirmed = true
            };
            var res = await userMgr.CreateAsync(admin, "Admin@123");
            if (res.Succeeded) await userMgr.AddToRoleAsync(admin, Roles.Admin);
        }
        const string ownerEmail = "owner@foodops.com";
        var owner = await userMgr.FindByEmailAsync(ownerEmail);
        if (owner == null)
        {
            owner = new ApplicationUser
            {
                Id = Guid.NewGuid(),
                UserName = ownerEmail,
                Email = ownerEmail,
                FullName = "Demo Owner",
                EmailConfirmed = true
            };
            var res = await userMgr.CreateAsync(owner, "Owner@123");
            if (res.Succeeded) await userMgr.AddToRoleAsync(owner, Roles.RestaurantOwner);
        }

        if (!await db.Restaurants.AnyAsync())
        {
            db.Restaurants.AddRange(
                new Restaurant
                {
                    Name = "Chennai Biryani House",
                    City = "Chennai",
                    Rating = 4.4m,
                    OwnerId = owner.Id,
                    MenuItems =
                    {
                new MenuItem { Name = "Chicken Biryani", Price = 220, Category = "Biryani" },
                new MenuItem { Name = "Mutton Biryani", Price = 320, Category = "Biryani" },
                new MenuItem { Name = "Chicken 65", Price = 180, Category = "Starters" }
                    }
                },
                new Restaurant
                {
                    Name = "Saravana Tiffins",
                    City = "Chennai",
                    Rating = 4.2m,
                    OwnerId = owner.Id,
                    MenuItems =
                    {
                new MenuItem { Name = "Masala Dosa", Price = 90, Category = "Tiffin" },
                new MenuItem { Name = "Idli (3 pcs)", Price = 60, Category = "Tiffin" },
                new MenuItem { Name = "Filter Coffee", Price = 40, Category = "Beverages" }
                    }
                },
                new Restaurant
                {
                    Name = "Pizza Corner",
                    City = "Coimbatore",
                    Rating = 4.0m,
                    OwnerId = owner.Id,
                    MenuItems =
                    {
                new MenuItem { Name = "Margherita Pizza", Price = 250, Category = "Pizza" },
                new MenuItem { Name = "Veg Garlic Bread", Price = 120, Category = "Sides" }
                    }
                });
            await db.SaveChangesAsync();
        }
    }
}