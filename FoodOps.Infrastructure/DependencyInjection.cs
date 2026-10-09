using FoodOps.Application.DelayRules;
using FoodOps.Application.Interfaces;
using FoodOps.Application.Notifications;
using FoodOps.Application.Operations;
using FoodOps.Application.OrderRules;
using FoodOps.Infrastructure.DelayRules;
using FoodOps.Infrastructure.Operations;
using FoodOps.Infrastructure.Persistence;
using FoodOps.Infrastructure.Repositories;
using FoodOps.Infrastructure.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace FoodOps.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration config)
    {
        services.AddDbContext<AppDbContext>(o =>
            o.UseSqlServer(config.GetConnectionString("DefaultConnection")));

        services.AddIdentityCore<ApplicationUser>(o =>
        {
            o.User.RequireUniqueEmail = true;
            o.Password.RequireNonAlphanumeric = false;
        })
        .AddRoles<IdentityRole<Guid>>()
        .AddEntityFrameworkStores<AppDbContext>();

        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IRestaurantService, RestaurantService>();
        services.AddScoped<IMenuService, MenuService>();
        services.AddScoped<IOrderService, OrderService>();
        services.AddScoped<IDeliveryService, DeliveryService>();
        services.AddScoped<IAdminService, AdminService>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped<IStatusTransitionRule, CustomerCancelRule>();
        services.AddScoped<IStatusTransitionRule, RestaurantFlowRule>();
        services.AddSingleton<IOrderNotificationFactory, OrderNotificationFactory>();
        services.AddSingleton(TimeProvider.System);
        services.AddSingleton<IDelayRule, OverallSlaRule>();
        services.AddSingleton<IDelayRule, PlacedNotAcceptedRule>();
        services.AddSingleton<IDelayRule, NoRiderRule>();
        services.AddSingleton<IDelayAnalyzer, DelayAnalyzer>();
        services.AddSingleton<IOpsRule, RestaurantOverloadRule>();
        services.AddSingleton<IOpsRule, RiderShortageRule>();
        services.AddScoped<IOperationsMonitor, OperationsMonitor>();
        services.AddSingleton<IOpsReportStore, InMemoryOpsReportStore>();
        return services;
    }
}