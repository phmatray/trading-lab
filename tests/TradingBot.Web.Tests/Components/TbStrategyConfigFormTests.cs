// <copyright file="TbStrategyConfigFormTests.cs" company="TradingBot">
// Copyright (c) TradingBot. All rights reserved.
// </copyright>

using Bunit;
using FakeItEasy;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using TradingBot.Web.Components.Features.Strategy;
using TradingBot.Web.Services;
using Xunit;

namespace TradingBot.Web.Tests.Components;

/// <summary>
/// Unit tests for TbStrategyConfigForm component.
/// </summary>
public class TbStrategyConfigFormTests
{
    /// <summary>
    /// Tests that form renders correctly.
    /// </summary>
    [Fact]
    public void Render_WithValidServices_DisplaysForm()
    {
        // Arrange
        using var ctx = new BunitContext();
        var fakeService = A.Fake<IStrategyManagementService>();
        ctx.Services.AddSingleton(fakeService);

        // Act
        var cut = ctx.Render<TbStrategyConfigForm>();

        // Assert
        cut.Find("form").ShouldNotBeNull();
    }
}
