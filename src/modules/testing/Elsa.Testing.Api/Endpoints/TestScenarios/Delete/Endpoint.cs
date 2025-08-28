using Elsa.Abstractions;
using JetBrains.Annotations;

namespace Elsa.Testing.Api.Endpoints.TestScenarios.Delete;

[UsedImplicitly]
public class Endpoint : ElsaEndpoint<Request>
{
    public override void Configure()
    {
        Delete("/testing/test-scenarios/{id}");
        ConfigurePermissions("test-scenarios:delete");
    }

    public override async Task HandleAsync(Request req, CancellationToken ct)
    {
        await Send.NoContentAsync(ct);
    }
}