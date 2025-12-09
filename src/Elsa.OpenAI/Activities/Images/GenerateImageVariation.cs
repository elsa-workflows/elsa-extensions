using Elsa.Workflows;
using Elsa.Workflows.Attributes;
using Elsa.Workflows.Models;
using JetBrains.Annotations;
using OpenAI.Images;

namespace Elsa.OpenAI.Activities.Images;

/// <summary>
/// Generates variations of an image using OpenAI's DALL-E API.
/// </summary>
[Activity(
    "Elsa.OpenAI.Images",
    "OpenAI Images",
    "Generates variations of an image using OpenAI's DALL-E API.",
    DisplayName = "Generate Image Variation")]
[UsedImplicitly]
public class GenerateImageVariation : OpenAIActivity
{
    /// <summary>
    /// The path to the input image file to create variations from.
    /// </summary>
    [Input(Description = "The path to the input image file to create variations from.")]
    public Input<string> ImageFilePath { get; set; } = null!;

    /// <summary>
    /// The size of the generated image variations (e.g., "1024x1024", "512x512", "256x256").
    /// </summary>
    [Input(Description = "The size of the generated image variations (e.g., \"1024x1024\", \"512x512\", \"256x256\").")]
    public Input<string?> Size { get; set; } = null!;

    /// <summary>
    /// The number of image variations to generate (1-10).
    /// </summary>
    [Input(Description = "The number of image variations to generate (1-10).")]
    public Input<int?> Count { get; set; } = null!;

    /// <summary>
    /// The URL of the first generated image variation.
    /// </summary>
    [Output(Description = "The URL of the first generated image variation.")]
    public Output<string?> ImageUrl { get; set; } = null!;

    /// <summary>
    /// The URLs of all generated image variations.
    /// </summary>
    [Output(Description = "The URLs of all generated image variations.")]
    public Output<List<string>?> ImageUrls { get; set; } = null!;

    /// <summary>
    /// The number of variations successfully generated.
    /// </summary>
    [Output(Description = "The number of variations successfully generated.")]
    public Output<int> GeneratedCount { get; set; } = null!;

    /// <summary>
    /// Executes the activity.
    /// </summary>
    protected override async ValueTask ExecuteAsync(ActivityExecutionContext context)
    {
        string imageFilePath = context.Get(ImageFilePath)!;
        string? size = context.Get(Size);
        int? count = context.Get(Count);

        ImageClient client = GetImageClient(context);

        // Read the input image file
        byte[] imageData = await File.ReadAllBytesAsync(imageFilePath);
        string filename = Path.GetFileName(imageFilePath);

        var options = new ImageVariationOptions()
        {
            ResponseFormat = GeneratedImageFormat.Uri
        };

        // Set size if provided
        if (!string.IsNullOrWhiteSpace(size))
        {
            if (Enum.TryParse<GeneratedImageSize>(size.Replace("x", "X"), out var parsedSize))
                options.Size = parsedSize;
        }

        // Set count if provided
        if (count.HasValue && count.Value > 0 && count.Value <= 10)
            options.ImageCount = count.Value;

        GeneratedImageCollection images = await client.GenerateImageVariationsAsync(imageData, filename, options);

        var imageUrls = new List<string>();
        string? firstImageUrl = null;

        foreach (GeneratedImage image in images)
        {
            if (image.ImageUri != null)
            {
                string imageUrl = image.ImageUri.ToString();
                imageUrls.Add(imageUrl);
                firstImageUrl ??= imageUrl;
            }
        }

        context.Set(ImageUrl, firstImageUrl);
        context.Set(ImageUrls, imageUrls.Count > 0 ? imageUrls : null);
        context.Set(GeneratedCount, imageUrls.Count);
    }
}