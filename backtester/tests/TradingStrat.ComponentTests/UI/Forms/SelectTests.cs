using AngleSharp.Dom;
using Bunit;
using Microsoft.AspNetCore.Components;
using Shouldly;
using TradingStrat.ComponentTests.Infrastructure;
using TradingStrat.Web.Components.UI.Forms;
using Xunit;

namespace TradingStrat.ComponentTests.UI.Forms;

/// <summary>
/// Tests for the Select component.
/// </summary>
public class SelectTests : BunitTestContext
{
    [Fact]
    public void Select_RendersAsSelectElement()
    {
        // Arrange & Act
        IRenderedComponent<Select> cut = Render<Select>(parameters => parameters
            .Add(p => p.ChildContent, (RenderFragment)(builder =>
            {
                builder.OpenElement(0, "option");
                builder.AddContent(1, "Option 1");
                builder.CloseElement();
            })));

        // Assert
        IElement select = cut.Find("select");
        select.ShouldNotBeNull();
    }

    [Fact]
    public void Select_WithMultipleTrue_AppliesMultipleAttribute()
    {
        // Arrange & Act
        IRenderedComponent<Select> cut = Render<Select>(parameters => parameters
            .Add(p => p.Multiple, true)
            .Add(p => p.ChildContent, (RenderFragment)(builder =>
            {
                builder.OpenElement(0, "option");
                builder.AddContent(1, "Option");
                builder.CloseElement();
            })));

        // Assert
        IElement select = cut.Find("select");
        select.GetAttribute("multiple").ShouldNotBeNullOrWhiteSpace();
    }

    [Fact]
    public void Select_WithMultipleFalse_IncludesDropdownArrow()
    {
        // Arrange & Act
        IRenderedComponent<Select> cut = Render<Select>(parameters => parameters
            .Add(p => p.Multiple, false)
            .Add(p => p.ChildContent, (RenderFragment)(builder =>
            {
                builder.OpenElement(0, "option");
                builder.AddContent(1, "Single");
                builder.CloseElement();
            })));

        // Assert
        // Single select should have background image (dropdown arrow)
        cut.Markup.ShouldContain("bg-[url");
        cut.Markup.ShouldContain("pr-8");
    }

    [Fact]
    public void Select_WithDisabled_AppliesDisabledAttribute()
    {
        // Arrange & Act
        IRenderedComponent<Select> cut = Render<Select>(parameters => parameters
            .Add(p => p.Disabled, true)
            .Add(p => p.ChildContent, (RenderFragment)(builder =>
            {
                builder.OpenElement(0, "option");
                builder.AddContent(1, "Disabled");
                builder.CloseElement();
            })));

        // Assert
        IElement select = cut.Find("select");
        select.HasAttribute("disabled").ShouldBeTrue();
    }

    [Fact]
    public void Select_WithInvalidTrue_AppliesInvalidClass()
    {
        // Arrange & Act
        IRenderedComponent<Select> cut = Render<Select>(parameters => parameters
            .Add(p => p.Invalid, true)
            .Add(p => p.ChildContent, (RenderFragment)(builder =>
            {
                builder.OpenElement(0, "option");
                builder.AddContent(1, "Invalid");
                builder.CloseElement();
            })));

        // Assert
        cut.Markup.ShouldContain("border-red-500");
        cut.Markup.ShouldContain("focus:ring-red-500");
    }

    [Fact]
    public void Select_AppliesBaseClasses()
    {
        // Arrange & Act
        IRenderedComponent<Select> cut = Render<Select>(parameters => parameters
            .Add(p => p.ChildContent, (RenderFragment)(builder =>
            {
                builder.OpenElement(0, "option");
                builder.AddContent(1, "Base");
                builder.CloseElement();
            })));

        // Assert
        cut.Markup.ShouldContain("block");
        cut.Markup.ShouldContain("w-full");
        cut.Markup.ShouldContain("appearance-none");
        cut.Markup.ShouldContain("rounded-lg");
        cut.Markup.ShouldContain("border");
    }

    [Fact]
    public void Select_AppliesDarkModeClasses()
    {
        // Arrange & Act
        IRenderedComponent<Select> cut = Render<Select>(parameters => parameters
            .Add(p => p.ChildContent, (RenderFragment)(builder =>
            {
                builder.OpenElement(0, "option");
                builder.AddContent(1, "Dark");
                builder.CloseElement();
            })));

        // Assert
        cut.Markup.ShouldContain("dark:border-white/10");
        cut.Markup.ShouldContain("dark:text-white");
    }

    [Fact]
    public void Select_AppliesFocusClasses()
    {
        // Arrange & Act
        IRenderedComponent<Select> cut = Render<Select>(parameters => parameters
            .Add(p => p.ChildContent, (RenderFragment)(builder =>
            {
                builder.OpenElement(0, "option");
                builder.AddContent(1, "Focus");
                builder.CloseElement();
            })));

        // Assert
        cut.Markup.ShouldContain("focus:outline");
        cut.Markup.ShouldContain("focus:outline-2");
        cut.Markup.ShouldContain("focus:-outline-offset-1");
        cut.Markup.ShouldContain("focus:outline-blue-500");
    }

    [Fact]
    public void Select_WithCustomClass_AppendsToExistingClasses()
    {
        // Arrange & Act
        IRenderedComponent<Select> cut = Render<Select>(parameters => parameters
            .Add(p => p.Class, "custom-select")
            .Add(p => p.ChildContent, (RenderFragment)(builder =>
            {
                builder.OpenElement(0, "option");
                builder.AddContent(1, "Custom");
                builder.CloseElement();
            })));

        // Assert
        IElement select = cut.Find("select");
        select.ClassList.ShouldContain("custom-select");
        select.ClassList.ShouldContain("block");
    }

    [Fact]
    public void Select_OnChange_TriggersCallback()
    {
        // Arrange
        bool changed = false;
        EventCallback<ChangeEventArgs> callback = EventCallback.Factory.Create(
            this,
            (ChangeEventArgs _) => changed = true);

        IRenderedComponent<Select> cut = Render<Select>(parameters => parameters
            .Add(p => p.OnChange, callback)
            .Add(p => p.ChildContent, (RenderFragment)(builder =>
            {
                builder.OpenElement(0, "option");
                builder.AddAttribute(1, "value", "1");
                builder.AddContent(2, "Option 1");
                builder.CloseElement();
                builder.OpenElement(3, "option");
                builder.AddAttribute(4, "value", "2");
                builder.AddContent(5, "Option 2");
                builder.CloseElement();
            })));

        // Act
        IElement select = cut.Find("select");
        select.Change("2");

        // Assert
        changed.ShouldBeTrue();
    }
}
