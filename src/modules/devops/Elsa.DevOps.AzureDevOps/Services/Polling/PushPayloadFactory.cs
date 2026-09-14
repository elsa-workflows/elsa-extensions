using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.TeamFoundation.SourceControl.WebApi;

namespace Elsa.DevOps.AzureDevOps.Services.Polling;

/// <summary>
/// Shapes a polled push the way a Service Hook delivers one, and decides which repositories polling covers.
/// </summary>
public static class PushPayloadFactory
{
    /// <summary>
    /// The Code Pushed trigger hands its payload out as raw JSON. A Service Hook sends camel-cased property names
    /// while the typed model would serialize as Pascal-cased ones, so a workflow reading <c>repository.name</c> would
    /// only work over one of the two routes without this.
    /// </summary>
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    public static JsonElement ToPayload(GitPush push)
    {
        ArgumentNullException.ThrowIfNull(push);
        return JsonSerializer.SerializeToElement(push, SerializerOptions);
    }

    /// <summary>
    /// Reports whether a repository is one polling covers. An empty configured set means every repository in the
    /// project; otherwise a repository matches on its name or its ID.
    /// </summary>
    public static bool IsWatched(GitRepository repository, IReadOnlyCollection<string> configured)
    {
        ArgumentNullException.ThrowIfNull(repository);
        ArgumentNullException.ThrowIfNull(configured);

        if (configured.Count == 0)
            return true;

        return configured.Any(entry =>
            string.Equals(entry?.Trim(), repository.Name, StringComparison.OrdinalIgnoreCase)
            || string.Equals(entry?.Trim(), repository.Id.ToString(), StringComparison.OrdinalIgnoreCase));
    }
}
