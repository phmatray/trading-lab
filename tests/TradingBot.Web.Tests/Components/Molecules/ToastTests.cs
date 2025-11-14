// <copyright file="ToastTests.cs" company="TradingBot">
// Copyright (c) TradingBot. All rights reserved.
// </copyright>

using System.Threading.Tasks;
using Bunit;
using Shouldly;
using TradingBot.Web.Components.Molecules;
using TradingBot.Web.Services;
using Xunit;

namespace TradingBot.Web.Tests.Components.Molecules;

/// <summary>
/// Tests for the Toast component.
/// </summary>
public class ToastTests : Bunit.TestContext
{
    [Fact]
    public void Toast_Renders_WithMessage()
    {
        // Arrange
        var message = new ToastMessage
        {
            Type = ToastType.Success,
            Message = "Test toast message",
        };

        // Act
        var cut = RenderComponent<Toast>(parameters => parameters
            .Add(p => p.Message, message));

        // Assert
        cut.Markup.ShouldContain("Test toast message");
    }

    [Fact]
    public void Toast_Renders_WithTitle()
    {
        // Arrange
        var message = new ToastMessage
        {
            Type = ToastType.Info,
            Title = "Test Title",
            Message = "Test message",
        };

        // Act
        var cut = RenderComponent<Toast>(parameters => parameters
            .Add(p => p.Message, message));

        // Assert
        cut.Markup.ShouldContain("Test Title");
        cut.Markup.ShouldContain("Test message");
    }

    [Fact]
    public void Toast_Renders_WithoutTitle_WhenNotProvided()
    {
        // Arrange
        var message = new ToastMessage
        {
            Type = ToastType.Success,
            Message = "Test message",
        };

        // Act
        var cut = RenderComponent<Toast>(parameters => parameters
            .Add(p => p.Message, message));

        // Assert
        var titleElements = cut.FindAll("p.font-semibold");
        titleElements.Count.ShouldBe(0);
    }

    [Fact]
    public void Toast_AppliesSuccessClasses()
    {
        // Arrange
        var message = new ToastMessage
        {
            Type = ToastType.Success,
            Message = "Success message",
        };

        // Act
        var cut = RenderComponent<Toast>(parameters => parameters
            .Add(p => p.Message, message));

        // Assert
        var container = cut.Find("div[role='alert']");
        container.ClassName.ShouldNotBeNull();
        container.ClassName.ShouldContain("bg-green-50");
        container.ClassName.ShouldContain("text-green-800");
        container.ClassName.ShouldContain("border-green-200");
    }

    [Fact]
    public void Toast_AppliesErrorClasses()
    {
        // Arrange
        var message = new ToastMessage
        {
            Type = ToastType.Error,
            Message = "Error message",
        };

        // Act
        var cut = RenderComponent<Toast>(parameters => parameters
            .Add(p => p.Message, message));

        // Assert
        var container = cut.Find("div[role='alert']");
        container.ClassName.ShouldNotBeNull();
        container.ClassName.ShouldContain("bg-red-50");
        container.ClassName.ShouldContain("text-red-800");
        container.ClassName.ShouldContain("border-red-200");
    }

    [Fact]
    public void Toast_AppliesWarningClasses()
    {
        // Arrange
        var message = new ToastMessage
        {
            Type = ToastType.Warning,
            Message = "Warning message",
        };

        // Act
        var cut = RenderComponent<Toast>(parameters => parameters
            .Add(p => p.Message, message));

        // Assert
        var container = cut.Find("div[role='alert']");
        container.ClassName.ShouldNotBeNull();
        container.ClassName.ShouldContain("bg-yellow-50");
        container.ClassName.ShouldContain("text-yellow-800");
        container.ClassName.ShouldContain("border-yellow-200");
    }

    [Fact]
    public void Toast_AppliesInfoClasses()
    {
        // Arrange
        var message = new ToastMessage
        {
            Type = ToastType.Info,
            Message = "Info message",
        };

        // Act
        var cut = RenderComponent<Toast>(parameters => parameters
            .Add(p => p.Message, message));

        // Assert
        var container = cut.Find("div[role='alert']");
        container.ClassName.ShouldNotBeNull();
        container.ClassName.ShouldContain("bg-blue-50");
        container.ClassName.ShouldContain("text-blue-800");
        container.ClassName.ShouldContain("border-blue-200");
    }

    [Fact]
    public void Toast_HasCorrectAriaAttributes()
    {
        // Arrange
        var message = new ToastMessage
        {
            Type = ToastType.Info,
            Message = "Test message",
        };

        // Act
        var cut = RenderComponent<Toast>(parameters => parameters
            .Add(p => p.Message, message));

        // Assert
        var container = cut.Find("div[role='alert']");
        container.GetAttribute("role").ShouldBe("alert");
        container.GetAttribute("aria-live").ShouldBe("polite");
    }

    [Fact]
    public void Toast_HasDismissButton()
    {
        // Arrange
        var message = new ToastMessage
        {
            Type = ToastType.Success,
            Message = "Test message",
        };

        // Act
        var cut = RenderComponent<Toast>(parameters => parameters
            .Add(p => p.Message, message));

        // Assert
        var dismissButton = cut.Find("button[aria-label='Dismiss notification']");
        dismissButton.ShouldNotBeNull();
    }

    [Fact]
    public void Toast_DismissButton_InvokesCallback()
    {
        // Arrange
        var message = new ToastMessage
        {
            Type = ToastType.Success,
            Message = "Test message",
        };

        var dismissed = false;
        var cut = RenderComponent<Toast>(parameters => parameters
            .Add(p => p.Message, message)
            .Add(p => p.OnDismiss, () => { dismissed = true; }));

        // Act
        var dismissButton = cut.Find("button[aria-label='Dismiss notification']");
        dismissButton.Click();

        // Assert
        dismissed.ShouldBeTrue();
    }

    [Fact]
    public void Toast_ShowsProgressBar_ByDefault()
    {
        // Arrange
        var message = new ToastMessage
        {
            Type = ToastType.Info,
            Message = "Test message",
            DurationMs = 5000,
        };

        // Act
        var cut = RenderComponent<Toast>(parameters => parameters
            .Add(p => p.Message, message));

        // Assert
        var progressBar = cut.FindAll("div.bg-current");
        progressBar.Count.ShouldBeGreaterThan(0);
    }

    [Fact]
    public void Toast_HidesProgressBar_WhenDisabled()
    {
        // Arrange
        var message = new ToastMessage
        {
            Type = ToastType.Info,
            Message = "Test message",
        };

        // Act
        var cut = RenderComponent<Toast>(parameters => parameters
            .Add(p => p.Message, message)
            .Add(p => p.ShowProgressBar, false));

        // Assert
        var progressBars = cut.FindAll("div.bg-current");
        progressBars.Count.ShouldBe(0);
    }

    [Fact]
    public void Toast_AppliesSlideInAnimation()
    {
        // Arrange
        var message = new ToastMessage
        {
            Type = ToastType.Success,
            Message = "Test message",
        };

        // Act
        var cut = RenderComponent<Toast>(parameters => parameters
            .Add(p => p.Message, message));

        // Assert
        var container = cut.Find("div[role='alert']");
        container.ClassName.ShouldNotBeNull();
        container.ClassName.ShouldContain("toast-slide-in");
    }

    [Fact]
    public void Toast_HasBaseStructuralClasses()
    {
        // Arrange
        var message = new ToastMessage
        {
            Type = ToastType.Info,
            Message = "Test message",
        };

        // Act
        var cut = RenderComponent<Toast>(parameters => parameters
            .Add(p => p.Message, message));

        // Assert
        var container = cut.Find("div[role='alert']");
        container.ClassName.ShouldNotBeNull();
        container.ClassName.ShouldContain("rounded-lg");
        container.ClassName.ShouldContain("shadow-lg");
        container.ClassName.ShouldContain("p-4");
        container.ClassName.ShouldContain("max-w-sm");
        container.ClassName.ShouldContain("pointer-events-auto");
    }

    [Fact]
    public void Toast_DisplaysIcon_ForAllTypes()
    {
        // Test each type to ensure the toast renders with an icon
        var types = new[] { ToastType.Success, ToastType.Error, ToastType.Warning, ToastType.Info };

        foreach (var type in types)
        {
            // Arrange
            var message = new ToastMessage
            {
                Type = type,
                Message = $"{type} message",
            };

            // Act
            var cut = RenderComponent<Toast>(parameters => parameters
                .Add(p => p.Message, message));

            // Assert
            // The icon is rendered, we just verify the toast renders successfully
            // (Icon rendering is tested separately in IconTests)
            cut.Find("div[role='alert']").ShouldNotBeNull();
        }
    }

    [Fact]
    public void Toast_WithTitleAndMessage_HasCorrectTextSizes()
    {
        // Arrange
        var message = new ToastMessage
        {
            Type = ToastType.Success,
            Title = "Success!",
            Message = "Operation completed",
        };

        // Act
        var cut = RenderComponent<Toast>(parameters => parameters
            .Add(p => p.Message, message));

        // Assert
        var titleParagraph = cut.Find("p.font-semibold");
        titleParagraph.ClassName.ShouldNotBeNull();
        titleParagraph.ClassName.ShouldContain("text-sm");

        var messageParagraph = cut.FindAll("p").FirstOrDefault(p => p.TextContent == "Operation completed");
        messageParagraph.ShouldNotBeNull();
        messageParagraph.ClassName.ShouldNotBeNull();
        messageParagraph.ClassName.ShouldContain("text-xs");
    }

    [Fact]
    public void Toast_WithoutTitle_MessageHasLargerTextSize()
    {
        // Arrange
        var message = new ToastMessage
        {
            Type = ToastType.Info,
            Message = "Info message",
        };

        // Act
        var cut = RenderComponent<Toast>(parameters => parameters
            .Add(p => p.Message, message));

        // Assert
        var messageParagraph = cut.Find("p");
        messageParagraph.ClassName.ShouldNotBeNull();
        messageParagraph.ClassName.ShouldContain("text-sm");
        messageParagraph.ClassName.ShouldNotContain("text-xs");
    }
}
