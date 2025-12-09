using Elsa.Workflows;
using Elsa.Workflows.Attributes;
using Elsa.Workflows.Models;
using JetBrains.Annotations;
using OpenAI.Audio;

namespace Elsa.OpenAI.Activities.Audio;

/// <summary>
/// Generates speech from text using OpenAI's Text-to-Speech API.
/// </summary>
[Activity(
    "Elsa.OpenAI.Audio",
    "OpenAI Audio",
    "Generates speech from text using OpenAI's Text-to-Speech API.",
    DisplayName = "Generate Speech")]
[UsedImplicitly]
public class GenerateSpeech : OpenAIActivity
{
    /// <summary>
    /// The text to convert to speech.
    /// </summary>
    [Input(Description = "The text to convert to speech.")]
    public Input<string> Text { get; set; } = null!;

    /// <summary>
    /// The voice to use for speech generation (alloy, echo, fable, onyx, nova, shimmer).
    /// </summary>
    [Input(Description = "The voice to use for speech generation (alloy, echo, fable, onyx, nova, shimmer).")]
    public Input<string?> Voice { get; set; } = null!;

    /// <summary>
    /// The speed of the generated audio (0.25 to 4.0).
    /// </summary>
    [Input(Description = "The speed of the generated audio (0.25 to 4.0).")]
    public Input<float?> Speed { get; set; } = null!;

    /// <summary>
    /// The output file path where the audio will be saved.
    /// </summary>
    [Input(Description = "The output file path where the audio will be saved.")]
    public Input<string> OutputFilePath { get; set; } = null!;

    /// <summary>
    /// The response format for the audio (mp3, opus, aac, flac).
    /// </summary>
    [Input(Description = "The response format for the audio (mp3, opus, aac, flac).")]
    public Input<string?> ResponseFormat { get; set; } = null!;

    /// <summary>
    /// The path where the generated audio file was saved.
    /// </summary>
    [Output(Description = "The path where the generated audio file was saved.")]
    public Output<string> GeneratedFilePath { get; set; } = null!;

    /// <summary>
    /// The size of the generated audio file in bytes.
    /// </summary>
    [Output(Description = "The size of the generated audio file in bytes.")]
    public Output<long> FileSize { get; set; } = null!;

    /// <summary>
    /// Executes the activity.
    /// </summary>
    protected override async ValueTask ExecuteAsync(ActivityExecutionContext context)
    {
        string text = context.Get(Text)!;
        string? voice = context.Get(Voice);
        float? speed = context.Get(Speed);
        string outputFilePath = context.Get(OutputFilePath)!;
        string? responseFormat = context.Get(ResponseFormat);

        AudioClient client = GetAudioClient(context);

        var options = new SpeechGenerationOptions();

        // Set voice if provided
        if (!string.IsNullOrWhiteSpace(voice))
        {
            options.Voice = voice.ToLowerInvariant() switch
            {
                "alloy" => GeneratedSpeechVoice.Alloy,
                "echo" => GeneratedSpeechVoice.Echo,
                "fable" => GeneratedSpeechVoice.Fable,
                "onyx" => GeneratedSpeechVoice.Onyx,
                "nova" => GeneratedSpeechVoice.Nova,
                "shimmer" => GeneratedSpeechVoice.Shimmer,
                _ => GeneratedSpeechVoice.Alloy
            };
        }

        // Set speed if provided
        if (speed.HasValue)
            options.Speed = Math.Clamp(speed.Value, 0.25f, 4.0f);

        // Set response format if provided
        if (!string.IsNullOrWhiteSpace(responseFormat))
        {
            options.ResponseFormat = responseFormat.ToLowerInvariant() switch
            {
                "mp3" => GeneratedSpeechFormat.Mp3,
                "opus" => GeneratedSpeechFormat.Opus,
                "aac" => GeneratedSpeechFormat.Aac,
                "flac" => GeneratedSpeechFormat.Flac,
                _ => GeneratedSpeechFormat.Mp3
            };
        }

        BinaryData audioData = await client.GenerateSpeechAsync(text, options);

        // Ensure the output directory exists
        string? directory = Path.GetDirectoryName(outputFilePath);
        if (!string.IsNullOrEmpty(directory))
            Directory.CreateDirectory(directory);

        // Save the audio data to file
        await File.WriteAllBytesAsync(outputFilePath, audioData.ToArray());

        // Get file size
        var fileInfo = new FileInfo(outputFilePath);

        context.Set(GeneratedFilePath, outputFilePath);
        context.Set(FileSize, fileInfo.Length);
    }
}