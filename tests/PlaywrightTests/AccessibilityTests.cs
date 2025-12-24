using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Playwright;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace PlaywrightTests;

[TestClass]
public class AccessibilityTests
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
    public async Task Axe_NoViolations_On_TestOrchestrator()
    {
        var page = await _browser!.NewPageAsync();
        await page.GotoAsync("http://localhost:5224/test-orchestrator", new() { WaitUntil = WaitUntilState.NetworkIdle });

        await InjectAxeAsync(page);
        var violations = await RunAxeAsync(page);

        if (violations.Any())
        {
            var summary = string.Join("\n\n", violations.Select(v => $"{v.Id}: {v.Impact} - {v.Description}\nNodes: {string.Join(", ", v.Nodes.Select(n => n.Target))}"));
            Assert.Fail($"Accessibility violations found:\n{summary}");
        }

        await page.CloseAsync();
    }

    [TestMethod]
    public async Task Axe_NoViolations_On_TestPipelineLog()
    {
        var page = await _browser!.NewPageAsync();
        await page.GotoAsync("https://localhost:5001/test-pipeline-log", new() { WaitUntil = WaitUntilState.NetworkIdle });

        await InjectAxeAsync(page);
        var violations = await RunAxeAsync(page);

        if (violations.Any())
        {
            var summary = string.Join("\n\n", violations.Select(v => $"{v.Id}: {v.Impact} - {v.Description}\nNodes: {string.Join(", ", v.Nodes.Select(n => n.Target))}"));
            Assert.Fail($"Accessibility violations found:\n{summary}");
        }

        await page.CloseAsync();
    }

    private static async Task InjectAxeAsync(IPage page)
    {
        // Load axe-core from CDN
        await page.AddScriptTagAsync(new PageAddScriptTagOptions
        {
            Url = "https://cdnjs.cloudflare.com/ajax/libs/axe-core/4.8.2/axe.min.js"
        });
    }

    private static async Task<AxeViolation[]> RunAxeAsync(IPage page)
    {
        // Run axe and return violations
        var resultJson = await page.EvaluateAsync<string>(@"
            async () => {
              const res = await (window).axe.run(document, { resultTypes: ['violations'] });
              return JSON.stringify(res.violations.map(v => ({
                id: v.id,
                impact: v.impact,
                description: v.description,
                helpUrl: v.helpUrl,
                nodes: v.nodes.map(n => ({ target: n.target && n.target[0] ? n.target[0] : '' }))
              })));
            }
        ");

        return JsonSerializer.Deserialize<AxeViolation[]>(resultJson, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        }) ?? System.Array.Empty<AxeViolation>();
    }

    private record AxeNode(string Target);

    private record AxeViolation(string Id, string Impact, string Description, string HelpUrl, AxeNode[] Nodes);
}
