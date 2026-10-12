using FoodOps.Application.DelayRules;
using FoodOps.Domain.Enums;
using FoodOps.Infrastructure.DelayRules;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Time.Testing;

namespace FoodOps.Tests.DelayRules;

public class DelayAnalyzerTests
{
    private readonly FakeTimeProvider _clock =
        new(new DateTimeOffset(2026, 10, 12, 12, 0, 0, TimeSpan.Zero));

    private DelayAnalyzer CreateAnalyzer() =>
        new(new IDelayRule[] { new OverallSlaRule(Options.Create(new DelayOptions())) }, _clock);

    private ActiveOrder OrderPlacedMinutesAgo(int id, int minutes) =>
        new(id, "Test Kadai", OrderStatus.Preparing, _clock.GetUtcNow().UtcDateTime.AddMinutes(-minutes), null);

    [Fact]
    public void Fresh_order_produces_no_finding()
    {
        var findings = CreateAnalyzer().Analyze(new[] { OrderPlacedMinutesAgo(1, 10) });

        Assert.Empty(findings);
    }

    [Fact]
    public void Old_order_is_reported_as_delayed_with_elapsed_minutes()
    {
        var findings = CreateAnalyzer().Analyze(new[] { OrderPlacedMinutesAgo(1, 50) });

        var f = Assert.Single(findings);
        Assert.Equal(RiskLevel.Delayed, f.Level);
        Assert.Equal(50, f.ElapsedMinutes);
    }

    [Fact]
    public void Most_severe_order_comes_first()
    {
        var orders = new[]
        {
            OrderPlacedMinutesAgo(1, 32),   // at risk
            OrderPlacedMinutesAgo(2, 60),   // delayed
        };

        var findings = CreateAnalyzer().Analyze(orders);

        Assert.Equal(new[] { 2, 1 }, findings.Select(f => f.OrderId));
    }

    [Fact]
    public void Time_moving_forward_changes_the_result()
    {
        var analyzer = CreateAnalyzer();
        var order = OrderPlacedMinutesAgo(1, 20);

        Assert.Empty(analyzer.Analyze(new[] { order }));

        _clock.Advance(TimeSpan.FromMinutes(15));   // now 35 minutes old

        Assert.Equal(RiskLevel.AtRisk, Assert.Single(analyzer.Analyze(new[] { order })).Level);
    }
}