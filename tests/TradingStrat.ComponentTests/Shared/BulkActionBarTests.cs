using Bunit;
using Microsoft.AspNetCore.Components;
using Shouldly;
using TradingStrat.ComponentTests.Infrastructure;
using TradingStrat.Web.Components.Shared;
using Xunit;

namespace TradingStrat.ComponentTests.Shared;

/// <summary>
/// Tests for the BulkActionBar component.
/// </summary>
public class BulkActionBarTests : BunitTestContext
{
    [Fact]
    public void BulkActionBar_HiddenWhenNoSelection()
    {
        // Arrange & Act
        var cut = Render<BulkActionBar>(parameters => parameters
            .Add(p => p.SelectedCount, 0)
            .Add(p => p.OnRefreshSelected, EventCallback.Factory.Create(this, () => { }))
            .Add(p => p.OnDeleteSelected, EventCallback.Factory.Create(this, () => { }))
            .Add(p => p.OnExportSelected, EventCallback.Factory.Create(this, () => { }))
            .Add(p => p.OnClearSelection, EventCallback.Factory.Create(this, () => { })));

        // Assert
        cut.Markup.ShouldBeEmpty();
    }

    [Fact]
    public void BulkActionBar_ShowsWhenItemsSelected()
    {
        // Arrange & Act
        var cut = Render<BulkActionBar>(parameters => parameters
            .Add(p => p.SelectedCount, 3)
            .Add(p => p.OnRefreshSelected, EventCallback.Factory.Create(this, () => { }))
            .Add(p => p.OnDeleteSelected, EventCallback.Factory.Create(this, () => { }))
            .Add(p => p.OnExportSelected, EventCallback.Factory.Create(this, () => { }))
            .Add(p => p.OnClearSelection, EventCallback.Factory.Create(this, () => { })));

        // Assert
        cut.Markup.ShouldNotBeEmpty();
        cut.Markup.ShouldContain("items selected");
    }

    [Fact]
    public void BulkActionBar_DisplaysSingularText()
    {
        // Arrange & Act
        var cut = Render<BulkActionBar>(parameters => parameters
            .Add(p => p.SelectedCount, 1)
            .Add(p => p.OnRefreshSelected, EventCallback.Factory.Create(this, () => { }))
            .Add(p => p.OnDeleteSelected, EventCallback.Factory.Create(this, () => { }))
            .Add(p => p.OnExportSelected, EventCallback.Factory.Create(this, () => { }))
            .Add(p => p.OnClearSelection, EventCallback.Factory.Create(this, () => { })));

        // Assert
        cut.Markup.ShouldContain("1");
        cut.Markup.ShouldContain("item selected");
        cut.Markup.ShouldNotContain("items selected");
    }

    [Fact]
    public void BulkActionBar_DisplaysPluralText()
    {
        // Arrange & Act
        var cut = Render<BulkActionBar>(parameters => parameters
            .Add(p => p.SelectedCount, 5)
            .Add(p => p.OnRefreshSelected, EventCallback.Factory.Create(this, () => { }))
            .Add(p => p.OnDeleteSelected, EventCallback.Factory.Create(this, () => { }))
            .Add(p => p.OnExportSelected, EventCallback.Factory.Create(this, () => { }))
            .Add(p => p.OnClearSelection, EventCallback.Factory.Create(this, () => { })));

        // Assert
        cut.Markup.ShouldContain("5");
        cut.Markup.ShouldContain("items selected");
    }

    [Fact]
    public void BulkActionBar_RefreshButtonTriggersCallback()
    {
        // Arrange
        bool refreshCalled = false;
        var cut = Render<BulkActionBar>(parameters => parameters
            .Add(p => p.SelectedCount, 1)
            .Add(p => p.OnRefreshSelected, EventCallback.Factory.Create(this, () => refreshCalled = true))
            .Add(p => p.OnDeleteSelected, EventCallback.Factory.Create(this, () => { }))
            .Add(p => p.OnExportSelected, EventCallback.Factory.Create(this, () => { }))
            .Add(p => p.OnClearSelection, EventCallback.Factory.Create(this, () => { })));

        // Act
        var refreshButtonComponent = cut.FindComponents<Button>().First(b => b.Instance.Text == "Refresh Selected");
        refreshButtonComponent.Find("button").Click();

        // Assert
        refreshCalled.ShouldBeTrue();
    }

    [Fact]
    public void BulkActionBar_DeleteButtonTriggersCallback()
    {
        // Arrange
        bool deleteCalled = false;
        var cut = Render<BulkActionBar>(parameters => parameters
            .Add(p => p.SelectedCount, 1)
            .Add(p => p.OnRefreshSelected, EventCallback.Factory.Create(this, () => { }))
            .Add(p => p.OnDeleteSelected, EventCallback.Factory.Create(this, () => deleteCalled = true))
            .Add(p => p.OnExportSelected, EventCallback.Factory.Create(this, () => { }))
            .Add(p => p.OnClearSelection, EventCallback.Factory.Create(this, () => { })));

        // Act
        var deleteButtonComponent = cut.FindComponents<Button>().First(b => b.Instance.Text == "Delete Selected");
        deleteButtonComponent.Find("button").Click();

        // Assert
        deleteCalled.ShouldBeTrue();
    }

    [Fact]
    public void BulkActionBar_ExportButtonTriggersCallback()
    {
        // Arrange
        bool exportCalled = false;
        var cut = Render<BulkActionBar>(parameters => parameters
            .Add(p => p.SelectedCount, 1)
            .Add(p => p.OnRefreshSelected, EventCallback.Factory.Create(this, () => { }))
            .Add(p => p.OnDeleteSelected, EventCallback.Factory.Create(this, () => { }))
            .Add(p => p.OnExportSelected, EventCallback.Factory.Create(this, () => exportCalled = true))
            .Add(p => p.OnClearSelection, EventCallback.Factory.Create(this, () => { })));

        // Act
        var exportButtonComponent = cut.FindComponents<Button>().First(b => b.Instance.Text == "Export Selected");
        exportButtonComponent.Find("button").Click();

        // Assert
        exportCalled.ShouldBeTrue();
    }

    [Fact]
    public void BulkActionBar_ClearButtonTriggersCallback()
    {
        // Arrange
        bool clearCalled = false;
        var cut = Render<BulkActionBar>(parameters => parameters
            .Add(p => p.SelectedCount, 1)
            .Add(p => p.OnRefreshSelected, EventCallback.Factory.Create(this, () => { }))
            .Add(p => p.OnDeleteSelected, EventCallback.Factory.Create(this, () => { }))
            .Add(p => p.OnExportSelected, EventCallback.Factory.Create(this, () => { }))
            .Add(p => p.OnClearSelection, EventCallback.Factory.Create(this, () => clearCalled = true)));

        // Act
        // Clear button is a plain button element, not a Button component
        var clearButton = cut.FindAll("button").Last(); // Clear button is the last button
        clearButton.Click();

        // Assert
        clearCalled.ShouldBeTrue();
    }

    [Fact]
    public void BulkActionBar_RefreshButtonHiddenWhenDisabled()
    {
        // Arrange & Act
        var cut = Render<BulkActionBar>(parameters => parameters
            .Add(p => p.SelectedCount, 1)
            .Add(p => p.ShowRefreshAction, false)
            .Add(p => p.OnRefreshSelected, EventCallback.Factory.Create(this, () => { }))
            .Add(p => p.OnDeleteSelected, EventCallback.Factory.Create(this, () => { }))
            .Add(p => p.OnExportSelected, EventCallback.Factory.Create(this, () => { }))
            .Add(p => p.OnClearSelection, EventCallback.Factory.Create(this, () => { })));

        // Assert
        cut.Markup.ShouldNotContain("Refresh Selected");
    }

    [Fact]
    public void BulkActionBar_DeleteButtonHiddenWhenDisabled()
    {
        // Arrange & Act
        var cut = Render<BulkActionBar>(parameters => parameters
            .Add(p => p.SelectedCount, 1)
            .Add(p => p.ShowDeleteAction, false)
            .Add(p => p.OnRefreshSelected, EventCallback.Factory.Create(this, () => { }))
            .Add(p => p.OnDeleteSelected, EventCallback.Factory.Create(this, () => { }))
            .Add(p => p.OnExportSelected, EventCallback.Factory.Create(this, () => { }))
            .Add(p => p.OnClearSelection, EventCallback.Factory.Create(this, () => { })));

        // Assert
        cut.Markup.ShouldNotContain("Delete Selected");
    }

    [Fact]
    public void BulkActionBar_ExportButtonHiddenWhenDisabled()
    {
        // Arrange & Act
        var cut = Render<BulkActionBar>(parameters => parameters
            .Add(p => p.SelectedCount, 1)
            .Add(p => p.ShowExportAction, false)
            .Add(p => p.OnRefreshSelected, EventCallback.Factory.Create(this, () => { }))
            .Add(p => p.OnDeleteSelected, EventCallback.Factory.Create(this, () => { }))
            .Add(p => p.OnExportSelected, EventCallback.Factory.Create(this, () => { }))
            .Add(p => p.OnClearSelection, EventCallback.Factory.Create(this, () => { })));

        // Assert
        cut.Markup.ShouldNotContain("Export Selected");
    }

    [Fact]
    public void BulkActionBar_ClearButtonHasCorrectAriaLabel()
    {
        // Arrange & Act
        var cut = Render<BulkActionBar>(parameters => parameters
            .Add(p => p.SelectedCount, 1)
            .Add(p => p.OnRefreshSelected, EventCallback.Factory.Create(this, () => { }))
            .Add(p => p.OnDeleteSelected, EventCallback.Factory.Create(this, () => { }))
            .Add(p => p.OnExportSelected, EventCallback.Factory.Create(this, () => { }))
            .Add(p => p.OnClearSelection, EventCallback.Factory.Create(this, () => { })));

        // Assert
        var clearButton = cut.Find("button[aria-label='Clear selection']");
        clearButton.ShouldNotBeNull();
    }

    [Fact]
    public void BulkActionBar_HasStickyPositioning()
    {
        // Arrange & Act
        var cut = Render<BulkActionBar>(parameters => parameters
            .Add(p => p.SelectedCount, 1)
            .Add(p => p.OnRefreshSelected, EventCallback.Factory.Create(this, () => { }))
            .Add(p => p.OnDeleteSelected, EventCallback.Factory.Create(this, () => { }))
            .Add(p => p.OnExportSelected, EventCallback.Factory.Create(this, () => { }))
            .Add(p => p.OnClearSelection, EventCallback.Factory.Create(this, () => { })));

        // Assert
        var container = cut.Find(".sticky");
        container.ShouldNotBeNull();
        container.ClassList.ShouldContain("bottom-0");
    }

    [Fact]
    public void BulkActionBar_HasCorrectStyling()
    {
        // Arrange & Act
        var cut = Render<BulkActionBar>(parameters => parameters
            .Add(p => p.SelectedCount, 1)
            .Add(p => p.OnRefreshSelected, EventCallback.Factory.Create(this, () => { }))
            .Add(p => p.OnDeleteSelected, EventCallback.Factory.Create(this, () => { }))
            .Add(p => p.OnExportSelected, EventCallback.Factory.Create(this, () => { }))
            .Add(p => p.OnClearSelection, EventCallback.Factory.Create(this, () => { })));

        // Assert
        var container = cut.Find(".sticky");
        container.ClassList.ShouldContain("bg-blue-600");
        container.ClassList.ShouldContain("shadow-lg");
        container.ClassList.ShouldContain("z-10");
    }
}
