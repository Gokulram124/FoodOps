namespace FoodOps.Application.DelayRules;

public class DelayOptions
{
    public const string SectionName = "Delay";

    // Overall SLA: minutes since the order was placed
    public int AtRiskMinutes { get; set; } = 30;
    public int DelayedMinutes { get; set; } = 45;

    // Restaurant has not accepted the order
    public int PlacedAtRiskMinutes { get; set; } = 5;
    public int PlacedDelayedMinutes { get; set; } = 10;

    // Ready for pickup but no rider assigned
    public int NoRiderAtRiskMinutes { get; set; } = 25;
    public int NoRiderDelayedMinutes { get; set; } = 35;
}