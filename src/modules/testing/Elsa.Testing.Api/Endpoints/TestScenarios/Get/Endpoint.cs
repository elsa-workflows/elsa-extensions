using Elsa.Abstractions;
using JetBrains.Annotations;

namespace Elsa.Testing.Api.Endpoints.TestScenarios.Get;

[UsedImplicitly]
public class Endpoint : ElsaEndpoint<Request, object>
{
    public override void Configure()
    {
        Get("/testing/test-scenarios/{id}");
        ConfigurePermissions("test-scenarios:read");
    }

    public override async Task<object> ExecuteAsync(Request req, CancellationToken ct)
    {
        // TODO: Implement get logic for test scenario by id
        var result = new { id = req.Id };
        return await Task.FromResult(result);
    }
}