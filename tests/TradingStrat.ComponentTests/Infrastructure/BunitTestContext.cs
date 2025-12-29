using Bunit;
using Microsoft.Extensions.DependencyInjection;
using TradingStrat.ComponentTests.TestDoubles;
using TradingStrat.Web.Services;
using TradingStrat.Web.Services.State;

namespace TradingStrat.ComponentTests.Infrastructure;

/// <summary>
/// Base class for all BUnit component tests.
/// Provides dependency injection setup with fake services for isolated testing.
/// </summary>
public abstract class BunitTestContext : BunitContext
{
    /// <summary>
    /// Gets the fake LocalStorage service. Use this to verify localStorage interactions.
    /// </summary>
    protected FakeLocalStorageService FakeLocalStorage { get; }

    /// <summary>
    /// Gets the fake Notification service. Use this to verify notification interactions.
    /// </summary>
    protected FakeNotificationService FakeNotificationService { get; }

    /// <summary>
    /// Gets the fake Progress service. Use this to verify progress reporting.
    /// </summary>
    protected FakeProgressService FakeProgressService { get; }

    /// <summary>
    /// Gets the fake Portfolio state service. Use this to verify portfolio state interactions.
    /// </summary>
    protected FakePortfolioStateService FakePortfolioState { get; }

    /// <summary>
    /// Gets the fake Chat state service. Use this to verify chat state interactions.
    /// </summary>
    protected FakeChatStateService FakeChatState { get; }

    /// <summary>
    /// Gets the fake User Preferences service. Use this to verify user preferences interactions.
    /// </summary>
    protected FakeUserPreferencesService FakeUserPreferences { get; }

    protected BunitTestContext()
    {
        // Create fake services
        FakeLocalStorage = new FakeLocalStorageService();
        FakeNotificationService = new FakeNotificationService();
        FakeProgressService = new FakeProgressService();
        FakePortfolioState = new FakePortfolioStateService();
        FakeChatState = new FakeChatStateService();
        FakeUserPreferences = new FakeUserPreferencesService();

        // Register services in DI container
        Services.AddSingleton<LocalStorageService>(FakeLocalStorage);
        Services.AddSingleton<NotificationService>(FakeNotificationService);
        Services.AddSingleton<ProgressService>(FakeProgressService);
        Services.AddSingleton<PortfolioStateService>(FakePortfolioState);
        Services.AddSingleton<ChatStateService>(FakeChatState);
        Services.AddSingleton<UserPreferencesService>(FakeUserPreferences);

        // Setup JSInterop for Catalyst UI components (Dialog, etc.)
        JSInterop.Mode = JSRuntimeMode.Loose; // Allow calls without explicit setup
        JSInterop.SetupVoid("catalyst.initializeDialog", _ => true);
        JSInterop.SetupVoid("catalyst.focusDialog", _ => true);
        JSInterop.SetupVoid("catalyst.disposeDialog", _ => true);

        // Note: Components that require use case interfaces or application ports
        // should mock those dependencies in their specific test classes using FakeItEasy.
        // This base class only provides the common UI services.
        //
        // Each test gets a new instance of BunitTestContext, so services are
        // automatically isolated between tests.
    }
}
