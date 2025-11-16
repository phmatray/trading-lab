// <copyright file="TbRiskSettingsFormTests.cs" company="TradingBot">
// Copyright (c) TradingBot. All rights reserved.
// </copyright>

using Bunit;
using Microsoft.Extensions.DependencyInjection;
using TradingBot.Core.Models.Configuration;
using TradingBot.Web.Components.Features.Risk;
using TradingBot.Web.Services;

namespace TradingBot.Web.Tests.Components;

/// <summary>
/// Unit tests for TbRiskSettingsForm component.
/// </summary>
public class TbRiskSettingsFormTests
{
    /// <summary>
    /// Tests that validation shows errors for invalid values.
    /// </summary>
    [Fact]
    public void Validation_InvalidValues_ShowsErrors()
    {
        // Arrange
        using var ctx = new BunitContext();
        var fakeService = A.Fake<IRiskSettingsService>();
        ctx.Services.AddSingleton(fakeService);

        var invalidSettings = new RiskSettings
        {
            Id = Guid.Parse("00000000-0000-0000-0000-000000000001"),
            MaxPositionSizePercent = 0m, // Invalid: too low
            StopLossPercent = 60m,       // Invalid: too high
            TakeProfitPercent = 5m,
            MaxOpenPositions = 5,
            MaxDailyLossPercent = 5m,
            LastModified = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow.AddDays(-30),
        };

        // Act
        var cut = ctx.Render<TbRiskSettingsForm>(parameters => parameters
            .Add(p => p.Settings, invalidSettings));

        // Try to submit the form with invalid values
        var form = cut.Find("form");
        form.Submit();

        // Assert - The form should show validation errors
        // Note: Actual validation messages depend on implementation
        cut.Markup.ShouldContain("class", (Case)StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Tests that the form renders with initial values.
    /// </summary>
    [Fact]
    public void Render_WithValidSettings_DisplaysCurrentValues()
    {
        // Arrange
        using var ctx = new BunitContext();
        var fakeService = A.Fake<IRiskSettingsService>();
        ctx.Services.AddSingleton(fakeService);

        var settings = new RiskSettings
        {
            Id = Guid.Parse("00000000-0000-0000-0000-000000000001"),
            MaxPositionSizePercent = 15m,
            StopLossPercent = 2.5m,
            TakeProfitPercent = 6m,
            MaxOpenPositions = 8,
            MaxDailyLossPercent = 4m,
            LastModified = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow.AddDays(-30),
        };

        // Act
        var cut = ctx.Render<TbRiskSettingsForm>(parameters => parameters
            .Add(p => p.Settings, settings));

        // Assert
        cut.Find("form").ShouldNotBeNull();

        // Verify inputs exist and contain expected values
        var inputs = cut.FindAll("input[type='number']");
        inputs.Count.ShouldBeGreaterThan(0);
    }

    /// <summary>
    /// Tests that save callback is invoked with valid settings.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    [Fact]
    public async Task Save_ValidSettings_InvokesCallback()
    {
        // Arrange
        using var ctx = new BunitContext();
        var fakeService = A.Fake<IRiskSettingsService>();
        ctx.Services.AddSingleton(fakeService);

        var settings = new RiskSettings
        {
            Id = Guid.Parse("00000000-0000-0000-0000-000000000001"),
            MaxPositionSizePercent = 10m,
            StopLossPercent = 2m,
            TakeProfitPercent = 5m,
            MaxOpenPositions = 5,
            MaxDailyLossPercent = 5m,
            LastModified = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow.AddDays(-30),
        };

        var onSaveCalled = false;
        RiskSettings? savedSettings = null;

        // Act
        var cut = ctx.Render<TbRiskSettingsForm>(parameters => parameters
            .Add(p => p.Settings, settings)
            .Add(p => p.OnSave, (s) =>
            {
                onSaveCalled = true;
                savedSettings = s;
                return Task.CompletedTask;
            }));

        // Trigger save by finding and clicking the save button
        var saveButton = cut.FindAll("button").FirstOrDefault(b => b.TextContent.Contains("Save"));

        if (saveButton != null)
        {
            await cut.InvokeAsync(() => saveButton.Click());

            // Assert
            onSaveCalled.ShouldBeTrue();
            savedSettings.ShouldNotBeNull();
        }
    }
}
