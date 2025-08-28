using Elsa.Features.Abstractions;
using Elsa.Features.Attributes;
using Elsa.Features.Services;
using Elsa.Http.Features;
using Elsa.Http.OpenApi.Extensions;
using Elsa.Http.OpenApi.Options;

namespace Elsa.Http.OpenApi.Features;

/// <summary>
/// Feature for HTTP OpenAPI functionality.
/// </summary>
[DependsOn(typeof(HttpFeature))]
public class HttpOpenApiFeature : FeatureBase
{
    /// <inheritdoc />
    public HttpOpenApiFeature(IModule module) : base(module)
    {
    }

    /// <inheritdoc />
    public override void Apply()
    {
        Services.AddHttpOpenApi(options =>
        {
            options.Enabled = true;
            options.Title = "Elsa Workflow HTTP Endpoints";
            options.Version = "v1";
            options.Description = "HTTP endpoints exposed by Elsa workflows";
        });
    }
}
