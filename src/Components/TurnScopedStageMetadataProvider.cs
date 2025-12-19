using Narratoria.Narration;

namespace Narratoria.Components;

public sealed class TurnScopedStageMetadataProvider : IStageMetadataProvider
{
    private readonly Guid _turnId;
    private readonly IStageMetadataProvider _inner;

    public TurnScopedStageMetadataProvider(Guid turnId, IStageMetadataProvider inner)
    {
        if (turnId == Guid.Empty) throw new ArgumentException("TurnId is required.", nameof(turnId));
        _turnId = turnId;
        _inner = inner ?? throw new ArgumentNullException(nameof(inner));
    }

    public void ApplyTelemetry(NarrationStageTelemetry telemetry)
    {
        if (telemetry is null) return;
        _inner.ApplyTelemetry(telemetry with { SessionId = _turnId });
    }

    public void ApplyProviderMetrics(Guid sessionId, NarrationStageKind stage, int? promptTokens, int? completionTokens, string? model)
    {
        _inner.ApplyProviderMetrics(_turnId, stage, promptTokens, completionTokens, model);
    }

    public NarrationStageHover? Get(Guid turnId, NarrationStageKind stage)
    {
        return _inner.Get(turnId, stage);
    }

    public IReadOnlyDictionary<(Guid, NarrationStageKind), NarrationStageHover> Snapshot()
    {
        return _inner.Snapshot();
    }
}
