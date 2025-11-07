# Quickstart Guide: UX/UI Enhancement - Navigation & Settings

**Feature**: 003-ux-ui-enhancement | **Date**: 2025-11-07

## Overview

This quickstart guide provides step-by-step instructions for developers to understand, build, test, and deploy the UX/UI enhancements to the TradingBot Blazor Server dashboard.

## Prerequisites

- Completed implementation of spec 002 (Blazor Server Trading Dashboard)
- .NET 9 SDK installed
- Node.js 18+ and npm installed (for Tailwind CSS and DaisyUI)
- SQLite database with existing TradingBot schema
- IDE with C# support (Visual Studio 2022, Rider, or VS Code with C# extension)
- Basic understanding of Blazor Server, Tailwind CSS, and Entity Framework Core

## Quick Start (5 Minutes)

### 1. Install DaisyUI

```bash
cd src/TradingBot.Web
npm install daisyui@latest
```

### 2. Update Tailwind Configuration

**File**: `src/TradingBot.Web/tailwind.config.js`

```javascript
module.exports = {
  content: [
    './Components/**/*.{razor,html,cs}',
    './Pages/**/*.{razor,html,cs}'
  ],
  theme: {
    extend: {},
  },
  plugins: [
    require('daisyui'),
  ],
  daisyui: {
    themes: ["light", "dark"],
  },
}
```

### 3. Create Database Migration

```bash
cd src/TradingBot.Infrastructure
dotnet ef migrations add AddUserPreferences --startup-project ../TradingBot.Web
dotnet ef database update --startup-project ../TradingBot.Web
```

### 4. Run the Application

```bash
cd src/TradingBot.Web
dotnet run
```

Navigate to: `https://localhost:5001`

## Development Workflow

### Phase 1: Core Entities & Services (Day 1)

#### Step 1: Create UserPreferences Entity

**File**: `src/TradingBot.Core/Models/UserPreferences.cs`

```csharp
namespace TradingBot.Core.Models;

public class UserPreferences
{
    public int Id { get; set; }
    public string UserId { get; set; } = string.Empty;
    public string Theme { get; set; } = "light";
    public int DashboardRefreshInterval { get; set; } = 10;
    public string NotificationTypesEnabled { get; set; } = "{\"success\":true,\"error\":true,\"info\":true,\"warning\":true}";
    public int NotificationDuration { get; set; } = 5;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
```

#### Step 2: Create Repository Interface

**File**: `src/TradingBot.Core/Interfaces/IUserPreferencesRepository.cs`

```csharp
namespace TradingBot.Core.Interfaces;

public interface IUserPreferencesRepository
{
    Task<UserPreferences?> GetByUserIdAsync(string userId, CancellationToken cancellationToken = default);
    Task<UserPreferences> CreateAsync(UserPreferences preferences, CancellationToken cancellationToken = default);
    Task<UserPreferences> UpdateAsync(UserPreferences preferences, CancellationToken cancellationToken = default);
    Task<UserPreferences> GetOrCreateDefaultAsync(string userId, CancellationToken cancellationToken = default);
}
```

#### Step 3: Implement Repository

**File**: `src/TradingBot.Infrastructure/Persistence/Repositories/UserPreferencesRepository.cs`

See `data-model.md` for complete implementation.

#### Step 4: Configure Entity Framework

**File**: `src/TradingBot.Infrastructure/Persistence/Configurations/UserPreferencesConfiguration.cs`

See `data-model.md` for complete configuration.

**Register in DbContext**:

```csharp
// In TradingBotDbContext.cs
public DbSet<UserPreferences> UserPreferences => Set<UserPreferences>();

protected override void OnModelCreating(ModelBuilder modelBuilder)
{
    base.OnModelCreating(modelBuilder);
    modelBuilder.ApplyConfiguration(new UserPreferencesConfiguration());
}
```

### Phase 2: Theme Service (Day 2)

#### Step 1: Create Theme Service

**File**: `src/TradingBot.Web/Services/ThemeService.cs`

```csharp
namespace TradingBot.Web.Services;

public class ThemeService : IThemeService
{
    private readonly IJSRuntime _jsRuntime;
    private readonly IUserPreferencesRepository _preferencesRepository;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public ThemeService(
        IJSRuntime jsRuntime,
        IUserPreferencesRepository preferencesRepository,
        IHttpContextAccessor httpContextAccessor)
    {
        _jsRuntime = jsRuntime;
        _preferencesRepository = preferencesRepository;
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task LoadUserThemeAsync()
    {
        var userId = _httpContextAccessor.HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId)) return;

        var preferences = await _preferencesRepository.GetOrCreateDefaultAsync(userId);
        await SetThemeAsync(preferences.Theme);
    }

    public async Task SetThemeAsync(string theme)
    {
        await _jsRuntime.InvokeVoidAsync("setTheme", theme);
    }
}
```

#### Step 2: Add JavaScript Theme Switcher

**File**: `src/TradingBot.Web/wwwroot/js/theme-switcher.js`

```javascript
window.setTheme = (theme) => {
    document.documentElement.setAttribute('data-theme', theme);
    localStorage.setItem('theme', theme);
};

window.getTheme = () => {
    return document.documentElement.getAttribute('data-theme') || 'light';
};
```

### Phase 3: Left Sidebar Navigation (Day 3)

#### Step 1: Create LeftSidebar Component

**File**: `src/TradingBot.Web/Components/Layout/LeftSidebar.razor`

```razor
@inject NavigationManager Navigation

<aside class="@SidebarClass">
    <nav class="menu p-4 w-64 min-h-screen bg-base-200">
        <button @onclick="ToggleSidebar"
                class="btn btn-ghost btn-sm mb-4"
                aria-label="Toggle sidebar">
            <span class="text-xl">☰</span>
        </button>

        <ul class="space-y-2">
            <li>
                <a href="/" class="@GetMenuItemClass("/")">
                    <span class="icon">📊</span>
                    @if (!IsCollapsed) { <span>Dashboard</span> }
                </a>
            </li>
            <li>
                <a href="/portfolio" class="@GetMenuItemClass("/portfolio")">
                    <span class="icon">💼</span>
                    @if (!IsCollapsed) { <span>Portfolio</span> }
                </a>
            </li>
            <!-- Add remaining menu items -->
        </ul>
    </nav>
</aside>

@code {
    [Parameter] public bool IsCollapsed { get; set; }
    [Parameter] public EventCallback<bool> IsCollapsedChanged { get; set; }

    private string SidebarClass => IsCollapsed ? "sidebar-collapsed" : "sidebar-expanded";

    private void ToggleSidebar()
    {
        IsCollapsed = !IsCollapsed;
        IsCollapsedChanged.InvokeAsync(IsCollapsed);
    }

    private string GetMenuItemClass(string href)
    {
        var currentPath = new Uri(Navigation.Uri).PathAndQuery;
        return currentPath == href ? "menu-item active" : "menu-item";
    }
}
```

#### Step 2: Integrate into MainLayout

**File**: `src/TradingBot.Web/Components/Layout/MainLayout.razor`

```razor
@inherits LayoutComponentBase

<div class="flex min-h-screen">
    <LeftSidebar IsCollapsed="@_sidebarCollapsed"
                 IsCollapsedChanged="@OnSidebarCollapsedChanged" />

    <main class="flex-1 p-6">
        @Body
    </main>
</div>

@code {
    private bool _sidebarCollapsed = false;

    private void OnSidebarCollapsedChanged(bool isCollapsed)
    {
        _sidebarCollapsed = isCollapsed;
        StateHasChanged();
    }
}
```

### Phase 4: Settings Page (Day 4-5)

#### Step 1: Create Settings Components

See `plan.md` Project Structure for complete component list.

#### Step 2: Create Preferences Service

**File**: `src/TradingBot.Web/Services/PreferencesService.cs`

```csharp
public class PreferencesService
{
    private readonly IUserPreferencesRepository _repository;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IMemoryCache _cache;

    // Load, Save, Reset methods...
}
```

### Phase 5: Testing (Day 6)

#### Unit Tests

```bash
cd tests/TradingBot.Web.Tests
dotnet test
```

**Example Component Test**:

```csharp
public class LeftSidebarTests : TestContext
{
    [Fact]
    public void LeftSidebar_WhenCollapsed_ShowsIconsOnly()
    {
        // Arrange
        var cut = RenderComponent<LeftSidebar>(parameters => parameters
            .Add(p => p.IsCollapsed, true));

        // Act
        var menuItems = cut.FindAll(".menu-item span:not(.icon)");

        // Assert
        menuItems.Should().BeEmpty();
    }
}
```

#### Integration Tests

```csharp
[Fact]
public async Task UserPreferences_SaveAndLoad_PersistsCorrectly()
{
    // Arrange
    var preferences = new UserPreferences { /* ... */ };

    // Act
    await _repository.CreateAsync(preferences);
    var loaded = await _repository.GetByUserIdAsync(preferences.UserId);

    // Assert
    loaded.Should().NotBeNull();
    loaded!.Theme.Should().Be(preferences.Theme);
}
```

## Common Commands

### Database Operations

```bash
# Create migration
dotnet ef migrations add MigrationName --project src/TradingBot.Infrastructure --startup-project src/TradingBot.Web

# Apply migration
dotnet ef database update --project src/TradingBot.Infrastructure --startup-project src/TradingBot.Web

# Rollback migration
dotnet ef database update PreviousMigrationName --project src/TradingBot.Infrastructure --startup-project src/TradingBot.Web

# Remove last migration (if not applied)
dotnet ef migrations remove --project src/TradingBot.Infrastructure --startup-project src/TradingBot.Web
```

### CSS Build

```bash
# Development build (watch mode)
cd src/TradingBot.Web
npm run css:watch

# Production build
npm run css:build
```

### Testing

```bash
# Run all tests
dotnet test

# Run specific test project
dotnet test tests/TradingBot.Web.Tests

# Run tests with coverage
dotnet test --collect:"XPlat Code Coverage"

# Run specific test
dotnet test --filter "FullyQualifiedName~LeftSidebarTests"
```

### Code Quality

```bash
# Run analyzers
dotnet build /p:RunAnalyzers=true

# Format code
dotnet format
```

## Debugging Tips

### Blazor Component Debugging

1. Use browser DevTools (F12)
2. Check Blazor Server reconnection in Console tab
3. Inspect SignalR network traffic for real-time updates
4. Use `@key` directive to identify component re-rendering issues

### Database Debugging

```bash
# Connect to SQLite database
sqlite3 tradingbot.db

# View UserPreferences table
.schema UserPreferences
SELECT * FROM UserPreferences;
```

### CSS Debugging

- Use browser DevTools Elements tab to inspect applied classes
- Check if Tailwind/DaisyUI classes are being purged incorrectly
- Verify `tailwind.config.js` content paths include all Razor files

## Performance Optimization

### Caching User Preferences

```csharp
// In PreferencesService.cs
public async Task<UserPreferences> GetPreferencesAsync(string userId)
{
    var cacheKey = $"preferences_{userId}";

    if (!_cache.TryGetValue(cacheKey, out UserPreferences? preferences))
    {
        preferences = await _repository.GetOrCreateDefaultAsync(userId);

        var cacheOptions = new MemoryCacheEntryOptions()
            .SetSlidingExpiration(TimeSpan.FromMinutes(30));

        _cache.Set(cacheKey, preferences, cacheOptions);
    }

    return preferences!;
}
```

### Debouncing Settings Saves

```razor
@code {
    private System.Timers.Timer? _saveTimer;

    private void OnSettingChanged()
    {
        _saveTimer?.Stop();
        _saveTimer = new System.Timers.Timer(1000); // 1 second debounce
        _saveTimer.Elapsed += async (s, e) => await SavePreferencesAsync();
        _saveTimer.AutoReset = false;
        _saveTimer.Start();
    }
}
```

## Deployment Checklist

- [ ] Run full test suite (`dotnet test`)
- [ ] Apply all migrations (`dotnet ef database update`)
- [ ] Build CSS for production (`npm run css:build`)
- [ ] Run code analyzers (`dotnet build /p:RunAnalyzers=true`)
- [ ] Test keyboard navigation on all pages
- [ ] Test screen reader compatibility (NVDA/VoiceOver)
- [ ] Verify theme switching works correctly
- [ ] Test settings persistence across sessions
- [ ] Check performance metrics (< 1s settings save, < 100ms visual feedback)
- [ ] Review security (input validation, authorization)

## Troubleshooting

### Issue: Theme not applying

**Solution**: Check browser console for JavaScript errors, verify `theme-switcher.js` is loaded

### Issue: Sidebar not collapsing

**Solution**: Inspect CSS classes, check if Tailwind transitions are working

### Issue: Settings not saving

**Solution**: Check database connection, verify UserPreferences table exists, check logs for errors

### Issue: Keyboard shortcuts not working

**Solution**: Verify JSInterop calls, check for conflicting browser shortcuts, ensure event listeners registered

## Next Steps

After completing this feature:
1. Run `/speckit.tasks` to generate detailed implementation tasks
2. Review generated `tasks.md` for step-by-step implementation plan
3. Begin implementation following the phased approach in `plan.md`
4. Use this quickstart guide as reference during development

## Resources

- [DaisyUI Documentation](https://daisyui.com/)
- [Blazor Server Documentation](https://learn.microsoft.com/en-us/aspnet/core/blazor/)
- [Tailwind CSS Documentation](https://tailwindcss.com/)
- [WCAG 2.1 Guidelines](https://www.w3.org/WAI/WCAG21/quickref/)
- [bUnit Documentation](https://bunit.dev/)
- [EF Core Migrations](https://learn.microsoft.com/en-us/ef/core/managing-schemas/migrations/)
