using Elsa.Http.OpenApi.Contracts;
using Elsa.Http.OpenApi.Models;
using System.Text.Json;

namespace Elsa.Http.OpenApi.Services;

/// <summary>
/// Service for generating OpenAPI JSON documentation from workflow endpoints.
/// </summary>
public class OpenApiGenerator : IOpenApiGenerator
{
    /// <summary>
    /// Generates OpenAPI JSON documentation from a list of endpoint definitions.
    /// </summary>
    /// <param name="endpoints">The list of endpoint definitions.</param>
    /// <returns>OpenAPI JSON string.</returns>
    public string GenerateOpenApiJson(List<EndpointDefinition> endpoints)
    {
        var openApiDoc = new
        {
            openapi = "3.0.0",
            info = new
            {
                title = "Elsa Workflow HTTP Endpoints",
                version = "1.0.0",
                description = "HTTP endpoints exposed by Elsa workflows"
            },
            paths = GeneratePaths(endpoints)
        };

        return JsonSerializer.Serialize(openApiDoc, new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });
    }

    private object GeneratePaths(List<EndpointDefinition> endpoints)
    {
        var paths = new Dictionary<string, object>();

        foreach (var endpoint in endpoints)
        {
            if (!paths.ContainsKey(endpoint.Path))
            {
                paths[endpoint.Path] = new Dictionary<string, object>();
            }

            var pathItem = (Dictionary<string, object>)paths[endpoint.Path];
            pathItem[endpoint.Method.ToLowerInvariant()] = new
            {
                summary = $"{endpoint.Method.ToUpperInvariant()} {endpoint.Path}",
                description = $"Workflow endpoint for {endpoint.Method.ToUpperInvariant()} {endpoint.Path}",
                responses = new
                {
                    @default = new
                    {
                        description = "Response from workflow execution"
                    }
                },
                tags = new[] { "Workflows" }
            };
        }

        return paths;
    }
}
