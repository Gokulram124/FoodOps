namespace FoodOps.Application.DelayRules;

public interface IDelayAnalyzer
{
    List<DelayFinding> Analyze(IEnumerable<ActiveOrder> orders);
}