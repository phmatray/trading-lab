using AngleSharp.Dom;
using Bunit;
using Microsoft.AspNetCore.Components;
using Shouldly;
using TradingStrat.ComponentTests.Infrastructure;
using TradingStrat.Web.Components.Shared;
using Xunit;

namespace TradingStrat.ComponentTests.Shared;

/// <summary>
/// Tests for the PaginationControls component.
/// </summary>
public class PaginationControlsTests : BunitTestContext
{
    [Fact]
    public void PaginationControls_DisplaysCurrentPage()
    {
        // Arrange & Act
        IRenderedComponent<PaginationControls> cut = Render<PaginationControls>(parameters => parameters
            .Add(p => p.CurrentPage, 2)
            .Add(p => p.TotalPages, 10)
            .Add(p => p.PageSize, 25)
            .Add(p => p.OnPageChanged, EventCallback.Factory.Create<int>(this, _ => { }))
            .Add(p => p.OnPageSizeChanged, EventCallback.Factory.Create<int>(this, _ => { })));

        // Assert
        cut.Markup.ShouldContain("Page 2 of 10");
    }

    [Fact]
    public void PaginationControls_PreviousButtonDisabledOnFirstPage()
    {
        // Arrange & Act
        IRenderedComponent<PaginationControls> cut = Render<PaginationControls>(parameters => parameters
            .Add(p => p.CurrentPage, 1)
            .Add(p => p.TotalPages, 5)
            .Add(p => p.PageSize, 25)
            .Add(p => p.OnPageChanged, EventCallback.Factory.Create<int>(this, _ => { }))
            .Add(p => p.OnPageSizeChanged, EventCallback.Factory.Create<int>(this, _ => { })));

        // Assert
        IElement prevButton = cut.FindAll("button").First();
        prevButton.HasAttribute("disabled").ShouldBeTrue();
        prevButton.ClassList.ShouldContain("opacity-50");
        prevButton.ClassList.ShouldContain("cursor-not-allowed");
    }

    [Fact]
    public void PaginationControls_NextButtonDisabledOnLastPage()
    {
        // Arrange & Act
        IRenderedComponent<PaginationControls> cut = Render<PaginationControls>(parameters => parameters
            .Add(p => p.CurrentPage, 5)
            .Add(p => p.TotalPages, 5)
            .Add(p => p.PageSize, 25)
            .Add(p => p.OnPageChanged, EventCallback.Factory.Create<int>(this, _ => { }))
            .Add(p => p.OnPageSizeChanged, EventCallback.Factory.Create<int>(this, _ => { })));

        // Assert
        IElement nextButton = cut.FindAll("button").Last();
        nextButton.HasAttribute("disabled").ShouldBeTrue();
        nextButton.ClassList.ShouldContain("opacity-50");
    }

    [Fact]
    public void PaginationControls_PreviousButtonNavigates()
    {
        // Arrange
        int? capturedPage = null;
        IRenderedComponent<PaginationControls> cut = Render<PaginationControls>(parameters => parameters
            .Add(p => p.CurrentPage, 3)
            .Add(p => p.TotalPages, 10)
            .Add(p => p.PageSize, 25)
            .Add(p => p.OnPageChanged, EventCallback.Factory.Create<int>(this, page => capturedPage = page))
            .Add(p => p.OnPageSizeChanged, EventCallback.Factory.Create<int>(this, _ => { })));

        // Act
        IElement prevButton = cut.FindAll("button").First();
        prevButton.Click();

        // Assert
        capturedPage.ShouldBe(2);
    }

    [Fact]
    public void PaginationControls_NextButtonNavigates()
    {
        // Arrange
        int? capturedPage = null;
        IRenderedComponent<PaginationControls> cut = Render<PaginationControls>(parameters => parameters
            .Add(p => p.CurrentPage, 3)
            .Add(p => p.TotalPages, 10)
            .Add(p => p.PageSize, 25)
            .Add(p => p.OnPageChanged, EventCallback.Factory.Create<int>(this, page => capturedPage = page))
            .Add(p => p.OnPageSizeChanged, EventCallback.Factory.Create<int>(this, _ => { })));

        // Act
        IElement nextButton = cut.FindAll("button").Last();
        nextButton.Click();

        // Assert
        capturedPage.ShouldBe(4);
    }

    [Fact]
    public void PaginationControls_PageSizeSelectorDisplaysOptions()
    {
        // Arrange & Act
        IRenderedComponent<PaginationControls> cut = Render<PaginationControls>(parameters => parameters
            .Add(p => p.CurrentPage, 1)
            .Add(p => p.TotalPages, 10)
            .Add(p => p.PageSize, 25)
            .Add(p => p.OnPageChanged, EventCallback.Factory.Create<int>(this, _ => { }))
            .Add(p => p.OnPageSizeChanged, EventCallback.Factory.Create<int>(this, _ => { })));

        // Assert
        IElement select = cut.Find("select");
        IHtmlCollection<IElement> options = select.QuerySelectorAll("option");
        options.Length.ShouldBe(4); // 25, 50, 100, 200
        options[0].GetAttribute("value").ShouldBe("25");
        options[1].GetAttribute("value").ShouldBe("50");
        options[2].GetAttribute("value").ShouldBe("100");
        options[3].GetAttribute("value").ShouldBe("200");
    }

    [Fact]
    public void PaginationControls_PageSizeChangeTriggersEvent()
    {
        // Arrange
        int? capturedPageSize = null;
        IRenderedComponent<PaginationControls> cut = Render<PaginationControls>(parameters => parameters
            .Add(p => p.CurrentPage, 1)
            .Add(p => p.TotalPages, 10)
            .Add(p => p.PageSize, 25)
            .Add(p => p.OnPageChanged, EventCallback.Factory.Create<int>(this, _ => { }))
            .Add(p => p.OnPageSizeChanged, EventCallback.Factory.Create<int>(this, size => capturedPageSize = size)));

        // Act
        IElement select = cut.Find("select");
        select.Change("50");

        // Assert
        capturedPageSize.ShouldBe(50);
    }

    [Fact]
    public void PaginationControls_ShowsCorrectPageRange()
    {
        // Arrange & Act
        IRenderedComponent<PaginationControls> cut = Render<PaginationControls>(parameters => parameters
            .Add(p => p.CurrentPage, 5)
            .Add(p => p.TotalPages, 10)
            .Add(p => p.PageSize, 50)
            .Add(p => p.OnPageChanged, EventCallback.Factory.Create<int>(this, _ => { }))
            .Add(p => p.OnPageSizeChanged, EventCallback.Factory.Create<int>(this, _ => { })));

        // Assert
        cut.Markup.ShouldContain("Page 5 of 10");
    }

    [Fact]
    public void PaginationControls_SinglePageHidesNavigation()
    {
        // Arrange & Act
        IRenderedComponent<PaginationControls> cut = Render<PaginationControls>(parameters => parameters
            .Add(p => p.CurrentPage, 1)
            .Add(p => p.TotalPages, 1)
            .Add(p => p.PageSize, 25)
            .Add(p => p.OnPageChanged, EventCallback.Factory.Create<int>(this, _ => { }))
            .Add(p => p.OnPageSizeChanged, EventCallback.Factory.Create<int>(this, _ => { })));

        // Assert - Both buttons should be disabled
        IReadOnlyList<IElement> buttons = cut.FindAll("button");
        buttons.ShouldAllBe(b => b.HasAttribute("disabled"));
    }

    [Fact]
    public void PaginationControls_HasCorrectAriaLabels()
    {
        // Arrange & Act
        IRenderedComponent<PaginationControls> cut = Render<PaginationControls>(parameters => parameters
            .Add(p => p.CurrentPage, 2)
            .Add(p => p.TotalPages, 5)
            .Add(p => p.PageSize, 25)
            .Add(p => p.OnPageChanged, EventCallback.Factory.Create<int>(this, _ => { }))
            .Add(p => p.OnPageSizeChanged, EventCallback.Factory.Create<int>(this, _ => { })));

        // Assert
        IReadOnlyList<IElement> buttons = cut.FindAll("button");
        buttons[0].GetAttribute("aria-label").ShouldBe("Previous page");
        buttons[1].GetAttribute("aria-label").ShouldBe("Next page");
    }
}
