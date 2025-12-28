using AngleSharp.Dom;
using Bunit;
using Microsoft.AspNetCore.Components;
using Shouldly;
using TradingStrat.ComponentTests.Infrastructure;
using TradingStrat.Web.Components.UI.Forms;
using Xunit;

namespace TradingStrat.ComponentTests.UI.Forms;

/// <summary>
/// Tests for the Fieldset component.
/// </summary>
public class FieldsetTests : BunitTestContext
{
    [Fact]
    public void Fieldset_RendersAsFieldsetElement()
    {
        // Arrange & Act
        IRenderedComponent<Fieldset> cut = Render<Fieldset>(parameters => parameters
            .Add(p => p.ChildContent, (RenderFragment)(builder => builder.AddContent(0, "Content"))));

        // Assert
        IElement fieldset = cut.Find("fieldset");
        fieldset.ShouldNotBeNull();
    }

    [Fact]
    public void Fieldset_WithDisabled_AppliesDisabledAttribute()
    {
        // Arrange & Act
        IRenderedComponent<Fieldset> cut = Render<Fieldset>(parameters => parameters
            .Add(p => p.Disabled, true)
            .Add(p => p.ChildContent, (RenderFragment)(builder => builder.AddContent(0, "Disabled"))));

        // Assert
        IElement fieldset = cut.Find("fieldset");
        fieldset.HasAttribute("disabled").ShouldBeTrue();
    }

    [Fact]
    public void Fieldset_AppliesVerticalSpacing()
    {
        // Arrange & Act
        IRenderedComponent<Fieldset> cut = Render<Fieldset>(parameters => parameters
            .Add(p => p.ChildContent, (RenderFragment)(builder => builder.AddContent(0, "Spacing"))));

        // Assert
        cut.Markup.ShouldContain("space-y-8");
    }

    [Fact]
    public void Fieldset_WithCustomClass_AppendsToExistingClasses()
    {
        // Arrange & Act
        IRenderedComponent<Fieldset> cut = Render<Fieldset>(parameters => parameters
            .Add(p => p.Class, "custom-fieldset")
            .Add(p => p.ChildContent, (RenderFragment)(builder => builder.AddContent(0, "Custom"))));

        // Assert
        IElement fieldset = cut.Find("fieldset");
        fieldset.ClassList.ShouldContain("custom-fieldset");
        fieldset.ClassList.ShouldContain("space-y-8");
    }

    [Fact]
    public void Fieldset_WithEmptyContent_RendersWithoutError()
    {
        // Arrange & Act
        IRenderedComponent<Fieldset> cut = Render<Fieldset>();

        // Assert
        cut.Markup.ShouldNotBeEmpty();
        cut.Find("fieldset").ShouldNotBeNull();
    }

    [Fact]
    public void Fieldset_DefaultsToNotDisabled()
    {
        // Arrange & Act
        IRenderedComponent<Fieldset> cut = Render<Fieldset>(parameters => parameters
            .Add(p => p.ChildContent, (RenderFragment)(builder => builder.AddContent(0, "Enabled"))));

        // Assert
        IElement fieldset = cut.Find("fieldset");
        fieldset.HasAttribute("disabled").ShouldBeFalse();
    }
}
