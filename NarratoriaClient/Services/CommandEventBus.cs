namespace NarratoriaClient.Services;

public sealed record CommandEvent(
    string Token,
    string? Args,
    string Author,
    DateTimeOffset Timestamp,
    bool IsError = false,
    string? Message = null,
    string? SessionId = null);

public interface ICommandEventBus
{
    event EventHandler<CommandEvent>? CommandReceived;
    void Publish(CommandEvent commandEvent);
}

public sealed class CommandEventBus : ICommandEventBus
{
    public event EventHandler<CommandEvent>? CommandReceived;

    public void Publish(CommandEvent commandEvent)
    {
        CommandReceived?.Invoke(this, commandEvent);
    }
}
