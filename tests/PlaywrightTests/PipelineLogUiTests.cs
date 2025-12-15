using System.Threading.Tasks;
using Microsoft.Playwright;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace PlaywrightTests;

[TestClass]
public class PipelineLogUiTests
{
    private static IPlaywright? _playwright;
    private static IBrowser? _browser;

    [ClassInitialize]
    public static async Task Setup(TestContext _)
    {
        _playwright = await Playwright.CreateAsync();
        _browser = await _playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions { Headless = true });
    }

    [ClassCleanup]
    public static async Task Teardown()
    {
        if (_browser is not null) await _browser.CloseAsync();
        _playwright?.Dispose();
    }

    [TestMethod]
    public async Task ShowsTurnsAndStageChips()
    {
        var page = await _browser!.NewPageAsync();
        await page.GotoAsync("https://localhost:5001/test-pipeline-log", new() { WaitUntil = WaitUntilState.NetworkIdle });

        // Two turn sections
        var turnSections = await page.Locator("section.turn").CountAsync();
        Assert.AreEqual(2, turnSections);

        // Chip counts: 4 stages * 2 turns
        var chips = await page.Locator(".chip").CountAsync();
        Assert.AreEqual(8, chips);

        // Prompt input present
        await page.GetByTestId("prompt-input").IsVisibleAsync();
        await page.CloseAsync();
    }

    [TestMethod]
    public async Task StreamingTurnShowsEllipsis()
    {
        var page = await _browser!.NewPageAsync();
        await page.GotoAsync("https://localhost:5001/test-pipeline-log", new() { WaitUntil = WaitUntilState.NetworkIdle });
        var lastBubble = page.Locator(".turn").Last.Locator(".narration-bubble");
        var text = await lastBubble.InnerTextAsync();
        StringAssert.Contains(text, "...");
        await page.CloseAsync();
    }
}
