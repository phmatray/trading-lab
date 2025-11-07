# Data Model: UX/UI Enhancement

**Feature**: 003-ux-ui-enhancement
**Date**: 2025-11-07

## Overview

This document defines the data entities, their relationships, and validation rules for the UX/UI Enhancement feature. The primary focus is on user preferences storage and UI state management.

---

## Entity Diagram

```
┌─────────────────────────────┐
│    UserPreferences          │
├─────────────────────────────┤
│ PK  Id: int                 │
│     UserId: string          │ ← "default" (single user, future-proof)
│     Theme: Theme            │ ← SmartEnum (Light, Dark)
│     DashboardRefreshInterval│ ← 1-300 seconds
│     NotificationDuration    │ ← 2-10 seconds
│     ShowSuccessNotifs: bool │
│     ShowErrorNotifs: bool   │
│     ShowInfoNotifs: bool    │
│     ShowWarningNotifs: bool │
│     CustomSettings: string? │ ← JSON for future extensions
│     CreatedAt: DateTime     │
│     UpdatedAt: DateTime     │
└─────────────────────────────┘
           │
           │ (future: FK to User)
           ▼
     [No User table yet]
     (Single user = "default")
```

---

## Entities

### 1. UserPreferences

**Purpose**: Store user-specific settings for UI customization and application behavior.

**Table**: `UserPreferences`

**Columns**:

| Column | Type | Constraints | Default | Description |
|--------|------|-------------|---------|-------------|
| `Id` | INTEGER | PK, AUTOINCREMENT | - | Unique identifier |
| `UserId` | TEXT | NOT NULL, UNIQUE | `'default'` | User identifier (future-proof for multi-user) |
| `Theme` | TEXT | NOT NULL, CHECK IN ('Light', 'Dark') | `'Light'` | UI theme preference |
| `DashboardRefreshInterval` | INTEGER | NOT NULL, CHECK (1-300) | `5` | Dashboard auto-refresh interval in seconds |
| `NotificationDuration` | INTEGER | NOT NULL, CHECK (2-10) | `5` | Toast notification display duration in seconds |
| `ShowSuccessNotifications` | INTEGER | NOT NULL | `1` | Show success toasts (SQLite boolean as int) |
| `ShowErrorNotifications` | INTEGER | NOT NULL | `1` | Show error toasts |
| `ShowInfoNotifications` | INTEGER | NOT NULL | `1` | Show info toasts |
| `ShowWarningNotifications` | INTEGER | NOT NULL | `1` | Show warning toasts |
| `CustomSettings` | TEXT | NULL | `NULL` | JSON blob for extensible settings |
| `CreatedAt` | TEXT | NOT NULL | `CURRENT_TIMESTAMP` | Record creation timestamp (ISO 8601) |
| `UpdatedAt` | TEXT | NOT NULL | `CURRENT_TIMESTAMP` | Last update timestamp (ISO 8601) |

**Indexes**:
- `UNIQUE INDEX IX_UserPreferences_UserId ON UserPreferences(UserId)`

**Seed Data**:
```sql
INSERT INTO UserPreferences (UserId, Theme, DashboardRefreshInterval, NotificationDuration)
VALUES ('default', 'Light', 5, 5);
```

**C# Entity Model**:

```csharp
// <copyright file="UserPreferences.cs" company="TradingBot">
// Copyright (c) TradingBot. All rights reserved.
// </copyright>

namespace TradingBot.Core.Entities;

/// <summary>
/// Represents user-specific preferences for UI customization.
/// </summary>
public class UserPreferences
{
    /// <summary>
    /// Gets or sets the unique identifier.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Gets or sets the user identifier.
    /// For single-user systems, this defaults to "default".
    /// </summary>
    public string UserId { get; set; } = "default";

    /// <summary>
    /// Gets or sets the UI theme preference (Light or Dark).
    /// </summary>
    public Theme Theme { get; set; } = Theme.Light;

    /// <summary>
    /// Gets or sets the dashboard refresh interval in seconds (1-300).
    /// </summary>
    public int DashboardRefreshInterval { get; set; } = 5;

    /// <summary>
    /// Gets or sets the notification display duration in seconds (2-10).
    /// </summary>
    public int NotificationDuration { get; set; } = 5;

    /// <summary>
    /// Gets or sets a value indicating whether success notifications are displayed.
    /// </summary>
    public bool ShowSuccessNotifications { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether error notifications are displayed.
    /// </summary>
    public bool ShowErrorNotifications { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether info notifications are displayed.
    /// </summary>
    public bool ShowInfoNotifications { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether warning notifications are displayed.
    /// </summary>
    public bool ShowWarningNotifications { get; set; } = true;

    /// <summary>
    /// Gets or sets custom settings as JSON for future extensibility.
    /// </summary>
    public string? CustomSettings { get; set; }

    /// <summary>
    /// Gets or sets the record creation timestamp.
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Gets or sets the last update timestamp.
    /// </summary>
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
```

**EF Core Configuration**:

```csharp
// <copyright file="UserPreferencesConfiguration.cs" company="TradingBot">
// Copyright (c) TradingBot. All rights reserved.
// </copyright>

namespace TradingBot.Infrastructure.Persistence.Configurations;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TradingBot.Core.Entities;
using TradingBot.Core.ValueObjects;

/// <summary>
/// EF Core configuration for UserPreferences entity.
/// </summary>
public class UserPreferencesConfiguration : IEntityTypeConfiguration<UserPreferences>
{
    /// <inheritdoc/>
    public void Configure(EntityTypeBuilder<UserPreferences> builder)
    {
        builder.ToTable("UserPreferences");

        builder.HasKey(p => p.Id);

        builder.HasIndex(p => p.UserId)
            .IsUnique();

        builder.Property(p => p.UserId)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(p => p.Theme)
            .HasConversion(
                v => v.Name,
                v => Theme.FromName(v, ignoreCase: false))
            .IsRequired()
            .HasMaxLength(20);

        builder.Property(p => p.DashboardRefreshInterval)
            .IsRequired();

        builder.Property(p => p.NotificationDuration)
            .IsRequired();

        builder.Property(p => p.ShowSuccessNotifications)
            .IsRequired();

        builder.Property(p => p.ShowErrorNotifications)
            .IsRequired();

        builder.Property(p => p.ShowInfoNotifications)
            .IsRequired();

        builder.Property(p => p.ShowWarningNotifications)
            .IsRequired();

        builder.Property(p => p.CustomSettings)
            .HasColumnType("TEXT");

        builder.Property(p => p.CreatedAt)
            .IsRequired();

        builder.Property(p => p.UpdatedAt)
            .IsRequired();
    }
}
```

---

### 2. Theme (SmartEnum)

**Purpose**: Type-safe enumeration for theme values.

**File**: `TradingBot.Core/ValueObjects/Theme.cs`

```csharp
// <copyright file="Theme.cs" company="TradingBot">
// Copyright (c) TradingBot. All rights reserved.
// </copyright>

namespace TradingBot.Core.ValueObjects;

using Ardalis.SmartEnum;

/// <summary>
/// Represents the UI theme options.
/// </summary>
public sealed class Theme : SmartEnum<Theme>
{
    /// <summary>
    /// Light theme.
    /// </summary>
    public static readonly Theme Light = new(0, nameof(Light));

    /// <summary>
    /// Dark theme.
    /// </summary>
    public static readonly Theme Dark = new(1, nameof(Dark));

    private Theme(int value, string name)
        : base(value, name)
    {
    }
}
```

---

## Client-Side State Models

These models exist only in the Blazor application and are NOT persisted to the database.

### 3. UIState (Client Service)

**Purpose**: Manage transient UI state (sidebar collapsed, modals open, etc.)

**File**: `TradingBot.Web/Services/UIStateService.cs`

```csharp
// <copyright file="UIStateService.cs" company="TradingBot">
// Copyright (c) TradingBot. All rights reserved.
// </copyright>

namespace TradingBot.Web.Services;

/// <summary>
/// Manages transient UI state for the application.
/// State does NOT persist across sessions.
/// </summary>
public class UIStateService
{
    private bool _sidebarCollapsed = false;

    /// <summary>
    /// Occurs when UI state changes.
    /// </summary>
    public event Action? OnStateChanged;

    /// <summary>
    /// Gets or sets a value indicating whether the sidebar is collapsed.
    /// Note: This state does NOT persist across sessions per FR-003a.
    /// </summary>
    public bool SidebarCollapsed
    {
        get => _sidebarCollapsed;
        set
        {
            if (_sidebarCollapsed == value)
            {
                return;
            }

            _sidebarCollapsed = value;
            NotifyStateChanged();
        }
    }

    /// <summary>
    /// Toggles the sidebar collapsed state.
    /// </summary>
    public void ToggleSidebar()
    {
        SidebarCollapsed = !SidebarCollapsed;
    }

    private void NotifyStateChanged()
    {
        OnStateChanged?.Invoke();
    }
}
```

### 4. Toast Notification Models

**Purpose**: Manage toast notification display and lifecycle.

**File**: `TradingBot.Web/Models/ToastNotification.cs`

```csharp
// <copyright file="ToastNotification.cs" company="TradingBot">
// Copyright (c) TradingBot. All rights reserved.
// </copyright>

namespace TradingBot.Web.Models;

/// <summary>
/// Represents a toast notification.
/// </summary>
public class ToastNotification
{
    /// <summary>
    /// Gets or sets the unique identifier.
    /// </summary>
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// Gets or sets the notification type.
    /// </summary>
    public ToastType Type { get; set; }

    /// <summary>
    /// Gets or sets the message to display.
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the duration in seconds to display the toast.
    /// </summary>
    public int DurationSeconds { get; set; } = 5;

    /// <summary>
    /// Gets or sets the timestamp when the toast was created.
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Toast notification types.
/// </summary>
public enum ToastType
{
    /// <summary>
    /// Success notification (green).
    /// </summary>
    Success,

    /// <summary>
    /// Error notification (red).
    /// </summary>
    Error,

    /// <summary>
    /// Warning notification (yellow).
    /// </summary>
    Warning,

    /// <summary>
    /// Information notification (blue).
    /// </summary>
    Info,
}
```

**File**: `TradingBot.Web/Services/ToastService.cs`

```csharp
// <copyright file="ToastService.cs" company="TradingBot">
// Copyright (c) TradingBot. All rights reserved.
// </copyright>

namespace TradingBot.Web.Services;

using TradingBot.Web.Models;

/// <summary>
/// Service for managing toast notifications.
/// </summary>
public class ToastService
{
    private readonly List<ToastNotification> _toasts = new();
    private readonly IUserPreferencesService _preferencesService;

    /// <summary>
    /// Initializes a new instance of the <see cref="ToastService"/> class.
    /// </summary>
    /// <param name="preferencesService">User preferences service.</param>
    public ToastService(IUserPreferencesService preferencesService)
    {
        _preferencesService = preferencesService;
    }

    /// <summary>
    /// Occurs when toasts change.
    /// </summary>
    public event Action? OnToastsChanged;

    /// <summary>
    /// Gets the current list of toasts.
    /// </summary>
    public IReadOnlyList<ToastNotification> Toasts => _toasts.AsReadOnly();

    /// <summary>
    /// Shows a toast notification if the type is enabled in user preferences.
    /// </summary>
    public async Task ShowAsync(ToastType type, string message)
    {
        var preferences = await _preferencesService.GetPreferencesAsync();

        var shouldShow = type switch
        {
            ToastType.Success => preferences.ShowSuccessNotifications,
            ToastType.Error => preferences.ShowErrorNotifications,
            ToastType.Warning => preferences.ShowWarningNotifications,
            ToastType.Info => preferences.ShowInfoNotifications,
            _ => true
        };

        if (!shouldShow)
        {
            return;
        }

        var toast = new ToastNotification
        {
            Type = type,
            Message = message,
            DurationSeconds = preferences.NotificationDuration
        };

        _toasts.Add(toast);
        NotifyStateChanged();

        // Auto-dismiss after duration
        _ = Task.Delay(TimeSpan.FromSeconds(toast.DurationSeconds))
            .ContinueWith(_ => Dismiss(toast.Id));
    }

    /// <summary>
    /// Dismisses a toast by ID.
    /// </summary>
    public void Dismiss(Guid id)
    {
        var toast = _toasts.FirstOrDefault(t => t.Id == id);
        if (toast != null)
        {
            _toasts.Remove(toast);
            NotifyStateChanged();
        }
    }

    /// <summary>
    /// Dismisses all toasts.
    /// </summary>
    public void DismissAll()
    {
        _toasts.Clear();
        NotifyStateChanged();
    }

    private void NotifyStateChanged()
    {
        OnToastsChanged?.Invoke();
    }
}
```

---

## Validation Rules

### UserPreferences Validation

**Validation Class**: `TradingBot.Core/Validators/UserPreferencesValidator.cs`

```csharp
// <copyright file="UserPreferencesValidator.cs" company="TradingBot">
// Copyright (c) TradingBot. All rights reserved.
// </copyright>

namespace TradingBot.Core.Validators;

using TradingBot.Core.Entities;

/// <summary>
/// Validator for UserPreferences entity.
/// </summary>
public class UserPreferencesValidator
{
    /// <summary>
    /// Validates a UserPreferences instance.
    /// </summary>
    /// <param name="preferences">The preferences to validate.</param>
    /// <returns>A validation result.</returns>
    public ValidationResult Validate(UserPreferences preferences)
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(preferences.UserId))
        {
            errors.Add("User ID is required.");
        }

        if (preferences.DashboardRefreshInterval < 1 || preferences.DashboardRefreshInterval > 300)
        {
            errors.Add("Dashboard refresh interval must be between 1 and 300 seconds.");
        }

        if (preferences.NotificationDuration < 2 || preferences.NotificationDuration > 10)
        {
            errors.Add("Notification duration must be between 2 and 10 seconds.");
        }

        if (preferences.Theme == null)
        {
            errors.Add("Theme is required.");
        }

        return errors.Any()
            ? ValidationResult.Error(errors)
            : ValidationResult.Success();
    }
}

/// <summary>
/// Represents a validation result.
/// </summary>
public class ValidationResult
{
    private ValidationResult(bool isSuccess, IEnumerable<string> errors)
    {
        IsSuccess = isSuccess;
        Errors = errors.ToList();
    }

    /// <summary>
    /// Gets a value indicating whether validation succeeded.
    /// </summary>
    public bool IsSuccess { get; }

    /// <summary>
    /// Gets the list of validation errors.
    /// </summary>
    public IReadOnlyList<string> Errors { get; }

    /// <summary>
    /// Creates a successful validation result.
    /// </summary>
    public static ValidationResult Success() => new(true, []);

    /// <summary>
    /// Creates a failed validation result.
    /// </summary>
    public static ValidationResult Error(IEnumerable<string> errors) => new(false, errors);
}
```

---

## Repository Interface

**File**: `TradingBot.Core/Interfaces/IUserPreferencesRepository.cs`

```csharp
// <copyright file="IUserPreferencesRepository.cs" company="TradingBot">
// Copyright (c) TradingBot. All rights reserved.
// </copyright>

namespace TradingBot.Core.Interfaces;

using TradingBot.Core.Entities;

/// <summary>
/// Repository interface for UserPreferences.
/// </summary>
public interface IUserPreferencesRepository
{
    /// <summary>
    /// Gets user preferences by user ID.
    /// </summary>
    /// <param name="userId">The user ID (default: "default").</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The user preferences, or default preferences if not found.</returns>
    Task<UserPreferences> GetByUserIdAsync(
        string userId = "default",
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Saves or updates user preferences.
    /// </summary>
    /// <param name="preferences">The preferences to save.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the async operation.</returns>
    Task SaveAsync(UserPreferences preferences, CancellationToken cancellationToken = default);

    /// <summary>
    /// Resets user preferences to default values.
    /// </summary>
    /// <param name="userId">The user ID (default: "default").</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The reset preferences.</returns>
    Task<UserPreferences> ResetToDefaultAsync(
        string userId = "default",
        CancellationToken cancellationToken = default);
}
```

---

## Service Interface

**File**: `TradingBot.Core/Interfaces/IUserPreferencesService.cs`

```csharp
// <copyright file="IUserPreferencesService.cs" company="TradingBot">
// Copyright (c) TradingBot. All rights reserved.
// </copyright>

namespace TradingBot.Core.Interfaces;

using TradingBot.Core.Entities;
using TradingBot.Core.Validators;

/// <summary>
/// Service interface for managing user preferences.
/// </summary>
public interface IUserPreferencesService
{
    /// <summary>
    /// Gets the current user's preferences.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The user preferences.</returns>
    Task<UserPreferences> GetPreferencesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates the current user's preferences.
    /// </summary>
    /// <param name="preferences">The updated preferences.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Validation result indicating success or errors.</returns>
    Task<ValidationResult> UpdatePreferencesAsync(
        UserPreferences preferences,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Resets preferences to default values.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The reset preferences.</returns>
    Task<UserPreferences> ResetToDefaultAsync(CancellationToken cancellationToken = default);
}
```

---

## Database Migration

**Migration Name**: `AddUserPreferences`

**Up Migration**:
```sql
CREATE TABLE UserPreferences (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    UserId TEXT NOT NULL DEFAULT 'default',
    Theme TEXT NOT NULL CHECK(Theme IN ('Light', 'Dark')) DEFAULT 'Light',
    DashboardRefreshInterval INTEGER NOT NULL DEFAULT 5 CHECK(DashboardRefreshInterval >= 1 AND DashboardRefreshInterval <= 300),
    NotificationDuration INTEGER NOT NULL DEFAULT 5 CHECK(NotificationDuration >= 2 AND NotificationDuration <= 10),
    ShowSuccessNotifications INTEGER NOT NULL DEFAULT 1,
    ShowErrorNotifications INTEGER NOT NULL DEFAULT 1,
    ShowInfoNotifications INTEGER NOT NULL DEFAULT 1,
    ShowWarningNotifications INTEGER NOT NULL DEFAULT 1,
    CustomSettings TEXT,
    CreatedAt TEXT NOT NULL DEFAULT (datetime('now')),
    UpdatedAt TEXT NOT NULL DEFAULT (datetime('now'))
);

CREATE UNIQUE INDEX IX_UserPreferences_UserId ON UserPreferences(UserId);

-- Seed default preferences for single user
INSERT INTO UserPreferences (UserId, Theme, DashboardRefreshInterval, NotificationDuration)
VALUES ('default', 'Light', 5, 5);
```

**Down Migration**:
```sql
DROP TABLE IF EXISTS UserPreferences;
```

---

## Summary

### New Entities
1. **UserPreferences**: Database-persisted user settings
2. **Theme**: SmartEnum for theme values (Light, Dark)

### Client-Side Models (Not Persisted)
1. **UIStateService**: Transient UI state (sidebar collapsed, etc.)
2. **ToastNotification**: Toast message model
3. **ToastService**: Toast notification manager

### Validation
- `UserPreferencesValidator`: Validates settings before save
- Range checks: DashboardRefreshInterval (1-300), NotificationDuration (2-10)

### Repository/Service Interfaces
- `IUserPreferencesRepository`: Data access layer
- `IUserPreferencesService`: Business logic layer

---

**Next Phase**: Generate API contracts (if needed) and quickstart guide.