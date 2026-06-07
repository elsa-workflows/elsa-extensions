using Microsoft.Graph;

namespace Elsa.Integrations.OneDrive.Services;

/// <summary>
/// Factory for creating OneDrive clients using Microsoft Graph.
/// </summary>
public class OneDriveClientFactory
{
    private readonly GraphServiceClient _graphClient;

    /// <summary>
    /// Initializes a new instance of the <see cref="OneDriveClientFactory"/> class.
    /// </summary>
    public OneDriveClientFactory(GraphServiceClient graphClient)
    {
        _graphClient = graphClient;
    }

    /// <summary>
    /// Gets the Microsoft Graph client.
    /// </summary>
    /// <returns>The Microsoft Graph client.</returns>
    public GraphServiceClient CreateClient()
    {
        return _graphClient;
    }
}