using Microsoft.Playwright;
using Xunit;

namespace NarratoriaClient.PlaywrightTests;

public class ComponentCoverageTests : IClassFixture<NarratoriaServerFixture>
{
    private readonly NarratoriaServerFixture _server;

    public ComponentCoverageTests(NarratoriaServerFixture server)
    {
        _server = server;
    }

    [Fact]
    public async Task SessionsManagerCreatesAndActivatesNewSession()
    {
        var launch = await LaunchBrowserAsync();
        using var playwright = launch.Playwright;
        await using var browser = launch.Browser;
        var page = await browser.NewPageAsync();
        await page.GotoAsync(_server.BaseUrl);
        await WaitForAppReadyAsync(page);

        await OpenSessionsManagerAsync(page);

        var items = page.Locator(".sessions-manager__item");
        var initialCount = await items.CountAsync();
        Assert.True(initialCount >= 1);

        await page.GetByRole(AriaRole.Button, new() { Name = "Start New Session" }).ClickAsync();

        await Assertions.Expect(items).ToHaveCountAsync(initialCount + 1);
        await Assertions.Expect(page.Locator(".home-scrollback__header h2")).ToContainTextAsync("Session 2");

        var newSessionItem = page.Locator(".sessions-manager__item", new() { HasTextString = "Session 2" });
        await Assertions.Expect(newSessionItem.GetByText("Active")).ToBeVisibleAsync();
    }

    [Fact]
    public async Task HomeHeadingUpdatesWhenSwitchingSessions()
    {
        var launch = await LaunchBrowserAsync();
        using var playwright = launch.Playwright;
        await using var browser = launch.Browser;
        var page = await browser.NewPageAsync();
        await page.GotoAsync(_server.BaseUrl);
        await WaitForAppReadyAsync(page);

        await OpenSessionsManagerAsync(page);
        await page.GetByRole(AriaRole.Button, new() { Name = "Start New Session" }).ClickAsync();
        await Assertions.Expect(page.Locator(".home-scrollback__header h2")).ToContainTextAsync("Session 2");

        var sessionOneSwitch = page.Locator(".sessions-manager__item", new() { HasTextString = "Session 1" })
            .GetByRole(AriaRole.Button, new() { Name = "Switch" });
        await sessionOneSwitch.ClickAsync();

        await Assertions.Expect(page.Locator(".home-scrollback__header h2")).ToContainTextAsync("Session 1");
        var sessionOneItem = page.Locator(".sessions-manager__item", new() { HasTextString = "Session 1" });
        await Assertions.Expect(sessionOneItem.GetByText("Active")).ToBeVisibleAsync();
    }

    [Fact]
    public async Task ReplyEditorIgnoresEmptySubmitAndClearsOnSend()
    {
        var launch = await LaunchBrowserAsync();
        using var playwright = launch.Playwright;
        await using var browser = launch.Browser;
        var page = await browser.NewPageAsync();
        await page.GotoAsync(_server.BaseUrl);
        await WaitForAppReadyAsync(page);

        var messages = page.Locator(".chat-item");
        var initialCount = await messages.CountAsync();

        await page.Locator("button[type=submit]").ClickAsync();
        await Task.Delay(200);
        Assert.Equal(initialCount, await messages.CountAsync());

        const string playerMessage = "Test send from reply editor.";
        await page.Locator("textarea.message-input").FillAsync(playerMessage);
        await page.Locator("button[type=submit]").ClickAsync();

        await page.WaitForTimeoutAsync(300);
        var afterSendCount = await messages.CountAsync();
        Assert.True(afterSendCount > initialCount);

        await Assertions.Expect(page.Locator("textarea.message-input")).ToHaveValueAsync(string.Empty);
    }

    private static async Task<(IPlaywright Playwright, IBrowser Browser)> LaunchBrowserAsync()
    {
        var playwright = await Playwright.CreateAsync();
        var headless = !string.Equals(Environment.GetEnvironmentVariable("PLAYWRIGHT_HEADFUL"), "true", StringComparison.OrdinalIgnoreCase);
        var slowMo = int.TryParse(Environment.GetEnvironmentVariable("PLAYWRIGHT_SLOWMO_MS"), out var parsedSlowMo) ? parsedSlowMo : 0;

        var browser = await playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
        {
            Headless = headless,
            SlowMo = slowMo
        });
        return (playwright, browser);
    }

    private static async Task OpenSessionsManagerAsync(IPage page)
    {
        await page.Locator("textarea.message-input").FillAsync("@sessions");
        await page.Locator("button[type=submit]").ClickAsync();
        await page.WaitForSelectorAsync(".sessions-manager", new() { Timeout = 30000 });
    }

    private static async Task WaitForAppReadyAsync(IPage page)
    {
        await page.WaitForSelectorAsync("textarea.message-input", new() { Timeout = 30000 });
    }
}
