using Narratoria.OpenAi;

namespace Narratoria.Narration;

public interface INarrationPipelineObserver
{
    void OnStageCompleted(NarrationStageTelemetry telemetry);
    void OnError(NarrationPipelineError error);
    void OnTokensStreamed(Guid sessionId, int tokenCount);
}

public sealed record NarrationStageTelemetry(
    string Stage,
    string Status,
    string ErrorClass,
    Guid SessionId,
    TraceMetadata Trace,
    TimeSpan Elapsed);

public sealed class NullNarrationPipelineObserver : INarrationPipelineObserver
{
    public static INarrationPipelineObserver Instance { get; } = new NullNarrationPipelineObserver();

    public void OnError(NarrationPipelineError error)
    {
    }

    public void OnStageCompleted(NarrationStageTelemetry telemetry)
    {
    }

    public void OnTokensStreamed(Guid sessionId, int tokenCount)
    {
    }
}
