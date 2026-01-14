using Microsoft.VisualStudio.TestTools.UnitTesting;
using Narratoria.Pipeline;
using Narratoria.Pipeline.Transforms.Llm;
using Narratoria.Pipeline.Transforms.Llm.Providers;

namespace Narratoria.Tests.Pipeline.Llm;

[TestClass]
public sealed class LoggingTests
{
    [TestMethod]
    public async Task RewriteTransform_OnProviderFailure_LogsTransformAndContext()
    {
        var logger = new CapturingLogger<RewriteNarrationTransform>();
        var service = new ThrowingService(new InvalidOperationException("boom"));
        var transform = new RewriteNarrationTransform(service, logger);

        var metadata = PipelineChunkMetadata.Empty
            .WithAnnotation("narratoria.session_id", "s1")
            .WithAnnotation("narratoria.turn_index", "7");

        _ = await LlmPipelineHarness.RunAsync(
            [new TextChunk("Text.", metadata)],
            [transform]);

        var joined = string.Join("\n", logger.Messages);
        StringAssert.Contains(joined, nameof(RewriteNarrationTransform));
        StringAssert.Contains(joined, "provider call failed");
        StringAssert.Contains(joined, "session_id=s1");
        StringAssert.Contains(joined, "turn_index=7");
    }

    [TestMethod]
    public async Task CharacterTrackerTransform_OnParseFailure_LogsTransformAndContext()
    {
        var logger = new CapturingLogger<CharacterTrackerTransform>();
        var service = new FakeTextGenerationService(_ => new TextGenerationResponse { GeneratedText = "not json" });
        var transform = new CharacterTrackerTransform(service, logger);

        var metadata = PipelineChunkMetadata.Empty
            .WithAnnotation("narratoria.session_id", "s1")
            .WithAnnotation("narratoria.turn_id", "t1");

        _ = await LlmPipelineHarness.RunAsync(
            [new TextChunk("Text.", metadata)],
            [transform]);

        var joined = string.Join("\n", logger.Messages);
        StringAssert.Contains(joined, nameof(CharacterTrackerTransform));
        StringAssert.Contains(joined, "could not parse JSON update");
        StringAssert.Contains(joined, "session_id=s1");
        StringAssert.Contains(joined, "turn_id=t1");
    }

    private sealed class ThrowingService(Exception ex) : ITextGenerationService
    {
        public Task<TextGenerationResponse> GenerateAsync(TextGenerationRequest request, CancellationToken cancellationToken) =>
            throw ex;
    }

    private sealed class CapturingLogger<T> : Microsoft.Extensions.Logging.ILogger<T>
    {
        private sealed class NullScope : IDisposable
        {
            public static readonly NullScope Instance = new();
            public void Dispose() { }
        }

        public List<string> Messages { get; } = [];

        public IDisposable BeginScope<TState>(TState state) where TState : notnull => NullScope.Instance;

        public bool IsEnabled(Microsoft.Extensions.Logging.LogLevel logLevel) => true;

        public void Log<TState>(
            Microsoft.Extensions.Logging.LogLevel logLevel,
            Microsoft.Extensions.Logging.EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            Messages.Add(formatter(state, exception));
        }
    }
}
