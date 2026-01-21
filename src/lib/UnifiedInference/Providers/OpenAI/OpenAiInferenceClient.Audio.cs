using System.Reflection;
using System.IO;
using OpenAI.Audio;
using UnifiedInference.Abstractions;

namespace UnifiedInference.Providers.OpenAI;

public sealed partial class OpenAiInferenceClient
{
    public async Task<AudioResponse> GenerateAudioTtsAsync(AudioRequest request, CancellationToken cancellationToken)
    {
        var audioClient = _client.GetAudioClient(request.ModelId);
        var text = request.TextInput ?? string.Empty;
        var (voice, options) = CreateSpeechOptions(request);
        var clientResult = await audioClient.GenerateSpeechFromTextAsync(text, voice, options, cancellationToken).ConfigureAwait(false);
        dynamic value = clientResult.Value;

        BinaryData? audioData = null;
        try
        {
            audioData = value?.AudioData as BinaryData
                        ?? value?.AudioBytes as BinaryData
                        ?? value as BinaryData;
        }
        catch
        {
            // ignore extraction issues
        }

        byte[]? audioBytes = audioData?.ToArray();

        return new AudioResponse(audioBytes, null, new { id = (string?)value?.Id });
    }

    public async Task<AudioResponse> GenerateAudioSttAsync(AudioRequest request, CancellationToken cancellationToken)
    {
        var audioClient = _client.GetAudioClient(request.ModelId);
        using var stream = new MemoryStream(request.AudioInput ?? Array.Empty<byte>());
        var options = CreateTranscriptionOptions(request);

        var clientResult = await audioClient.TranscribeAudioAsync(stream, "audio.wav", options, cancellationToken).ConfigureAwait(false);
        dynamic value = clientResult.Value;

        string? text = null;
        try
        {
            text = value?.Text as string ?? value?.Text?.ToString();
        }
        catch
        {
            // ignore
        }

        return new AudioResponse(null, text ?? string.Empty, new { id = (string?)value?.Id });
    }

    private static (GeneratedSpeechVoice voice, SpeechGenerationOptions options) CreateSpeechOptions(AudioRequest request)
    {
        var voice = GeneratedSpeechVoice.Alloy;
        if (!string.IsNullOrWhiteSpace(request.Voice))
        {
            try
            {
                voice = (GeneratedSpeechVoice)Enum.Parse(typeof(GeneratedSpeechVoice), request.Voice, ignoreCase: true);
            }
            catch { }
        }

        var options = new SpeechGenerationOptions();
        return (voice, options);
    }

    private static AudioTranscriptionOptions CreateTranscriptionOptions(AudioRequest request)
    {
        return string.IsNullOrWhiteSpace(request.Language)
            ? new AudioTranscriptionOptions()
            : new AudioTranscriptionOptions { Language = request.Language };
    }
}
