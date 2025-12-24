using System.Collections.Immutable;
using Narratoria.Narration;

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

    public static ImmutableArray<NarrationStageSnapshot> ToSnapshots(ImmutableArray<NarrationStageView> views)
    {
        var list = new List<NarrationStageSnapshot>(views.Length);
        foreach (var v in views)
        {
            list.Add(new NarrationStageSnapshot
            {
                StageId = v.Kind.Name,
                Status = v.Status.ToString(),
                Duration = v.Duration,
                ErrorClass = v.ErrorClass,
                ErrorMessage = v.ErrorMessage,
                PromptTokens = v.PromptTokens,
                CompletionTokens = v.CompletionTokens,
                Model = v.Model
            });
        }
        return list.ToImmutableArray();
    }

    public static NarrationPipelineTurnView FromRecord(NarrationTurnRecord record)
    {
        var stageViews = record.StageOrder
            .Select(name =>
            {
                var snap = record.Stages.FirstOrDefault(s => string.Equals(s.StageId, name, StringComparison.Ordinal));
                var status = snap is null ? NarrationStageStatus.Pending : Enum.TryParse<NarrationStageStatus>(snap.Status, out var st) ? st : NarrationStageStatus.Pending;
                return new NarrationStageView
                {
                    Kind = NarrationStageKind.Custom(name),
                    Status = status,
                    Duration = snap?.Duration,
                    ErrorClass = snap?.ErrorClass,
                    ErrorMessage = snap?.ErrorMessage,
                    PromptTokens = snap?.PromptTokens,
                    CompletionTokens = snap?.CompletionTokens,
                    Model = snap?.Model
                };
            })
            .ToImmutableArray();

        return new NarrationPipelineTurnView
        {
            TurnId = record.TurnId,
            UserPrompt = record.Prompt,
            PromptAt = record.CreatedAt,
            Stages = stageViews,
            Output = new NarrationOutputView
            {
                IsStreaming = false,
                FinalText = string.Concat(record.OutputSegments)
            }
        };
    }
}
