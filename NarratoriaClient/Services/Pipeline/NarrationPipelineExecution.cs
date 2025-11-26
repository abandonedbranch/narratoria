using System.Threading.Channels;

namespace NarratoriaClient.Services.Pipeline;

public sealed class NarrationPipelineExecution
{
    private readonly ChannelReader<NarrationLifecycleEvent> _reader;

    public NarrationPipelineExecution(ChannelReader<NarrationLifecycleEvent> reader, Task completionTask)
    {
        _reader = reader ?? throw new ArgumentNullException(nameof(reader));
        Completion = completionTask ?? throw new ArgumentNullException(nameof(completionTask));
    }

    public IAsyncEnumerable<NarrationLifecycleEvent> ReadEventsAsync(CancellationToken cancellationToken = default)
    {
        return _reader.ReadAllAsync(cancellationToken);
    }

    public Task Completion { get; }
}
