using Elsa.Workflows;
using Elsa.Workflows.Attributes;
using Elsa.Workflows.Models;
using JetBrains.Annotations;
using OpenAI.Embeddings;

namespace Elsa.OpenAI.Activities.Embeddings;

/// <summary>
/// Creates text embeddings using OpenAI's Embeddings API.
/// </summary>
[Activity(
    "Elsa.OpenAI.Embeddings",
    "OpenAI Embeddings",
    "Creates text embeddings using OpenAI's Embeddings API.",
    DisplayName = "Create Embedding")]
[UsedImplicitly]
public class CreateEmbedding : OpenAIActivity
{
    /// <summary>
    /// The input text to generate embeddings for.
    /// </summary>
    [Input(Description = "The input text to generate embeddings for.")]
    public Input<string> Text { get; set; } = null!;

    /// <summary>
    /// Multiple input texts to generate embeddings for (alternative to single text).
    /// </summary>
    [Input(Description = "Multiple input texts to generate embeddings for (alternative to single text).")]
    public Input<List<string>?> Texts { get; set; } = null!;

    /// <summary>
    /// The embedding dimensions (only supported for certain models).
    /// </summary>
    [Input(Description = "The embedding dimensions (only supported for certain models).")]
    public Input<int?> Dimensions { get; set; } = null!;

    /// <summary>
    /// The embedding vector for the input text.
    /// </summary>
    [Output(Description = "The embedding vector for the input text.")]
    public Output<ReadOnlyMemory<float>?> Embedding { get; set; } = null!;

    /// <summary>
    /// The embedding vectors for multiple input texts.
    /// </summary>
    [Output(Description = "The embedding vectors for multiple input texts.")]
    public Output<List<ReadOnlyMemory<float>>?> Embeddings { get; set; } = null!;

    /// <summary>
    /// The number of tokens used in the request.
    /// </summary>
    [Output(Description = "The number of tokens used in the request.")]
    public Output<int?> TokenCount { get; set; } = null!;

    /// <summary>
    /// The number of embedding vectors generated.
    /// </summary>
    [Output(Description = "The number of embedding vectors generated.")]
    public Output<int> EmbeddingCount { get; set; } = null!;

    /// <summary>
    /// Executes the activity.
    /// </summary>
    protected override async ValueTask ExecuteAsync(ActivityExecutionContext context)
    {
        string? singleText = context.Get(Text);
        List<string>? multipleTexts = context.Get(Texts);
        int? dimensions = context.Get(Dimensions);

        EmbeddingClient client = GetEmbeddingClient(context);

        var options = new EmbeddingGenerationOptions();
        if (dimensions.HasValue)
            options.Dimensions = dimensions.Value;

        EmbeddingCollection embeddingCollection;

        if (multipleTexts?.Count > 0)
        {
            // Generate embeddings for multiple texts
            embeddingCollection = await client.GenerateEmbeddingsAsync(multipleTexts, options);
        }
        else if (!string.IsNullOrWhiteSpace(singleText))
        {
            // Generate embedding for single text
            embeddingCollection = await client.GenerateEmbeddingsAsync([singleText], options);
        }
        else
        {
            throw new InvalidOperationException("Either Text or Texts must be provided.");
        }

        var embeddings = new List<ReadOnlyMemory<float>>();
        ReadOnlyMemory<float>? firstEmbedding = null;

        foreach (Embedding embedding in embeddingCollection)
        {
            embeddings.Add(embedding.Vector);
            firstEmbedding ??= embedding.Vector;
        }

        context.Set(Embedding, firstEmbedding);
        context.Set(Embeddings, embeddings.Count > 0 ? embeddings : null);
        context.Set(TokenCount, embeddingCollection.Usage?.TotalTokenCount);
        context.Set(EmbeddingCount, embeddings.Count);
    }
}