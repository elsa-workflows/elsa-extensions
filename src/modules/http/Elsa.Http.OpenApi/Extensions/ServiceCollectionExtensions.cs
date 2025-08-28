using Elsa.Http.OpenApi.Contracts;
using Elsa.Http.OpenApi.Options;
using Elsa.Http.OpenApi.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Http.OpenApi.Extensions;

/// <summary>
/// Extension methods for configuring HTTP OpenAPI services.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds HTTP OpenAPI services to the service collection.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configure">Optional configuration action for HttpOpenApiOptions.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddHttpOpenApi(this IServiceCollection services, Action<HttpOpenApiOptions>? configure = null)
    {
        // Register options
        if (configure != null)
        {
            services.Configure(configure);
        }
        else
        {
            services.Configure<HttpOpenApiOptions>(options => { });
        }

        // Register services
        services.AddScoped<IWorkflowEndpointExtractor, WorkflowEndpointExtractor>();
        services.AddScoped<IOpenApiGenerator, OpenApiGenerator>();

        return services;
    }
}
