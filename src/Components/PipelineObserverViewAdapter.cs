using System.Collections.Immutable;
using Narratoria.Narration;

namespace Narratoria.Components;

public sealed class PipelineObserverViewAdapter : INarrationPipelineObserver
{
    private readonly IReadOnlyList<NarrationStageKind> _stageOrder;
    private readonly Func<NarrationPipelineTurnView, NarrationPipelineTurnView> _getTurn;
    private readonly Action<NarrationPipelineTurnView> _setTurn;
    private readonly IStageMetadataProvider? _metadata;

    public PipelineObserverViewAdapter(
        IReadOnlyList<NarrationStageKind> stageOrder,
        Func<NarrationPipelineTurnView, NarrationPipelineTurnView> getTurn,
        Action<NarrationPipelineTurnView> setTurn,
        IStageMetadataProvider? metadata = null)
    {
        _stageOrder = stageOrder ?? throw new ArgumentNullException(nameof(stageOrder));
        _getTurn = getTurn ?? throw new ArgumentNullException(nameof(getTurn));
        _setTurn = setTurn ?? throw new ArgumentNullException(nameof(setTurn));
        _metadata = metadata;
    }

    public void OnStageCompleted(NarrationStageTelemetry telemetry)
    {
        var current = _getTurn(default!);
        _metadata?.ApplyTelemetry(telemetry);
        var updatedStages = current.Stages.Select(s => s.Kind.Name == telemetry.Stage
            ? new NarrationStageView { Kind = s.Kind, Status = telemetry.Status switch
            {
                "success" => NarrationStageStatus.Completed,
                "failure" => NarrationStageStatus.Failed,
                "canceled" => NarrationStageStatus.Failed,
                "skipped" => NarrationStageStatus.Skipped,
                _ => s.Status
            }, Duration = telemetry.Elapsed }
            : s).ToImmutableArray();
        _setTurn(current with { Stages = updatedStages });
    }

    public void OnTokensStreamed(Guid sessionId, int tokenCount)
    {
        var current = _getTurn(default!);
        var output = current.Output with { IsStreaming = true };
        _setTurn(current with { Output = output });
    }

    public void OnError(NarrationPipelineError error)
    {
        var current = _getTurn(default!);
        var updatedStages = current.Stages.Select(s => s.Kind.Name == error.Stage
            ? new NarrationStageView { Kind = s.Kind, Status = NarrationStageStatus.Failed, ErrorClass = error.ErrorClass.ToString() }
            : s).ToImmutableArray();
        _setTurn(current with { Stages = updatedStages });
    }
}
