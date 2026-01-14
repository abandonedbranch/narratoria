namespace Narratoria.Pipeline.Transforms.Llm.Prompts;

public static class SummaryPromptBuilder
{
    public static string Build(string? priorSummary, string narrationText)
    {
        priorSummary ??= string.Empty;

        return $"{PromptTemplates.SummaryInstructions}\n\nPRIOR SUMMARY:\n\n\"\"\"\n{priorSummary}\n\"\"\"\n\nNEW NARRATION:\n\n\"\"\"\n{narrationText}\n\"\"\"\n";
    }
}
