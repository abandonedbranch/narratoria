using System.Collections.Generic;

namespace UnifiedInference.Abstractions;

public enum InferenceProvider
{
    HuggingFace = 0
}

public enum AudioMode
{
    TextToSpeech,
    SpeechToText
}

public sealed record TextRequest
{
    public string ModelId { get; init; } = string.Empty;
    public string Prompt { get; init; } = string.Empty;
    public bool Stream { get; init; }
    public GenerationSettings Settings { get; init; } = new();
}

public sealed record TextResponse
{
    public string Text { get; init; } = string.Empty;
    public int? TokensUsed { get; init; }
    public IDictionary<string, object>? ProviderMetadata { get; init; }
}

public sealed record ImageRequest
{
    public string ModelId { get; init; } = string.Empty;
    public string Prompt { get; init; } = string.Empty;
    public string? NegativePrompt { get; init; }
    public int? Height { get; init; }
    public int? Width { get; init; }
    public GenerationSettings Settings { get; init; } = new();
}

public sealed record ImageResponse
{
    public byte[]? Bytes { get; init; }
    public string? Uri { get; init; }
    public IDictionary<string, object>? ProviderMetadata { get; init; }
}

public sealed record AudioRequest
{
    public string ModelId { get; init; } = string.Empty;
    public AudioMode Mode { get; init; }
    public string? TextInput { get; init; }
    public byte[]? AudioInput { get; init; }
    public string? Language { get; init; }
    public string? Voice { get; init; }
    public GenerationSettings Settings { get; init; } = new();
}

public sealed record AudioResponse
{
    public byte[]? AudioBytes { get; init; }
    public string? TranscriptText { get; init; }
    public IDictionary<string, object>? ProviderMetadata { get; init; }
}

public sealed record VideoRequest
{
    public string ModelId { get; init; } = string.Empty;
    public string Prompt { get; init; } = string.Empty;
    public int? DurationSeconds { get; init; }
    public string? Quality { get; init; }
    public GenerationSettings Settings { get; init; } = new();
}

public sealed record VideoResponse
{
    public byte[]? Bytes { get; init; }
    public string? Uri { get; init; }
    public IDictionary<string, object>? ProviderMetadata { get; init; }
}

public sealed record MusicRequest
{
    public string ModelId { get; init; } = string.Empty;
    public string Prompt { get; init; } = string.Empty;
    public GenerationSettings Settings { get; init; } = new();
}

public sealed record MusicResponse
{
    public byte[]? Bytes { get; init; }
    public IDictionary<string, object>? ProviderMetadata { get; init; }
}
