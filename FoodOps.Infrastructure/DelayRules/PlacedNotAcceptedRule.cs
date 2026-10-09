using FoodOps.Application.DelayRules;
using FoodOps.Domain.Enums;
using Microsoft.Extensions.Options;

namespace FoodOps.Infrastructure.DelayRules;

public class PlacedNotAcceptedRule : IDelayRule
{
    private readonly DelayOptions _o;
    public PlacedNotAcceptedRule(IOptions<DelayOptions> options) => _o = options.Value;

    public RuleResult? Evaluate(ActiveOrder order, int elapsedMinutes)
    {
        if (order.Status != OrderStatus.Placed) return null;

        if (elapsedMinutes >= _o.PlacedDelayedMinutes)
            return new RuleResult(RiskLevel.Delayed, $"Restaurant has not accepted for {elapsedMinutes} min");
        if (elapsedMinutes >= _o.PlacedAtRiskMinutes)
            return new RuleResult(RiskLevel.AtRisk, $"Restaurant has not accepted for {elapsedMinutes} min");
        return null;
    }
}