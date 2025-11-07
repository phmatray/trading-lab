# Research: UX/UI Enhancement - Navigation & Settings

**Feature**: 003-ux-ui-enhancement | **Date**: 2025-11-07

## Overview

This document captures research findings for implementing comprehensive UX/UI enhancements to the TradingBot Blazor Server dashboard, including navigation, settings management, visual polish, accessibility, and contextual help.

## Technology Research

### 1. DaisyUI Integration with Blazor Server

**Decision**: Use DaisyUI 4.x as a Tailwind CSS plugin for component styling

**Rationale**:
- DaisyUI provides semantic, accessible component classes that align with WCAG 2.1 Level AA requirements
- Integrates seamlessly with existing Tailwind CSS setup (no additional build complexity)
- Includes built-in theme support (light/dark modes) that can be controlled programmatically
- Component classes (btn, card, modal, toast, drawer) reduce custom CSS needed
- Active development and strong community support

**Alternatives Considered**:
- **MudBlazor**: Full Blazor component library but heavier, more opinionated, would require significant refactoring of existing components
- **Ant Design Blazor**: Comprehensive but introduces additional JS dependencies and complex state management
- **Custom CSS only**: More flexible but requires extensive custom component development and accessibility implementation

**Implementation Notes**:
- Install via npm: `npm install daisyui@latest`
- Configure in `tailwind.config.js`: `plugins: [require("daisyui")]`
- DaisyUI themes controlled via `data-theme` attribute on `<html>` element
- Can extend/override DaisyUI theme colors in Tailwind config

### 2. Left Sidebar Navigation Pattern

**Decision**: Implement collapsible left sidebar with persistent state within session (always starts expanded)

**Rationale**:
- Left sidebar is standard pattern for dashboard applications (familiar UX)
- Provides persistent visibility without consuming header space
- DaisyUI's `drawer` component provides accessible, responsive sidebar functionality
- Collapsed state (icon-only) maximizes content area when needed
- Per clarifications, sidebar always starts expanded (no cross-session persistence)

**Alternatives Considered**:
- **Top horizontal navbar**: Limited space for 6+ navigation items, doesn't scale well
- **Bottom app bar**: Non-standard for desktop applications, better for mobile
- **Persistent state across sessions**: Rejected per user clarification (always start expanded)

**Implementation Notes**:
- Use DaisyUI drawer component with `drawer-open` class for initial state
- Toggle handled by Blazor component state (not persisted to UserPreferences)
- Collapse to icon-only mode using CSS transitions
- Keyboard shortcut (e.g., Alt+S) to toggle sidebar

### 3. Theme Management (Light/Dark Mode)

**Decision**: Use DaisyUI theme system with Blazor service for switching and persistence

**Rationale**:
- DaisyUI includes built-in light and dark themes with proper contrast ratios (WCAG AA compliant)
- Theme switching via `data-theme` attribute change (instant, no page reload)
- Can be controlled programmatically from C# via JSInterop
- Theme preference persisted in UserPreferences entity (database)
- CSS custom properties enable smooth transitions between themes

**Alternatives Considered**:
- **Custom CSS variables**: More control but requires manual accessibility testing
- **CSS class toggling (dark-mode class)**: Less maintainable than DaisyUI's semantic approach
- **LocalStorage only**: Would not sync across devices/browsers for same user

**Implementation Notes**:
- Create `ThemeService` in Blazor with JSInterop for `document.documentElement.setAttribute('data-theme', theme)`
- Load theme preference on app startup from UserPreferences
- Apply theme before Blazor components render to prevent flash of wrong theme
- Support system preference detection as fallback: `window.matchMedia('(prefers-color-scheme: dark)')`

### 4. Toast Notification System

**Decision**: Implement toast notification service using DaisyUI `alert` component with position management

**Rationale**:
- DaisyUI alert component provides semantic, accessible notification UI
- Notifications need to be non-blocking, dismissible, and timed (2-10 seconds)
- Service-based approach allows any component to trigger notifications
- Position (top-right typically) managed via absolute positioning
- Multiple notifications stack vertically

**Alternatives Considered**:
- **Blazored.Toast**: Third-party library but adds dependency, less customizable
- **SignalR-based notifications**: Overkill for client-only notifications (no server events)
- **Browser notifications API**: Requires permissions, better for background notifications

**Implementation Notes**:
- Create `NotificationService` with observable collection of active notifications
- Each notification has: message, type (success/error/info/warning), duration (2-10s), dismissible flag
- Auto-remove after duration using `Task.Delay` and `StateHasChanged()`
- Render notifications in `MainLayout.razor` via `<NotificationContainer>` component
- CSS transitions for enter/exit animations

### 5. User Preferences Storage

**Decision**: Store preferences in SQLite database via new UserPreferences entity and EF Core repository

**Rationale**:
- Aligns with existing architecture (SQLite + EF Core pattern established)
- Preferences tied to user account (multi-device sync when database is shared)
- Structured storage with validation at database level
- Repository pattern maintains consistency with existing data access
- No additional infrastructure required

**Alternatives Considered**:
- **Browser LocalStorage**: Client-only, doesn't sync across devices, less secure
- **Cookies**: Size limitations, sent with every request (performance overhead)
- **Separate JSON file per user**: File I/O complexity, no transactional support

**Implementation Notes**:
- Add `UserPreferences` entity to TradingBot.Core
- Add `IUserPreferencesRepository` interface to Core, implementation to Infrastructure
- EF Core configuration with unique constraint on UserId
- JSON column for `NotificationTypesEnabled` (EF Core 9 supports JSON mapping)
- Create migration: `dotnet ef migrations add AddUserPreferences`

### 6. Keyboard Navigation & Accessibility

**Decision**: Implement keyboard shortcuts using JSInterop with focus management and ARIA labels throughout

**Rationale**:
- WCAG 2.1 Level AA requires full keyboard navigation support
- Keyboard shortcuts improve power user efficiency (spec requirement)
- Browser-native keyboard events via JSInterop provide reliable handling
- ARIA labels and roles enable screen reader compatibility
- DaisyUI components include basic accessibility features (extend as needed)

**Alternatives Considered**:
- **Pure C# event handling**: Blazor doesn't natively support global keyboard shortcuts well
- **Third-party library (e.g., Blazor.Keybindings)**: Adds dependency, may conflict with browser shortcuts
- **No keyboard shortcuts**: Fails spec requirement for power user efficiency

**Implementation Notes**:
- Create `KeyboardShortcutService` with JSInterop for global keydown listener
- Register shortcuts: Alt+D (Dashboard), Alt+P (Portfolio), Alt+F (Performance), Alt+S (Sidebar toggle), Alt+T (Settings)
- Prevent conflicts with browser shortcuts (avoid Ctrl+, Cmd+)
- Implement focus trap for modals (Tab cycles within modal, Escape closes)
- Add `aria-label`, `aria-describedby`, `role` attributes to all interactive elements
- Ensure tab order is logical (top-to-bottom, left-to-right)
- Visible focus indicators with sufficient contrast (`:focus-visible` CSS)

### 7. Responsive Layout (Desktop-Only, 1024px Minimum)

**Decision**: Use Tailwind CSS responsive utilities with 1024px breakpoint, test down to 1024px width only

**Rationale**:
- Spec explicitly states desktop-only, no mobile optimization needed
- Tailwind's `lg:` breakpoint (1024px) aligns with minimum width requirement
- DaisyUI components are responsive by default (no extra configuration)
- Sidebar collapses gracefully at smaller desktop sizes
- Content area uses max-width constraints to prevent over-stretching on ultra-wide screens

**Alternatives Considered**:
- **Mobile-first responsive**: Out of scope per spec
- **Fixed width layout**: Poor UX on varying desktop sizes
- **CSS Grid only**: Tailwind provides more flexibility with utility classes

**Implementation Notes**:
- Primary breakpoint: `lg:` (1024px+)
- Sidebar: Full width on < 1024px (off-canvas drawer), persistent on >= 1024px
- Content area: `max-w-7xl` (1280px) to prevent over-stretching
- Test matrix: 1024px, 1280px, 1920px, 2560px widths

### 8. Help & Contextual Guidance

**Decision**: Implement tooltip component with DaisyUI tooltip utilities and info icons throughout interface

**Rationale**:
- DaisyUI includes `tooltip` directive for accessible hover/focus tooltips
- Info icons (question mark in circle) are recognizable pattern for help
- Tooltips provide contextual help without cluttering interface
- ARIA labels make tooltips accessible to screen readers
- Can be added incrementally to complex metrics and settings

**Alternatives Considered**:
- **Separate help page only**: Less discoverable, requires navigation away
- **Inline help text**: Takes up space, creates visual clutter
- **Video tutorials**: Out of scope per spec (advanced help features excluded)

**Implementation Notes**:
- Create `HelpIcon` component with `<Tooltip>` wrapper
- Tooltip content loaded from resource files for easy maintenance/localization
- Position tooltips dynamically (top/bottom/left/right) based on available space
- Keyboard accessible: show on focus, hide on Escape
- Add help icons to: complex metrics (Sharpe ratio, Sortino, etc.), risk settings, empty states

## Best Practices

### Blazor Component Structure
- Keep components small and focused (< 200 lines)
- Use code-behind files for complex logic
- Leverage dependency injection for services
- Implement `IDisposable` for event subscriptions
- Use `StateHasChanged()` judiciously (performance)

### Tailwind CSS with DaisyUI
- Use semantic color names from DaisyUI themes (primary, secondary, accent)
- Extend theme in `tailwind.config.js` for custom colors (trading-specific)
- Group related utilities with `@apply` in component CSS for reusability
- Use `@layer components` for custom component classes
- Purge unused CSS in production build

### Accessibility Implementation
- Test with screen reader (NVDA on Windows, VoiceOver on macOS)
- Use automated testing tools (axe DevTools, Lighthouse)
- Ensure color contrast ratios (4.5:1 for normal text, 3:1 for large text)
- Provide keyboard alternatives for all mouse interactions
- Skip links for keyboard users to jump to main content

### Performance Optimization
- Lazy load theme CSS (only active theme)
- Debounce settings save operations (avoid excessive database writes)
- Use `@key` directive in loops for efficient re-rendering
- Minimize JSInterop calls (batch when possible)
- Cache UserPreferences in memory (load once per session)

## Integration Patterns

### Theme Service Integration
```csharp
// Startup in Program.cs
builder.Services.AddScoped<IThemeService, ThemeService>();

// Load theme on app initialization
protected override async Task OnInitializedAsync()
{
    await ThemeService.LoadUserThemeAsync();
}
```

### Notification Service Pattern
```csharp
// Usage in any component
await NotificationService.ShowSuccessAsync("Settings saved successfully");
await NotificationService.ShowErrorAsync("Failed to save settings", dismissible: true);
```

### Keyboard Shortcut Registration
```csharp
// Register shortcuts in MainLayout
protected override async Task OnAfterRenderAsync(bool firstRender)
{
    if (firstRender)
    {
        await KeyboardShortcutService.RegisterAsync("Alt+D", NavigateToDashboard);
        await KeyboardShortcutService.RegisterAsync("Alt+P", NavigateToPortfolio);
    }
}
```

## Migration Strategy

### Phase 1: Foundation
1. Install DaisyUI and configure Tailwind
2. Create UserPreferences entity and migration
3. Implement ThemeService and NotificationService
4. Build basic LeftSidebar component

### Phase 2: Settings Management
1. Build Settings page with DisplaySettings component
2. Implement NotificationSettings component
3. Add PreferencesService for CRUD operations
4. Wire up save/load functionality

### Phase 3: Visual Polish
1. Apply DaisyUI component classes to existing components
2. Add hover states and transitions
3. Implement loading indicators
4. Add success/error notifications to existing operations

### Phase 4: Accessibility
1. Add ARIA labels and roles throughout
2. Implement KeyboardShortcutService
3. Add focus management for modals
4. Test with screen readers and fix issues

### Phase 5: Contextual Help
1. Create Tooltip and HelpIcon components
2. Add help icons to complex metrics
3. Document common tasks in help section
4. Add empty state guidance

## Testing Strategy

### Component Tests (bUnit)
- LeftSidebar: render, collapse/expand, navigation clicks
- SettingsPage: render, input validation, save/cancel
- Toast: render, auto-dismiss, manual dismiss
- Modal: render, focus trap, keyboard close

### Service Tests (xUnit)
- ThemeService: load, save, JSInterop calls
- NotificationService: add, remove, timeout
- PreferencesService: CRUD operations, validation

### Integration Tests
- Keyboard navigation: tab order, shortcuts
- Theme switching: persistence, visual verification
- Settings persistence: save → reload → verify

### Accessibility Tests
- Automated: axe-core integration tests
- Manual: screen reader walkthroughs, keyboard-only navigation

## References

- [DaisyUI Documentation](https://daisyui.com/)
- [Tailwind CSS Documentation](https://tailwindcss.com/)
- [WCAG 2.1 Guidelines](https://www.w3.org/WAI/WCAG21/quickref/)
- [Blazor Server Documentation](https://learn.microsoft.com/en-us/aspnet/core/blazor/)
- [bUnit Documentation](https://bunit.dev/)
- [EF Core JSON Columns](https://learn.microsoft.com/en-us/ef/core/modeling/json-columns)
