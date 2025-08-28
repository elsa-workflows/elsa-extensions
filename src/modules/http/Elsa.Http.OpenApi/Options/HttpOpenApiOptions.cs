namespace Elsa.Http.OpenApi.Options;

/// <summary>
/// Configuration options for HTTP OpenAPI functionality.
/// </summary>
public class HttpOpenApiOptions
{
    /// <summary>
    /// Gets or sets a value indicating whether OpenAPI documentation should be enabled.
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Gets or sets the title of the OpenAPI document.
    /// </summary>
    public string Title { get; set; } = "Elsa Workflow HTTP Endpoints";

    /// <summary>
    /// Gets or sets the version of the OpenAPI document.
    /// </summary>
    public string Version { get; set; } = "v1";

    /// <summary>
    /// Gets or sets the description of the OpenAPI document.
    /// </summary>
    public string Description { get; set; } = "HTTP endpoints exposed by Elsa workflows";
}
