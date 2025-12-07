using System.Text;
using Narratoria.OpenAi;

namespace Narratoria.Narration;

public interface INarrationPromptSerializer
{
    SerializedPrompt Serialize(NarrationContext context);
}

public sealed class BasicNarrationPromptSerializer : INarrationPromptSerializer
{
    public SerializedPrompt Serialize(NarrationContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var builder = new StringBuilder();
        if (!context.PriorNarration.IsDefaultOrEmpty)
        {
            builder.AppendLine("Previous narration:");
            foreach (var line in context.PriorNarration)
            {
                builder.AppendLine(line);
            }
            builder.AppendLine();
        }

        builder.AppendLine("Player prompt:");
        builder.AppendLine(context.PlayerPrompt ?? string.Empty);

        var payload = builder.ToString();
        return new SerializedPrompt(Guid.NewGuid(), payload, context.Metadata);
    }
}
