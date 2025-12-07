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

        if (!context.WorkingContextSegments.IsDefaultOrEmpty)
        {
            var segmentsBuilder = new StringBuilder();
            foreach (var segment in context.WorkingContextSegments)
            {
                if (string.IsNullOrWhiteSpace(segment.Content))
                {
                    continue;
                }

                segmentsBuilder.AppendLine(segment.Content);
                segmentsBuilder.AppendLine();
            }

            var payloadFromSegments = segmentsBuilder.ToString().TrimEnd();
            return new SerializedPrompt(Guid.NewGuid(), payloadFromSegments, context.Metadata);
        }

        var builder = new StringBuilder();
        if (!context.PriorNarration.IsDefaultOrEmpty)
        {
            foreach (var line in context.PriorNarration)
            {
                builder.AppendLine(line);
            }
            builder.AppendLine();
        }

        builder.AppendLine(context.PlayerPrompt ?? string.Empty);

        var payload = builder.ToString().TrimEnd();
        return new SerializedPrompt(Guid.NewGuid(), payload, context.Metadata);
    }
}
