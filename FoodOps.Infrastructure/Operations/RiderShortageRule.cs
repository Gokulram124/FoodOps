using FoodOps.Application.Operations;
using Microsoft.Extensions.Options;

namespace FoodOps.Infrastructure.Operations;

public class RiderShortageRule : IOpsRule
{
    private readonly OpsOptions _o;
    public RiderShortageRule(IOptions<OpsOptions> options) => _o = options.Value;

    public IEnumerable<OpsAlert> Evaluate(OpsSnapshot s)
    {
        var free = s.AvailableRiders;
        var demand = s.WaitingForRider + s.UpcomingPickups;
        if (demand == 0) yield break;

        if (s.WaitingForRider > 0 && free == 0)
            yield return new OpsAlert("RiderShortage", AlertSeverity.Critical, "Riders",
                $"{s.WaitingForRider} order(s) waiting for a rider and no rider is free ({s.OnlineRiders} online)");
        else if (demand > free * _o.OrdersPerFreeRider)
            yield return new OpsAlert("RiderShortage", AlertSeverity.Warning, "Riders",
                $"{demand} order(s) need a rider soon but only {free} rider(s) free ({s.OnlineRiders} online)");
    }
}