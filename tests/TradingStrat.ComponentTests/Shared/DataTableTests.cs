using AngleSharp.Dom;
using Bunit;
using Shouldly;
using TradingStrat.ComponentTests.Infrastructure;
using TradingStrat.Web.Components.Shared;
using Xunit;

namespace TradingStrat.ComponentTests.Shared;

/// <summary>
/// Tests for the DataTable component.
/// </summary>
public class DataTableTests : BunitTestContext
{
    [Fact]
    public void DataTable_WhenLoading_DisplaysLoadingSpinner()
    {
        // Arrange & Act
        IRenderedComponent<DataTable> cut = Render<DataTable>(parameters => parameters
            .Add(p => p.IsLoading, true));

        // Assert
        cut.Markup.ShouldContain("Loading data...");
        IElement spinner = cut.Find("div.animate-spin");
        spinner.ShouldNotBeNull();
    }

    [Fact]
    public void DataTable_WhenLoading_DoesNotDisplayTable()
    {
        // Arrange & Act
        IRenderedComponent<DataTable> cut = Render<DataTable>(parameters => parameters
            .Add(p => p.IsLoading, true));

        // Assert
        IReadOnlyList<IElement> tables = cut.FindAll("table");
        tables.ShouldBeEmpty();
    }

    [Fact]
    public void DataTable_WhenEmpty_DisplaysEmptyMessage()
    {
        // Arrange
        string emptyMessage = "No records found";

        // Act
        IRenderedComponent<DataTable> cut = Render<DataTable>(parameters => parameters
            .Add(p => p.IsEmpty, true)
            .Add(p => p.EmptyMessage, emptyMessage));

        // Assert
        cut.Markup.ShouldContain(emptyMessage);
        IElement emptyIcon = cut.Find("svg");
        emptyIcon.ShouldNotBeNull();
    }

    [Fact]
    public void DataTable_WhenEmpty_UsesDefaultEmptyMessage()
    {
        // Arrange & Act
        IRenderedComponent<DataTable> cut = Render<DataTable>(parameters => parameters
            .Add(p => p.IsEmpty, true));

        // Assert
        cut.Markup.ShouldContain("No data available");
    }

    [Fact]
    public void DataTable_WhenEmptyWithActions_DisplaysActions()
    {
        // Arrange & Act
        IRenderedComponent<DataTable> cut = Render<DataTable>(parameters => parameters
            .Add(p => p.IsEmpty, true)
            .Add(p => p.EmptyActions, builder =>
            {
                builder.OpenElement(0, "button");
                builder.AddAttribute(1, "class", "test-action");
                builder.AddContent(2, "Add Data");
                builder.CloseElement();
            }));

        // Assert
        IElement actionButton = cut.Find("button.test-action");
        actionButton.ShouldNotBeNull();
        actionButton.TextContent.ShouldBe("Add Data");
    }

    [Fact]
    public void DataTable_WithData_DisplaysTable()
    {
        // Arrange & Act
        IRenderedComponent<DataTable> cut = Render<DataTable>(parameters => parameters
            .Add(p => p.IsLoading, false)
            .Add(p => p.IsEmpty, false)
            .Add(p => p.TableBody, builder =>
            {
                builder.OpenElement(0, "tr");
                builder.AddContent(1, "Test Row");
                builder.CloseElement();
            }));

        // Assert
        IElement table = cut.Find("table");
        table.ShouldNotBeNull();
        table.ClassList.ShouldContain("min-w-full");
    }

    [Fact]
    public void DataTable_WithTableHeader_RendersHeader()
    {
        // Arrange & Act
        IRenderedComponent<DataTable> cut = Render<DataTable>(parameters => parameters
            .Add(p => p.TableHeader, builder =>
            {
                builder.OpenElement(0, "tr");
                builder.OpenElement(1, "th");
                builder.AddContent(2, "Column 1");
                builder.CloseElement();
                builder.CloseElement();
            }));

        // Assert
        IElement thead = cut.Find("thead");
        thead.ShouldNotBeNull();
        thead.ClassList.ShouldContain("bg-gray-50");
        thead.TextContent.ShouldContain("Column 1");
    }

    [Fact]
    public void DataTable_WithTableBody_RendersBody()
    {
        // Arrange & Act
        IRenderedComponent<DataTable> cut = Render<DataTable>(parameters => parameters
            .Add(p => p.TableBody, builder =>
            {
                builder.OpenElement(0, "tr");
                builder.OpenElement(1, "td");
                builder.AddContent(2, "Cell Data");
                builder.CloseElement();
                builder.CloseElement();
            }));

        // Assert
        IElement tbody = cut.Find("tbody");
        tbody.ShouldNotBeNull();
        tbody.ClassList.ShouldContain("bg-white");
        tbody.TextContent.ShouldContain("Cell Data");
    }

    [Fact]
    public void DataTable_WithTableFooter_RendersFooter()
    {
        // Arrange & Act
        IRenderedComponent<DataTable> cut = Render<DataTable>(parameters => parameters
            .Add(p => p.TableFooter, builder =>
            {
                builder.OpenElement(0, "tr");
                builder.OpenElement(1, "td");
                builder.AddContent(2, "Footer Content");
                builder.CloseElement();
                builder.CloseElement();
            }));

        // Assert
        IElement tfoot = cut.Find("tfoot");
        tfoot.ShouldNotBeNull();
        tfoot.ClassList.ShouldContain("bg-gray-50");
        tfoot.TextContent.ShouldContain("Footer Content");
    }

    [Fact]
    public void DataTable_WithCompleteTable_RendersAllParts()
    {
        // Arrange & Act
        IRenderedComponent<DataTable> cut = Render<DataTable>(parameters => parameters
            .Add(p => p.TableHeader, builder =>
            {
                builder.OpenElement(0, "tr");
                builder.OpenElement(1, "th");
                builder.AddContent(2, "Header");
                builder.CloseElement();
                builder.CloseElement();
            })
            .Add(p => p.TableBody, builder =>
            {
                builder.OpenElement(0, "tr");
                builder.OpenElement(1, "td");
                builder.AddContent(2, "Body");
                builder.CloseElement();
                builder.CloseElement();
            })
            .Add(p => p.TableFooter, builder =>
            {
                builder.OpenElement(0, "tr");
                builder.OpenElement(1, "td");
                builder.AddContent(2, "Footer");
                builder.CloseElement();
                builder.CloseElement();
            }));

        // Assert
        IElement table = cut.Find("table");
        table.ShouldNotBeNull();

        IElement thead = cut.Find("thead");
        thead.ShouldNotBeNull();
        thead.TextContent.ShouldContain("Header");

        IElement tbody = cut.Find("tbody");
        tbody.ShouldNotBeNull();
        tbody.TextContent.ShouldContain("Body");

        IElement tfoot = cut.Find("tfoot");
        tfoot.ShouldNotBeNull();
        tfoot.TextContent.ShouldContain("Footer");
    }

    [Fact]
    public void DataTable_HasCorrectTestId()
    {
        // Arrange & Act
        IRenderedComponent<DataTable> cut = Render<DataTable>();

        // Assert
        IElement container = cut.Find("[data-testid='data-table']");
        container.ShouldNotBeNull();
        container.ClassList.ShouldContain("card");
    }

    [Fact]
    public void DataTable_LoadingTakesPriorityOverEmpty()
    {
        // Arrange & Act
        IRenderedComponent<DataTable> cut = Render<DataTable>(parameters => parameters
            .Add(p => p.IsLoading, true)
            .Add(p => p.IsEmpty, true));

        // Assert
        // Should show loading, not empty state
        cut.Markup.ShouldContain("Loading data...");
        cut.Markup.ShouldNotContain("No data available");
    }
}
