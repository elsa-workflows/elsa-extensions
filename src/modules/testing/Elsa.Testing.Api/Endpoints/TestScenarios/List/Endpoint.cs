using Elsa.Abstractions;
using JetBrains.Annotations;

namespace Elsa.Testing.Api.Endpoints.TestScenarios.List;

[UsedImplicitly]
public class Endpoint : ElsaEndpoint<Request, object[]>
{
    public override void Configure()
    {
        Get("testing//test-scenarios");
        ConfigurePermissions("test-scenarios:read");
    }

    public override async Task<object[]> ExecuteAsync(Request req, CancellationToken ct)
    {
        // TODO: Implement logic to list all test scenarios
        return await Task.FromResult(Array.Empty<object>());
    }
}