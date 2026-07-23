using AngleSharp.Dom;
using Bunit;
using Microsoft.AspNetCore.Components;
using Shouldly;
using TradingStrat.ComponentTests.Infrastructure;
using TradingStrat.Web.Components.UI.DataDisplay.Table;
using Xunit;

namespace TradingStrat.ComponentTests.UI.DataDisplay.Table;

/// <summary>
/// Tests for the Catalyst TableRow component.
/// </summary>
public class TableRowTests : BunitTestContext
{
    [Fact]
    public void TableRow_WithoutParameters_RendersBasicRow()
    {
        // Arrange & Act
        IRenderedComponent<TableRow> cut = Render<TableRow>(parameters => parameters
            .Add(p => p.ChildContent, (RenderFragment)(builder => builder.AddContent(0, "Row content"))));

        // Assert
        cut.Markup.ShouldNotBeEmpty();
        IElement row = cut.Find("tr");
        row.ShouldNotBeNull();
    }

    [Fact]
    public void TableRow_WithHref_RendersClickableRow()
    {
        // Arrange & Act
        IRenderedComponent<TableRow> cut = Render<TableRow>(parameters => parameters
            .Add(p => p.Href, "/details")
            .Add(p => p.Title, "View details")
            .Add(p => p.ChildContent, (RenderFragment)(builder =>
            {
                builder.OpenComponent<TableCell>(0);
                builder.AddAttribute(1, "ChildContent", (RenderFragment)(cb => cb.AddContent(0, "Data")));
                builder.CloseComponent();
            })));

        // Assert
        IElement row = cut.Find("tr");
        row.ShouldNotBeNull();
        // Clickable row has focus and hover styles
        row.ClassList.ShouldContain("hover:bg-zinc-950/2.5");
    }

    [Fact]
    public void TableRow_InStripedTable_AppliesStripedClasses()
    {
        // Arrange & Act
        IRenderedComponent<Web.Components.UI.DataDisplay.Table.Table> cut = Render<Web.Components.UI.DataDisplay.Table.Table>(parameters => parameters
            .Add(p => p.Striped, true)
            .Add(p => p.ChildContent, (RenderFragment)(builder =>
            {
                builder.OpenComponent<TableBody>(0);
                builder.AddAttribute(1, "ChildContent", (RenderFragment)(b =>
                {
                    b.OpenComponent<TableRow>(0);
                    b.AddAttribute(1, "ChildContent", (RenderFragment)(rb => rb.AddContent(0, "Row")));
                    b.CloseComponent();
                }));
                builder.CloseComponent();
            })));

        // Assert
        IElement row = cut.Find("tr");
        row.ShouldNotBeNull();
        row.ClassList.ShouldContain("even:bg-zinc-950/2.5");
    }

    [Fact]
    public void TableRow_WithCustomClass_AppendsToExistingClasses()
    {
        // Arrange & Act
        IRenderedComponent<TableRow> cut = Render<TableRow>(parameters => parameters
            .Add(p => p.Class, "custom-row")
            .Add(p => p.ChildContent, (RenderFragment)(builder => builder.AddContent(0, "Row"))));

        // Assert
        IElement row = cut.Find("tr");
        row.ClassList.ShouldContain("custom-row");
    }
}
