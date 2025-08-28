using Elsa.Abstractions;
using Elsa.Testing.Core.Contracts;
using Elsa.Testing.Core.Entities;
using Elsa.Testing.Core.Serialization;
using Elsa.Workflows;
using JetBrains.Annotations;

namespace Elsa.Testing.Api.Endpoints.TestScenarios.Post;

[UsedImplicitly]
public class Endpoint(ITestScenarioStore store, IIdentityGenerator identityGenerator, AssertionSerializer assertionSerializer) : ElsaEndpointWithoutRequest
{
    public override void Configure()
    {
        Post("/testing/test-scenarios");
        ConfigurePermissions("test-scenarios:create");
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var serializerOptions = assertionSerializer.SerializerOptions;
        var request = (await HttpContext.Request.ReadFromJsonAsync<Request>(serializerOptions, ct))!;
        var (scenario, isNew) = await GetOrCreateScenarioAsync(request, ct);

        scenario.Name = request.Name.Trim();
        scenario.Description = request.Description?.Trim() ?? string.Empty;
        scenario.WorkflowDefinitionId = request.WorkflowDefinitionId;
        scenario.Input = request.Input ?? new Dictionary<string, object>();
        scenario.Variables = request.Variables ?? new Dictionary<string, object>();
        scenario.Assertions = request.Assertions;

        if (isNew)
            await store.AddAsync(scenario, ct);
        else
            await store.UpdateAsync(scenario, ct);

        await Send.OkAsync(scenario, ct);
    }

    private async Task<(TestScenario, bool)> GetOrCreateScenarioAsync(Request request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Id))
            return new();

        var existingScenario = await store.FindAsync(new() { Id = request.Id }, cancellationToken);
        var isNew = existingScenario == null;
        return (existingScenario ?? new TestScenario { Id = string.IsNullOrWhiteSpace(request.Id) ? identityGenerator.GenerateId() : request.Id }, isNew);
    }
}