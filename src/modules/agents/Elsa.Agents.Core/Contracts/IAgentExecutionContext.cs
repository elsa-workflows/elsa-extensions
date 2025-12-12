namespace Elsa.Agents;

public interface IAgentExecutionContext
{
    string Message { get; set; }
    CancellationToken CancellationToken { get; set; }
}