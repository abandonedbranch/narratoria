using System.Collections.Immutable;

namespace Narratoria.Components;

public static class NarrationSessionOrchestratorLogic
{
    public static NarrationPipelineTurnView CreateNewTurn(string prompt, IReadOnlyList<NarrationStageKind> stageOrder, int expectedAttachmentCount)
    {
        var stages = stageOrder.Select((kind, index) => new NarrationStageView
        {
            Kind = kind,
            Status = kind.Name == "attachment_ingestion" && expectedAttachmentCount <= 0
                ? NarrationStageStatus.Skipped
                : index == 0 ? NarrationStageStatus.Running : NarrationStageStatus.Pending
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

    public static NarrationPipelineTurnView FinalizeTurn(NarrationPipelineTurnView turn, IEnumerable<string> allSegments)
    {
        var text = string.Concat(allSegments ?? Array.Empty<string>());
        return turn with
        {
            Output = new NarrationOutputView { IsStreaming = false, FinalText = text }
        };
    }
}
