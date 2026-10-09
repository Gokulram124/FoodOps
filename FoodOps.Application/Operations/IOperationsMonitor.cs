namespace FoodOps.Application.Operations;

public interface IOperationsMonitor
{
    Task<OpsReport> ScanAsync();
}