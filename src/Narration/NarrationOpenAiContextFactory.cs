using Microsoft.Extensions.Options;
using Narratoria.OpenAi;

namespace Narratoria.Narration;

public interface INarrationOpenAiContextFactory
{
    OpenAiRequestContext Create(NarrationContext context);
}

public sealed class NarrationOpenAiContextFactory : INarrationOpenAiContextFactory
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IOptions<NarrationOpenAiOptions> _options;
    private readonly IOpenAiApiServiceMetrics _metrics;
    private readonly ILoggerFactory _loggerFactory;
    private readonly IOpenAiStreamingProvider _streamingProvider;

    public NarrationOpenAiContextFactory(
        IHttpClientFactory httpClientFactory,
        IOptions<NarrationOpenAiOptions> options,
        IOpenAiApiServiceMetrics metrics,
        ILoggerFactory loggerFactory,
        IOpenAiStreamingProvider streamingProvider)
    {
        _httpClientFactory = httpClientFactory ?? throw new ArgumentNullException(nameof(httpClientFactory));
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _metrics = metrics ?? throw new ArgumentNullException(nameof(metrics));
        _loggerFactory = loggerFactory ?? throw new ArgumentNullException(nameof(loggerFactory));
        _streamingProvider = streamingProvider ?? throw new ArgumentNullException(nameof(streamingProvider));
    }

    public OpenAiRequestContext Create(NarrationContext context)
    {
        var opts = _options.Value ?? new NarrationOpenAiOptions();
        var client = _httpClientFactory.CreateClient("openai");
        var endpoint = new Uri(opts.Endpoint);
        var credentials = new OpenAiProviderCredentials(opts.ApiKey);
        var policy = new OpenAiRequestPolicy(opts.Timeout, opts.Idempotent);
        var logger = _loggerFactory.CreateLogger<NarrationOpenAiContextFactory>();
        var headers = CreateHeaders(opts);

        return new OpenAiRequestContext(
            client,
            endpoint,
            credentials,
            policy,
            logger,
            _metrics,
            context.Trace,
            _streamingProvider,
            headers);
    }

    private static IReadOnlyDictionary<string, string>? CreateHeaders(NarrationOpenAiOptions opts)
    {
        var headers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        if (!string.IsNullOrWhiteSpace(opts.OrganizationId))
        {
            headers["OpenAI-Organization"] = opts.OrganizationId;
        }

        if (!string.IsNullOrWhiteSpace(opts.ProjectId))
        {
            headers["OpenAI-Project"] = opts.ProjectId;
        }

        return headers.Count == 0 ? null : headers;
    }
}
