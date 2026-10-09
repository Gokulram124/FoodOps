using FoodOps.Application.DelayRules;
using FoodOps.Domain.Enums;
using Microsoft.Extensions.Options;

namespace FoodOps.Infrastructure.DelayRules;

public class NoRiderRule : IDelayRule
{
    private readonly DelayOptions _o;
    public NoRiderRule(IOptions<DelayOptions> options) => _o = options.Value;

    public RuleResult? Evaluate(ActiveOrder order, int elapsedMinutes)
    {
        if (order.Status != OrderStatus.ReadyForPickup || order.RiderId != null) return null;

        if (elapsedMinutes >= _o.NoRiderDelayedMinutes)
            return new RuleResult(RiskLevel.Delayed, $"Ready but no rider assigned after {elapsedMinutes} min");
        if (elapsedMinutes >= _o.NoRiderAtRiskMinutes)
            return new RuleResult(RiskLevel.AtRisk, $"Ready but no rider assigned after {elapsedMinutes} min");
        return null;
    }
}