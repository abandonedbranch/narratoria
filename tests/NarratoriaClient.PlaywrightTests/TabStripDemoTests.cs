using Microsoft.Playwright;
using Xunit;

namespace NarratoriaClient.PlaywrightTests;

public class TabStripDemoTests : IClassFixture<NarratoriaServerFixture>
{
    private readonly NarratoriaServerFixture _server;

    public TabStripDemoTests(NarratoriaServerFixture server)
    {
        _server = server;
    }

    [Fact]
    public async Task TabStripSwitchesTabsAndKeepsPanelsMounted()
    {
        var launch = await LaunchBrowserAsync();
        using var playwright = launch.Playwright;
        await using var browser = launch.Browser;
        var page = await browser.NewPageAsync();
        await page.GotoAsync($"{_server.BaseUrl}/tabs-demo");
        await WaitForTabsReadyAsync(page);

        var tabTwo = page.GetByRole(AriaRole.Tab, new() { Name = "Tab 2" });
        await Assertions.Expect(tabTwo).ToHaveAttributeAsync("aria-selected", "true");

        var tabOne = page.GetByRole(AriaRole.Tab, new() { Name = "Tab 1" });
        await tabOne.ClickAsync();
        await page.FillAsync("#tab1-notes", "Preserved note between tabs");

        var tabThree = page.GetByRole(AriaRole.Tab, new() { Name = "Tab 3" });
        await tabThree.ClickAsync();
        await Assertions.Expect(page.Locator("#tab3-panel")).ToBeVisibleAsync();
        await Assertions.Expect(page.Locator("#tab1-panel")).ToBeHiddenAsync();

        await tabOne.ClickAsync();
        var noteValue = await page.InputValueAsync("#tab1-notes");
        Assert.Equal("Preserved note between tabs", noteValue);
    }

    [Fact]
    public async Task TabStripHandlesKeyboardNavigationAndProgrammaticSelection()
    {
        var launch = await LaunchBrowserAsync();
        using var playwright = launch.Playwright;
        await using var browser = launch.Browser;
        var page = await browser.NewPageAsync();
        await page.GotoAsync($"{_server.BaseUrl}/tabs-demo");
        await WaitForTabsReadyAsync(page);

        var tabTwo = page.GetByRole(AriaRole.Tab, new() { Name = "Tab 2" });
        await tabTwo.ClickAsync();

        await page.Keyboard.PressAsync("ArrowRight");
        var tabThree = page.GetByRole(AriaRole.Tab, new() { Name = "Tab 3" });
        await Assertions.Expect(tabThree).ToHaveAttributeAsync("aria-selected", "true");
        await Assertions.Expect(page.Locator("#tab3-panel")).ToBeVisibleAsync();

        await page.Keyboard.PressAsync("Home");
        var tabOne = page.GetByRole(AriaRole.Tab, new() { Name = "Tab 1" });
        await Assertions.Expect(tabOne).ToHaveAttributeAsync("aria-selected", "true");

        await page.GetByRole(AriaRole.Button, new() { Name = "Go to Tab 3" }).ClickAsync();
        await Assertions.Expect(tabThree).ToHaveAttributeAsync("aria-selected", "true");
    }

    private static async Task<(IPlaywright Playwright, IBrowser Browser)> LaunchBrowserAsync()
    {
        var playwright = await Playwright.CreateAsync();

        var browser = await playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
        {
            Headless = true,
            SlowMo = 0
        });

        return (playwright, browser);
    }

    private static Task WaitForTabsReadyAsync(IPage page)
    {
        return page.WaitForSelectorAsync(".tab-strip__tab", new() { Timeout = 30000 });
    }
}
