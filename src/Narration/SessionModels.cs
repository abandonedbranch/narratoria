using System.Collections.Immutable;
using System.Text.Json;
using Narratoria.OpenAi;
using Narratoria.Storage.IndexedDb;

namespace Narratoria.Narration;

public sealed record SessionRecord
{
    public required Guid SessionId { get; init; }
    public required string Title { get; init; }
    public bool IsTitleUserSet { get; init; }
    public required DateTimeOffset CreatedAt { get; init; }
    public required DateTimeOffset UpdatedAt { get; init; }
}

public sealed record CreateSessionRequest
{
    public required Guid SessionId { get; init; }
    public required TraceMetadata Trace { get; init; }
    public string? InitialTitle { get; init; }
}

public enum NarrationTurnOutcome
{
    Succeeded,
    Failed,
    Canceled
}

public sealed record NarrationStageSnapshot
{
    public required string StageId { get; init; }
    public required string Status { get; init; }
    public TimeSpan? Duration { get; init; }
    public string? ErrorClass { get; init; }
    public string? ErrorMessage { get; init; }
    public int? PromptTokens { get; init; }
    public int? CompletionTokens { get; init; }
    public string? Model { get; init; }
}

public sealed record NarrationTurnRecord
{
    public required Guid SessionId { get; init; }
    public required Guid TurnId { get; init; }
    public required DateTimeOffset CreatedAt { get; init; }
    public required DateTimeOffset UpdatedAt { get; init; }
    public required string Prompt { get; init; }
    public required NarrationTurnOutcome Outcome { get; init; }
    public required ImmutableArray<string> StageOrder { get; init; }
    public required ImmutableArray<NarrationStageSnapshot> Stages { get; init; }
    public required ImmutableArray<string> OutputSegments { get; init; }
    public required bool IsFinal { get; init; }
    public string? FailureClass { get; init; }
    public required TraceMetadata Trace { get; init; }
}

internal sealed class SessionRecordSerializer : IIndexedDbValueSerializer<SessionRecord>
{
    private static readonly JsonSerializerOptions Options = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    public ValueTask<IndexedDbSerializedValue> SerializeAsync(SessionRecord value, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var bytes = JsonSerializer.SerializeToUtf8Bytes(value, Options);
        return ValueTask.FromResult(new IndexedDbSerializedValue(bytes, bytes.LongLength));
    }

    public ValueTask<SessionRecord> DeserializeAsync(IndexedDbSerializedValue payload, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var record = JsonSerializer.Deserialize<SessionRecord>(payload.Payload, Options)
                     ?? throw new InvalidOperationException("Unable to deserialize SessionRecord");
        return ValueTask.FromResult(record);
    }
}

internal sealed class NarrationTurnRecordSerializer : IIndexedDbValueSerializer<NarrationTurnRecord>
{
    private static readonly JsonSerializerOptions Options = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    public ValueTask<IndexedDbSerializedValue> SerializeAsync(NarrationTurnRecord value, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var bytes = JsonSerializer.SerializeToUtf8Bytes(value, Options);
        return ValueTask.FromResult(new IndexedDbSerializedValue(bytes, bytes.LongLength));
    }

    public ValueTask<NarrationTurnRecord> DeserializeAsync(IndexedDbSerializedValue payload, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var record = JsonSerializer.Deserialize<NarrationTurnRecord>(payload.Payload, Options)
                     ?? throw new InvalidOperationException("Unable to deserialize NarrationTurnRecord");
        return ValueTask.FromResult(record);
    }
}
