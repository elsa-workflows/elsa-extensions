namespace Elsa.Agents;

public class AgentExecutionContext : IAgentExecutionContext
{
    public string Message { get; set; } = null!;
    public CancellationToken CancellationToken { get; set; }
}