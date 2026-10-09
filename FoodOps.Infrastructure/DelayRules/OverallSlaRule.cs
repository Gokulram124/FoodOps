using FoodOps.Application.DelayRules;
using Microsoft.Extensions.Options;

namespace FoodOps.Infrastructure.DelayRules;

public class OverallSlaRule : IDelayRule
{
    private readonly DelayOptions _o;
    public OverallSlaRule(IOptions<DelayOptions> options) => _o = options.Value;

    public RuleResult? Evaluate(ActiveOrder order, int elapsedMinutes)
    {
        if (elapsedMinutes >= _o.DelayedMinutes)
            return new RuleResult(RiskLevel.Delayed, $"Open for {elapsedMinutes} min (limit {_o.DelayedMinutes})");
        if (elapsedMinutes >= _o.AtRiskMinutes)
            return new RuleResult(RiskLevel.AtRisk, $"Open for {elapsedMinutes} min (limit {_o.AtRiskMinutes})");
        return null;
    }
}