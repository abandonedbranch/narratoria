using Bunit;
using Microsoft.AspNetCore.Components;
using NarratoriaClient.Components;
using Xunit;

namespace NarratoriaClient.ComponentTests;

public class TabStripTests : TestContext
{
    [Fact]
    public void ActivatesFirstTabByDefault()
    {
        var cut = RenderComponent<TabStrip>(parameters => parameters.AddChildContent(BuildTabs()));

        cut.WaitForAssertion(() =>
        {
            var tabs = cut.FindAll("[role=tab]");
            Assert.Equal(3, tabs.Count);
            Assert.Contains("is-active", tabs[0].ClassList);
            Assert.DoesNotContain("is-active", tabs[1].ClassList);
            Assert.DoesNotContain("is-active", tabs[2].ClassList);

            var panels = cut.FindAll(".tab-strip__panel");
            Assert.Equal(3, panels.Count);
            Assert.Contains("is-active", panels[0].ClassList);
            Assert.DoesNotContain("is-active", panels[1].ClassList);
            Assert.DoesNotContain("is-active", panels[2].ClassList);
        });
    }

    [Fact]
    public void ClickingTabChangesActivePanel()
    {
        var cut = RenderComponent<TabStrip>(parameters => parameters.AddChildContent(BuildTabs()));

        cut.WaitForAssertion(() =>
        {
            var active = cut.FindAll("[role=tab]");
            Assert.Contains("is-active", active[0].ClassList);
        });

        var tabThree = cut.FindAll("[role=tab]")[2];
        tabThree.Click();

        cut.WaitForAssertion(() =>
        {
            var tabs = cut.FindAll("[role=tab]");
            Assert.Contains("is-active", tabs[2].ClassList);
            Assert.DoesNotContain("is-active", tabs[0].ClassList);

            var panels = cut.FindAll(".tab-strip__panel");
            Assert.Contains("is-active", panels[2].ClassList);
            Assert.DoesNotContain("is-active", panels[0].ClassList);
        });
    }

    private static RenderFragment BuildTabs() => builder =>
    {
        builder.OpenComponent<TabStripTab>(0);
        builder.AddAttribute(1, nameof(TabStripTab.TabId), "tab1");
        builder.AddAttribute(2, nameof(TabStripTab.Title), "Tab 1");
        builder.AddAttribute(3, nameof(TabStripTab.ChildContent), (RenderFragment)(b => b.AddContent(0, "First tab")));
        builder.CloseComponent();

        builder.OpenComponent<TabStripTab>(4);
        builder.AddAttribute(5, nameof(TabStripTab.TabId), "tab2");
        builder.AddAttribute(6, nameof(TabStripTab.Title), "Tab 2");
        builder.AddAttribute(7, nameof(TabStripTab.ChildContent), (RenderFragment)(b => b.AddContent(0, "Second tab")));
        builder.CloseComponent();

        builder.OpenComponent<TabStripTab>(8);
        builder.AddAttribute(9, nameof(TabStripTab.TabId), "tab3");
        builder.AddAttribute(10, nameof(TabStripTab.Title), "Tab 3");
        builder.AddAttribute(11, nameof(TabStripTab.ChildContent), (RenderFragment)(b => b.AddContent(0, "Third tab")));
        builder.CloseComponent();
    };
}
