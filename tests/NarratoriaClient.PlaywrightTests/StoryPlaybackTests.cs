using Microsoft.Playwright;
using Xunit;

namespace NarratoriaClient.PlaywrightTests;

public class StoryPlaybackTests : IClassFixture<NarratoriaServerFixture>
{
    private readonly NarratoriaServerFixture _server;

    public StoryPlaybackTests(NarratoriaServerFixture server)
    {
        _server = server;
    }

    [Fact]
    public async Task PlayerPromptProducesNarratorReply()
    {
        using var playwright = await Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
        {
            Headless = true,
            SlowMo = 0
        });

        var page = await browser.NewPageAsync();
        page.SetDefaultTimeout(15000);
        var response = await page.GotoAsync(_server.BaseUrl);
        Assert.True(response?.Ok ?? false, $"Root navigation failed: {response?.Status} {response?.StatusText}");

        await page.Locator("textarea.message-input").FillAsync("Investigate the ruins ahead.");
        await page.Locator("button[type=submit]").ClickAsync();

        var narratorLocator = page.Locator(".chat-message", new PageLocatorOptions
        {
            HasTextString = "[FAKE NARRATOR]"
        });

        await narratorLocator.WaitForAsync(new LocatorWaitForOptions
        {
            Timeout = 30000
        });

        var transcript = await narratorLocator.InnerTextAsync();
        Assert.Contains("Investigate the ruins ahead", transcript);
    }
}
