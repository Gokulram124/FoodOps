using FoodOps.Application.Operations;

namespace FoodOps.Infrastructure.Operations;

public class InMemoryOpsReportStore : IOpsReportStore
{
    private volatile OpsReport? _latest;

    public OpsReport? Latest => _latest;
    public void Save(OpsReport report) => _latest = report;
}