# Quickstart: UX/UI Enhancement Development Guide

**Feature**: 003-ux-ui-enhancement
**Branch**: `003-ux-ui-enhancement`
**Date**: 2025-11-07

## Overview

This guide provides developers with everything needed to start implementing the UX/UI Enhancement feature for the TradingBot Blazor web application.

---

## Prerequisites

### Required Tools
- .NET 9 SDK
- Node.js 18+ (for Tailwind CSS build)
- npm or yarn
- Your preferred IDE (Visual Studio 2022, Rider, or VS Code)
- SQLite browser (optional, for database inspection)

### Existing Codebase
This feature builds on the existing Blazor Server application from spec `002-blazor-server-app`:
- **Project**: `src/TradingBot.Web`
- **Architecture**: Blazor Server with SignalR
- **Database**: SQLite via Entity Framework Core 9
- **Styling**: Tailwind CSS (already configured)

---

## Project Structure

```
TradingBot/
├── src/
│   └── TradingBot.Web/                    # Main Blazor Server project
│       ├── Components/                     # Blazor components
│       │   ├── Atoms/                     # NEW: Basic UI elements
│       │   │   ├── Button.razor
│       │   │   ├── Input.razor
│       │   │   ├── Icon.razor
│       │   │   └── Badge.razor
│       │   ├── Molecules/                 # NEW: Composite components
│       │   │   ├── FormField.razor
│       │   │   ├── MenuItem.razor
│       │   │   ├── Toast.razor
│       │   │   └── Tooltip.razor
│       │   ├── Organisms/                 # NEW: Complex components
│       │   │   ├── NavigationSidebar.razor
│       │   │   ├── SettingsForm.razor
│       │   │   └── ThemeProvider.razor
│       │   ├── Layout/
│       │   │   ├── MainLayout.razor       # UPDATE: Add sidebar + theme
│       │   │   └── NavMenu.razor          # DEPRECATED: Replace with NavigationSidebar
│       │   ├── Pages/                     # Blazor pages
│       │   │   ├── Settings.razor         # NEW: Settings page
│       │   │   └── ...
│       │   └── Shared/
│       │       └── ToastContainer.razor   # UPDATE: Use new Toast atoms
│       ├── Services/                       # Application services
│       │   ├── UIStateService.cs          # NEW: Client UI state
│       │   ├── UserPreferencesService.cs  # NEW: Preferences business logic
│       │   └── ToastService.cs            # UPDATE: Use preferences
│       ├── Models/                         # View models
│       │   └── ToastNotification.cs       # UPDATE: From data-model.md
│       ├── Styles/
│       │   └── app.css                    # UPDATE: Add theme CSS variables
│       ├── wwwroot/
│       │   └── css/
│       │       └── app.css                # Built Tailwind CSS (generated)
│       └── tailwind.config.js             # UPDATE: Add dark mode, theme colors
├── src/TradingBot.Core/
│   ├── Entities/
│   │   └── UserPreferences.cs             # NEW: From data-model.md
│   ├── ValueObjects/
│   │   └── Theme.cs                       # NEW: SmartEnum
│   ├── Interfaces/
│   │   ├── IUserPreferencesRepository.cs  # NEW
│   │   └── IUserPreferencesService.cs     # NEW
│   └── Validators/
│       └── UserPreferencesValidator.cs    # NEW
└── src/TradingBot.Infrastructure/
    ├── Persistence/
    │   ├── Configurations/
    │   │   └── UserPreferencesConfiguration.cs  # NEW: EF config
    │   └── Repositories/
    │       └── UserPreferencesRepository.cs     # NEW
    └── Services/
        └── UserPreferencesService.cs            # NEW: Implementation
```

---

## Setup Steps

### 1. Branch Setup

```bash
# Ensure you're on the feature branch
git checkout 003-ux-ui-enhancement

# Pull latest changes
git pull origin 003-ux-ui-enhancement

# If starting fresh, create from main
git checkout -b 003-ux-ui-enhancement main
```

### 2. Install Dependencies

```bash
# Navigate to Web project
cd src/TradingBot.Web

# Install npm packages (Tailwind CSS tooling)
npm install

# Restore .NET packages
dotnet restore
```

### 3. Tailwind CSS Configuration

The Web project already has Tailwind configured. You'll need to update the config for dark mode support:

**Update `src/TradingBot.Web/tailwind.config.js`**:

```javascript
/** @type {import('tailwindcss').Config} */
module.exports = {
  darkMode: 'class', // Enable class-based dark mode
  content: [
    './Components/**/*.{razor,html,cshtml}',
    './Pages/**/*.{razor,html,cshtml}'
  ],
  theme: {
    extend: {
      colors: {
        // Custom color palette using CSS variables
        background: 'rgb(var(--color-background) / <alpha-value>)',
        foreground: 'rgb(var(--color-foreground) / <alpha-value>)',
        primary: 'rgb(var(--color-primary) / <alpha-value>)',
        success: 'rgb(var(--color-success) / <alpha-value>)',
        warning: 'rgb(var(--color-warning) / <alpha-value>)',
        danger: 'rgb(var(--color-danger) / <alpha-value>)',
      },
      // Smooth transitions for theme switching
      transitionProperty: {
        'height': 'height',
        'spacing': 'margin, padding',
      }
    },
  },
  plugins: [],
}
```

**Update `src/TradingBot.Web/Styles/app.css`**:

```css
@tailwind base;
@tailwind components;
@tailwind utilities;

/* Theme CSS Variables */
@layer base {
  :root {
    /* Light theme (default) */
    --color-background: 255 255 255;
    --color-foreground: 0 0 0;
    --color-primary: 59 130 246;      /* blue-500 */
    --color-success: 34 197 94;       /* green-500 */
    --color-warning: 234 179 8;       /* yellow-500 */
    --color-danger: 239 68 68;        /* red-500 */
  }

  .dark {
    /* Dark theme */
    --color-background: 17 24 39;     /* gray-900 */
    --color-foreground: 243 244 246;  /* gray-100 */
    --color-primary: 96 165 250;      /* blue-400 */
    --color-success: 74 222 128;      /* green-400 */
    --color-warning: 250 204 21;      /* yellow-400 */
    --color-danger: 248 113 113;      /* red-400 */
  }

  /* Smooth theme transitions */
  * {
    @apply transition-colors duration-200;
  }
}

/* Custom component styles (if needed beyond Tailwind utilities) */
@layer components {
  .sidebar-transition {
    @apply transition-all duration-300 ease-in-out;
  }

  .toast-slide-in {
    animation: slideInRight 0.3s ease-out;
  }
}

@keyframes slideInRight {
  from {
    transform: translateX(100%);
    opacity: 0;
  }
  to {
    transform: translateX(0);
    opacity: 1;
  }
}
```

### 4. Build Tailwind CSS

```bash
# From src/TradingBot.Web directory
npm run css:build

# For development with watch mode (optional)
npm run css:watch
```

### 5. Database Migration

Create and apply the migration for UserPreferences:

```bash
# From repository root
cd /Users/phmatray/Repositories/github-phm/TradingBot

# Create migration
dotnet ef migrations add AddUserPreferences \
  --project src/TradingBot.Infrastructure \
  --startup-project src/TradingBot.Web

# Apply migration
dotnet ef database update \
  --project src/TradingBot.Infrastructure \
  --startup-project src/TradingBot.Web
```

**Expected Migration File** (auto-generated):
```csharp
public partial class AddUserPreferences : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "UserPreferences",
            columns: table => new
            {
                Id = table.Column<int>(nullable: false)
                    .Annotation("Sqlite:Autoincrement", true),
                UserId = table.Column<string>(maxLength: 100, nullable: false, defaultValue: "default"),
                Theme = table.Column<string>(maxLength: 20, nullable: false, defaultValue: "Light"),
                DashboardRefreshInterval = table.Column<int>(nullable: false, defaultValue: 5),
                NotificationDuration = table.Column<int>(nullable: false, defaultValue: 5),
                ShowSuccessNotifications = table.Column<bool>(nullable: false, defaultValue: true),
                ShowErrorNotifications = table.Column<bool>(nullable: false, defaultValue: true),
                ShowInfoNotifications = table.Column<bool>(nullable: false, defaultValue: true),
                ShowWarningNotifications = table.Column<bool>(nullable: false, defaultValue: true),
                CustomSettings = table.Column<string>(type: "TEXT", nullable: true),
                CreatedAt = table.Column<DateTime>(nullable: false),
                UpdatedAt = table.Column<DateTime>(nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_UserPreferences", x => x.Id);
            });

        migrationBuilder.CreateIndex(
            name: "IX_UserPreferences_UserId",
            table: "UserPreferences",
            column: "UserId",
            unique: true);

        // Seed default preferences
        migrationBuilder.InsertData(
            table: "UserPreferences",
            columns: new[] { "UserId", "Theme", "DashboardRefreshInterval", "NotificationDuration", "CreatedAt", "UpdatedAt" },
            values: new object[] { "default", "Light", 5, 5, DateTime.UtcNow, DateTime.UtcNow });
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "UserPreferences");
    }
}
```

### 6. Dependency Injection Setup

**Update `src/TradingBot.Web/Program.cs`**:

```csharp
// Add new services
builder.Services.AddScoped<IUserPreferencesService, UserPreferencesService>();
builder.Services.AddScoped<IUserPreferencesRepository, UserPreferencesRepository>();
builder.Services.AddScoped<UIStateService>();
builder.Services.AddScoped<ToastService>();
```

---

## Development Workflow

### Component Development Pattern

Follow the **Atomic Design** hierarchy when creating new components:

#### 1. Atoms (Basic Building Blocks)

Example: `Components/Atoms/Button.razor`

```razor
@* Simple, reusable button component *@
<button type="@Type"
        class="@GetClasses()"
        disabled="@IsDisabled"
        @onclick="OnClick">
    @ChildContent
</button>

@code {
    [Parameter] public RenderFragment? ChildContent { get; set; }
    [Parameter] public string Type { get; set; } = "button";
    [Parameter] public ButtonVariant Variant { get; set; } = ButtonVariant.Primary;
    [Parameter] public bool IsDisabled { get; set; }
    [Parameter] public EventCallback<MouseEventArgs> OnClick { get; set; }

    private string GetClasses() => $"btn btn-{Variant.ToString().ToLower()}";
}

public enum ButtonVariant { Primary, Secondary, Danger, Ghost }
```

#### 2. Molecules (Component Combinations)

Example: `Components/Molecules/FormField.razor`

```razor
@* Combines label, input, and error message *@
<div class="form-field">
    <label for="@InputId" class="block text-sm font-medium mb-1">
        @Label
    </label>
    <Input Id="@InputId"
           @bind-Value="Value"
           Type="@InputType"
           Placeholder="@Placeholder"
           Class="@InputClass" />
    @if (!string.IsNullOrEmpty(ErrorMessage))
    {
        <p class="text-red-600 text-sm mt-1">@ErrorMessage</p>
    }
</div>

@code {
    [Parameter] public string Label { get; set; } = "";
    [Parameter] public string Value { get; set; } = "";
    [Parameter] public EventCallback<string> ValueChanged { get; set; }
    [Parameter] public string InputType { get; set; } = "text";
    [Parameter] public string? Placeholder { get; set; }
    [Parameter] public string? ErrorMessage { get; set; }

    private string InputId => $"input-{Guid.NewGuid()}";
    private string InputClass => !string.IsNullOrEmpty(ErrorMessage) ? "border-red-500" : "";
}
```

#### 3. Organisms (Complex Components)

Example: `Components/Organisms/NavigationSidebar.razor`

```razor
@inject UIStateService UIState
@inject NavigationManager Navigation
@implements IDisposable

<aside class="@SidebarClasses">
    <div class="flex items-center justify-between p-4">
        @if (!UIState.SidebarCollapsed)
        {
            <h1 class="text-xl font-bold">TradingBot</h1>
        }
        <button @onclick="UIState.ToggleSidebar" class="p-2">
            <Icon Name="@ToggleIcon" Class="w-6 h-6" />
        </button>
    </div>

    <nav class="mt-4">
        @foreach (var item in MenuItems)
        {
            <MenuItem Href="@item.Href"
                      Icon="@item.Icon"
                      Label="@item.Label"
                      IsCollapsed="@UIState.SidebarCollapsed"
                      IsActive="@IsActive(item.Href)" />
        }
    </nav>
</aside>

@code {
    // Component implementation...
}
```

### Icon Setup

Download Heroicons SVG files or integrate via inline SVG in the Icon component:

**`Components/Atoms/Icon.razor`**:

```razor
@code {
    [Parameter] public IconName Name { get; set; }
    [Parameter] public string Class { get; set; } = "w-6 h-6";
}

<svg class="@Class" fill="none" viewBox="0 0 24 24" stroke-width="1.5" stroke="currentColor">
    @(GetIconPath(Name))
</svg>

@code {
    private MarkupString GetIconPath(IconName name) => name switch
    {
        IconName.Home => (MarkupString)@"<path stroke-linecap=""round"" stroke-linejoin=""round"" d=""M2.25 12l8.954-8.955c.44-.439 1.152-.439 1.591 0L21.75 12M4.5 9.75v10.125c0 .621.504 1.125 1.125 1.125H9.75v-4.875c0-.621.504-1.125 1.125-1.125h2.25c.621 0 1.125.504 1.125 1.125V21h4.125c.621 0 1.125-.504 1.125-1.125V9.75M8.25 21h8.25"" />",
        IconName.Cog => (MarkupString)@"<path stroke-linecap=""round"" stroke-linejoin=""round"" d=""M9.594 3.94c.09-.542.56-.94 1.11-.94h2.593c.55 0 1.02.398 1.11.94l.213 1.281c.063.374.313.686.645.87.074.04.147.083.22.127.324.196.72.257 1.075.124l1.217-.456a1.125 1.125 0 011.37.49l1.296 2.247a1.125 1.125 0 01-.26 1.431l-1.003.827c-.293.24-.438.613-.431.992a6.759 6.759 0 010 .255c-.007.378.138.75.43.99l1.005.828c.424.35.534.954.26 1.43l-1.298 2.247a1.125 1.125 0 01-1.369.491l-1.217-.456c-.355-.133-.75-.072-1.076.124a6.57 6.57 0 01-.22.128c-.331.183-.581.495-.644.869l-.213 1.28c-.09.543-.56.941-1.11.941h-2.594c-.55 0-1.02-.398-1.11-.94l-.213-1.281c-.062-.374-.312-.686-.644-.87a6.52 6.52 0 01-.22-.127c-.325-.196-.72-.257-1.076-.124l-1.217.456a1.125 1.125 0 01-1.369-.49l-1.297-2.247a1.125 1.125 0 01.26-1.431l1.004-.827c.292-.24.437-.613.43-.992a6.932 6.932 0 010-.255c.007-.378-.138-.75-.43-.99l-1.004-.828a1.125 1.125 0 01-.26-1.43l1.297-2.247a1.125 1.125 0 011.37-.491l1.216.456c.356.133.751.072 1.076-.124.072-.044.146-.087.22-.128.332-.183.582-.495.644-.869l.214-1.281z"" /><path stroke-linecap=""round"" stroke-linejoin=""round"" d=""M15 12a3 3 0 11-6 0 3 3 0 016 0z"" />",
        // Add more icons as needed...
        _ => (MarkupString)""
    };
}

public enum IconName
{
    Home,
    Cog,
    ChartBar,
    Briefcase,
    // ... more icons
}
```

### Testing Components

Use bUnit for Blazor component testing:

```csharp
// tests/TradingBot.Web.Tests/Components/Atoms/ButtonTests.cs
public class ButtonTests : TestContext
{
    [Fact]
    public void Button_RendersWithCorrectClasses()
    {
        // Arrange
        var cut = RenderComponent<Button>(parameters => parameters
            .Add(p => p.Variant, ButtonVariant.Primary)
            .Add(p => p.ChildContent, "Click Me"));

        // Act
        var button = cut.Find("button");

        // Assert
        button.ClassList.Should().Contain("btn-primary");
        button.TextContent.Should().Be("Click Me");
    }

    [Fact]
    public async Task Button_InvokesOnClick()
    {
        // Arrange
        var clicked = false;
        var cut = RenderComponent<Button>(parameters => parameters
            .Add(p => p.OnClick, EventCallback.Factory.Create<MouseEventArgs>(this, () => clicked = true))
            .Add(p => p.ChildContent, "Click Me"));

        // Act
        await cut.Find("button").ClickAsync(new MouseEventArgs());

        // Assert
        clicked.Should().BeTrue();
    }
}
```

---

## Running the Application

### Development Server

```bash
# From repository root
dotnet run --project src/TradingBot.Web

# Or with watch mode (auto-reload on changes)
dotnet watch --project src/TradingBot.Web
```

Navigate to: `http://localhost:5000`

### Build for Production

```bash
# Build Tailwind CSS for production (minified)
cd src/TradingBot.Web
npm run css:build -- --minify

# Build the .NET application
cd ../..
dotnet build --configuration Release
```

---

## Debugging Tips

### Blazor Server Debugging

1. **Browser DevTools**: Use F12 to inspect elements and check Tailwind classes
2. **Blazor DevTools**: Install Blazor WASM DevTools browser extension
3. **Hot Reload**: Use `dotnet watch` for instant feedback on changes
4. **SignalR Connection**: Monitor browser console for WebSocket errors

### Common Issues

**Issue**: Tailwind classes not applying
- **Solution**: Ensure `npm run css:build` has been run and `wwwroot/css/app.css` exists

**Issue**: Dark mode not working
- **Solution**: Check that `class="dark"` is applied to root element in `ThemeProvider.razor`

**Issue**: Database migration fails
- **Solution**: Ensure `TradingBotDbContext` has been updated with `DbSet<UserPreferences>`

**Issue**: Services not injected
- **Solution**: Verify services are registered in `Program.cs` with correct lifetime

---

## Code Quality

### Before Committing

```bash
# Format code
dotnet format

# Run analyzers
dotnet build /p:RunAnalyzers=true

# Run tests
dotnet test

# Check Tailwind build
npm run css:build
```

### StyleCop Compliance

All C# files must have copyright headers:

```csharp
// <copyright file="FileName.cs" company="TradingBot">
// Copyright (c) TradingBot. All rights reserved.
// </copyright>
```

Use the following script to add headers to new files:

```bash
# Add copyright headers to all .cs files without them
find src -name "*.cs" -exec sed -i '' '1i\
// <copyright file="$(basename {})" company="TradingBot">\
// Copyright (c) TradingBot. All rights reserved.\
// </copyright>\
' {} \;
```

---

## Documentation

### Component Documentation

Document each component with XML comments:

```csharp
/// <summary>
/// A reusable button component with variant support.
/// </summary>
/// <example>
/// <code>
/// &lt;Button Variant="ButtonVariant.Primary" OnClick="HandleClick"&gt;
///     Click Me
/// &lt;/Button&gt;
/// </code>
/// </example>
public class Button : ComponentBase
{
    /// <summary>
    /// Gets or sets the button variant (Primary, Secondary, Danger, Ghost).
    /// </summary>
    [Parameter]
    public ButtonVariant Variant { get; set; } = ButtonVariant.Primary;
}
```

---

## Next Steps

1. **Implement Core Entities**: Create `UserPreferences` entity and SmartEnum `Theme`
2. **Build Atomic Components**: Start with Button, Input, Icon atoms
3. **Create Services**: Implement `UIStateService` and `UserPreferencesService`
4. **Build Navigation**: Implement `NavigationSidebar` organism
5. **Settings Page**: Create settings form with theme toggle
6. **Test**: Write bUnit tests for all components

---

## Resources

- [Tailwind CSS Documentation](https://tailwindcss.com/docs)
- [Heroicons](https://heroicons.com/)
- [Blazor Documentation](https://learn.microsoft.com/en-us/aspnet/core/blazor)
- [bUnit Testing](https://bunit.dev/)
- [EF Core Migrations](https://learn.microsoft.com/en-us/ef/core/managing-schemas/migrations/)

---

**Ready to Start**: You now have everything needed to begin implementation. Follow the task list generated by `/speckit.tasks` for step-by-step execution.
