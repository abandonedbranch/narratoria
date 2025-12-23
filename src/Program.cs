using System.Linq;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Narratoria.Components;
using Narratoria.Narration;
using Narratoria.Narration.Attachments;
using Narratoria.OpenAi;
using Narratoria.Storage;
using Narratoria.Storage.IndexedDb;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();
builder.Services.AddStorageQuotaAwareness();
builder.Services.AddOptions<NarrationOpenAiOptions>().Bind(builder.Configuration.GetSection("OpenAi"));
builder.Services.AddOptions<ProviderDispatchOptions>().Bind(builder.Configuration.GetSection("ProviderDispatch"));
builder.Services.AddOptions<NarrationOpenAiOptions>()
    .Validate(o => !string.IsNullOrWhiteSpace(o.ApiKey), "OpenAI ApiKey is required")
    .Validate(o => !string.IsNullOrWhiteSpace(o.Model), "OpenAI model is required")
    .ValidateOnStart();
builder.Services.AddOptions<ProviderDispatchOptions>()
    .Validate(o => o.Timeout > TimeSpan.Zero || o.Timeout == Timeout.InfiniteTimeSpan, "Provider timeout must be positive or infinite")
    .ValidateOnStart();
builder.Services.AddHttpClient("openai");

builder.Services.AddSingleton<IIndexedDbStorageMetrics, LoggingIndexedDbStorageMetrics>();
builder.Services.AddSingleton(sp =>
{
    var attachmentsStore = ProcessedAttachmentStore.CreateStoreDefinition();
    var uploadsStore = AttachmentUploadStore.CreateStoreDefinition();
    var contextStore = IndexedDbNarrationSessionStore.CreateContextStoreDefinition();
    var sessionsStore = IndexedDbNarrationSessionStore.CreateSessionsStoreDefinition();
    var turnsStore = IndexedDbNarrationSessionStore.CreateTurnsStoreDefinition();
    return new IndexedDbSchema
    {
        DatabaseName = "narratoria",
        Version = 3,
        Stores = new[] { attachmentsStore, uploadsStore, contextStore, sessionsStore, turnsStore }
    };
});
builder.Services.AddIndexedDbStorageService();

builder.Services.AddScoped<IIndexedDbValueSerializer<NarrationContext>, NarrationContextSerializer>();
builder.Services.AddScoped<IIndexedDbValueSerializer<SessionRecord>, SessionRecordSerializer>();
builder.Services.AddScoped<IIndexedDbValueSerializer<NarrationTurnRecord>, NarrationTurnRecordSerializer>();
builder.Services.AddScoped<IIndexedDbValueSerializer<UploadedFile>, UploadedFileSerializer>();
builder.Services.AddScoped<IIndexedDbValueSerializer<ProcessedAttachment>, ProcessedAttachmentSerializer>();
builder.Services.AddScoped<INarrationSessionStore>(sp =>
{
    var storage = sp.GetRequiredService<IIndexedDbStorageService>();
    var quotaStorage = sp.GetRequiredService<IIndexedDbStorageWithQuota>();
    var schema = sp.GetRequiredService<IndexedDbSchema>();
    var context = schema.Stores.Single(s => s.Name == "narration_sessions");
    var sessions = schema.Stores.Single(s => s.Name == "sessions");
    var turns = schema.Stores.Single(s => s.Name == "turns");
    var serializer = sp.GetRequiredService<IIndexedDbValueSerializer<NarrationContext>>();
    var sessionSerializer = sp.GetRequiredService<IIndexedDbValueSerializer<SessionRecord>>();
    var turnSerializer = sp.GetRequiredService<IIndexedDbValueSerializer<NarrationTurnRecord>>();
    var logger = sp.GetRequiredService<ILogger<IndexedDbNarrationSessionStore>>();
    var scope = new StorageScope(schema.DatabaseName, context.Name);
    return new IndexedDbNarrationSessionStore(storage, quotaStorage, context, sessions, turns, scope, logger, serializer, sessionSerializer, turnSerializer);
});

builder.Services.AddScoped<IAttachmentUploadStore>(sp =>
{
    var storage = sp.GetRequiredService<IIndexedDbStorageService>();
    var quotaStorage = sp.GetRequiredService<IIndexedDbStorageWithQuota>();
    var schema = sp.GetRequiredService<IndexedDbSchema>();
    var store = schema.Stores.Single(s => s.Name == "attachment_uploads");
    var serializer = sp.GetRequiredService<IIndexedDbValueSerializer<UploadedFile>>();
    var logger = sp.GetRequiredService<ILogger<AttachmentUploadStore>>();
    var scope = new StorageScope(schema.DatabaseName, store.Name);
    return new AttachmentUploadStore(storage, quotaStorage, store, scope, logger, serializer);
});

builder.Services.AddScoped<IProcessedAttachmentStore>(sp =>
{
    var storage = sp.GetRequiredService<IIndexedDbStorageService>();
    var quotaStorage = sp.GetRequiredService<IIndexedDbStorageWithQuota>();
    var schema = sp.GetRequiredService<IndexedDbSchema>();
    var store = schema.Stores.Single(s => s.Name == "attachments");
    var serializer = sp.GetRequiredService<IIndexedDbValueSerializer<ProcessedAttachment>>();
    var logger = sp.GetRequiredService<ILogger<ProcessedAttachmentStore>>();
    var scope = new StorageScope(schema.DatabaseName, store.Name);
    return new ProcessedAttachmentStore(storage, quotaStorage, store, scope, logger, serializer);
});

builder.Services.AddSingleton<IAttachmentIngestionMetrics>(_ => NullAttachmentIngestionMetrics.Instance);
builder.Services.AddSingleton<IAttachmentOpenAiContextFactory, AttachmentOpenAiContextFactory>();
builder.Services.AddSingleton(AttachmentIngestionOptions.Default);
builder.Services.AddScoped<IAttachmentIngestionService, AttachmentIngestionService>();
builder.Services.AddSingleton<INarrationPipelineObserver>(_ => NullNarrationPipelineObserver.Instance);

builder.Services.AddSingleton<IOpenAiApiService, OpenAiApiService>();
builder.Services.AddSingleton<IOpenAiApiServiceMetrics, LoggingOpenAiApiServiceMetrics>();
builder.Services.AddSingleton<IOpenAiStreamingProvider>(sp =>
{
    var opts = sp.GetRequiredService<IOptions<NarrationOpenAiOptions>>().Value;
    var credentials = new OpenAiProviderCredentials(opts.ApiKey);
    var endpoint = new Uri(opts.Endpoint);
    var headers = new Dictionary<string, string>();
    if (!string.IsNullOrWhiteSpace(opts.OrganizationId))
    {
        headers["OpenAI-Organization"] = opts.OrganizationId!;
    }
    if (!string.IsNullOrWhiteSpace(opts.ProjectId))
    {
        headers["OpenAI-Project"] = opts.ProjectId!;
    }
    return new OpenAiChatStreamingProvider(opts.Model, credentials, endpoint, headers);
});
builder.Services.AddSingleton<INarrationPromptSerializer, BasicNarrationPromptSerializer>();
builder.Services.AddSingleton<INarrationOpenAiContextFactory, NarrationOpenAiContextFactory>();
builder.Services.AddSingleton<INarrationProvider, OpenAiNarrationProvider>();
builder.Services.AddSingleton<NarrationContentGuardianMiddleware>();
builder.Services.AddSingleton<NarrationSystemPromptMiddleware>();
builder.Services.AddOptions<SystemPromptProfileConfig>().Bind(builder.Configuration.GetSection("SystemPromptProfile")).Validate(config =>
    !string.IsNullOrWhiteSpace(config.ProfileId) && !string.IsNullOrWhiteSpace(config.PromptText) && !string.IsNullOrWhiteSpace(config.Version),
    "SystemPromptProfile requires ProfileId, PromptText, Version").ValidateOnStart();
builder.Services.AddSingleton<ISystemPromptProfileResolver, ConfigSystemPromptProfileResolver>();
builder.Services.AddSingleton<ISessionService, SessionService>();
builder.Services.AddSingleton(new SessionTitleOptions { MaxChars = 64 });

builder.Services.AddSingleton<ProviderDispatchMiddleware>(sp =>
{
    var provider = sp.GetRequiredService<INarrationProvider>();
    var observer = sp.GetRequiredService<INarrationPipelineObserver>();
    var options = sp.GetRequiredService<IOptions<ProviderDispatchOptions>>().Value;
    // If ProviderDispatchOptions.Model is not set, default to OpenAi options model
    if (string.IsNullOrWhiteSpace(options.Model))
    {
        var openAi = sp.GetRequiredService<IOptions<NarrationOpenAiOptions>>().Value;
        options = options with { Model = openAi.Model };
    }
    var logger = sp.GetRequiredService<ILogger<ProviderDispatchMiddleware>>();
    return new ProviderDispatchMiddleware(provider, options, observer, logger);
});
builder.Services.AddSingleton<NarrationPersistenceMiddleware>(sp =>
    new NarrationPersistenceMiddleware(sp.GetRequiredService<INarrationSessionStore>(), sp.GetRequiredService<INarrationPipelineObserver>()));
builder.Services.AddSingleton<NarrationSessionTitleMiddleware>(sp =>
    {
        var pdOptions = sp.GetRequiredService<IOptions<ProviderDispatchOptions>>().Value;
        if (string.IsNullOrWhiteSpace(pdOptions.SystemModel))
        {
            var openAi = sp.GetRequiredService<IOptions<NarrationOpenAiOptions>>().Value;
            pdOptions = pdOptions with { SystemModel = openAi.Model };
        }
        return new NarrationSessionTitleMiddleware(
            sp.GetRequiredService<INarrationSessionStore>(),
            sp.GetRequiredService<Narratoria.OpenAi.IOpenAiApiService>(),
            sp.GetRequiredService<INarrationOpenAiContextFactory>(),
            sp.GetRequiredService<INarrationPipelineObserver>(),
            TitleOptions.Default,
            sp.GetRequiredService<ILogger<NarrationSessionTitleMiddleware>>(),
            pdOptions);
    });
builder.Services.AddSingleton<NarrationPipelineService>(sp =>
{
    var persistence = sp.GetRequiredService<NarrationPersistenceMiddleware>();
    var systemPrompt = sp.GetRequiredService<NarrationSystemPromptMiddleware>();
    var guardian = sp.GetRequiredService<NarrationContentGuardianMiddleware>();
    var dispatch = sp.GetRequiredService<ProviderDispatchMiddleware>();
    var titleUpdate = sp.GetRequiredService<NarrationSessionTitleMiddleware>();
    return new NarrationPipelineService(new NarrationMiddleware[]
    {
        persistence.InvokeAsync,
        systemPrompt.InvokeAsync,
        guardian.InvokeAsync,
        dispatch.InvokeAsync,
        titleUpdate.InvokeAsync
    });
});

builder.Services.AddScoped<INarrationPipelineFactory>(sp =>
{
    var sessions = sp.GetRequiredService<INarrationSessionStore>();
    var profiles = sp.GetRequiredService<ISystemPromptProfileResolver>();
    var provider = sp.GetRequiredService<INarrationProvider>();

    var options = sp.GetRequiredService<IOptions<ProviderDispatchOptions>>().Value;
    if (string.IsNullOrWhiteSpace(options.Model))
    {
        var openAi = sp.GetRequiredService<IOptions<NarrationOpenAiOptions>>().Value;
        options = options with { Model = openAi.Model };
    }

    var loggerFactory = sp.GetRequiredService<ILoggerFactory>();
    var processed = sp.GetRequiredService<IProcessedAttachmentStore>();
    return new NarrationPipelineFactory(sessions, profiles, provider, options, loggerFactory, processed);
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}
app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();

app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
