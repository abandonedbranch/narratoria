using Microsoft.Playwright;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Threading.Tasks;

namespace PlaywrightTests;

[TestClass]
public class OrchestratorUiTests
{
    [TestMethod]
    public async Task SubmitPrompt_AppendsNewTurnAndFinalizes()
    {
        using var playwright = await Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions { Headless = true });
        var context = await browser.NewContextAsync();
        var page = await context.NewPageAsync();

        await page.GotoAsync("http://localhost:5224/test-orchestrator");

        // Stage an attachment via payload
        var dropzone = page.GetByTestId("attachments-input");
        await dropzone.SetInputFilesAsync(new[]
        {
            new FilePayload
            {
                Name = "test.txt",
                MimeType = "text/plain",
                Buffer = System.Text.Encoding.UTF8.GetBytes("hello world")
            }
        });
        await page.GetByTestId("attachments-accept").ClickAsync();

        var input = page.GetByTestId("prompt-bar-input");
        await input.FillAsync("Test prompt");
        await page.GetByTestId("prompt-bar-submit").ClickAsync();

        await page.WaitForSelectorAsync(".turn");
        var turns = await page.Locator(".turn").CountAsync();
        Assert.IsTrue(turns >= 1);

        await page.WaitForSelectorAsync("text=NARRATION (streaming)");
        await page.WaitForSelectorAsync("text=NARRATION", new PageWaitForSelectorOptions { State = WaitForSelectorState.Visible, Timeout = 5000 });

        // Assert hover title includes provider metrics for Llm stage
        var llmChip = page.Locator(".chip", new LocatorOptions { HasTextString = "Llm" });
        var title = await llmChip.First.EvaluateAsync<string>("e => e.getAttribute('title')");
        Assert.IsNotNull(title);
        StringAssert.Contains(title, "C=4");
        StringAssert.Contains(title, "fake-narrator");
    }
}
