using System.Security.Cryptography;
using System.Text;
using Microsoft.VisualStudio.Services.Common;
using Microsoft.VisualStudio.Services.WebApi;

namespace Elsa.DevOps.AzureDevOps.Services;

/// <summary>
/// Factory for creating and caching Azure DevOps connections.
/// Implements <see cref="IDisposable"/> to ensure cached <see cref="VssConnection"/> instances are properly disposed.
/// </summary>
public class AzureDevOpsConnectionFactory : IDisposable
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

    private static string ComputeHash(string input)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(input));
        return Convert.ToHexString(bytes);
    }

    /// <inheritdoc />
    public void Dispose()
    {
        foreach (var connection in _connections.Values)
            connection.Dispose();

        _connections.Clear();
        _semaphore.Dispose();
    }
}
