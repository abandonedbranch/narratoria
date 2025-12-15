using System.Collections.Concurrent;
using System.Collections.Immutable;
using Narratoria.Narration;

namespace Narratoria.Components;

public interface IStageMetadataProvider
{
    void ApplyTelemetry(NarrationStageTelemetry telemetry);
    void ApplyProviderMetrics(Guid sessionId, NarrationStageKind stage, int? promptTokens, int? completionTokens, string? model);
    NarrationStageHover? Get(Guid turnId, NarrationStageKind stage);
    IReadOnlyDictionary<(Guid, NarrationStageKind), NarrationStageHover> Snapshot();
}

public sealed class StageMetadataProvider : IStageMetadataProvider
{
    private readonly ConcurrentDictionary<(Guid, string), NarrationStageHover> _byKey = new();
    private readonly IReadOnlyList<NarrationStageKind> _order;

    public StageMetadataProvider(IReadOnlyList<NarrationStageKind> stageOrder)
    {
        _order = stageOrder ?? throw new ArgumentNullException(nameof(stageOrder));
    }

    public void ApplyTelemetry(NarrationStageTelemetry telemetry)
    {
        var kind = _order.FirstOrDefault(k => string.Equals(k.Name, telemetry.Stage, StringComparison.Ordinal));
        if (kind.Equals(default)) return;
        var key = (telemetry.SessionId, telemetry.Stage);
        _byKey.AddOrUpdate(key,
            _ => new NarrationStageHover { TurnId = telemetry.SessionId, Stage = kind, Duration = telemetry.Elapsed },
            (_, existing) => existing with { Duration = telemetry.Elapsed });
    }

    public void ApplyProviderMetrics(Guid sessionId, NarrationStageKind stage, int? promptTokens, int? completionTokens, string? model)
    {
        var key = (sessionId, stage.Name);
        _byKey.AddOrUpdate(key,
            _ => new NarrationStageHover { TurnId = sessionId, Stage = stage, PromptTokens = promptTokens, CompletionTokens = completionTokens, Model = model },
            (_, existing) => existing with { PromptTokens = promptTokens, CompletionTokens = completionTokens, Model = model });
    }

    public NarrationStageHover? Get(Guid turnId, NarrationStageKind stage)
    {
        return _byKey.TryGetValue((turnId, stage.Name), out var hover) ? hover : null;
    }

    public IReadOnlyDictionary<(Guid, NarrationStageKind), NarrationStageHover> Snapshot()
    {
        var dict = new Dictionary<(Guid, NarrationStageKind), NarrationStageHover>(_byKey.Count);
        foreach (var kvp in _byKey)
        {
            var kind = _order.FirstOrDefault(k => string.Equals(k.Name, kvp.Key.Item2, StringComparison.Ordinal));
            if (kind.Equals(default)) continue;
            dict[(kvp.Value.TurnId, kind)] = kvp.Value;
        }
        return dict;
    }
}
