using Asp.Versioning.ApiExplorer;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace FoodOps.API;

// Creates one Swagger document per API version (v1, v2, ...)
public class ConfigureSwaggerOptions : IConfigureOptions<SwaggerGenOptions>
{
    private readonly IApiVersionDescriptionProvider _provider;
    public ConfigureSwaggerOptions(IApiVersionDescriptionProvider provider) => _provider = provider;

    public void Configure(SwaggerGenOptions options)
    {
        foreach (var d in _provider.ApiVersionDescriptions)
        {
            options.SwaggerDoc(d.GroupName, new OpenApiInfo
            {
                Title = "FoodOps API",
                Version = d.ApiVersion.ToString(),
                Description = d.IsDeprecated ? "This API version is deprecated." : null
            });
        }
    }
}