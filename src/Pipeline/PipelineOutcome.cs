namespace Narratoria.Pipeline;

public enum PipelineOutcomeStatus
{
    Completed = 0,
    Failed = 1,
    Canceled = 2,
    Blocked = 3,
}

public enum PipelineFailureKind
{
    None = 0,
    Unknown = 1,
    TypeMismatch = 2,
    DecodeFailure = 3,
    SourceFailed = 4,
    TransformFailed = 5,
    SinkFailed = 6,
}

public sealed record PipelineOutcome(
    PipelineOutcomeStatus Status,
    PipelineFailureKind FailureKind,
    string? SafeMessage)
{
    public static PipelineOutcome Completed() => new(PipelineOutcomeStatus.Completed, PipelineFailureKind.None, null);

    public static PipelineOutcome Canceled() => new(PipelineOutcomeStatus.Canceled, PipelineFailureKind.None, "Canceled");

    public static PipelineOutcome Blocked(string safeMessage) => new(PipelineOutcomeStatus.Blocked, PipelineFailureKind.None, safeMessage);

    public static PipelineOutcome Failed(PipelineFailureKind failureKind, string safeMessage) =>
        new(PipelineOutcomeStatus.Failed, failureKind, safeMessage);
}
