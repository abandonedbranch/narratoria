using System.Collections.Immutable;
using System.Diagnostics;
using System.Diagnostics.Metrics;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Narratoria.Narration;

public sealed class ConfigSystemPromptProfileResolver : ISystemPromptProfileResolver
{
    private static readonly Meter Meter = new("Narratoria.Narration.SystemPrompt.Resolve");
    private static readonly Histogram<double> ResolveLatency = Meter.CreateHistogram<double>("system_prompt_resolve_ms");
    private static readonly Counter<long> ResolveCount = Meter.CreateCounter<long>("system_prompt_resolve_count");

    private readonly IOptions<SystemPromptProfileConfig> _config;
    private readonly ILogger<ConfigSystemPromptProfileResolver> _logger;

    public ConfigSystemPromptProfileResolver(IOptions<SystemPromptProfileConfig> config, ILogger<ConfigSystemPromptProfileResolver> logger)
    {
        _config = config ?? throw new ArgumentNullException(nameof(config));
        _logger = logger ?? Microsoft.Extensions.Logging.Abstractions.NullLogger<ConfigSystemPromptProfileResolver>.Instance;
    }

    public ValueTask<SystemPromptProfile?> ResolveAsync(Guid sessionId, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var sw = Stopwatch.StartNew();
        try
        {
            var value = _config.Value;
            if (string.IsNullOrWhiteSpace(value.PromptText))
            {
                _logger.LogWarning("system_prompt_resolver not_found session={SessionId}", sessionId);
                Record("not_found", "none", sw.Elapsed);
                return new ValueTask<SystemPromptProfile?>((SystemPromptProfile?)null);
            }

            var profile = new SystemPromptProfile(
                value.ProfileId,
                value.PromptText,
                value.Instructions.Where(s => !string.IsNullOrWhiteSpace(s)).ToImmutableArray(),
                value.Version);

            _logger.LogInformation("system_prompt_resolver success session={SessionId} profile_id={ProfileId} version={Version}", sessionId, profile.ProfileId, profile.Version);
            Record("success", "none", sw.Elapsed);
            return new ValueTask<SystemPromptProfile?>(profile);
        }
        catch (OperationCanceledException)
        {
            Record("cancelled", "none", sw.Elapsed);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "system_prompt_resolver error session={SessionId}", sessionId);
            Record("error", ex.GetType().Name, sw.Elapsed);
            return new ValueTask<SystemPromptProfile?>((SystemPromptProfile?)null);
        }
    }

    private static void Record(string status, string errorClass, TimeSpan elapsed)
    {
        var tags = new KeyValuePair<string, object?>[]
        {
            new("status", status),
            new("error_class", errorClass)
        };
        ResolveLatency.Record(elapsed.TotalMilliseconds, tags);
        ResolveCount.Add(1, tags);
    }
}
