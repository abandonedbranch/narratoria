using System.Collections.Immutable;
using System.Linq;
using System.Text;

namespace Narratoria.Components;

public static class NarrationPipelineLogLogic
{
    public static bool IsStageOrderValid(IReadOnlyList<NarrationStageKind> stageOrder)
    {
        if (stageOrder.Count == 0) return false;
        return stageOrder.Count == stageOrder.Distinct().Count();
    }

    public static bool AreTurnsAligned(IReadOnlyList<NarrationStageKind> stageOrder, IReadOnlyList<NarrationPipelineTurnView> turns)
    {
        if (stageOrder.Count == 0) return false;
        var orderSet = stageOrder.ToHashSet();
        foreach (var turn in turns)
        {
            if (turn.Stages.Length != stageOrder.Count) return false;
            if (!orderSet.SetEquals(turn.Stages.Select(s => s.Kind))) return false;
        }
        return true;
    }

    public static bool HasStreamMismatch(IReadOnlyList<NarrationPipelineTurnView> turns)
    {
        if (turns.Count <= 1) return false;
        for (var i = 0; i < turns.Count - 1; i++)
        {
            if (turns[i].Output.IsStreaming || turns[i].Stages.Any(s => s.Status == NarrationStageStatus.Running)) return true;
        }
        return false;
    }

    public static string RenderOutput(NarrationOutputView output)
    {
        if (!string.IsNullOrWhiteSpace(output.FinalText)) return output.FinalText!;
        if (output.StreamedSegments.Length == 0) return output.IsStreaming ? "..." : string.Empty;
        var builder = new StringBuilder();
        foreach (var segment in output.StreamedSegments) builder.Append(segment);
        if (output.IsStreaming) builder.Append(" ...");
        return builder.ToString();
    }
}
