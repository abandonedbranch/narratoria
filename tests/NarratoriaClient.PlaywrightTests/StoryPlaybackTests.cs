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
        var headless = !string.Equals(Environment.GetEnvironmentVariable("PLAYWRIGHT_HEADFUL"), "true", StringComparison.OrdinalIgnoreCase);
        var slowMo = int.TryParse(Environment.GetEnvironmentVariable("PLAYWRIGHT_SLOWMO_MS"), out var parsedSlowMo) ? parsedSlowMo : 0;

        await using var browser = await playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
        {
            Headless = headless,
            SlowMo = slowMo
        });

        var page = await browser.NewPageAsync();
        await page.GotoAsync(_server.BaseUrl);

        await page.Locator("textarea.message-input").FillAsync("Investigate the ruins ahead.");
        await page.Locator("button[type=submit]").ClickAsync();

        var narratorLocator = page.Locator(".chat-message", new PageLocatorOptions
        {
            HasTextString = "[FAKE NARRATOR]"
        });

        await narratorLocator.WaitForAsync(new LocatorWaitForOptions
        {
            Timeout = 15000
        });

        var transcript = await narratorLocator.InnerTextAsync();
        Assert.Contains("Investigate the ruins ahead", transcript);
    }
}
