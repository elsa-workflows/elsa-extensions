using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using Microsoft.SemanticKernel.PromptTemplates.Handlebars;

#pragma warning disable SKEXP0010
#pragma warning disable SKEXP0001
#pragma warning disable SKEXP0110

namespace Elsa.Agents;

public class AgentInvoker(
    IKernelFactory kernelFactory, 
    IKernelConfigProvider kernelConfigProvider,
    AgentFrameworkFactory agentFrameworkFactory)
{
    /// <summary>
    /// Invokes an agent using the Microsoft Agent Framework (new approach).
    /// </summary>
    public async Task<InvokeAgentResult> InvokeAgentAsync(string agentName, IDictionary<string, object?> input, CancellationToken cancellationToken = default)
    {
        return await InvokeAgentAsync(agentName, input, useAgentFramework: true, cancellationToken);
    }

    /// <summary>
    /// Invokes an agent with option to use legacy Semantic Kernel or new Agent Framework.
    /// </summary>
    public async Task<InvokeAgentResult> InvokeAgentAsync(string agentName, IDictionary<string, object?> input, bool useAgentFramework, CancellationToken cancellationToken = default)
    {
        if (useAgentFramework)
            return await InvokeAgentWithFrameworkAsync(agentName, input, cancellationToken);
        else
            return await InvokeAgentLegacyAsync(agentName, input, cancellationToken);
    }

    private async Task<InvokeAgentResult> InvokeAgentWithFrameworkAsync(string agentName, IDictionary<string, object?> input, CancellationToken cancellationToken)
    {
        var kernelConfig = await kernelConfigProvider.GetKernelConfigAsync(cancellationToken);
        var agentConfig = kernelConfig.Agents[agentName];
        
        // Create agent using Agent Framework
        var agent = agentFrameworkFactory.CreateAgent(kernelConfig, agentConfig);
        
        // Create chat history
        ChatHistory chatHistory = [];
        
        // Format and add user input
        var promptTemplateConfig = new PromptTemplateConfig
        {
            Template = agentConfig.PromptTemplate,
            TemplateFormat = "handlebars",
            Name = agentConfig.FunctionName
        };

        var templateFactory = new HandlebarsPromptTemplateFactory();
        var promptTemplate = templateFactory.Create(promptTemplateConfig);
        var kernelArguments = new KernelArguments(input);
        string renderedPrompt = await promptTemplate.RenderAsync(agent.Kernel, kernelArguments);
        
        chatHistory.AddUserMessage(renderedPrompt);

        // Get response from agent
        var response = await agent.InvokeAsync(chatHistory, cancellationToken: cancellationToken).LastOrDefaultAsync(cancellationToken);
        
        if (response == null)
            throw new InvalidOperationException("Agent did not produce a response");

        return new InvokeAgentResult(agentConfig, response);
    }

    private async Task<InvokeAgentResult> InvokeAgentLegacyAsync(string agentName, IDictionary<string, object?> input, CancellationToken cancellationToken)
    {
        var kernelConfig = await kernelConfigProvider.GetKernelConfigAsync(cancellationToken);
        var kernel = kernelFactory.CreateKernel(kernelConfig, agentName);
        var agentConfig = kernelConfig.Agents[agentName];
        var executionSettings = agentConfig.ExecutionSettings;
        var promptExecutionSettings = new OpenAIPromptExecutionSettings
        {
            Temperature = executionSettings.Temperature,
            TopP = executionSettings.TopP,
            MaxTokens = executionSettings.MaxTokens,
            PresencePenalty = executionSettings.PresencePenalty,
            FrequencyPenalty = executionSettings.FrequencyPenalty,
            ResponseFormat = executionSettings.ResponseFormat,
            ChatSystemPrompt = agentConfig.PromptTemplate,
            FunctionChoiceBehavior = FunctionChoiceBehavior.Auto()
        };
        
        var promptExecutionSettingsDictionary = new Dictionary<string, PromptExecutionSettings>
        {
            [PromptExecutionSettings.DefaultServiceId] = promptExecutionSettings,
        };
        
        var promptTemplateConfig = new PromptTemplateConfig
        {
            Name = agentConfig.FunctionName,
            Description = agentConfig.Description,
            Template = agentConfig.PromptTemplate,
            ExecutionSettings = promptExecutionSettingsDictionary,
            AllowDangerouslySetContent = true,
            InputVariables = agentConfig.InputVariables.Select(x => new InputVariable
            {
                Name = x.Name,
                Description = x.Description,
                IsRequired = true,
                AllowDangerouslySetContent = true
            }).ToList()
        };

        var templateFactory = new HandlebarsPromptTemplateFactory();

        var promptConfig = new PromptTemplateConfig
        {
            Template = agentConfig.PromptTemplate,
            TemplateFormat = "handlebars",
            Name = agentConfig.FunctionName
        };

        var promptTemplate = templateFactory.Create(promptConfig);

        var kernelArguments = new KernelArguments(input);
        string renderedPrompt = await promptTemplate.RenderAsync(kernel, kernelArguments);

        ChatHistory chatHistory = [];
        chatHistory.AddUserMessage(renderedPrompt);

        IChatCompletionService chatCompletion = kernel.GetRequiredService<IChatCompletionService>();

        OpenAIPromptExecutionSettings openAIPromptExecutionSettings = new()
        {
            FunctionChoiceBehavior = FunctionChoiceBehavior.Auto()
        };

        var response = await chatCompletion.GetChatMessageContentAsync(
            chatHistory,
            executionSettings: openAIPromptExecutionSettings,
            kernel: kernel);

        return new(agentConfig, response);
    }
}