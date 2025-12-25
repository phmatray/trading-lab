using Bunit;
using Shouldly;
using TradingStrat.ComponentTests.Infrastructure;
using TradingStrat.Web.Components.Shared;
using TradingStrat.Web.Models;
using Xunit;

namespace TradingStrat.ComponentTests.Shared;

/// <summary>
/// Tests for the AlertMessage component.
/// </summary>
public class AlertMessageTests : BunitTestContext
{
    [Fact]
    public void AlertMessage_WithEmptyMessage_RendersNothing()
    {
        // Arrange & Act
        var cut = Render<AlertMessage>(parameters => parameters
            .Add(p => p.Message, string.Empty));

        // Assert
        cut.Markup.ShouldBeEmpty();
    }

    [Fact]
    public void AlertMessage_WithNullMessage_RendersNothing()
    {
        // Arrange & Act
        var cut = Render<AlertMessage>(parameters => parameters
            .Add(p => p.Message, null!));

        // Assert
        cut.Markup.ShouldBeEmpty();
    }

    [Fact]
    public void AlertMessage_WithMessage_DisplaysMessage()
    {
        // Arrange
        string message = "This is an alert message";

        // Act
        var cut = Render<AlertMessage>(parameters => parameters
            .Add(p => p.Message, message));

        // Assert
        var messageElement = cut.Find(".text-sm.font-medium");
        messageElement.TextContent.ShouldBe(message);
    }

    [Fact]
    public void AlertMessage_Success_AppliesGreenStyling()
    {
        // Arrange & Act
        var cut = Render<AlertMessage>(parameters => parameters
            .Add(p => p.Message, "Success message")
            .Add(p => p.Type, AlertType.Success));

        // Assert
        var alert = cut.Find("div[role='alert']");
        alert.ClassList.ShouldContain("bg-green-50");
        alert.ClassList.ShouldContain("border-green-200");
        alert.ClassList.ShouldContain("text-green-800");
    }

    [Fact]
    public void AlertMessage_Error_AppliesRedStyling()
    {
        // Arrange & Act
        var cut = Render<AlertMessage>(parameters => parameters
            .Add(p => p.Message, "Error message")
            .Add(p => p.Type, AlertType.Error));

        // Assert
        var alert = cut.Find("div[role='alert']");
        alert.ClassList.ShouldContain("bg-red-50");
        alert.ClassList.ShouldContain("border-red-200");
        alert.ClassList.ShouldContain("text-red-800");
    }

    [Fact]
    public void AlertMessage_Warning_AppliesYellowStyling()
    {
        // Arrange & Act
        var cut = Render<AlertMessage>(parameters => parameters
            .Add(p => p.Message, "Warning message")
            .Add(p => p.Type, AlertType.Warning));

        // Assert
        var alert = cut.Find("div[role='alert']");
        alert.ClassList.ShouldContain("bg-yellow-50");
        alert.ClassList.ShouldContain("border-yellow-200");
        alert.ClassList.ShouldContain("text-yellow-800");
    }

    [Fact]
    public void AlertMessage_Info_AppliesBlueStyling()
    {
        // Arrange & Act
        var cut = Render<AlertMessage>(parameters => parameters
            .Add(p => p.Message, "Info message")
            .Add(p => p.Type, AlertType.Info));

        // Assert
        var alert = cut.Find("div[role='alert']");
        alert.ClassList.ShouldContain("bg-blue-50");
        alert.ClassList.ShouldContain("border-blue-200");
        alert.ClassList.ShouldContain("text-blue-800");
    }

    [Theory]
    [InlineData(AlertType.Success)]
    [InlineData(AlertType.Error)]
    [InlineData(AlertType.Warning)]
    [InlineData(AlertType.Info)]
    public void AlertMessage_AllTypes_RenderWithIcon(AlertType type)
    {
        // Arrange & Act
        var cut = Render<AlertMessage>(parameters => parameters
            .Add(p => p.Message, $"{type} message")
            .Add(p => p.Type, type));

        // Assert
        var icon = cut.Find("svg");
        icon.ShouldNotBeNull();
        icon.ClassList.ShouldContain("w-5");
        icon.ClassList.ShouldContain("h-5");
    }
}
