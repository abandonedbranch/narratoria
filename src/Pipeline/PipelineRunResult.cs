namespace Narratoria.Pipeline;

public sealed record PipelineRunResult<TSinkResult>(PipelineOutcome Outcome, TSinkResult SinkResult);
