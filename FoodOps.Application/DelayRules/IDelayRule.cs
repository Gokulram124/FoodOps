namespace FoodOps.Application.DelayRules;

public interface IDelayRule
{
    // Returns null when this rule has nothing to say about the order
    RuleResult? Evaluate(ActiveOrder order, int elapsedMinutes);
}