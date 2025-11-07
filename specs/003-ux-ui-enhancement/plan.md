# Implementation Plan: UX/UI Enhancement - Navigation & Settings

**Branch**: `003-ux-ui-enhancement` | **Date**: 2025-11-07 | **Spec**: [spec.md](./spec.md)
**Input**: Feature specification from `/specs/003-ux-ui-enhancement/spec.md`

**Note**: This template is filled in by the `/speckit.plan` command. See `.specify/templates/commands/plan.md` for the execution workflow.

## Summary

Enhance the existing Blazor Server trading dashboard with comprehensive UX/UI improvements including a persistent left sidebar navigation menu, user settings and preferences management (theme, refresh intervals, notifications), visual design polish, keyboard navigation and WCAG 2.1 Level AA accessibility compliance, responsive layouts with component consistency, and contextual help throughout the application. The enhancements will use DaisyUI components and Tailwind CSS for consistent styling while maintaining the existing Blazor Server architecture.

## Technical Context

**Language/Version**: C# / .NET 9 + ASP.NET Core Blazor Server
**Primary Dependencies**: DaisyUI, Tailwind CSS, existing TradingBot.Web project from spec 002
**Storage**: SQLite via Entity Framework Core 9 (extend schema for user preferences)
**Testing**: xUnit + bUnit for component tests, FakeItEasy for mocking, Shouldly for assertions
**Target Platform**: Web browsers (Chrome, Edge, Firefox, Safari - last 2 versions), desktop-only (minimum 1024px width)
**Project Type**: Web application enhancement (builds on existing TradingBot.Web)
**Performance Goals**: Navigation < 2 clicks to any section, settings save < 1s, visual feedback < 100ms, theme change instant, keyboard navigation fully functional
**Constraints**: Must maintain existing Blazor Server architecture, reuse existing Core/Infrastructure layers, WCAG 2.1 Level AA compliance required, no mobile optimization (1024px minimum), desktop-only keyboard shortcuts
**Scale/Scope**: 6 user stories (P1-P3 priority), ~40 functional requirements across 6 categories, left sidebar navigation with 6 main sections, user preferences entity with 5+ configurable settings

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

### Code Quality Gates
- ✅ **Layered Architecture**: UI enhancements remain in presentation layer (TradingBot.Web), user preferences added to Core domain
- ✅ **SOLID Principles**: Settings management follows SRP, DIP through service abstractions for preferences
- ✅ **Naming Conventions**: All C# code follows PascalCase for public, camelCase with underscore for private fields
- ✅ **File Headers**: All .cs files will include copyright headers per existing standard
- ✅ **SmartEnum Pattern**: Will use existing SmartEnum types, no new enums introduced for this feature

### Testing Gates
- ✅ **80% Code Coverage**: Component tests for all new UI components, service tests for preferences management
- ✅ **100% Critical Path Coverage**: Settings persistence, theme switching, navigation rendering, accessibility features
- ✅ **AAA Pattern**: All tests follow Arrange-Act-Assert structure
- ✅ **Test Naming**: `ComponentName_Scenario_ExpectedBehavior` format (e.g., `LeftSidebar_WhenCollapsed_ShowsIconsOnly`)
- ✅ **Fast Execution**: Component tests expected < 50ms each, full suite addition < 30 seconds

### Performance Gates
- ✅ **Response Time**: Settings save < 1s, navigation render < 100ms, theme change instant (per spec SC-003, SC-004, SC-005)
- ✅ **API p95 < 200ms**: User preferences CRUD operations meet constitution requirement
- ✅ **Caching**: Theme configuration cached in memory, preferences loaded once per session
- ✅ **Monitoring**: Structured logging for settings changes, navigation errors, accessibility issues

### Security Gates
- ✅ **Authentication**: Leverage existing ASP.NET Core Identity (no changes needed)
- ✅ **Authorization**: User preferences scoped to authenticated user, no cross-user access
- ✅ **Input Validation**: All settings inputs validated (refresh interval 1-300s, notification duration 2-10s)
- ✅ **XSS Prevention**: Blazor automatic escaping, parameterized queries for preferences storage
- ✅ **Audit Trail**: Settings changes logged with user ID and timestamp

### UX Consistency Gates
- ✅ **Responsive Design**: Desktop-only per spec (1024px minimum), follows DaisyUI and Tailwind CSS design system
- ✅ **Accessibility**: WCAG 2.1 Level AA compliance for keyboard navigation, screen readers, color contrast (per spec SC-007)
- ✅ **Error Handling**: User-friendly messages for settings validation errors, save failures, connection issues
- ✅ **Visual Feedback**: Hover states on all interactive elements < 100ms, loading indicators < 200ms (per spec SC-005, SC-009)
- ✅ **Color Coding**: Consistent use of green (success), red (error), yellow (warning), blue (info) per constitution

### Status: ✅ PASS
All gates align with constitution requirements. This feature enhances the existing TradingBot.Web project without violating architecture principles. No new violations requiring justification.

## Project Structure

### Documentation (this feature)

```text
specs/003-ux-ui-enhancement/
├── plan.md              # This file (/speckit.plan command output)
├── research.md          # Phase 0 output (/speckit.plan command)
├── data-model.md        # Phase 1 output (/speckit.plan command)
├── quickstart.md        # Phase 1 output (/speckit.plan command)
├── contracts/           # Phase 1 output (/speckit.plan command)
│   └── user-preferences-api.yaml  # User preferences CRUD endpoints
└── tasks.md             # Phase 2 output (/speckit.tasks command - NOT created by /speckit.plan)
```

### Source Code (repository root)

**Existing Structure** (from spec 002, will be enhanced):
```text
src/
├── TradingBot.Core/           # ADD: UserPreferences entity, IUserPreferencesRepository
├── TradingBot.Infrastructure/ # ADD: UserPreferencesRepository, UserPreferences EF configuration
├── TradingBot.Web/            # ENHANCE: Navigation, settings components, theme system
│   ├── Components/
│   │   ├── Layout/
│   │   │   ├── MainLayout.razor          # MODIFY: Add left sidebar integration
│   │   │   ├── LeftSidebar.razor         # NEW: Persistent left sidebar navigation
│   │   │   └── TopHeader.razor           # NEW: Top header with settings access
│   │   ├── Settings/                     # NEW: Settings components
│   │   │   ├── SettingsPage.razor        # NEW: Main settings page
│   │   │   ├── DisplaySettings.razor     # NEW: Theme, refresh interval settings
│   │   │   ├── NotificationSettings.razor # NEW: Toast notification preferences
│   │   │   └── PreferencesService.cs     # NEW: Settings management service
│   │   ├── Shared/                       # ENHANCE: Shared UI components
│   │   │   ├── Toast.razor               # NEW: Toast notification component
│   │   │   ├── Modal.razor               # NEW: Accessible modal dialog
│   │   │   ├── Tooltip.razor             # NEW: Contextual help tooltips
│   │   │   ├── LoadingIndicator.razor    # NEW: Loading state component
│   │   │   └── HelpIcon.razor            # NEW: Help icon with tooltip
│   │   └── [Other existing components]   # ENHANCE: Add keyboard nav, ARIA labels
│   ├── Pages/                             # ENHANCE: All existing pages
│   │   └── Settings.razor                 # NEW: Settings page route
│   ├── Services/
│   │   ├── ThemeService.cs                # NEW: Theme management and persistence
│   │   ├── NotificationService.cs         # NEW: Toast notification management
│   │   └── KeyboardShortcutService.cs     # NEW: Keyboard shortcut handling
│   ├── wwwroot/
│   │   ├── css/
│   │   │   ├── app.css                    # ENHANCE: Add sidebar, theme variables
│   │   │   └── themes/                    # NEW: Light and dark theme CSS
│   │   │       ├── light.css
│   │   │       └── dark.css
│   │   └── js/
│   │       ├── theme-switcher.js          # NEW: Client-side theme switching
│   │       └── keyboard-shortcuts.js      # NEW: Keyboard shortcut handlers
│   ├── Migrations/                         # ADD: New migration for UserPreferences table
│   ├── appsettings.json                    # ADD: Default user preferences config
│   └── Program.cs                          # MODIFY: Register new services

tests/
└── TradingBot.Web.Tests/
    ├── Components/
    │   ├── Layout/
    │   │   └── LeftSidebarTests.cs         # NEW: Sidebar component tests
    │   ├── Settings/
    │   │   ├── SettingsPageTests.cs        # NEW: Settings page tests
    │   │   └── DisplaySettingsTests.cs     # NEW: Display settings tests
    │   └── Shared/
    │       ├── ToastTests.cs               # NEW: Toast component tests
    │       └── ModalTests.cs               # NEW: Modal accessibility tests
    ├── Services/
    │   ├── ThemeServiceTests.cs            # NEW: Theme service tests
    │   ├── NotificationServiceTests.cs     # NEW: Notification service tests
    │   └── PreferencesServiceTests.cs      # NEW: Preferences service tests
    └── Accessibility/
        └── KeyboardNavigationTests.cs      # NEW: Keyboard nav integration tests
```

**DaisyUI and Tailwind CSS Integration**:
```text
src/TradingBot.Web/
├── Styles/
│   ├── app.css                            # ENHANCE: Add DaisyUI imports, custom utilities
│   ├── components/                        # NEW: Component-specific styles
│   │   ├── sidebar.css                    # Sidebar animations and states
│   │   ├── toast.css                      # Toast notification styles
│   │   └── themes.css                     # Theme-specific overrides
│   └── utilities/                         # NEW: Custom Tailwind utilities
│       └── accessibility.css              # Focus indicators, screen reader utilities
├── tailwind.config.js                     # MODIFY: Add DaisyUI plugin, theme configuration
├── package.json                           # MODIFY: Add DaisyUI dependency
└── package-lock.json                      # UPDATE: Lock DaisyUI version
```

**Database Schema Updates**:
```text
Database: tradingbot.db (SQLite)

NEW TABLE: UserPreferences
- Id (PK)
- UserId (FK to AspNetUsers)
- Theme (string: "light" or "dark")
- DashboardRefreshInterval (int: 1-300 seconds)
- NotificationTypesEnabled (JSON: {success: bool, error: bool, info: bool, warning: bool})
- NotificationDuration (int: 2-10 seconds)
- SidebarCollapsed (bool: always false on load, transient state)
- CreatedAt (datetime)
- UpdatedAt (datetime)
```

**Structure Decision**:
- This is a **web application enhancement** building on the existing TradingBot.Web project from spec 002
- User preferences are added as a new domain entity in TradingBot.Core with repository in TradingBot.Infrastructure
- Navigation and settings components are added to the existing Blazor component hierarchy
- Theme management is implemented as a new service layer in TradingBot.Web
- DaisyUI is integrated as a plugin to the existing Tailwind CSS setup
- All new components follow the established Blazor Server patterns (dependency injection, component lifecycle)
- Accessibility features are integrated throughout existing components via ARIA labels and keyboard handlers
- Settings persistence uses the existing EF Core DbContext with a new UserPreferences table

## Complexity Tracking

> **Fill ONLY if Constitution Check has violations that must be justified**

No violations detected. This feature enhances the existing TradingBot.Web project without introducing architectural violations.
