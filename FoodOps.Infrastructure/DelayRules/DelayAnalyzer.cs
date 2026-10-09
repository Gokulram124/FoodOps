using FoodOps.Application.DelayRules;

namespace FoodOps.Infrastructure.DelayRules;

public class DelayAnalyzer : IDelayAnalyzer
{
    private readonly IEnumerable<IDelayRule> _rules;
    private readonly TimeProvider _clock;

    public DelayAnalyzer(IEnumerable<IDelayRule> rules, TimeProvider clock)
    {
        _rules = rules;
        _clock = clock;
    }

    public List<DelayFinding> Analyze(IEnumerable<ActiveOrder> orders)
    {
        var now = _clock.GetUtcNow().UtcDateTime;
        var findings = new List<DelayFinding>();

        foreach (var order in orders)
        {
            var elapsed = (int)(now - order.OrderTime).TotalMinutes;

            var results = _rules
                .Select(r => r.Evaluate(order, elapsed))
                .Where(r => r != null)
                .Select(r => r!)
                .ToList();
            if (results.Count == 0) continue;

            findings.Add(new DelayFinding(
                order.Id, order.RestaurantName, order.Status, elapsed,
                results.Max(r => r.Level),
                results.Select(r => r.Reason).ToList()));
        }

        return findings
            .OrderByDescending(f => f.Level)
            .ThenByDescending(f => f.ElapsedMinutes)
            .ToList();
    }
}