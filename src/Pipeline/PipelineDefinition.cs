namespace Narratoria.Pipeline;

public sealed record PipelineDefinition<TSinkResult>(
    IPipelineSource Source,
    IReadOnlyList<IPipelineTransform> Transforms,
    IPipelineSink<TSinkResult> Sink);
