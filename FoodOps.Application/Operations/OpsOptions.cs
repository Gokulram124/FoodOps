namespace FoodOps.Application.Operations;

public class OpsOptions
{
    public const string SectionName = "Ops";

    public int OverloadWarningOrders { get; set; } = 5;
    public int OverloadCriticalOrders { get; set; } = 8;
    public int OrdersPerFreeRider { get; set; } = 2;
    public int ScanIntervalSeconds { get; set; } = 30;
}