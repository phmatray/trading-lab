using AngleSharp.Dom;
using Bunit;
using Microsoft.AspNetCore.Components;
using Shouldly;
using TradingStrat.ComponentTests.Infrastructure;
using TradingStrat.Web.Components.UI.DataDisplay.Table;
using Xunit;

namespace TradingStrat.ComponentTests.UI.DataDisplay.Table;

/// <summary>
/// Tests for the Catalyst TableCell component.
/// </summary>
public class TableCellTests : BunitTestContext
{
    [Fact]
    public void TableCell_WithoutParameters_RendersBasicCell()
    {
        // Arrange & Act
        IRenderedComponent<TableCell> cut = Render<TableCell>(parameters => parameters
            .Add(p => p.ChildContent, (RenderFragment)(builder => builder.AddContent(0, "Cell content"))));

        // Assert
        cut.Markup.ShouldNotBeEmpty();
        IElement cell = cut.Find("td");
        cell.ShouldNotBeNull();
        cell.TextContent.ShouldBe("Cell content");
    }

    [Fact]
    public void TableCell_InClickableRow_RendersLinkOverlay()
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
        IElement link = cut.Find("a[data-row-link]");
        link.ShouldNotBeNull();
        link.GetAttribute("href").ShouldBe("/details");
        link.GetAttribute("aria-label").ShouldBe("View details");
    }

    [Fact]
    public void TableCell_InDenseTable_AppliesDensePadding()
    {
        // Arrange & Act
        IRenderedComponent<Web.Components.UI.DataDisplay.Table.Table> cut = Render<Web.Components.UI.DataDisplay.Table.Table>(parameters => parameters
            .Add(p => p.Dense, true)
            .Add(p => p.ChildContent, (RenderFragment)(builder =>
            {
                builder.OpenComponent<TableBody>(0);
                builder.AddAttribute(1, "ChildContent", (RenderFragment)(b =>
                {
                    b.OpenComponent<TableRow>(0);
                    b.AddAttribute(1, "ChildContent", (RenderFragment)(rb =>
                    {
                        rb.OpenComponent<TableCell>(0);
                        rb.AddAttribute(1, "ChildContent", (RenderFragment)(cb => cb.AddContent(0, "Data")));
                        rb.CloseComponent();
                    }));
                    b.CloseComponent();
                }));
                builder.CloseComponent();
            })));

        // Assert
        IElement cell = cut.Find("td");
        cell.ShouldNotBeNull();
        cell.ClassList.ShouldContain("py-2.5");
    }

    [Fact]
    public void TableCell_WithCustomClass_AppendsToExistingClasses()
    {
        // Arrange & Act
        IRenderedComponent<TableCell> cut = Render<TableCell>(parameters => parameters
            .Add(p => p.Class, "custom-cell")
            .Add(p => p.ChildContent, (RenderFragment)(builder => builder.AddContent(0, "Data"))));

        // Assert
        IElement cell = cut.Find("td");
        cell.ClassList.ShouldContain("custom-cell");
        cell.ClassList.ShouldContain("relative");
    }
}
