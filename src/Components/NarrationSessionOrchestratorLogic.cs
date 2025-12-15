using System.Collections.Immutable;

namespace Narratoria.Components;

public static class NarrationSessionOrchestratorLogic
{
    public static NarrationPipelineTurnView CreateNewTurn(string prompt, IReadOnlyList<NarrationStageKind> stageOrder)
    {
        var stages = stageOrder.Select(kind => new NarrationStageView
        {
            Kind = kind,
            Status = kind == NarrationStageKind.Llm ? NarrationStageStatus.Running : NarrationStageStatus.Pending
        }).ToImmutableArray();

        return new NarrationPipelineTurnView
        {
            TurnId = Guid.NewGuid(),
            UserPrompt = prompt,
            PromptAt = DateTimeOffset.UtcNow,
            Stages = stages,
            Output = new NarrationOutputView { IsStreaming = true, StreamedSegments = ImmutableArray<string>.Empty }
        };
    }

    public static NarrationPipelineTurnView ApplyStreamSegment(NarrationPipelineTurnView turn, string segment)
    {
        var segments = turn.Output.StreamedSegments.Add(segment ?? string.Empty);
        return turn with { Output = turn.Output with { IsStreaming = true, StreamedSegments = segments } };
    }

    public static NarrationPipelineTurnView FinalizeTurn(NarrationPipelineTurnView turn, IEnumerable<string> allSegments, IReadOnlyList<NarrationStageKind> stageOrder)
    {
        var text = string.Concat(allSegments ?? Array.Empty<string>());
        var stages = turn.Stages.Select(s => s.Kind == NarrationStageKind.Llm
            ? new NarrationStageView { Kind = s.Kind, Status = NarrationStageStatus.Completed }
            : new NarrationStageView { Kind = s.Kind, Status = NarrationStageStatus.Completed }).ToImmutableArray();

        return turn with
        {
            Stages = stages,
            Output = new NarrationOutputView { IsStreaming = false, FinalText = text }
        };
    }
}
