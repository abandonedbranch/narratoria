namespace NarratoriaClient.Services.Pipeline;

public interface INarrationPipeline
{
    NarrationPipelineExecution Execute(NarrationPipelineContext context, CancellationToken cancellationToken = default);
}

public interface INarrationPipelineStage
{
    string StageName { get; }

    int Order { get; }

    Task ExecuteAsync(NarrationPipelineContext context, CancellationToken cancellationToken);
}
