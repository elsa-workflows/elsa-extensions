using System.Diagnostics.CodeAnalysis;
using Elsa.Extensions;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Connectors.InMemory;
using Microsoft.SemanticKernel.Memory;
using Microsoft.SemanticKernel.Connectors.OpenAI;

namespace Elsa.Agents;

public class OpenAIEmbeddingGenerator : IAgentServiceProvider
{
    public string Name => "OpenAIEmbeddingGenerator";

    [Experimental("SKEXP0010")]
    public void ConfigureKernel(KernelBuilderContext context)
    {
        var modelId = (string)context.ServiceConfig.Settings["ModelId"];
        var apiKey = context.GetApiKey();

        context.KernelBuilder.Services.AddInMemoryVectorStore();
        context.KernelBuilder.AddOpenAIEmbeddingGenerator(modelId, apiKey);
    }
}