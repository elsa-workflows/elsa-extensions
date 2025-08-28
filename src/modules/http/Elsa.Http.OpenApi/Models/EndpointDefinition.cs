namespace Elsa.Http.OpenApi.Models;

/// <summary>
/// Represents an HTTP endpoint definition for OpenAPI documentation.
/// </summary>
public record EndpointDefinition
{
    /// <summary>
    /// Gets or sets the HTTP path of the endpoint.
    /// </summary>
    public string Path { get; set; } = default!;

    /// <summary>
    /// Gets or sets the HTTP method of the endpoint.
    /// </summary>
    public string Method { get; set; } = default!;
}
