namespace Narratoria.OpenAi;

public interface IOpenAiApiServiceMetrics
{
    void RecordRequest(string status, string errorClass);
    void RecordLatency(TimeSpan duration);
    void RecordBytesSent(long bytes);
    void RecordBytesReceived(long bytes);
}
