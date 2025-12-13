namespace Elsa.Agents;

public interface ICodeFirstAgentResolver
{
    Task<IElsaAgent> ResolveAsync(string agentName, CancellationToken cancellationToken = default);
}