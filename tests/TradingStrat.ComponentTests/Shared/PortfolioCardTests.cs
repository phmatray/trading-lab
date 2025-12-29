using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Shouldly;
using TradingStrat.Web.Components.Shared;
using Xunit;

namespace TradingStrat.ComponentTests.Shared;

/// <summary>
/// Tests for the PortfolioCard component.
/// </summary>
public class PortfolioCardTests : BunitTestContext
{
    [Fact]
    public void PortfolioCard_WithRequiredProps_RendersCorrectly()
    {
        // Arrange & Act
        var cut = RenderComponent<PortfolioCard>(parameters => parameters
            .Add(p => p.Name, "Test Portfolio")
            .Add(p => p.Cash, 10000m)
            .Add(p => p.PositionCount, 5)
            .Add(p => p.CreatedAt, new DateTime(2024, 1, 15)));

        // Assert
        cut.Find("h3").TextContent.ShouldBe("Test Portfolio");
        cut.Markup.ShouldContain("$10,000.00");
        cut.Markup.ShouldContain("5");
        cut.Markup.ShouldContain("Jan 15, 2024");
    }

    [Fact]
    public void PortfolioCard_WithDescription_RendersDescription()
    {
        // Arrange & Act
        var cut = RenderComponent<PortfolioCard>(parameters => parameters
            .Add(p => p.Name, "Test")
            .Add(p => p.Description, "My test portfolio")
            .Add(p => p.Cash, 5000m)
            .Add(p => p.PositionCount, 3)
            .Add(p => p.CreatedAt, DateTime.Now));

        // Assert
        cut.Markup.ShouldContain("My test portfolio");
    }

    [Fact]
    public void PortfolioCard_WithoutDescription_DoesNotRenderDescriptionElement()
    {
        // Arrange & Act
        var cut = RenderComponent<PortfolioCard>(parameters => parameters
            .Add(p => p.Name, "Test")
            .Add(p => p.Description, (string?)null)
            .Add(p => p.Cash, 5000m)
            .Add(p => p.PositionCount, 3)
            .Add(p => p.CreatedAt, DateTime.Now));

        // Assert
        // Should only have one text component (the name heading)
        var textComponents = cut.FindAll("p");
        textComponents.Count.ShouldBe(0, "No description paragraph should be rendered when Description is null");
    }

    [Fact]
    public void PortfolioCard_OnClick_InvokesCallback()
    {
        // Arrange
        bool clicked = false;
        var cut = RenderComponent<PortfolioCard>(parameters => parameters
            .Add(p => p.Name, "Test")
            .Add(p => p.Cash, 5000m)
            .Add(p => p.PositionCount, 3)
            .Add(p => p.CreatedAt, DateTime.Now)
            .Add(p => p.OnClick, EventCallback.Factory.Create(this, () => clicked = true)));

        // Act
        var card = cut.Find(".bg-white");
        card.Click();

        // Assert
        clicked.ShouldBeTrue();
    }

    [Fact]
    public void PortfolioCard_EnterKey_InvokesOnClick()
    {
        // Arrange
        bool clicked = false;
        var cut = RenderComponent<PortfolioCard>(parameters => parameters
            .Add(p => p.Name, "Test")
            .Add(p => p.Cash, 5000m)
            .Add(p => p.PositionCount, 3)
            .Add(p => p.CreatedAt, DateTime.Now)
            .Add(p => p.OnClick, EventCallback.Factory.Create(this, () => clicked = true)));

        // Act
        var card = cut.Find(".bg-white");
        card.KeyDown(new KeyboardEventArgs { Key = "Enter" });

        // Assert
        clicked.ShouldBeTrue();
    }

    [Fact]
    public void PortfolioCard_SpaceKey_InvokesOnClick()
    {
        // Arrange
        bool clicked = false;
        var cut = RenderComponent<PortfolioCard>(parameters => parameters
            .Add(p => p.Name, "Test")
            .Add(p => p.Cash, 5000m)
            .Add(p => p.PositionCount, 3)
            .Add(p => p.CreatedAt, DateTime.Now)
            .Add(p => p.OnClick, EventCallback.Factory.Create(this, () => clicked = true)));

        // Act
        var card = cut.Find(".bg-white");
        card.KeyDown(new KeyboardEventArgs { Key = " " });

        // Assert
        clicked.ShouldBeTrue();
    }

    [Fact]
    public void PortfolioCard_OtherKey_DoesNotInvokeOnClick()
    {
        // Arrange
        bool clicked = false;
        var cut = RenderComponent<PortfolioCard>(parameters => parameters
            .Add(p => p.Name, "Test")
            .Add(p => p.Cash, 5000m)
            .Add(p => p.PositionCount, 3)
            .Add(p => p.CreatedAt, DateTime.Now)
            .Add(p => p.OnClick, EventCallback.Factory.Create(this, () => clicked = true)));

        // Act
        var card = cut.Find(".bg-white");
        card.KeyDown(new KeyboardEventArgs { Key = "a" });

        // Assert
        clicked.ShouldBeFalse();
    }

    [Fact]
    public void PortfolioCard_DeleteButton_InvokesOnDelete()
    {
        // Arrange
        MouseEventArgs? capturedArgs = null;
        var cut = RenderComponent<PortfolioCard>(parameters => parameters
            .Add(p => p.Name, "Test Portfolio")
            .Add(p => p.Cash, 5000m)
            .Add(p => p.PositionCount, 3)
            .Add(p => p.CreatedAt, DateTime.Now)
            .Add(p => p.OnDelete, EventCallback.Factory.Create<MouseEventArgs>(this, args => capturedArgs = args)));

        // Act
        var deleteButton = cut.Find("button[aria-label^='Delete portfolio']");
        deleteButton.Click();

        // Assert
        capturedArgs.ShouldNotBeNull();
    }

    [Fact]
    public void PortfolioCard_DeleteButton_DoesNotTriggerCardClick()
    {
        // Arrange
        bool cardClicked = false;
        bool deleteClicked = false;

        var cut = RenderComponent<PortfolioCard>(parameters => parameters
            .Add(p => p.Name, "Test")
            .Add(p => p.Cash, 5000m)
            .Add(p => p.PositionCount, 3)
            .Add(p => p.CreatedAt, DateTime.Now)
            .Add(p => p.OnClick, EventCallback.Factory.Create(this, () => cardClicked = true))
            .Add(p => p.OnDelete, EventCallback.Factory.Create<MouseEventArgs>(this, _ => deleteClicked = true)));

        // Act
        var deleteButton = cut.Find("button[aria-label^='Delete portfolio']");
        deleteButton.Click();

        // Assert
        deleteClicked.ShouldBeTrue();
        cardClicked.ShouldBeFalse("Card onClick should not trigger when delete button clicked (stopPropagation)");
    }

    [Fact]
    public void PortfolioCard_HasAccessibilityAttributes()
    {
        // Arrange & Act
        var cut = RenderComponent<PortfolioCard>(parameters => parameters
            .Add(p => p.Name, "Accessible Portfolio")
            .Add(p => p.Cash, 5000m)
            .Add(p => p.PositionCount, 3)
            .Add(p => p.CreatedAt, DateTime.Now));

        // Assert
        var card = cut.Find(".bg-white");
        card.GetAttribute("role").ShouldBe("button");
        card.GetAttribute("tabindex").ShouldBe("0");
        card.GetAttribute("aria-label").ShouldContain("Accessible Portfolio");

        var deleteButton = cut.Find("button");
        deleteButton.GetAttribute("aria-label").ShouldContain("Delete portfolio");
        deleteButton.GetAttribute("title").ShouldContain("Delete");
    }

    [Fact]
    public void PortfolioCard_HasCorrectStyling()
    {
        // Arrange & Act
        var cut = RenderComponent<PortfolioCard>(parameters => parameters
            .Add(p => p.Name, "Test")
            .Add(p => p.Cash, 5000m)
            .Add(p => p.PositionCount, 3)
            .Add(p => p.CreatedAt, DateTime.Now));

        // Assert
        var card = cut.Find(".bg-white");
        var classes = card.GetAttribute("class");

        classes.ShouldContain("dark:bg-zinc-900");
        classes.ShouldContain("rounded-lg");
        classes.ShouldContain("border");
        classes.ShouldContain("hover:border-gray-300");
        classes.ShouldContain("cursor-pointer");
        classes.ShouldContain("focus-within:ring-2");
    }

    [Fact]
    public void PortfolioCard_DisplaysCashBalance()
    {
        // Arrange & Act
        var cut = RenderComponent<PortfolioCard>(parameters => parameters
            .Add(p => p.Name, "Test")
            .Add(p => p.Cash, 25678.50m)
            .Add(p => p.PositionCount, 3)
            .Add(p => p.CreatedAt, DateTime.Now));

        // Assert
        cut.Markup.ShouldContain("Cash Balance");
        cut.Markup.ShouldContain("$25,678.50");
    }

    [Fact]
    public void PortfolioCard_DisplaysPositionCount()
    {
        // Arrange & Act
        var cut = RenderComponent<PortfolioCard>(parameters => parameters
            .Add(p => p.Name, "Test")
            .Add(p => p.Cash, 5000m)
            .Add(p => p.PositionCount, 12)
            .Add(p => p.CreatedAt, DateTime.Now));

        // Assert
        cut.Markup.ShouldContain("Positions");
        cut.Markup.ShouldContain("12");
    }

    [Fact]
    public void PortfolioCard_DisplaysCreationDate()
    {
        // Arrange
        var createdDate = new DateTime(2024, 3, 20);

        // Act
        var cut = RenderComponent<PortfolioCard>(parameters => parameters
            .Add(p => p.Name, "Test")
            .Add(p => p.Cash, 5000m)
            .Add(p => p.PositionCount, 3)
            .Add(p => p.CreatedAt, createdDate));

        // Assert
        cut.Markup.ShouldContain("Created");
        cut.Markup.ShouldContain("Mar 20, 2024");
    }

    [Fact]
    public void PortfolioCard_DeleteButtonSVG_IsHiddenFromScreenReaders()
    {
        // Arrange & Act
        var cut = RenderComponent<PortfolioCard>(parameters => parameters
            .Add(p => p.Name, "Test")
            .Add(p => p.Cash, 5000m)
            .Add(p => p.PositionCount, 3)
            .Add(p => p.CreatedAt, DateTime.Now));

        // Assert
        var svg = cut.Find("svg");
        svg.GetAttribute("aria-hidden").ShouldBe("true");
    }
}
