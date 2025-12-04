using Narratoria.OpenAi;

namespace Narratoria.Narration;

public enum NarrationPipelineErrorClass
{
    ProviderError,
    MissingSession,
    DecodeError
}

public sealed record NarrationPipelineError(
    NarrationPipelineErrorClass ErrorClass,
    string Message,
    Guid SessionId,
    TraceMetadata Trace,
    string Stage);

public sealed class NarrationPipelineException : Exception
{
    public NarrationPipelineException(NarrationPipelineError error, Exception? innerException = null)
        : base(error.Message, innerException)
    {
        Error = error;
    }

    public NarrationPipelineError Error { get; }
}
