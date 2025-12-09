using Elsa.Workflows;
using Elsa.Workflows.Attributes;
using Elsa.Workflows.Models;
using JetBrains.Annotations;
using OpenAI.Images;

namespace Elsa.OpenAI.Activities.Images;

/// <summary>
/// Generates an image using OpenAI's DALL-E API.
/// </summary>
[Activity(
    "Elsa.OpenAI.Images",
    "OpenAI Images",
    "Generates an image using OpenAI's DALL-E API.",
    DisplayName = "Generate Image")]
[UsedImplicitly]
public class GenerateImage : OpenAIActivity
{
    /// <summary>
    /// The text description of the image to generate.
    /// </summary>
    [Input(Description = "The text description of the image to generate.")]
    public Input<string> Prompt { get; set; } = null!;

    /// <summary>
    /// The size of the generated image (e.g., "1024x1024", "512x512", "256x256").
    /// </summary>
    [Input(Description = "The size of the generated image (e.g., \"1024x1024\", \"512x512\", \"256x256\").")]
    public Input<string?> Size { get; set; } = null!;

    /// <summary>
    /// The quality of the image ("standard" or "hd").
    /// </summary>
    [Input(Description = "The quality of the image (\"standard\" or \"hd\").")]
    public Input<string?> Quality { get; set; } = null!;

    /// <summary>
    /// The style of the generated image ("vivid" or "natural").
    /// </summary>
    [Input(Description = "The style of the generated image (\"vivid\" or \"natural\").")]
    public Input<string?> Style { get; set; } = null!;

    /// <summary>
    /// The number of images to generate (1-10).
    /// </summary>
    [Input(Description = "The number of images to generate (1-10).")]
    public Input<int?> Count { get; set; } = null!;

    /// <summary>
    /// The URL of the generated image.
    /// </summary>
    [Output(Description = "The URL of the generated image.")]
    public Output<string?> ImageUrl { get; set; } = null!;

    /// <summary>
    /// The URLs of all generated images (when count > 1).
    /// </summary>
    [Output(Description = "The URLs of all generated images (when count > 1).")]
    public Output<List<string>?> ImageUrls { get; set; } = null!;

    /// <summary>
    /// The revised prompt that was used to generate the image.
    /// </summary>
    [Output(Description = "The revised prompt that was used to generate the image.")]
    public Output<string?> RevisedPrompt { get; set; } = null!;

    /// <summary>
    /// Executes the activity.
    /// </summary>
    protected override async ValueTask ExecuteAsync(ActivityExecutionContext context)
    {
        string prompt = context.Get(Prompt)!;
        string? size = context.Get(Size);
        string? quality = context.Get(Quality);
        string? style = context.Get(Style);
        int? count = context.Get(Count);

        ImageClient client = GetImageClient(context);

        var options = new ImageGenerationOptions()
        {
            ResponseFormat = GeneratedImageFormat.Uri
        };

        // Set size if provided
        if (!string.IsNullOrWhiteSpace(size))
        {
            if (Enum.TryParse<GeneratedImageSize>(size.Replace("x", "X"), out var parsedSize))
                options.Size = parsedSize;
        }

        // Set quality if provided
        if (!string.IsNullOrWhiteSpace(quality))
        {
            if (Enum.TryParse<GeneratedImageQuality>(quality, true, out var parsedQuality))
                options.Quality = parsedQuality;
        }

        // Set style if provided
        if (!string.IsNullOrWhiteSpace(style))
        {
            if (Enum.TryParse<GeneratedImageStyle>(style, true, out var parsedStyle))
                options.Style = parsedStyle;
        }

        // Set count if provided
        if (count.HasValue && count.Value > 0 && count.Value <= 10)
            options.ImageCount = count.Value;

        GeneratedImageCollection images = await client.GenerateImageAsync(prompt, options);

        var imageUrls = new List<string>();
        string? firstImageUrl = null;
        string? revisedPrompt = null;

        foreach (GeneratedImage image in images)
        {
            if (image.ImageUri != null)
            {
                string imageUrl = image.ImageUri.ToString();
                imageUrls.Add(imageUrl);
                firstImageUrl ??= imageUrl;
            }
            revisedPrompt ??= image.RevisedPrompt;
        }

        context.Set(ImageUrl, firstImageUrl);
        context.Set(ImageUrls, imageUrls.Count > 0 ? imageUrls : null);
        context.Set(RevisedPrompt, revisedPrompt);
    }
}