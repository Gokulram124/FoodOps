using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace FoodOps.API.Filters;

// Hides the auto-generated X-Api-Version header box in Swagger (URL versioning is enough there)
public class RemoveVersionHeaderFilter : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        var p = operation.Parameters?
            .FirstOrDefault(x => x.Name == "X-Api-Version" && x.In == ParameterLocation.Header);
        if (p is not null) operation.Parameters!.Remove(p);
    }
}