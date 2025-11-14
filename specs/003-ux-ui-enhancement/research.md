# Research: UX/UI Enhancement - Navigation & Settings

**Feature**: 003-ux-ui-enhancement
**Date**: 2025-11-07
**Status**: Complete

## Executive Summary

This research document addresses technical decisions for implementing a comprehensive UX/UI enhancement to the TradingBot Blazor web application. The feature adds a persistent left sidebar navigation, user settings management, and polished UI components using Tailwind CSS with custom reusable components.

**Key Decisions**:
- Custom Tailwind-based component library (no third-party framework)
- Heroicons for iconography
- Atomic design pattern for component architecture
- Browser localStorage for client-side state with database persistence
- No authentication changes (single user system)

---

## 1. Component Architecture Strategy

### Decision: Atomic Design Pattern with Tailwind CSS

**Rationale**:
- **Atomic Design**: Provides clear component hierarchy (atoms → molecules → organisms → templates → pages)
- **Tailwind CSS**: Offers utility-first approach for rapid, consistent styling without CSS bloat
- **No Component Library**: Eliminates dependency on DaisyUI or other frameworks, giving complete control
- **Reusability**: Atomic components promote DRY principle and consistency

**Implementation Approach**:
```
Components/
├── Atoms/              # Basic building blocks
│   ├── Button.razor
│   ├── Input.razor
│   ├── Icon.razor
│   ├── Badge.razor
│   └── Spinner.razor
├── Molecules/          # Simple component combinations
│   ├── FormField.razor
│   ├── MenuItem.razor
│   ├── Toast.razor
│   └── Tooltip.razor
├── Organisms/          # Complex components
│   ├── NavigationSidebar.razor
│   ├── SettingsForm.razor
│   ├── NotificationCenter.razor
│   └── ThemeProvider.razor
└── Shared/             # Legacy/existing shared components
```

**Alternatives Considered**:
1. **DaisyUI Components**: Rejected - adds abstraction layer, less control, user specifically requested removal
2. **Bootstrap Components**: Rejected - already removed from project, heavyweight
3. **Flat Component Structure**: Rejected - harder to maintain, less organized
4. **MudBlazor/Radzen**: Rejected - adds large dependency, opinionated styling

**Benefits**:
- Complete styling control with Tailwind utilities
- Smaller bundle size (no component library overhead)
- Clear component organization and discoverability
- Easy to extend and customize
- Better alignment with modern frontend practices

---

## 2. Icon System

### Decision: Heroicons via SVG Integration

**Rationale**:
- **Heroicons**: Open-source, MIT licensed, designed by Tailwind CSS creators
- **SVG Format**: Scalable, customizable, performant
- **Two Variants**: Outline (24x24) for main UI, Solid (20x20) for smaller elements
- **Tailwind Integration**: Built to work seamlessly with Tailwind classes

**Implementation Strategy**:
1. Create `Icon.razor` atom component accepting name and variant parameters
2. Store SVG paths in a static dictionary or embedded resources
3. Support Tailwind classes for color, size customization
4. Provide intellisense-friendly enum for icon names

```razor
@* Usage Example *@
<Icon Name="IconName.Home" Variant="IconVariant.Outline" Class="w-6 h-6 text-blue-500" />
```

**Icon Sets Needed**:
- Navigation: Home, ChartBar, Briefcase, Cog, BeakerIcon, ChartLine
- Actions: Plus, Minus, XMark, Check, ArrowPath
- Status: ExclamationTriangle, InformationCircle, CheckCircle, XCircle
- UI: ChevronLeft, ChevronRight, ChevronDown, Bars3, Bell

**Alternatives Considered**:
1. **Font Awesome**: Rejected - requires CDN or package, heavier weight
2. **Lucide Icons**: Considered - similar to Heroicons but less Tailwind integration
3. **Tabler Icons**: Considered - good but Heroicons better Tailwind fit
4. **Material Icons**: Rejected - Google dependency, different design language

**Benefits**:
- Zero external dependencies for icons
- Perfect visual alignment with Tailwind design system
- Customizable via Tailwind classes
- Optimized SVG performance in Blazor

---

## 3. State Management Strategy

### Decision: Hybrid Client/Server State with Cascading Parameters

**Rationale**:
- **Client State**: UI-only state (sidebar collapsed, modal open) → Browser localStorage
- **User Preferences**: Persisted settings (theme, intervals) → Database + localStorage cache
- **Navigation State**: Current route tracking → Blazor NavigationManager
- **Real-time Updates**: SignalR for server-pushed data (unchanged from spec 002)

**State Architecture**:

```csharp
// Client-side UI State Service
public class UIStateService
{
    private bool _sidebarCollapsed = false;
    public event Action? OnStateChanged;

    public bool SidebarCollapsed
    {
        get => _sidebarCollapsed;
        set
        {
            _sidebarCollapsed = value;
            // Do NOT persist - always start expanded per spec
            NotifyStateChanged();
        }
    }
}

// User Preferences Service (Database-backed)
public class UserPreferencesService
{
    private UserPreferences _preferences;

    public async Task<UserPreferences> GetPreferencesAsync()
    {
        // Try localStorage cache first
        var cached = await localStorage.GetItemAsync<UserPreferences>("preferences");
        if (cached != null) return cached;

        // Fallback to database
        return await repository.GetPreferencesAsync();
    }

    public async Task SavePreferencesAsync(UserPreferences prefs)
    {
        // Save to both database and localStorage
        await repository.SavePreferencesAsync(prefs);
        await localStorage.SetItemAsync("preferences", prefs);
        NotifyPreferencesChanged();
    }
}
```

**Cascading State Pattern**:
```razor
@* App.razor or MainLayout.razor *@
<CascadingValue Value="uiState">
<CascadingValue Value="userPreferences">
    @Body
</CascadingValue>
</CascadingValue>
```

**Alternatives Considered**:
1. **Fluxor (Redux pattern)**: Rejected - overkill for this scope, adds complexity
2. **Pure Database State**: Rejected - slower, unnecessary server round-trips for UI state
3. **Pure Client State**: Rejected - preferences must persist across devices/browsers
4. **Session Storage**: Rejected - clears on tab close, not suitable for preferences

**Benefits**:
- Fast UI interactions (client-side state)
- Persistent user preferences (database)
- Simple mental model (clear client vs server boundaries)
- No external state management library needed

---

## 4. Theme Implementation

### Decision: Tailwind Dark Mode with CSS Variables

**Rationale**:
- **Tailwind Dark Mode**: Built-in `dark:` variant support
- **CSS Variables**: Dynamic theme values without recompilation
- **Class Strategy**: Use `class="dark"` on root element for explicit control
- **Persistence**: Store theme preference in database + localStorage

**Implementation**:

```css
/* app.css */
@tailwind base;
@tailwind components;
@tailwind utilities;

@layer base {
  :root {
    --color-background: 255 255 255;
    --color-foreground: 0 0 0;
    --color-primary: 59 130 246;      /* blue-500 */
    --color-success: 34 197 94;       /* green-500 */
    --color-warning: 234 179 8;       /* yellow-500 */
    --color-danger: 239 68 68;        /* red-500 */
  }

  .dark {
    --color-background: 17 24 39;     /* gray-900 */
    --color-foreground: 243 244 246;  /* gray-100 */
    --color-primary: 96 165 250;      /* blue-400 */
    --color-success: 74 222 128;      /* green-400 */
    --color-warning: 250 204 21;      /* yellow-400 */
    --color-danger: 248 113 113;      /* red-400 */
  }
}
```

```razor
@* ThemeProvider.razor *@
<div class="@ThemeClass">
    @ChildContent
</div>

@code {
    [Parameter] public RenderFragment? ChildContent { get; set; }

    private string ThemeClass =>
        Preferences?.Theme == Theme.Dark ? "dark" : "";
}
```

**Tailwind Configuration**:
```javascript
// tailwind.config.js
module.exports = {
  darkMode: 'class',
  theme: {
    extend: {
      colors: {
        background: 'rgb(var(--color-background) / <alpha-value>)',
        foreground: 'rgb(var(--color-foreground) / <alpha-value>)',
        // ...
      }
    }
  }
}
```

**Alternatives Considered**:
1. **Media Query Strategy**: Rejected - can't override system preference
2. **Separate CSS Files**: Rejected - doubles CSS bundle size
3. **JavaScript Theme Switching**: Rejected - Blazor can handle with C#
4. **Pre-built Theme Libraries**: Rejected - less control

**Benefits**:
- Smooth theme transitions without page reload
- Respects user preference over system default
- Leverages Tailwind's built-in dark mode
- Minimal CSS overhead

---

## 5. Accessibility Implementation

### Decision: WCAG 2.1 Level AA Compliance with Blazor Components

**Rationale**:
- **Target**: WCAG 2.1 Level AA (per spec and constitution)
- **Keyboard Navigation**: Native HTML semantics + Blazor `@onkeydown` handlers
- **ARIA**: Proper roles, labels, and states on all interactive elements
- **Focus Management**: Visible focus rings, logical tab order, focus trapping

**Key Accessibility Patterns**:

1. **Navigation Menu**:
```razor
<nav aria-label="Main navigation" role="navigation">
    <ul role="list">
        <li>
            <a href="/"
               role="menuitem"
               aria-current="@(IsActive("/") ? "page" : null)"
               class="focus:ring-2 focus:ring-blue-500">
                Dashboard
            </a>
        </li>
    </ul>
</nav>
```

2. **Modal Dialogs**:
```razor
<div role="dialog"
     aria-modal="true"
     aria-labelledby="modal-title"
     @onkeydown="HandleKeyDown">
    <h2 id="modal-title">@Title</h2>
    @* Focus trap implementation *@
</div>
```

3. **Form Fields**:
```razor
<div class="form-field">
    <label for="@InputId">@Label</label>
    <input id="@InputId"
           aria-describedby="@($"{InputId}-help")"
           aria-invalid="@(HasError ? "true" : null)" />
    <span id="@($"{InputId}-help")" class="text-sm">@HelpText</span>
</div>
```

**Color Contrast Requirements**:
- Normal text: 4.5:1 contrast ratio
- Large text (18pt+): 3:1 contrast ratio
- Interactive elements: 3:1 against adjacent colors

**Keyboard Shortcuts**:
- `Tab` / `Shift+Tab`: Navigate focus
- `Enter` / `Space`: Activate buttons/links
- `Escape`: Close modals/dropdowns
- `Arrow Keys`: Navigate within menus/lists
- `Alt+[Letter]`: Global navigation shortcuts (optional enhancement)

**Alternatives Considered**:
1. **Third-party A11y Libraries**: Rejected - Blazor + proper HTML sufficient
2. **AAA Compliance**: Rejected - out of scope per spec
3. **Screen Reader Testing Only**: Rejected - need full WCAG checklist

**Testing Strategy**:
- Automated: axe-core or pa11y in CI/CD
- Manual: Keyboard-only navigation testing
- Screen Reader: NVDA (Windows) / VoiceOver (Mac) spot checks

**Benefits**:
- Inclusive design for all users
- Legal compliance (ADA, Section 508)
- Better keyboard navigation for power users
- Improved SEO and semantic HTML

---

## 6. Form Validation & User Feedback

### Decision: Built-in Data Annotations with Custom Toast System

**Rationale**:
- **Data Annotations**: Leverage .NET's validation attributes for consistency
- **Blazor EditForm**: Built-in validation display components
- **Custom Toast**: Tailwind-based notification system for non-form feedback
- **Real-time Validation**: Display errors on blur, not on every keystroke

**Validation Pattern**:

```csharp
public class UserPreferencesValidator
{
    public ValidationResult Validate(UserPreferences prefs)
    {
        var errors = new List<string>();

        if (prefs.DashboardRefreshInterval < 1 || prefs.DashboardRefreshInterval > 300)
            errors.Add("Refresh interval must be between 1 and 300 seconds");

        if (prefs.NotificationDuration < 2 || prefs.NotificationDuration > 10)
            errors.Add("Notification duration must be between 2 and 10 seconds");

        return errors.Any()
            ? ValidationResult.Error(errors)
            : ValidationResult.Success();
    }
}
```

**Toast Notification System**:
```razor
@* ToastContainer.razor *@
<div class="fixed top-4 right-4 z-50 space-y-2">
    @foreach (var toast in ToastService.Toasts)
    {
        <div class="@GetToastClasses(toast.Type) rounded-lg shadow-lg p-4 flex items-center gap-3
                    animate-slide-in-right">
            <Icon Name="@GetIcon(toast.Type)" />
            <p>@toast.Message</p>
            <button @onclick="() => ToastService.Dismiss(toast.Id)"
                    aria-label="Dismiss">
                <Icon Name="IconName.XMark" />
            </button>
        </div>
    }
</div>

@code {
    private string GetToastClasses(ToastType type) => type switch
    {
        ToastType.Success => "bg-green-100 dark:bg-green-900 text-green-800 dark:text-green-100",
        ToastType.Error => "bg-red-100 dark:bg-red-900 text-red-800 dark:text-red-100",
        ToastType.Warning => "bg-yellow-100 dark:bg-yellow-900 text-yellow-800 dark:text-yellow-100",
        ToastType.Info => "bg-blue-100 dark:bg-blue-900 text-blue-800 dark:text-blue-100",
        _ => ""
    };
}
```

**Unsaved Changes Detection**:
```razor
@implements IDisposable

<EditForm Model="Model" OnValidSubmit="HandleSubmit">
    <NavigationLock OnBeforeInternalNavigation="OnNavigating" />
    @* Form fields *@
</EditForm>

@code {
    private bool _isDirty = false;

    private async Task OnNavigating(LocationChangingContext context)
    {
        if (!_isDirty) return;

        var confirmed = await JSRuntime.InvokeAsync<bool>(
            "confirm",
            "You have unsaved changes. Are you sure you want to leave?");

        if (!confirmed)
            context.PreventNavigation();
    }
}
```

**Alternatives Considered**:
1. **FluentValidation**: Rejected - overkill for simple validation rules
2. **Client-side Only Validation**: Rejected - must validate server-side for security
3. **Alert() for Notifications**: Rejected - poor UX, blocking
4. **SignalR for Toasts**: Rejected - toasts are client-side UI events

**Benefits**:
- Familiar .NET validation patterns
- Clear, non-intrusive error feedback
- Prevents data loss from accidental navigation
- Configurable notification display duration

---

## 7. Responsive Design Strategy

### Decision: Desktop-First with Tailwind Breakpoints (min 1024px)

**Rationale**:
- **Target Audience**: Desktop traders (per spec assumptions)
- **Minimum Width**: 1024px (tablet landscape / small desktop)
- **Breakpoint Strategy**: Default desktop, responsive down to 1024px
- **Navigation**: Collapsible sidebar for space optimization

**Tailwind Breakpoints**:
```
Default (Desktop):    1280px+     Full sidebar, multi-column layouts
lg:                   1024px+     Collapsed sidebar option, 2-column
md:                   768px       Out of scope (mobile)
sm:                   640px       Out of scope (mobile)
```

**Responsive Patterns**:

1. **Navigation Sidebar**:
```razor
<aside class="@SidebarClasses">
    @* Expanded: w-64, Collapsed: w-16 *@
</aside>
<main class="@ContentClasses">
    @* Adjusts margin based on sidebar state *@
</main>

@code {
    private string SidebarClasses =>
        $"fixed inset-y-0 left-0 transition-all duration-300 " +
        $"{IsCollapsed ? "w-16" : "w-64"}";

    private string ContentClasses =>
        $"transition-all duration-300 " +
        $"{IsCollapsed ? "ml-16" : "ml-64"}";
}
```

2. **Data Tables**:
```html
<!-- Horizontal scroll on smaller screens -->
<div class="overflow-x-auto">
    <table class="min-w-full">
        <!-- Table content -->
    </table>
</div>
```

3. **Dashboard Grid**:
```html
<!-- 3 columns default, 2 columns at lg breakpoint -->
<div class="grid grid-cols-3 lg:grid-cols-2 gap-4">
    <div class="card">...</div>
</div>
```

**Alternatives Considered**:
1. **Mobile-First**: Rejected - not the primary use case per spec
2. **Fixed Layout**: Rejected - wastes space on large monitors
3. **Full Mobile Support**: Rejected - explicitly out of scope
4. **Responsive Tables Library**: Rejected - Tailwind utilities sufficient

**Benefits**:
- Optimized for target users (desktop traders)
- Graceful degradation to minimum supported width
- Clear scope boundary (no mobile complexity)
- Smooth sidebar transitions

---

## 8. Performance Optimization

### Decision: Blazor Server with SignalR Throttling + Lazy Loading

**Rationale**:
- **Existing Architecture**: Blazor Server already implemented (spec 002)
- **SignalR Throttling**: Reduce unnecessary re-renders from real-time updates
- **Lazy Loading**: Load heavy components (charts) only when needed
- **Component Virtualization**: For long lists (trade history, logs)

**Optimization Strategies**:

1. **SignalR Throttling**:
```csharp
public class ThrottledMarketDataHub : Hub
{
    private readonly TimeSpan _throttleInterval = TimeSpan.FromMilliseconds(500);

    public async Task StreamPriceUpdates(CancellationToken cancellationToken)
    {
        await foreach (var update in GetPriceUpdates(cancellationToken))
        {
            await Clients.All.SendAsync("PriceUpdate", update, cancellationToken);
            await Task.Delay(_throttleInterval, cancellationToken);
        }
    }
}
```

2. **Lazy Component Loading**:
```razor
@* Load chart only when tab is active *@
@if (ActiveTab == "performance")
{
    <Suspense Fallback="@LoadingSpinner">
        <EquityCurveChart Data="chartData" />
    </Suspense>
}
```

3. **Virtualization for Lists**:
```razor
<Virtualize Items="trades" Context="trade">
    <TradeRow Trade="trade" />
</Virtualize>
```

4. **Caching Strategy**:
```csharp
[ResponseCache(Duration = 60, VaryByQueryKeys = new[] { "filter" })]
public async Task<PerformanceMetrics> GetMetricsAsync()
{
    // Cache performance metrics for 60 seconds
}
```

**Performance Targets** (per constitution):
- API Endpoints: < 200ms p95
- Page Load: < 1.5s FCP, < 3.5s TTI
- SignalR Updates: Throttled to 500ms (2 updates/sec max)
- Dashboard Refresh: User-configurable 1-300s

**Monitoring**:
- Browser Performance API for client-side metrics
- Serilog for server-side timing
- SignalR connection health monitoring

**Alternatives Considered**:
1. **Blazor WebAssembly**: Rejected - requires architecture rewrite
2. **No Throttling**: Rejected - unnecessary server load and re-renders
3. **Server-Side Rendering (SSR)**: Rejected - Blazor Server is interactive
4. **Aggressive Caching**: Rejected - trading data must be near real-time

**Benefits**:
- Meets constitutional performance requirements
- Reduced server load from smart throttling
- Improved perceived performance via lazy loading
- Scalable for multiple concurrent users (future)

---

## 9. Database Schema for User Preferences

### Decision: Extend Existing SQLite Database with UserPreferences Table

**Rationale**:
- **Single Database**: Maintain single SQLite database for simplicity
- **EF Core Migration**: Use existing migration workflow
- **JSON Column**: Store flexible settings as JSON for extensibility
- **User FK**: Prepare for multi-user support (even though single user now)

**Schema Design**:

```sql
CREATE TABLE UserPreferences (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    UserId TEXT NOT NULL DEFAULT 'default',  -- Future-proof for auth
    Theme TEXT NOT NULL CHECK(Theme IN ('light', 'dark')) DEFAULT 'light',
    DashboardRefreshInterval INTEGER NOT NULL DEFAULT 5 CHECK(DashboardRefreshInterval BETWEEN 1 AND 300),
    NotificationDuration INTEGER NOT NULL DEFAULT 5 CHECK(NotificationDuration BETWEEN 2 AND 10),
    ShowSuccessNotifications INTEGER NOT NULL DEFAULT 1,  -- SQLite boolean as int
    ShowErrorNotifications INTEGER NOT NULL DEFAULT 1,
    ShowInfoNotifications INTEGER NOT NULL DEFAULT 1,
    ShowWarningNotifications INTEGER NOT NULL DEFAULT 1,
    CustomSettings TEXT,  -- JSON column for future extensions
    CreatedAt TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP,
    UpdatedAt TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP,
    UNIQUE(UserId)
);

-- Default preferences for single user
INSERT INTO UserPreferences (UserId) VALUES ('default');
```

**Entity Model**:
```csharp
public class UserPreferences
{
    public int Id { get; set; }
    public string UserId { get; set; } = "default";
    public Theme Theme { get; set; } = Theme.Light;
    public int DashboardRefreshInterval { get; set; } = 5;
    public int NotificationDuration { get; set; } = 5;
    public bool ShowSuccessNotifications { get; set; } = true;
    public bool ShowErrorNotifications { get; set; } = true;
    public bool ShowInfoNotifications { get; set; } = true;
    public bool ShowWarningNotifications { get; set; } = true;
    public string? CustomSettings { get; set; }  // JSON
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public sealed class Theme : SmartEnum<Theme>
{
    public static readonly Theme Light = new(0, nameof(Light));
    public static readonly Theme Dark = new(1, nameof(Dark));

    private Theme(int value, string name) : base(value, name) { }
}
```

**EF Core Configuration**:
```csharp
public class UserPreferencesConfiguration : IEntityTypeConfiguration<UserPreferences>
{
    public void Configure(EntityTypeBuilder<UserPreferences> builder)
    {
        builder.ToTable("UserPreferences");
        builder.HasKey(p => p.Id);
        builder.HasIndex(p => p.UserId).IsUnique();

        builder.Property(p => p.Theme)
            .HasConversion(
                v => v.Name,
                v => Theme.FromName(v, false))
            .IsRequired();

        builder.Property(p => p.CustomSettings)
            .HasColumnType("TEXT");  // JSON stored as TEXT
    }
}
```

**Alternatives Considered**:
1. **Separate Database**: Rejected - unnecessary complexity
2. **Separate Table per Setting**: Rejected - over-normalization
3. **All Settings in JSON**: Rejected - lose type safety and constraints
4. **Local Storage Only**: Rejected - can't sync across devices (future)

**Benefits**:
- Type-safe core settings with database constraints
- Extensible via JSON column
- Consistent with existing data architecture
- Future-proof for multi-user scenarios

---

## 10. Component Reusability Patterns

### Decision: Headless Component Pattern with Slot-Based Composition

**Rationale**:
- **Headless Components**: Separate behavior from presentation
- **Slots (RenderFragments)**: Allow flexible content injection
- **Base Classes**: Share common component logic
- **Generic Components**: Type-safe reusable components

**Pattern Examples**:

1. **Headless Disclosure Component** (for collapsible sections):
```razor
@* DisclosureBase.razor *@
<div>
    <button @onclick="Toggle"
            aria-expanded="@IsOpen"
            aria-controls="@PanelId"
            class="@ButtonClass">
        @ButtonContent
    </button>

    @if (IsOpen)
    {
        <div id="@PanelId" class="@PanelClass">
            @PanelContent
        </div>
    }
</div>

@code {
    [Parameter] public bool IsOpen { get; set; }
    [Parameter] public EventCallback<bool> IsOpenChanged { get; set; }
    [Parameter] public RenderFragment? ButtonContent { get; set; }
    [Parameter] public RenderFragment? PanelContent { get; set; }
    [Parameter] public string ButtonClass { get; set; } = "";
    [Parameter] public string PanelClass { get; set; } = "";

    private string PanelId { get; } = Guid.NewGuid().ToString();

    private async Task Toggle()
    {
        IsOpen = !IsOpen;
        await IsOpenChanged.InvokeAsync(IsOpen);
    }
}
```

Usage:
```razor
<DisclosureBase @bind-IsOpen="sidebarOpen">
    <ButtonContent>
        <Icon Name="IconName.Bars3" />
    </ButtonContent>
    <PanelContent>
        <NavigationMenu />
    </PanelContent>
</DisclosureBase>
```

2. **Generic Table Component**:
```razor
@typeparam TItem

<table class="min-w-full divide-y divide-gray-200 dark:divide-gray-700">
    <thead>
        <tr>
            @TableHeader
        </tr>
    </thead>
    <tbody class="divide-y divide-gray-200 dark:divide-gray-700">
        @foreach (var item in Items)
        {
            <tr class="hover:bg-gray-50 dark:hover:bg-gray-800">
                @RowTemplate(item)
            </tr>
        }
    </tbody>
</table>

@code {
    [Parameter] public IEnumerable<TItem> Items { get; set; } = [];
    [Parameter] public RenderFragment? TableHeader { get; set; }
    [Parameter] public RenderFragment<TItem>? RowTemplate { get; set; }
}
```

3. **Button with Variants**:
```razor
<button type="@Type"
        class="@GetButtonClasses()"
        disabled="@IsDisabled"
        @onclick="OnClick">
    @if (IsLoading)
    {
        <Spinner Size="sm" />
    }
    @if (LeadingIcon != null)
    {
        <Icon Name="@LeadingIcon" Class="w-5 h-5" />
    }
    @ChildContent
    @if (TrailingIcon != null)
    {
        <Icon Name="@TrailingIcon" Class="w-5 h-5" />
    }
</button>

@code {
    [Parameter] public RenderFragment? ChildContent { get; set; }
    [Parameter] public ButtonVariant Variant { get; set; } = ButtonVariant.Primary;
    [Parameter] public ButtonSize Size { get; set; } = ButtonSize.Medium;
    [Parameter] public IconName? LeadingIcon { get; set; }
    [Parameter] public IconName? TrailingIcon { get; set; }
    [Parameter] public bool IsLoading { get; set; }
    [Parameter] public bool IsDisabled { get; set; }
    [Parameter] public string Type { get; set; } = "button";
    [Parameter] public EventCallback<MouseEventArgs> OnClick { get; set; }

    private string GetButtonClasses()
    {
        var baseClasses = "inline-flex items-center gap-2 font-medium rounded-lg " +
                          "focus:outline-none focus:ring-2 focus:ring-offset-2 " +
                          "disabled:opacity-50 disabled:cursor-not-allowed transition-colors";

        var sizeClasses = Size switch
        {
            ButtonSize.Small => "px-3 py-1.5 text-sm",
            ButtonSize.Medium => "px-4 py-2 text-base",
            ButtonSize.Large => "px-6 py-3 text-lg",
            _ => ""
        };

        var variantClasses = Variant switch
        {
            ButtonVariant.Primary => "bg-blue-600 text-white hover:bg-blue-700 focus:ring-blue-500",
            ButtonVariant.Secondary => "bg-gray-200 text-gray-900 hover:bg-gray-300 focus:ring-gray-500",
            ButtonVariant.Danger => "bg-red-600 text-white hover:bg-red-700 focus:ring-red-500",
            ButtonVariant.Ghost => "text-gray-700 hover:bg-gray-100 focus:ring-gray-500",
            _ => ""
        };

        return $"{baseClasses} {sizeClasses} {variantClasses}";
    }
}

public enum ButtonVariant { Primary, Secondary, Danger, Ghost }
public enum ButtonSize { Small, Medium, Large }
```

**Component Documentation**:
Each component should have XML doc comments explaining:
- Purpose and use cases
- Required vs optional parameters
- Usage examples
- Accessibility considerations

**Alternatives Considered**:
1. **Copy-Paste Components**: Rejected - violates DRY
2. **Inheritance-Heavy**: Rejected - composition over inheritance
3. **Monolithic Components**: Rejected - reduces flexibility
4. **External Component Library**: Rejected - per user request

**Benefits**:
- Maximum reusability and flexibility
- Type-safe generic components
- Clear separation of concerns
- Easy to test in isolation
- Consistent API across component library

---

## 11. Navigation State Management

### Decision: Blazor NavigationManager with Custom Active Route Detection

**Rationale**:
- **Built-in NavigationManager**: No additional dependencies
- **Active Route Detection**: Highlight current page in sidebar
- **Navigation Guards**: Prevent navigation with unsaved changes
- **Keyboard Shortcuts**: Global keyboard handlers for navigation

**Implementation**:

```csharp
public class NavigationService
{
    private readonly NavigationManager _navigationManager;

    public NavigationService(NavigationManager navigationManager)
    {
        _navigationManager = navigationManager;
        _navigationManager.LocationChanged += OnLocationChanged;
    }

    public bool IsActive(string href, bool exactMatch = false)
    {
        var currentPath = new Uri(_navigationManager.Uri).AbsolutePath;

        return exactMatch
            ? currentPath.Equals(href, StringComparison.OrdinalIgnoreCase)
            : currentPath.StartsWith(href, StringComparison.OrdinalIgnoreCase);
    }

    public void Navigate(string href, bool forceLoad = false)
    {
        _navigationManager.NavigateTo(href, forceLoad);
    }

    private void OnLocationChanged(object? sender, LocationChangedEventArgs e)
    {
        OnNavigationChanged?.Invoke(e.Location);
    }

    public event Action<string>? OnNavigationChanged;
}
```

**Keyboard Shortcuts**:
```razor
@implements IDisposable
@inject IJSRuntime JS

<div @ref="rootElement" tabindex="-1" @onkeydown="HandleKeyDown">
    @ChildContent
</div>

@code {
    private ElementReference rootElement;

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            await JS.InvokeVoidAsync("focusElement", rootElement);
        }
    }

    private void HandleKeyDown(KeyboardEventArgs e)
    {
        if (!e.AltKey) return;

        var route = e.Key switch
        {
            "d" or "D" => "/",              // Alt+D = Dashboard
            "p" or "P" => "/portfolio",     // Alt+P = Portfolio
            "r" or "R" => "/performance",   // Alt+R = Performance (Results)
            "s" or "S" => "/strategies",    // Alt+S = Strategies
            "g" or "G" => "/settings",      // Alt+G = Settings (Gear)
            "b" or "B" => "/backtest",      // Alt+B = Backtest
            _ => null
        };

        if (route != null)
            NavigationService.Navigate(route);
    }
}
```

**Navigation Menu with Active State**:
```razor
<nav class="space-y-1" role="navigation" aria-label="Main navigation">
    @foreach (var item in MenuItems)
    {
        <NavLink href="@item.Href"
                 class="@GetNavLinkClasses(item)"
                 ActiveClass="bg-blue-100 dark:bg-blue-900 text-blue-700 dark:text-blue-100"
                 Match="@item.MatchMode">
            <Icon Name="@item.Icon" Class="w-5 h-5" />
            @if (!IsSidebarCollapsed)
            {
                <span>@item.Label</span>
            }
        </NavLink>
    }
</nav>

@code {
    private record MenuItem(
        string Label,
        string Href,
        IconName Icon,
        NavLinkMatch MatchMode = NavLinkMatch.Prefix);

    private MenuItem[] MenuItems =>
    [
        new("Dashboard", "/", IconName.Home, NavLinkMatch.All),
        new("Portfolio", "/portfolio", IconName.Briefcase),
        new("Performance", "/performance", IconName.ChartBar),
        new("Strategies", "/strategies", IconName.Beaker),
        new("Risk Settings", "/settings/risk", IconName.ShieldCheck),
        new("Backtesting", "/backtest", IconName.ChartLine),
        new("Settings", "/settings", IconName.Cog),
    ];
}
```

**Alternatives Considered**:
1. **Manual Route Tracking**: Rejected - Blazor provides this built-in
2. **React Router-style Library**: Rejected - not needed for Blazor
3. **No Keyboard Shortcuts**: Rejected - accessibility and power user benefit
4. **Hash-based Routing**: Rejected - clean URLs preferred

**Benefits**:
- Native Blazor routing capabilities
- Clear visual feedback of current location
- Keyboard accessibility for navigation
- Simple implementation without external dependencies

---

## Technology Stack Summary

| Category | Technology | Justification |
|----------|-----------|---------------|
| **Framework** | ASP.NET Core Blazor Server (.NET 9) | Existing architecture from spec 002 |
| **Styling** | Tailwind CSS 3.x | Utility-first, no component library overhead |
| **Icons** | Heroicons (SVG) | Designed for Tailwind, MIT licensed, lightweight |
| **Database** | SQLite + EF Core 9 | Existing infrastructure, single-file simplicity |
| **State** | Hybrid (Services + Cascading) | Client UI state + persisted preferences |
| **Theme** | CSS Variables + Tailwind Dark Mode | Built-in Tailwind support, dynamic switching |
| **Validation** | Data Annotations + Custom | .NET standard, type-safe |
| **Accessibility** | Native HTML + ARIA | WCAG 2.1 AA compliance |
| **Performance** | SignalR Throttling + Lazy Loading | Meet constitutional targets |
| **Architecture** | Atomic Design Pattern | Clear hierarchy, reusable components |

---

## Next Steps (Phase 1)

1. **Data Model**: Define complete entity models and relationships
2. **Contracts**: Create API contracts for settings endpoints (if needed)
3. **Quickstart**: Developer setup guide for new components
4. **Update Agent Context**: Add Tailwind + Heroicons to tech stack

---

**Research Complete**: All technical unknowns resolved. Ready for Phase 1 design.