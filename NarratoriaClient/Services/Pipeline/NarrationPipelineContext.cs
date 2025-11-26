using System.Collections.Concurrent;
using NarratoriaClient.Services;

namespace NarratoriaClient.Services.Pipeline;

public sealed class NarrationPipelineContext
{
    private readonly Action<NarrationStatus, string>? _statusCallback;

    private readonly ConcurrentDictionary<string, object?> _items = new(StringComparer.Ordinal);

    public NarrationPipelineContext(string playerInput, Action<NarrationStatus, string>? statusCallback = null)
    {
        if (string.IsNullOrWhiteSpace(playerInput))
        {
            throw new ArgumentException("Player input must be provided.", nameof(playerInput));
        }

        PlayerInput = playerInput.Trim();
        CorrelationId = Guid.NewGuid();
        _statusCallback = statusCallback;
    }

    public Guid CorrelationId { get; }

    public string PlayerInput { get; }

    public string? NormalizedInput { get; set; }

    public IReadOnlyList<ChatPromptMessage>? PromptMessages { get; set; }

    public string? SelectedModel { get; set; }

    public string? GeneratedNarration { get; set; }

    public bool ShouldContinue { get; set; } = true;

    public string TargetWorkflow { get; set; } = "narrator";

    public bool IsCommand { get; set; }

    public string? CommandName { get; set; }

    public string? CommandArgs { get; set; }

    public IReadOnlyDictionary<string, object?> Items => _items;

    public void SetItem(string key, object? value)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            throw new ArgumentException("Key must not be empty.", nameof(key));
        }

        _items[key] = value;
    }

    public bool TryGetItem<T>(string key, out T? value)
    {
        if (_items.TryGetValue(key, out var stored) && stored is T typed)
        {
            value = typed;
            return true;
        }

        value = default;
        return false;
    }

    public void ReportStatus(NarrationStatus status, string message)
    {
        _statusCallback?.Invoke(status, message);
    }
}
