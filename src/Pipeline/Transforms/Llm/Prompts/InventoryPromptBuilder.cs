namespace Narratoria.Pipeline.Transforms.Llm.Prompts;

public static class InventoryPromptBuilder
{
    public static string Build(string? summary, string narrationText)
    {
        summary ??= string.Empty;

        return $"{PromptTemplates.InventoryExtractionInstructions}\n\nOUTPUT JSON SHAPE:\n{{\n  \"charactersToUpsert\": null,\n  \"inventoryUpdates\": [ /* InventoryItemUpdate[] */ ],\n  \"summary\": null\n}}\n\nCURRENT SUMMARY:\n\n\"\"\"\n{summary}\n\"\"\"\n\nNARRATION:\n\n\"\"\"\n{narrationText}\n\"\"\"\n";
    }
}
