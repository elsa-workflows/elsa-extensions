using Microsoft.SemanticKernel.Agents;

namespace Elsa.Agents;

/// <summary>
/// IElsaAgent adapter over a Semantic Kernel ChatCompletionAgent.
/// </summary>
public class SemanticKernelElsaAgent(ChatCompletionAgent innerAgent) : IElsaAgent
{
    public async Task<IAgentExecutionResponse> RunAsync(IAgentExecutionContext context)
    {
        var cancellationToken = context.CancellationToken;
        var result = await innerAgent.InvokeAsync(context.Message, cancellationToken: cancellationToken).LastOrDefaultAsync(cancellationToken);
        if (result is null)
            throw new InvalidOperationException("Agent did not produce a response.");

        var responseMessage = result.Message.Content ?? throw new InvalidOperationException("Agent did not produce a response.");
        return new AgentExecutionResponse
        {
            Text = responseMessage
        };
    }
}
