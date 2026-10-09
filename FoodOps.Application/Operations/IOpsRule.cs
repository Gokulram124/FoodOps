namespace FoodOps.Application.Operations;

public interface IOpsRule
{
    IEnumerable<OpsAlert> Evaluate(OpsSnapshot snapshot);
}