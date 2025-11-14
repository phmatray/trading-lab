# Quickstart Guide: Blazor Server Trading Dashboard

**Feature**: 002-blazor-server-app
**Date**: 2025-11-07
**Audience**: Developers implementing the Blazor Server web application

## Overview

This quickstart guide provides step-by-step instructions for setting up and running the TradingBot Blazor Server application for local development. Follow these steps to get the web dashboard running alongside the existing CLI application.

---

## Prerequisites

Before starting, ensure you have the following installed:

- **.NET 9 SDK**: [Download from Microsoft](https://dotnet.microsoft.com/download/dotnet/9.0)
- **Node.js 18+**: Required for Tailwind CSS compilation ([Download](https://nodejs.org/))
- **Git**: For version control
- **Visual Studio Code** or **Visual Studio 2022**: Recommended IDEs
- **SQLite**: Database file shared with CLI (created automatically)

**Verify installations**:
```bash
dotnet --version  # Should show 9.0.x
node --version    # Should show v18.x or higher
npm --version     # Should show 9.x or higher
```

---

## Initial Setup

### Step 1: Switch to Feature Branch

```bash
cd /path/to/TradingBot
git checkout 002-blazor-server-app
```

### Step 2: Create Blazor Server Project

```bash
# Create new Blazor Server project
dotnet new blazorserver -n TradingBot.Web -o src/TradingBot.Web

# Add project to solution
dotnet sln add src/TradingBot.Web/TradingBot.Web.csproj
```

### Step 3: Add Project References

```bash
cd src/TradingBot.Web

# Reference all existing projects
dotnet add reference ../TradingBot.Core/TradingBot.Core.csproj
dotnet add reference ../TradingBot.Infrastructure/TradingBot.Infrastructure.csproj
dotnet add reference ../TradingBot.Engine/TradingBot.Engine.csproj
dotnet add reference ../TradingBot.Analytics/TradingBot.Analytics.csproj
dotnet add reference ../TradingBot.Strategies/TradingBot.Strategies.csproj
```

### Step 4: Install NuGet Packages

```bash
# Charting library
dotnet add package Blazor-ApexCharts --version 6.0.2

# SignalR MessagePack protocol (optional, for performance)
dotnet add package Microsoft.AspNetCore.SignalR.Protocols.MessagePack --version 9.0.0
```

### Step 5: Set Up Tailwind CSS

```bash
# Initialize npm project
npm init -y

# Install Tailwind CSS
npm install -D tailwindcss

# Initialize Tailwind configuration
npx tailwindcss init
```

**Configure Tailwind (tailwind.config.js)**:
```javascript
/** @type {import('tailwindcss').Config} */
module.exports = {
  content: [
    './Components/**/*.{razor,html}',
    './Pages/**/*.{razor,html}',
    './**/*.razor'
  ],
  theme: {
    extend: {
      colors: {
        'trading-profit': '#10b981',
        'trading-loss': '#ef4444',
        'trading-warning': '#f59e0b',
      }
    },
  },
  plugins: [],
}
```

**Create Tailwind Input File (Styles/app.css)**:
```bash
mkdir Styles
```

Create `Styles/app.css`:
```css
@tailwind base;
@tailwind components;
@tailwind utilities;

@layer components {
  .card-trading {
    @apply bg-base-200 shadow-lg rounded-lg p-6;
  }

  .stat-positive {
    @apply text-trading-profit font-semibold;
  }

  .stat-negative {
    @apply text-trading-loss font-semibold;
  }
}
```

**Update package.json with build scripts**:
```json
{
  "scripts": {
    "css:watch": "tailwindcss -i ./Styles/app.css -o ./wwwroot/css/app.css --watch",
    "css:build": "tailwindcss -i ./Styles/app.css -o ./wwwroot/css/app.css",
    "css:prod": "tailwindcss -i ./Styles/app.css -o ./wwwroot/css/app.css --minify"
  },
  "devDependencies": {
    "tailwindcss": "^3.4.0"
  }
}
```

**Update TradingBot.Web.csproj**:

Add these targets for automatic Tailwind compilation:
```xml
<!-- Tailwind CSS Build -->
<Target Name="BuildTailwindCSS" BeforeTargets="BeforeBuild">
  <Exec Command="npm run css:build" />
</Target>

<Target Name="BuildTailwindCSSProduction" BeforeTargets="BeforeBuild" Condition="'$(Configuration)' == 'Release'">
  <Exec Command="npm run css:prod" />
</Target>
```

### Step 6: Create Test Project

```bash
cd ../..

# Create xUnit test project
dotnet new xunit -n TradingBot.Web.Tests -o tests/TradingBot.Web.Tests

# Add to solution
dotnet sln add tests/TradingBot.Web.Tests/TradingBot.Web.Tests.csproj

# Add project reference
cd tests/TradingBot.Web.Tests
dotnet add reference ../../src/TradingBot.Web/TradingBot.Web.csproj
dotnet add reference ../../src/TradingBot.Core/TradingBot.Core.csproj

# Install bUnit and testing packages
dotnet add package bUnit --version 1.28.9
dotnet add package FakeItEasy --version 8.3.0
dotnet add package Shouldly --version 4.2.1
```

---

## Project Structure Setup

### Create Directory Structure

```bash
cd ../../src/TradingBot.Web

# Create component directories
mkdir -p Components/Layout
mkdir -p Components/Dashboard
mkdir -p Components/Portfolio
mkdir -p Components/Performance
mkdir -p Components/Strategy
mkdir -p Components/Risk
mkdir -p Components/Shared
mkdir -p Components/Charts

# Create pages directory
mkdir -p Pages

# Create services directory
mkdir -p Services

# Create hubs directory
mkdir -p Hubs

# Create wwwroot structure
mkdir -p wwwroot/css
mkdir -p wwwroot/js
```

---

## Configuration

### Update appsettings.json

Replace the default `appsettings.json` content:
```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning",
      "Microsoft.AspNetCore.SignalR": "Information"
    }
  },
  "AllowedHosts": "*",
  "ConnectionStrings": {
    "DefaultConnection": "Data Source=../TradingBot.Cli/tradingbot.db"
  },
  "TradingBot": {
    "InitialCapital": 100000,
    "DefaultLeverage": 1.0,
    "MaxPositionSize": 10.0,
    "StopLossPercent": 2.0,
    "TakeProfitPercent": 5.0
  },
  "SignalR": {
    "ClientTimeoutInterval": 60,
    "KeepAliveInterval": 15,
    "MaximumReceiveMessageSize": 1048576
  }
}
```

### Update Program.cs

Replace the default `Program.cs`:
```csharp
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using TradingBot.Infrastructure;
using TradingBot.Web.Hubs;
using TradingBot.Web.Services;

var builder = WebApplication.CreateBuilder(args);

// Add Blazor services
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// Add TradingBot services from Infrastructure project
builder.Services.AddTradingBotServices(builder.Configuration);

// Add ApexCharts
builder.Services.AddApexCharts();

// Add SignalR with performance tuning
builder.Services.AddSignalR(options =>
{
    options.ClientTimeoutInterval = TimeSpan.FromSeconds(
        builder.Configuration.GetValue<int>("SignalR:ClientTimeoutInterval", 60));
    options.KeepAliveInterval = TimeSpan.FromSeconds(
        builder.Configuration.GetValue<int>("SignalR:KeepAliveInterval", 15));
    options.MaximumReceiveMessageSize = builder.Configuration.GetValue<int>(
        "SignalR:MaximumReceiveMessageSize", 1024 * 1024);
});

// Add application services
builder.Services.AddScoped<IDashboardService, DashboardService>();
builder.Services.AddScoped<IPortfolioService, PortfolioService>();
builder.Services.AddScoped<IPerformanceService, PerformanceService>();
builder.Services.AddScoped<IStrategyManagementService, StrategyManagementService>();
builder.Services.AddScoped<IRiskSettingsService, RiskSettingsService>();
builder.Services.AddScoped<IBacktestService, BacktestService>();

// Add real-time update background service
builder.Services.AddHostedService<RealtimeUpdateService>();

var app = builder.Build();

// Configure middleware pipeline
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseAntiforgery();

// Map Blazor components
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

// Map SignalR hub
app.MapHub<TradingHub>("/tradinghub");

app.Run();
```

---

## Running the Application

### Option 1: Development with Hot Reload

**Terminal 1 - Blazor App**:
```bash
cd src/TradingBot.Web
dotnet watch run
```

**Terminal 2 - Tailwind CSS Watch**:
```bash
cd src/TradingBot.Web
npm run css:watch
```

Navigate to: `https://localhost:5001` (or the port shown in Terminal 1)

### Option 2: Single Terminal with Build

```bash
cd src/TradingBot.Web

# Build Tailwind CSS
npm run css:build

# Run app
dotnet run
```

### Option 3: Production Build

```bash
dotnet build -c Release
dotnet run -c Release --project src/TradingBot.Web
```

---

## Verifying the Setup

### 1. Check Database Connection

After starting the app, verify the SQLite database exists:
```bash
ls -la src/TradingBot.Cli/tradingbot.db
```

If the database doesn't exist, run the CLI app once to create it:
```bash
dotnet run --project src/TradingBot.Cli -- config show
```

### 2. Check Tailwind CSS

Verify that `wwwroot/css/app.css` was generated:
```bash
ls -la src/TradingBot.Web/wwwroot/css/app.css
```

The file should be >100KB (contains all Tailwind utilities).

### 3. Check SignalR Connection

Open browser DevTools → Console, you should see:
```
SignalR connected: <connection-id>
```

### 4. Run Tests

```bash
dotnet test tests/TradingBot.Web.Tests
```

---

## Troubleshooting

### Issue: Tailwind CSS not generating

**Solution**:
```bash
# Manually run Tailwind build
cd src/TradingBot.Web
npx tailwindcss -i ./Styles/app.css -o ./wwwroot/css/app.css
```

Check that `tailwind.config.js` content paths include all `.razor` files.

### Issue: Database connection error

**Error**: `SQLite Error: unable to open database file`

**Solution**:
- Ensure the path in `appsettings.json` is correct relative to the Web project
- Run the CLI app once to initialize the database
- Check file permissions on `tradingbot.db`

### Issue: SignalR connection failing

**Error**: `Failed to start the connection: Error: Failed to complete negotiation`

**Solution**:
- Check that `/tradinghub` is mapped in `Program.cs`
- Verify firewall allows WebSocket connections
- Check browser console for CORS errors
- Ensure app is running on HTTPS

### Issue: NuGet restore errors

**Solution**:
```bash
# Clear NuGet cache
dotnet nuget locals all --clear

# Restore packages
dotnet restore
```

### Issue: Hot reload not working

**Solution**:
```bash
# Use dotnet watch instead of dotnet run
dotnet watch run
```

Ensure you're using .NET 9 SDK (check with `dotnet --version`).

---

## Next Steps

After completing the quickstart:

1. **Implement Basic Dashboard**:
   - Create `Pages/Index.razor` (dashboard page)
   - Create `Components/Dashboard/AccountSummary.razor`
   - Wire up `IDashboardService`

2. **Set Up SignalR Hub**:
   - Implement `Hubs/TradingHub.cs`
   - Implement `Services/RealtimeUpdateService.cs`
   - Test real-time updates

3. **Create First Chart**:
   - Implement `Components/Charts/EquityCurveChart.razor`
   - Test with sample data

4. **Write First Component Test**:
   - Create `tests/TradingBot.Web.Tests/Components/AccountSummaryTests.cs`
   - Verify bUnit setup is working

5. **Review Documentation**:
   - `research.md`: Technology best practices
   - `data-model.md`: Entity relationships
   - `contracts/signalr-hub.md`: Real-time contracts
   - `contracts/service-contracts.md`: Service layer

---

## Development Workflow

### Daily Development Cycle

1. **Start development environment**:
   ```bash
   # Terminal 1: Blazor with hot reload
   dotnet watch run --project src/TradingBot.Web

   # Terminal 2: Tailwind watch
   cd src/TradingBot.Web && npm run css:watch

   # Terminal 3: Tests with watch
   dotnet watch test --project tests/TradingBot.Web.Tests
   ```

2. **Make changes** to components/services

3. **Hot reload triggers automatically** (Blazor and Tailwind)

4. **Run tests** to verify changes

5. **Commit** when tests pass

### Before Committing

```bash
# Ensure all tests pass
dotnet test

# Ensure code builds in Release mode
dotnet build -c Release

# Check code style (if StyleCop configured)
dotnet build /p:EnforceCodeStyleInBuild=true

# Review changes
git status
git diff
```

---

## Useful Commands

### Development

```bash
# Run app with specific port
dotnet run --project src/TradingBot.Web --urls "https://localhost:5001"

# Run with verbose logging
dotnet run --project src/TradingBot.Web -- --verbose

# Clear and rebuild
dotnet clean && dotnet build
```

### Testing

```bash
# Run all tests
dotnet test

# Run tests with coverage
dotnet test --collect:"XPlat Code Coverage"

# Run specific test
dotnet test --filter "FullyQualifiedName~AccountSummaryTests"

# Run tests with detailed output
dotnet test --verbosity detailed
```

### Database

```bash
# View database tables (requires sqlite3)
sqlite3 src/TradingBot.Cli/tradingbot.db ".tables"

# View accounts
sqlite3 src/TradingBot.Cli/tradingbot.db "SELECT * FROM Accounts;"

# Create migration (if schema changes needed)
dotnet ef migrations add MigrationName --project src/TradingBot.Infrastructure --startup-project src/TradingBot.Web
```

---

## Resources

- **Blazor Documentation**: https://learn.microsoft.com/aspnet/core/blazor/
- **Tailwind CSS**: https://tailwindcss.com/docs
- **bUnit Documentation**: https://bunit.dev/docs/
- **SignalR Documentation**: https://learn.microsoft.com/aspnet/core/signalr/
- **ApexCharts.Blazor**: https://github.com/apexcharts/Blazor-ApexCharts

---

## Summary

You now have a fully configured Blazor Server development environment with:
- ✅ .NET 9 Blazor Server project
- ✅ Tailwind CSS integration with auto-compilation
- ✅ SignalR configured for real-time updates
- ✅ ApexCharts for data visualization
- ✅ bUnit test project with xUnit
- ✅ All TradingBot layer references configured
- ✅ Database connection to shared SQLite file
- ✅ Hot reload enabled for rapid development

Start implementing features by following the `/speckit.tasks` command output next!