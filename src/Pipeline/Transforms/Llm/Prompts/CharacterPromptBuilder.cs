namespace Narratoria.Pipeline.Transforms.Llm.Prompts;

public static class CharacterPromptBuilder
{
    public static string Build(string? summary, string narrationText)
    {
        summary ??= string.Empty;

        return $"{PromptTemplates.CharacterExtractionInstructions}\n\nOUTPUT JSON SHAPE:\n{{\n  \"charactersToUpsert\": [ /* CharacterRecord[] */ ],\n  \"inventoryUpdates\": null,\n  \"summary\": null\n}}\n\nCURRENT SUMMARY:\n\n\"\"\"\n{summary}\n\"\"\"\n\nNARRATION:\n\n\"\"\"\n{narrationText}\n\"\"\"\n";
    }
}
