namespace Narratoria.OpenAi;

public sealed record SerializedPrompt(Guid Id, string Payload, IReadOnlyDictionary<string, string>? Metadata = null);

public sealed record StreamedToken(string Content, bool IsFinal, DateTimeOffset Timestamp);

public sealed record TraceMetadata(string TraceId, string RequestId);
