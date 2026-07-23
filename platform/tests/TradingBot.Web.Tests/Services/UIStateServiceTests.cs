// <copyright file="UIStateServiceTests.cs" company="TradingBot">
// Copyright (c) TradingBot. All rights reserved.
// </copyright>

using TradingBot.Web.Services;

namespace TradingBot.Web.Tests.Services;

/// <summary>
/// Unit tests for UIStateService.
/// </summary>
public class UIStateServiceTests
{
    [Fact]
    public void SidebarCollapsed_InitialState_ShouldBeFalse()
    {
        // Arrange
        var service = new UIStateService();

        // Act & Assert
        service.SidebarCollapsed.ShouldBeFalse();
    }

    [Fact]
    public void SidebarCollapsed_SetToTrue_ShouldUpdateState()
    {
        // Arrange
        var service = new UIStateService();

        // Act
        service.SidebarCollapsed = true;

        // Assert
        service.SidebarCollapsed.ShouldBeTrue();
    }

    [Fact]
    public void SidebarCollapsed_SetToFalse_ShouldUpdateState()
    {
        // Arrange
        var service = new UIStateService();
        service.SidebarCollapsed = true;

        // Act
        service.SidebarCollapsed = false;

        // Assert
        service.SidebarCollapsed.ShouldBeFalse();
    }

    [Fact]
    public void SidebarCollapsed_SetToSameValue_ShouldNotTriggerEvent()
    {
        // Arrange
        var service = new UIStateService();
        var eventRaised = false;
        service.OnStateChanged += () => eventRaised = true;

        // Act - Set to initial value (false)
        service.SidebarCollapsed = false;

        // Assert
        eventRaised.ShouldBeFalse();
    }

    [Fact]
    public void SidebarCollapsed_SetToDifferentValue_ShouldTriggerEvent()
    {
        // Arrange
        var service = new UIStateService();
        var eventRaised = false;
        service.OnStateChanged += () => eventRaised = true;

        // Act
        service.SidebarCollapsed = true;

        // Assert
        eventRaised.ShouldBeTrue();
    }

    [Fact]
    public void ToggleSidebar_WhenFalse_ShouldBecomeTrue()
    {
        // Arrange
        var service = new UIStateService();

        // Act
        service.ToggleSidebar();

        // Assert
        service.SidebarCollapsed.ShouldBeTrue();
    }

    [Fact]
    public void ToggleSidebar_WhenTrue_ShouldBecomeFalse()
    {
        // Arrange
        var service = new UIStateService();
        service.SidebarCollapsed = true;

        // Act
        service.ToggleSidebar();

        // Assert
        service.SidebarCollapsed.ShouldBeFalse();
    }

    [Fact]
    public void ToggleSidebar_ShouldTriggerStateChangedEvent()
    {
        // Arrange
        var service = new UIStateService();
        var eventRaiseCount = 0;
        service.OnStateChanged += () => eventRaiseCount++;

        // Act
        service.ToggleSidebar();

        // Assert
        eventRaiseCount.ShouldBe(1);
    }

    [Fact]
    public void ToggleSidebar_MultipleTimes_ShouldAlternateState()
    {
        // Arrange
        var service = new UIStateService();

        // Act & Assert
        service.SidebarCollapsed.ShouldBeFalse(); // Initial state

        service.ToggleSidebar();
        service.SidebarCollapsed.ShouldBeTrue();

        service.ToggleSidebar();
        service.SidebarCollapsed.ShouldBeFalse();

        service.ToggleSidebar();
        service.SidebarCollapsed.ShouldBeTrue();
    }

    [Fact]
    public void OnStateChanged_MultipleSubscribers_ShouldNotifyAll()
    {
        // Arrange
        var service = new UIStateService();
        var subscriber1Called = false;
        var subscriber2Called = false;
        service.OnStateChanged += () => subscriber1Called = true;
        service.OnStateChanged += () => subscriber2Called = true;

        // Act
        service.SidebarCollapsed = true;

        // Assert
        subscriber1Called.ShouldBeTrue();
        subscriber2Called.ShouldBeTrue();
    }
}
