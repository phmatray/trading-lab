using AngleSharp.Dom;
using Bunit;
using Microsoft.AspNetCore.Components;
using Shouldly;
using TradingStrat.ComponentTests.Infrastructure;
using TradingStrat.Web.Components.UI.DataDisplay.Table;
using Xunit;

namespace TradingStrat.ComponentTests.UI.DataDisplay.Table;

/// <summary>
/// Tests for the Catalyst Table component.
/// </summary>
public class TableTests : BunitTestContext
{
    [Fact]
    public void Table_WithoutParameters_RendersBasicTable()
    {
        // Arrange & Act
        IRenderedComponent<Web.Components.UI.DataDisplay.Table.Table> cut = Render<Web.Components.UI.DataDisplay.Table.Table>(parameters => parameters
            .Add(p => p.ChildContent, (RenderFragment)(builder => builder.AddContent(0, "Content"))));

        // Assert
        cut.Markup.ShouldNotBeEmpty();
        IElement table = cut.Find("table");
        table.ShouldNotBeNull();
        table.ClassList.ShouldContain("min-w-full");
    }

    [Fact]
    public void Table_WithBleedMode_AppliesCorrectClasses()
    {
        // Arrange & Act
        IRenderedComponent<Web.Components.UI.DataDisplay.Table.Table> cut = Render<Web.Components.UI.DataDisplay.Table.Table>(parameters => parameters
            .Add(p => p.Bleed, true)
            .Add(p => p.ChildContent, (RenderFragment)(builder => builder.AddContent(0, "Content"))));

        // Assert
        IElement outerDiv = cut.Find("div.overflow-x-auto");
        outerDiv.ShouldNotBeNull();
    }

    [Fact]
    public void Table_WithDenseMode_PassesConfigurationToChildren()
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
        // Dense mode applies py-2.5 instead of py-4
        cell.ClassList.ShouldContain("py-2.5");
    }

    [Fact]
    public void Table_WithGridMode_AppliesVerticalBorders()
    {
        // Arrange & Act
        IRenderedComponent<Web.Components.UI.DataDisplay.Table.Table> cut = Render<Web.Components.UI.DataDisplay.Table.Table>(parameters => parameters
            .Add(p => p.Grid, true)
            .Add(p => p.ChildContent, (RenderFragment)(builder =>
            {
                builder.OpenComponent<TableHead>(0);
                builder.AddAttribute(1, "ChildContent", (RenderFragment)(hb =>
                {
                    hb.OpenComponent<TableRow>(0);
                    hb.AddAttribute(1, "ChildContent", (RenderFragment)(rb =>
                    {
                        rb.OpenComponent<TableHeader>(0);
                        rb.AddAttribute(1, "ChildContent", (RenderFragment)(thb => thb.AddContent(0, "Header")));
                        rb.CloseComponent();
                    }));
                    hb.CloseComponent();
                }));
                builder.CloseComponent();
            })));

        // Assert
        IElement header = cut.Find("th");
        header.ShouldNotBeNull();
        header.ClassList.ShouldContain("border-l");
    }

    [Fact]
    public void Table_WithStripedMode_AppliesStripedStyling()
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
        IElement row = cut.Find("tr");
        row.ShouldNotBeNull();
        // Striped mode applies even:bg styles
        row.ClassList.ShouldContain("even:bg-zinc-950/2.5");
    }

    [Fact]
    public void Table_WithCustomClass_AppendsToExistingClasses()
    {
        // Arrange & Act
        IRenderedComponent<Web.Components.UI.DataDisplay.Table.Table> cut = Render<Web.Components.UI.DataDisplay.Table.Table>(parameters => parameters
            .Add(p => p.Class, "custom-table")
            .Add(p => p.ChildContent, (RenderFragment)(builder => builder.AddContent(0, "Content"))));

        // Assert
        IElement outerDiv = cut.Find("div.custom-table");
        outerDiv.ShouldNotBeNull();
    }

    [Fact]
    public void Table_WithAllModesEnabled_AppliesAllConfigurations()
    {
        // Arrange & Act
        IRenderedComponent<Web.Components.UI.DataDisplay.Table.Table> cut = Render<Web.Components.UI.DataDisplay.Table.Table>(parameters => parameters
            .Add(p => p.Bleed, true)
            .Add(p => p.Dense, true)
            .Add(p => p.Grid, true)
            .Add(p => p.Striped, true)
            .Add(p => p.ChildContent, (RenderFragment)(builder => builder.AddContent(0, "Content"))));

        // Assert
        cut.Markup.ShouldNotBeEmpty();
        IElement table = cut.Find("table");
        table.ShouldNotBeNull();
    }
}
