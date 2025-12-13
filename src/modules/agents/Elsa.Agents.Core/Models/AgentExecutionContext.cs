namespace Elsa.Agents;

public class AgentExecutionContext
{
    public string Message { get; set; } = null!;
    public CancellationToken CancellationToken { get; set; }
}