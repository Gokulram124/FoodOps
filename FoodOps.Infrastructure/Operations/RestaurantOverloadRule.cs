using FoodOps.Application.Operations;
using Microsoft.Extensions.Options;

namespace FoodOps.Infrastructure.Operations;

public class RestaurantOverloadRule : IOpsRule
{
    private readonly OpsOptions _o;
    public RestaurantOverloadRule(IOptions<OpsOptions> options) => _o = options.Value;

    public IEnumerable<OpsAlert> Evaluate(OpsSnapshot snapshot)
    {
        foreach (var r in snapshot.Restaurants)
        {
            if (r.KitchenOrders >= _o.OverloadCriticalOrders)
                yield return new OpsAlert("RestaurantOverload", AlertSeverity.Critical, r.RestaurantName,
                    $"{r.KitchenOrders} orders in the kitchen (critical at {_o.OverloadCriticalOrders})");
            else if (r.KitchenOrders >= _o.OverloadWarningOrders)
                yield return new OpsAlert("RestaurantOverload", AlertSeverity.Warning, r.RestaurantName,
                    $"{r.KitchenOrders} orders in the kitchen (warning at {_o.OverloadWarningOrders})");
        }
    }
}