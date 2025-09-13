using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using Microsoft.SemanticKernel.PromptTemplates.Handlebars;

#pragma warning disable SKEXP0010
#pragma warning disable SKEXP0001

namespace Elsa.Agents;

public class AgentInvoker(IKernelFactory kernelFactory, IKernelConfigProvider kernelConfigProvider)
{
    public async Task<InvokeAgentResult> InvokeAgentAsync(string agentName, IDictionary<string, object?> input, CancellationToken cancellationToken = default)
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