namespace FoodOps.Application.Operations;

public interface IOpsReportStore
{
    OpsReport? Latest { get; }
    void Save(OpsReport report);
}