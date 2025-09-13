using Elsa.Api.Client.Shared.Models;
using Refit;

namespace Elsa.Studio.Agents.Client;

///  Represents a client API for retrieving available service providers.
public interface IServiceProvidersApi
{
    /// Lists all service providers.
    [Get("/ai/service-providers")]
    Task<ListResponse<string>> ListAsync(CancellationToken cancellationToken = default);
}
