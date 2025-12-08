using System.Collections.Immutable;
using System.Diagnostics;
using System.Diagnostics.Metrics;
using Microsoft.Extensions.Logging;

namespace Narratoria.Narration;

public interface ISystemPromptProfileResolver
{
    ValueTask<SystemPromptProfile?> ResolveAsync(Guid sessionId, CancellationToken cancellationToken);
}

public sealed record SystemPromptProfile(
    string ProfileId,
    string PromptText,
    ImmutableArray<string> Instructions,
    string Version);

public sealed class NarrationSystemPromptMiddleware
{
    private const string Stage = "system_prompt_injection";
    private const string Source = "system_prompt_middleware";
    private const string MetadataKeyProfileId = "system_prompt_profile_id";
    private const string MetadataKeyVersion = "system_prompt_version";

    private static readonly Meter Meter = new("Narratoria.Narration.SystemPrompt");
    private static readonly Histogram<double> InjectionLatency = Meter.CreateHistogram<double>("system_prompt_injection_latency_ms");
    private static readonly Counter<long> InjectionCount = Meter.CreateCounter<long>("system_prompt_injection_count");

    private readonly ISystemPromptProfileResolver _resolver;
    private readonly INarrationPipelineObserver _observer;
    private readonly ILogger<NarrationSystemPromptMiddleware> _logger;

    public NarrationSystemPromptMiddleware(
        ISystemPromptProfileResolver resolver,
        INarrationPipelineObserver? observer = null,
        ILogger<NarrationSystemPromptMiddleware>? logger = null)
    {
        _resolver = resolver ?? throw new ArgumentNullException(nameof(resolver));
        _observer = observer ?? NullNarrationPipelineObserver.Instance;
        _logger = logger ?? Microsoft.Extensions.Logging.Abstractions.NullLogger<NarrationSystemPromptMiddleware>.Instance;
    }

    public ValueTask<MiddlewareResult> InvokeAsync(NarrationContext context, MiddlewareResult result, NarrationMiddlewareNext next, CancellationToken cancellationToken)
    {
        return InvokeInternalAsync();

        async ValueTask<MiddlewareResult> InvokeInternalAsync()
        {
            cancellationToken.ThrowIfCancellationRequested();
            var stopwatch = Stopwatch.StartNew();

            if (context.WorkingContextSegments.IsDefault)
            {
                var error = new NarrationPipelineError(NarrationPipelineErrorClass.ContextError, "WorkingContextSegments is unavailable", context.SessionId, context.Trace, Stage);
                _observer.OnError(error);
                _observer.OnStageCompleted(new NarrationStageTelemetry(Stage, "failure", NarrationPipelineErrorClass.ContextError.ToString(), context.SessionId, context.Trace, stopwatch.Elapsed));
                RecordMetrics("failure", NarrationPipelineErrorClass.ContextError.ToString(), profileId: null, version: null, stopwatch.Elapsed);
                throw new NarrationPipelineException(error);
            }

            var profile = await _resolver.ResolveAsync(context.SessionId, cancellationToken).ConfigureAwait(false);
            cancellationToken.ThrowIfCancellationRequested();

            if (profile == null || string.IsNullOrWhiteSpace(profile.PromptText))
            {
                var error = new NarrationPipelineError(NarrationPipelineErrorClass.ContextError, "System prompt profile unavailable or prompt text is empty", context.SessionId, context.Trace, Stage);
                _observer.OnError(error);
                _observer.OnStageCompleted(new NarrationStageTelemetry(Stage, "failure", NarrationPipelineErrorClass.ContextError.ToString(), context.SessionId, context.Trace, stopwatch.Elapsed));
                RecordMetrics("failure", NarrationPipelineErrorClass.ContextError.ToString(), profileId: null, version: null, stopwatch.Elapsed);
                throw new NarrationPipelineException(error);
            }

            var metadata = AsImmutable(context.Metadata);
            if (metadata.TryGetValue(MetadataKeyProfileId, out var existingProfileId) &&
                metadata.TryGetValue(MetadataKeyVersion, out var existingVersion) &&
                string.Equals(existingProfileId, profile.ProfileId, StringComparison.Ordinal) &&
                string.Equals(existingVersion, profile.Version, StringComparison.Ordinal))
            {
                _logger.LogInformation(
                    "Narration system prompt skipped trace={TraceId} request={RequestId} session={SessionId} profile_id={ProfileId} version={Version}",
                    context.Trace.TraceId,
                    context.Trace.RequestId,
                    context.SessionId,
                    profile.ProfileId,
                    profile.Version);
                _observer.OnStageCompleted(new NarrationStageTelemetry(Stage, "skipped", "none", context.SessionId, context.Trace, stopwatch.Elapsed));
                RecordMetrics("skipped", "none", profile.ProfileId, profile.Version, stopwatch.Elapsed);
                return await next(context, result, cancellationToken).ConfigureAwait(false);
            }

            var baselineSegments = context.WorkingContextSegments.IsDefaultOrEmpty
                ? ImmutableArray<ContextSegment>.Empty
                : context.WorkingContextSegments;

            var builder = ImmutableArray.CreateBuilder<ContextSegment>();
            builder.Add(new ContextSegment("system", profile.PromptText, Source));

            foreach (var instruction in profile.Instructions)
            {
                if (!string.IsNullOrWhiteSpace(instruction))
                {
                    builder.Add(new ContextSegment("instruction", instruction, Source));
                }
            }

            foreach (var segment in baselineSegments)
            {
                builder.Add(segment);
            }

            var updatedSegments = builder.ToImmutable();
            var updatedMetadata = metadata
                .SetItem(MetadataKeyProfileId, profile.ProfileId)
                .SetItem(MetadataKeyVersion, profile.Version);

            var updatedContext = context with
            {
                WorkingContextSegments = updatedSegments,
                Metadata = updatedMetadata
            };

            _logger.LogInformation(
                "Narration system prompt injected trace={TraceId} request={RequestId} session={SessionId} profile_id={ProfileId} version={Version}",
                context.Trace.TraceId,
                context.Trace.RequestId,
                context.SessionId,
                profile.ProfileId,
                profile.Version);
            _observer.OnStageCompleted(new NarrationStageTelemetry(Stage, "success", "none", context.SessionId, context.Trace, stopwatch.Elapsed));
            RecordMetrics("success", "none", profile.ProfileId, profile.Version, stopwatch.Elapsed);
            return await next(updatedContext, result, cancellationToken).ConfigureAwait(false);
        }
    }

    private static ImmutableDictionary<string, string> AsImmutable(IReadOnlyDictionary<string, string> metadata)
    {
        return metadata as ImmutableDictionary<string, string> ?? metadata.ToImmutableDictionary(StringComparer.Ordinal);
    }

    private static void RecordMetrics(string status, string errorClass, string? profileId, string? version, TimeSpan elapsed)
    {
        var tags = new List<KeyValuePair<string, object?>>(5)
        {
            new("status", status),
            new("error_class", errorClass)
        };

        if (!string.IsNullOrEmpty(profileId))
        {
            tags.Add(new KeyValuePair<string, object?>("profile_id", profileId));
        }

        if (!string.IsNullOrEmpty(version))
        {
            tags.Add(new KeyValuePair<string, object?>("profile_version", version));
        }

        InjectionLatency.Record(elapsed.TotalMilliseconds, tags.ToArray());
        InjectionCount.Add(1, tags.ToArray());
    }
}
