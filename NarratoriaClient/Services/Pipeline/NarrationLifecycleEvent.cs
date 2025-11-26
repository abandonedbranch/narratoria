namespace NarratoriaClient.Services.Pipeline;

public enum NarrationLifecycleEventKind
{
    StageStarting,
    StageCompleted,
    StageFailed,
    PipelineCompleted,
    PipelineFailed
}

public sealed record NarrationLifecycleEvent(
    Guid CorrelationId,
    string Stage,
    NarrationLifecycleEventKind Kind,
    DateTimeOffset Timestamp,
    IReadOnlyDictionary<string, object?>? Metadata = null,
    Exception? Error = null);
