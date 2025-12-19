using System.Collections.Immutable;
using Narratoria.Narration;

namespace Narratoria.Components;

public sealed class PipelineObserverViewAdapter : INarrationPipelineObserver
{
    private const string AttachmentIngestionStage = "attachment_ingestion";

    private readonly IReadOnlyList<NarrationStageKind> _stageOrder;
    private readonly Func<NarrationPipelineTurnView, NarrationPipelineTurnView> _getTurn;
    private readonly Action<NarrationPipelineTurnView> _setTurn;
    private readonly IStageMetadataProvider? _metadata;
    private readonly int _attachmentExpectedCount;
    private int _attachmentCompletedCount;

    public PipelineObserverViewAdapter(
        IReadOnlyList<NarrationStageKind> stageOrder,
        Func<NarrationPipelineTurnView, NarrationPipelineTurnView> getTurn,
        Action<NarrationPipelineTurnView> setTurn,
        IStageMetadataProvider? metadata = null,
        int attachmentExpectedCount = 0)
    {
        _stageOrder = stageOrder ?? throw new ArgumentNullException(nameof(stageOrder));
        _getTurn = getTurn ?? throw new ArgumentNullException(nameof(getTurn));
        _setTurn = setTurn ?? throw new ArgumentNullException(nameof(setTurn));
        _metadata = metadata;
        _attachmentExpectedCount = attachmentExpectedCount;
    }

    public void OnStageCompleted(NarrationStageTelemetry telemetry)
    {
        var current = _getTurn(default!);
        _metadata?.ApplyTelemetry(telemetry);
        var stages = current.Stages.ToArray();
        var stageIndex = Array.FindIndex(stages, s => string.Equals(s.Kind.Name, telemetry.Stage, StringComparison.Ordinal));
        if (stageIndex < 0) return;

        var status = telemetry.Status switch
        {
            "success" => NarrationStageStatus.Completed,
            "failure" => NarrationStageStatus.Failed,
            "canceled" => NarrationStageStatus.Failed,
            "skipped" => NarrationStageStatus.Skipped,
            _ => stages[stageIndex].Status
        };

        stages[stageIndex] = stages[stageIndex] with { Status = status, Duration = telemetry.Elapsed };

        if (string.Equals(telemetry.Stage, AttachmentIngestionStage, StringComparison.Ordinal) && _attachmentExpectedCount > 0)
        {
            if (status == NarrationStageStatus.Completed)
            {
                _attachmentCompletedCount = Math.Min(_attachmentExpectedCount, _attachmentCompletedCount + 1);
                if (_attachmentCompletedCount < _attachmentExpectedCount)
                {
                    stages[stageIndex] = stages[stageIndex] with { Status = NarrationStageStatus.Running };
                    _setTurn(current with { Stages = stages.ToImmutableArray() });
                    return;
                }
            }

            if (status == NarrationStageStatus.Failed)
            {
                _setTurn(current with { Stages = stages.ToImmutableArray() });
                return;
            }
        }

        if (status is NarrationStageStatus.Completed or NarrationStageStatus.Skipped)
        {
            AdvanceToNextStage(stages, stageIndex);
        }

        _setTurn(current with { Stages = stages.ToImmutableArray() });
    }

    private static void AdvanceToNextStage(NarrationStageView[] stages, int completedIndex)
    {
        var anyRunning = stages.Any(s => s.Status == NarrationStageStatus.Running);
        if (anyRunning) return;

        for (var i = completedIndex + 1; i < stages.Length; i++)
        {
            if (stages[i].Status == NarrationStageStatus.Pending)
            {
                stages[i] = stages[i] with { Status = NarrationStageStatus.Running };
                return;
            }
        }
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
            ? s with
            {
                Status = NarrationStageStatus.Failed,
                ErrorClass = error.ErrorClass.ToString(),
                ErrorMessage = error.Message
            }
            : s).ToImmutableArray();
        _setTurn(current with { Stages = updatedStages });
    }
}
