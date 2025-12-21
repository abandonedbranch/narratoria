using Narratoria.Components;
using Narratoria.OpenAi;

namespace Narratoria.Narration;

public sealed record NarrationPipelineBuildRequest(
    Guid SessionId,
    Guid TurnId,
    TraceMetadata Trace,
    IReadOnlyList<NarrationStageKind> StageOrder,
    IReadOnlyList<string> AttachmentIds,
    INarrationPipelineObserver Observer,
    IStageMetadataProvider? StageMetadata);

public interface INarrationPipelineFactory
{
    NarrationPipelineService Create(NarrationPipelineBuildRequest request);
}

public sealed class NarrationPipelineFactory : INarrationPipelineFactory
{
    private readonly INarrationSessionStore _sessions;
    private readonly ISystemPromptProfileResolver _systemPromptProfiles;
    private readonly INarrationProvider _provider;
    private readonly ProviderDispatchOptions _dispatchOptions;
    private readonly ILoggerFactory _loggerFactory;
    private readonly Narratoria.Narration.Attachments.IProcessedAttachmentStore _processedAttachments;

    public NarrationPipelineFactory(
        INarrationSessionStore sessions,
        ISystemPromptProfileResolver systemPromptProfiles,
        INarrationProvider provider,
        ProviderDispatchOptions dispatchOptions,
        ILoggerFactory loggerFactory,
        Narratoria.Narration.Attachments.IProcessedAttachmentStore processedAttachments)
    {
        _sessions = sessions ?? throw new ArgumentNullException(nameof(sessions));
        _systemPromptProfiles = systemPromptProfiles ?? throw new ArgumentNullException(nameof(systemPromptProfiles));
        _provider = provider ?? throw new ArgumentNullException(nameof(provider));
        _dispatchOptions = dispatchOptions ?? throw new ArgumentNullException(nameof(dispatchOptions));
        _loggerFactory = loggerFactory ?? throw new ArgumentNullException(nameof(loggerFactory));
        _processedAttachments = processedAttachments ?? throw new ArgumentNullException(nameof(processedAttachments));
    }

    public NarrationPipelineService Create(NarrationPipelineBuildRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (request.SessionId == Guid.Empty) throw new ArgumentException("SessionId is required.", nameof(request));
        if (request.TurnId == Guid.Empty) throw new ArgumentException("TurnId is required.", nameof(request));
        if (request.StageOrder is null || request.StageOrder.Count == 0) throw new ArgumentException("StageOrder is required.", nameof(request));
        if (request.Observer is null) throw new ArgumentException("Observer is required.", nameof(request));

        var observer = request.Observer;
        var stageMetadata = request.StageMetadata;

        var persistence = new NarrationPersistenceMiddleware(_sessions, observer);
        var systemPrompt = new NarrationSystemPromptMiddleware(_systemPromptProfiles, observer, _loggerFactory.CreateLogger<NarrationSystemPromptMiddleware>());
        var guardian = new NarrationContentGuardianMiddleware(observer);

        var options = _dispatchOptions;
        var logger = _loggerFactory.CreateLogger<ProviderDispatchMiddleware>();
        var dispatch = new ProviderDispatchMiddleware(_provider, options, observer, logger, stageMetadata);

        var middleware = new List<NarrationMiddleware>
        {
            persistence.InvokeAsync,
            systemPrompt.InvokeAsync,
            guardian.InvokeAsync
        };

        var injection = new Narratoria.Narration.Attachments.AttachmentContextInjectionMiddleware(
            _processedAttachments,
            request.AttachmentIds ?? Array.Empty<string>(),
            observer);
        middleware.Add(injection.InvokeAsync);

        middleware.Add(dispatch.InvokeAsync);

        return new NarrationPipelineService(middleware);
    }
}
