using Microsoft.Extensions.Logging;
using Narratoria.Pipeline.Transforms.Llm.Prompts;
using Narratoria.Pipeline.Transforms.Llm.Providers;
using Narratoria.Pipeline.Transforms.Llm.StoryState;

namespace Narratoria.Pipeline.Transforms.Llm;

public sealed class RewriteNarrationTransform : IPipelineTransform
{
    private const string TransformName = nameof(RewriteNarrationTransform);

    private readonly ITextGenerationService _service;
    private readonly ILogger<RewriteNarrationTransform> _logger;

    public RewriteNarrationTransform(ITextGenerationService service, ILogger<RewriteNarrationTransform> logger)
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
        await foreach (var chunk in input.WithCancellation(cancellationToken))
        {
            if (chunk is not TextChunk textChunk)
            {
                throw new PipelineStageException(PipelineFailureKind.TransformFailed, $"Expected text chunk, got '{chunk.Type}'");
            }

            var original = StoryStateAnnotations.TryGetAnnotation(textChunk.Metadata, StoryStateAnnotations.OriginalTextKey)
                ?? textChunk.Text;

            var updatedMetadata = textChunk.Metadata.WithAnnotation(StoryStateAnnotations.OriginalTextKey, original);

            var prompt = RewritePromptBuilder.Build(textChunk.Text);
            var generated = await LlmTransformErrorHandling.TryGenerateTextAsync(
                _service,
                new TextGenerationRequest { Prompt = prompt },
                _logger,
                TransformName,
                textChunk.Metadata,
                cancellationToken).ConfigureAwait(false);

            if (string.IsNullOrWhiteSpace(generated))
            {
                yield return new TextChunk(textChunk.Text, updatedMetadata);
                continue;
            }

            yield return new TextChunk(generated, updatedMetadata);
        }
    }
}
