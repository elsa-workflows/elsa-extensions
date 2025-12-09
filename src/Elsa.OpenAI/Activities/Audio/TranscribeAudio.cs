using Elsa.Workflows;
using Elsa.Workflows.Attributes;
using Elsa.Workflows.Models;
using JetBrains.Annotations;
using OpenAI.Audio;

namespace Elsa.OpenAI.Activities.Audio;

/// <summary>
/// Transcribes audio using OpenAI's Whisper API.
/// </summary>
[Activity(
    "Elsa.OpenAI.Audio",
    "OpenAI Audio",
    "Transcribes audio using OpenAI's Whisper API.",
    DisplayName = "Transcribe Audio")]
[UsedImplicitly]
public class TranscribeAudio : OpenAIActivity
{
    /// <summary>
    /// The path to the audio file to transcribe.
    /// </summary>
    [Input(Description = "The path to the audio file to transcribe.")]
    public Input<string> AudioFilePath { get; set; } = null!;

    /// <summary>
    /// The filename to use for the audio file (optional, inferred from path if not provided).
    /// </summary>
    [Input(Description = "The filename to use for the audio file (optional, inferred from path if not provided).")]
    public Input<string?> Filename { get; set; } = null!;

    /// <summary>
    /// The language of the input audio (ISO-639-1 format, e.g., "en", "es", "fr").
    /// </summary>
    [Input(Description = "The language of the input audio (ISO-639-1 format, e.g., \"en\", \"es\", \"fr\").")]
    public Input<string?> Language { get; set; } = null!;

    /// <summary>
    /// Optional text to guide the model's style or continue a previous audio segment.
    /// </summary>
    [Input(Description = "Optional text to guide the model's style or continue a previous audio segment.")]
    public Input<string?> Prompt { get; set; } = null!;

    /// <summary>
    /// The response format ("json", "text", "srt", "verbose_json", "vtt").
    /// </summary>
    [Input(Description = "The response format (\"json\", \"text\", \"srt\", \"verbose_json\", \"vtt\").")]
    public Input<string?> ResponseFormat { get; set; } = null!;

    /// <summary>
    /// The sampling temperature, between 0 and 1. Higher values make output more random.
    /// </summary>
    [Input(Description = "The sampling temperature, between 0 and 1. Higher values make output more random.")]
    public Input<float?> Temperature { get; set; } = null!;

    /// <summary>
    /// The transcribed text.
    /// </summary>
    [Output(Description = "The transcribed text.")]
    public Output<string> Text { get; set; } = null!;

    /// <summary>
    /// The language detected in the audio (if not provided as input).
    /// </summary>
    [Output(Description = "The language detected in the audio (if not provided as input).")]
    public Output<string?> DetectedLanguage { get; set; } = null!;

    /// <summary>
    /// The duration of the audio in seconds.
    /// </summary>
    [Output(Description = "The duration of the audio in seconds.")]
    public Output<float?> Duration { get; set; } = null!;

    /// <summary>
    /// Executes the activity.
    /// </summary>
    protected override async ValueTask ExecuteAsync(ActivityExecutionContext context)
    {
        string audioFilePath = context.Get(AudioFilePath)!;
        string? filename = context.Get(Filename);
        string? language = context.Get(Language);
        string? prompt = context.Get(Prompt);
        string? responseFormat = context.Get(ResponseFormat);
        float? temperature = context.Get(Temperature);

        AudioClient client = GetAudioClient(context);

        // If filename is not provided, extract from path
        filename ??= Path.GetFileName(audioFilePath);

        // Read audio file
        byte[] audioData = await File.ReadAllBytesAsync(audioFilePath);

        var options = new AudioTranscriptionOptions()
        {
            ResponseFormat = AudioTranscriptionFormat.SimpleText
        };

        // Set response format if provided
        if (!string.IsNullOrWhiteSpace(responseFormat))
        {
            options.ResponseFormat = responseFormat.ToLowerInvariant() switch
            {
                "json" => AudioTranscriptionFormat.Simple,
                "text" => AudioTranscriptionFormat.SimpleText,
                "srt" => AudioTranscriptionFormat.Srt,
                "verbose_json" => AudioTranscriptionFormat.Verbose,
                "vtt" => AudioTranscriptionFormat.Vtt,
                _ => AudioTranscriptionFormat.SimpleText
            };
        }

        // Set language if provided
        if (!string.IsNullOrWhiteSpace(language))
            options.Language = language;

        // Set prompt if provided
        if (!string.IsNullOrWhiteSpace(prompt))
            options.Prompt = prompt;

        // Set temperature if provided
        if (temperature.HasValue)
            options.Temperature = temperature.Value;

        AudioTranscription transcription = await client.TranscribeAudioAsync(audioData, filename, options);

        context.Set(Text, transcription.Text);
        context.Set(DetectedLanguage, transcription.Language);
        context.Set(Duration, transcription.Duration?.TotalSeconds);
    }
}