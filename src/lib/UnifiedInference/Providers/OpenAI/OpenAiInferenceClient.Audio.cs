using System.IO;
using System.Reflection;
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
        var response = await audioClient.GenerateSpeechFromTextAsync(text, voice, options, cancellationToken).ConfigureAwait(false);
        dynamic value = response?.Value ?? response;

        byte[]? audioBytes = null;
        try
        {
            var bin = (object?)value?.AudioData ?? (object?)value;
            if (bin is null)
            {
                bin = (object?)value?.AudioBytes;
            }

            if (bin is not null)
            {
                var toArray = bin.GetType().GetMethod("ToArray", BindingFlags.Public | BindingFlags.Instance);
                if (toArray is not null)
                {
                    audioBytes = toArray.Invoke(bin, null) as byte[];
                }
            }
        }
        catch
        {
            // ignore extraction issues
        }

        return new AudioResponse(audioBytes, null, new { id = (string?)value?.Id });
    }

    public async Task<AudioResponse> GenerateAudioSttAsync(AudioRequest request, CancellationToken cancellationToken)
    {
        var audioClient = _client.GetAudioClient(request.ModelId);
        using var stream = new MemoryStream(request.AudioInput ?? Array.Empty<byte>());
        var options = CreateTranscriptionOptions(request);

        var response = await audioClient.TranscribeAudioAsync(stream, "audio.wav", options, cancellationToken).ConfigureAwait(false);
        dynamic value = response?.Value ?? response;

        string? text = null;
        try
        {
            text = value?.Text;
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
