using AngleSharp.Dom;
using Bunit;
using Microsoft.AspNetCore.Components;
using Shouldly;
using TradingStrat.ComponentTests.Infrastructure;
using TradingStrat.Web.Components.Shared;
using Xunit;

namespace TradingStrat.ComponentTests.Shared;

/// <summary>
/// Tests for the MultiSelectTable component.
/// </summary>
public class MultiSelectTableTests : BunitTestContext
{
    private record TestItem(string Id, string Name);

    [Fact]
    public void MultiSelectTable_RendersEmptyStateWithDefaultMessage()
    {
        // Arrange & Act
        IRenderedComponent<MultiSelectTable<TestItem>> cut = Render<MultiSelectTable<TestItem>>(parameters => parameters
            .Add(p => p.Items, Array.Empty<TestItem>())
            .Add(p => p.GetItemKey, item => item.Id)
            .Add(p => p.HeaderContent, builder => builder.AddMarkupContent(0, "<th>Name</th>"))
            .Add(p => p.RowContent, item => builder => builder.AddMarkupContent(0, $"<td>{item.Name}</td>"))
            .Add(p => p.OnSelectionChanged, EventCallback.Factory.Create<HashSet<string>>(this, _ => { })));

        // Assert
        cut.Markup.ShouldContain("No items to display");
        cut.Markup.ShouldContain("M9 3.75H6.912"); // Part of inbox icon SVG path
    }

    [Fact]
    public void MultiSelectTable_RendersEmptyStateWithCustomMessage()
    {
        // Arrange & Act
        IRenderedComponent<MultiSelectTable<TestItem>> cut = Render<MultiSelectTable<TestItem>>(parameters => parameters
            .Add(p => p.Items, Array.Empty<TestItem>())
            .Add(p => p.GetItemKey, item => item.Id)
            .Add(p => p.EmptyMessage, "No data available")
            .Add(p => p.HeaderContent, builder => builder.AddMarkupContent(0, "<th>Name</th>"))
            .Add(p => p.RowContent, item => builder => builder.AddMarkupContent(0, $"<td>{item.Name}</td>"))
            .Add(p => p.OnSelectionChanged, EventCallback.Factory.Create<HashSet<string>>(this, _ => { })));

        // Assert
        cut.Markup.ShouldContain("No data available");
    }

    [Fact]
    public void MultiSelectTable_RendersTableWithItems()
    {
        // Arrange
        List<TestItem> items = new()
        {
            new TestItem("1", "Item 1"),
            new TestItem("2", "Item 2"),
            new TestItem("3", "Item 3")
        };

        // Act
        IRenderedComponent<MultiSelectTable<TestItem>> cut = Render<MultiSelectTable<TestItem>>(parameters => parameters
            .Add(p => p.Items, items)
            .Add(p => p.GetItemKey, item => item.Id)
            .Add(p => p.HeaderContent, builder => builder.AddMarkupContent(0, "<th>Name</th>"))
            .Add(p => p.RowContent, item => builder => builder.AddMarkupContent(0, $"<td>{item.Name}</td>"))
            .Add(p => p.OnSelectionChanged, EventCallback.Factory.Create<HashSet<string>>(this, _ => { })));

        // Assert
        IReadOnlyList<IElement> rows = cut.FindAll("tbody tr");
        rows.Count.ShouldBe(3);
        cut.Markup.ShouldContain("Item 1");
        cut.Markup.ShouldContain("Item 2");
        cut.Markup.ShouldContain("Item 3");
    }

    [Fact]
    public void MultiSelectTable_SelectAllCheckboxPresent()
    {
        // Arrange
        List<TestItem> items = new() { new TestItem("1", "Item 1") };

        // Act
        IRenderedComponent<MultiSelectTable<TestItem>> cut = Render<MultiSelectTable<TestItem>>(parameters => parameters
            .Add(p => p.Items, items)
            .Add(p => p.GetItemKey, item => item.Id)
            .Add(p => p.AllowSelectAll, true)
            .Add(p => p.HeaderContent, builder => builder.AddMarkupContent(0, "<th>Name</th>"))
            .Add(p => p.RowContent, item => builder => builder.AddMarkupContent(0, $"<td>{item.Name}</td>"))
            .Add(p => p.OnSelectionChanged, EventCallback.Factory.Create<HashSet<string>>(this, _ => { })));

        // Assert
        IElement headerCheckbox = cut.Find("thead input[type='checkbox']");
        headerCheckbox.ShouldNotBeNull();
        headerCheckbox.GetAttribute("aria-label").ShouldBe("Select all items");
    }

    [Fact]
    public void MultiSelectTable_SelectAllCheckboxHidden()
    {
        // Arrange
        List<TestItem> items = new() { new TestItem("1", "Item 1") };

        // Act
        IRenderedComponent<MultiSelectTable<TestItem>> cut = Render<MultiSelectTable<TestItem>>(parameters => parameters
            .Add(p => p.Items, items)
            .Add(p => p.GetItemKey, item => item.Id)
            .Add(p => p.AllowSelectAll, false)
            .Add(p => p.HeaderContent, builder => builder.AddMarkupContent(0, "<th>Name</th>"))
            .Add(p => p.RowContent, item => builder => builder.AddMarkupContent(0, $"<td>{item.Name}</td>"))
            .Add(p => p.OnSelectionChanged, EventCallback.Factory.Create<HashSet<string>>(this, _ => { })));

        // Assert
        IReadOnlyList<IElement> headerCheckboxes = cut.FindAll("thead input[type='checkbox']");
        headerCheckboxes.ShouldBeEmpty();
    }

    [Fact]
    public void MultiSelectTable_SelectAllChecksAllItems()
    {
        // Arrange
        List<TestItem> items = new()
        {
            new TestItem("1", "Item 1"),
            new TestItem("2", "Item 2"),
            new TestItem("3", "Item 3")
        };
        HashSet<string>? capturedSelection = null;

        IRenderedComponent<MultiSelectTable<TestItem>> cut = Render<MultiSelectTable<TestItem>>(parameters => parameters
            .Add(p => p.Items, items)
            .Add(p => p.GetItemKey, item => item.Id)
            .Add(p => p.HeaderContent, builder => builder.AddMarkupContent(0, "<th>Name</th>"))
            .Add(p => p.RowContent, item => builder => builder.AddMarkupContent(0, $"<td>{item.Name}</td>"))
            .Add(p => p.OnSelectionChanged, EventCallback.Factory.Create<HashSet<string>>(this, sel => capturedSelection = sel)));

        // Act
        IElement selectAllCheckbox = cut.Find("thead input[type='checkbox']");
        selectAllCheckbox.Change(true);

        // Assert
        capturedSelection.ShouldNotBeNull();
        capturedSelection.Count.ShouldBe(3);
        capturedSelection.ShouldContain("1");
        capturedSelection.ShouldContain("2");
        capturedSelection.ShouldContain("3");
    }

    [Fact]
    public void MultiSelectTable_SelectAllUnchecksAllItems()
    {
        // Arrange
        List<TestItem> items = new()
        {
            new TestItem("1", "Item 1"),
            new TestItem("2", "Item 2")
        };
        HashSet<string> selectedKeys = new() { "1", "2" };
        HashSet<string>? capturedSelection = null;

        IRenderedComponent<MultiSelectTable<TestItem>> cut = Render<MultiSelectTable<TestItem>>(parameters => parameters
            .Add(p => p.Items, items)
            .Add(p => p.GetItemKey, item => item.Id)
            .Add(p => p.SelectedKeys, selectedKeys)
            .Add(p => p.HeaderContent, builder => builder.AddMarkupContent(0, "<th>Name</th>"))
            .Add(p => p.RowContent, item => builder => builder.AddMarkupContent(0, $"<td>{item.Name}</td>"))
            .Add(p => p.OnSelectionChanged, EventCallback.Factory.Create<HashSet<string>>(this, sel => capturedSelection = sel)));

        // Act
        IElement selectAllCheckbox = cut.Find("thead input[type='checkbox']");
        selectAllCheckbox.Change(false);

        // Assert
        capturedSelection.ShouldNotBeNull();
        capturedSelection.Count.ShouldBe(0);
    }

    [Fact]
    public void MultiSelectTable_IndividualRowSelectionWorks()
    {
        // Arrange
        List<TestItem> items = new() { new TestItem("1", "Item 1"), new TestItem("2", "Item 2") };
        HashSet<string>? capturedSelection = null;

        IRenderedComponent<MultiSelectTable<TestItem>> cut = Render<MultiSelectTable<TestItem>>(parameters => parameters
            .Add(p => p.Items, items)
            .Add(p => p.GetItemKey, item => item.Id)
            .Add(p => p.HeaderContent, builder => builder.AddMarkupContent(0, "<th>Name</th>"))
            .Add(p => p.RowContent, item => builder => builder.AddMarkupContent(0, $"<td>{item.Name}</td>"))
            .Add(p => p.OnSelectionChanged, EventCallback.Factory.Create<HashSet<string>>(this, sel => capturedSelection = sel)));

        // Act
        IReadOnlyList<IElement> rowCheckboxes = cut.FindAll("tbody input[type='checkbox']");
        rowCheckboxes[0].Change(true);

        // Assert
        capturedSelection.ShouldNotBeNull();
        capturedSelection.Count.ShouldBe(1);
        capturedSelection.ShouldContain("1");
    }

    [Fact]
    public void MultiSelectTable_IndividualRowDeselectionWorks()
    {
        // Arrange
        List<TestItem> items = new() { new TestItem("1", "Item 1") };
        HashSet<string> selectedKeys = new() { "1" };
        HashSet<string>? capturedSelection = null;

        IRenderedComponent<MultiSelectTable<TestItem>> cut = Render<MultiSelectTable<TestItem>>(parameters => parameters
            .Add(p => p.Items, items)
            .Add(p => p.GetItemKey, item => item.Id)
            .Add(p => p.SelectedKeys, selectedKeys)
            .Add(p => p.HeaderContent, builder => builder.AddMarkupContent(0, "<th>Name</th>"))
            .Add(p => p.RowContent, item => builder => builder.AddMarkupContent(0, $"<td>{item.Name}</td>"))
            .Add(p => p.OnSelectionChanged, EventCallback.Factory.Create<HashSet<string>>(this, sel => capturedSelection = sel)));

        // Act
        IElement rowCheckbox = cut.Find("tbody input[type='checkbox']");
        rowCheckbox.Change(false);

        // Assert
        capturedSelection.ShouldNotBeNull();
        capturedSelection.Count.ShouldBe(0);
    }

    [Fact]
    public void MultiSelectTable_SelectedRowHasHighlightClass()
    {
        // Arrange
        List<TestItem> items = new() { new TestItem("1", "Item 1") };
        HashSet<string> selectedKeys = new() { "1" };

        // Act
        IRenderedComponent<MultiSelectTable<TestItem>> cut = Render<MultiSelectTable<TestItem>>(parameters => parameters
            .Add(p => p.Items, items)
            .Add(p => p.GetItemKey, item => item.Id)
            .Add(p => p.SelectedKeys, selectedKeys)
            .Add(p => p.HeaderContent, builder => builder.AddMarkupContent(0, "<th>Name</th>"))
            .Add(p => p.RowContent, item => builder => builder.AddMarkupContent(0, $"<td>{item.Name}</td>"))
            .Add(p => p.OnSelectionChanged, EventCallback.Factory.Create<HashSet<string>>(this, _ => { })));

        // Assert
        IElement row = cut.Find("tbody tr");
        row.ClassList.ShouldContain("bg-blue-50");
    }

    [Fact]
    public void MultiSelectTable_RowCheckboxHasCorrectAriaLabel()
    {
        // Arrange
        List<TestItem> items = new() { new TestItem("AAPL", "Apple") };

        // Act
        IRenderedComponent<MultiSelectTable<TestItem>> cut = Render<MultiSelectTable<TestItem>>(parameters => parameters
            .Add(p => p.Items, items)
            .Add(p => p.GetItemKey, item => item.Id)
            .Add(p => p.HeaderContent, builder => builder.AddMarkupContent(0, "<th>Name</th>"))
            .Add(p => p.RowContent, item => builder => builder.AddMarkupContent(0, $"<td>{item.Name}</td>"))
            .Add(p => p.OnSelectionChanged, EventCallback.Factory.Create<HashSet<string>>(this, _ => { })));

        // Assert
        IElement rowCheckbox = cut.Find("tbody input[type='checkbox']");
        rowCheckbox.GetAttribute("aria-label").ShouldBe("Select AAPL");
    }

    [Fact]
    public void MultiSelectTable_SelectAllIsCheckedWhenAllItemsSelected()
    {
        // Arrange
        List<TestItem> items = new() { new TestItem("1", "Item 1"), new TestItem("2", "Item 2") };
        HashSet<string> selectedKeys = new() { "1", "2" };

        // Act
        IRenderedComponent<MultiSelectTable<TestItem>> cut = Render<MultiSelectTable<TestItem>>(parameters => parameters
            .Add(p => p.Items, items)
            .Add(p => p.GetItemKey, item => item.Id)
            .Add(p => p.SelectedKeys, selectedKeys)
            .Add(p => p.HeaderContent, builder => builder.AddMarkupContent(0, "<th>Name</th>"))
            .Add(p => p.RowContent, item => builder => builder.AddMarkupContent(0, $"<td>{item.Name}</td>"))
            .Add(p => p.OnSelectionChanged, EventCallback.Factory.Create<HashSet<string>>(this, _ => { })));

        // Assert
        IElement selectAllCheckbox = cut.Find("thead input[type='checkbox']");
        selectAllCheckbox.HasAttribute("checked").ShouldBeTrue();
    }
}
