using Microsoft.VisualStudio.Services.Common;
using Microsoft.VisualStudio.Services.WebApi;

namespace Elsa.DevOps.AzureDevOps.Services;

/// <summary>
/// Factory for creating Azure DevOps connections.
/// </summary>
public class AzureDevOpsConnectionFactory
{
    private readonly SemaphoreSlim _semaphore = new(1, 1);
    private readonly Dictionary<string, VssConnection> _connections = new();

    /// <summary>
    /// Gets a connection for the specified organization URL and token.
    /// </summary>
    public VssConnection GetConnection(string organizationUrl, string token)
    {
        var key = $"{organizationUrl}|{ComputeHash(token)}";
        if (_connections.TryGetValue(key, out var connection))
            return connection;

        try
        {
            _semaphore.Wait();

            if (_connections.TryGetValue(key, out connection))
                return connection;

            var uri = new Uri(organizationUrl.TrimEnd('/'));
            var credentials = new VssBasicCredential(string.Empty, token);
            connection = new VssConnection(uri, credentials);
            _connections[key] = connection;
            return connection;
        }
        finally
        {
            _semaphore.Release();
        }
    }
}
