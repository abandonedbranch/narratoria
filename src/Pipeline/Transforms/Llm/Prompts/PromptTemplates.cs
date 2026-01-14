namespace Narratoria.Pipeline.Transforms.Llm.Prompts;

public static class PromptTemplates
{
    public const string RewriteInstructions =
        "You are a rewriting assistant for a narrated interactive story. " +
        "Rewrite the input narration to be grammatically correct and narration-ready while preserving meaning. " +
        "Make minimal changes when the text is already good. " +
        "Return ONLY the rewritten narration text, with no additional commentary.";

    public const string SummaryInstructions =
        "You are a story summarizer. Given the prior summary (may be empty) and new narration text, " +
        "produce an updated rolling summary that preserves important prior context and adds new key events. " +
        "Return ONLY the updated summary text.";

    public const string CharacterExtractionInstructions =
        "You extract and update a structured character roster from story narration. " +
        "Return ONLY valid JSON matching the requested schema. Do not invent characters or facts; " +
        "if uncertain, set confidence low and include a supporting sourceSnippet.";

    public const string InventoryExtractionInstructions =
        "You extract and update the player's inventory from story narration. " +
        "Return ONLY valid JSON matching the requested schema. Do not invent items; " +
        "if uncertain, set confidence low and include a supporting sourceSnippet.";
}
