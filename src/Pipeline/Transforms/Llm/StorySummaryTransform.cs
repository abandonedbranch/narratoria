using Microsoft.Extensions.Logging;
using Narratoria.Pipeline.Transforms.Llm.Prompts;
using Narratoria.Pipeline.Transforms.Llm.Providers;
using Narratoria.Pipeline.Transforms.Llm.StoryState;
using StoryStateModel = Narratoria.Pipeline.Transforms.Llm.StoryState.StoryState;

namespace Narratoria.Pipeline.Transforms.Llm;

public sealed class StorySummaryTransform : IPipelineTransform
{
    private const string TransformName = nameof(StorySummaryTransform);

    private readonly ITextGenerationService _service;
    private readonly ILogger<StorySummaryTransform> _logger;

    public StorySummaryTransform(ITextGenerationService service, ILogger<StorySummaryTransform> logger)
    {
        _service = service ?? throw new ArgumentNullException(nameof(service));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

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

            var prompt = SummaryPromptBuilder.Build(current.Summary, textChunk.Text);
            var generated = await LlmTransformErrorHandling.TryGenerateTextAsync(
                _service,
                new TextGenerationRequest { Prompt = prompt },
                _logger,
                TransformName,
                textChunk.Metadata,
                cancellationToken).ConfigureAwait(false);

            StoryStateModel nextState = current;

            if (!string.IsNullOrWhiteSpace(generated))
            {
                var update = new StoryStateUpdate { Summary = generated };
                nextState = StoryStateMerge.ApplyUpdate(current, update, TransformName, DateTimeOffset.UtcNow);
            }

            var updatedMetadata = StoryStateAnnotations.Write(textChunk.Metadata, nextState);
            runningState = nextState;
            yield return textChunk with { Metadata = updatedMetadata };
        }
    }
}
