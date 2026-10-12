using FoodOps.Application.DelayRules;
using FoodOps.Domain.Enums;
using FoodOps.Infrastructure.DelayRules;
using Microsoft.Extensions.Options;

namespace FoodOps.Tests.DelayRules;

public class OverallSlaRuleTests
{
    // defaults: at risk = 30 min, delayed = 45 min
    private static OverallSlaRule CreateRule() => new(Options.Create(new DelayOptions()));

    private static ActiveOrder AnyOrder() =>
        new(1, "Test Kadai", OrderStatus.Placed, DateTime.UtcNow, null);

    [Theory]
    [InlineData(0)]
    [InlineData(29)]
    public void Below_the_at_risk_limit_has_nothing_to_say(int elapsed)
    {
        var result = CreateRule().Evaluate(AnyOrder(), elapsed);

        Assert.Null(result);
    }

    [Theory]
    [InlineData(30, RiskLevel.AtRisk)]
    [InlineData(44, RiskLevel.AtRisk)]
    [InlineData(45, RiskLevel.Delayed)]
    [InlineData(120, RiskLevel.Delayed)]
    public void Level_follows_the_elapsed_minutes(int elapsed, RiskLevel expected)
    {
        var result = CreateRule().Evaluate(AnyOrder(), elapsed);

        Assert.NotNull(result);
        Assert.Equal(expected, result!.Level);
    }

    [Fact]
    public void Custom_thresholds_from_options_are_respected()
    {
        var rule = new OverallSlaRule(Options.Create(new DelayOptions { AtRiskMinutes = 10, DelayedMinutes = 20 }));

        Assert.Equal(RiskLevel.AtRisk, rule.Evaluate(AnyOrder(), 10)!.Level);
        Assert.Equal(RiskLevel.Delayed, rule.Evaluate(AnyOrder(), 20)!.Level);
    }
}