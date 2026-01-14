namespace Narratoria.Pipeline.Transforms.Llm.Prompts;

public static class RewritePromptBuilder
{
    public static string Build(string narrationText)
    {
        if (string.IsNullOrWhiteSpace(narrationText))
        {
            return PromptTemplates.RewriteInstructions;
        }

        return $"{PromptTemplates.RewriteInstructions}\n\nINPUT NARRATION:\n\n\"\"\"\n{narrationText}\n\"\"\"\n";
    }
}
