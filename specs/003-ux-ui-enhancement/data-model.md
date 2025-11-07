# Data Model: UX/UI Enhancement - Navigation & Settings

**Feature**: 003-ux-ui-enhancement | **Date**: 2025-11-07

## Overview

This document defines the data entities and their relationships for the UX/UI enhancement feature, primarily focused on user preferences storage and management.

## Entities

### UserPreferences

**Purpose**: Stores user-specific UI preferences and settings for the Blazor Server dashboard.

**Location**: `TradingBot.Core.Models.UserPreferences`

**Fields**:

| Field Name | Type | Constraints | Description |
|-----------|------|-------------|-------------|
| Id | int | PK, Identity | Unique identifier for the preference record |
| UserId | string | FK (AspNetUsers.Id), Required, Unique | Links preferences to a specific user account |
| Theme | string | Required, MaxLength(20), Default="light" | UI theme preference: "light" or "dark" |
| DashboardRefreshInterval | int | Required, Range(1-300), Default=10 | Dashboard refresh interval in seconds (1-300) |
| NotificationTypesEnabled | string (JSON) | Required | JSON object storing enabled notification types: `{"success": true, "error": true, "info": true, "warning": true}` |
| NotificationDuration | int | Required, Range(2-10), Default=5 | Toast notification display duration in seconds (2-10) |
| CreatedAt | DateTime | Required | Timestamp when preferences were created |
| UpdatedAt | DateTime | Required | Timestamp when preferences were last updated |

**Relationships**:
- Many-to-One with `AspNetUsers` (one user has one set of preferences)

**Validation Rules**:
- UserId must exist in AspNetUsers table
- Theme must be either "light" or "dark"
- DashboardRefreshInterval must be between 1 and 300 seconds
- NotificationDuration must be between 2 and 10 seconds
- NotificationTypesEnabled must be valid JSON with boolean values for: success, error, info, warning
- UpdatedAt must be >= CreatedAt

**State Transitions**: None (simple CRUD entity)

**Indexes**:
- Unique index on UserId (one preference record per user)
- Clustered index on Id (primary key)

**Default Values**:
```json
{
  "Theme": "light",
  "DashboardRefreshInterval": 10,
  "NotificationTypesEnabled": {
    "success": true,
    "error": true,
    "info": true,
    "warning": true
  },
  "NotificationDuration": 5
}
```

### NotificationTypesEnabled (Value Object)

**Purpose**: Represents which types of toast notifications are enabled for display.

**Location**: `TradingBot.Core.ValueObjects.NotificationTypesEnabled`

**Structure**:
```csharp
public record NotificationTypesEnabled
{
    public bool Success { get; init; } = true;
    public bool Error { get; init; } = true;
    public bool Info { get; init; } = true;
    public bool Warning { get; init; } = true;
}
```

**Validation**:
- No additional validation needed (boolean values are inherently valid)

**Serialization**: Stored as JSON string in UserPreferences.NotificationTypesEnabled column

## Entity Relationships

```text
AspNetUsers (Identity)
    ↓ 1:1
UserPreferences
```

**Cardinality**:
- One AspNetUser has exactly zero or one UserPreferences record
- One UserPreferences record belongs to exactly one AspNetUser

## Database Schema (SQLite)

### UserPreferences Table

```sql
CREATE TABLE UserPreferences (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    UserId TEXT NOT NULL UNIQUE,
    Theme TEXT NOT NULL DEFAULT 'light' CHECK(Theme IN ('light', 'dark')),
    DashboardRefreshInterval INTEGER NOT NULL DEFAULT 10 CHECK(DashboardRefreshInterval BETWEEN 1 AND 300),
    NotificationTypesEnabled TEXT NOT NULL DEFAULT '{"success":true,"error":true,"info":true,"warning":true}',
    NotificationDuration INTEGER NOT NULL DEFAULT 5 CHECK(NotificationDuration BETWEEN 2 AND 10),
    CreatedAt TEXT NOT NULL,
    UpdatedAt TEXT NOT NULL,
    FOREIGN KEY (UserId) REFERENCES AspNetUsers(Id) ON DELETE CASCADE
);

CREATE UNIQUE INDEX IX_UserPreferences_UserId ON UserPreferences(UserId);
```

## EF Core Configuration

### UserPreferences Entity Configuration

**Location**: `TradingBot.Infrastructure.Persistence.Configurations.UserPreferencesConfiguration`

```csharp
public class UserPreferencesConfiguration : IEntityTypeConfiguration<UserPreferences>
{
    public void Configure(EntityTypeBuilder<UserPreferences> builder)
    {
        builder.ToTable("UserPreferences");

        builder.HasKey(up => up.Id);

        builder.Property(up => up.UserId)
            .IsRequired()
            .HasMaxLength(450); // AspNetUsers.Id max length

        builder.HasIndex(up => up.UserId)
            .IsUnique();

        builder.Property(up => up.Theme)
            .IsRequired()
            .HasMaxLength(20)
            .HasDefaultValue("light");

        builder.Property(up => up.DashboardRefreshInterval)
            .IsRequired()
            .HasDefaultValue(10);

        builder.Property(up => up.NotificationTypesEnabled)
            .IsRequired()
            .HasColumnType("TEXT")
            .HasConversion(
                v => JsonSerializer.Serialize(v, (JsonSerializerOptions)null),
                v => JsonSerializer.Deserialize<NotificationTypesEnabled>(v, (JsonSerializerOptions)null))
            .HasDefaultValue(new NotificationTypesEnabled());

        builder.Property(up => up.NotificationDuration)
            .IsRequired()
            .HasDefaultValue(5);

        builder.Property(up => up.CreatedAt)
            .IsRequired();

        builder.Property(up => up.UpdatedAt)
            .IsRequired();

        // Relationship with AspNetUsers
        builder.HasOne<IdentityUser>()
            .WithOne()
            .HasForeignKey<UserPreferences>(up => up.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
```

## Data Access

### IUserPreferencesRepository Interface

**Location**: `TradingBot.Core.Interfaces.IUserPreferencesRepository`

```csharp
public interface IUserPreferencesRepository
{
    Task<UserPreferences?> GetByUserIdAsync(string userId, CancellationToken cancellationToken = default);
    Task<UserPreferences> CreateAsync(UserPreferences preferences, CancellationToken cancellationToken = default);
    Task<UserPreferences> UpdateAsync(UserPreferences preferences, CancellationToken cancellationToken = default);
    Task DeleteAsync(string userId, CancellationToken cancellationToken = default);
    Task<UserPreferences> GetOrCreateDefaultAsync(string userId, CancellationToken cancellationToken = default);
}
```

### UserPreferencesRepository Implementation

**Location**: `TradingBot.Infrastructure.Persistence.Repositories.UserPreferencesRepository`

**Key Operations**:

1. **GetByUserIdAsync**: Retrieves preferences for a specific user
   - Returns null if no preferences exist
   - Uses AsNoTracking for read-only operations

2. **CreateAsync**: Creates new preferences record
   - Sets CreatedAt and UpdatedAt to current UTC time
   - Returns created entity with Id populated

3. **UpdateAsync**: Updates existing preferences
   - Updates UpdatedAt to current UTC time
   - Uses entity tracking for change detection

4. **DeleteAsync**: Deletes preferences for a user
   - Cascade delete handled by database constraint

5. **GetOrCreateDefaultAsync**: Gets existing preferences or creates default
   - Convenience method for initialization
   - Creates with default values if not found

## Data Validation

### Domain-Level Validation

**Location**: `TradingBot.Core.Models.UserPreferences`

```csharp
public class UserPreferences
{
    // ... properties ...

    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(UserId))
            throw new ArgumentException("UserId is required", nameof(UserId));

        if (Theme != "light" && Theme != "dark")
            throw new ArgumentException("Theme must be 'light' or 'dark'", nameof(Theme));

        if (DashboardRefreshInterval < 1 || DashboardRefreshInterval > 300)
            throw new ArgumentOutOfRangeException(nameof(DashboardRefreshInterval),
                "Dashboard refresh interval must be between 1 and 300 seconds");

        if (NotificationDuration < 2 || NotificationDuration > 10)
            throw new ArgumentOutOfRangeException(nameof(NotificationDuration),
                "Notification duration must be between 2 and 10 seconds");

        if (UpdatedAt < CreatedAt)
            throw new InvalidOperationException("UpdatedAt cannot be earlier than CreatedAt");
    }
}
```

### Service-Level Validation

**Location**: `TradingBot.Web.Services.PreferencesService`

- Validates input before calling repository
- Returns validation errors to UI layer
- Ensures consistency between domain and database constraints

## Migration

### EF Core Migration Command

```bash
dotnet ef migrations add AddUserPreferences --project src/TradingBot.Infrastructure --startup-project src/TradingBot.Web
dotnet ef database update --project src/TradingBot.Infrastructure --startup-project src/TradingBot.Web
```

### Migration File (Sample)

```csharp
public partial class AddUserPreferences : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "UserPreferences",
            columns: table => new
            {
                Id = table.Column<int>(type: "INTEGER", nullable: false)
                    .Annotation("Sqlite:Autoincrement", true),
                UserId = table.Column<string>(type: "TEXT", maxLength: 450, nullable: false),
                Theme = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false, defaultValue: "light"),
                DashboardRefreshInterval = table.Column<int>(type: "INTEGER", nullable: false, defaultValue: 10),
                NotificationTypesEnabled = table.Column<string>(type: "TEXT", nullable: false,
                    defaultValue: "{\"success\":true,\"error\":true,\"info\":true,\"warning\":true}"),
                NotificationDuration = table.Column<int>(type: "INTEGER", nullable: false, defaultValue: 5),
                CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_UserPreferences", x => x.Id);
                table.ForeignKey(
                    name: "FK_UserPreferences_AspNetUsers_UserId",
                    column: x => x.UserId,
                    principalTable: "AspNetUsers",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
                table.CheckConstraint("CK_UserPreferences_Theme", "Theme IN ('light', 'dark')");
                table.CheckConstraint("CK_UserPreferences_DashboardRefreshInterval",
                    "DashboardRefreshInterval BETWEEN 1 AND 300");
                table.CheckConstraint("CK_UserPreferences_NotificationDuration",
                    "NotificationDuration BETWEEN 2 AND 10");
            });

        migrationBuilder.CreateIndex(
            name: "IX_UserPreferences_UserId",
            table: "UserPreferences",
            column: "UserId",
            unique: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "UserPreferences");
    }
}
```

## Data Seeding (Optional)

### Default Preferences for Existing Users

**Location**: `TradingBot.Infrastructure.Persistence.TradingBotDbContextSeed`

```csharp
public static class TradingBotDbContextSeed
{
    public static async Task SeedDefaultPreferencesAsync(TradingBotDbContext context)
    {
        // Get all users without preferences
        var usersWithoutPreferences = await context.Users
            .Where(u => !context.UserPreferences.Any(up => up.UserId == u.Id))
            .ToListAsync();

        foreach (var user in usersWithoutPreferences)
        {
            var preferences = new UserPreferences
            {
                UserId = user.Id,
                Theme = "light",
                DashboardRefreshInterval = 10,
                NotificationTypesEnabled = new NotificationTypesEnabled(),
                NotificationDuration = 5,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            context.UserPreferences.Add(preferences);
        }

        await context.SaveChangesAsync();
    }
}
```

**Execution**: Call during app startup in Program.cs for development/staging environments only

## Data Volume Estimates

- **UserPreferences**: One record per user
- **Average Record Size**: ~200 bytes (including JSON serialization)
- **Growth Rate**: Linear with user base growth
- **Expected Volume**: 1-10,000 records (small dataset)

## Performance Considerations

- **Indexing**: Unique index on UserId ensures fast lookups (O(1) time complexity)
- **Caching**: Preferences loaded once per session and cached in memory (IMemoryCache)
- **Writes**: Debounced save operations prevent excessive database writes
- **JSON Column**: NotificationTypesEnabled stored as JSON for flexibility (minimal overhead for small objects)

## Data Privacy & Security

- **PII**: User preferences contain no personally identifiable information (PII)
- **Encryption**: No encryption needed (non-sensitive UI preferences)
- **Access Control**: Users can only read/write their own preferences (enforced by UserId filter)
- **Audit Trail**: CreatedAt and UpdatedAt provide basic audit information
- **GDPR Compliance**: Cascade delete ensures preferences removed when user account deleted

## Future Extensions (Out of Scope)

- Custom theme colors (user-defined color schemes)
- Layout preferences (widget positions, column widths)
- Keyboard shortcut customization
- Notification sound preferences
- Advanced accessibility settings (font size, contrast adjustments)
