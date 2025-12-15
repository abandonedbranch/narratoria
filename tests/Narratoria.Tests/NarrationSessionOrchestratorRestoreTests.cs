using Microsoft.VisualStudio.TestTools.UnitTesting;
using Narratoria.Components;
using Narratoria.Narration;
using System.Collections.Immutable;

namespace Narratoria.Tests;

[TestClass]
public class NarrationSessionOrchestratorRestoreTests
{
    [TestMethod]
    public void Restore_PopulatesTurnsFromStore()
    {
        var sessionId = Guid.NewGuid();
        var store = new InMemoryStore(new NarrationContext
        {
            SessionId = sessionId,
            PlayerPrompt = "prior-prompt",
            PriorNarration = ImmutableArray.Create("line-1", "line-2"),
            WorkingNarration = ImmutableArray<string>.Empty,
            WorkingContextSegments = ImmutableArray<ContextSegment>.Empty,
            Metadata = ImmutableDictionary<string, string>.Empty,
            Trace = new Narratoria.OpenAi.TraceMetadata("t","r")
        });

        var component = new NarrationSessionOrchestratorHarness(sessionId, store);
        var turns = component.GetTurns();
        Assert.AreEqual(2, turns.Count);
        Assert.AreEqual("line-1", turns[0].Output.FinalText);
        Assert.AreEqual("line-2", turns[1].Output.FinalText);
    }

    private sealed class InMemoryStore : INarrationSessionStore
    {
        private readonly NarrationContext _context;
        public InMemoryStore(NarrationContext context) { _context = context; }
        public ValueTask<NarrationContext?> LoadAsync(Guid sessionId, CancellationToken cancellationToken) => ValueTask.FromResult<NarrationContext?>(sessionId == _context.SessionId ? _context : null);
        public ValueTask SaveAsync(NarrationContext context, CancellationToken cancellationToken) => ValueTask.CompletedTask;
    }

    private sealed class NarrationSessionOrchestratorHarness
    {
        private readonly NarrationSessionOrchestrator _component = new();
        public NarrationSessionOrchestratorHarness(Guid sessionId, INarrationSessionStore store)
        {
            _component.SessionId = sessionId;
            _component.SessionStore = store;
            var onInitialized = typeof(NarrationSessionOrchestrator).GetMethod("OnInitialized", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            onInitialized!.Invoke(_component, Array.Empty<object>());
        }
        public IReadOnlyList<NarrationPipelineTurnView> GetTurns()
        {
            var field = typeof(NarrationSessionOrchestrator).GetField("_turns", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            return (IReadOnlyList<NarrationPipelineTurnView>)field!.GetValue(_component)!;
        }
    }
}
