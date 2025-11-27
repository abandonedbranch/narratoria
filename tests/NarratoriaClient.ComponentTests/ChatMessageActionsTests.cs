using Bunit;
using NarratoriaClient.Components;
using Xunit;

namespace NarratoriaClient.ComponentTests;

public class ChatMessageActionsTests : TestContext
{
    [Fact]
    public void InvokesDeleteWhenConfirmed()
    {
        JSInterop.Setup<bool>("confirm", "Delete this message?").SetResult(true);

        string? deletedId = null;

        var cut = RenderComponent<ChatMessageActions>(parameters =>
        {
            parameters.Add(p => p.MessageId, "msg-1");
            parameters.Add(p => p.OnDelete, id => deletedId = id);
        });

        cut.Find("button.chat-action--delete").Click();

        cut.WaitForAssertion(() =>
        {
            Assert.Equal("msg-1", deletedId);
        });
    }

    [Fact]
    public void DoesNotInvokeDeleteWhenCancelled()
    {
        JSInterop.Setup<bool>("confirm", "Delete this message?").SetResult(false);

        bool called = false;

        var cut = RenderComponent<ChatMessageActions>(parameters =>
        {
            parameters.Add(p => p.MessageId, "msg-2");
            parameters.Add(p => p.OnDelete, _ => called = true);
        });

        cut.Find("button.chat-action--delete").Click();

        cut.WaitForAssertion(() =>
        {
            Assert.False(called);
        });
    }
}
