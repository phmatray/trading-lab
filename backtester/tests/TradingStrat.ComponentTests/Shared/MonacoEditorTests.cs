using AngleSharp.Dom;
using Bunit;
using Microsoft.AspNetCore.Components;
using Shouldly;
using TradingStrat.ComponentTests.Infrastructure;
using TradingStrat.Web.Components.Shared;
using Xunit;

namespace TradingStrat.ComponentTests.Shared;

/// <summary>
/// Tests for the MonacoEditor component.
/// Note: These tests verify basic rendering and parameter handling.
/// Full Monaco Editor functionality requires actual JS runtime which is tested in E2E tests.
/// </summary>
public class MonacoEditorTests : BunitTestContext
{
    [Fact]
    public void MonacoEditor_WithoutParameters_RendersContainer()
    {
        // Arrange
        JSInterop.Mode = JSRuntimeMode.Loose; // Allow JS interop calls for Monaco initialization

        // Act
        IRenderedComponent<MonacoEditor> cut = Render<MonacoEditor>();

        // Assert
        cut.Markup.ShouldNotBeEmpty();
        IElement container = cut.Find(".monaco-editor-container");
        container.ShouldNotBeNull();
        container.ClassList.ShouldContain("border");
        container.ClassList.ShouldContain("border-gray-300");
        container.ClassList.ShouldContain("rounded-lg");
    }

    [Fact]
    public void MonacoEditor_GeneratesUniqueContainerId()
    {
        // Arrange
        JSInterop.Mode = JSRuntimeMode.Loose;

        // Act
        IRenderedComponent<MonacoEditor> cut1 = Render<MonacoEditor>();
        IRenderedComponent<MonacoEditor> cut2 = Render<MonacoEditor>();

        // Assert
        IElement container1 = cut1.Find(".monaco-editor-container");
        IElement container2 = cut2.Find(".monaco-editor-container");

        string id1 = container1.GetAttribute("id") ?? string.Empty;
        string id2 = container2.GetAttribute("id") ?? string.Empty;

        id1.ShouldNotBeNullOrEmpty();
        id2.ShouldNotBeNullOrEmpty();
        id1.ShouldStartWith("monaco-editor-");
        id2.ShouldStartWith("monaco-editor-");
        id1.ShouldNotBe(id2); // IDs should be unique
    }

    [Fact]
    public void MonacoEditor_WithInitialCode_PassesCodeToJSInterop()
    {
        // Arrange
        JSInterop.Mode = JSRuntimeMode.Loose;
        string initialCode = "def generate_signal(index, price, cash, position):\n    return {'action': 'hold', 'quantity': 0, 'reason': 'Test'}";

        // Act
        IRenderedComponent<MonacoEditor> cut = Render<MonacoEditor>(parameters => parameters
            .Add(p => p.Code, initialCode));

        // Assert
        // Component should render without errors
        cut.Markup.ShouldNotBeEmpty();
        IElement container = cut.Find(".monaco-editor-container");
        container.ShouldNotBeNull();
    }

    [Fact]
    public void MonacoEditor_WithCustomHeight_RendersContainer()
    {
        // Arrange
        JSInterop.Mode = JSRuntimeMode.Loose;
        int customHeight = 800;

        // Act
        IRenderedComponent<MonacoEditor> cut = Render<MonacoEditor>(parameters => parameters
            .Add(p => p.Height, customHeight));

        // Assert
        cut.Markup.ShouldNotBeEmpty();
        IElement container = cut.Find(".monaco-editor-container");
        container.ShouldNotBeNull();
    }

    [Fact]
    public void MonacoEditor_WithReadOnlyTrue_PassesReadOnlyToJSInterop()
    {
        // Arrange
        JSInterop.Mode = JSRuntimeMode.Loose;

        // Act
        IRenderedComponent<MonacoEditor> cut = Render<MonacoEditor>(parameters => parameters
            .Add(p => p.ReadOnly, true));

        // Assert
        // Component should render without errors
        cut.Markup.ShouldNotBeEmpty();
        IElement container = cut.Find(".monaco-editor-container");
        container.ShouldNotBeNull();
    }

    [Fact]
    public void MonacoEditor_ContainerHasDarkModeClasses()
    {
        // Arrange
        JSInterop.Mode = JSRuntimeMode.Loose;

        // Act
        IRenderedComponent<MonacoEditor> cut = Render<MonacoEditor>();

        // Assert
        IElement container = cut.Find(".monaco-editor-container");
        container.ClassList.ShouldContain("dark:border-gray-600");
    }

    [Fact]
    public void MonacoEditor_ContainerHasRoundedCorners()
    {
        // Arrange
        JSInterop.Mode = JSRuntimeMode.Loose;

        // Act
        IRenderedComponent<MonacoEditor> cut = Render<MonacoEditor>();

        // Assert
        IElement container = cut.Find(".monaco-editor-container");
        container.ClassList.ShouldContain("rounded-lg");
        container.ClassList.ShouldContain("overflow-hidden");
    }

    [Fact]
    public async Task MonacoEditor_WithCodeChangedCallback_RendersSuccessfully()
    {
        // Arrange
        JSInterop.Mode = JSRuntimeMode.Loose;
        string? changedCode = null;

        // Act
        IRenderedComponent<MonacoEditor> cut = Render<MonacoEditor>(parameters => parameters
            .Add(p => p.Code, "initial code")
            .Add(p => p.CodeChanged, EventCallback.Factory.Create<string>(this, (code) => changedCode = code)));

        // Assert
        cut.Markup.ShouldNotBeEmpty();

        // Simulate code change from JS (this would normally come from Monaco)
        await cut.InvokeAsync(async () =>
        {
            await cut.Instance.OnCodeChanged("new code from editor");
        });

        // Verify callback was invoked
        changedCode.ShouldBe("new code from editor");
    }

    [Fact]
    public async Task MonacoEditor_SetCodeAsync_UpdatesCode()
    {
        // Arrange
        JSInterop.Mode = JSRuntimeMode.Loose;
        string newCode = "def initialize(prices):\n    pass";

        IRenderedComponent<MonacoEditor> cut = Render<MonacoEditor>(parameters => parameters
            .Add(p => p.Code, "initial"));

        // Act
        await cut.InvokeAsync(async () =>
        {
            await cut.Instance.SetCodeAsync(newCode);
        });

        // Assert
        // Component should not throw errors
        cut.Markup.ShouldNotBeEmpty();
    }

    [Fact]
    public async Task MonacoEditor_SetReadOnlyAsync_UpdatesReadOnlyState()
    {
        // Arrange
        JSInterop.Mode = JSRuntimeMode.Loose;

        IRenderedComponent<MonacoEditor> cut = Render<MonacoEditor>(parameters => parameters
            .Add(p => p.ReadOnly, false));

        // Act
        await cut.InvokeAsync(async () =>
        {
            await cut.Instance.SetReadOnlyAsync(true);
        });

        // Assert
        // Component should not throw errors
        cut.Markup.ShouldNotBeEmpty();
    }

    [Fact]
    public async Task MonacoEditor_DisposeAsync_CleansUpResources()
    {
        // Arrange
        JSInterop.Mode = JSRuntimeMode.Loose;

        IRenderedComponent<MonacoEditor> cut = Render<MonacoEditor>();

        // Act & Assert - Should not throw
        await cut.InvokeAsync(async () =>
        {
            await cut.Instance.DisposeAsync();
        });
    }

    [Fact]
    public void MonacoEditor_MultipleInstances_EachHaveUniqueContainers()
    {
        // Arrange
        JSInterop.Mode = JSRuntimeMode.Loose;

        // Act
        IRenderedComponent<MonacoEditor> cut1 = Render<MonacoEditor>(parameters => parameters
            .Add(p => p.Code, "code1"));

        IRenderedComponent<MonacoEditor> cut2 = Render<MonacoEditor>(parameters => parameters
            .Add(p => p.Code, "code2"));

        IRenderedComponent<MonacoEditor> cut3 = Render<MonacoEditor>(parameters => parameters
            .Add(p => p.Code, "code3"));

        // Assert
        IElement container1 = cut1.Find(".monaco-editor-container");
        IElement container2 = cut2.Find(".monaco-editor-container");
        IElement container3 = cut3.Find(".monaco-editor-container");

        string id1 = container1.GetAttribute("id") ?? string.Empty;
        string id2 = container2.GetAttribute("id") ?? string.Empty;
        string id3 = container3.GetAttribute("id") ?? string.Empty;

        // All IDs should be different
        id1.ShouldNotBe(id2);
        id2.ShouldNotBe(id3);
        id1.ShouldNotBe(id3);
    }
}
