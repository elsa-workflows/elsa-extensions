using Elsa.Workflows;
using Elsa.Workflows.Attributes;
using Elsa.Workflows.Models;
using JetBrains.Annotations;
using OpenAI.Moderations;

namespace Elsa.OpenAI.Activities.Moderation;

/// <summary>
/// Moderates content using OpenAI's Moderation API.
/// </summary>
[Activity(
    "Elsa.OpenAI.Moderation",
    "OpenAI Moderation",
    "Moderates content using OpenAI's Moderation API.",
    DisplayName = "Moderate Content")]
[UsedImplicitly]
public class ModerateContent : OpenAIActivity
{
    /// <summary>
    /// The text content to moderate.
    /// </summary>
    [Input(Description = "The text content to moderate.")]
    public Input<string> Text { get; set; } = null!;

    /// <summary>
    /// Multiple text contents to moderate (alternative to single text).
    /// </summary>
    [Input(Description = "Multiple text contents to moderate (alternative to single text).")]
    public Input<List<string>?> Texts { get; set; } = null!;

    /// <summary>
    /// Whether the content was flagged by the moderation model.
    /// </summary>
    [Output(Description = "Whether the content was flagged by the moderation model.")]
    public Output<bool> IsFlagged { get; set; } = null!;

    /// <summary>
    /// The category scores for the moderation result.
    /// </summary>
    [Output(Description = "The category scores for the moderation result.")]
    public Output<Dictionary<string, float>?> CategoryScores { get; set; } = null!;

    /// <summary>
    /// The categories that were flagged.
    /// </summary>
    [Output(Description = "The categories that were flagged.")]
    public Output<List<string>?> FlaggedCategories { get; set; } = null!;

    /// <summary>
    /// All moderation results (when moderating multiple texts).
    /// </summary>
    [Output(Description = "All moderation results (when moderating multiple texts).")]
    public Output<List<object>?> ModerationResults { get; set; } = null!;

    /// <summary>
    /// Whether any of the texts were flagged (when moderating multiple texts).
    /// </summary>
    [Output(Description = "Whether any of the texts were flagged (when moderating multiple texts).")]
    public Output<bool> AnyFlagged { get; set; } = null!;

    /// <summary>
    /// Executes the activity.
    /// </summary>
    protected override async ValueTask ExecuteAsync(ActivityExecutionContext context)
    {
        string? singleText = context.Get(Text);
        List<string>? multipleTexts = context.Get(Texts);

        ModerationClient client = GetModerationClient(context);

        ModerationCollection moderationCollection;

        if (multipleTexts?.Count > 0)
        {
            // Moderate multiple texts
            moderationCollection = await client.ClassifyTextAsync(multipleTexts);
        }
        else if (!string.IsNullOrWhiteSpace(singleText))
        {
            // Moderate single text
            moderationCollection = await client.ClassifyTextAsync(singleText);
        }
        else
        {
            throw new InvalidOperationException("Either Text or Texts must be provided.");
        }

        var allResults = new List<object>();
        bool anyFlagged = false;
        bool firstIsFlagged = false;
        Dictionary<string, float>? firstCategoryScores = null;
        List<string>? firstFlaggedCategories = null;

        foreach (ModerationResult moderation in moderationCollection)
        {
            bool isFlagged = moderation.Flagged;
            anyFlagged = anyFlagged || isFlagged;

            // Store first result details for single-result outputs
            if (allResults.Count == 0)
            {
                firstIsFlagged = isFlagged;
                firstCategoryScores = ExtractCategoryScores(moderation);
                firstFlaggedCategories = ExtractFlaggedCategories(moderation);
            }

            // Add to all results
            allResults.Add(new
            {
                IsFlagged = isFlagged,
                CategoryScores = ExtractCategoryScores(moderation),
                FlaggedCategories = ExtractFlaggedCategories(moderation)
            });
        }

        context.Set(IsFlagged, firstIsFlagged);
        context.Set(CategoryScores, firstCategoryScores);
        context.Set(FlaggedCategories, firstFlaggedCategories?.Count > 0 ? firstFlaggedCategories : null);
        context.Set(ModerationResults, allResults.Count > 1 ? allResults : null);
        context.Set(AnyFlagged, anyFlagged);
    }

    private static Dictionary<string, float> ExtractCategoryScores(ModerationResult moderation)
    {
        return new Dictionary<string, float>
        {
            ["hate"] = moderation.CategoryScores.Hate,
            ["hate/threatening"] = moderation.CategoryScores.HateThreatening,
            ["harassment"] = moderation.CategoryScores.Harassment,
            ["harassment/threatening"] = moderation.CategoryScores.HarassmentThreatening,
            ["self-harm"] = moderation.CategoryScores.SelfHarm,
            ["self-harm/intent"] = moderation.CategoryScores.SelfHarmIntent,
            ["self-harm/instructions"] = moderation.CategoryScores.SelfHarmInstructions,
            ["sexual"] = moderation.CategoryScores.Sexual,
            ["sexual/minors"] = moderation.CategoryScores.SexualMinors,
            ["violence"] = moderation.CategoryScores.Violence,
            ["violence/graphic"] = moderation.CategoryScores.ViolenceGraphic
        };
    }

    private static List<string> ExtractFlaggedCategories(ModerationResult moderation)
    {
        var flagged = new List<string>();
        
        if (moderation.Categories.Hate) flagged.Add("hate");
        if (moderation.Categories.HateThreatening) flagged.Add("hate/threatening");
        if (moderation.Categories.Harassment) flagged.Add("harassment");
        if (moderation.Categories.HarassmentThreatening) flagged.Add("harassment/threatening");
        if (moderation.Categories.SelfHarm) flagged.Add("self-harm");
        if (moderation.Categories.SelfHarmIntent) flagged.Add("self-harm/intent");
        if (moderation.Categories.SelfHarmInstructions) flagged.Add("self-harm/instructions");
        if (moderation.Categories.Sexual) flagged.Add("sexual");
        if (moderation.Categories.SexualMinors) flagged.Add("sexual/minors");
        if (moderation.Categories.Violence) flagged.Add("violence");
        if (moderation.Categories.ViolenceGraphic) flagged.Add("violence/graphic");

        return flagged;
    }
}