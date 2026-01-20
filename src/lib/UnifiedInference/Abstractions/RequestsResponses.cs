namespace UnifiedInference.Abstractions;

public sealed record TextRequest(
    InferenceProvider Provider,
    string ModelId,
    string Prompt,
    GenerationSettings Settings
);

public sealed record TextResponse(
    string Text,
    int? TokensUsed,
    object? ProviderMetadata
);

public sealed record ImageRequest(
    InferenceProvider Provider,
    string ModelId,
    string Prompt,
    string? Size,
    GenerationSettings Settings
);

public sealed record ImageResponse(
    byte[]? Bytes,
    Uri? Uri,
    object? ProviderMetadata
);

public enum AudioMode { TextToSpeech, SpeechToText }

public sealed record AudioRequest(
    InferenceProvider Provider,
    string ModelId,
    AudioMode Mode,
    string? TextInput,
    byte[]? AudioInput,
    string? Voice,
    string? Language,
    GenerationSettings Settings
);

public sealed record AudioResponse(
    byte[]? AudioBytes,
    string? TranscriptText,
    object? ProviderMetadata
);

public sealed record VideoRequest(
    InferenceProvider Provider,
    string ModelId,
    string Prompt,
    TimeSpan? Duration,
    GenerationSettings Settings
);

public sealed record VideoResponse(
    byte[]? Bytes,
    Uri? Uri,
    object? ProviderMetadata
);

public sealed record MusicRequest(
    InferenceProvider Provider,
    string ModelId,
    string Prompt,
    GenerationSettings Settings
);

public sealed record MusicResponse(
    byte[]? Bytes,
    Uri? Uri,
    object? ProviderMetadata
);
