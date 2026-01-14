using Microsoft.Extensions.Logging;
using Narratoria.Pipeline.Transforms.Llm.Prompts;
using Narratoria.Pipeline.Transforms.Llm.Providers;
using Narratoria.Pipeline.Transforms.Llm.StoryState;
using StoryStateModel = Narratoria.Pipeline.Transforms.Llm.StoryState.StoryState;

namespace Narratoria.Pipeline.Transforms.Llm;

public sealed class InventoryTrackerTransform(ITextGenerationService service, ILogger<InventoryTrackerTransform> logger) : IPipelineTransform
{
    private const string TransformName = nameof(InventoryTrackerTransform);

    private readonly ITextGenerationService _service = service ?? throw new ArgumentNullException(nameof(service));
    private readonly ILogger<InventoryTrackerTransform> _logger = logger ?? throw new ArgumentNullException(nameof(logger));

    public PipelineChunkType InputType => PipelineChunkType.Text;

    public PipelineChunkType OutputType => PipelineChunkType.Text;

    public async IAsyncEnumerable<PipelineChunk> TransformAsync(
        IAsyncEnumerable<PipelineChunk> input,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken)
    {
        StoryStateModel? runningState = null;

        await foreach (var chunk in input.WithCancellation(cancellationToken))
        {
            if (chunk is not TextChunk textChunk)
            {
                throw new PipelineStageException(PipelineFailureKind.TransformFailed, $"Expected text chunk, got '{chunk.Type}'");
            }

            var incomingState = StoryStateAnnotations.ReadOrCreate(textChunk.Metadata, TransformName);
            var current = runningState is null || incomingState.Version > runningState.Version
                ? incomingState
                : runningState;

            var prompt = InventoryPromptBuilder.Build(current.Summary, textChunk.Text);
            var generated = await LlmTransformErrorHandling.TryGenerateTextAsync(
                _service,
                new TextGenerationRequest { Prompt = prompt },
                _logger,
                TransformName,
                textChunk.Metadata,
                cancellationToken).ConfigureAwait(false);

            StoryStateModel nextState = current;

            if (!string.IsNullOrWhiteSpace(generated) && StoryStateUpdateJson.TryDeserialize(generated, out var update) && update is not null)
            {
                nextState = StoryStateMerge.ApplyUpdate(current, update, TransformName, DateTimeOffset.UtcNow);
            }
            else if (!string.IsNullOrWhiteSpace(generated))
            {
                _logger.LogWarning("{Transform} could not parse JSON update. {Context}", TransformName, LlmTransformErrorHandling.FormatContext(textChunk.Metadata));
            }

            var updatedMetadata = StoryStateAnnotations.Write(textChunk.Metadata, nextState);
            runningState = nextState;
            yield return textChunk with { Metadata = updatedMetadata };
        }
    }
}
