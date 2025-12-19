using Narratoria.Narration;

namespace Narratoria.Components;

public sealed class TurnScopedObserver : INarrationPipelineObserver
{
    private readonly Guid _turnId;
    private readonly INarrationPipelineObserver _inner;

    public TurnScopedObserver(Guid turnId, INarrationPipelineObserver inner)
    {
        if (turnId == Guid.Empty) throw new ArgumentException("TurnId is required.", nameof(turnId));
        _turnId = turnId;
        _inner = inner ?? throw new ArgumentNullException(nameof(inner));
    }

    public void OnStageCompleted(NarrationStageTelemetry telemetry)
    {
        if (telemetry is null) return;
        _inner.OnStageCompleted(telemetry with { SessionId = _turnId });
    }

    public void OnError(NarrationPipelineError error)
    {
        if (error is null) return;
        _inner.OnError(error with { SessionId = _turnId });
    }

    public void OnTokensStreamed(Guid sessionId, int tokenCount)
    {
        _inner.OnTokensStreamed(_turnId, tokenCount);
    }
}
