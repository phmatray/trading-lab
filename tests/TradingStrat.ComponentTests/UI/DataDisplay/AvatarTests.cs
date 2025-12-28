using AngleSharp.Dom;
using Bunit;
using Shouldly;
using TradingStrat.ComponentTests.Infrastructure;
using TradingStrat.Web.Components.UI.DataDisplay;
using Xunit;

namespace TradingStrat.ComponentTests.UI.DataDisplay;

/// <summary>
/// Tests for the Avatar component.
/// </summary>
public class AvatarTests : BunitTestContext
{
    [Fact]
    public void Avatar_WithSrc_RendersImageElement()
    {
        // Arrange & Act
        IRenderedComponent<Avatar> cut = Render<Avatar>(parameters => parameters
            .Add(p => p.Src, "/images/user.jpg")
            .Add(p => p.Alt, "User avatar"));

        // Assert
        IElement img = cut.Find("img");
        img.ShouldNotBeNull();
        img.GetAttribute("src").ShouldBe("/images/user.jpg");
        img.GetAttribute("alt").ShouldBe("User avatar");
    }

    [Fact]
    public void Avatar_WithInitials_RendersSvgWithText()
    {
        // Arrange & Act
        IRenderedComponent<Avatar> cut = Render<Avatar>(parameters => parameters
            .Add(p => p.Initials, "AB"));

        // Assert
        IElement svg = cut.Find("svg");
        svg.ShouldNotBeNull();
        IElement text = cut.Find("text");
        text.ShouldNotBeNull();
        text.TextContent.ShouldBe("AB");
    }

    [Fact]
    public void Avatar_WithSquareTrue_AppliesRoundedMdClass()
    {
        // Arrange & Act
        IRenderedComponent<Avatar> cut = Render<Avatar>(parameters => parameters
            .Add(p => p.Initials, "AB")
            .Add(p => p.Square, true));

        // Assert
        cut.Markup.ShouldContain("rounded-[20%]");
        cut.Markup.ShouldNotContain("rounded-full");
    }

    [Fact]
    public void Avatar_WithSquareFalse_AppliesRoundedFullClass()
    {
        // Arrange & Act
        IRenderedComponent<Avatar> cut = Render<Avatar>(parameters => parameters
            .Add(p => p.Initials, "AB")
            .Add(p => p.Square, false));

        // Assert
        cut.Markup.ShouldContain("rounded-full");
    }

    [Fact]
    public void Avatar_AppliesDefaultSizeClasses()
    {
        // Arrange & Act
        IRenderedComponent<Avatar> cut = Render<Avatar>(parameters => parameters
            .Add(p => p.Initials, "AB"));

        // Assert
        cut.Markup.ShouldContain("size-10");
    }

    [Fact]
    public void Avatar_WithCustomClass_AppendsToExistingClasses()
    {
        // Arrange & Act
        IRenderedComponent<Avatar> cut = Render<Avatar>(parameters => parameters
            .Add(p => p.Initials, "CD")
            .Add(p => p.Class, "custom-avatar"));

        // Assert
        IElement span = cut.Find("span");
        span.ClassList.ShouldContain("custom-avatar");
        span.ClassList.ShouldContain("size-10");
    }

    [Fact]
    public void Avatar_InitialsSvg_CentersText()
    {
        // Arrange & Act
        IRenderedComponent<Avatar> cut = Render<Avatar>(parameters => parameters
            .Add(p => p.Initials, "XY"));

        // Assert
        IElement text = cut.Find("text");
        text.GetAttribute("x").ShouldBe("50%");
        text.GetAttribute("y").ShouldBe("50%");
        text.GetAttribute("text-anchor").ShouldBe("middle");
        text.GetAttribute("dominant-baseline").ShouldBe("middle");
    }

    [Fact]
    public void Avatar_Image_AppliesObjectCoverClass()
    {
        // Arrange & Act
        IRenderedComponent<Avatar> cut = Render<Avatar>(parameters => parameters
            .Add(p => p.Src, "/avatar.png")
            .Add(p => p.Alt, "Avatar"));

        // Assert
        IElement img = cut.Find("img");
        img.ClassList.ShouldContain("object-cover");
    }

    [Fact]
    public void Avatar_InitialsSvg_FillsContainer()
    {
        // Arrange & Act
        IRenderedComponent<Avatar> cut = Render<Avatar>(parameters => parameters
            .Add(p => p.Initials, "TU"));

        // Assert
        IElement svg = cut.Find("svg");
        svg.GetAttribute("width").ShouldBe("100%");
        svg.GetAttribute("height").ShouldBe("100%");
    }
}
