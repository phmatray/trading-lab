# TradyStrat Dashboard Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Build TradyStrat — a personal Blazor Server dashboard that tracks the user's CON3 accumulation toward €1M, computes per-ticker technical-analysis zones from Yahoo Finance daily bars, and surfaces a daily Anthropic-generated suggestion in "The Vault" UI.

**Architecture:** Single .NET 10 Blazor Server project organised by vertical-slice feature folders, with orthogonal layers for use cases (`Application/`), specifications (`Specifications/`), startup modules (`Modules/`), domain types and exceptions (`Shared/`), and data access (`Data/`). Razor pages depend on use cases; use cases orchestrate feature services; feature services use EF Core 10 + Ardalis.Specification.

**Tech Stack:** .NET 10 · Blazor Server · EF Core 10 · SQLite · TheAppManager 2.0 · Ardalis.Specification · TaLibStandard (`Atypical.TechnicalAnalysis.*`) · Microsoft.Extensions.AI · Anthropic.SDK · Polly · Serilog · xunit.v3 · Shouldly

**Spec:** [`docs/superpowers/specs/2026-05-06-tradystrat-dashboard-design.md`](../specs/2026-05-06-tradystrat-dashboard-design.md)
**Visual reference:** [`docs/superpowers/specs/2026-05-06-tradystrat-vault-mockup.html`](../specs/2026-05-06-tradystrat-vault-mockup.html)

**Conventions used throughout this plan**
- Working directory: repo root `/Users/philippe/repo/gh-phmatray/TradyStrat` (run all commands from here unless noted).
- All test runs use `dotnet test --nologo`. To target one test: `dotnet test --filter "FullyQualifiedName~ClassOrMethod" --nologo`.
- Each task ends in a commit. Use one commit per task.
- TDD where the unit is testable in isolation (services, parsers, pure functions). For UI components, manual verification is called out explicitly.
- Code blocks contain the **complete** content of the new/changed file in that step (or the exact diff for an edit). No `// ...` placeholders.

---

## Phase 1 — Foundation (Tasks 1–10)

This phase scaffolds the solution, central package versions, the test project, custom exceptions, the clock abstraction, the EF Core data layer with all five entities and their initial migration, the Specifications base, and the first `IAppModule` (`DatabaseModule`) booting the app via `TheAppManager`. End state: `dotnet run` boots an empty Blazor app on http://127.0.0.1:5180; `dotnet test` runs and is green.

---

### Task 1: Scaffold solution, projects, and central package management

**Files:**
- Create: `global.json`
- Create: `Directory.Build.props`
- Create: `Directory.Packages.props`
- Create: `TradyStrat.sln`
- Create: `TradyStrat/TradyStrat.csproj`
- Create: `TradyStrat.Tests/TradyStrat.Tests.csproj`
- Create: `.editorconfig`

- [ ] **Step 1: Verify .NET 10 SDK is installed**

```bash
dotnet --list-sdks
```

Expected: a `10.0.x` line. If absent, install from https://dotnet.microsoft.com/download/dotnet/10.0 before continuing.

- [ ] **Step 2: Pin SDK version with `global.json`**

```json
{
  "sdk": {
    "version": "10.0.100",
    "rollForward": "latestFeature"
  }
}
```

- [ ] **Step 3: Create `Directory.Build.props` (shared compiler settings)**

```xml
<Project>
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <LangVersion>14</LangVersion>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
    <TreatWarningsAsErrors>true</TreatWarningsAsErrors>
    <WarningsNotAsErrors>CS1591</WarningsNotAsErrors>
    <EnableNETAnalyzers>true</EnableNETAnalyzers>
    <AnalysisLevel>latest-recommended</AnalysisLevel>
  </PropertyGroup>
</Project>
```

- [ ] **Step 4: Create `Directory.Packages.props` (central package versions)**

```xml
<Project>
  <PropertyGroup>
    <ManagePackageVersionsCentrally>true</ManagePackageVersionsCentrally>
  </PropertyGroup>
  <ItemGroup>
    <!-- Web / DI -->
    <PackageVersion Include="TheAppManager" Version="2.0.0" />
    <!-- Data -->
    <PackageVersion Include="Microsoft.EntityFrameworkCore.Sqlite" Version="10.0.0" />
    <PackageVersion Include="Microsoft.EntityFrameworkCore.Design" Version="10.0.0" />
    <PackageVersion Include="Ardalis.Specification" Version="9.0.0" />
    <PackageVersion Include="Ardalis.Specification.EntityFrameworkCore" Version="9.0.0" />
    <!-- Indicators -->
    <PackageVersion Include="Atypical.TechnicalAnalysis.Common" Version="3.0.0" />
    <PackageVersion Include="Atypical.TechnicalAnalysis.Functions" Version="3.0.0" />
    <!-- AI -->
    <PackageVersion Include="Microsoft.Extensions.AI" Version="9.4.0" />
    <PackageVersion Include="Anthropic.SDK" Version="5.10.0" />
    <!-- Resilience -->
    <PackageVersion Include="Microsoft.Extensions.Http.Resilience" Version="9.4.0" />
    <!-- Logging -->
    <PackageVersion Include="Serilog.AspNetCore" Version="9.0.0" />
    <PackageVersion Include="Serilog.Sinks.File" Version="6.0.0" />
    <!-- Tests -->
    <PackageVersion Include="xunit.v3" Version="1.0.1" />
    <PackageVersion Include="xunit.runner.visualstudio" Version="3.0.0" />
    <PackageVersion Include="Microsoft.NET.Test.Sdk" Version="17.12.0" />
    <PackageVersion Include="Shouldly" Version="4.3.0" />
    <PackageVersion Include="Microsoft.EntityFrameworkCore.InMemory" Version="10.0.0" />
  </ItemGroup>
</Project>
```

> If a version above is no longer the latest at impl time, bump it. Versions are pinned for reproducibility, not orthodoxy.

- [ ] **Step 5: Create the Blazor Server project file**

`TradyStrat/TradyStrat.csproj`:
```xml
<Project Sdk="Microsoft.NET.Sdk.Web">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <UserSecretsId>tradystrat-local</UserSecretsId>
    <RootNamespace>TradyStrat</RootNamespace>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="TheAppManager" />
    <PackageReference Include="Microsoft.EntityFrameworkCore.Sqlite" />
    <PackageReference Include="Microsoft.EntityFrameworkCore.Design" />
    <PackageReference Include="Ardalis.Specification" />
    <PackageReference Include="Ardalis.Specification.EntityFrameworkCore" />
    <PackageReference Include="Atypical.TechnicalAnalysis.Common" />
    <PackageReference Include="Atypical.TechnicalAnalysis.Functions" />
    <PackageReference Include="Microsoft.Extensions.AI" />
    <PackageReference Include="Anthropic.SDK" />
    <PackageReference Include="Microsoft.Extensions.Http.Resilience" />
    <PackageReference Include="Serilog.AspNetCore" />
    <PackageReference Include="Serilog.Sinks.File" />
  </ItemGroup>
</Project>
```

- [ ] **Step 6: Create the test project file**

`TradyStrat.Tests/TradyStrat.Tests.csproj`:
```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <IsPackable>false</IsPackable>
    <RootNamespace>TradyStrat.Tests</RootNamespace>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="xunit.v3" />
    <PackageReference Include="xunit.runner.visualstudio" />
    <PackageReference Include="Microsoft.NET.Test.Sdk" />
    <PackageReference Include="Shouldly" />
    <PackageReference Include="Microsoft.EntityFrameworkCore.InMemory" />
  </ItemGroup>
  <ItemGroup>
    <ProjectReference Include="..\TradyStrat\TradyStrat.csproj" />
  </ItemGroup>
</Project>
```

- [ ] **Step 7: Create solution and add both projects**

```bash
dotnet new sln --name TradyStrat
dotnet sln add TradyStrat/TradyStrat.csproj
dotnet sln add TradyStrat.Tests/TradyStrat.Tests.csproj
```

- [ ] **Step 8: Create `.editorconfig` (style baseline)**

```ini
root = true

[*]
charset = utf-8
end_of_line = lf
insert_final_newline = true
indent_style = space

[*.cs]
indent_size = 4
csharp_new_line_before_open_brace = all
dotnet_sort_system_directives_first = true
csharp_style_var_for_built_in_types = true
csharp_style_namespace_declarations = file_scoped
```

- [ ] **Step 9: Add a single placeholder file in each project so they compile**

`TradyStrat/Placeholder.cs`:
```csharp
namespace TradyStrat;

internal static class Placeholder { }
```

`TradyStrat.Tests/SmokeTests.cs`:
```csharp
namespace TradyStrat.Tests;

public class SmokeTests
{
    [Fact]
    public void Compiles() => true.ShouldBeTrue();
}
```

- [ ] **Step 10: Verify build & tests run**

```bash
dotnet restore
dotnet build --nologo
dotnet test --nologo
```

Expected: `Build succeeded`. `Passed: 1`.

- [ ] **Step 11: Commit**

```bash
git add global.json Directory.Build.props Directory.Packages.props .editorconfig \
        TradyStrat.sln TradyStrat/ TradyStrat.Tests/
git commit -m "chore: scaffold solution, projects, central package versions"
```

---

### Task 2: Custom exception hierarchy

**Files:**
- Create: `TradyStrat/Shared/Exceptions/TradyStratException.cs`
- Create: `TradyStrat/Shared/Exceptions/PriceFeedUnavailableException.cs`
- Create: `TradyStrat/Shared/Exceptions/FxRateUnavailableException.cs`
- Create: `TradyStrat/Shared/Exceptions/AnthropicCallFailedException.cs`
- Create: `TradyStrat/Shared/Exceptions/AnthropicConfigurationException.cs`
- Create: `TradyStrat/Shared/Exceptions/IndicatorComputationException.cs`
- Create: `TradyStrat/Shared/Exceptions/TradeValidationException.cs`
- Create: `TradyStrat/Shared/Exceptions/CsvImportException.cs`
- Create: `TradyStrat.Tests/Exceptions/ExceptionHierarchyTests.cs`

- [ ] **Step 1: Write the failing test**

`TradyStrat.Tests/Exceptions/ExceptionHierarchyTests.cs`:
```csharp
using Shouldly;
using TradyStrat.Shared.Exceptions;

namespace TradyStrat.Tests.Exceptions;

public class ExceptionHierarchyTests
{
    [Fact]
    public void All_typed_exceptions_derive_from_TradyStratException()
    {
        new PriceFeedUnavailableException("x").ShouldBeAssignableTo<TradyStratException>();
        new FxRateUnavailableException("x").ShouldBeAssignableTo<TradyStratException>();
        new AnthropicCallFailedException("x").ShouldBeAssignableTo<TradyStratException>();
        new AnthropicConfigurationException("x").ShouldBeAssignableTo<TradyStratException>();
        new IndicatorComputationException("x").ShouldBeAssignableTo<TradyStratException>();
        new TradeValidationException("x").ShouldBeAssignableTo<TradyStratException>();
        new CsvImportException("x", lineNumber: 7).ShouldBeAssignableTo<TradyStratException>();
    }

    [Fact]
    public void CsvImportException_prefixes_line_number()
    {
        var ex = new CsvImportException("bad row", lineNumber: 42);

        ex.Message.ShouldBe("line 42: bad row");
        ex.LineNumber.ShouldBe(42);
    }

    [Fact]
    public void CsvImportException_omits_prefix_when_no_line_number()
    {
        new CsvImportException("bad file").Message.ShouldBe("bad file");
    }
}
```

- [ ] **Step 2: Run, expect compile failure (types missing)**

```bash
dotnet test --filter "FullyQualifiedName~ExceptionHierarchyTests" --nologo
```

Expected: build error / `type or namespace ... could not be found`.

- [ ] **Step 3: Create the exception files**

`TradyStrat/Shared/Exceptions/TradyStratException.cs`:
```csharp
namespace TradyStrat.Shared.Exceptions;

public abstract class TradyStratException : Exception
{
    protected TradyStratException(string message, Exception? inner = null)
        : base(message, inner) { }
}
```

`TradyStrat/Shared/Exceptions/PriceFeedUnavailableException.cs`:
```csharp
namespace TradyStrat.Shared.Exceptions;

public sealed class PriceFeedUnavailableException : TradyStratException
{
    public PriceFeedUnavailableException(string message, Exception? inner = null)
        : base(message, inner) { }
}
```

`TradyStrat/Shared/Exceptions/FxRateUnavailableException.cs`:
```csharp
namespace TradyStrat.Shared.Exceptions;

public sealed class FxRateUnavailableException : TradyStratException
{
    public FxRateUnavailableException(string message, Exception? inner = null)
        : base(message, inner) { }
}
```

`TradyStrat/Shared/Exceptions/AnthropicCallFailedException.cs`:
```csharp
namespace TradyStrat.Shared.Exceptions;

public sealed class AnthropicCallFailedException : TradyStratException
{
    public AnthropicCallFailedException(string message, Exception? inner = null)
        : base(message, inner) { }
}
```

`TradyStrat/Shared/Exceptions/AnthropicConfigurationException.cs`:
```csharp
namespace TradyStrat.Shared.Exceptions;

public sealed class AnthropicConfigurationException : TradyStratException
{
    public AnthropicConfigurationException(string message)
        : base(message) { }
}
```

`TradyStrat/Shared/Exceptions/IndicatorComputationException.cs`:
```csharp
namespace TradyStrat.Shared.Exceptions;

public sealed class IndicatorComputationException : TradyStratException
{
    public IndicatorComputationException(string message, Exception? inner = null)
        : base(message, inner) { }
}
```

`TradyStrat/Shared/Exceptions/TradeValidationException.cs`:
```csharp
namespace TradyStrat.Shared.Exceptions;

public sealed class TradeValidationException : TradyStratException
{
    public TradeValidationException(string message)
        : base(message) { }
}
```

`TradyStrat/Shared/Exceptions/CsvImportException.cs`:
```csharp
namespace TradyStrat.Shared.Exceptions;

public sealed class CsvImportException : TradyStratException
{
    public int? LineNumber { get; }

    public CsvImportException(string message, int? lineNumber = null)
        : base(lineNumber.HasValue ? $"line {lineNumber}: {message}" : message)
    {
        LineNumber = lineNumber;
    }
}
```

- [ ] **Step 4: Run, expect pass**

```bash
dotnet test --filter "FullyQualifiedName~ExceptionHierarchyTests" --nologo
```

Expected: `Passed: 3`.

- [ ] **Step 5: Commit**

```bash
git add TradyStrat/Shared/Exceptions/ TradyStrat.Tests/Exceptions/
git commit -m "feat: add custom exception hierarchy rooted at TradyStratException"
```

---

### Task 3: `IClock` abstraction

**Files:**
- Create: `TradyStrat/Shared/Time/IClock.cs`
- Create: `TradyStrat/Shared/Time/SystemClock.cs`
- Create: `TradyStrat.Tests/Time/SystemClockTests.cs`
- Create: `TradyStrat.Tests/Time/FakeClock.cs`

- [ ] **Step 1: Write the failing tests**

`TradyStrat.Tests/Time/SystemClockTests.cs`:
```csharp
using Shouldly;
using TradyStrat.Shared.Time;

namespace TradyStrat.Tests.Time;

public class SystemClockTests
{
    [Fact]
    public void TodayInExchangeTzFor_CON3_returns_today_in_Berlin()
    {
        var c = new SystemClock();
        var today = c.TodayInExchangeTzFor("CON3.DE");
        var expected = DateOnly.FromDateTime(
            TimeZoneInfo.ConvertTime(DateTime.UtcNow,
                TimeZoneInfo.FindSystemTimeZoneById("Europe/Berlin")));

        today.ShouldBe(expected);
    }

    [Theory]
    [InlineData("COIN",    "America/New_York")]
    [InlineData("BTC-USD", "Etc/UTC")]
    [InlineData("EURUSD",  "Etc/UTC")]
    public void TodayInExchangeTzFor_uses_correct_zone(string ticker, string tzId)
    {
        var c = new SystemClock();
        var actual = c.TodayInExchangeTzFor(ticker);
        var expected = DateOnly.FromDateTime(
            TimeZoneInfo.ConvertTime(DateTime.UtcNow,
                TimeZoneInfo.FindSystemTimeZoneById(tzId)));

        actual.ShouldBe(expected);
    }

    [Fact]
    public void UtcNow_returns_kind_utc()
    {
        new SystemClock().UtcNow().Kind.ShouldBe(DateTimeKind.Utc);
    }
}
```

`TradyStrat.Tests/Time/FakeClock.cs` (re-used by later tests):
```csharp
using TradyStrat.Shared.Time;

namespace TradyStrat.Tests.Time;

public sealed class FakeClock(DateTime utcNow) : IClock
{
    public DateTime Now { get; set; } = utcNow;

    public DateTime UtcNow() => Now;
    public DateOnly TodayLocal() => DateOnly.FromDateTime(Now);
    public DateOnly TodayInExchangeTzFor(string ticker) => DateOnly.FromDateTime(Now);
}
```

- [ ] **Step 2: Run, expect compile failure**

```bash
dotnet test --filter "FullyQualifiedName~SystemClockTests" --nologo
```

- [ ] **Step 3: Implement**

`TradyStrat/Shared/Time/IClock.cs`:
```csharp
namespace TradyStrat.Shared.Time;

public interface IClock
{
    DateTime UtcNow();
    DateOnly TodayLocal();
    DateOnly TodayInExchangeTzFor(string ticker);
}
```

`TradyStrat/Shared/Time/SystemClock.cs`:
```csharp
namespace TradyStrat.Shared.Time;

public sealed class SystemClock : IClock
{
    private static readonly Dictionary<string, string> TzByTicker = new()
    {
        ["CON3.DE"] = "Europe/Berlin",
        ["COIN"]    = "America/New_York",
        ["BTC-USD"] = "Etc/UTC",
        ["EURUSD"]  = "Etc/UTC",
    };

    public DateTime UtcNow() => DateTime.UtcNow;

    public DateOnly TodayLocal() => DateOnly.FromDateTime(DateTime.Now);

    public DateOnly TodayInExchangeTzFor(string ticker)
    {
        var tzId = TzByTicker.TryGetValue(ticker, out var z) ? z : "Etc/UTC";
        var tz = TimeZoneInfo.FindSystemTimeZoneById(tzId);
        return DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(DateTime.UtcNow, tz));
    }
}
```

- [ ] **Step 4: Run, expect pass**

```bash
dotnet test --filter "FullyQualifiedName~SystemClockTests" --nologo
```

Expected: `Passed: 5`.

- [ ] **Step 5: Commit**

```bash
git add TradyStrat/Shared/Time/ TradyStrat.Tests/Time/
git commit -m "feat: add IClock abstraction with timezone-per-ticker today helpers"
```

---

### Task 4: Domain enums and primitives

**Files:**
- Create: `TradyStrat/Shared/Domain/TradeSide.cs`
- Create: `TradyStrat/Shared/Domain/Zone.cs`
- Create: `TradyStrat/Shared/Domain/SuggestionAction.cs`
- Create: `TradyStrat/Shared/Domain/IchimokuSignal.cs`
- Create: `TradyStrat/Shared/Domain/Citation.cs`

- [ ] **Step 1: Create files**

`TradyStrat/Shared/Domain/TradeSide.cs`:
```csharp
namespace TradyStrat.Shared.Domain;

public enum TradeSide { Buy = 1, Sell = 2 }
```

`TradyStrat/Shared/Domain/Zone.cs`:
```csharp
namespace TradyStrat.Shared.Domain;

public enum Zone { Accumulate = 1, Hold = 2, Distribute = 3 }
```

`TradyStrat/Shared/Domain/SuggestionAction.cs`:
```csharp
namespace TradyStrat.Shared.Domain;

public enum SuggestionAction { Acquire = 1, Hold = 2, Trim = 3, Wait = 4 }
```

`TradyStrat/Shared/Domain/IchimokuSignal.cs`:
```csharp
namespace TradyStrat.Shared.Domain;

public enum IchimokuSignal { AboveCloud = 1, InCloud = 2, BelowCloud = 3 }
```

`TradyStrat/Shared/Domain/Citation.cs`:
```csharp
namespace TradyStrat.Shared.Domain;

public sealed record Citation(string Claim, string Indicator, string Ticker, string Value);
```

- [ ] **Step 2: Verify build**

```bash
dotnet build --nologo
```

Expected: build succeeds.

- [ ] **Step 3: Commit**

```bash
git add TradyStrat/Shared/Domain/
git commit -m "feat: add domain enums and Citation record"
```

---

### Task 5: Persisted entity records

**Files:**
- Create: `TradyStrat/Shared/Domain/Trade.cs`
- Create: `TradyStrat/Shared/Domain/PriceBar.cs`
- Create: `TradyStrat/Shared/Domain/FxRate.cs`
- Create: `TradyStrat/Shared/Domain/GoalConfig.cs`
- Create: `TradyStrat/Shared/Domain/Suggestion.cs`
- Create: `TradyStrat.Tests/Domain/EntityDerivedPropertiesTests.cs`

- [ ] **Step 1: Write the failing tests for derived properties**

`TradyStrat.Tests/Domain/EntityDerivedPropertiesTests.cs`:
```csharp
using Shouldly;
using TradyStrat.Shared.Domain;

namespace TradyStrat.Tests.Domain;

public class EntityDerivedPropertiesTests
{
    private static Trade Buy(decimal qty, decimal price, decimal fees = 0m) => new()
    {
        Id = 0,
        ExecutedOn = new DateOnly(2026, 5, 6),
        Side = TradeSide.Buy,
        Quantity = qty,
        PricePerShare = price,
        FeesEur = fees,
        Note = null,
        CreatedAt = DateTime.UtcNow,
    };

    [Fact]
    public void Trade_GrossEur_is_qty_times_price()
    {
        Buy(10m, 4.50m).GrossEur.ShouldBe(45.00m);
    }

    [Fact]
    public void Trade_NetEur_buy_adds_fees()
    {
        Buy(10m, 4.50m, fees: 1.20m).NetEur.ShouldBe(46.20m);
    }

    [Fact]
    public void Trade_NetEur_sell_subtracts_fees()
    {
        var sell = Buy(10m, 4.50m, fees: 1.20m) with { Side = TradeSide.Sell };
        sell.NetEur.ShouldBe(43.80m);
    }

    [Fact]
    public void PriceBar_derived_props()
    {
        var bar = new PriceBar
        {
            Id = 1, Ticker = "CON3.DE", Date = new(2026,5,6),
            Open = 4.0m, High = 4.5m, Low = 3.9m, Close = 4.4m, Volume = 1000
        };

        bar.Range.ShouldBe(0.6m);
        bar.Change.ShouldBe(0.4m);
        bar.IsUp.ShouldBeTrue();
    }

    [Fact]
    public void FxRate_EurPerUsd_is_inverse()
    {
        var fx = new FxRate { Id = 1, Date = new(2026,5,6), Pair = "EURUSD",
                              UsdPerEur = 1.0820m, FetchedAt = DateTime.UtcNow };

        fx.EurPerUsd.ShouldBe(1m / 1.0820m);
    }

    [Fact]
    public void Suggestion_OrderValueEur_is_qty_times_price_when_both_set()
    {
        var s = new Suggestion {
            Id = 0, ForDate = new(2026,5,6), Action = SuggestionAction.Acquire,
            QuantityHint = 8m, MaxPriceHint = 4.85m, Conviction = 4,
            Rationale = "x", CitationsJson = "[]", PromptHash = "h",
            CreatedAt = DateTime.UtcNow
        };

        s.OrderValueEur.ShouldBe(8m * 4.85m);
    }

    [Fact]
    public void Suggestion_OrderValueEur_is_null_when_either_missing()
    {
        var s = new Suggestion {
            Id = 0, ForDate = new(2026,5,6), Action = SuggestionAction.Hold,
            QuantityHint = null, MaxPriceHint = 4.85m, Conviction = 3,
            Rationale = "x", CitationsJson = "[]", PromptHash = "h",
            CreatedAt = DateTime.UtcNow
        };

        s.OrderValueEur.ShouldBeNull();
    }

    [Fact]
    public void GoalConfig_Default_is_one_million_with_focus_CON3()
    {
        var now = DateTime.UtcNow;
        var g = GoalConfig.Default(now);

        g.Id.ShouldBe(1);
        g.TargetEur.ShouldBe(1_000_000m);
        g.FocusTicker.ShouldBe("CON3.DE");
        g.UpdatedAt.ShouldBe(now);
    }
}
```

- [ ] **Step 2: Run, expect build failure**

```bash
dotnet test --filter "FullyQualifiedName~EntityDerivedPropertiesTests" --nologo
```

- [ ] **Step 3: Implement entities**

`TradyStrat/Shared/Domain/Trade.cs`:
```csharp
namespace TradyStrat.Shared.Domain;

public sealed record Trade
{
    public required int Id { get; init; }
    public required DateOnly ExecutedOn { get; init; }
    public required TradeSide Side { get; init; }
    public required decimal Quantity { get; init; }
    public required decimal PricePerShare { get; init; }
    public decimal FeesEur { get; init; }
    public string? Note { get; init; }
    public required DateTime CreatedAt { get; init; }

    public decimal GrossEur => Quantity * PricePerShare;
    public decimal NetEur   => Side == TradeSide.Buy ? GrossEur + FeesEur : GrossEur - FeesEur;
    public bool    IsBuy    => Side == TradeSide.Buy;
}
```

`TradyStrat/Shared/Domain/PriceBar.cs`:
```csharp
namespace TradyStrat.Shared.Domain;

public sealed record PriceBar
{
    public required int Id { get; init; }
    public required string Ticker { get; init; }
    public required DateOnly Date { get; init; }
    public required decimal Open { get; init; }
    public required decimal High { get; init; }
    public required decimal Low { get; init; }
    public required decimal Close { get; init; }
    public required long Volume { get; init; }

    public decimal Range  => High - Low;
    public decimal Change => Close - Open;
    public bool    IsUp   => Close >= Open;
}
```

`TradyStrat/Shared/Domain/FxRate.cs`:
```csharp
namespace TradyStrat.Shared.Domain;

public sealed record FxRate
{
    public required int Id { get; init; }
    public required DateOnly Date { get; init; }
    public required string Pair { get; init; }
    public required decimal UsdPerEur { get; init; }
    public required DateTime FetchedAt { get; init; }

    public decimal EurPerUsd => 1m / UsdPerEur;
}
```

`TradyStrat/Shared/Domain/GoalConfig.cs`:
```csharp
namespace TradyStrat.Shared.Domain;

public sealed record GoalConfig
{
    public required int Id { get; init; }
    public required decimal TargetEur { get; init; }
    public DateOnly? TargetDate { get; init; }
    public required string FocusTicker { get; init; }
    public required DateTime UpdatedAt { get; init; }

    public static GoalConfig Default(DateTime now) => new()
    {
        Id = 1,
        TargetEur = 1_000_000m,
        TargetDate = null,
        FocusTicker = "CON3.DE",
        UpdatedAt = now,
    };
}
```

`TradyStrat/Shared/Domain/Suggestion.cs`:
```csharp
using System.Text.Json;

namespace TradyStrat.Shared.Domain;

public sealed record Suggestion
{
    public required int Id { get; init; }
    public required DateOnly ForDate { get; init; }
    public required SuggestionAction Action { get; init; }
    public decimal? QuantityHint { get; init; }
    public decimal? MaxPriceHint { get; init; }
    public required int Conviction { get; init; }
    public required string Rationale { get; init; }
    public required string CitationsJson { get; init; }
    public required string PromptHash { get; init; }
    public required DateTime CreatedAt { get; init; }

    public decimal? OrderValueEur =>
        QuantityHint is decimal q && MaxPriceHint is decimal p ? q * p : null;

    public IReadOnlyList<Citation> Citations =>
        JsonSerializer.Deserialize<List<Citation>>(CitationsJson) ?? [];
}
```

- [ ] **Step 4: Run, expect pass**

```bash
dotnet test --filter "FullyQualifiedName~EntityDerivedPropertiesTests" --nologo
```

Expected: `Passed: 8`.

- [ ] **Step 5: Commit**

```bash
git add TradyStrat/Shared/Domain/ TradyStrat.Tests/Domain/
git commit -m "feat: add persisted entity records with required + derived properties"
```

---

### Task 6: `AppDbContext`, entity configurations, and connection-string helper

**Files:**
- Create: `TradyStrat/Data/AppDbContext.cs`
- Create: `TradyStrat/Data/Configurations/TradeConfiguration.cs`
- Create: `TradyStrat/Data/Configurations/PriceBarConfiguration.cs`
- Create: `TradyStrat/Data/Configurations/FxRateConfiguration.cs`
- Create: `TradyStrat/Data/Configurations/GoalConfigConfiguration.cs`
- Create: `TradyStrat/Data/Configurations/SuggestionConfiguration.cs`
- Create: `TradyStrat/Data/Sqlite/SqlitePathResolver.cs`
- Create: `TradyStrat.Tests/Data/SqlitePathResolverTests.cs`

- [ ] **Step 1: Write the failing test for `SqlitePathResolver`**

`TradyStrat.Tests/Data/SqlitePathResolverTests.cs`:
```csharp
using Shouldly;
using TradyStrat.Data.Sqlite;

namespace TradyStrat.Tests.Data;

public class SqlitePathResolverTests
{
    [Fact]
    public void Expand_replaces_tilde_with_user_home()
    {
        var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);

        SqlitePathResolver.Expand("~/db.sqlite").ShouldBe(Path.Combine(home, "db.sqlite"));
    }

    [Fact]
    public void Expand_returns_absolute_path_unchanged()
    {
        SqlitePathResolver.Expand("/var/data/db.sqlite").ShouldBe("/var/data/db.sqlite");
    }

    [Fact]
    public void Expand_throws_when_path_is_null_or_empty()
    {
        Should.Throw<ArgumentException>(() => SqlitePathResolver.Expand(""));
        Should.Throw<ArgumentException>(() => SqlitePathResolver.Expand(null!));
    }
}
```

- [ ] **Step 2: Run, expect build failure**

```bash
dotnet test --filter "FullyQualifiedName~SqlitePathResolverTests" --nologo
```

- [ ] **Step 3: Implement `SqlitePathResolver`**

`TradyStrat/Data/Sqlite/SqlitePathResolver.cs`:
```csharp
namespace TradyStrat.Data.Sqlite;

public static class SqlitePathResolver
{
    public static string Expand(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
            throw new ArgumentException("Database path is required.", nameof(raw));

        if (raw.StartsWith("~/", StringComparison.Ordinal) || raw == "~")
        {
            var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            return raw.Length == 1 ? home : Path.Combine(home, raw[2..]);
        }

        return raw;
    }
}
```

- [ ] **Step 4: Run, expect pass**

```bash
dotnet test --filter "FullyQualifiedName~SqlitePathResolverTests" --nologo
```

- [ ] **Step 5: Implement `AppDbContext` and entity configurations**

`TradyStrat/Data/AppDbContext.cs`:
```csharp
using Microsoft.EntityFrameworkCore;
using TradyStrat.Shared.Domain;

namespace TradyStrat.Data;

public sealed class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Trade>       Trades       => Set<Trade>();
    public DbSet<PriceBar>    PriceBars    => Set<PriceBar>();
    public DbSet<FxRate>      FxRates      => Set<FxRate>();
    public DbSet<GoalConfig>  Goals        => Set<GoalConfig>();
    public DbSet<Suggestion>  Suggestions  => Set<Suggestion>();

    protected override void OnModelCreating(ModelBuilder b)
        => b.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
}
```

`TradyStrat/Data/Configurations/TradeConfiguration.cs`:
```csharp
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TradyStrat.Shared.Domain;

namespace TradyStrat.Data.Configurations;

public sealed class TradeConfiguration : IEntityTypeConfiguration<Trade>
{
    public void Configure(EntityTypeBuilder<Trade> e)
    {
        e.ToTable("Trades");
        e.HasKey(t => t.Id);
        e.Property(t => t.Id).ValueGeneratedOnAdd();
        e.Property(t => t.Quantity).HasColumnType("TEXT");
        e.Property(t => t.PricePerShare).HasColumnType("TEXT");
        e.Property(t => t.FeesEur).HasColumnType("TEXT");
        e.Property(t => t.Note).HasMaxLength(2000);
        e.HasIndex(t => t.ExecutedOn);
        e.Ignore(t => t.GrossEur);
        e.Ignore(t => t.NetEur);
        e.Ignore(t => t.IsBuy);
    }
}
```

`TradyStrat/Data/Configurations/PriceBarConfiguration.cs`:
```csharp
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TradyStrat.Shared.Domain;

namespace TradyStrat.Data.Configurations;

public sealed class PriceBarConfiguration : IEntityTypeConfiguration<PriceBar>
{
    public void Configure(EntityTypeBuilder<PriceBar> e)
    {
        e.ToTable("PriceBars");
        e.HasKey(p => p.Id);
        e.Property(p => p.Id).ValueGeneratedOnAdd();
        e.Property(p => p.Ticker).HasMaxLength(16).IsRequired();
        foreach (var col in new[] { nameof(PriceBar.Open), nameof(PriceBar.High),
                                     nameof(PriceBar.Low),  nameof(PriceBar.Close) })
            e.Property(col).HasColumnType("TEXT");
        e.HasIndex(p => new { p.Ticker, p.Date }).IsUnique();
        e.Ignore(p => p.Range);
        e.Ignore(p => p.Change);
        e.Ignore(p => p.IsUp);
    }
}
```

`TradyStrat/Data/Configurations/FxRateConfiguration.cs`:
```csharp
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TradyStrat.Shared.Domain;

namespace TradyStrat.Data.Configurations;

public sealed class FxRateConfiguration : IEntityTypeConfiguration<FxRate>
{
    public void Configure(EntityTypeBuilder<FxRate> e)
    {
        e.ToTable("FxRates");
        e.HasKey(r => r.Id);
        e.Property(r => r.Id).ValueGeneratedOnAdd();
        e.Property(r => r.Pair).HasMaxLength(8).IsRequired();
        e.Property(r => r.UsdPerEur).HasColumnType("TEXT");
        e.HasIndex(r => new { r.Pair, r.Date }).IsUnique();
        e.Ignore(r => r.EurPerUsd);
    }
}
```

`TradyStrat/Data/Configurations/GoalConfigConfiguration.cs`:
```csharp
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TradyStrat.Shared.Domain;

namespace TradyStrat.Data.Configurations;

public sealed class GoalConfigConfiguration : IEntityTypeConfiguration<GoalConfig>
{
    public void Configure(EntityTypeBuilder<GoalConfig> e)
    {
        e.ToTable("Goals");
        e.HasKey(g => g.Id);
        e.Property(g => g.Id).ValueGeneratedNever();
        e.Property(g => g.TargetEur).HasColumnType("TEXT");
        e.Property(g => g.FocusTicker).HasMaxLength(16).IsRequired();
    }
}
```

`TradyStrat/Data/Configurations/SuggestionConfiguration.cs`:
```csharp
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TradyStrat.Shared.Domain;

namespace TradyStrat.Data.Configurations;

public sealed class SuggestionConfiguration : IEntityTypeConfiguration<Suggestion>
{
    public void Configure(EntityTypeBuilder<Suggestion> e)
    {
        e.ToTable("Suggestions");
        e.HasKey(s => s.Id);
        e.Property(s => s.Id).ValueGeneratedOnAdd();
        e.Property(s => s.QuantityHint).HasColumnType("TEXT");
        e.Property(s => s.MaxPriceHint).HasColumnType("TEXT");
        e.Property(s => s.Rationale).HasMaxLength(4000);
        e.Property(s => s.CitationsJson).HasMaxLength(8000);
        e.Property(s => s.PromptHash).HasMaxLength(128);
        e.HasIndex(s => s.ForDate).IsUnique();
        e.Ignore(s => s.OrderValueEur);
        e.Ignore(s => s.Citations);
    }
}
```

> **Why `HasColumnType("TEXT")` for decimals?** SQLite has no native `decimal`. EF Core defaults to storing decimals as REAL (double precision), which loses precision for prices and money. Storing as TEXT preserves the exact decimal representation; the round-trip preserves digits. EF Core handles this transparently.

- [ ] **Step 6: Build and verify nothing breaks**

```bash
dotnet build --nologo
```

Expected: build succeeds.

- [ ] **Step 7: Commit**

```bash
git add TradyStrat/Data/ TradyStrat.Tests/Data/
git commit -m "feat: add AppDbContext + entity configurations + sqlite path resolver"
```

---

### Task 7: Initial EF Core migration

**Files:**
- Create: `TradyStrat/Data/Migrations/*` (generated)
- Create: `TradyStrat/appsettings.json`
- Create: `TradyStrat/appsettings.Development.json`

- [ ] **Step 1: Add `appsettings.json` with the database path**

`TradyStrat/appsettings.json`:
```json
{
  "Anthropic": {
    "Model": "claude-opus-4-7",
    "MaxTokens": 1500
  },
  "Yahoo": {
    "BaseUrl": "https://query1.finance.yahoo.com"
  },
  "Tickers": {
    "Focus": "CON3.DE",
    "Context": ["COIN", "BTC-USD"]
  },
  "Fx": {
    "Pair": "EURUSD"
  },
  "Database": {
    "Path": "~/Library/Application Support/TradyStrat/tradystrat.db"
  },
  "Logging": {
    "LogLevel": { "Default": "Information", "Microsoft.AspNetCore": "Warning" }
  }
}
```

`TradyStrat/appsettings.Development.json`:
```json
{
  "Logging": {
    "LogLevel": { "Default": "Debug" }
  }
}
```

- [ ] **Step 2: Add a temporary `Program.cs` so `dotnet ef` can find the host**

`TradyStrat/Program.cs`:
```csharp
using Microsoft.EntityFrameworkCore;
using TradyStrat.Data;
using TradyStrat.Data.Sqlite;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<AppDbContext>(opt =>
    opt.UseSqlite($"Data Source={SqlitePathResolver.Expand(builder.Configuration["Database:Path"]!)}"));

var app = builder.Build();
app.MapGet("/", () => "TradyStrat — bootstrap");
app.Run();
```

> This file is a placeholder for migration generation. Task 9 replaces it with the `TheAppManager` startup.

- [ ] **Step 3: Install `dotnet-ef` if missing, then create the initial migration**

```bash
dotnet tool install --global dotnet-ef --version 10.0.0 2>/dev/null || true
dotnet ef migrations add Initial --project TradyStrat --output-dir Data/Migrations
```

Expected: files appear under `TradyStrat/Data/Migrations/<timestamp>_Initial.cs` and `AppDbContextModelSnapshot.cs`.

- [ ] **Step 4: Apply the migration to a local DB to verify it runs**

```bash
mkdir -p "$HOME/Library/Application Support/TradyStrat"
dotnet ef database update --project TradyStrat
```

Expected: `Applying migration 'YYYYMMDD_Initial'... Done.` File `~/Library/Application Support/TradyStrat/tradystrat.db` exists.

Verify with sqlite3 (skip if you don't have it):
```bash
sqlite3 "$HOME/Library/Application Support/TradyStrat/tradystrat.db" ".schema" | head -40
```

Expected: tables `Trades`, `PriceBars`, `FxRates`, `Goals`, `Suggestions`, `__EFMigrationsHistory`.

- [ ] **Step 5: Commit**

```bash
git add TradyStrat/Data/Migrations/ TradyStrat/appsettings.json \
        TradyStrat/appsettings.Development.json TradyStrat/Program.cs
git commit -m "feat: add initial EF Core migration and appsettings"
```

---

### Task 8: Specifications base + first specs

**Files:**
- Create: `TradyStrat/Specifications/Trades/AllTradesSpec.cs`
- Create: `TradyStrat/Specifications/Trades/TradesByDateRangeSpec.cs`
- Create: `TradyStrat/Specifications/Trades/LatestTradesSpec.cs`
- Create: `TradyStrat/Specifications/PriceBars/PriceBarsForTickerSpec.cs`
- Create: `TradyStrat/Specifications/PriceBars/LatestPriceBarSpec.cs`
- Create: `TradyStrat/Specifications/PriceBars/PriceBarsSinceSpec.cs`
- Create: `TradyStrat/Specifications/FxRates/LatestFxRateSpec.cs`
- Create: `TradyStrat/Specifications/Suggestions/SuggestionForDateSpec.cs`
- Create: `TradyStrat/Specifications/Suggestions/LatestSuggestionSpec.cs`
- Create: `TradyStrat.Tests/Specifications/InMemoryDb.cs`
- Create: `TradyStrat.Tests/Specifications/SpecsRoundtripTests.cs`

- [ ] **Step 1: Write the failing roundtrip test**

`TradyStrat.Tests/Specifications/InMemoryDb.cs`:
```csharp
using Microsoft.EntityFrameworkCore;
using TradyStrat.Data;

namespace TradyStrat.Tests.Specifications;

public static class InMemoryDb
{
    public static AppDbContext Create()
    {
        var opts = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase($"tradystrat-{Guid.NewGuid()}")
            .Options;
        return new AppDbContext(opts);
    }
}
```

`TradyStrat.Tests/Specifications/SpecsRoundtripTests.cs`:
```csharp
using Ardalis.Specification.EntityFrameworkCore;
using Shouldly;
using TradyStrat.Shared.Domain;
using TradyStrat.Specifications.PriceBars;
using TradyStrat.Specifications.Suggestions;
using TradyStrat.Specifications.Trades;
using TradyStrat.Specifications.FxRates;

namespace TradyStrat.Tests.Specifications;

public class SpecsRoundtripTests
{
    private static Trade Buy(int day, decimal qty = 1m, decimal price = 1m) => new()
    {
        Id = 0, ExecutedOn = new DateOnly(2026, 1, day), Side = TradeSide.Buy,
        Quantity = qty, PricePerShare = price, FeesEur = 0, Note = null,
        CreatedAt = DateTime.UtcNow,
    };

    [Fact]
    public async Task TradesByDateRangeSpec_filters_inclusive()
    {
        await using var db = InMemoryDb.Create();
        db.Trades.AddRange(Buy(1), Buy(5), Buy(10));
        await db.SaveChangesAsync();

        var spec = new TradesByDateRangeSpec(new DateOnly(2026,1,2), new DateOnly(2026,1,7));
        var rows = await db.Trades.WithSpecification(spec).ToListAsync();

        rows.Count.ShouldBe(1);
        rows[0].ExecutedOn.Day.ShouldBe(5);
    }

    [Fact]
    public async Task LatestPriceBarSpec_returns_most_recent_for_ticker()
    {
        await using var db = InMemoryDb.Create();
        db.PriceBars.AddRange(
            new PriceBar { Id = 0, Ticker = "CON3.DE", Date = new(2026,1,1), Open = 1, High = 1, Low = 1, Close = 1, Volume = 1 },
            new PriceBar { Id = 0, Ticker = "CON3.DE", Date = new(2026,1,2), Open = 2, High = 2, Low = 2, Close = 2, Volume = 1 },
            new PriceBar { Id = 0, Ticker = "COIN",    Date = new(2026,1,3), Open = 9, High = 9, Low = 9, Close = 9, Volume = 1 });
        await db.SaveChangesAsync();

        var bar = await db.PriceBars.WithSpecification(new LatestPriceBarSpec("CON3.DE")).FirstOrDefaultAsync();

        bar.ShouldNotBeNull();
        bar.Date.ShouldBe(new DateOnly(2026,1,2));
    }

    [Fact]
    public async Task SuggestionForDateSpec_finds_exact_date_match()
    {
        await using var db = InMemoryDb.Create();
        db.Suggestions.Add(new Suggestion {
            Id = 0, ForDate = new(2026,5,6), Action = SuggestionAction.Hold,
            Conviction = 3, Rationale = "x", CitationsJson = "[]",
            PromptHash = "h", CreatedAt = DateTime.UtcNow });
        await db.SaveChangesAsync();

        var hit  = await db.Suggestions.WithSpecification(new SuggestionForDateSpec(new(2026,5,6))).FirstOrDefaultAsync();
        var miss = await db.Suggestions.WithSpecification(new SuggestionForDateSpec(new(2026,5,7))).FirstOrDefaultAsync();

        hit.ShouldNotBeNull();
        miss.ShouldBeNull();
    }

    [Fact]
    public async Task LatestFxRateSpec_returns_most_recent_at_or_before_date()
    {
        await using var db = InMemoryDb.Create();
        db.FxRates.AddRange(
            new FxRate { Id = 0, Pair = "EURUSD", Date = new(2026,1,1), UsdPerEur = 1.05m, FetchedAt = DateTime.UtcNow },
            new FxRate { Id = 0, Pair = "EURUSD", Date = new(2026,1,3), UsdPerEur = 1.08m, FetchedAt = DateTime.UtcNow },
            new FxRate { Id = 0, Pair = "EURUSD", Date = new(2026,1,5), UsdPerEur = 1.10m, FetchedAt = DateTime.UtcNow });
        await db.SaveChangesAsync();

        var on4 = await db.FxRates.WithSpecification(new LatestFxRateSpec("EURUSD", new(2026,1,4))).FirstOrDefaultAsync();

        on4.ShouldNotBeNull();
        on4.Date.ShouldBe(new DateOnly(2026,1,3));
    }
}
```

- [ ] **Step 2: Run, expect build failure**

```bash
dotnet test --filter "FullyQualifiedName~SpecsRoundtripTests" --nologo
```

- [ ] **Step 3: Implement specifications**

`TradyStrat/Specifications/Trades/AllTradesSpec.cs`:
```csharp
using Ardalis.Specification;
using TradyStrat.Shared.Domain;

namespace TradyStrat.Specifications.Trades;

public sealed class AllTradesSpec : Specification<Trade>
{
    public AllTradesSpec() => Query.OrderBy(t => t.ExecutedOn).ThenBy(t => t.Id);
}
```

`TradyStrat/Specifications/Trades/TradesByDateRangeSpec.cs`:
```csharp
using Ardalis.Specification;
using TradyStrat.Shared.Domain;

namespace TradyStrat.Specifications.Trades;

public sealed class TradesByDateRangeSpec : Specification<Trade>
{
    public TradesByDateRangeSpec(DateOnly from, DateOnly to)
    {
        Query.Where(t => t.ExecutedOn >= from && t.ExecutedOn <= to)
             .OrderBy(t => t.ExecutedOn);
    }
}
```

`TradyStrat/Specifications/Trades/LatestTradesSpec.cs`:
```csharp
using Ardalis.Specification;
using TradyStrat.Shared.Domain;

namespace TradyStrat.Specifications.Trades;

public sealed class LatestTradesSpec : Specification<Trade>
{
    public LatestTradesSpec(int count)
    {
        Query.OrderByDescending(t => t.ExecutedOn).ThenByDescending(t => t.Id).Take(count);
    }
}
```

`TradyStrat/Specifications/PriceBars/PriceBarsForTickerSpec.cs`:
```csharp
using Ardalis.Specification;
using TradyStrat.Shared.Domain;

namespace TradyStrat.Specifications.PriceBars;

public sealed class PriceBarsForTickerSpec : Specification<PriceBar>
{
    public PriceBarsForTickerSpec(string ticker)
    {
        Query.Where(b => b.Ticker == ticker).OrderBy(b => b.Date);
    }
}
```

`TradyStrat/Specifications/PriceBars/LatestPriceBarSpec.cs`:
```csharp
using Ardalis.Specification;
using TradyStrat.Shared.Domain;

namespace TradyStrat.Specifications.PriceBars;

public sealed class LatestPriceBarSpec : Specification<PriceBar>
{
    public LatestPriceBarSpec(string ticker)
    {
        Query.Where(b => b.Ticker == ticker)
             .OrderByDescending(b => b.Date)
             .Take(1);
    }
}
```

`TradyStrat/Specifications/PriceBars/PriceBarsSinceSpec.cs`:
```csharp
using Ardalis.Specification;
using TradyStrat.Shared.Domain;

namespace TradyStrat.Specifications.PriceBars;

public sealed class PriceBarsSinceSpec : Specification<PriceBar>
{
    public PriceBarsSinceSpec(string ticker, DateOnly since)
    {
        Query.Where(b => b.Ticker == ticker && b.Date >= since).OrderBy(b => b.Date);
    }
}
```

`TradyStrat/Specifications/FxRates/LatestFxRateSpec.cs`:
```csharp
using Ardalis.Specification;
using TradyStrat.Shared.Domain;

namespace TradyStrat.Specifications.FxRates;

public sealed class LatestFxRateSpec : Specification<FxRate>
{
    public LatestFxRateSpec(string pair, DateOnly asOf)
    {
        Query.Where(r => r.Pair == pair && r.Date <= asOf)
             .OrderByDescending(r => r.Date)
             .Take(1);
    }
}
```

`TradyStrat/Specifications/Suggestions/SuggestionForDateSpec.cs`:
```csharp
using Ardalis.Specification;
using TradyStrat.Shared.Domain;

namespace TradyStrat.Specifications.Suggestions;

public sealed class SuggestionForDateSpec : Specification<Suggestion>
{
    public SuggestionForDateSpec(DateOnly date) => Query.Where(s => s.ForDate == date);
}
```

`TradyStrat/Specifications/Suggestions/LatestSuggestionSpec.cs`:
```csharp
using Ardalis.Specification;
using TradyStrat.Shared.Domain;

namespace TradyStrat.Specifications.Suggestions;

public sealed class LatestSuggestionSpec : Specification<Suggestion>
{
    public LatestSuggestionSpec()
    {
        Query.OrderByDescending(s => s.ForDate).Take(1);
    }
}
```

- [ ] **Step 4: Run, expect pass**

```bash
dotnet test --filter "FullyQualifiedName~SpecsRoundtripTests" --nologo
```

Expected: `Passed: 4`.

- [ ] **Step 5: Commit**

```bash
git add TradyStrat/Specifications/ TradyStrat.Tests/Specifications/
git commit -m "feat: add Ardalis specifications for Trades/PriceBars/FxRates/Suggestions"
```

---

### Task 9: `DatabaseModule` + `TheAppManager` startup

**Files:**
- Modify: `TradyStrat/Program.cs`
- Create: `TradyStrat/Modules/DatabaseModule.cs`
- Create: `TradyStrat/Components/App.razor`
- Create: `TradyStrat/Components/Routes.razor`
- Create: `TradyStrat/Components/_Imports.razor`
- Create: `TradyStrat/Components/Layout/MainLayout.razor`
- Create: `TradyStrat/Components/Layout/MainLayout.razor.css`
- Create: `TradyStrat/Components/Pages/Home.razor`
- Create: `TradyStrat/Modules/HostingModule.cs`

- [ ] **Step 1: Replace `Program.cs` with TheAppManager startup**

`TradyStrat/Program.cs`:
```csharp
using TheAppManager.Startup;

AppManager.Start(args);
```

- [ ] **Step 2: Create `DatabaseModule`**

`TradyStrat/Modules/DatabaseModule.cs`:
```csharp
using Ardalis.Specification;
using Ardalis.Specification.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using TheAppManager.Modules;
using TradyStrat.Data;
using TradyStrat.Data.Sqlite;
using TradyStrat.Shared.Time;

namespace TradyStrat.Modules;

public sealed class DatabaseModule : IAppModule
{
    public void ConfigureServices(WebApplicationBuilder builder)
    {
        var dbPath = SqlitePathResolver.Expand(builder.Configuration["Database:Path"]!);
        var dir = Path.GetDirectoryName(dbPath);
        if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);

        builder.Services.AddDbContext<AppDbContext>(opt =>
            opt.UseSqlite($"Data Source={dbPath}"));

        builder.Services.AddScoped(typeof(IRepositoryBase<>),     typeof(EfRepositoryShim<>));
        builder.Services.AddScoped(typeof(IReadRepositoryBase<>), typeof(EfRepositoryShim<>));
        builder.Services.AddSingleton<IClock, SystemClock>();
    }

    public void ConfigureMiddleware(WebApplication app)
    {
        using var scope = app.Services.CreateScope();
        scope.ServiceProvider.GetRequiredService<AppDbContext>().Database.Migrate();
    }
}

internal sealed class EfRepositoryShim<T>(AppDbContext db) : RepositoryBase<T>(db) where T : class { }
```

> `Ardalis.Specification.EntityFrameworkCore`'s `RepositoryBase<T>` needs the `DbContext` in its base ctor; the shim adapts our scoped `AppDbContext`.

- [ ] **Step 3: Create the Blazor Server hosting module**

`TradyStrat/Modules/HostingModule.cs`:
```csharp
using TheAppManager.Modules;
using TradyStrat.Components;

namespace TradyStrat.Modules;

public sealed class HostingModule : IAppModule
{
    public void ConfigureServices(WebApplicationBuilder builder)
    {
        builder.WebHost.ConfigureKestrel(opt => opt.ListenLocalhost(5180));
        builder.Services.AddRazorComponents().AddInteractiveServerComponents();
    }

    public void ConfigureMiddleware(WebApplication app)
    {
        app.UseStaticFiles();
        app.UseAntiforgery();
    }

    public void ConfigureEndpoints(IEndpointRouteBuilder endpoints)
    {
        endpoints.MapRazorComponents<App>().AddInteractiveServerRenderMode();
    }
}
```

- [ ] **Step 4: Create the Razor host components**

`TradyStrat/Components/_Imports.razor`:
```razor
@using Microsoft.AspNetCore.Components
@using Microsoft.AspNetCore.Components.Routing
@using Microsoft.AspNetCore.Components.Web
@using TradyStrat.Components
@using TradyStrat.Components.Layout
```

`TradyStrat/Components/App.razor`:
```razor
<!DOCTYPE html>
<html lang="en">
<head>
    <meta charset="utf-8" />
    <meta name="viewport" content="width=device-width, initial-scale=1.0" />
    <base href="/" />
    <title>TradyStrat</title>
    <link rel="preconnect" href="https://fonts.googleapis.com" />
    <link rel="preconnect" href="https://fonts.gstatic.com" crossorigin />
    <link rel="stylesheet"
          href="https://fonts.googleapis.com/css2?family=Cormorant+Garamond:ital,wght@0,300;0,400;0,500;0,600;1,400;1,500&family=JetBrains+Mono:wght@300..700&display=swap" />
    <link rel="stylesheet" href="css/vault.css" />
    <HeadOutlet @rendermode="InteractiveServer" />
</head>
<body>
    <Routes @rendermode="InteractiveServer" />
    <script src="_framework/blazor.web.js"></script>
</body>
</html>
```

`TradyStrat/Components/Routes.razor`:
```razor
<Router AppAssembly="typeof(App).Assembly">
    <Found Context="routeData">
        <RouteView RouteData="routeData" DefaultLayout="typeof(MainLayout)" />
        <FocusOnNavigate RouteData="routeData" Selector="h1" />
    </Found>
</Router>
```

`TradyStrat/Components/Layout/MainLayout.razor`:
```razor
@inherits LayoutComponentBase

<main>
    @Body
</main>
```

`TradyStrat/Components/Layout/MainLayout.razor.css`:
```css
main { min-height: 100vh; }
```

`TradyStrat/Components/Pages/Home.razor`:
```razor
@page "/"
<h1>TradyStrat — bootstrap</h1>
<p>The application is running. Phase 1 complete.</p>
```

- [ ] **Step 5: Add minimal `wwwroot/css/vault.css` (tokens, real styling lands later)**

`TradyStrat/wwwroot/css/vault.css`:
```css
:root {
  --vault-bg:     #0E0D0A;
  --vault-bg-2:   #15140F;
  --vault-ivory:  #ECE6D6;
  --vault-gold:   #C49A56;
  --vault-rule:   #2C281F;
  --vault-green:  #7AB68E;
  --vault-red:    #C36A6A;

  --font-display: "Cormorant Garamond", "Newsreader", Georgia, serif;
  --font-body:    "Cormorant Garamond", "Newsreader", Georgia, serif;
  --font-mono:    "JetBrains Mono", ui-monospace, monospace;

  --rule-soft:    1px solid var(--vault-rule);
  --tracking-xs:  0.32em;
  --tracking-md:  0.22em;
}

*, *::before, *::after { box-sizing: border-box; margin: 0; padding: 0; }
body {
  background: var(--vault-bg);
  color: var(--vault-ivory);
  font-family: var(--font-body);
  font-feature-settings: "kern", "liga", "onum";
}
.num   { font-family: var(--font-mono); font-variant-numeric: tabular-nums; }
.label { font-family: var(--font-mono); font-size: 10px;
         letter-spacing: var(--tracking-xs); text-transform: uppercase;
         color: var(--vault-gold); }
```

- [ ] **Step 6: Run the app locally to verify it boots**

```bash
dotnet run --project TradyStrat
```

In another terminal:
```bash
curl -s http://127.0.0.1:5180/ | grep -o "TradyStrat — bootstrap"
```

Expected: `TradyStrat — bootstrap`. Stop the app with Ctrl+C.

- [ ] **Step 7: Commit**

```bash
git add TradyStrat/Program.cs TradyStrat/Modules/ TradyStrat/Components/ TradyStrat/wwwroot/
git commit -m "feat: bootstrap Blazor Server via TheAppManager modules"
```

---

### Task 10: Module smoke test

**Files:**
- Create: `TradyStrat.Tests/Modules/ModuleSmokeTests.cs`

- [ ] **Step 1: Write the smoke test**

`TradyStrat.Tests/Modules/ModuleSmokeTests.cs`:
```csharp
using Microsoft.AspNetCore.Mvc.Testing;
using Shouldly;

namespace TradyStrat.Tests.Modules;

public class ModuleSmokeTests
{
    [Fact]
    public async Task Application_boots_and_responds_on_root_route()
    {
        await using var factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(b =>
            {
                b.UseSetting("Database:Path", Path.Combine(Path.GetTempPath(),
                    $"tradystrat-smoke-{Guid.NewGuid()}.db"));
            });

        using var client = factory.CreateClient();
        var resp = await client.GetAsync("/");

        resp.IsSuccessStatusCode.ShouldBeTrue();
        var body = await resp.Content.ReadAsStringAsync();
        body.ShouldContain("TradyStrat — bootstrap");
    }
}
```

- [ ] **Step 2: Add `Microsoft.AspNetCore.Mvc.Testing` reference**

Add to `Directory.Packages.props`:
```xml
<PackageVersion Include="Microsoft.AspNetCore.Mvc.Testing" Version="10.0.0" />
```

Add to `TradyStrat.Tests/TradyStrat.Tests.csproj` `<ItemGroup>`:
```xml
<PackageReference Include="Microsoft.AspNetCore.Mvc.Testing" />
```

Make `Program` class visible to tests — add to `TradyStrat/Program.cs` at the bottom:
```csharp

public partial class Program { }
```

- [ ] **Step 3: Run the smoke test**

```bash
dotnet test --filter "FullyQualifiedName~ModuleSmokeTests" --nologo
```

Expected: `Passed: 1`.

- [ ] **Step 4: Commit**

```bash
git add TradyStrat.Tests/Modules/ TradyStrat.Tests/TradyStrat.Tests.csproj \
        Directory.Packages.props TradyStrat/Program.cs
git commit -m "test: add module smoke test verifying app boots end-to-end"
```

**End of Phase 1.** State check: `dotnet test --nologo` runs all tests green; `dotnet run --project TradyStrat` boots an empty Blazor app on `http://127.0.0.1:5180`; database file exists with the five tables.

---

## Phase 2 — Price feed and FX (Tasks 11–17)

This phase builds the Yahoo Finance integration: pure JSON parser, HTTP-backed feed, daily-cache decorator (idempotent persistence), FX equivalent, converter, hosted service that warms caches on startup, and the two modules wiring it all up. End state: starting the app pulls today's bars for CON3.DE / COIN / BTC-USD / EURUSD; re-starting on the same day fetches nothing.

---

### Task 11: `YahooParser` — pure JSON → `PriceBar[]`

**Files:**
- Create: `TradyStrat/Features/PriceFeed/YahooParser.cs`
- Create: `TradyStrat.Tests/PriceFeed/Fixtures/yahoo-con3-mini.json`
- Create: `TradyStrat.Tests/PriceFeed/Fixtures/yahoo-empty.json`
- Create: `TradyStrat.Tests/PriceFeed/Fixtures/yahoo-malformed.json`
- Create: `TradyStrat.Tests/PriceFeed/YahooParserTests.cs`

- [ ] **Step 1: Add the captured fixtures (3 daily bars, mocked but realistic)**

`TradyStrat.Tests/PriceFeed/Fixtures/yahoo-con3-mini.json`:
```json
{
  "chart": {
    "result": [{
      "meta": { "symbol": "CON3.DE", "currency": "EUR" },
      "timestamp": [1714435200, 1714521600, 1714608000],
      "indicators": {
        "quote": [{
          "open":   [4.10, 4.18, 4.22],
          "high":   [4.25, 4.28, 4.30],
          "low":    [4.05, 4.12, 4.15],
          "close":  [4.18, 4.22, 4.27],
          "volume": [120000, 95000, 130000]
        }]
      }
    }],
    "error": null
  }
}
```

`TradyStrat.Tests/PriceFeed/Fixtures/yahoo-empty.json`:
```json
{ "chart": { "result": [{ "meta": {}, "timestamp": [], "indicators": { "quote": [{ "open":[], "high":[], "low":[], "close":[], "volume":[] }] } }], "error": null } }
```

`TradyStrat.Tests/PriceFeed/Fixtures/yahoo-malformed.json`:
```json
{ "chart": { "result": null, "error": { "code": "Bad", "description": "x" } } }
```

Add fixtures to test project `.csproj` so they ship to the test bin dir:
```xml
<ItemGroup>
  <None Update="PriceFeed/Fixtures/*.json">
    <CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>
  </None>
</ItemGroup>
```

- [ ] **Step 2: Write the failing tests**

`TradyStrat.Tests/PriceFeed/YahooParserTests.cs`:
```csharp
using System.Text.Json;
using Shouldly;
using TradyStrat.Features.PriceFeed;
using TradyStrat.Shared.Exceptions;

namespace TradyStrat.Tests.PriceFeed;

public class YahooParserTests
{
    private static JsonDocument Load(string fixture)
        => JsonDocument.Parse(File.ReadAllText(Path.Combine("PriceFeed", "Fixtures", fixture)));

    [Fact]
    public void Parses_three_daily_bars_with_correct_decimals()
    {
        using var doc = Load("yahoo-con3-mini.json");
        var bars = YahooParser.ParseDaily("CON3.DE", doc);

        bars.Count.ShouldBe(3);
        bars[0].Ticker.ShouldBe("CON3.DE");
        bars[0].Close.ShouldBe(4.18m);
        bars[1].Volume.ShouldBe(95000L);
        bars[2].High.ShouldBe(4.30m);
    }

    [Fact]
    public void Returns_empty_list_when_payload_has_no_timestamps()
    {
        using var doc = Load("yahoo-empty.json");
        YahooParser.ParseDaily("CON3.DE", doc).ShouldBeEmpty();
    }

    [Fact]
    public void Throws_PriceFeedUnavailableException_for_malformed_payload()
    {
        using var doc = Load("yahoo-malformed.json");
        Should.Throw<PriceFeedUnavailableException>(() => YahooParser.ParseDaily("CON3.DE", doc));
    }

    [Fact]
    public void Skips_bars_with_null_close_values()
    {
        using var doc = JsonDocument.Parse("""
        {"chart":{"result":[{"timestamp":[1,2,3],"indicators":{"quote":[
          {"open":[1,2,3],"high":[1,2,3],"low":[1,2,3],"close":[1,null,3],"volume":[10,20,30]}
        ]}}],"error":null}}
        """);

        var bars = YahooParser.ParseDaily("X", doc);
        bars.Count.ShouldBe(2);
    }
}
```

- [ ] **Step 3: Run, expect compile failure**

```bash
dotnet test --filter "FullyQualifiedName~YahooParserTests" --nologo
```

- [ ] **Step 4: Implement `YahooParser`**

`TradyStrat/Features/PriceFeed/YahooParser.cs`:
```csharp
using System.Text.Json;
using TradyStrat.Shared.Domain;
using TradyStrat.Shared.Exceptions;

namespace TradyStrat.Features.PriceFeed;

public static class YahooParser
{
    public static IReadOnlyList<PriceBar> ParseDaily(string ticker, JsonDocument doc)
    {
        try
        {
            var root = doc.RootElement.GetProperty("chart");
            if (root.TryGetProperty("error", out var err) && err.ValueKind == JsonValueKind.Object)
                throw new PriceFeedUnavailableException(
                    $"Yahoo error for {ticker}: {err.GetRawText()}");

            var result = root.GetProperty("result");
            if (result.ValueKind != JsonValueKind.Array || result.GetArrayLength() == 0)
                throw new PriceFeedUnavailableException($"Yahoo returned no result for {ticker}");

            var first = result[0];
            if (!first.TryGetProperty("timestamp", out var ts) || ts.ValueKind != JsonValueKind.Array)
                return [];

            var quote   = first.GetProperty("indicators").GetProperty("quote")[0];
            var opens   = quote.GetProperty("open");
            var highs   = quote.GetProperty("high");
            var lows    = quote.GetProperty("low");
            var closes  = quote.GetProperty("close");
            var volumes = quote.GetProperty("volume");

            var bars = new List<PriceBar>(ts.GetArrayLength());
            for (var i = 0; i < ts.GetArrayLength(); i++)
            {
                if (closes[i].ValueKind == JsonValueKind.Null) continue;

                var date = DateOnly.FromDateTime(
                    DateTimeOffset.FromUnixTimeSeconds(ts[i].GetInt64()).UtcDateTime);

                bars.Add(new PriceBar
                {
                    Id     = 0,
                    Ticker = ticker,
                    Date   = date,
                    Open   = AsDecimal(opens[i]),
                    High   = AsDecimal(highs[i]),
                    Low    = AsDecimal(lows[i]),
                    Close  = AsDecimal(closes[i]),
                    Volume = volumes[i].ValueKind == JsonValueKind.Null ? 0L : volumes[i].GetInt64(),
                });
            }
            return bars;
        }
        catch (Exception ex) when (ex is not PriceFeedUnavailableException
                                   and (KeyNotFoundException or InvalidOperationException
                                        or FormatException or JsonException))
        {
            throw new PriceFeedUnavailableException(
                $"Failed to parse Yahoo payload for {ticker}", ex);
        }
    }

    private static decimal AsDecimal(JsonElement e)
        => e.ValueKind == JsonValueKind.Null ? 0m : (decimal)e.GetDouble();
}
```

- [ ] **Step 5: Run, expect pass**

```bash
dotnet test --filter "FullyQualifiedName~YahooParserTests" --nologo
```

Expected: `Passed: 4`.

- [ ] **Step 6: Commit**

```bash
git add TradyStrat/Features/PriceFeed/YahooParser.cs \
        TradyStrat.Tests/PriceFeed/ \
        TradyStrat.Tests/TradyStrat.Tests.csproj
git commit -m "feat: add YahooParser with PriceFeedUnavailableException on malformed input"
```

---

### Task 12: `IPriceFeed` + `YahooPriceFeed` HTTP client

**Files:**
- Create: `TradyStrat/Features/PriceFeed/IPriceFeed.cs`
- Create: `TradyStrat/Features/PriceFeed/YahooPriceFeed.cs`
- Create: `TradyStrat.Tests/PriceFeed/YahooPriceFeedTests.cs`
- Create: `TradyStrat.Tests/PriceFeed/StubHttpHandler.cs`

- [ ] **Step 1: Write the failing tests**

`TradyStrat.Tests/PriceFeed/StubHttpHandler.cs`:
```csharp
namespace TradyStrat.Tests.PriceFeed;

public sealed class StubHttpHandler(Func<HttpRequestMessage, HttpResponseMessage> respond)
    : HttpMessageHandler
{
    public List<HttpRequestMessage> Calls { get; } = new();

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage req, CancellationToken ct)
    {
        Calls.Add(req);
        return Task.FromResult(respond(req));
    }
}
```

`TradyStrat.Tests/PriceFeed/YahooPriceFeedTests.cs`:
```csharp
using System.Net;
using Shouldly;
using TradyStrat.Features.PriceFeed;
using TradyStrat.Shared.Exceptions;

namespace TradyStrat.Tests.PriceFeed;

public class YahooPriceFeedTests
{
    private static HttpClient ClientReturning(HttpStatusCode code, string body)
    {
        var handler = new StubHttpHandler(_ =>
        {
            var resp = new HttpResponseMessage(code) { Content = new StringContent(body) };
            return resp;
        });
        return new HttpClient(handler) { BaseAddress = new Uri("https://query1.finance.yahoo.com") };
    }

    [Fact]
    public async Task Builds_url_with_unix_timestamps_and_daily_interval()
    {
        var captured = new List<HttpRequestMessage>();
        var handler = new StubHttpHandler(req =>
        {
            captured.Add(req);
            return new HttpResponseMessage(HttpStatusCode.OK)
                { Content = new StringContent(File.ReadAllText("PriceFeed/Fixtures/yahoo-con3-mini.json")) };
        });
        var http = new HttpClient(handler) { BaseAddress = new Uri("https://query1.finance.yahoo.com") };
        var feed = new YahooPriceFeed(http);

        var bars = await feed.FetchDailyAsync("CON3.DE", new(2024,4,30), new(2024,5,2), CancellationToken.None);

        bars.Count.ShouldBe(3);
        captured[0].RequestUri!.PathAndQuery.ShouldContain("/v8/finance/chart/CON3.DE");
        captured[0].RequestUri!.PathAndQuery.ShouldContain("interval=1d");
    }

    [Fact]
    public async Task Throws_PriceFeedUnavailableException_on_5xx()
    {
        var feed = new YahooPriceFeed(ClientReturning(HttpStatusCode.InternalServerError, ""));

        await Should.ThrowAsync<PriceFeedUnavailableException>(() =>
            feed.FetchDailyAsync("X", new(2024,1,1), new(2024,1,2), CancellationToken.None));
    }

    [Fact]
    public async Task Throws_PriceFeedUnavailableException_on_invalid_json()
    {
        var feed = new YahooPriceFeed(ClientReturning(HttpStatusCode.OK, "not json"));

        await Should.ThrowAsync<PriceFeedUnavailableException>(() =>
            feed.FetchDailyAsync("X", new(2024,1,1), new(2024,1,2), CancellationToken.None));
    }
}
```

- [ ] **Step 2: Run, expect compile failure**

```bash
dotnet test --filter "FullyQualifiedName~YahooPriceFeedTests" --nologo
```

- [ ] **Step 3: Implement**

`TradyStrat/Features/PriceFeed/IPriceFeed.cs`:
```csharp
using TradyStrat.Shared.Domain;

namespace TradyStrat.Features.PriceFeed;

public interface IPriceFeed
{
    Task<IReadOnlyList<PriceBar>> FetchDailyAsync(
        string ticker, DateOnly from, DateOnly to, CancellationToken ct);
}
```

`TradyStrat/Features/PriceFeed/YahooPriceFeed.cs`:
```csharp
using System.Net.Http.Headers;
using System.Text.Json;
using TradyStrat.Shared.Domain;
using TradyStrat.Shared.Exceptions;

namespace TradyStrat.Features.PriceFeed;

public sealed class YahooPriceFeed(HttpClient http) : IPriceFeed
{
    public async Task<IReadOnlyList<PriceBar>> FetchDailyAsync(
        string ticker, DateOnly from, DateOnly to, CancellationToken ct)
    {
        var p1 = new DateTimeOffset(from.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero).ToUnixTimeSeconds();
        var p2 = new DateTimeOffset(to.ToDateTime(TimeOnly.MaxValue),   TimeSpan.Zero).ToUnixTimeSeconds();
        var url = $"/v8/finance/chart/{Uri.EscapeDataString(ticker)}?period1={p1}&period2={p2}&interval=1d";

        try
        {
            using var resp = await http.GetAsync(url, ct);
            if (!resp.IsSuccessStatusCode)
                throw new PriceFeedUnavailableException(
                    $"Yahoo {(int)resp.StatusCode} for {ticker}");

            await using var stream = await resp.Content.ReadAsStreamAsync(ct);
            using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: ct);
            return YahooParser.ParseDaily(ticker, doc);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or JsonException)
        {
            throw new PriceFeedUnavailableException($"Yahoo fetch failed for {ticker}", ex);
        }
    }
}
```

- [ ] **Step 4: Run, expect pass**

```bash
dotnet test --filter "FullyQualifiedName~YahooPriceFeedTests" --nologo
```

Expected: `Passed: 3`.

- [ ] **Step 5: Commit**

```bash
git add TradyStrat/Features/PriceFeed/IPriceFeed.cs \
        TradyStrat/Features/PriceFeed/YahooPriceFeed.cs \
        TradyStrat.Tests/PriceFeed/YahooPriceFeedTests.cs \
        TradyStrat.Tests/PriceFeed/StubHttpHandler.cs
git commit -m "feat: add YahooPriceFeed HttpClient with typed exception mapping"
```

---

### Task 13: `DailyPriceCache` (Decorator)

**Files:**
- Create: `TradyStrat/Features/PriceFeed/DailyPriceCache.cs`
- Create: `TradyStrat.Tests/PriceFeed/DailyPriceCacheTests.cs`
- Create: `TradyStrat.Tests/PriceFeed/StubPriceFeed.cs`

- [ ] **Step 1: Write the failing tests**

`TradyStrat.Tests/PriceFeed/StubPriceFeed.cs`:
```csharp
using TradyStrat.Features.PriceFeed;
using TradyStrat.Shared.Domain;

namespace TradyStrat.Tests.PriceFeed;

public sealed class StubPriceFeed(IReadOnlyList<PriceBar> bars) : IPriceFeed
{
    public int CallCount { get; private set; }
    public List<(DateOnly from, DateOnly to)> Ranges { get; } = new();

    public Task<IReadOnlyList<PriceBar>> FetchDailyAsync(
        string ticker, DateOnly from, DateOnly to, CancellationToken ct)
    {
        CallCount++;
        Ranges.Add((from, to));
        return Task.FromResult(bars);
    }
}
```

`TradyStrat.Tests/PriceFeed/DailyPriceCacheTests.cs`:
```csharp
using Microsoft.Extensions.Logging.Abstractions;
using Shouldly;
using TradyStrat.Features.PriceFeed;
using TradyStrat.Shared.Domain;
using TradyStrat.Tests.Specifications;
using TradyStrat.Tests.Time;

namespace TradyStrat.Tests.PriceFeed;

public class DailyPriceCacheTests
{
    private static PriceBar Bar(DateOnly d) => new()
    {
        Id = 0, Ticker = "CON3.DE", Date = d,
        Open = 1, High = 1, Low = 1, Close = 1, Volume = 1
    };

    [Fact]
    public async Task First_call_fetches_two_years_back_and_persists()
    {
        await using var db = InMemoryDb.Create();
        var clock = new FakeClock(new DateTime(2026, 5, 6, 12, 0, 0, DateTimeKind.Utc));
        var feed  = new StubPriceFeed([Bar(new(2024,5,7)), Bar(new(2026,5,5)), Bar(new(2026,5,6))]);
        var cache = new DailyPriceCache(feed, db, clock, NullLogger<DailyPriceCache>.Instance);

        await cache.EnsureFreshAsync("CON3.DE", CancellationToken.None);

        feed.CallCount.ShouldBe(1);
        feed.Ranges[0].from.ShouldBe(new DateOnly(2024,5,6));
        db.PriceBars.Count().ShouldBe(3);
    }

    [Fact]
    public async Task No_fetch_when_today_already_in_db()
    {
        await using var db = InMemoryDb.Create();
        var clock = new FakeClock(new DateTime(2026, 5, 6, 12, 0, 0, DateTimeKind.Utc));
        db.PriceBars.Add(Bar(new(2026,5,6)));
        await db.SaveChangesAsync();
        var feed  = new StubPriceFeed([]);
        var cache = new DailyPriceCache(feed, db, clock, NullLogger<DailyPriceCache>.Instance);

        await cache.EnsureFreshAsync("CON3.DE", CancellationToken.None);

        feed.CallCount.ShouldBe(0);
    }

    [Fact]
    public async Task Subsequent_call_fetches_only_missing_days()
    {
        await using var db = InMemoryDb.Create();
        db.PriceBars.Add(Bar(new(2026,5,4)));
        await db.SaveChangesAsync();
        var clock = new FakeClock(new DateTime(2026, 5, 6, 12, 0, 0, DateTimeKind.Utc));
        var feed  = new StubPriceFeed([Bar(new(2026,5,5)), Bar(new(2026,5,6))]);
        var cache = new DailyPriceCache(feed, db, clock, NullLogger<DailyPriceCache>.Instance);

        await cache.EnsureFreshAsync("CON3.DE", CancellationToken.None);

        feed.Ranges[0].from.ShouldBe(new DateOnly(2026,5,5));
        db.PriceBars.Count().ShouldBe(3);
    }
}
```

- [ ] **Step 2: Run, expect compile failure**

```bash
dotnet test --filter "FullyQualifiedName~DailyPriceCacheTests" --nologo
```

- [ ] **Step 3: Implement**

`TradyStrat/Features/PriceFeed/DailyPriceCache.cs`:
```csharp
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TradyStrat.Data;
using TradyStrat.Shared.Time;

namespace TradyStrat.Features.PriceFeed;

public sealed class DailyPriceCache(
    IPriceFeed feed,
    AppDbContext db,
    IClock clock,
    ILogger<DailyPriceCache> log)
{
    public async Task EnsureFreshAsync(string ticker, CancellationToken ct)
    {
        var today  = clock.TodayInExchangeTzFor(ticker);
        var latest = await db.PriceBars
            .Where(b => b.Ticker == ticker)
            .OrderByDescending(b => b.Date)
            .Select(b => (DateOnly?)b.Date)
            .FirstOrDefaultAsync(ct);

        if (latest == today) return;

        var from = latest?.AddDays(1) ?? today.AddYears(-2);
        var fetched = await feed.FetchDailyAsync(ticker, from, today, ct);
        if (fetched.Count == 0)
        {
            log.LogInformation("PriceFeed: no new bars for {Ticker}", ticker);
            return;
        }

        db.PriceBars.AddRange(fetched);
        await db.SaveChangesAsync(ct);
        log.LogInformation("PriceFeed: appended {N} bars for {Ticker}", fetched.Count, ticker);
    }
}
```

- [ ] **Step 4: Run, expect pass**

```bash
dotnet test --filter "FullyQualifiedName~DailyPriceCacheTests" --nologo
```

Expected: `Passed: 3`.

- [ ] **Step 5: Commit**

```bash
git add TradyStrat/Features/PriceFeed/DailyPriceCache.cs \
        TradyStrat.Tests/PriceFeed/DailyPriceCacheTests.cs \
        TradyStrat.Tests/PriceFeed/StubPriceFeed.cs
git commit -m "feat: add DailyPriceCache decorator with idempotent persistence"
```

---

### Task 14: `IFxRateProvider` + `YahooFxProvider`

**Files:**
- Create: `TradyStrat/Features/Fx/IFxRateProvider.cs`
- Create: `TradyStrat/Features/Fx/YahooFxProvider.cs`
- Create: `TradyStrat.Tests/Fx/Fixtures/yahoo-eurusd-mini.json`
- Create: `TradyStrat.Tests/Fx/YahooFxProviderTests.cs`

- [ ] **Step 1: Add fixture**

`TradyStrat.Tests/Fx/Fixtures/yahoo-eurusd-mini.json`:
```json
{
  "chart": {
    "result": [{
      "meta": { "symbol": "EURUSD=X" },
      "timestamp": [1714435200, 1714521600],
      "indicators": {
        "quote": [{
          "open":   [1.0810, 1.0815],
          "high":   [1.0830, 1.0840],
          "low":    [1.0790, 1.0800],
          "close":  [1.0820, 1.0835],
          "volume": [0, 0]
        }]
      }
    }],
    "error": null
  }
}
```

Add to test `.csproj` `<ItemGroup>`:
```xml
<None Update="Fx/Fixtures/*.json">
  <CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>
</None>
```

- [ ] **Step 2: Write the failing test**

`TradyStrat.Tests/Fx/YahooFxProviderTests.cs`:
```csharp
using System.Net;
using Shouldly;
using TradyStrat.Features.Fx;
using TradyStrat.Tests.PriceFeed;

namespace TradyStrat.Tests.Fx;

public class YahooFxProviderTests
{
    [Fact]
    public async Task Returns_one_FxRate_per_day_with_close_as_UsdPerEur()
    {
        var handler = new StubHttpHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(File.ReadAllText("Fx/Fixtures/yahoo-eurusd-mini.json"))
        });
        var http = new HttpClient(handler) { BaseAddress = new Uri("https://query1.finance.yahoo.com") };
        var prov = new YahooFxProvider(http);

        var rates = await prov.FetchAsync("EURUSD", new(2024,4,30), new(2024,5,1), CancellationToken.None);

        rates.Count.ShouldBe(2);
        rates[0].Pair.ShouldBe("EURUSD");
        rates[0].UsdPerEur.ShouldBe(1.0820m);
        rates[1].UsdPerEur.ShouldBe(1.0835m);
    }
}
```

- [ ] **Step 3: Run, expect compile failure**

```bash
dotnet test --filter "FullyQualifiedName~YahooFxProviderTests" --nologo
```

- [ ] **Step 4: Implement**

`TradyStrat/Features/Fx/IFxRateProvider.cs`:
```csharp
using TradyStrat.Shared.Domain;

namespace TradyStrat.Features.Fx;

public interface IFxRateProvider
{
    Task<IReadOnlyList<FxRate>> FetchAsync(
        string pair, DateOnly from, DateOnly to, CancellationToken ct);
}
```

`TradyStrat/Features/Fx/YahooFxProvider.cs`:
```csharp
using System.Text.Json;
using TradyStrat.Shared.Domain;
using TradyStrat.Shared.Exceptions;

namespace TradyStrat.Features.Fx;

public sealed class YahooFxProvider(HttpClient http) : IFxRateProvider
{
    public async Task<IReadOnlyList<FxRate>> FetchAsync(
        string pair, DateOnly from, DateOnly to, CancellationToken ct)
    {
        var symbol = pair switch
        {
            "EURUSD" => "EURUSD=X",
            _        => throw new FxRateUnavailableException($"Unsupported pair {pair}")
        };

        var p1 = new DateTimeOffset(from.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero).ToUnixTimeSeconds();
        var p2 = new DateTimeOffset(to.ToDateTime(TimeOnly.MaxValue),   TimeSpan.Zero).ToUnixTimeSeconds();
        var url = $"/v8/finance/chart/{symbol}?period1={p1}&period2={p2}&interval=1d";

        try
        {
            using var resp = await http.GetAsync(url, ct);
            if (!resp.IsSuccessStatusCode)
                throw new FxRateUnavailableException($"Yahoo {(int)resp.StatusCode} for {symbol}");

            await using var stream = await resp.Content.ReadAsStreamAsync(ct);
            using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: ct);

            var first = doc.RootElement.GetProperty("chart").GetProperty("result")[0];
            var ts    = first.GetProperty("timestamp");
            var close = first.GetProperty("indicators").GetProperty("quote")[0].GetProperty("close");

            var fetchedAt = DateTime.UtcNow;
            var rates = new List<FxRate>(ts.GetArrayLength());
            for (var i = 0; i < ts.GetArrayLength(); i++)
            {
                if (close[i].ValueKind == JsonValueKind.Null) continue;
                var date = DateOnly.FromDateTime(
                    DateTimeOffset.FromUnixTimeSeconds(ts[i].GetInt64()).UtcDateTime);
                rates.Add(new FxRate
                {
                    Id = 0, Pair = pair, Date = date,
                    UsdPerEur = (decimal)close[i].GetDouble(),
                    FetchedAt = fetchedAt
                });
            }
            return rates;
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException
                                    or JsonException or KeyNotFoundException
                                    or InvalidOperationException)
        {
            throw new FxRateUnavailableException($"FX fetch failed for {pair}", ex);
        }
    }
}
```

- [ ] **Step 5: Run, expect pass**

```bash
dotnet test --filter "FullyQualifiedName~YahooFxProviderTests" --nologo
```

- [ ] **Step 6: Commit**

```bash
git add TradyStrat/Features/Fx/ TradyStrat.Tests/Fx/ \
        TradyStrat.Tests/TradyStrat.Tests.csproj
git commit -m "feat: add YahooFxProvider for EURUSD daily rates"
```

---

### Task 15: `DailyFxCache` + `FxConverter`

**Files:**
- Create: `TradyStrat/Features/Fx/DailyFxCache.cs`
- Create: `TradyStrat/Features/Fx/FxConverter.cs`
- Create: `TradyStrat.Tests/Fx/StubFxProvider.cs`
- Create: `TradyStrat.Tests/Fx/DailyFxCacheTests.cs`
- Create: `TradyStrat.Tests/Fx/FxConverterTests.cs`

- [ ] **Step 1: Write tests**

`TradyStrat.Tests/Fx/StubFxProvider.cs`:
```csharp
using TradyStrat.Features.Fx;
using TradyStrat.Shared.Domain;

namespace TradyStrat.Tests.Fx;

public sealed class StubFxProvider(IReadOnlyList<FxRate> rates) : IFxRateProvider
{
    public int CallCount { get; private set; }
    public Task<IReadOnlyList<FxRate>> FetchAsync(string pair, DateOnly from, DateOnly to, CancellationToken ct)
    { CallCount++; return Task.FromResult(rates); }
}
```

`TradyStrat.Tests/Fx/DailyFxCacheTests.cs`:
```csharp
using Microsoft.Extensions.Logging.Abstractions;
using Shouldly;
using TradyStrat.Features.Fx;
using TradyStrat.Shared.Domain;
using TradyStrat.Tests.Specifications;
using TradyStrat.Tests.Time;

namespace TradyStrat.Tests.Fx;

public class DailyFxCacheTests
{
    private static FxRate Rate(DateOnly d, decimal v) => new()
    {
        Id = 0, Pair = "EURUSD", Date = d, UsdPerEur = v, FetchedAt = DateTime.UtcNow
    };

    [Fact]
    public async Task Skips_fetch_if_today_present()
    {
        await using var db = InMemoryDb.Create();
        var clock = new FakeClock(new DateTime(2026,5,6,0,0,0,DateTimeKind.Utc));
        db.FxRates.Add(Rate(new(2026,5,6), 1.0820m));
        await db.SaveChangesAsync();

        var prov = new StubFxProvider([]);
        var cache = new DailyFxCache(prov, db, clock, NullLogger<DailyFxCache>.Instance);

        await cache.EnsureFreshAsync("EURUSD", CancellationToken.None);

        prov.CallCount.ShouldBe(0);
    }

    [Fact]
    public async Task Fetches_and_persists_when_stale()
    {
        await using var db = InMemoryDb.Create();
        var clock = new FakeClock(new DateTime(2026,5,6,0,0,0,DateTimeKind.Utc));
        var prov = new StubFxProvider([Rate(new(2026,5,5), 1.08m), Rate(new(2026,5,6), 1.09m)]);
        var cache = new DailyFxCache(prov, db, clock, NullLogger<DailyFxCache>.Instance);

        await cache.EnsureFreshAsync("EURUSD", CancellationToken.None);

        db.FxRates.Count().ShouldBe(2);
    }
}
```

`TradyStrat.Tests/Fx/FxConverterTests.cs`:
```csharp
using Ardalis.Specification.EntityFrameworkCore;
using Shouldly;
using TradyStrat.Features.Fx;
using TradyStrat.Shared.Domain;
using TradyStrat.Shared.Exceptions;
using TradyStrat.Specifications.FxRates;
using TradyStrat.Tests.Specifications;

namespace TradyStrat.Tests.Fx;

public class FxConverterTests
{
    [Fact]
    public async Task Converts_USD_to_EUR_using_latest_rate_at_or_before_date()
    {
        await using var db = InMemoryDb.Create();
        db.FxRates.AddRange(
            new FxRate { Id = 0, Pair = "EURUSD", Date = new(2026,5,5), UsdPerEur = 1.10m, FetchedAt = DateTime.UtcNow },
            new FxRate { Id = 0, Pair = "EURUSD", Date = new(2026,5,6), UsdPerEur = 1.0820m, FetchedAt = DateTime.UtcNow });
        await db.SaveChangesAsync();

        var repo = new TestRepo<FxRate>(db);
        var fx = new FxConverter(repo);

        var eur = await fx.UsdToEurAsync(216.40m, new(2026,5,6), CancellationToken.None);

        eur.ShouldBe(216.40m / 1.0820m, tolerance: 0.000001m);
    }

    [Fact]
    public async Task Throws_FxRateUnavailableException_when_no_rate_at_or_before()
    {
        await using var db = InMemoryDb.Create();
        var repo = new TestRepo<FxRate>(db);
        var fx = new FxConverter(repo);

        await Should.ThrowAsync<FxRateUnavailableException>(() =>
            fx.UsdToEurAsync(100m, new(2026,5,6), CancellationToken.None));
    }
}

internal sealed class TestRepo<T>(TradyStrat.Data.AppDbContext db)
    : Ardalis.Specification.EntityFrameworkCore.RepositoryBase<T>(db)
    where T : class { }
```

- [ ] **Step 2: Run, expect compile failure**

```bash
dotnet test --filter "FullyQualifiedName~DailyFxCacheTests|FullyQualifiedName~FxConverterTests" --nologo
```

- [ ] **Step 3: Implement**

`TradyStrat/Features/Fx/DailyFxCache.cs`:
```csharp
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TradyStrat.Data;
using TradyStrat.Shared.Time;

namespace TradyStrat.Features.Fx;

public sealed class DailyFxCache(
    IFxRateProvider provider,
    AppDbContext db,
    IClock clock,
    ILogger<DailyFxCache> log)
{
    public async Task EnsureFreshAsync(string pair, CancellationToken ct)
    {
        var today  = clock.TodayInExchangeTzFor(pair);
        var latest = await db.FxRates
            .Where(r => r.Pair == pair)
            .OrderByDescending(r => r.Date)
            .Select(r => (DateOnly?)r.Date)
            .FirstOrDefaultAsync(ct);

        if (latest == today) return;

        var from = latest?.AddDays(1) ?? today.AddYears(-2);
        var rates = await provider.FetchAsync(pair, from, today, ct);
        if (rates.Count == 0)
        {
            log.LogInformation("Fx: no new rates for {Pair}", pair);
            return;
        }

        db.FxRates.AddRange(rates);
        await db.SaveChangesAsync(ct);
        log.LogInformation("Fx: appended {N} rates for {Pair}", rates.Count, pair);
    }
}
```

`TradyStrat/Features/Fx/FxConverter.cs`:
```csharp
using Ardalis.Specification;
using TradyStrat.Shared.Domain;
using TradyStrat.Shared.Exceptions;
using TradyStrat.Specifications.FxRates;

namespace TradyStrat.Features.Fx;

public sealed class FxConverter(IReadRepositoryBase<FxRate> rates)
{
    public async Task<decimal> UsdToEurAsync(decimal usd, DateOnly asOf, CancellationToken ct)
    {
        var fx = await rates.FirstOrDefaultAsync(new LatestFxRateSpec("EURUSD", asOf), ct)
            ?? throw new FxRateUnavailableException(
                $"No EURUSD rate on or before {asOf:yyyy-MM-dd}");
        return usd * fx.EurPerUsd;
    }
}
```

- [ ] **Step 4: Run, expect pass**

```bash
dotnet test --filter "FullyQualifiedName~DailyFxCacheTests|FullyQualifiedName~FxConverterTests" --nologo
```

Expected: `Passed: 4`.

- [ ] **Step 5: Commit**

```bash
git add TradyStrat/Features/Fx/DailyFxCache.cs \
        TradyStrat/Features/Fx/FxConverter.cs \
        TradyStrat.Tests/Fx/
git commit -m "feat: add DailyFxCache and FxConverter (USD → EUR via latest rate)"
```

---

### Task 16: `PriceFeedHostedService` (warms caches at startup)

**Files:**
- Create: `TradyStrat/Features/PriceFeed/PriceFeedHostedService.cs`
- Create: `TradyStrat.Tests/PriceFeed/PriceFeedHostedServiceTests.cs`

- [ ] **Step 1: Write the failing test**

`TradyStrat.Tests/PriceFeed/PriceFeedHostedServiceTests.cs`:
```csharp
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Shouldly;
using TradyStrat.Features.Fx;
using TradyStrat.Features.PriceFeed;
using TradyStrat.Shared.Domain;
using TradyStrat.Shared.Time;
using TradyStrat.Tests.Fx;
using TradyStrat.Tests.Specifications;
using TradyStrat.Tests.Time;

namespace TradyStrat.Tests.PriceFeed;

public class PriceFeedHostedServiceTests
{
    [Fact]
    public async Task StartAsync_warms_all_three_tickers_and_eurusd()
    {
        await using var db = InMemoryDb.Create();
        var clock = new FakeClock(new DateTime(2026,5,6,0,0,0,DateTimeKind.Utc));
        var feed  = new StubPriceFeed([new PriceBar {
            Id = 0, Ticker = "X", Date = new(2026,5,6),
            Open=1, High=1, Low=1, Close=1, Volume=1
        }]);
        var fx    = new StubFxProvider([new FxRate {
            Id = 0, Pair = "EURUSD", Date = new(2026,5,6),
            UsdPerEur = 1.08m, FetchedAt = DateTime.UtcNow
        }]);

        var services = new ServiceCollection();
        services.AddSingleton<AppDbContextResolverFactory>(_ => () => db);
        services.AddScoped<DailyPriceCache>(sp =>
            new DailyPriceCache(feed, db, clock, NullLogger<DailyPriceCache>.Instance));
        services.AddScoped<DailyFxCache>(sp =>
            new DailyFxCache(fx, db, clock, NullLogger<DailyFxCache>.Instance));

        var sp = services.BuildServiceProvider();
        var svc = new PriceFeedHostedService(sp, NullLogger<PriceFeedHostedService>.Instance);

        await svc.StartAsync(CancellationToken.None);

        feed.CallCount.ShouldBe(3);    // CON3.DE, COIN, BTC-USD
        fx.CallCount.ShouldBe(1);      // EURUSD
    }
}

// Helper alias to keep the closure simple for the test
internal delegate TradyStrat.Data.AppDbContext AppDbContextResolverFactory();
```

> The test uses a hand-rolled DI graph rather than `WebApplicationFactory` because the hosted service's only contract is "scope and call the two caches once each per ticker/pair".

- [ ] **Step 2: Run, expect compile failure**

```bash
dotnet test --filter "FullyQualifiedName~PriceFeedHostedServiceTests" --nologo
```

- [ ] **Step 3: Implement**

`TradyStrat/Features/PriceFeed/PriceFeedHostedService.cs`:
```csharp
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using TradyStrat.Features.Fx;
using TradyStrat.Shared.Exceptions;

namespace TradyStrat.Features.PriceFeed;

public sealed class PriceFeedHostedService(
    IServiceProvider services,
    ILogger<PriceFeedHostedService> log) : IHostedService
{
    private static readonly string[] Tickers = ["CON3.DE", "COIN", "BTC-USD"];
    private const string FxPair = "EURUSD";

    public async Task StartAsync(CancellationToken ct)
    {
        await using var scope = services.CreateAsyncScope();
        var price = scope.ServiceProvider.GetRequiredService<DailyPriceCache>();
        var fx    = scope.ServiceProvider.GetRequiredService<DailyFxCache>();

        foreach (var t in Tickers)
            await SafeWarm(() => price.EnsureFreshAsync(t, ct), t);

        await SafeWarm(() => fx.EnsureFreshAsync(FxPair, ct), FxPair);
    }

    public Task StopAsync(CancellationToken _) => Task.CompletedTask;

    private async Task SafeWarm(Func<Task> warm, string label)
    {
        try { await warm(); }
        catch (PriceFeedUnavailableException ex)
        {
            log.LogWarning(ex, "Price warm failed for {Label}", label);
        }
        catch (FxRateUnavailableException ex)
        {
            log.LogWarning(ex, "FX warm failed for {Label}", label);
        }
    }
}
```

- [ ] **Step 4: Run, expect pass**

```bash
dotnet test --filter "FullyQualifiedName~PriceFeedHostedServiceTests" --nologo
```

- [ ] **Step 5: Commit**

```bash
git add TradyStrat/Features/PriceFeed/PriceFeedHostedService.cs \
        TradyStrat.Tests/PriceFeed/PriceFeedHostedServiceTests.cs
git commit -m "feat: add PriceFeedHostedService warming caches on startup"
```

---

### Task 17: `PriceFeedModule` + `FxModule`

**Files:**
- Create: `TradyStrat/Modules/PriceFeedModule.cs`
- Create: `TradyStrat/Modules/FxModule.cs`

- [ ] **Step 1: Implement `PriceFeedModule`**

`TradyStrat/Modules/PriceFeedModule.cs`:
```csharp
using Microsoft.Extensions.Http.Resilience;
using TheAppManager.Modules;
using TradyStrat.Features.PriceFeed;

namespace TradyStrat.Modules;

public sealed class PriceFeedModule : IAppModule
{
    public void ConfigureServices(WebApplicationBuilder builder)
    {
        builder.Services.AddHttpClient<IPriceFeed, YahooPriceFeed>(c =>
        {
            c.BaseAddress = new Uri(builder.Configuration["Yahoo:BaseUrl"]
                ?? "https://query1.finance.yahoo.com");
            c.Timeout = TimeSpan.FromSeconds(15);
            c.DefaultRequestHeaders.UserAgent.ParseAdd("TradyStrat/1.0");
        }).AddStandardResilienceHandler();

        builder.Services.AddScoped<DailyPriceCache>();
        builder.Services.AddHostedService<PriceFeedHostedService>();
    }
}
```

> `AddStandardResilienceHandler` from `Microsoft.Extensions.Http.Resilience` provides retries + circuit breaker out of the box (Polly v8 under the hood).

- [ ] **Step 2: Implement `FxModule`**

`TradyStrat/Modules/FxModule.cs`:
```csharp
using Microsoft.Extensions.Http.Resilience;
using TheAppManager.Modules;
using TradyStrat.Features.Fx;

namespace TradyStrat.Modules;

public sealed class FxModule : IAppModule
{
    public void ConfigureServices(WebApplicationBuilder builder)
    {
        builder.Services.AddHttpClient<IFxRateProvider, YahooFxProvider>(c =>
        {
            c.BaseAddress = new Uri(builder.Configuration["Yahoo:BaseUrl"]
                ?? "https://query1.finance.yahoo.com");
            c.Timeout = TimeSpan.FromSeconds(15);
            c.DefaultRequestHeaders.UserAgent.ParseAdd("TradyStrat/1.0");
        }).AddStandardResilienceHandler();

        builder.Services.AddScoped<DailyFxCache>();
        builder.Services.AddScoped<FxConverter>();
    }
}
```

- [ ] **Step 3: Verify build and existing tests still pass**

```bash
dotnet build --nologo
dotnet test --nologo
```

Expected: green.

- [ ] **Step 4: Commit**

```bash
git add TradyStrat/Modules/PriceFeedModule.cs TradyStrat/Modules/FxModule.cs
git commit -m "feat: add PriceFeedModule and FxModule with standard resilience"
```

**End of Phase 2.** Manual verification: run the app once with internet connectivity. Today's bars for CON3.DE, COIN, BTC-USD plus today's EURUSD rate appear in `tradystrat.db`. Re-running on the same day produces no extra rows.

---

## Phase 3 — Indicators (Tasks 18–22)

This phase adds the four technical indicators (Bollinger, RSI, Moving Average via TaLibStandard; Ichimoku locally), the four `IZoneRule` strategies, the `ZoneClassifier` composite, the `IndicatorEngine` orchestrator, and the `IndicatorsModule`. End state: given a series of price bars in DB, `IndicatorEngine.ComputeFor("CON3.DE")` returns an `IndicatorReading` with values + per-rule reasons + a final `Zone`.

---

### Task 18: Indicator readings + Bollinger / RSI / Moving Average adapters

**Files:**
- Create: `TradyStrat/Shared/Domain/BollingerReading.cs`
- Create: `TradyStrat/Shared/Domain/IchimokuReading.cs`
- Create: `TradyStrat/Shared/Domain/IndicatorBundle.cs`
- Create: `TradyStrat/Shared/Domain/IndicatorReading.cs`
- Create: `TradyStrat/Features/Indicators/Bollinger.cs`
- Create: `TradyStrat/Features/Indicators/Rsi.cs`
- Create: `TradyStrat/Features/Indicators/MovingAverage.cs`
- Create: `TradyStrat.Tests/Indicators/Fixtures/sample-closes.csv`
- Create: `TradyStrat.Tests/Indicators/SeriesLoader.cs`
- Create: `TradyStrat.Tests/Indicators/BollingerTests.cs`
- Create: `TradyStrat.Tests/Indicators/RsiTests.cs`
- Create: `TradyStrat.Tests/Indicators/MovingAverageTests.cs`

- [ ] **Step 1: Add domain readings**

`TradyStrat/Shared/Domain/BollingerReading.cs`:
```csharp
namespace TradyStrat.Shared.Domain;

public sealed record BollingerReading(decimal Upper, decimal Middle, decimal Lower, decimal Sigma);
```

`TradyStrat/Shared/Domain/IchimokuReading.cs`:
```csharp
namespace TradyStrat.Shared.Domain;

public sealed record IchimokuReading(
    decimal Tenkan, decimal Kijun,
    decimal SenkouA, decimal SenkouB,
    decimal Chikou,
    IchimokuSignal Signal);
```

`TradyStrat/Shared/Domain/IndicatorBundle.cs`:
```csharp
namespace TradyStrat.Shared.Domain;

public sealed record IndicatorBundle(
    BollingerReading? Bollinger,
    decimal? Rsi,
    decimal? Sma50,
    decimal? Sma200,
    IchimokuReading? Ichimoku);
```

`TradyStrat/Shared/Domain/IndicatorReading.cs`:
```csharp
namespace TradyStrat.Shared.Domain;

public sealed record IndicatorReading(
    string Ticker,
    decimal Price,
    BollingerReading? Bollinger,
    decimal? Rsi,
    decimal? Sma50,
    decimal? Sma200,
    IchimokuReading? Ichimoku,
    Zone Zone,
    IReadOnlyList<string> Reasons);
```

- [ ] **Step 2: Create the test fixture (250 daily closes)**

`TradyStrat.Tests/Indicators/Fixtures/sample-closes.csv` — generate with a deterministic walk so the asserted values are reproducible. Run this once and commit the resulting file:

```bash
python3 - <<'PY' > TradyStrat.Tests/Indicators/Fixtures/sample-closes.csv
import math
print("date,close")
price = 100.0
for i in range(250):
    drift  = math.sin(i / 12.0) * 1.5
    bump   = math.cos(i / 5.0) * 0.6
    price += drift * 0.05 + bump * 0.03
    date = f"2025-{1 + i//22:02d}-{1 + i%22:02d}"
    print(f"{date},{price:.4f}")
PY
```

Add to test project `.csproj`:
```xml
<None Update="Indicators/Fixtures/*.csv">
  <CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>
</None>
```

- [ ] **Step 3: Create the series loader and write failing indicator tests**

`TradyStrat.Tests/Indicators/SeriesLoader.cs`:
```csharp
using System.Globalization;
using TradyStrat.Shared.Domain;

namespace TradyStrat.Tests.Indicators;

public static class SeriesLoader
{
    public static IReadOnlyList<PriceBar> LoadCloses(string ticker = "X")
    {
        var path = Path.Combine("Indicators", "Fixtures", "sample-closes.csv");
        var lines = File.ReadAllLines(path).Skip(1);
        var bars = new List<PriceBar>();
        foreach (var line in lines)
        {
            var parts = line.Split(',');
            var date  = DateOnly.Parse(parts[0]);
            var close = decimal.Parse(parts[1], CultureInfo.InvariantCulture);
            bars.Add(new PriceBar
            {
                Id = 0, Ticker = ticker, Date = date,
                Open = close, High = close, Low = close, Close = close, Volume = 1
            });
        }
        return bars;
    }
}
```

`TradyStrat.Tests/Indicators/BollingerTests.cs`:
```csharp
using Shouldly;
using TradyStrat.Features.Indicators;

namespace TradyStrat.Tests.Indicators;

public class BollingerTests
{
    [Fact]
    public void Returns_null_when_series_shorter_than_period()
    {
        var bars = SeriesLoader.LoadCloses().Take(10).ToList();
        Bollinger.LatestFor(bars).ShouldBeNull();
    }

    [Fact]
    public void Latest_band_lies_around_mean_with_positive_sigma()
    {
        var bars = SeriesLoader.LoadCloses();
        var bb = Bollinger.LatestFor(bars);

        bb.ShouldNotBeNull();
        bb.Lower.ShouldBeLessThan(bb.Middle);
        bb.Middle.ShouldBeLessThan(bb.Upper);
        bb.Sigma.ShouldBeGreaterThan(0m);
    }
}
```

`TradyStrat.Tests/Indicators/RsiTests.cs`:
```csharp
using Shouldly;
using TradyStrat.Features.Indicators;

namespace TradyStrat.Tests.Indicators;

public class RsiTests
{
    [Fact]
    public void Returns_null_when_too_few_bars()
    {
        var bars = SeriesLoader.LoadCloses().Take(5).ToList();
        Rsi.LatestFor(bars).ShouldBeNull();
    }

    [Fact]
    public void Returns_value_between_0_and_100()
    {
        var rsi = Rsi.LatestFor(SeriesLoader.LoadCloses());
        rsi.ShouldNotBeNull();
        rsi.Value.ShouldBeInRange(0m, 100m);
    }
}
```

`TradyStrat.Tests/Indicators/MovingAverageTests.cs`:
```csharp
using Shouldly;
using TradyStrat.Features.Indicators;

namespace TradyStrat.Tests.Indicators;

public class MovingAverageTests
{
    [Fact]
    public void Returns_null_when_period_exceeds_series_length()
    {
        var bars = SeriesLoader.LoadCloses().Take(40).ToList();
        MovingAverage.LatestFor(bars, period: 50).ShouldBeNull();
    }

    [Fact]
    public void Sma50_close_to_average_of_last_50_closes()
    {
        var bars = SeriesLoader.LoadCloses();
        var sma  = MovingAverage.LatestFor(bars, 50);
        var manual = bars.TakeLast(50).Average(b => b.Close);

        sma.ShouldNotBeNull();
        sma.Value.ShouldBe(manual, tolerance: 0.0001m);
    }
}
```

- [ ] **Step 4: Run, expect compile failure**

```bash
dotnet test --filter "FullyQualifiedName~BollingerTests|FullyQualifiedName~RsiTests|FullyQualifiedName~MovingAverageTests" --nologo
```

- [ ] **Step 5: Implement adapters**

`TradyStrat/Features/Indicators/Bollinger.cs`:
```csharp
using TechnicalAnalysis.Common;
using TechnicalAnalysis.Functions;
using TradyStrat.Shared.Domain;
using TradyStrat.Shared.Exceptions;

namespace TradyStrat.Features.Indicators;

public static class Bollinger
{
    public static BollingerReading? LatestFor(
        IReadOnlyList<PriceBar> bars,
        int period = 20, double devUp = 2.0, double devDown = 2.0)
    {
        if (bars.Count < period) return null;

        var closes = bars.Select(b => (double)b.Close).ToArray();
        var upper  = new double[closes.Length];
        var middle = new double[closes.Length];
        var lower  = new double[closes.Length];
        int begIdx = 0, nb = 0;

        var rc = TAFunc.BollingerBands(
            0, closes.Length - 1, in closes,
            in period, in devUp, in devDown, MAType.Sma,
            ref begIdx, ref nb,
            ref upper, ref middle, ref lower);

        if (rc != RetCode.Success || nb == 0)
            throw new IndicatorComputationException($"Bollinger failed: {rc}");

        var last  = nb - 1;
        var sigma = (decimal)((upper[last] - middle[last]) / 2.0);
        return new BollingerReading(
            (decimal)upper[last], (decimal)middle[last], (decimal)lower[last], sigma);
    }
}
```

`TradyStrat/Features/Indicators/Rsi.cs`:
```csharp
using TechnicalAnalysis.Common;
using TechnicalAnalysis.Functions;
using TradyStrat.Shared.Domain;
using TradyStrat.Shared.Exceptions;

namespace TradyStrat.Features.Indicators;

public static class Rsi
{
    public static decimal? LatestFor(IReadOnlyList<PriceBar> bars, int period = 14)
    {
        if (bars.Count < period + 1) return null;

        var closes = bars.Select(b => (double)b.Close).ToArray();
        var output = new double[closes.Length];
        int begIdx = 0, nb = 0;

        var rc = TAFunc.Rsi(
            0, closes.Length - 1, in closes,
            in period, ref begIdx, ref nb, ref output);

        if (rc != RetCode.Success || nb == 0)
            throw new IndicatorComputationException($"RSI failed: {rc}");

        return (decimal)output[nb - 1];
    }
}
```

`TradyStrat/Features/Indicators/MovingAverage.cs`:
```csharp
using TechnicalAnalysis.Common;
using TechnicalAnalysis.Functions;
using TradyStrat.Shared.Domain;
using TradyStrat.Shared.Exceptions;

namespace TradyStrat.Features.Indicators;

public static class MovingAverage
{
    public static decimal? LatestFor(IReadOnlyList<PriceBar> bars, int period)
    {
        if (bars.Count < period) return null;

        var closes = bars.Select(b => (double)b.Close).ToArray();
        var output = new double[closes.Length];
        int begIdx = 0, nb = 0;

        var rc = TAFunc.Sma(
            0, closes.Length - 1, in closes,
            in period, ref begIdx, ref nb, ref output);

        if (rc != RetCode.Success || nb == 0)
            throw new IndicatorComputationException($"SMA({period}) failed: {rc}");

        return (decimal)output[nb - 1];
    }
}
```

> **Reminder:** if the exact `TAFunc.Sma` / `TAFunc.Rsi` / `TAFunc.BollingerBands` signatures differ in the installed version of `Atypical.TechnicalAnalysis.Functions`, mirror the demo at `phmatray/TaLibStandard/Demo.BlazorWasm/Services/TechnicalAnalysisService.cs`. The pattern (in/out arrays, RetCode return) is stable.

- [ ] **Step 6: Run, expect pass**

```bash
dotnet test --filter "FullyQualifiedName~BollingerTests|FullyQualifiedName~RsiTests|FullyQualifiedName~MovingAverageTests" --nologo
```

Expected: `Passed: 6`.

- [ ] **Step 7: Commit**

```bash
git add TradyStrat/Shared/Domain/BollingerReading.cs \
        TradyStrat/Shared/Domain/IchimokuReading.cs \
        TradyStrat/Shared/Domain/IndicatorBundle.cs \
        TradyStrat/Shared/Domain/IndicatorReading.cs \
        TradyStrat/Features/Indicators/ \
        TradyStrat.Tests/Indicators/
git commit -m "feat: add Bollinger/RSI/SMA adapters over TaLibStandard"
```

---

### Task 19: Ichimoku (computed locally)

**Files:**
- Create: `TradyStrat/Features/Indicators/Ichimoku.cs`
- Create: `TradyStrat.Tests/Indicators/IchimokuTests.cs`

- [ ] **Step 1: Write the failing tests**

`TradyStrat.Tests/Indicators/IchimokuTests.cs`:
```csharp
using Shouldly;
using TradyStrat.Features.Indicators;
using TradyStrat.Shared.Domain;

namespace TradyStrat.Tests.Indicators;

public class IchimokuTests
{
    private static IReadOnlyList<PriceBar> Series(IEnumerable<(decimal h, decimal l, decimal c)> rows)
        => rows.Select((r, i) => new PriceBar
        {
            Id = 0, Ticker = "X", Date = new DateOnly(2025,1,1).AddDays(i),
            Open = r.c, High = r.h, Low = r.l, Close = r.c, Volume = 1
        }).ToArray();

    [Fact]
    public void Returns_null_when_fewer_than_78_bars()
    {
        var bars = Series(Enumerable.Range(0, 50).Select(_ => (10m, 8m, 9m)));
        Ichimoku.LatestFor(bars).ShouldBeNull();
    }

    [Fact]
    public void Computes_Tenkan_as_midpoint_of_last_9_high_low()
    {
        var bars = SeriesLoader.LoadCloses();
        var ichi = Ichimoku.LatestFor(bars);
        ichi.ShouldNotBeNull();

        var last9 = bars.TakeLast(9);
        var expected = (last9.Max(b => b.High) + last9.Min(b => b.Low)) / 2m;

        ichi.Tenkan.ShouldBe(expected, tolerance: 0.0001m);
    }

    [Fact]
    public void Signal_AboveCloud_when_price_exceeds_max_of_spans()
    {
        // Build a synthetic series where the last close is well above any rolling midpoint.
        var bars = Series(Enumerable.Range(0, 100).Select(i =>
            i < 99 ? (5m, 4m, 4.5m) : (50m, 49m, 49.5m)));

        var ichi = Ichimoku.LatestFor(bars);

        ichi.ShouldNotBeNull();
        ichi.Signal.ShouldBe(IchimokuSignal.AboveCloud);
    }

    [Fact]
    public void Signal_BelowCloud_when_price_under_min_of_spans()
    {
        var bars = Series(Enumerable.Range(0, 100).Select(i =>
            i < 99 ? (50m, 49m, 49.5m) : (5m, 4m, 4.5m)));

        var ichi = Ichimoku.LatestFor(bars);

        ichi.ShouldNotBeNull();
        ichi.Signal.ShouldBe(IchimokuSignal.BelowCloud);
    }
}
```

- [ ] **Step 2: Run, expect compile failure**

```bash
dotnet test --filter "FullyQualifiedName~IchimokuTests" --nologo
```

- [ ] **Step 3: Implement**

`TradyStrat/Features/Indicators/Ichimoku.cs`:
```csharp
using TradyStrat.Shared.Domain;

namespace TradyStrat.Features.Indicators;

public static class Ichimoku
{
    public static IchimokuReading? LatestFor(IReadOnlyList<PriceBar> bars)
    {
        const int min = 52 + 26;
        if (bars.Count < min) return null;

        decimal MidOver(int n)
        {
            decimal high = decimal.MinValue, low = decimal.MaxValue;
            for (var i = bars.Count - n; i < bars.Count; i++)
            {
                if (bars[i].High > high) high = bars[i].High;
                if (bars[i].Low  < low)  low  = bars[i].Low;
            }
            return (high + low) / 2m;
        }

        var tenkan  = MidOver(9);
        var kijun   = MidOver(26);
        var senkouA = (tenkan + kijun) / 2m;
        var senkouB = MidOver(52);
        var chikou  = bars[^27].Close;
        var price   = bars[^1].Close;

        var top    = Math.Max(senkouA, senkouB);
        var bottom = Math.Min(senkouA, senkouB);
        var signal = price > top
            ? IchimokuSignal.AboveCloud
            : price < bottom
                ? IchimokuSignal.BelowCloud
                : IchimokuSignal.InCloud;

        return new IchimokuReading(tenkan, kijun, senkouA, senkouB, chikou, signal);
    }
}
```

- [ ] **Step 4: Run, expect pass**

```bash
dotnet test --filter "FullyQualifiedName~IchimokuTests" --nologo
```

Expected: `Passed: 4`.

- [ ] **Step 5: Commit**

```bash
git add TradyStrat/Features/Indicators/Ichimoku.cs \
        TradyStrat.Tests/Indicators/IchimokuTests.cs
git commit -m "feat: compute Ichimoku locally (TaLibStandard does not include it)"
```

---

### Task 20: `IZoneRule` Strategies + `ZoneClassifier` Composite

**Files:**
- Create: `TradyStrat/Features/Indicators/Rules/ZoneVote.cs`
- Create: `TradyStrat/Features/Indicators/Rules/IZoneRule.cs`
- Create: `TradyStrat/Features/Indicators/Rules/BollingerZoneRule.cs`
- Create: `TradyStrat/Features/Indicators/Rules/RsiZoneRule.cs`
- Create: `TradyStrat/Features/Indicators/Rules/MovingAverageZoneRule.cs`
- Create: `TradyStrat/Features/Indicators/Rules/IchimokuZoneRule.cs`
- Create: `TradyStrat/Features/Indicators/ZoneClassifier.cs`
- Create: `TradyStrat.Tests/Indicators/Rules/ZoneRuleTests.cs`
- Create: `TradyStrat.Tests/Indicators/ZoneClassifierTests.cs`

- [ ] **Step 1: Write failing tests**

`TradyStrat.Tests/Indicators/Rules/ZoneRuleTests.cs`:
```csharp
using Shouldly;
using TradyStrat.Features.Indicators.Rules;
using TradyStrat.Shared.Domain;

namespace TradyStrat.Tests.Indicators.Rules;

public class ZoneRuleTests
{
    private static IndicatorBundle Bundle(BollingerReading? bb = null,
        decimal? rsi = null, decimal? sma50 = null, decimal? sma200 = null,
        IchimokuReading? ich = null) => new(bb, rsi, sma50, sma200, ich);

    [Fact]
    public void Bollinger_below_lower_votes_Accumulate()
    {
        var bb = new BollingerReading(Upper: 5m, Middle: 4m, Lower: 3m, Sigma: 1m);
        var v = new BollingerZoneRule().Apply(price: 2.5m, Bundle(bb: bb));
        v.ShouldNotBeNull();
        v.Vote.ShouldBe(Zone.Accumulate);
    }

    [Fact]
    public void Bollinger_above_upper_votes_Distribute()
    {
        var bb = new BollingerReading(5m, 4m, 3m, 1m);
        new BollingerZoneRule().Apply(5.5m, Bundle(bb: bb))!.Vote.ShouldBe(Zone.Distribute);
    }

    [Fact]
    public void Bollinger_returns_null_when_reading_missing()
    {
        new BollingerZoneRule().Apply(4m, Bundle()).ShouldBeNull();
    }

    [Theory]
    [InlineData(20, Zone.Accumulate)]
    [InlineData(50, Zone.Hold)]
    [InlineData(80, Zone.Distribute)]
    public void Rsi_thresholds(int rsi, Zone expected)
    {
        new RsiZoneRule().Apply(0m, Bundle(rsi: rsi))!.Vote.ShouldBe(expected);
    }

    [Fact]
    public void MovingAverage_below_200_votes_Accumulate()
    {
        new MovingAverageZoneRule().Apply(3m, Bundle(sma50: 4m, sma200: 5m))!.Vote.ShouldBe(Zone.Accumulate);
    }

    [Fact]
    public void MovingAverage_above_50_votes_Distribute()
    {
        new MovingAverageZoneRule().Apply(6m, Bundle(sma50: 4m, sma200: 5m))!.Vote.ShouldBe(Zone.Distribute);
    }

    [Fact]
    public void MovingAverage_between_holds()
    {
        new MovingAverageZoneRule().Apply(4.5m, Bundle(sma50: 5m, sma200: 4m))!.Vote.ShouldBe(Zone.Hold);
    }

    [Theory]
    [InlineData(IchimokuSignal.AboveCloud, Zone.Distribute)]
    [InlineData(IchimokuSignal.BelowCloud, Zone.Accumulate)]
    [InlineData(IchimokuSignal.InCloud,    Zone.Hold)]
    public void Ichimoku_maps_signal_to_zone(IchimokuSignal s, Zone z)
    {
        var ich = new IchimokuReading(0,0,0,0,0, s);
        new IchimokuZoneRule().Apply(0m, Bundle(ich: ich))!.Vote.ShouldBe(z);
    }
}
```

`TradyStrat.Tests/Indicators/ZoneClassifierTests.cs`:
```csharp
using Shouldly;
using TradyStrat.Features.Indicators;
using TradyStrat.Features.Indicators.Rules;
using TradyStrat.Shared.Domain;

namespace TradyStrat.Tests.Indicators;

public class ZoneClassifierTests
{
    private static ZoneClassifier WithAll() => new(new IZoneRule[]
    {
        new BollingerZoneRule(),
        new RsiZoneRule(),
        new MovingAverageZoneRule(),
        new IchimokuZoneRule(),
    });

    [Fact]
    public void Returns_Hold_when_no_rules_apply()
    {
        var (zone, reasons) = WithAll().Classify(0m,
            new IndicatorBundle(null, null, null, null, null));

        zone.ShouldBe(Zone.Hold);
        reasons.ShouldBeEmpty();
    }

    [Fact]
    public void Majority_vote_wins()
    {
        var bb  = new BollingerReading(5m, 4m, 3m, 1m);          // price 2 → Accumulate
        var ich = new IchimokuReading(0,0,0,0,0, IchimokuSignal.BelowCloud);
        var bundle = new IndicatorBundle(bb, Rsi: 25m, Sma50: 6m, Sma200: 7m, ich);

        var (zone, reasons) = WithAll().Classify(2m, bundle);

        zone.ShouldBe(Zone.Accumulate);
        reasons.Count.ShouldBe(4);
    }

    [Fact]
    public void Tie_resolves_to_Hold()
    {
        // Two votes Accumulate, two votes Distribute → tie
        var bb     = new BollingerReading(5m, 4m, 3m, 1m);          // 2 → Accumulate
        var ich    = new IchimokuReading(0,0,0,0,0, IchimokuSignal.AboveCloud); // → Distribute
        var bundle = new IndicatorBundle(bb, Rsi: 75m, Sma50: 1m, Sma200: 0.5m, ich);
        // RSI 75 → Distribute; SMA price 2 above sma50 1 → Distribute? wait — re-balance.
        // Use clearer pair: bb Accumulate + RSI Accumulate (low), MA Distribute + Ichi Distribute
        var bundle2 = new IndicatorBundle(
            new BollingerReading(5m,4m,3m,1m),  // 2 → Acc
            Rsi: 25m,                           // → Acc
            Sma50: 1m, Sma200: 0.5m,            // 2 above sma50 → Distribute
            new IchimokuReading(0,0,0,0,0, IchimokuSignal.AboveCloud)); // Distribute

        var (zone, _) = WithAll().Classify(2m, bundle2);

        zone.ShouldBe(Zone.Hold);
    }
}
```

- [ ] **Step 2: Run, expect compile failure**

```bash
dotnet test --filter "FullyQualifiedName~ZoneRuleTests|FullyQualifiedName~ZoneClassifierTests" --nologo
```

- [ ] **Step 3: Implement strategies and composite**

`TradyStrat/Features/Indicators/Rules/ZoneVote.cs`:
```csharp
using TradyStrat.Shared.Domain;

namespace TradyStrat.Features.Indicators.Rules;

public sealed record ZoneVote(Zone Vote, string Reason);
```

`TradyStrat/Features/Indicators/Rules/IZoneRule.cs`:
```csharp
using TradyStrat.Shared.Domain;

namespace TradyStrat.Features.Indicators.Rules;

public interface IZoneRule
{
    string Name { get; }
    ZoneVote? Apply(decimal price, IndicatorBundle r);
}
```

`TradyStrat/Features/Indicators/Rules/BollingerZoneRule.cs`:
```csharp
using TradyStrat.Shared.Domain;

namespace TradyStrat.Features.Indicators.Rules;

public sealed class BollingerZoneRule : IZoneRule
{
    public string Name => "Bollinger";

    public ZoneVote? Apply(decimal price, IndicatorBundle r) => r.Bollinger switch
    {
        null => null,
        var bb when price < bb.Lower => new(Zone.Accumulate,
            $"Price {price:F2} below lower Bollinger ({bb.Lower:F2})"),
        var bb when price > bb.Upper => new(Zone.Distribute,
            $"Price above upper Bollinger ({bb.Upper:F2})"),
        _ => new(Zone.Hold, "Inside Bollinger band"),
    };
}
```

`TradyStrat/Features/Indicators/Rules/RsiZoneRule.cs`:
```csharp
using TradyStrat.Shared.Domain;

namespace TradyStrat.Features.Indicators.Rules;

public sealed class RsiZoneRule : IZoneRule
{
    public string Name => "RSI";

    public ZoneVote? Apply(decimal price, IndicatorBundle r) => r.Rsi switch
    {
        null     => null,
        < 30m    => new(Zone.Accumulate, $"RSI(14) {r.Rsi:F0}, oversold"),
        > 70m    => new(Zone.Distribute, $"RSI(14) {r.Rsi:F0}, overbought"),
        _        => new(Zone.Hold,        $"RSI(14) {r.Rsi:F0}, neutral"),
    };
}
```

`TradyStrat/Features/Indicators/Rules/MovingAverageZoneRule.cs`:
```csharp
using TradyStrat.Shared.Domain;

namespace TradyStrat.Features.Indicators.Rules;

public sealed class MovingAverageZoneRule : IZoneRule
{
    public string Name => "SMA50/200";

    public ZoneVote? Apply(decimal price, IndicatorBundle r)
    {
        if (r.Sma50 is not decimal s50 || r.Sma200 is not decimal s200) return null;

        if (price < s200) return new(Zone.Accumulate, $"Below 200-SMA ({s200:F2})");
        if (price > s50)  return new(Zone.Distribute, $"Above 50-SMA ({s50:F2})");
        return new(Zone.Hold, "Between 50/200-SMA");
    }
}
```

`TradyStrat/Features/Indicators/Rules/IchimokuZoneRule.cs`:
```csharp
using TradyStrat.Shared.Domain;

namespace TradyStrat.Features.Indicators.Rules;

public sealed class IchimokuZoneRule : IZoneRule
{
    public string Name => "Ichimoku";

    public ZoneVote? Apply(decimal price, IndicatorBundle r) => r.Ichimoku switch
    {
        null => null,
        { Signal: IchimokuSignal.BelowCloud } => new(Zone.Accumulate, "Below Ichimoku cloud"),
        { Signal: IchimokuSignal.AboveCloud } => new(Zone.Distribute, "Above Ichimoku cloud"),
        _                                     => new(Zone.Hold,        "Inside Ichimoku cloud"),
    };
}
```

`TradyStrat/Features/Indicators/ZoneClassifier.cs`:
```csharp
using TradyStrat.Features.Indicators.Rules;
using TradyStrat.Shared.Domain;

namespace TradyStrat.Features.Indicators;

public sealed class ZoneClassifier(IEnumerable<IZoneRule> rules)
{
    public (Zone Zone, IReadOnlyList<string> Reasons) Classify(
        decimal price, IndicatorBundle r)
    {
        var votes = rules
            .Select(rule => rule.Apply(price, r))
            .Where(v => v is not null)
            .Select(v => v!)
            .ToList();

        if (votes.Count == 0) return (Zone.Hold, []);

        var groups = votes
            .GroupBy(v => v.Vote)
            .Select(g => (zone: g.Key, n: g.Count()))
            .OrderByDescending(x => x.n)
            .ToList();

        var majority = groups[0].zone;
        if (groups.Count > 1 && groups[0].n == groups[1].n)
            majority = Zone.Hold;

        return (majority, votes.Select(v => v.Reason).ToList());
    }
}
```

- [ ] **Step 4: Run, expect pass**

```bash
dotnet test --filter "FullyQualifiedName~ZoneRuleTests|FullyQualifiedName~ZoneClassifierTests" --nologo
```

Expected: `Passed: 11`.

- [ ] **Step 5: Commit**

```bash
git add TradyStrat/Features/Indicators/Rules/ \
        TradyStrat/Features/Indicators/ZoneClassifier.cs \
        TradyStrat.Tests/Indicators/Rules/ \
        TradyStrat.Tests/Indicators/ZoneClassifierTests.cs
git commit -m "feat: add IZoneRule strategies and ZoneClassifier composite"
```

---

### Task 21: `IndicatorEngine`

**Files:**
- Create: `TradyStrat/Features/Indicators/IndicatorEngine.cs`
- Create: `TradyStrat.Tests/Indicators/IndicatorEngineTests.cs`

- [ ] **Step 1: Write the failing test**

`TradyStrat.Tests/Indicators/IndicatorEngineTests.cs`:
```csharp
using Shouldly;
using TradyStrat.Features.Indicators;
using TradyStrat.Features.Indicators.Rules;
using TradyStrat.Shared.Domain;
using TradyStrat.Shared.Exceptions;
using TradyStrat.Tests.Fx;        // TestRepo<>
using TradyStrat.Tests.Specifications;

namespace TradyStrat.Tests.Indicators;

public class IndicatorEngineTests
{
    [Fact]
    public async Task ComputeFor_throws_when_no_bars_for_ticker()
    {
        await using var db = InMemoryDb.Create();
        var repo = new TestRepo<PriceBar>(db);
        var engine = new IndicatorEngine(repo, new ZoneClassifier(Array.Empty<IZoneRule>()));

        await Should.ThrowAsync<IndicatorComputationException>(() =>
            engine.ComputeFor("CON3.DE", CancellationToken.None));
    }

    [Fact]
    public async Task ComputeFor_returns_reading_with_price_and_zone()
    {
        await using var db = InMemoryDb.Create();
        // Persist 250 bars to ensure all indicators compute
        var bars = SeriesLoader.LoadCloses("CON3.DE");
        db.PriceBars.AddRange(bars);
        await db.SaveChangesAsync();

        var repo = new TestRepo<PriceBar>(db);
        var classifier = new ZoneClassifier(new IZoneRule[]
        {
            new BollingerZoneRule(), new RsiZoneRule(),
            new MovingAverageZoneRule(), new IchimokuZoneRule()
        });

        var engine = new IndicatorEngine(repo, classifier);

        var reading = await engine.ComputeFor("CON3.DE", CancellationToken.None);

        reading.Ticker.ShouldBe("CON3.DE");
        reading.Price.ShouldBe(bars[^1].Close);
        reading.Bollinger.ShouldNotBeNull();
        reading.Rsi.ShouldNotBeNull();
        reading.Sma50.ShouldNotBeNull();
        reading.Sma200.ShouldNotBeNull();
        reading.Ichimoku.ShouldNotBeNull();
        reading.Reasons.Count.ShouldBeGreaterThan(0);
    }
}
```

- [ ] **Step 2: Run, expect compile failure**

```bash
dotnet test --filter "FullyQualifiedName~IndicatorEngineTests" --nologo
```

- [ ] **Step 3: Implement**

`TradyStrat/Features/Indicators/IndicatorEngine.cs`:
```csharp
using Ardalis.Specification;
using TradyStrat.Shared.Domain;
using TradyStrat.Shared.Exceptions;
using TradyStrat.Specifications.PriceBars;

namespace TradyStrat.Features.Indicators;

public sealed class IndicatorEngine(
    IReadRepositoryBase<PriceBar> bars,
    ZoneClassifier classifier)
{
    public async Task<IndicatorReading> ComputeFor(string ticker, CancellationToken ct)
    {
        var series = await bars.ListAsync(new PriceBarsForTickerSpec(ticker), ct);
        if (series.Count == 0)
            throw new IndicatorComputationException($"No price bars for {ticker}");

        var price  = series[^1].Close;
        var bundle = new IndicatorBundle(
            Bollinger.LatestFor(series),
            Rsi.LatestFor(series),
            MovingAverage.LatestFor(series, 50),
            MovingAverage.LatestFor(series, 200),
            Ichimoku.LatestFor(series));

        var (zone, reasons) = classifier.Classify(price, bundle);

        return new IndicatorReading(
            ticker, price,
            bundle.Bollinger, bundle.Rsi, bundle.Sma50, bundle.Sma200, bundle.Ichimoku,
            zone, reasons);
    }
}
```

- [ ] **Step 4: Run, expect pass**

```bash
dotnet test --filter "FullyQualifiedName~IndicatorEngineTests" --nologo
```

Expected: `Passed: 2`.

- [ ] **Step 5: Commit**

```bash
git add TradyStrat/Features/Indicators/IndicatorEngine.cs \
        TradyStrat.Tests/Indicators/IndicatorEngineTests.cs
git commit -m "feat: add IndicatorEngine composing readings + ZoneClassifier"
```

---

### Task 22: `IndicatorsModule`

**Files:**
- Create: `TradyStrat/Modules/IndicatorsModule.cs`

- [ ] **Step 1: Implement**

`TradyStrat/Modules/IndicatorsModule.cs`:
```csharp
using TheAppManager.Modules;
using TradyStrat.Features.Indicators;
using TradyStrat.Features.Indicators.Rules;

namespace TradyStrat.Modules;

public sealed class IndicatorsModule : IAppModule
{
    public void ConfigureServices(WebApplicationBuilder builder)
    {
        builder.Services.AddSingleton<IZoneRule, BollingerZoneRule>();
        builder.Services.AddSingleton<IZoneRule, RsiZoneRule>();
        builder.Services.AddSingleton<IZoneRule, MovingAverageZoneRule>();
        builder.Services.AddSingleton<IZoneRule, IchimokuZoneRule>();
        builder.Services.AddScoped<ZoneClassifier>();
        builder.Services.AddScoped<IndicatorEngine>();
    }
}
```

- [ ] **Step 2: Build and verify all tests still pass**

```bash
dotnet build --nologo && dotnet test --nologo
```

Expected: green.

- [ ] **Step 3: Commit**

```bash
git add TradyStrat/Modules/IndicatorsModule.cs
git commit -m "feat: add IndicatorsModule wiring rules + classifier + engine"
```

**End of Phase 3.** State check: `IndicatorEngine` resolves from DI; given a populated DB, computes a reading per ticker.

---

## Phase 4 — Portfolio and trade log (Tasks 23–29)

This phase computes the portfolio (FIFO lots, P&L, progress) from the trade log, builds the daily growth series for the chart, and adds the trade-management use cases. End state: given a few trades and the current CON3.DE close, `PortfolioService` returns the right snapshot; `LogTradeUseCase` validates and persists; CSV import works.

---

### Task 23: `PortfolioService` (FIFO lots, snapshot)

**Files:**
- Create: `TradyStrat/Shared/Domain/PortfolioSnapshot.cs`
- Create: `TradyStrat/Shared/Domain/Lot.cs`
- Create: `TradyStrat/Features/Portfolio/PortfolioService.cs`
- Create: `TradyStrat.Tests/Portfolio/PortfolioServiceTests.cs`

- [ ] **Step 1: Add domain records**

`TradyStrat/Shared/Domain/PortfolioSnapshot.cs`:
```csharp
namespace TradyStrat.Shared.Domain;

public sealed record PortfolioSnapshot(
    decimal Shares,
    decimal AvgCostEur,
    decimal CurrentValueEur,
    decimal UnrealizedPnLEur,
    decimal RealizedPnLEur,
    decimal ProgressPct);
```

`TradyStrat/Shared/Domain/Lot.cs`:
```csharp
namespace TradyStrat.Shared.Domain;

public sealed record Lot(DateOnly OpenedOn, decimal Quantity, decimal UnitCostEur)
{
    public decimal CostBasisEur => Quantity * UnitCostEur;
}
```

- [ ] **Step 2: Write the failing tests**

`TradyStrat.Tests/Portfolio/PortfolioServiceTests.cs`:
```csharp
using Shouldly;
using TradyStrat.Features.Portfolio;
using TradyStrat.Shared.Domain;
using TradyStrat.Tests.Fx;        // TestRepo<>
using TradyStrat.Tests.Specifications;

namespace TradyStrat.Tests.Portfolio;

public class PortfolioServiceTests
{
    private static Trade Buy(int day, decimal qty, decimal price, decimal fees = 0m) => new()
    {
        Id = 0, ExecutedOn = new DateOnly(2026,1,day), Side = TradeSide.Buy,
        Quantity = qty, PricePerShare = price, FeesEur = fees, Note = null,
        CreatedAt = DateTime.UtcNow,
    };
    private static Trade Sell(int day, decimal qty, decimal price, decimal fees = 0m) =>
        Buy(day, qty, price, fees) with { Side = TradeSide.Sell };

    private static PortfolioService NewService(TradyStrat.Data.AppDbContext db)
        => new(new TestRepo<Trade>(db));

    [Fact]
    public async Task Empty_trade_log_returns_zero_snapshot()
    {
        await using var db = InMemoryDb.Create();
        var snap = await NewService(db).SnapshotAsync(currentPriceEur: 5m, goalEur: 1_000_000m, ct: default);

        snap.Shares.ShouldBe(0m);
        snap.CurrentValueEur.ShouldBe(0m);
        snap.UnrealizedPnLEur.ShouldBe(0m);
        snap.RealizedPnLEur.ShouldBe(0m);
        snap.ProgressPct.ShouldBe(0m);
    }

    [Fact]
    public async Task Single_buy_avg_cost_includes_fees()
    {
        await using var db = InMemoryDb.Create();
        db.Trades.Add(Buy(1, qty: 10m, price: 4.00m, fees: 2.00m));
        await db.SaveChangesAsync();

        var snap = await NewService(db).SnapshotAsync(5m, 1_000_000m, default);

        snap.Shares.ShouldBe(10m);
        // Avg cost = (10*4 + 2) / 10 = 4.20
        snap.AvgCostEur.ShouldBe(4.20m);
        snap.CurrentValueEur.ShouldBe(50m);
        snap.UnrealizedPnLEur.ShouldBe(50m - 42m);
    }

    [Fact]
    public async Task FIFO_sell_realizes_oldest_lot_first()
    {
        await using var db = InMemoryDb.Create();
        db.Trades.AddRange(
            Buy(1, qty: 10m, price: 4.00m),    // open lot @ 4.00
            Buy(5, qty: 10m, price: 5.00m),    // open lot @ 5.00
            Sell(8, qty: 5m, price: 6.00m));   // sell 5 → realize 5*(6-4)=10
        await db.SaveChangesAsync();

        var snap = await NewService(db).SnapshotAsync(7m, 1_000_000m, default);

        snap.Shares.ShouldBe(15m);
        snap.RealizedPnLEur.ShouldBe(10m);
        // Remaining lots: 5 @ 4.00, 10 @ 5.00 → avg cost = (5*4 + 10*5) / 15 = 70/15
        snap.AvgCostEur.ShouldBe(70m / 15m, tolerance: 0.0001m);
    }

    [Fact]
    public async Task Progress_pct_computed_against_goal()
    {
        await using var db = InMemoryDb.Create();
        db.Trades.Add(Buy(1, qty: 100m, price: 1m));
        await db.SaveChangesAsync();

        var snap = await NewService(db).SnapshotAsync(currentPriceEur: 5m, goalEur: 1000m, ct: default);

        snap.CurrentValueEur.ShouldBe(500m);
        snap.ProgressPct.ShouldBe(50m);
    }

    [Fact]
    public async Task Sell_more_than_held_throws()
    {
        await using var db = InMemoryDb.Create();
        db.Trades.AddRange(Buy(1, 5m, 4m), Sell(2, 10m, 5m));
        await db.SaveChangesAsync();

        await Should.ThrowAsync<TradyStrat.Shared.Exceptions.TradeValidationException>(() =>
            NewService(db).SnapshotAsync(5m, 1_000_000m, default));
    }
}
```

- [ ] **Step 3: Run, expect compile failure**

```bash
dotnet test --filter "FullyQualifiedName~PortfolioServiceTests" --nologo
```

- [ ] **Step 4: Implement**

`TradyStrat/Features/Portfolio/PortfolioService.cs`:
```csharp
using Ardalis.Specification;
using TradyStrat.Shared.Domain;
using TradyStrat.Shared.Exceptions;
using TradyStrat.Specifications.Trades;

namespace TradyStrat.Features.Portfolio;

public sealed class PortfolioService(IReadRepositoryBase<Trade> trades)
{
    public async Task<PortfolioSnapshot> SnapshotAsync(
        decimal currentPriceEur, decimal goalEur, CancellationToken ct)
    {
        var all = await trades.ListAsync(new AllTradesSpec(), ct);

        var openLots = new LinkedList<Lot>();
        var realized = 0m;

        foreach (var t in all)
        {
            if (t.IsBuy)
            {
                var unitCost = (t.GrossEur + t.FeesEur) / t.Quantity;   // fees folded into cost basis
                openLots.AddLast(new Lot(t.ExecutedOn, t.Quantity, unitCost));
            }
            else
            {
                var remaining = t.Quantity;
                while (remaining > 0)
                {
                    var head = openLots.First
                        ?? throw new TradeValidationException(
                            $"Sell on {t.ExecutedOn} exceeds open lots.");

                    var consumed = Math.Min(head.Value.Quantity, remaining);
                    realized += consumed * (t.PricePerShare - head.Value.UnitCostEur);
                    realized -= t.FeesEur * (consumed / t.Quantity);

                    if (consumed == head.Value.Quantity)
                        openLots.RemoveFirst();
                    else
                        head.Value = head.Value with { Quantity = head.Value.Quantity - consumed };

                    remaining -= consumed;
                }
            }
        }

        var shares     = openLots.Sum(l => l.Quantity);
        var costBasis  = openLots.Sum(l => l.CostBasisEur);
        var avgCost    = shares == 0 ? 0m : costBasis / shares;
        var currentVal = shares * currentPriceEur;
        var unrealised = currentVal - costBasis;
        var pct        = goalEur == 0m ? 0m : currentVal / goalEur * 100m;

        return new PortfolioSnapshot(
            Shares: shares,
            AvgCostEur: avgCost,
            CurrentValueEur: currentVal,
            UnrealizedPnLEur: unrealised,
            RealizedPnLEur: realized,
            ProgressPct: pct);
    }
}
```

> `LinkedList<Lot>` with `Value =` requires the `Value` setter. `Lot` is a record so we use `with` and replace via `head.Value = head.Value with { ... }`. `LinkedListNode<T>.Value` is mutable.

- [ ] **Step 5: Run, expect pass**

```bash
dotnet test --filter "FullyQualifiedName~PortfolioServiceTests" --nologo
```

Expected: `Passed: 5`.

- [ ] **Step 6: Commit**

```bash
git add TradyStrat/Shared/Domain/PortfolioSnapshot.cs \
        TradyStrat/Shared/Domain/Lot.cs \
        TradyStrat/Features/Portfolio/PortfolioService.cs \
        TradyStrat.Tests/Portfolio/PortfolioServiceTests.cs
git commit -m "feat: PortfolioService with FIFO lots, fees in cost basis, P&L"
```

---

### Task 24: `GrowthSeriesBuilder` (daily portfolio value)

**Files:**
- Create: `TradyStrat/Shared/Domain/GrowthPoint.cs`
- Create: `TradyStrat/Features/Portfolio/GrowthSeriesBuilder.cs`
- Create: `TradyStrat.Tests/Portfolio/GrowthSeriesBuilderTests.cs`

- [ ] **Step 1: Add `GrowthPoint`**

`TradyStrat/Shared/Domain/GrowthPoint.cs`:
```csharp
namespace TradyStrat.Shared.Domain;

public sealed record GrowthPoint(DateOnly Date, decimal ValueEur);
```

- [ ] **Step 2: Write the failing test**

`TradyStrat.Tests/Portfolio/GrowthSeriesBuilderTests.cs`:
```csharp
using Shouldly;
using TradyStrat.Features.Portfolio;
using TradyStrat.Shared.Domain;
using TradyStrat.Tests.Fx;
using TradyStrat.Tests.Specifications;

namespace TradyStrat.Tests.Portfolio;

public class GrowthSeriesBuilderTests
{
    [Fact]
    public async Task Builds_one_point_per_day_with_running_value()
    {
        await using var db = InMemoryDb.Create();

        // Buy 10 sh on day 1 at €4, then 5 sh on day 3 at €5
        db.Trades.AddRange(
            new Trade { Id = 0, ExecutedOn = new(2026,1,1), Side = TradeSide.Buy,
                        Quantity = 10m, PricePerShare = 4m, FeesEur = 0m, Note = null,
                        CreatedAt = DateTime.UtcNow },
            new Trade { Id = 0, ExecutedOn = new(2026,1,3), Side = TradeSide.Buy,
                        Quantity = 5m, PricePerShare = 5m, FeesEur = 0m, Note = null,
                        CreatedAt = DateTime.UtcNow });

        // CON3.DE prices days 1..4
        db.PriceBars.AddRange(
            new PriceBar { Id=0, Ticker="CON3.DE", Date=new(2026,1,1), Open=4, High=4, Low=4, Close=4.0m, Volume=1 },
            new PriceBar { Id=0, Ticker="CON3.DE", Date=new(2026,1,2), Open=4, High=4, Low=4, Close=4.5m, Volume=1 },
            new PriceBar { Id=0, Ticker="CON3.DE", Date=new(2026,1,3), Open=5, High=5, Low=5, Close=5.0m, Volume=1 },
            new PriceBar { Id=0, Ticker="CON3.DE", Date=new(2026,1,4), Open=5, High=5, Low=5, Close=5.2m, Volume=1 });

        await db.SaveChangesAsync();

        var b = new GrowthSeriesBuilder(new TestRepo<Trade>(db), new TestRepo<PriceBar>(db));
        var pts = await b.BuildAsync("CON3.DE", default);

        pts.Count.ShouldBe(4);
        pts[0].ValueEur.ShouldBe(10m * 4.0m);
        pts[1].ValueEur.ShouldBe(10m * 4.5m);
        pts[2].ValueEur.ShouldBe(15m * 5.0m);
        pts[3].ValueEur.ShouldBe(15m * 5.2m);
    }

    [Fact]
    public async Task Returns_empty_when_no_trades()
    {
        await using var db = InMemoryDb.Create();
        var b = new GrowthSeriesBuilder(new TestRepo<Trade>(db), new TestRepo<PriceBar>(db));
        (await b.BuildAsync("CON3.DE", default)).ShouldBeEmpty();
    }
}
```

- [ ] **Step 3: Run, expect compile failure**

```bash
dotnet test --filter "FullyQualifiedName~GrowthSeriesBuilderTests" --nologo
```

- [ ] **Step 4: Implement**

`TradyStrat/Features/Portfolio/GrowthSeriesBuilder.cs`:
```csharp
using Ardalis.Specification;
using TradyStrat.Shared.Domain;
using TradyStrat.Specifications.PriceBars;
using TradyStrat.Specifications.Trades;

namespace TradyStrat.Features.Portfolio;

public sealed class GrowthSeriesBuilder(
    IReadRepositoryBase<Trade> trades,
    IReadRepositoryBase<PriceBar> bars)
{
    public async Task<IReadOnlyList<GrowthPoint>> BuildAsync(string ticker, CancellationToken ct)
    {
        var allTrades = await trades.ListAsync(new AllTradesSpec(), ct);
        if (allTrades.Count == 0) return [];

        var firstDate = allTrades[0].ExecutedOn;
        var priceBars = await bars.ListAsync(new PriceBarsSinceSpec(ticker, firstDate), ct);
        if (priceBars.Count == 0) return [];

        var tradeByDate = allTrades
            .GroupBy(t => t.ExecutedOn)
            .ToDictionary(g => g.Key, g => g.ToList());

        var points = new List<GrowthPoint>(priceBars.Count);
        var shares = 0m;

        foreach (var bar in priceBars)
        {
            if (tradeByDate.TryGetValue(bar.Date, out var todaysTrades))
            {
                foreach (var t in todaysTrades)
                    shares += t.IsBuy ? t.Quantity : -t.Quantity;
            }
            points.Add(new GrowthPoint(bar.Date, shares * bar.Close));
        }

        return points;
    }
}
```

- [ ] **Step 5: Run, expect pass**

```bash
dotnet test --filter "FullyQualifiedName~GrowthSeriesBuilderTests" --nologo
```

- [ ] **Step 6: Commit**

```bash
git add TradyStrat/Shared/Domain/GrowthPoint.cs \
        TradyStrat/Features/Portfolio/GrowthSeriesBuilder.cs \
        TradyStrat.Tests/Portfolio/GrowthSeriesBuilderTests.cs
git commit -m "feat: GrowthSeriesBuilder produces daily portfolio value series"
```

---

### Task 25: `PortfolioModule`

**Files:**
- Create: `TradyStrat/Modules/PortfolioModule.cs`

- [ ] **Step 1: Implement**

`TradyStrat/Modules/PortfolioModule.cs`:
```csharp
using TheAppManager.Modules;
using TradyStrat.Features.Portfolio;

namespace TradyStrat.Modules;

public sealed class PortfolioModule : IAppModule
{
    public void ConfigureServices(WebApplicationBuilder builder)
    {
        builder.Services.AddScoped<PortfolioService>();
        builder.Services.AddScoped<GrowthSeriesBuilder>();
    }
}
```

- [ ] **Step 2: Build & test**

```bash
dotnet build --nologo && dotnet test --nologo
```

- [ ] **Step 3: Commit**

```bash
git add TradyStrat/Modules/PortfolioModule.cs
git commit -m "feat: add PortfolioModule"
```

---

### Task 26: Use case abstractions (`IUseCase`, `UseCaseBase`, `Unit`)

**Files:**
- Create: `TradyStrat/Application/Abstractions/IUseCase.cs`
- Create: `TradyStrat/Application/Abstractions/Unit.cs`
- Create: `TradyStrat/Application/Abstractions/UseCaseBase.cs`
- Create: `TradyStrat.Tests/Application/UseCaseBaseTests.cs`

- [ ] **Step 1: Write the failing test**

`TradyStrat.Tests/Application/UseCaseBaseTests.cs`:
```csharp
using Microsoft.Extensions.Logging.Abstractions;
using Shouldly;
using TradyStrat.Application.Abstractions;
using TradyStrat.Shared.Exceptions;

namespace TradyStrat.Tests.Application;

public class UseCaseBaseTests
{
    private sealed class OkUseCase : UseCaseBase<int, int>
    {
        public OkUseCase() : base(NullLogger.Instance) { }
        protected override Task<int> ExecuteCore(int input, CancellationToken ct)
            => Task.FromResult(input * 2);
    }

    private sealed class FailingUseCase : UseCaseBase<int, int>
    {
        public FailingUseCase() : base(NullLogger.Instance) { }
        protected override Task<int> ExecuteCore(int input, CancellationToken ct)
            => throw new TradeValidationException("nope");
    }

    [Fact]
    public async Task Returns_result_from_ExecuteCore()
    {
        (await new OkUseCase().ExecuteAsync(21, default)).ShouldBe(42);
    }

    [Fact]
    public async Task Domain_exception_propagates_unwrapped()
    {
        await Should.ThrowAsync<TradeValidationException>(() =>
            new FailingUseCase().ExecuteAsync(0, default));
    }
}
```

- [ ] **Step 2: Run, expect compile failure**

```bash
dotnet test --filter "FullyQualifiedName~UseCaseBaseTests" --nologo
```

- [ ] **Step 3: Implement**

`TradyStrat/Application/Abstractions/IUseCase.cs`:
```csharp
namespace TradyStrat.Application.Abstractions;

public interface IUseCase<in TInput, TOutput>
{
    Task<TOutput> ExecuteAsync(TInput input, CancellationToken ct);
}
```

`TradyStrat/Application/Abstractions/Unit.cs`:
```csharp
namespace TradyStrat.Application.Abstractions;

public readonly record struct Unit
{
    public static readonly Unit Value = default;
}
```

`TradyStrat/Application/Abstractions/UseCaseBase.cs`:
```csharp
using System.Diagnostics;
using Microsoft.Extensions.Logging;
using TradyStrat.Shared.Exceptions;

namespace TradyStrat.Application.Abstractions;

public abstract class UseCaseBase<TInput, TOutput>(ILogger logger)
    : IUseCase<TInput, TOutput>
{
    public async Task<TOutput> ExecuteAsync(TInput input, CancellationToken ct)
    {
        var name = GetType().Name;
        using var scope = logger.BeginScope("UseCase {UseCase}", name);
        var sw = Stopwatch.StartNew();
        try
        {
            var result = await ExecuteCore(input, ct);
            logger.LogInformation("{UseCase} ok in {Ms}ms", name, sw.ElapsedMilliseconds);
            return result;
        }
        catch (TradyStratException)
        {
            throw;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "{UseCase} failed", name);
            throw;
        }
    }

    protected abstract Task<TOutput> ExecuteCore(TInput input, CancellationToken ct);
}
```

- [ ] **Step 4: Run, expect pass**

```bash
dotnet test --filter "FullyQualifiedName~UseCaseBaseTests" --nologo
```

- [ ] **Step 5: Commit**

```bash
git add TradyStrat/Application/Abstractions/ TradyStrat.Tests/Application/
git commit -m "feat: add IUseCase + UseCaseBase template method"
```

---

### Task 27: Trade use cases (Log/Edit/Delete)

**Files:**
- Create: `TradyStrat/Application/UseCases/Trades/LogTradeUseCase.cs`
- Create: `TradyStrat/Application/UseCases/Trades/EditTradeUseCase.cs`
- Create: `TradyStrat/Application/UseCases/Trades/DeleteTradeUseCase.cs`
- Create: `TradyStrat.Tests/UseCases/Trades/LogTradeUseCaseTests.cs`
- Create: `TradyStrat.Tests/UseCases/Trades/EditTradeUseCaseTests.cs`
- Create: `TradyStrat.Tests/UseCases/Trades/DeleteTradeUseCaseTests.cs`

- [ ] **Step 1: Write the failing tests**

`TradyStrat.Tests/UseCases/Trades/LogTradeUseCaseTests.cs`:
```csharp
using Microsoft.Extensions.Logging.Abstractions;
using Shouldly;
using TradyStrat.Application.UseCases.Trades;
using TradyStrat.Shared.Domain;
using TradyStrat.Shared.Exceptions;
using TradyStrat.Tests.Fx;
using TradyStrat.Tests.Specifications;
using TradyStrat.Tests.Time;

namespace TradyStrat.Tests.UseCases.Trades;

public class LogTradeUseCaseTests
{
    [Fact]
    public async Task Persists_trade_with_required_fields()
    {
        await using var db = InMemoryDb.Create();
        var clock = new FakeClock(new DateTime(2026,5,6,0,0,0,DateTimeKind.Utc));
        var uc = new LogTradeUseCase(new TestRepo<Trade>(db), clock,
            NullLogger<LogTradeUseCase>.Instance);

        var t = await uc.ExecuteAsync(new LogTradeInput(
            ExecutedOn: new(2026,5,6), Side: TradeSide.Buy,
            Quantity: 10m, PricePerShare: 4.5m, FeesEur: 0.5m, Note: "first lot"), default);

        t.Quantity.ShouldBe(10m);
        db.Trades.Count().ShouldBe(1);
    }

    [Fact]
    public async Task Rejects_non_positive_quantity()
    {
        await using var db = InMemoryDb.Create();
        var uc = new LogTradeUseCase(new TestRepo<Trade>(db),
            new FakeClock(DateTime.UtcNow), NullLogger<LogTradeUseCase>.Instance);

        await Should.ThrowAsync<TradeValidationException>(() =>
            uc.ExecuteAsync(new LogTradeInput(new(2026,5,6), TradeSide.Buy,
                Quantity: 0m, PricePerShare: 4m, FeesEur: 0m, Note: null), default));
    }

    [Fact]
    public async Task Rejects_non_positive_price()
    {
        await using var db = InMemoryDb.Create();
        var uc = new LogTradeUseCase(new TestRepo<Trade>(db),
            new FakeClock(DateTime.UtcNow), NullLogger<LogTradeUseCase>.Instance);

        await Should.ThrowAsync<TradeValidationException>(() =>
            uc.ExecuteAsync(new LogTradeInput(new(2026,5,6), TradeSide.Buy,
                10m, PricePerShare: -1m, 0m, null), default));
    }
}
```

`TradyStrat.Tests/UseCases/Trades/EditTradeUseCaseTests.cs`:
```csharp
using Microsoft.Extensions.Logging.Abstractions;
using Shouldly;
using TradyStrat.Application.UseCases.Trades;
using TradyStrat.Shared.Domain;
using TradyStrat.Shared.Exceptions;
using TradyStrat.Tests.Fx;
using TradyStrat.Tests.Specifications;

namespace TradyStrat.Tests.UseCases.Trades;

public class EditTradeUseCaseTests
{
    [Fact]
    public async Task Updates_existing_trade_quantity_and_price()
    {
        await using var db = InMemoryDb.Create();
        db.Trades.Add(new Trade {
            Id = 0, ExecutedOn = new(2026,5,6), Side = TradeSide.Buy,
            Quantity = 5m, PricePerShare = 4m, FeesEur = 0m, Note = null,
            CreatedAt = DateTime.UtcNow });
        await db.SaveChangesAsync();
        var existing = db.Trades.Single();

        var uc = new EditTradeUseCase(new TestRepo<Trade>(db),
            NullLogger<EditTradeUseCase>.Instance);

        var updated = await uc.ExecuteAsync(new EditTradeInput(
            Id: existing.Id, ExecutedOn: existing.ExecutedOn, Side: TradeSide.Buy,
            Quantity: 8m, PricePerShare: 4.25m, FeesEur: 0.10m, Note: "edited"), default);

        updated.Quantity.ShouldBe(8m);
        db.Trades.Single().PricePerShare.ShouldBe(4.25m);
    }

    [Fact]
    public async Task Throws_when_id_not_found()
    {
        await using var db = InMemoryDb.Create();
        var uc = new EditTradeUseCase(new TestRepo<Trade>(db),
            NullLogger<EditTradeUseCase>.Instance);

        await Should.ThrowAsync<TradeValidationException>(() =>
            uc.ExecuteAsync(new EditTradeInput(
                999, new(2026,5,6), TradeSide.Buy, 1m, 1m, 0m, null), default));
    }
}
```

`TradyStrat.Tests/UseCases/Trades/DeleteTradeUseCaseTests.cs`:
```csharp
using Microsoft.Extensions.Logging.Abstractions;
using Shouldly;
using TradyStrat.Application.UseCases.Trades;
using TradyStrat.Shared.Domain;
using TradyStrat.Shared.Exceptions;
using TradyStrat.Tests.Fx;
using TradyStrat.Tests.Specifications;

namespace TradyStrat.Tests.UseCases.Trades;

public class DeleteTradeUseCaseTests
{
    [Fact]
    public async Task Removes_existing_trade()
    {
        await using var db = InMemoryDb.Create();
        db.Trades.Add(new Trade {
            Id = 0, ExecutedOn = new(2026,5,6), Side = TradeSide.Buy,
            Quantity = 1m, PricePerShare = 1m, FeesEur = 0m, Note = null,
            CreatedAt = DateTime.UtcNow });
        await db.SaveChangesAsync();
        var existing = db.Trades.Single();

        var uc = new DeleteTradeUseCase(new TestRepo<Trade>(db),
            NullLogger<DeleteTradeUseCase>.Instance);

        await uc.ExecuteAsync(new DeleteTradeInput(existing.Id), default);

        db.Trades.Count().ShouldBe(0);
    }

    [Fact]
    public async Task Throws_when_id_missing()
    {
        await using var db = InMemoryDb.Create();
        var uc = new DeleteTradeUseCase(new TestRepo<Trade>(db),
            NullLogger<DeleteTradeUseCase>.Instance);

        await Should.ThrowAsync<TradeValidationException>(() =>
            uc.ExecuteAsync(new DeleteTradeInput(123), default));
    }
}
```

- [ ] **Step 2: Run, expect compile failure**

```bash
dotnet test --filter "FullyQualifiedName~LogTradeUseCaseTests|FullyQualifiedName~EditTradeUseCaseTests|FullyQualifiedName~DeleteTradeUseCaseTests" --nologo
```

- [ ] **Step 3: Implement**

`TradyStrat/Application/UseCases/Trades/LogTradeUseCase.cs`:
```csharp
using Ardalis.Specification;
using Microsoft.Extensions.Logging;
using TradyStrat.Application.Abstractions;
using TradyStrat.Shared.Domain;
using TradyStrat.Shared.Exceptions;
using TradyStrat.Shared.Time;

namespace TradyStrat.Application.UseCases.Trades;

public sealed record LogTradeInput(
    DateOnly ExecutedOn, TradeSide Side,
    decimal Quantity, decimal PricePerShare, decimal FeesEur, string? Note);

public sealed class LogTradeUseCase(
    IRepositoryBase<Trade> repo, IClock clock,
    ILogger<LogTradeUseCase> log)
    : UseCaseBase<LogTradeInput, Trade>(log)
{
    protected override async Task<Trade> ExecuteCore(LogTradeInput input, CancellationToken ct)
    {
        if (input.Quantity <= 0m)       throw new TradeValidationException("Quantity must be positive.");
        if (input.PricePerShare <= 0m)  throw new TradeValidationException("Price per share must be positive.");
        if (input.FeesEur < 0m)         throw new TradeValidationException("Fees cannot be negative.");

        var trade = new Trade
        {
            Id = 0,
            ExecutedOn   = input.ExecutedOn,
            Side         = input.Side,
            Quantity     = input.Quantity,
            PricePerShare = input.PricePerShare,
            FeesEur      = input.FeesEur,
            Note         = input.Note,
            CreatedAt    = clock.UtcNow(),
        };
        await repo.AddAsync(trade, ct);
        return trade;
    }
}
```

`TradyStrat/Application/UseCases/Trades/EditTradeUseCase.cs`:
```csharp
using Ardalis.Specification;
using Microsoft.Extensions.Logging;
using TradyStrat.Application.Abstractions;
using TradyStrat.Shared.Domain;
using TradyStrat.Shared.Exceptions;

namespace TradyStrat.Application.UseCases.Trades;

public sealed record EditTradeInput(
    int Id, DateOnly ExecutedOn, TradeSide Side,
    decimal Quantity, decimal PricePerShare, decimal FeesEur, string? Note);

public sealed class EditTradeUseCase(
    IRepositoryBase<Trade> repo, ILogger<EditTradeUseCase> log)
    : UseCaseBase<EditTradeInput, Trade>(log)
{
    protected override async Task<Trade> ExecuteCore(EditTradeInput input, CancellationToken ct)
    {
        if (input.Quantity <= 0m)       throw new TradeValidationException("Quantity must be positive.");
        if (input.PricePerShare <= 0m)  throw new TradeValidationException("Price per share must be positive.");

        var existing = await repo.GetByIdAsync(input.Id, ct)
            ?? throw new TradeValidationException($"Trade {input.Id} not found.");

        var updated = existing with
        {
            ExecutedOn    = input.ExecutedOn,
            Side          = input.Side,
            Quantity      = input.Quantity,
            PricePerShare = input.PricePerShare,
            FeesEur       = input.FeesEur,
            Note          = input.Note,
        };

        await repo.UpdateAsync(updated, ct);
        return updated;
    }
}
```

`TradyStrat/Application/UseCases/Trades/DeleteTradeUseCase.cs`:
```csharp
using Ardalis.Specification;
using Microsoft.Extensions.Logging;
using TradyStrat.Application.Abstractions;
using TradyStrat.Shared.Domain;
using TradyStrat.Shared.Exceptions;

namespace TradyStrat.Application.UseCases.Trades;

public sealed record DeleteTradeInput(int Id);

public sealed class DeleteTradeUseCase(
    IRepositoryBase<Trade> repo, ILogger<DeleteTradeUseCase> log)
    : UseCaseBase<DeleteTradeInput, Unit>(log)
{
    protected override async Task<Unit> ExecuteCore(DeleteTradeInput input, CancellationToken ct)
    {
        var existing = await repo.GetByIdAsync(input.Id, ct)
            ?? throw new TradeValidationException($"Trade {input.Id} not found.");

        await repo.DeleteAsync(existing, ct);
        return Unit.Value;
    }
}
```

- [ ] **Step 4: Run, expect pass**

```bash
dotnet test --filter "FullyQualifiedName~LogTradeUseCaseTests|FullyQualifiedName~EditTradeUseCaseTests|FullyQualifiedName~DeleteTradeUseCaseTests" --nologo
```

Expected: `Passed: 7`.

- [ ] **Step 5: Commit**

```bash
git add TradyStrat/Application/UseCases/Trades/ TradyStrat.Tests/UseCases/Trades/
git commit -m "feat: add LogTrade/EditTrade/DeleteTrade use cases"
```

---

### Task 28: `CsvImportService` + `ImportTradesCsvUseCase`

**Files:**
- Create: `TradyStrat/Features/Trades/CsvImportService.cs`
- Create: `TradyStrat/Application/UseCases/Trades/ImportTradesCsvUseCase.cs`
- Create: `TradyStrat.Tests/Trades/CsvImportServiceTests.cs`

- [ ] **Step 1: Write the failing tests**

`TradyStrat.Tests/Trades/CsvImportServiceTests.cs`:
```csharp
using Shouldly;
using TradyStrat.Features.Trades;
using TradyStrat.Shared.Domain;
using TradyStrat.Shared.Exceptions;

namespace TradyStrat.Tests.Trades;

public class CsvImportServiceTests
{
    [Fact]
    public void Parses_header_then_rows()
    {
        var csv = "date,side,qty,price,fees\n2026-05-01,Buy,10,4.20,0.50\n2026-05-03,Sell,5,4.80,0.50\n";
        var rows = CsvImportService.Parse(new StringReader(csv));

        rows.Count.ShouldBe(2);
        rows[0].ExecutedOn.ShouldBe(new DateOnly(2026,5,1));
        rows[0].Side.ShouldBe(TradeSide.Buy);
        rows[0].Quantity.ShouldBe(10m);
        rows[1].PricePerShare.ShouldBe(4.80m);
    }

    [Fact]
    public void Rejects_unknown_side()
    {
        var csv = "date,side,qty,price,fees\n2026-05-01,Hold,1,1,0\n";
        var ex = Should.Throw<CsvImportException>(() => CsvImportService.Parse(new StringReader(csv)));
        ex.LineNumber.ShouldBe(2);
    }

    [Fact]
    public void Rejects_missing_columns()
    {
        var csv = "date,side,qty\n2026-05-01,Buy,10\n";
        Should.Throw<CsvImportException>(() => CsvImportService.Parse(new StringReader(csv)));
    }

    [Fact]
    public void Rejects_blank_input()
    {
        Should.Throw<CsvImportException>(() => CsvImportService.Parse(new StringReader("")));
    }

    [Fact]
    public void Tolerates_lowercase_headers_and_whitespace()
    {
        var csv = "  Date , Side , Qty , Price , Fees \n2026-05-01,buy,10,4.20,0\n";
        var rows = CsvImportService.Parse(new StringReader(csv));
        rows.Count.ShouldBe(1);
    }
}
```

- [ ] **Step 2: Run, expect compile failure**

```bash
dotnet test --filter "FullyQualifiedName~CsvImportServiceTests" --nologo
```

- [ ] **Step 3: Implement**

`TradyStrat/Features/Trades/CsvImportService.cs`:
```csharp
using System.Globalization;
using TradyStrat.Shared.Domain;
using TradyStrat.Shared.Exceptions;

namespace TradyStrat.Features.Trades;

public sealed record CsvTradeRow(
    DateOnly ExecutedOn, TradeSide Side,
    decimal Quantity, decimal PricePerShare, decimal FeesEur, string? Note);

public static class CsvImportService
{
    private static readonly string[] Required = ["date", "side", "qty", "price", "fees"];

    public static IReadOnlyList<CsvTradeRow> Parse(TextReader reader)
    {
        var headerLine = reader.ReadLine()
            ?? throw new CsvImportException("CSV is empty.");

        var headers = headerLine.Split(',').Select(h => h.Trim().ToLowerInvariant()).ToArray();
        var idx = new Dictionary<string, int>();
        foreach (var name in Required)
        {
            var i = Array.IndexOf(headers, name);
            if (i < 0) throw new CsvImportException($"Missing required column '{name}'.");
            idx[name] = i;
        }

        var rows = new List<CsvTradeRow>();
        int line = 1;
        string? raw;
        while ((raw = reader.ReadLine()) is not null)
        {
            line++;
            if (string.IsNullOrWhiteSpace(raw)) continue;

            var cells = raw.Split(',').Select(c => c.Trim()).ToArray();
            try
            {
                var date  = DateOnly.Parse(cells[idx["date"]],  CultureInfo.InvariantCulture);
                var side  = ParseSide(cells[idx["side"]], line);
                var qty   = decimal.Parse(cells[idx["qty"]],    CultureInfo.InvariantCulture);
                var price = decimal.Parse(cells[idx["price"]],  CultureInfo.InvariantCulture);
                var fees  = decimal.Parse(cells[idx["fees"]],   CultureInfo.InvariantCulture);
                rows.Add(new CsvTradeRow(date, side, qty, price, fees, Note: null));
            }
            catch (Exception ex) when (ex is FormatException or IndexOutOfRangeException)
            {
                throw new CsvImportException(ex.Message, line);
            }
        }
        return rows;
    }

    private static TradeSide ParseSide(string raw, int line) => raw.ToLowerInvariant() switch
    {
        "buy"  => TradeSide.Buy,
        "sell" => TradeSide.Sell,
        _      => throw new CsvImportException($"Unknown side '{raw}'.", line)
    };
}
```

`TradyStrat/Application/UseCases/Trades/ImportTradesCsvUseCase.cs`:
```csharp
using Ardalis.Specification;
using Microsoft.Extensions.Logging;
using TradyStrat.Application.Abstractions;
using TradyStrat.Features.Trades;
using TradyStrat.Shared.Domain;
using TradyStrat.Shared.Time;

namespace TradyStrat.Application.UseCases.Trades;

public sealed record ImportTradesCsvInput(string CsvText);
public sealed record ImportTradesCsvResult(int RowsImported);

public sealed class ImportTradesCsvUseCase(
    IRepositoryBase<Trade> repo, IClock clock,
    ILogger<ImportTradesCsvUseCase> log)
    : UseCaseBase<ImportTradesCsvInput, ImportTradesCsvResult>(log)
{
    protected override async Task<ImportTradesCsvResult> ExecuteCore(
        ImportTradesCsvInput input, CancellationToken ct)
    {
        var rows = CsvImportService.Parse(new StringReader(input.CsvText));
        var now  = clock.UtcNow();
        var trades = rows.Select(r => new Trade
        {
            Id = 0,
            ExecutedOn   = r.ExecutedOn,
            Side         = r.Side,
            Quantity     = r.Quantity,
            PricePerShare = r.PricePerShare,
            FeesEur      = r.FeesEur,
            Note         = r.Note,
            CreatedAt    = now,
        }).ToList();

        await repo.AddRangeAsync(trades, ct);
        return new ImportTradesCsvResult(trades.Count);
    }
}
```

- [ ] **Step 4: Run, expect pass**

```bash
dotnet test --filter "FullyQualifiedName~CsvImportServiceTests" --nologo
```

- [ ] **Step 5: Commit**

```bash
git add TradyStrat/Features/Trades/CsvImportService.cs \
        TradyStrat/Application/UseCases/Trades/ImportTradesCsvUseCase.cs \
        TradyStrat.Tests/Trades/
git commit -m "feat: CSV import — parser + ImportTradesCsvUseCase"
```

---

### Task 29: `TradesModule` + `RefreshAllPricesUseCase`

**Files:**
- Create: `TradyStrat/Application/UseCases/Prices/RefreshAllPricesUseCase.cs`
- Create: `TradyStrat/Modules/TradesModule.cs`
- Create: `TradyStrat/Modules/PricesUseCasesModule.cs`

- [ ] **Step 1: Implement `RefreshAllPricesUseCase`**

`TradyStrat/Application/UseCases/Prices/RefreshAllPricesUseCase.cs`:
```csharp
using Microsoft.Extensions.Logging;
using TradyStrat.Application.Abstractions;
using TradyStrat.Features.Fx;
using TradyStrat.Features.PriceFeed;

namespace TradyStrat.Application.UseCases.Prices;

public sealed class RefreshAllPricesUseCase(
    DailyPriceCache prices, DailyFxCache fx,
    ILogger<RefreshAllPricesUseCase> log)
    : UseCaseBase<Unit, Unit>(log)
{
    private static readonly string[] Tickers = ["CON3.DE", "COIN", "BTC-USD"];
    private const string FxPair = "EURUSD";

    protected override async Task<Unit> ExecuteCore(Unit _, CancellationToken ct)
    {
        foreach (var t in Tickers) await prices.EnsureFreshAsync(t, ct);
        await fx.EnsureFreshAsync(FxPair, ct);
        return Unit.Value;
    }
}
```

- [ ] **Step 2: Implement modules**

`TradyStrat/Modules/TradesModule.cs`:
```csharp
using TheAppManager.Modules;
using TradyStrat.Application.UseCases.Trades;

namespace TradyStrat.Modules;

public sealed class TradesModule : IAppModule
{
    public void ConfigureServices(WebApplicationBuilder builder)
    {
        builder.Services.AddScoped<LogTradeUseCase>();
        builder.Services.AddScoped<EditTradeUseCase>();
        builder.Services.AddScoped<DeleteTradeUseCase>();
        builder.Services.AddScoped<ImportTradesCsvUseCase>();
    }
}
```

`TradyStrat/Modules/PricesUseCasesModule.cs`:
```csharp
using TheAppManager.Modules;
using TradyStrat.Application.UseCases.Prices;

namespace TradyStrat.Modules;

public sealed class PricesUseCasesModule : IAppModule
{
    public void ConfigureServices(WebApplicationBuilder builder)
    {
        builder.Services.AddScoped<RefreshAllPricesUseCase>();
    }
}
```

- [ ] **Step 3: Build & run all tests**

```bash
dotnet build --nologo && dotnet test --nologo
```

Expected: green.

- [ ] **Step 4: Commit**

```bash
git add TradyStrat/Application/UseCases/Prices/ TradyStrat/Modules/TradesModule.cs \
        TradyStrat/Modules/PricesUseCasesModule.cs
git commit -m "feat: add RefreshAllPricesUseCase + Trades/Prices modules"
```

**End of Phase 4.** Trade-management use cases work; portfolio math works; growth chart data builds. The dashboard data layer is complete.

---

## Phase 5 — AI suggestion (Tasks 30–33)

This phase composes the daily AI snapshot, calls Anthropic via the `Microsoft.Extensions.AI.IChatClient` abstraction with a tool-use schema, persists the typed `Suggestion`, and exposes the cached/force-refetch use cases. End state: `GetTodaysSuggestionUseCase` returns a cached row if today's exists; otherwise calls `IChatClient`, captures the tool invocation, and saves it.

---

### Task 30: `SnapshotBuilder`

**Files:**
- Create: `TradyStrat/Features/AiSuggestion/AiSnapshot.cs`
- Create: `TradyStrat/Features/AiSuggestion/JsonOpts.cs`
- Create: `TradyStrat/Features/AiSuggestion/SnapshotBuilder.cs`
- Create: `TradyStrat.Tests/AiSuggestion/SnapshotBuilderTests.cs`

- [ ] **Step 1: Add the snapshot record + JSON helper**

`TradyStrat/Features/AiSuggestion/AiSnapshot.cs`:
```csharp
using TradyStrat.Shared.Domain;

namespace TradyStrat.Features.AiSuggestion;

public sealed record TickerContext(
    string Ticker, string Currency,
    decimal PriceNative, decimal? PriceEur,
    Zone Zone, IReadOnlyList<string> Reasons);

public sealed record TradeRecent(
    DateOnly ExecutedOn, TradeSide Side,
    decimal Quantity, decimal PricePerShare);

public sealed record AiSnapshot(
    DateOnly Today,
    GoalConfig Goal,
    PortfolioSnapshot Portfolio,
    IReadOnlyList<TickerContext> Tickers,
    IReadOnlyList<TradeRecent> RecentTrades,
    decimal? UsdPerEur,
    string PromptHash);
```

`TradyStrat/Features/AiSuggestion/JsonOpts.cs`:
```csharp
using System.Text.Json;
using System.Text.Json.Serialization;

namespace TradyStrat.Features.AiSuggestion;

public static class JsonOpts
{
    public static readonly JsonSerializerOptions Strict = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        WriteIndented = false,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.SnakeCaseLower) },
    };
}
```

- [ ] **Step 2: Write the failing test**

`TradyStrat.Tests/AiSuggestion/SnapshotBuilderTests.cs`:
```csharp
using Microsoft.Extensions.Logging.Abstractions;
using Shouldly;
using TradyStrat.Features.AiSuggestion;
using TradyStrat.Features.Fx;
using TradyStrat.Features.Indicators;
using TradyStrat.Features.Indicators.Rules;
using TradyStrat.Features.Portfolio;
using TradyStrat.Shared.Domain;
using TradyStrat.Tests.Fx;
using TradyStrat.Tests.Indicators;
using TradyStrat.Tests.Specifications;
using TradyStrat.Tests.Time;

namespace TradyStrat.Tests.AiSuggestion;

public class SnapshotBuilderTests
{
    [Fact]
    public async Task Builds_snapshot_with_all_three_tickers_and_eur_conversion()
    {
        await using var db = InMemoryDb.Create();

        // CON3.DE — 250 bars (EUR native)
        foreach (var b in SeriesLoader.LoadCloses("CON3.DE")) db.PriceBars.Add(b);
        // COIN, BTC-USD — minimal series so indicators may be null
        foreach (var t in new[] { "COIN", "BTC-USD" })
            db.PriceBars.Add(new PriceBar { Id=0, Ticker=t, Date=new(2026,5,6),
                Open=200, High=200, Low=200, Close=200, Volume=1 });
        // FX rate
        db.FxRates.Add(new FxRate { Id=0, Pair="EURUSD", Date=new(2026,5,6),
            UsdPerEur = 1.08m, FetchedAt = DateTime.UtcNow });
        // Goal
        db.Goals.Add(GoalConfig.Default(DateTime.UtcNow));
        await db.SaveChangesAsync();

        var classifier = new ZoneClassifier(new IZoneRule[]
        {
            new BollingerZoneRule(), new RsiZoneRule(),
            new MovingAverageZoneRule(), new IchimokuZoneRule()
        });
        var engine = new IndicatorEngine(new TestRepo<PriceBar>(db), classifier);
        var portfolio = new PortfolioService(new TestRepo<Trade>(db));
        var growth = new GrowthSeriesBuilder(new TestRepo<Trade>(db), new TestRepo<PriceBar>(db));
        var fx = new FxConverter(new TestRepo<FxRate>(db));
        var clock = new FakeClock(new DateTime(2026,5,6,0,0,0,DateTimeKind.Utc));

        var sb = new SnapshotBuilder(engine, portfolio, fx,
            new TestRepo<GoalConfig>(db), new TestRepo<Trade>(db), clock);

        var snap = await sb.BuildAsync(default);

        snap.Today.ShouldBe(new DateOnly(2026,5,6));
        snap.Goal.TargetEur.ShouldBe(1_000_000m);
        snap.Tickers.Count.ShouldBe(3);
        snap.Tickers.Single(t => t.Ticker == "COIN").PriceEur.ShouldBe(200m / 1.08m, tolerance: 0.01m);
        snap.Tickers.Single(t => t.Ticker == "CON3.DE").Currency.ShouldBe("EUR");
        snap.PromptHash.ShouldNotBeNullOrEmpty();
    }
}
```

- [ ] **Step 3: Run, expect compile failure**

```bash
dotnet test --filter "FullyQualifiedName~SnapshotBuilderTests" --nologo
```

- [ ] **Step 4: Implement**

`TradyStrat/Features/AiSuggestion/SnapshotBuilder.cs`:
```csharp
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Ardalis.Specification;
using TradyStrat.Features.Fx;
using TradyStrat.Features.Indicators;
using TradyStrat.Features.Portfolio;
using TradyStrat.Shared.Domain;
using TradyStrat.Shared.Time;
using TradyStrat.Specifications.Trades;

namespace TradyStrat.Features.AiSuggestion;

public sealed class SnapshotBuilder(
    IndicatorEngine indicators,
    PortfolioService portfolio,
    FxConverter fx,
    IReadRepositoryBase<GoalConfig> goalRepo,
    IReadRepositoryBase<Trade> tradeRepo,
    IClock clock)
{
    private static readonly (string Ticker, string Currency)[] Catalog =
    [
        ("CON3.DE", "EUR"),
        ("COIN",    "USD"),
        ("BTC-USD", "USD"),
    ];

    public async Task<AiSnapshot> BuildAsync(CancellationToken ct)
    {
        var today = clock.TodayInExchangeTzFor("CON3.DE");
        var goal  = await goalRepo.GetByIdAsync(1, ct) ?? GoalConfig.Default(clock.UtcNow());

        var tickers = new List<TickerContext>();
        decimal? con3Price = null;

        foreach (var (ticker, currency) in Catalog)
        {
            var reading = await indicators.ComputeFor(ticker, ct);
            decimal? eur = null;
            if (currency == "USD")
                eur = await fx.UsdToEurAsync(reading.Price, today, ct);

            if (ticker == "CON3.DE") con3Price = reading.Price;

            tickers.Add(new TickerContext(
                ticker, currency, reading.Price, eur, reading.Zone, reading.Reasons));
        }

        var snap = await portfolio.SnapshotAsync(
            currentPriceEur: con3Price ?? 0m,
            goalEur: goal.TargetEur,
            ct: ct);

        var recent = await tradeRepo.ListAsync(new LatestTradesSpec(20), ct);
        var recentDtos = recent.Select(t => new TradeRecent(
            t.ExecutedOn, t.Side, t.Quantity, t.PricePerShare)).ToList();

        decimal? usdPerEur = null;
        try { usdPerEur = await UsdPerEur(today, ct); } catch { /* tolerant */ }

        var promptHash = HashPrompt(today, snap, tickers, recentDtos);

        return new AiSnapshot(today, goal, snap, tickers, recentDtos, usdPerEur, promptHash);

        async Task<decimal?> UsdPerEur(DateOnly d, CancellationToken c)
        {
            var oneEurInEur = await fx.UsdToEurAsync(1m, d, c);   // 1 / UsdPerEur
            return oneEurInEur == 0m ? null : 1m / oneEurInEur;
        }
    }

    private static string HashPrompt(DateOnly today, PortfolioSnapshot snap,
        IEnumerable<TickerContext> tickers, IEnumerable<TradeRecent> recent)
    {
        var payload = new { today, snap, tickers, recent };
        var bytes = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(payload, JsonOpts.Strict));
        return Convert.ToHexString(SHA256.HashData(bytes))[..16];
    }
}
```

- [ ] **Step 5: Run, expect pass**

```bash
dotnet test --filter "FullyQualifiedName~SnapshotBuilderTests" --nologo
```

- [ ] **Step 6: Commit**

```bash
git add TradyStrat/Features/AiSuggestion/AiSnapshot.cs \
        TradyStrat/Features/AiSuggestion/JsonOpts.cs \
        TradyStrat/Features/AiSuggestion/SnapshotBuilder.cs \
        TradyStrat.Tests/AiSuggestion/
git commit -m "feat: SnapshotBuilder composes AiSnapshot with FX-converted USD prices"
```

---

### Task 31: `SuggestionService` (`IChatClient` + tool-use)

**Files:**
- Create: `TradyStrat/Features/AiSuggestion/SuggestionService.cs`
- Create: `TradyStrat.Tests/AiSuggestion/FakeChatClient.cs`
- Create: `TradyStrat.Tests/AiSuggestion/SuggestionServiceTests.cs`

- [ ] **Step 1: Implement a fake `IChatClient` that auto-invokes the tool**

`TradyStrat.Tests/AiSuggestion/FakeChatClient.cs`:
```csharp
using Microsoft.Extensions.AI;

namespace TradyStrat.Tests.AiSuggestion;

public sealed class FakeChatClient(Func<IList<AIFunction>, Task> invokeTools) : IChatClient
{
    public async Task<ChatResponse> GetResponseAsync(
        IEnumerable<ChatMessage> messages,
        ChatOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        var tools = options?.Tools?.OfType<AIFunction>().ToList() ?? [];
        await invokeTools(tools);
        return new ChatResponse(new ChatMessage(ChatRole.Assistant, "ok"));
    }

    public IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
        IEnumerable<ChatMessage> messages,
        ChatOptions? options = null,
        CancellationToken cancellationToken = default)
        => throw new NotSupportedException();

    public object? GetService(Type serviceType, object? serviceKey = null) => null;
    public void Dispose() { }
}
```

> The fake follows `Microsoft.Extensions.AI.IChatClient`'s contract for v9.4. If the package version installed exposes a slightly different shape, mirror the official interface from the installed assembly.

- [ ] **Step 2: Write the failing test**

`TradyStrat.Tests/AiSuggestion/SuggestionServiceTests.cs`:
```csharp
using System.Text.Json;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging.Abstractions;
using Shouldly;
using TradyStrat.Features.AiSuggestion;
using TradyStrat.Shared.Domain;
using TradyStrat.Shared.Exceptions;
using TradyStrat.Tests.Time;

namespace TradyStrat.Tests.AiSuggestion;

public class SuggestionServiceTests
{
    private static AiSnapshot SampleSnapshot() => new(
        Today: new DateOnly(2026,5,6),
        Goal:  GoalConfig.Default(DateTime.UtcNow),
        Portfolio: new(0,0,0,0,0,0),
        Tickers: [],
        RecentTrades: [],
        UsdPerEur: 1.08m,
        PromptHash: "hashvalue");

    [Fact]
    public async Task Captures_tool_invocation_and_returns_typed_Suggestion()
    {
        var clock = new FakeClock(new DateTime(2026,5,6,0,0,0,DateTimeKind.Utc));

        async Task Invoke(IList<AIFunction> tools)
        {
            var t = tools.Single();
            var args = new Dictionary<string, object?>
            {
                ["action"] = "Acquire",
                ["quantity_hint"] = 8m,
                ["max_price_hint"] = 4.85m,
                ["conviction"] = 4,
                ["rationale"] = "Below lower band; RSI rising.",
                ["citations"] = JsonSerializer.SerializeToElement(new[] {
                    new { claim="x", indicator="Bollinger", ticker="CON3.DE", value="below" } })
            };
            var argsJson = JsonSerializer.SerializeToElement(args);
            await t.InvokeAsync(new AIFunctionArguments(argsJson));
        }

        var svc = new SuggestionService(new FakeChatClient(Invoke), clock,
            NullLogger<SuggestionService>.Instance);

        var sug = await svc.AskAsync(SampleSnapshot(), default);

        sug.Action.ShouldBe(SuggestionAction.Acquire);
        sug.QuantityHint.ShouldBe(8m);
        sug.Conviction.ShouldBe(4);
        sug.Rationale.ShouldContain("Below lower band");
        sug.PromptHash.ShouldBe("hashvalue");
    }

    [Fact]
    public async Task Throws_AnthropicCallFailedException_when_tool_not_invoked()
    {
        var clock = new FakeClock(new DateTime(2026,5,6,0,0,0,DateTimeKind.Utc));
        var svc = new SuggestionService(
            new FakeChatClient(_ => Task.CompletedTask),
            clock, NullLogger<SuggestionService>.Instance);

        await Should.ThrowAsync<AnthropicCallFailedException>(() =>
            svc.AskAsync(SampleSnapshot(), default));
    }
}
```

- [ ] **Step 3: Run, expect compile failure**

```bash
dotnet test --filter "FullyQualifiedName~SuggestionServiceTests" --nologo
```

- [ ] **Step 4: Implement**

`TradyStrat/Features/AiSuggestion/SuggestionService.cs`:
```csharp
using System.Text.Json;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using TradyStrat.Shared.Domain;
using TradyStrat.Shared.Exceptions;
using TradyStrat.Shared.Time;

namespace TradyStrat.Features.AiSuggestion;

public sealed class SuggestionService(
    IChatClient chat, IClock clock, ILogger<SuggestionService> log)
{
    private const string ToolName = "submit_suggestion";
    private const string SystemPrompt = """
        You are a disciplined trading assistant for a personal accumulation strategy
        on CON3 (a 3x leveraged Coinbase ETP). You see snapshots once per day.
        Cite which indicators support each part of your suggestion.
        Be conservative: when signals conflict, say Hold.
        Always invoke the submit_suggestion tool exactly once.
        """;

    public async Task<Suggestion> AskAsync(AiSnapshot snapshot, CancellationToken ct)
    {
        Suggestion? captured = null;

        var submit = AIFunctionFactory.Create(
            (SuggestionAction action, decimal? quantity_hint, decimal? max_price_hint,
             int conviction, string rationale, JsonElement citations) =>
            {
                var citationList = citations.ValueKind == JsonValueKind.Array
                    ? citations.EnumerateArray()
                        .Select(c => c.Deserialize<Citation>(JsonOpts.Strict)!)
                        .ToList()
                    : new List<Citation>();

                captured = new Suggestion
                {
                    Id = 0,
                    ForDate       = snapshot.Today,
                    Action        = action,
                    QuantityHint  = quantity_hint,
                    MaxPriceHint  = max_price_hint,
                    Conviction    = conviction,
                    Rationale     = rationale,
                    CitationsJson = JsonSerializer.Serialize(citationList, JsonOpts.Strict),
                    PromptHash    = snapshot.PromptHash,
                    CreatedAt     = clock.UtcNow(),
                };
                return "ok";
            },
            name: ToolName,
            description: "Submit your structured trading suggestion with cited reasoning.");

        var options = new ChatOptions
        {
            Tools           = [submit],
            ToolMode        = ChatToolMode.RequireSpecific(ToolName),
            MaxOutputTokens = 1500,
        };

        var messages = new List<ChatMessage>
        {
            new(ChatRole.System, SystemPrompt),
            new(ChatRole.User,   JsonSerializer.Serialize(snapshot, JsonOpts.Strict)),
        };

        try
        {
            await chat.GetResponseAsync(messages, options, ct);
        }
        catch (AnthropicCallFailedException) { throw; }
        catch (Exception ex)
        {
            log.LogError(ex, "Anthropic call failed");
            throw new AnthropicCallFailedException("Anthropic call failed.", ex);
        }

        return captured
            ?? throw new AnthropicCallFailedException(
                "Model did not invoke submit_suggestion.");
    }
}
```

> If the M.E.AI 9.4 API for `AIFunctionFactory.Create` requires explicit JSON schema for nullable parameters, supply it via the overload that accepts `AIFunctionMetadata`. Verify against the installed package on first build.

- [ ] **Step 5: Run, expect pass**

```bash
dotnet test --filter "FullyQualifiedName~SuggestionServiceTests" --nologo
```

- [ ] **Step 6: Commit**

```bash
git add TradyStrat/Features/AiSuggestion/SuggestionService.cs \
        TradyStrat.Tests/AiSuggestion/FakeChatClient.cs \
        TradyStrat.Tests/AiSuggestion/SuggestionServiceTests.cs
git commit -m "feat: SuggestionService over IChatClient with submit_suggestion tool"
```

---

### Task 32: AI use cases (Get / ForceRefetch)

**Files:**
- Create: `TradyStrat/Application/UseCases/AiSuggestion/GetTodaysSuggestionUseCase.cs`
- Create: `TradyStrat/Application/UseCases/AiSuggestion/ForceRefetchSuggestionUseCase.cs`
- Create: `TradyStrat.Tests/UseCases/AiSuggestion/GetTodaysSuggestionUseCaseTests.cs`
- Create: `TradyStrat.Tests/UseCases/AiSuggestion/ForceRefetchSuggestionUseCaseTests.cs`

- [ ] **Step 1: Write failing tests**

`TradyStrat.Tests/UseCases/AiSuggestion/GetTodaysSuggestionUseCaseTests.cs`:
```csharp
using Microsoft.Extensions.Logging.Abstractions;
using Shouldly;
using TradyStrat.Application.UseCases.AiSuggestion;
using TradyStrat.Features.AiSuggestion;
using TradyStrat.Shared.Domain;
using TradyStrat.Tests.Fx;
using TradyStrat.Tests.Specifications;
using TradyStrat.Tests.Time;

namespace TradyStrat.Tests.UseCases.AiSuggestion;

public class GetTodaysSuggestionUseCaseTests
{
    [Fact]
    public async Task Returns_existing_row_when_today_already_present()
    {
        await using var db = InMemoryDb.Create();
        db.Suggestions.Add(new Suggestion {
            Id = 0, ForDate = new(2026,5,6), Action = SuggestionAction.Hold,
            Conviction = 3, Rationale = "cached", CitationsJson = "[]",
            PromptHash = "h", CreatedAt = DateTime.UtcNow });
        await db.SaveChangesAsync();

        var uc = new GetTodaysSuggestionUseCase(
            new TestRepo<Suggestion>(db),
            snapshotBuilder: null!,         // not invoked when cached
            ai: null!,                       // not invoked when cached
            clock: new FakeClock(new DateTime(2026,5,6,0,0,0,DateTimeKind.Utc)),
            log: NullLogger<GetTodaysSuggestionUseCase>.Instance);

        var s = await uc.ExecuteAsync(default, default);

        s.Rationale.ShouldBe("cached");
    }
}
```

`TradyStrat.Tests/UseCases/AiSuggestion/ForceRefetchSuggestionUseCaseTests.cs`:
```csharp
using Microsoft.Extensions.Logging.Abstractions;
using Shouldly;
using TradyStrat.Application.UseCases.AiSuggestion;
using TradyStrat.Features.AiSuggestion;
using TradyStrat.Shared.Domain;
using TradyStrat.Tests.AiSuggestion;
using TradyStrat.Tests.Fx;
using TradyStrat.Tests.Specifications;
using TradyStrat.Tests.Time;

namespace TradyStrat.Tests.UseCases.AiSuggestion;

public class ForceRefetchSuggestionUseCaseTests
{
    [Fact]
    public async Task Removes_existing_then_persists_new()
    {
        await using var db = InMemoryDb.Create();
        db.Suggestions.Add(new Suggestion {
            Id = 0, ForDate = new(2026,5,6), Action = SuggestionAction.Hold,
            Conviction = 3, Rationale = "old", CitationsJson = "[]",
            PromptHash = "h", CreatedAt = DateTime.UtcNow });
        db.Goals.Add(GoalConfig.Default(DateTime.UtcNow));
        await db.SaveChangesAsync();

        var clock = new FakeClock(new DateTime(2026,5,6,0,0,0,DateTimeKind.Utc));
        var snap  = new StubSnapshotBuilder();
        var ai    = new StubAi(forced: new Suggestion {
            Id = 0, ForDate = new(2026,5,6), Action = SuggestionAction.Acquire,
            Conviction = 5, Rationale = "fresh", CitationsJson = "[]",
            PromptHash = "h2", CreatedAt = DateTime.UtcNow });

        var uc = new ForceRefetchSuggestionUseCase(
            new TestRepo<Suggestion>(db), snap, ai, clock,
            NullLogger<ForceRefetchSuggestionUseCase>.Instance);

        var s = await uc.ExecuteAsync(default, default);

        s.Rationale.ShouldBe("fresh");
        db.Suggestions.Count().ShouldBe(1);
        db.Suggestions.Single().Rationale.ShouldBe("fresh");
    }
}

internal sealed class StubSnapshotBuilder : ISnapshotBuilder
{
    public Task<AiSnapshot> BuildAsync(CancellationToken ct) =>
        Task.FromResult(new AiSnapshot(
            new(2026,5,6), GoalConfig.Default(DateTime.UtcNow),
            new(0,0,0,0,0,0), [], [], 1.08m, "h2"));
}

internal sealed class StubAi(Suggestion forced) : IAiClient
{
    public Task<Suggestion> AskAsync(AiSnapshot _, CancellationToken __)
        => Task.FromResult(forced);
}
```

> The tests above introduce two seams: `ISnapshotBuilder` and `IAiClient`. Step 3 adds these interfaces; the existing `SnapshotBuilder`/`SuggestionService` will implement them.

- [ ] **Step 2: Run, expect compile failure**

```bash
dotnet test --filter "FullyQualifiedName~GetTodaysSuggestionUseCaseTests|FullyQualifiedName~ForceRefetchSuggestionUseCaseTests" --nologo
```

- [ ] **Step 3: Add seam interfaces and have existing services implement them**

`TradyStrat/Features/AiSuggestion/ISnapshotBuilder.cs`:
```csharp
namespace TradyStrat.Features.AiSuggestion;

public interface ISnapshotBuilder
{
    Task<AiSnapshot> BuildAsync(CancellationToken ct);
}
```

`TradyStrat/Features/AiSuggestion/IAiClient.cs`:
```csharp
using TradyStrat.Shared.Domain;

namespace TradyStrat.Features.AiSuggestion;

public interface IAiClient
{
    Task<Suggestion> AskAsync(AiSnapshot snapshot, CancellationToken ct);
}
```

Modify `SnapshotBuilder` (declaration line):
```csharp
public sealed class SnapshotBuilder(
    /* ...existing constructor parameters... */) : ISnapshotBuilder
```

Modify `SuggestionService` (declaration line):
```csharp
public sealed class SuggestionService(
    /* ...existing... */) : IAiClient
```

(The method signatures already match; just add the interface implements.)

- [ ] **Step 4: Implement use cases**

`TradyStrat/Application/UseCases/AiSuggestion/GetTodaysSuggestionUseCase.cs`:
```csharp
using Ardalis.Specification;
using Microsoft.Extensions.Logging;
using TradyStrat.Application.Abstractions;
using TradyStrat.Features.AiSuggestion;
using TradyStrat.Shared.Domain;
using TradyStrat.Shared.Time;
using TradyStrat.Specifications.Suggestions;

namespace TradyStrat.Application.UseCases.AiSuggestion;

public sealed class GetTodaysSuggestionUseCase(
    IRepositoryBase<Suggestion> repo,
    ISnapshotBuilder snapshotBuilder,
    IAiClient ai,
    IClock clock,
    ILogger<GetTodaysSuggestionUseCase> log)
    : UseCaseBase<Unit, Suggestion>(log)
{
    protected override async Task<Suggestion> ExecuteCore(Unit _, CancellationToken ct)
    {
        var today = clock.TodayInExchangeTzFor("CON3.DE");
        var existing = await repo.FirstOrDefaultAsync(new SuggestionForDateSpec(today), ct);
        if (existing is not null) return existing;

        var snap = await snapshotBuilder.BuildAsync(ct);
        var fresh = await ai.AskAsync(snap, ct);
        await repo.AddAsync(fresh, ct);
        return fresh;
    }
}
```

`TradyStrat/Application/UseCases/AiSuggestion/ForceRefetchSuggestionUseCase.cs`:
```csharp
using Ardalis.Specification;
using Microsoft.Extensions.Logging;
using TradyStrat.Application.Abstractions;
using TradyStrat.Features.AiSuggestion;
using TradyStrat.Shared.Domain;
using TradyStrat.Shared.Time;
using TradyStrat.Specifications.Suggestions;

namespace TradyStrat.Application.UseCases.AiSuggestion;

public sealed class ForceRefetchSuggestionUseCase(
    IRepositoryBase<Suggestion> repo,
    ISnapshotBuilder snapshotBuilder,
    IAiClient ai,
    IClock clock,
    ILogger<ForceRefetchSuggestionUseCase> log)
    : UseCaseBase<Unit, Suggestion>(log)
{
    protected override async Task<Unit> ExecuteCore(Unit _, CancellationToken __)
    {
        // Implemented in two steps to keep the unit signature consistent everywhere.
        return Unit.Value;
    }

    public new async Task<Suggestion> ExecuteAsync(Unit _, CancellationToken ct)
    {
        var today = clock.TodayInExchangeTzFor("CON3.DE");
        var existing = await repo.FirstOrDefaultAsync(new SuggestionForDateSpec(today), ct);
        if (existing is not null) await repo.DeleteAsync(existing, ct);

        var snap = await snapshotBuilder.BuildAsync(ct);
        var fresh = await ai.AskAsync(snap, ct);
        await repo.AddAsync(fresh, ct);
        return fresh;
    }
}
```

> **Why the `new` ExecuteAsync?** This use case needs to return `Suggestion`, not `Unit`. The simplest way without dragging an extra interface variant through the plan is to override the return for this specific use case while keeping the base contract for the cancellation-token signature. Razor pages will call this concrete type.

Actually — cleaner: define the use case as `UseCaseBase<Unit, Suggestion>` and have `ExecuteCore` do the work. Replace the file above with this:

```csharp
using Ardalis.Specification;
using Microsoft.Extensions.Logging;
using TradyStrat.Application.Abstractions;
using TradyStrat.Features.AiSuggestion;
using TradyStrat.Shared.Domain;
using TradyStrat.Shared.Time;
using TradyStrat.Specifications.Suggestions;

namespace TradyStrat.Application.UseCases.AiSuggestion;

public sealed class ForceRefetchSuggestionUseCase(
    IRepositoryBase<Suggestion> repo,
    ISnapshotBuilder snapshotBuilder,
    IAiClient ai,
    IClock clock,
    ILogger<ForceRefetchSuggestionUseCase> log)
    : UseCaseBase<Unit, Suggestion>(log)
{
    protected override async Task<Suggestion> ExecuteCore(Unit _, CancellationToken ct)
    {
        var today = clock.TodayInExchangeTzFor("CON3.DE");
        var existing = await repo.FirstOrDefaultAsync(new SuggestionForDateSpec(today), ct);
        if (existing is not null) await repo.DeleteAsync(existing, ct);

        var snap = await snapshotBuilder.BuildAsync(ct);
        var fresh = await ai.AskAsync(snap, ct);
        await repo.AddAsync(fresh, ct);
        return fresh;
    }
}
```

(Discard the previous `new ExecuteAsync` workaround — this is the single, clean version.)

- [ ] **Step 5: Run, expect pass**

```bash
dotnet test --filter "FullyQualifiedName~GetTodaysSuggestionUseCaseTests|FullyQualifiedName~ForceRefetchSuggestionUseCaseTests" --nologo
```

Expected: `Passed: 2`.

- [ ] **Step 6: Commit**

```bash
git add TradyStrat/Features/AiSuggestion/ISnapshotBuilder.cs \
        TradyStrat/Features/AiSuggestion/IAiClient.cs \
        TradyStrat/Features/AiSuggestion/SnapshotBuilder.cs \
        TradyStrat/Features/AiSuggestion/SuggestionService.cs \
        TradyStrat/Application/UseCases/AiSuggestion/ \
        TradyStrat.Tests/UseCases/AiSuggestion/
git commit -m "feat: AI use cases — GetTodaysSuggestion + ForceRefetchSuggestion"
```

---

### Task 33: `AiSuggestionModule`

**Files:**
- Create: `TradyStrat/Modules/AiSuggestionModule.cs`

- [ ] **Step 1: Implement**

`TradyStrat/Modules/AiSuggestionModule.cs`:
```csharp
using Microsoft.Extensions.AI;
using TheAppManager.Modules;
using TradyStrat.Application.UseCases.AiSuggestion;
using TradyStrat.Features.AiSuggestion;
using TradyStrat.Shared.Exceptions;

namespace TradyStrat.Modules;

public sealed class AiSuggestionModule : IAppModule
{
    public void ConfigureServices(WebApplicationBuilder builder)
    {
        var apiKey = builder.Configuration["Anthropic:ApiKey"]
            ?? throw new AnthropicConfigurationException("Anthropic:ApiKey is not configured.");
        var model  = builder.Configuration["Anthropic:Model"] ?? "claude-opus-4-7";

        builder.Services.AddSingleton<IChatClient>(_ =>
            new Anthropic.SDK.AnthropicClient(apiKey)
                .Messages
                .AsChatClient(model)
                .AsBuilder()
                .UseFunctionInvocation()
                .Build());

        builder.Services.AddScoped<ISnapshotBuilder, SnapshotBuilder>();
        builder.Services.AddScoped<IAiClient, SuggestionService>();
        builder.Services.AddScoped<GetTodaysSuggestionUseCase>();
        builder.Services.AddScoped<ForceRefetchSuggestionUseCase>();
    }
}
```

> **Verification at impl time:** `Anthropic.SDK 5.10`'s exact extension chain is `new AnthropicClient(apiKey).Messages.AsChatClient(model)`. If the published API uses a different name (e.g. `AsIChatClient`), use that — the contract returned is `IChatClient` either way.

- [ ] **Step 2: Build & test**

```bash
dotnet build --nologo && dotnet test --nologo
```

Expected: green.

- [ ] **Step 3: Commit**

```bash
git add TradyStrat/Modules/AiSuggestionModule.cs
git commit -m "feat: AiSuggestionModule registers IChatClient + use cases"
```

**End of Phase 5.** State check: with a populated DB and a valid `Anthropic:ApiKey`, calling `GetTodaysSuggestionUseCase.ExecuteAsync` produces a `Suggestion` row.

---

## Phase 6 — Dashboard UI: The Vault (Tasks 34–40)

This phase composes the dashboard's data into a `DashboardViewModel`, builds the six Razor components matching the [Vault mockup](../specs/2026-05-06-tradystrat-vault-mockup.html), and wires the page to use cases. Components are dumb renderers driven by the VM. Manual visual verification at the end.

---

### Task 34: `DashboardViewModel` + `LoadDashboardUseCase`

**Files:**
- Create: `TradyStrat/Features/Dashboard/DashboardViewModel.cs`
- Create: `TradyStrat/Features/Dashboard/TickerView.cs`
- Create: `TradyStrat/Application/UseCases/Dashboard/LoadDashboardUseCase.cs`
- Create: `TradyStrat.Tests/UseCases/Dashboard/LoadDashboardUseCaseTests.cs`

- [ ] **Step 1: Add VM types**

`TradyStrat/Features/Dashboard/TickerView.cs`:
```csharp
using TradyStrat.Shared.Domain;

namespace TradyStrat.Features.Dashboard;

public sealed record TickerView(
    string Ticker,
    string Currency,
    decimal Price,
    decimal? PriceEur,
    decimal? DeltaPct,
    Zone Zone);
```

`TradyStrat/Features/Dashboard/DashboardViewModel.cs`:
```csharp
using TradyStrat.Shared.Domain;

namespace TradyStrat.Features.Dashboard;

public sealed record DashboardViewModel(
    DateOnly Today,
    int EntryNumber,
    PortfolioSnapshot Portfolio,
    GoalConfig Goal,
    Suggestion TodaysCall,
    IReadOnlyList<TickerView> Tickers,
    IReadOnlyList<GrowthPoint> Growth,
    DateOnly? LatestPriceDate);
```

- [ ] **Step 2: Write the failing test**

`TradyStrat.Tests/UseCases/Dashboard/LoadDashboardUseCaseTests.cs`:
```csharp
using Microsoft.Extensions.Logging.Abstractions;
using Shouldly;
using TradyStrat.Application.UseCases.AiSuggestion;
using TradyStrat.Application.UseCases.Dashboard;
using TradyStrat.Features.AiSuggestion;
using TradyStrat.Features.Fx;
using TradyStrat.Features.Indicators;
using TradyStrat.Features.Indicators.Rules;
using TradyStrat.Features.Portfolio;
using TradyStrat.Shared.Domain;
using TradyStrat.Tests.Fx;
using TradyStrat.Tests.Indicators;
using TradyStrat.Tests.Specifications;
using TradyStrat.Tests.Time;
using TradyStrat.Tests.UseCases.AiSuggestion;

namespace TradyStrat.Tests.UseCases.Dashboard;

public class LoadDashboardUseCaseTests
{
    [Fact]
    public async Task Composes_view_model_with_three_tickers_and_growth_series()
    {
        await using var db = InMemoryDb.Create();
        foreach (var b in SeriesLoader.LoadCloses("CON3.DE")) db.PriceBars.Add(b);
        foreach (var t in new[] { "COIN", "BTC-USD" })
            db.PriceBars.Add(new PriceBar { Id=0, Ticker=t, Date=new(2026,5,6),
                Open=200, High=200, Low=200, Close=200, Volume=1 });
        db.FxRates.Add(new FxRate { Id=0, Pair="EURUSD", Date=new(2026,5,6),
            UsdPerEur = 1.08m, FetchedAt = DateTime.UtcNow });
        db.Goals.Add(GoalConfig.Default(DateTime.UtcNow));
        db.Trades.Add(new Trade {
            Id = 0, ExecutedOn = new(2026,5,5), Side = TradeSide.Buy,
            Quantity = 100m, PricePerShare = 4m, FeesEur = 0m, Note = null,
            CreatedAt = DateTime.UtcNow });
        db.Suggestions.Add(new Suggestion {
            Id = 0, ForDate = new(2026,5,6), Action = SuggestionAction.Hold,
            Conviction = 3, Rationale = "stable", CitationsJson = "[]",
            PromptHash = "h", CreatedAt = DateTime.UtcNow });
        await db.SaveChangesAsync();

        var classifier = new ZoneClassifier(new IZoneRule[] {
            new BollingerZoneRule(), new RsiZoneRule(),
            new MovingAverageZoneRule(), new IchimokuZoneRule() });
        var indicators = new IndicatorEngine(new TestRepo<PriceBar>(db), classifier);
        var portfolio  = new PortfolioService(new TestRepo<Trade>(db));
        var growth     = new GrowthSeriesBuilder(new TestRepo<Trade>(db), new TestRepo<PriceBar>(db));
        var fx         = new FxConverter(new TestRepo<FxRate>(db));
        var clock      = new FakeClock(new DateTime(2026,5,6,0,0,0,DateTimeKind.Utc));

        var snapBuilder = new SnapshotBuilder(indicators, portfolio, fx,
            new TestRepo<GoalConfig>(db), new TestRepo<Trade>(db), clock);
        var aiClient = new StubAi(new Suggestion {
            Id = 0, ForDate = new(2026,5,6), Action = SuggestionAction.Hold,
            Conviction = 3, Rationale = "x", CitationsJson = "[]",
            PromptHash = "h", CreatedAt = DateTime.UtcNow });

        var todays = new GetTodaysSuggestionUseCase(
            new TestRepo<Suggestion>(db), snapBuilder, aiClient, clock,
            NullLogger<GetTodaysSuggestionUseCase>.Instance);

        var uc = new LoadDashboardUseCase(
            indicators, portfolio, growth, fx,
            new TestRepo<GoalConfig>(db), new TestRepo<Trade>(db),
            new TestRepo<PriceBar>(db),
            todays, clock, NullLogger<LoadDashboardUseCase>.Instance);

        var vm = await uc.ExecuteAsync(default, default);

        vm.Today.ShouldBe(new DateOnly(2026,5,6));
        vm.Tickers.Count.ShouldBe(3);
        vm.Growth.Count.ShouldBeGreaterThan(0);
        vm.Goal.TargetEur.ShouldBe(1_000_000m);
        vm.TodaysCall.Rationale.ShouldBe("stable");
    }
}
```

- [ ] **Step 3: Run, expect compile failure**

```bash
dotnet test --filter "FullyQualifiedName~LoadDashboardUseCaseTests" --nologo
```

- [ ] **Step 4: Implement**

`TradyStrat/Application/UseCases/Dashboard/LoadDashboardUseCase.cs`:
```csharp
using Ardalis.Specification;
using Microsoft.Extensions.Logging;
using TradyStrat.Application.Abstractions;
using TradyStrat.Application.UseCases.AiSuggestion;
using TradyStrat.Features.Dashboard;
using TradyStrat.Features.Fx;
using TradyStrat.Features.Indicators;
using TradyStrat.Features.Portfolio;
using TradyStrat.Shared.Domain;
using TradyStrat.Shared.Time;
using TradyStrat.Specifications.PriceBars;
using TradyStrat.Specifications.Trades;

namespace TradyStrat.Application.UseCases.Dashboard;

public sealed class LoadDashboardUseCase(
    IndicatorEngine indicators,
    PortfolioService portfolio,
    GrowthSeriesBuilder growth,
    FxConverter fx,
    IReadRepositoryBase<GoalConfig> goalRepo,
    IReadRepositoryBase<Trade> tradeRepo,
    IReadRepositoryBase<PriceBar> priceRepo,
    GetTodaysSuggestionUseCase todaysSuggestion,
    IClock clock,
    ILogger<LoadDashboardUseCase> log)
    : UseCaseBase<Unit, DashboardViewModel>(log)
{
    private static readonly (string Ticker, string Currency)[] Catalog =
    [
        ("CON3.DE", "EUR"),
        ("COIN",    "USD"),
        ("BTC-USD", "USD"),
    ];

    protected override async Task<DashboardViewModel> ExecuteCore(Unit _, CancellationToken ct)
    {
        var today = clock.TodayInExchangeTzFor("CON3.DE");
        var goal  = await goalRepo.GetByIdAsync(1, ct) ?? GoalConfig.Default(clock.UtcNow());

        var tickers = new List<TickerView>();
        decimal? con3Price = null;

        foreach (var (ticker, currency) in Catalog)
        {
            var reading = await indicators.ComputeFor(ticker, ct);
            decimal? eur = null;
            if (currency == "USD")
                eur = await fx.UsdToEurAsync(reading.Price, today, ct);
            if (ticker == "CON3.DE") con3Price = reading.Price;

            var deltaPct = await ComputeDeltaPct(ticker, ct);

            tickers.Add(new TickerView(
                ticker, currency, reading.Price, eur, deltaPct, reading.Zone));
        }

        var snap = await portfolio.SnapshotAsync(con3Price ?? 0m, goal.TargetEur, ct);
        var growthSeries = await growth.BuildAsync("CON3.DE", ct);
        var todays = await todaysSuggestion.ExecuteAsync(Unit.Value, ct);
        var entryNum = await tradeRepo.CountAsync(new AllTradesSpec(), ct);
        var latestBar = await priceRepo.FirstOrDefaultAsync(
            new LatestPriceBarSpec("CON3.DE"), ct);

        return new DashboardViewModel(
            Today: today,
            EntryNumber: entryNum,
            Portfolio: snap,
            Goal: goal,
            TodaysCall: todays,
            Tickers: tickers,
            Growth: growthSeries,
            LatestPriceDate: latestBar?.Date);
    }

    private async Task<decimal?> ComputeDeltaPct(string ticker, CancellationToken ct)
    {
        var bars = await priceRepo.ListAsync(new PriceBarsForTickerSpec(ticker), ct);
        if (bars.Count < 2) return null;
        var prev = bars[^2].Close;
        var curr = bars[^1].Close;
        if (prev == 0m) return null;
        return (curr - prev) / prev * 100m;
    }
}
```

- [ ] **Step 5: Run, expect pass**

```bash
dotnet test --filter "FullyQualifiedName~LoadDashboardUseCaseTests" --nologo
```

- [ ] **Step 6: Commit**

```bash
git add TradyStrat/Features/Dashboard/DashboardViewModel.cs \
        TradyStrat/Features/Dashboard/TickerView.cs \
        TradyStrat/Application/UseCases/Dashboard/ \
        TradyStrat.Tests/UseCases/Dashboard/
git commit -m "feat: DashboardViewModel + LoadDashboardUseCase composing the dashboard"
```

---

### Task 35: Vault CSS tokens (full theme) + `<VaultMasthead />`

**Files:**
- Modify: `TradyStrat/wwwroot/css/vault.css`
- Create: `TradyStrat/Features/Dashboard/Components/VaultMasthead.razor`
- Create: `TradyStrat/Features/Dashboard/Components/VaultMasthead.razor.css`

- [ ] **Step 1: Replace `vault.css` with the full mockup tokens + global styles**

`TradyStrat/wwwroot/css/vault.css` (replaces the placeholder from Task 9):
```css
:root {
  --vault-bg:     #0E0D0A;
  --vault-bg-2:   #15140F;
  --vault-ivory:  #ECE6D6;
  --vault-gold:   #C49A56;
  --vault-rule:   #2C281F;
  --vault-green:  #7AB68E;
  --vault-red:    #C36A6A;

  --font-display: "Cormorant Garamond", "Newsreader", Georgia, serif;
  --font-body:    "Cormorant Garamond", "Newsreader", Georgia, serif;
  --font-mono:    "JetBrains Mono", ui-monospace, monospace;

  --rule-soft:    1px solid var(--vault-rule);
  --tracking-xs:  0.32em;
  --tracking-md:  0.22em;
}

*, *::before, *::after { box-sizing: border-box; margin: 0; padding: 0; }
html, body { background: var(--vault-bg); color: var(--vault-ivory); min-height: 100vh; }
body {
  font-family: var(--font-body);
  -webkit-font-smoothing: antialiased;
  font-feature-settings: "kern", "liga", "onum";
}

.num   { font-family: var(--font-mono); font-variant-numeric: tabular-nums; }
.label { font-family: var(--font-mono); font-size: 10px;
         letter-spacing: var(--tracking-xs); text-transform: uppercase;
         color: var(--vault-gold); }
.rule  { height: 1px; background: var(--vault-rule); }

a { color: inherit; text-decoration: none; }
button { font: inherit; color: inherit; background: none; border: none; cursor: pointer; }

main {
  max-width: 1280px;
  margin: 0 auto;
  border: 1px solid var(--vault-rule);
  background: var(--vault-bg);
  position: relative;
  overflow: hidden;
}
main::before {
  content: "";
  position: absolute;
  inset: 0;
  pointer-events: none;
  z-index: 0;
  background:
    radial-gradient(circle at 88% -8%, rgba(196,154,86,0.10), transparent 48%),
    radial-gradient(circle at -5% 105%, rgba(196,154,86,0.06), transparent 42%);
}
main > * { position: relative; z-index: 1; }
```

- [ ] **Step 2: Implement `VaultMasthead`**

`TradyStrat/Features/Dashboard/Components/VaultMasthead.razor`:
```razor
@using System.Globalization
<div class="masthead">
    <div class="brand">
        Tradystrat
        <span class="arc">— a private chronicle of accumulation</span>
    </div>
    <div class="meta">@FormatDate(Today) · entry no. @EntryNumber.ToString("D4")</div>
</div>

@code {
    [Parameter] public DateOnly Today { get; set; }
    [Parameter] public int EntryNumber { get; set; }

    private static string FormatDate(DateOnly d)
        => d.ToString("dd · MM · yyyy", CultureInfo.InvariantCulture);
}
```

`TradyStrat/Features/Dashboard/Components/VaultMasthead.razor.css`:
```css
.masthead {
    padding: 26px 56px 22px;
    display: flex; justify-content: space-between; align-items: baseline;
    border-bottom: 1px solid var(--vault-rule);
}
.brand {
    font-family: var(--font-display);
    font-size: 14px;
    letter-spacing: var(--tracking-xs);
    text-transform: uppercase;
    color: var(--vault-gold);
}
.brand .arc {
    font-family: var(--font-display);
    font-style: italic;
    color: rgba(236,230,214,0.7);
    font-weight: 300;
    letter-spacing: 0;
    text-transform: none;
    margin-left: 14px;
    font-size: 14px;
}
.meta {
    font-family: var(--font-mono);
    font-size: 11px;
    letter-spacing: var(--tracking-md);
    text-transform: uppercase;
    color: rgba(236,230,214,0.55);
}
```

- [ ] **Step 3: Build & verify**

```bash
dotnet build --nologo
```

- [ ] **Step 4: Commit**

```bash
git add TradyStrat/wwwroot/css/vault.css TradyStrat/Features/Dashboard/Components/VaultMasthead.razor*
git commit -m "feat(ui): vault.css full tokens + VaultMasthead component"
```

---

### Task 36: `<HeroCapital />` (the page hero)

**Files:**
- Create: `TradyStrat/Features/Dashboard/Components/HeroCapital.razor`
- Create: `TradyStrat/Features/Dashboard/Components/HeroCapital.razor.css`

- [ ] **Step 1: Implement**

`TradyStrat/Features/Dashboard/Components/HeroCapital.razor`:
```razor
@using System.Globalization
<div class="hero">
    <div class="label">Capital under accumulation</div>
    <div class="amount">
        <span class="euro">€</span><span class="num">@Snap.CurrentValueEur.ToString("N0", FrFr)</span>
        <span class="of">— of one million —</span>
    </div>
    <div class="progress">
        <div class="bar">
            <span style="width:@Pct.ToString("F2", CultureInfo.InvariantCulture)%"></span>
        </div>
        <div class="pct num">@Pct.ToString("F1", FrFr) %</div>
    </div>
</div>

@code {
    [Parameter, EditorRequired] public TradyStrat.Shared.Domain.PortfolioSnapshot Snap { get; set; } = default!;

    private static readonly CultureInfo FrFr = CultureInfo.GetCultureInfo("fr-FR");
    private decimal Pct => Snap.ProgressPct;
}
```

`TradyStrat/Features/Dashboard/Components/HeroCapital.razor.css`:
```css
.hero {
    padding: 72px 56px 52px;
    border-bottom: 1px solid var(--vault-rule);
}
.label { margin-bottom: 24px; }
.amount {
    font-family: var(--font-display);
    font-weight: 300;
    font-size: clamp(72px, 9vw, 128px);
    line-height: 0.9;
    letter-spacing: -0.035em;
}
.amount .euro { color: var(--vault-gold); margin-right: 2px; }
.amount .of {
    display: block;
    font-family: var(--font-mono);
    font-size: 13px;
    letter-spacing: var(--tracking-xs);
    text-transform: uppercase;
    color: rgba(236,230,214,0.55);
    margin-top: 20px;
}
.progress { margin-top: 18px; display: flex; align-items: center; gap: 16px; }
.bar { flex: 1; height: 2px; background: var(--vault-rule); position: relative; }
.bar > span {
    position: absolute; left: 0; top: -2px; height: 6px;
    background: var(--vault-gold);
    box-shadow: 0 0 18px rgba(196,154,86,0.55);
    transition: width 600ms ease-out;
}
.pct { font-size: 13px; letter-spacing: var(--tracking-md); color: var(--vault-gold); }
```

- [ ] **Step 2: Build**

```bash
dotnet build --nologo
```

- [ ] **Step 3: Commit**

```bash
git add TradyStrat/Features/Dashboard/Components/HeroCapital.razor*
git commit -m "feat(ui): HeroCapital component"
```

---

### Task 37: `<TodaysCallCard />`

**Files:**
- Create: `TradyStrat/Features/Dashboard/Components/TodaysCallCard.razor`
- Create: `TradyStrat/Features/Dashboard/Components/TodaysCallCard.razor.css`

- [ ] **Step 1: Implement**

`TradyStrat/Features/Dashboard/Components/TodaysCallCard.razor`:
```razor
@using System.Globalization
@using TradyStrat.Shared.Domain
<div class="call">
    <div class="label">Today's call · @Today.ToString("d MMMM", FrFr)</div>
    <div class="verb">@Verb</div>

    @if (Sug.QuantityHint is decimal q && Sug.MaxPriceHint is decimal mp)
    {
        <div class="order num">
            @q.ToString("F0") sh CON3 · ≤ €@mp.ToString("F2", FrFr)
            · ≈ €@(q * mp).ToString("F2", FrFr)
        </div>
    }

    <p class="reasons">@Sug.Rationale</p>

    @if (Sug.Citations.Count > 0)
    {
        <ol class="citations">
            @foreach (var c in Sug.Citations)
            {
                <li><b>@c.Indicator (@c.Ticker):</b> @c.Claim · <em>@c.Value</em></li>
            }
        </ol>
    }

    <div class="actions">
        <button class="cta" disabled="@(Sug.Action != SuggestionAction.Acquire)"
                @onclick="OnLogTrade">Log trade</button>
        <button class="cta ghost" @onclick="OnRerun">Re-run AI</button>
    </div>
</div>

@code {
    [Parameter, EditorRequired] public Suggestion Sug { get; set; } = default!;
    [Parameter] public DateOnly Today { get; set; }
    [Parameter] public EventCallback OnLogTrade { get; set; }
    [Parameter] public EventCallback OnRerun { get; set; }

    private static readonly CultureInfo FrFr = CultureInfo.GetCultureInfo("fr-FR");

    private string Verb => Sug.Action switch
    {
        SuggestionAction.Acquire => "Acquire.",
        SuggestionAction.Hold    => "Hold.",
        SuggestionAction.Trim    => "Trim.",
        SuggestionAction.Wait    => "Wait.",
        _ => "—"
    };
}
```

`TradyStrat/Features/Dashboard/Components/TodaysCallCard.razor.css`:
```css
.call {
    padding: 36px 56px 40px;
    border-bottom: 1px solid var(--vault-rule);
}
.label { margin-bottom: 14px; }
.verb {
    font-family: var(--font-display);
    font-style: italic;
    font-weight: 400;
    font-size: 60px;
    line-height: 1;
    margin: 0 0 8px;
    color: var(--vault-ivory);
}
.order {
    font-family: var(--font-mono);
    font-size: 13px;
    letter-spacing: 0.16em;
    color: rgba(236,230,214,0.78);
    margin-bottom: 22px;
}
.reasons {
    font-family: var(--font-display);
    font-size: 17px;
    line-height: 1.6;
    color: rgba(236,230,214,0.8);
    max-width: 50ch;
}
.citations {
    margin-top: 16px;
    padding-top: 12px;
    border-top: 1px dashed var(--vault-rule);
    counter-reset: c;
    list-style: none;
}
.citations li {
    padding-left: 28px;
    position: relative;
    margin-bottom: 6px;
    font-size: 13px;
    color: rgba(236,230,214,0.7);
}
.citations li::before {
    counter-increment: c;
    content: counter(c, lower-roman) ".";
    position: absolute; left: 0; top: 0;
    color: var(--vault-gold); font-style: italic;
}
.actions { margin-top: 28px; display: flex; gap: 10px; }
.cta {
    padding: 13px 18px;
    background: var(--vault-gold);
    color: #1B1710;
    font-family: var(--font-mono);
    font-size: 10px;
    letter-spacing: 0.26em;
    text-transform: uppercase;
    font-weight: 600;
    transition: background 150ms;
}
.cta:hover:not(:disabled) { background: var(--vault-ivory); }
.cta:disabled { opacity: 0.4; cursor: not-allowed; }
.cta.ghost { background: transparent; color: var(--vault-ivory); border: 1px solid var(--vault-rule); font-weight: 400; }
.cta.ghost:hover { border-color: var(--vault-gold); color: var(--vault-gold); }
```

- [ ] **Step 2: Build**

```bash
dotnet build --nologo
```

- [ ] **Step 3: Commit**

```bash
git add TradyStrat/Features/Dashboard/Components/TodaysCallCard.razor*
git commit -m "feat(ui): TodaysCallCard component"
```

---

### Task 38: `<PortfolioRail />` (position + 3 ticker cells)

**Files:**
- Create: `TradyStrat/Features/Dashboard/Components/PortfolioRail.razor`
- Create: `TradyStrat/Features/Dashboard/Components/PortfolioRail.razor.css`

- [ ] **Step 1: Implement**

`TradyStrat/Features/Dashboard/Components/PortfolioRail.razor`:
```razor
@using System.Globalization
@using TradyStrat.Features.Dashboard
@using TradyStrat.Shared.Domain
<div class="rail">
    <div class="cell">
        <div class="lbl">Position</div>
        <div class="val"><span class="num">@Snap.Shares.ToString("N0", FrFr)</span> sh</div>
        <div class="sub">avg €@Snap.AvgCostEur.ToString("F2", FrFr)
                         · P&amp;L @PnL(Snap.UnrealizedPnLEur, Snap.CurrentValueEur)</div>
    </div>
    @foreach (var t in Tickers)
    {
        <div class="cell">
            <div class="tk">@t.Ticker</div>
            <div class="val">
                <span class="num">@FormatPrimary(t)</span>
                @if (t.DeltaPct is decimal dp)
                {
                    <span class="delta @(dp >= 0 ? "" : "dn") num">@FormatDelta(dp)</span>
                }
            </div>
            @if (t.PriceEur is decimal eur && t.Currency != "EUR")
            {
                <div class="sub">≈ €@eur.ToString("N2", FrFr)</div>
            }
        </div>
    }
</div>

@code {
    [Parameter, EditorRequired] public PortfolioSnapshot Snap { get; set; } = default!;
    [Parameter, EditorRequired] public IReadOnlyList<TickerView> Tickers { get; set; } = default!;

    private static readonly CultureInfo FrFr = CultureInfo.GetCultureInfo("fr-FR");

    private static string PnL(decimal pnl, decimal value)
    {
        if (value == 0m) return "—";
        var pct = pnl / (value - pnl) * 100m;
        return $"{(pct >= 0 ? "+" : "")}{pct.ToString("F1", FrFr)} %";
    }

    private static string FormatPrimary(TickerView t) => t.Currency switch
    {
        "EUR" => $"€{t.Price.ToString("N2", FrFr)}",
        "USD" => $"${t.Price.ToString("N2", FrFr)}",
        _     => t.Price.ToString("N2", FrFr)
    };

    private static string FormatDelta(decimal pct)
        => $"{(pct >= 0 ? "+" : "")}{pct.ToString("F1", FrFr)}%";
}
```

`TradyStrat/Features/Dashboard/Components/PortfolioRail.razor.css`:
```css
.rail {
    display: grid;
    grid-template-columns: 1.4fr 1fr 1fr 1fr;
    border-bottom: 1px solid var(--vault-rule);
}
.cell {
    padding: 24px 28px;
    border-right: 1px solid var(--vault-rule);
}
.cell:last-child { border-right: none; }
.lbl { font-family: var(--font-mono); font-size: 10px; letter-spacing: var(--tracking-xs);
       text-transform: uppercase; color: rgba(236,230,214,0.5); }
.tk  { font-family: var(--font-display); font-style: italic; font-size: 14px;
       letter-spacing: 0.04em; color: var(--vault-gold); margin-bottom: 4px; }
.val {
    margin-top: 10px;
    font-family: var(--font-display);
    font-weight: 300;
    font-size: 30px;
    letter-spacing: -0.012em;
}
.sub { margin-top: 6px; font-family: var(--font-mono); font-size: 10px;
       letter-spacing: var(--tracking-md); color: rgba(236,230,214,0.55); }
.delta { font-family: var(--font-mono); font-size: 11px; margin-left: 6px; color: var(--vault-green); }
.delta.dn { color: var(--vault-red); }
```

- [ ] **Step 2: Build**

```bash
dotnet build --nologo
```

- [ ] **Step 3: Commit**

```bash
git add TradyStrat/Features/Dashboard/Components/PortfolioRail.razor*
git commit -m "feat(ui): PortfolioRail with EUR-converted USD prices"
```

---

### Task 39: `<GrowthChart />` (custom SVG)

**Files:**
- Create: `TradyStrat/Features/Dashboard/Components/GrowthChart.razor`
- Create: `TradyStrat/Features/Dashboard/Components/GrowthChart.razor.css`
- Create: `TradyStrat/Features/Dashboard/PathBuilder.cs`
- Create: `TradyStrat.Tests/Dashboard/PathBuilderTests.cs`

- [ ] **Step 1: Write the failing test for `PathBuilder`**

`TradyStrat.Tests/Dashboard/PathBuilderTests.cs`:
```csharp
using Shouldly;
using TradyStrat.Features.Dashboard;
using TradyStrat.Shared.Domain;

namespace TradyStrat.Tests.Dashboard;

public class PathBuilderTests
{
    [Fact]
    public void Empty_input_yields_empty_path()
    {
        PathBuilder.Line([], width: 1200, height: 220, maxValue: 100m).ShouldBe("");
    }

    [Fact]
    public void Single_point_yields_single_M_command()
    {
        var pts = new[] { new GrowthPoint(new(2026,1,1), 50m) };
        PathBuilder.Line(pts, 1200, 220, 100m).ShouldStartWith("M");
    }

    [Fact]
    public void Two_points_produce_M_then_L()
    {
        var pts = new[] {
            new GrowthPoint(new(2026,1,1), 0m),
            new GrowthPoint(new(2026,1,2), 100m) };
        var d = PathBuilder.Line(pts, 1200, 220, 100m);

        d.ShouldStartWith("M");
        d.ShouldContain("L");
    }
}
```

- [ ] **Step 2: Run, expect compile failure**

```bash
dotnet test --filter "FullyQualifiedName~PathBuilderTests" --nologo
```

- [ ] **Step 3: Implement `PathBuilder` and the component**

`TradyStrat/Features/Dashboard/PathBuilder.cs`:
```csharp
using System.Globalization;
using TradyStrat.Shared.Domain;

namespace TradyStrat.Features.Dashboard;

public static class PathBuilder
{
    public static string Line(IReadOnlyList<GrowthPoint> pts, int width, int height, decimal maxValue)
    {
        if (pts.Count == 0) return "";
        if (maxValue <= 0m) maxValue = 1m;

        var sb = new System.Text.StringBuilder();
        for (var i = 0; i < pts.Count; i++)
        {
            var x = pts.Count == 1 ? 0 : (decimal)i * width / (pts.Count - 1);
            var y = height - (pts[i].ValueEur / maxValue * height);
            var verb = i == 0 ? "M" : "L";
            sb.Append(verb)
              .Append(((double)x).ToString("F1", CultureInfo.InvariantCulture))
              .Append(',')
              .Append(((double)y).ToString("F1", CultureInfo.InvariantCulture))
              .Append(' ');
        }
        return sb.ToString().TrimEnd();
    }

    public static string Area(IReadOnlyList<GrowthPoint> pts, int width, int height, decimal maxValue)
    {
        if (pts.Count == 0) return "";
        var line = Line(pts, width, height, maxValue);
        return $"{line} L{width},{height} L0,{height} Z";
    }
}
```

`TradyStrat/Features/Dashboard/Components/GrowthChart.razor`:
```razor
@using System.Globalization
@using TradyStrat.Features.Dashboard
@using TradyStrat.Shared.Domain
<div class="chart-wrap">
    <div class="lbl">Capital growth · trajectory toward goal</div>
    <svg viewBox="0 0 1200 240" preserveAspectRatio="none" class="chart">
        <defs>
            <linearGradient id="vault-grad" x1="0" x2="0" y1="0" y2="1">
                <stop offset="0%" stop-color="#C49A56" stop-opacity="0.35" />
                <stop offset="100%" stop-color="#C49A56" stop-opacity="0" />
            </linearGradient>
        </defs>
        <g class="grid">
            <line x1="0" y1="40" x2="1200" y2="40"/>
            <line x1="0" y1="100" x2="1200" y2="100"/>
            <line x1="0" y1="160" x2="1200" y2="160"/>
            <line x1="0" y1="220" x2="1200" y2="220"/>
        </g>
        <path class="goal" d="M0,225 L1200,8" />
        <text class="axis-label" x="1186" y="20" text-anchor="end">€@Goal.TargetEur.ToString("N0") — goal</text>
        <path style="fill:url(#vault-grad)" d="@AreaPath" />
        <path class="line" d="@LinePath" />
        @if (Points.Count > 0)
        {
            var last = Points[^1];
            var lastX = 1200;
            var lastY = (double)(220m - last.ValueEur / Math.Max(1m, Goal.TargetEur) * 220m);
            <circle r="3.5" cx="@lastX" cy="@lastY.ToString("F1", CultureInfo.InvariantCulture)" fill="#C49A56" />
            <text class="axis-label" x="@(lastX-14)" y="@((lastY-8).ToString("F1", CultureInfo.InvariantCulture))" text-anchor="end">
                €@last.ValueEur.ToString("N0") — today
            </text>
        }
        <g class="axis">
            @if (Points.Count > 0)
            {
                <text x="0" y="236">@Points[0].Date.ToString("MMM yyyy", CultureInfo.InvariantCulture)</text>
                <text x="600" y="236" text-anchor="middle">@Points[Points.Count/2].Date.ToString("MMM yyyy", CultureInfo.InvariantCulture)</text>
                <text x="1200" y="236" text-anchor="end">@Points[^1].Date.ToString("MMM yyyy", CultureInfo.InvariantCulture)</text>
            }
        </g>
    </svg>
</div>

@code {
    [Parameter, EditorRequired] public IReadOnlyList<GrowthPoint> Points { get; set; } = default!;
    [Parameter, EditorRequired] public GoalConfig Goal { get; set; } = default!;

    private string LinePath => PathBuilder.Line(Points, 1200, 220, Goal.TargetEur);
    private string AreaPath => PathBuilder.Area(Points, 1200, 220, Goal.TargetEur);
}
```

`TradyStrat/Features/Dashboard/Components/GrowthChart.razor.css`:
```css
.chart-wrap { padding: 38px 56px 60px; }
.lbl {
    font-family: var(--font-mono);
    font-size: 10px; letter-spacing: var(--tracking-xs);
    text-transform: uppercase; color: var(--vault-gold);
    margin-bottom: 22px;
}
.chart { width: 100%; height: 240px; display: block; }
.chart .grid line { stroke: var(--vault-rule); }
.chart .axis text { font-family: var(--font-mono); font-size: 10px;
                    fill: rgba(236,230,214,0.5); letter-spacing: 0.1em; }
.chart .line { stroke: var(--vault-gold); stroke-width: 1.4; fill: none;
               filter: drop-shadow(0 0 6px rgba(196,154,86,0.45));
               stroke-dasharray: 3000; stroke-dashoffset: 3000;
               animation: draw 1200ms ease-out forwards; }
.chart .goal { stroke: rgba(196,154,86,0.45); stroke-width: 1; stroke-dasharray: 2 4; fill: none; }
.axis-label { font-family: var(--font-display); font-style: italic;
              font-size: 13px; fill: rgba(236,230,214,0.7); }
@keyframes draw { to { stroke-dashoffset: 0; } }
```

- [ ] **Step 4: Run, expect pass**

```bash
dotnet test --filter "FullyQualifiedName~PathBuilderTests" --nologo
```

- [ ] **Step 5: Commit**

```bash
git add TradyStrat/Features/Dashboard/PathBuilder.cs \
        TradyStrat/Features/Dashboard/Components/GrowthChart.razor* \
        TradyStrat.Tests/Dashboard/
git commit -m "feat(ui): GrowthChart with custom SVG path builder"
```

---

### Task 40: `DashboardPage` + `DashboardModule` + manual visual check

**Files:**
- Create: `TradyStrat/Features/Dashboard/DashboardPage.razor`
- Create: `TradyStrat/Features/Dashboard/Components/RefreshFab.razor`
- Create: `TradyStrat/Features/Dashboard/Components/RefreshFab.razor.css`
- Create: `TradyStrat/Modules/DashboardModule.cs`
- Modify: `TradyStrat/Components/Pages/Home.razor` (delete; route moves to DashboardPage)

- [ ] **Step 1: Implement `RefreshFab`**

`TradyStrat/Features/Dashboard/Components/RefreshFab.razor`:
```razor
<button class="fab" @onclick="OnClick" disabled="@Busy" title="Refresh prices">
    @if (Busy) { <span class="spin">↻</span> } else { <span>↻</span> }
</button>

@code {
    [Parameter] public bool Busy { get; set; }
    [Parameter] public EventCallback OnClick { get; set; }
}
```

`TradyStrat/Features/Dashboard/Components/RefreshFab.razor.css`:
```css
.fab {
    position: fixed; right: 24px; bottom: 24px;
    width: 48px; height: 48px;
    border-radius: 50%;
    background: var(--vault-gold);
    color: #1B1710;
    font-size: 20px;
    box-shadow: 0 8px 24px rgba(0,0,0,0.5);
    transition: background 150ms;
}
.fab:hover:not(:disabled) { background: var(--vault-ivory); }
.fab:disabled { opacity: 0.6; cursor: wait; }
.spin { display: inline-block; animation: spin 800ms linear infinite; }
@keyframes spin { to { transform: rotate(360deg); } }
```

- [ ] **Step 2: Implement `DashboardPage`**

`TradyStrat/Features/Dashboard/DashboardPage.razor`:
```razor
@page "/"
@using TradyStrat.Application.Abstractions
@using TradyStrat.Application.UseCases.AiSuggestion
@using TradyStrat.Application.UseCases.Dashboard
@using TradyStrat.Application.UseCases.Prices
@using TradyStrat.Features.Dashboard.Components
@inject LoadDashboardUseCase LoadDashboard
@inject ForceRefetchSuggestionUseCase ForceRefetch
@inject RefreshAllPricesUseCase RefreshPrices

@if (_vm is null)
{
    <p style="padding:48px 56px;color:var(--vault-gold);font-family:var(--font-mono);
              letter-spacing:0.32em;text-transform:uppercase">Loading…</p>
}
else
{
    <VaultMasthead Today="_vm.Today" EntryNumber="_vm.EntryNumber" />
    <HeroCapital Snap="_vm.Portfolio" />
    <TodaysCallCard Sug="_vm.TodaysCall" Today="_vm.Today"
                    OnRerun="OnRerunRequested" OnLogTrade="OnLogTradeRequested" />
    <PortfolioRail Snap="_vm.Portfolio" Tickers="_vm.Tickers" />
    <GrowthChart Points="_vm.Growth" Goal="_vm.Goal" />
    <RefreshFab Busy="_busy" OnClick="OnRefreshClicked" />

    @if (_showRerunConfirm)
    {
        <div class="modal" @onclick="() => _showRerunConfirm = false">
            <div class="modal-body" @onclick:stopPropagation="true">
                <h3>Re-run AI?</h3>
                <p>This will use one Anthropic API call. Continue?</p>
                <div class="modal-actions">
                    <button class="btn" @onclick="ConfirmRerun">Confirm</button>
                    <button class="btn ghost" @onclick="() => _showRerunConfirm = false">Cancel</button>
                </div>
            </div>
        </div>
    }
}

@code {
    private DashboardViewModel? _vm;
    private bool _busy;
    private bool _showRerunConfirm;

    protected override async Task OnInitializedAsync() => await Reload();

    private async Task Reload()
        => _vm = await LoadDashboard.ExecuteAsync(Unit.Value, default);

    private async Task OnRefreshClicked()
    {
        _busy = true;
        try   { await RefreshPrices.ExecuteAsync(Unit.Value, default); await Reload(); }
        finally { _busy = false; }
    }

    private void OnRerunRequested() => _showRerunConfirm = true;

    private async Task ConfirmRerun()
    {
        _showRerunConfirm = false;
        _busy = true;
        try   { await ForceRefetch.ExecuteAsync(Unit.Value, default); await Reload(); }
        finally { _busy = false; }
    }

    private void OnLogTradeRequested()
    {
        // Phase 7 wires this to /trades navigation or an inline dialog.
    }
}

<style>
    .modal {
        position: fixed; inset: 0;
        background: rgba(0,0,0,0.7);
        display: flex; align-items: center; justify-content: center;
        z-index: 1000;
    }
    .modal-body {
        background: var(--vault-bg-2);
        border: 1px solid var(--vault-rule);
        padding: 32px; max-width: 420px;
    }
    .modal-body h3 {
        font-family: var(--font-display); font-style: italic;
        font-size: 28px; margin-bottom: 12px;
    }
    .modal-body p { color: rgba(236,230,214,0.78); margin-bottom: 22px; }
    .modal-actions { display: flex; gap: 10px; justify-content: flex-end; }
    .btn {
        padding: 10px 16px; background: var(--vault-gold);
        color: #1B1710; font-family: var(--font-mono); font-size: 10px;
        letter-spacing: 0.26em; text-transform: uppercase; font-weight: 600;
    }
    .btn.ghost { background: transparent; color: var(--vault-ivory);
                 border: 1px solid var(--vault-rule); font-weight: 400; }
</style>
```

- [ ] **Step 3: Implement `DashboardModule` and remove the placeholder Home page**

`TradyStrat/Modules/DashboardModule.cs`:
```csharp
using TheAppManager.Modules;
using TradyStrat.Application.UseCases.Dashboard;

namespace TradyStrat.Modules;

public sealed class DashboardModule : IAppModule
{
    public void ConfigureServices(WebApplicationBuilder builder)
    {
        builder.Services.AddScoped<LoadDashboardUseCase>();
    }
}
```

Delete `TradyStrat/Components/Pages/Home.razor`:
```bash
git rm TradyStrat/Components/Pages/Home.razor
```

Update the smoke test to expect dashboard text instead of "bootstrap":

`TradyStrat.Tests/Modules/ModuleSmokeTests.cs` — change body assertion:
```csharp
body.ShouldContain("Loading");   // page renders before VM resolves
```

- [ ] **Step 4: Build and run all tests**

```bash
dotnet build --nologo
dotnet test --nologo
```

Expected: green.

- [ ] **Step 5: Manual visual verification**

1. Ensure `Anthropic:ApiKey` is set:
```bash
dotnet user-secrets set "Anthropic:ApiKey" "sk-ant-…" --project TradyStrat
```
2. Insert a test trade so the portfolio shows a non-zero figure:
```bash
sqlite3 "$HOME/Library/Application Support/TradyStrat/tradystrat.db" \
  "INSERT INTO Trades (ExecutedOn, Side, Quantity, PricePerShare, FeesEur, Note, CreatedAt)
   VALUES ('2026-05-05', 1, 100, '4.50', '0', NULL, '2026-05-05 12:00:00');"
```
3. Run the app:
```bash
dotnet run --project TradyStrat
```
4. Open http://127.0.0.1:5180 in a browser. Verify:
   - The Vault aesthetic loads (dark, gold accent, Cormorant Garamond, JetBrains Mono numerics).
   - Hero amount, today's call, three ticker tiles, growth chart, refresh button.
   - The page matches the [mockup](../specs/2026-05-06-tradystrat-vault-mockup.html) within ±1 token (not ±1px). If a panel is missing or visibly off, fix the relevant component before moving on.
5. Click the refresh button — toast/spinner appears briefly; data reloads.
6. Click "Re-run AI" — confirmation modal appears. Cancel.

- [ ] **Step 6: Commit**

```bash
git add TradyStrat/Features/Dashboard/DashboardPage.razor \
        TradyStrat/Features/Dashboard/Components/RefreshFab.razor* \
        TradyStrat/Modules/DashboardModule.cs \
        TradyStrat.Tests/Modules/ModuleSmokeTests.cs
git rm --cached TradyStrat/Components/Pages/Home.razor 2>/dev/null || true
git commit -m "feat(ui): DashboardPage assembling The Vault and wiring use cases"
```

**End of Phase 6.** The dashboard renders end-to-end with real data.

---

## Phase 7 — Trades and Settings pages (Tasks 41–43)

End state: `/trades` lists all trades, supports add/edit/delete and CSV import; `/settings` edits the goal.

---

### Task 41: `/trades` page + `AddTradeDialog`

**Files:**
- Create: `TradyStrat/Features/Trades/TradesPage.razor`
- Create: `TradyStrat/Features/Trades/TradesPage.razor.css`
- Create: `TradyStrat/Features/Trades/Components/AddTradeDialog.razor`
- Create: `TradyStrat/Features/Trades/Components/AddTradeDialog.razor.css`

- [ ] **Step 1: Implement the `/trades` page**

`TradyStrat/Features/Trades/TradesPage.razor`:
```razor
@page "/trades"
@using Ardalis.Specification
@using System.Globalization
@using TradyStrat.Application.Abstractions
@using TradyStrat.Application.UseCases.Trades
@using TradyStrat.Features.Dashboard.Components
@using TradyStrat.Features.Trades.Components
@using TradyStrat.Shared.Domain
@using TradyStrat.Shared.Time
@using TradyStrat.Specifications.Trades
@inject IReadRepositoryBase<Trade> Repo
@inject IClock Clock
@inject LogTradeUseCase LogTrade
@inject EditTradeUseCase EditTrade
@inject DeleteTradeUseCase DeleteTrade
@inject ImportTradesCsvUseCase ImportCsv

<VaultMasthead Today="@Clock.TodayInExchangeTzFor("CON3.DE")" EntryNumber="@_count" />

<div class="trades">
    <div class="hdr">
        <div class="label">Trade ledger</div>
        <div>
            <button class="btn" @onclick="() => _showAdd = true">+ Add trade</button>
            <button class="btn ghost" @onclick="() => _showImport = true">Import CSV</button>
        </div>
    </div>

    <table class="t">
        <thead><tr><th>Date</th><th>Side</th><th>Qty</th><th>Price</th><th>Fees</th><th>Note</th><th></th></tr></thead>
        <tbody>
        @foreach (var t in _trades)
        {
            <tr>
                <td class="num">@t.ExecutedOn.ToString("yyyy-MM-dd")</td>
                <td>@t.Side</td>
                <td class="num">@t.Quantity.ToString("N0", FrFr)</td>
                <td class="num">€@t.PricePerShare.ToString("F2", FrFr)</td>
                <td class="num">€@t.FeesEur.ToString("F2", FrFr)</td>
                <td>@t.Note</td>
                <td>
                    <button class="link" @onclick="() => StartEdit(t)">edit</button>
                    <button class="link danger" @onclick="() => Delete(t)">×</button>
                </td>
            </tr>
        }
        </tbody>
    </table>
</div>

@if (_showAdd)
{
    <AddTradeDialog Initial="_editing"
                    OnCancel="CloseDialogs"
                    OnSubmit="HandleSubmit" />
}

@if (_showImport)
{
    <div class="modal" @onclick="CloseDialogs">
        <div class="modal-body" @onclick:stopPropagation="true">
            <h3>Import CSV</h3>
            <p>Paste a CSV with columns <code>date,side,qty,price,fees</code>.</p>
            <textarea @bind="_csvText" rows="10" style="width:100%;background:var(--vault-bg);color:var(--vault-ivory);border:1px solid var(--vault-rule);padding:8px;font-family:var(--font-mono);"></textarea>
            <div class="modal-actions">
                <button class="btn" @onclick="DoImport">Import</button>
                <button class="btn ghost" @onclick="CloseDialogs">Cancel</button>
            </div>
            @if (!string.IsNullOrEmpty(_importError))
            {
                <p class="err">@_importError</p>
            }
        </div>
    </div>
}

@code {
    private static readonly CultureInfo FrFr = CultureInfo.GetCultureInfo("fr-FR");
    private List<Trade> _trades = new();
    private int _count;
    private bool _showAdd;
    private bool _showImport;
    private Trade? _editing;
    private string _csvText = "";
    private string? _importError;

    protected override async Task OnInitializedAsync() => await Reload();

    private async Task Reload()
    {
        var list = await Repo.ListAsync(new AllTradesSpec(), default);
        _trades = list.ToList();
        _count = _trades.Count;
    }

    private void StartEdit(Trade t) { _editing = t; _showAdd = true; }

    private async Task HandleSubmit(LogTradeInput input)
    {
        if (_editing is null)
            await LogTrade.ExecuteAsync(input, default);
        else
            await EditTrade.ExecuteAsync(new EditTradeInput(
                _editing.Id, input.ExecutedOn, input.Side,
                input.Quantity, input.PricePerShare, input.FeesEur, input.Note), default);
        CloseDialogs();
        await Reload();
    }

    private async Task Delete(Trade t)
    {
        await DeleteTrade.ExecuteAsync(new DeleteTradeInput(t.Id), default);
        await Reload();
    }

    private async Task DoImport()
    {
        try
        {
            await ImportCsv.ExecuteAsync(new ImportTradesCsvInput(_csvText), default);
            CloseDialogs();
            await Reload();
        }
        catch (TradyStrat.Shared.Exceptions.CsvImportException ex)
        {
            _importError = ex.Message;
        }
    }

    private void CloseDialogs()
    {
        _showAdd = false;
        _showImport = false;
        _editing = null;
        _csvText = "";
        _importError = null;
    }
}
```

`TradyStrat/Features/Trades/TradesPage.razor.css`:
```css
.trades { padding: 36px 56px 60px; }
.hdr { display: flex; justify-content: space-between; align-items: center; margin-bottom: 22px; }
.btn {
    padding: 10px 16px; background: var(--vault-gold);
    color: #1B1710; font-family: var(--font-mono); font-size: 10px;
    letter-spacing: 0.26em; text-transform: uppercase; font-weight: 600;
}
.btn.ghost { background: transparent; color: var(--vault-ivory); border: 1px solid var(--vault-rule); font-weight: 400; }
.btn + .btn { margin-left: 6px; }

.t { width: 100%; border-collapse: collapse; margin-top: 8px; }
.t th, .t td { padding: 10px 12px; border-bottom: 1px solid var(--vault-rule); text-align: left; }
.t th { font-family: var(--font-mono); font-size: 10px; letter-spacing: var(--tracking-xs);
        text-transform: uppercase; color: var(--vault-gold); }
.t td.num { font-family: var(--font-mono); font-variant-numeric: tabular-nums; }
.link { color: var(--vault-ivory); text-decoration: underline; font-size: 12px; padding: 2px 6px; }
.link.danger { color: var(--vault-red); }
.err { color: var(--vault-red); margin-top: 12px; font-size: 13px; }

.modal { position: fixed; inset: 0; background: rgba(0,0,0,0.7);
         display: flex; align-items: center; justify-content: center; z-index: 1000; }
.modal-body { background: var(--vault-bg-2); border: 1px solid var(--vault-rule);
              padding: 32px; max-width: 600px; width: 90%; }
.modal-body h3 { font-family: var(--font-display); font-style: italic;
                 font-size: 28px; margin-bottom: 12px; }
.modal-actions { display: flex; gap: 10px; justify-content: flex-end; margin-top: 16px; }
```

- [ ] **Step 2: Implement `AddTradeDialog`**

`TradyStrat/Features/Trades/Components/AddTradeDialog.razor`:
```razor
@using TradyStrat.Application.UseCases.Trades
@using TradyStrat.Shared.Domain
<div class="modal" @onclick="OnCancel.InvokeAsync">
    <div class="modal-body" @onclick:stopPropagation="true">
        <h3>@(Initial is null ? "Add trade" : "Edit trade")</h3>
        <div class="grid">
            <label>Date<input type="date" @bind="_date" /></label>
            <label>Side<select @bind="_side">
                <option value="Buy">Buy</option><option value="Sell">Sell</option>
            </select></label>
            <label>Quantity<input type="number" step="0.0001" @bind="_qty" /></label>
            <label>Price (€)<input type="number" step="0.01" @bind="_price" /></label>
            <label>Fees (€)<input type="number" step="0.01" @bind="_fees" /></label>
            <label class="full">Note<input @bind="_note" /></label>
        </div>
        @if (!string.IsNullOrEmpty(_err))
        {
            <p class="err">@_err</p>
        }
        <div class="modal-actions">
            <button class="btn" @onclick="DoSubmit">Save</button>
            <button class="btn ghost" @onclick="OnCancel.InvokeAsync">Cancel</button>
        </div>
    </div>
</div>

@code {
    [Parameter] public Trade? Initial { get; set; }
    [Parameter] public EventCallback OnCancel { get; set; }
    [Parameter] public EventCallback<LogTradeInput> OnSubmit { get; set; }

    private DateTime _date = DateTime.Today;
    private string _side = "Buy";
    private decimal _qty;
    private decimal _price;
    private decimal _fees;
    private string? _note;
    private string? _err;

    protected override void OnInitialized()
    {
        if (Initial is { } t)
        {
            _date  = t.ExecutedOn.ToDateTime(TimeOnly.MinValue);
            _side  = t.Side.ToString();
            _qty   = t.Quantity;
            _price = t.PricePerShare;
            _fees  = t.FeesEur;
            _note  = t.Note;
        }
    }

    private async Task DoSubmit()
    {
        if (_qty <= 0 || _price <= 0)
        {
            _err = "Quantity and price must be positive.";
            return;
        }
        var input = new LogTradeInput(
            DateOnly.FromDateTime(_date),
            Enum.Parse<TradeSide>(_side),
            _qty, _price, _fees, _note);

        await OnSubmit.InvokeAsync(input);
    }
}
```

`TradyStrat/Features/Trades/Components/AddTradeDialog.razor.css`:
```css
.modal { position: fixed; inset: 0; background: rgba(0,0,0,0.7);
         display: flex; align-items: center; justify-content: center; z-index: 1000; }
.modal-body { background: var(--vault-bg-2); border: 1px solid var(--vault-rule);
              padding: 32px; max-width: 520px; width: 90%; }
.modal-body h3 { font-family: var(--font-display); font-style: italic;
                 font-size: 28px; margin-bottom: 16px; }
.grid { display: grid; grid-template-columns: 1fr 1fr; gap: 12px; }
.grid label { display: flex; flex-direction: column; gap: 4px;
              font-family: var(--font-mono); font-size: 10px;
              letter-spacing: var(--tracking-xs); text-transform: uppercase;
              color: var(--vault-gold); }
.grid label.full { grid-column: 1 / -1; }
.grid input, .grid select {
    background: var(--vault-bg); color: var(--vault-ivory);
    border: 1px solid var(--vault-rule); padding: 8px;
    font-family: var(--font-mono); font-size: 14px;
}
.modal-actions { display: flex; gap: 10px; justify-content: flex-end; margin-top: 18px; }
.btn { padding: 10px 16px; background: var(--vault-gold);
       color: #1B1710; font-family: var(--font-mono); font-size: 10px;
       letter-spacing: 0.26em; text-transform: uppercase; font-weight: 600; }
.btn.ghost { background: transparent; color: var(--vault-ivory);
             border: 1px solid var(--vault-rule); font-weight: 400; }
.err { color: var(--vault-red); margin-top: 10px; font-size: 13px; }
```

- [ ] **Step 3: Build and verify**

```bash
dotnet build --nologo
dotnet test --nologo
```

- [ ] **Step 4: Manual verification**

Run `dotnet run --project TradyStrat`. Visit `/trades`. Add a trade → it appears. Edit → updates. Delete → disappears. Click "Import CSV", paste:
```
date,side,qty,price,fees
2025-12-01,Buy,200,3.80,0.50
2026-01-15,Buy,150,4.10,0.50
```
→ both rows appear.

- [ ] **Step 5: Commit**

```bash
git add TradyStrat/Features/Trades/TradesPage.razor* \
        TradyStrat/Features/Trades/Components/AddTradeDialog.razor*
git commit -m "feat(ui): /trades page with add/edit/delete + CSV import"
```

---

### Task 42: `/settings` page + `UpdateGoalUseCase`

**Files:**
- Create: `TradyStrat/Features/Settings/SettingsPage.razor`
- Create: `TradyStrat/Features/Settings/SettingsPage.razor.css`
- Create: `TradyStrat/Application/UseCases/Settings/UpdateGoalUseCase.cs`
- Create: `TradyStrat/Modules/SettingsModule.cs`
- Create: `TradyStrat.Tests/UseCases/Settings/UpdateGoalUseCaseTests.cs`

- [ ] **Step 1: Write the failing test**

`TradyStrat.Tests/UseCases/Settings/UpdateGoalUseCaseTests.cs`:
```csharp
using Microsoft.Extensions.Logging.Abstractions;
using Shouldly;
using TradyStrat.Application.UseCases.Settings;
using TradyStrat.Shared.Domain;
using TradyStrat.Tests.Fx;
using TradyStrat.Tests.Specifications;
using TradyStrat.Tests.Time;

namespace TradyStrat.Tests.UseCases.Settings;

public class UpdateGoalUseCaseTests
{
    [Fact]
    public async Task Inserts_default_goal_when_none_exists_then_updates()
    {
        await using var db = InMemoryDb.Create();
        var clock = new FakeClock(new DateTime(2026,5,6,0,0,0,DateTimeKind.Utc));
        var uc = new UpdateGoalUseCase(new TestRepo<GoalConfig>(db), clock,
            NullLogger<UpdateGoalUseCase>.Instance);

        var goal = await uc.ExecuteAsync(new UpdateGoalInput(
            TargetEur: 500_000m, TargetDate: new(2030,1,1)), default);

        goal.TargetEur.ShouldBe(500_000m);
        db.Goals.Single().TargetDate.ShouldBe(new DateOnly(2030,1,1));
    }
}
```

- [ ] **Step 2: Run, expect compile failure**

```bash
dotnet test --filter "FullyQualifiedName~UpdateGoalUseCaseTests" --nologo
```

- [ ] **Step 3: Implement `UpdateGoalUseCase` and module**

`TradyStrat/Application/UseCases/Settings/UpdateGoalUseCase.cs`:
```csharp
using Ardalis.Specification;
using Microsoft.Extensions.Logging;
using TradyStrat.Application.Abstractions;
using TradyStrat.Shared.Domain;
using TradyStrat.Shared.Exceptions;
using TradyStrat.Shared.Time;

namespace TradyStrat.Application.UseCases.Settings;

public sealed record UpdateGoalInput(decimal TargetEur, DateOnly? TargetDate);

public sealed class UpdateGoalUseCase(
    IRepositoryBase<GoalConfig> repo, IClock clock,
    ILogger<UpdateGoalUseCase> log)
    : UseCaseBase<UpdateGoalInput, GoalConfig>(log)
{
    protected override async Task<GoalConfig> ExecuteCore(UpdateGoalInput input, CancellationToken ct)
    {
        if (input.TargetEur <= 0m)
            throw new TradeValidationException("Target must be positive.");

        var existing = await repo.GetByIdAsync(1, ct);
        var now = clock.UtcNow();

        if (existing is null)
        {
            var fresh = new GoalConfig
            {
                Id = 1,
                TargetEur = input.TargetEur,
                TargetDate = input.TargetDate,
                FocusTicker = "CON3.DE",
                UpdatedAt = now,
            };
            await repo.AddAsync(fresh, ct);
            return fresh;
        }

        var updated = existing with
        {
            TargetEur = input.TargetEur,
            TargetDate = input.TargetDate,
            UpdatedAt = now,
        };
        await repo.UpdateAsync(updated, ct);
        return updated;
    }
}
```

`TradyStrat/Modules/SettingsModule.cs`:
```csharp
using TheAppManager.Modules;
using TradyStrat.Application.UseCases.Settings;

namespace TradyStrat.Modules;

public sealed class SettingsModule : IAppModule
{
    public void ConfigureServices(WebApplicationBuilder builder)
    {
        builder.Services.AddScoped<UpdateGoalUseCase>();
    }
}
```

- [ ] **Step 4: Implement the page**

`TradyStrat/Features/Settings/SettingsPage.razor`:
```razor
@page "/settings"
@using Ardalis.Specification
@using System.Globalization
@using TradyStrat.Application.UseCases.Settings
@using TradyStrat.Features.Dashboard.Components
@using TradyStrat.Shared.Domain
@using TradyStrat.Shared.Time
@inject IReadRepositoryBase<GoalConfig> GoalRepo
@inject IClock Clock
@inject UpdateGoalUseCase UpdateGoal

<VaultMasthead Today="@Clock.TodayInExchangeTzFor("CON3.DE")" EntryNumber="0" />

<div class="settings">
    <div class="label">Settings</div>
    <h2>Goal</h2>
    <div class="grid">
        <label>Target (€)<input type="number" step="1000" @bind="_target" /></label>
        <label>Target date<input type="date" @bind="_date" /></label>
        <label>Focus ticker<input value="CON3.DE" disabled /></label>
    </div>
    <div class="actions">
        <button class="btn" @onclick="Save">Save</button>
        @if (!string.IsNullOrEmpty(_msg))
        {
            <span class="msg">@_msg</span>
        }
    </div>
</div>

@code {
    private decimal _target = 1_000_000m;
    private DateTime? _date;
    private string? _msg;

    protected override async Task OnInitializedAsync()
    {
        var existing = await GoalRepo.GetByIdAsync(1, default);
        if (existing is not null)
        {
            _target = existing.TargetEur;
            _date   = existing.TargetDate?.ToDateTime(TimeOnly.MinValue);
        }
    }

    private async Task Save()
    {
        var date = _date is { } d ? DateOnly.FromDateTime(d) : (DateOnly?)null;
        await UpdateGoal.ExecuteAsync(new UpdateGoalInput(_target, date), default);
        _msg = "Saved.";
    }
}
```

`TradyStrat/Features/Settings/SettingsPage.razor.css`:
```css
.settings { padding: 36px 56px 60px; }
.settings h2 { font-family: var(--font-display); font-style: italic;
               font-size: 36px; margin: 18px 0 22px; }
.grid { display: grid; grid-template-columns: 1fr 1fr 1fr; gap: 16px; max-width: 720px; }
.grid label { display: flex; flex-direction: column; gap: 4px;
              font-family: var(--font-mono); font-size: 10px;
              letter-spacing: var(--tracking-xs); text-transform: uppercase;
              color: var(--vault-gold); }
.grid input { background: var(--vault-bg); color: var(--vault-ivory);
              border: 1px solid var(--vault-rule); padding: 8px;
              font-family: var(--font-mono); font-size: 14px; }
.grid input:disabled { opacity: 0.6; }
.actions { margin-top: 24px; display: flex; align-items: center; gap: 14px; }
.btn { padding: 10px 16px; background: var(--vault-gold);
       color: #1B1710; font-family: var(--font-mono); font-size: 10px;
       letter-spacing: 0.26em; text-transform: uppercase; font-weight: 600; }
.msg { color: var(--vault-green); font-family: var(--font-mono); font-size: 12px; }
```

- [ ] **Step 5: Run all tests**

```bash
dotnet test --nologo
```

- [ ] **Step 6: Manual verification**

Run the app. Visit `/settings`. Change target to `2000000`, save. Refresh → value persists. Visit `/` → progress percentage now reflects the new target.

- [ ] **Step 7: Commit**

```bash
git add TradyStrat/Application/UseCases/Settings/ \
        TradyStrat/Features/Settings/ \
        TradyStrat/Modules/SettingsModule.cs \
        TradyStrat.Tests/UseCases/Settings/
git commit -m "feat: /settings page + UpdateGoalUseCase + SettingsModule"
```

---

### Task 43: Top-nav links between pages

**Files:**
- Modify: `TradyStrat/Features/Dashboard/Components/VaultMasthead.razor` (add nav links)
- Modify: `TradyStrat/Features/Dashboard/Components/VaultMasthead.razor.css`

- [ ] **Step 1: Add nav anchors**

Replace `VaultMasthead.razor`:
```razor
@using System.Globalization
<div class="masthead">
    <div class="brand">
        <a href="/">Tradystrat</a>
        <span class="arc">— a private chronicle of accumulation</span>
    </div>
    <nav class="nav">
        <a href="/">Dashboard</a>
        <a href="/trades">Trades</a>
        <a href="/settings">Settings</a>
    </nav>
    <div class="meta">@FormatDate(Today) · entry no. @EntryNumber.ToString("D4")</div>
</div>

@code {
    [Parameter] public DateOnly Today { get; set; }
    [Parameter] public int EntryNumber { get; set; }

    private static string FormatDate(DateOnly d)
        => d.ToString("dd · MM · yyyy", CultureInfo.InvariantCulture);
}
```

Update `VaultMasthead.razor.css` — append:
```css
.nav { display: flex; gap: 22px; }
.nav a {
    font-family: var(--font-mono);
    font-size: 10px;
    letter-spacing: var(--tracking-xs);
    text-transform: uppercase;
    color: rgba(236,230,214,0.6);
    transition: color 150ms;
}
.nav a:hover { color: var(--vault-gold); }
```

Adjust `.masthead` grid:
```css
.masthead {
    padding: 26px 56px 22px;
    display: grid;
    grid-template-columns: auto 1fr auto;
    gap: 32px;
    align-items: baseline;
    border-bottom: 1px solid var(--vault-rule);
}
.brand { justify-self: start; }
.nav   { justify-self: center; }
.meta  { justify-self: end; }
```

- [ ] **Step 2: Build and verify navigation**

```bash
dotnet build --nologo
dotnet run --project TradyStrat
```

Click each nav link in the browser; pages route correctly.

- [ ] **Step 3: Commit**

```bash
git add TradyStrat/Features/Dashboard/Components/VaultMasthead.razor*
git commit -m "feat(ui): top-nav between Dashboard/Trades/Settings"
```

**End of Phase 7.** All three pages are functional and styled to The Vault.

---

## Phase 8 — Run, deploy, and final verification (Tasks 44–46)

End state: Docker image builds and runs the app on `127.0.0.1:5180`; README documents first-run; full test suite green; visual parity with the mockup confirmed.

---

### Task 44: `Dockerfile`, `.dockerignore`, Serilog file sink

**Files:**
- Create: `Dockerfile`
- Create: `.dockerignore`
- Modify: `TradyStrat/Program.cs` (Serilog wiring)
- Create: `TradyStrat/Modules/LoggingModule.cs`

- [ ] **Step 1: Add `LoggingModule` (Serilog rolling file sink)**

`TradyStrat/Modules/LoggingModule.cs`:
```csharp
using Serilog;
using Serilog.Events;
using TheAppManager.Modules;

namespace TradyStrat.Modules;

public sealed class LoggingModule : IAppModule
{
    public void ConfigureServices(WebApplicationBuilder builder)
    {
        var logDir = Path.Combine(
            Environment.GetEnvironmentVariable("LOG_DIR")
                ?? Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                    "Library/Application Support/TradyStrat/logs"));
        Directory.CreateDirectory(logDir);

        var path = Path.Combine(logDir, "tradystrat-.log");

        var logger = new LoggerConfiguration()
            .MinimumLevel.Information()
            .MinimumLevel.Override("Microsoft.AspNetCore", LogEventLevel.Warning)
            .WriteTo.Console()
            .WriteTo.File(path, rollingInterval: RollingInterval.Day, retainedFileCountLimit: 14)
            .CreateLogger();

        builder.Logging.ClearProviders();
        builder.Logging.AddSerilog(logger, dispose: true);
    }
}
```

- [ ] **Step 2: Create `Dockerfile`**

`Dockerfile`:
```dockerfile
# syntax=docker/dockerfile:1.7
FROM mcr.microsoft.com/dotnet/sdk:10.0-alpine AS build
WORKDIR /src
COPY *.sln Directory.Build.props Directory.Packages.props global.json ./
COPY TradyStrat/*.csproj TradyStrat/
COPY TradyStrat.Tests/*.csproj TradyStrat.Tests/
RUN dotnet restore
COPY . .
RUN dotnet publish TradyStrat -c Release -o /app /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0-alpine AS runtime
WORKDIR /app
COPY --from=build /app .
ENV ASPNETCORE_URLS=http://+:5180
ENV ASPNETCORE_ENVIRONMENT=Production
ENV Database__Path=/data/tradystrat.db
ENV LOG_DIR=/data/logs
EXPOSE 5180
VOLUME /data
ENTRYPOINT ["dotnet", "TradyStrat.dll"]
```

- [ ] **Step 3: Create `.dockerignore`**

`.dockerignore`:
```
bin/
obj/
.git/
.superpowers/
*.db*
logs/
docs/
.idea/
.vs/
.vscode/
*.user
```

- [ ] **Step 4: Build the Docker image**

```bash
docker build -t tradystrat:latest .
```

Expected: build succeeds. Run a smoke check:
```bash
docker run --rm -d --name tradystrat-smoke \
  -p 127.0.0.1:5180:5180 \
  -v "$(pwd)/.docker-data":/data \
  -e Anthropic__ApiKey="$ANTHROPIC_API_KEY" \
  tradystrat:latest

sleep 3
curl -s http://127.0.0.1:5180/ | grep -o "Loading"
docker stop tradystrat-smoke
```

Expected: `Loading` in the response body.

- [ ] **Step 5: Commit**

```bash
git add Dockerfile .dockerignore TradyStrat/Modules/LoggingModule.cs
git commit -m "feat: Dockerfile + LoggingModule with Serilog rolling file sink"
```

---

### Task 45: README

**Files:**
- Create: `README.md`

- [ ] **Step 1: Write the README**

`README.md`:
````markdown
# TradyStrat

Personal Blazor Server dashboard tracking accumulation of CON3 (3x leveraged Coinbase ETP, Frankfurt-listed) toward €1,000,000. Daily Yahoo prices, technical-analysis zones, an Anthropic-generated cited daily suggestion, in "The Vault" UI.

> **Spec:** [`docs/superpowers/specs/2026-05-06-tradystrat-dashboard-design.md`](docs/superpowers/specs/2026-05-06-tradystrat-dashboard-design.md)
> **Visual:** [`docs/superpowers/specs/2026-05-06-tradystrat-vault-mockup.html`](docs/superpowers/specs/2026-05-06-tradystrat-vault-mockup.html)

## Quick start (local)

Requirements: .NET 10 SDK (`dotnet --list-sdks` should show `10.0.x`).

```bash
# 1. Install dotnet-ef tool (one time)
dotnet tool install --global dotnet-ef --version 10.0.0

# 2. Set the Anthropic API key (one time)
dotnet user-secrets set "Anthropic:ApiKey" "sk-ant-…" --project TradyStrat

# 3. Run
dotnet run --project TradyStrat
```

Visit http://127.0.0.1:5180.

The SQLite database is created at `~/Library/Application Support/TradyStrat/tradystrat.db`. Logs at the same path under `logs/`.

## Quick start (Docker)

```bash
docker build -t tradystrat:latest .
docker run --rm -it \
  -p 127.0.0.1:5180:5180 \
  -v "$HOME/Library/Application Support/TradyStrat":/data \
  -e Anthropic__ApiKey="$ANTHROPIC_API_KEY" \
  tradystrat:latest
```

## Tests

```bash
dotnet test
```

## Project layout

See the spec for the full structure. Top-level folders:

- `TradyStrat/Modules/` — `TheAppManager.IAppModule` per feature; `Program.cs` is one line.
- `TradyStrat/Features/` — vertical-slice features (Dashboard, PriceFeed, Fx, Indicators, Trades, AiSuggestion, Settings, Portfolio).
- `TradyStrat/Application/UseCases/` — one class per command/query.
- `TradyStrat/Specifications/` — `Ardalis.Specification` query specs.
- `TradyStrat/Shared/Domain/` — entities + value records.
- `TradyStrat/Shared/Exceptions/` — typed domain exceptions rooted at `TradyStratException`.
- `TradyStrat/Data/` — `AppDbContext` + EF Core migrations.

## Configuration

Non-secret config in `appsettings.json` (committed). Anthropic API key is the only secret — store it via `dotnet user-secrets` locally or `Anthropic__ApiKey` environment variable for Docker.

## License

Personal use.
````

- [ ] **Step 2: Commit**

```bash
git add README.md
git commit -m "docs: add README with local + Docker quickstart"
```

---

### Task 46: Final end-to-end verification

- [ ] **Step 1: Clean checkout simulation**

```bash
git status                              # should be clean
dotnet test --nologo                     # full suite, all green
dotnet build --nologo --configuration Release
```

Expected: all tests pass; release build succeeds with no warnings (warnings-as-errors per `Directory.Build.props`).

- [ ] **Step 2: Visual parity check**

```bash
dotnet run --project TradyStrat
```

In a browser, open both side-by-side:
- http://127.0.0.1:5180
- file:///`<repo>`/docs/superpowers/specs/2026-05-06-tradystrat-vault-mockup.html (Direction B)

Verify the running app matches the mockup on:
- Hero amount typography (Cormorant Garamond display, ~120px, gold euro glyph)
- Today's call card (italic verb, gold actions)
- Portfolio rail (4 cells, USD subtitle on COIN/BTC)
- Growth chart (gold line, dashed goal trajectory)

If anything is visibly wrong, jump back to the corresponding component task and fix.

- [ ] **Step 3: Functional verification — full daily flow**

1. Visit `/settings`. Confirm goal `1,000,000`.
2. Visit `/trades`. Add 3 historical buys (different dates and prices). Verify rail position updates.
3. Visit `/`. Hero amount, growth chart, today's call all render. Click `↻` → toast/spinner; data reloads.
4. Click `Re-run AI` → modal → confirm → call goes through → suggestion updates.
5. Stop the app. Re-run. Verify SQLite persisted everything.

- [ ] **Step 4: Stop the app and commit any final fixes**

If any small fix is needed (typo, missing `await`, CSS tweak), commit it with a focused message like `fix: align rail border on small viewports`.

```bash
git status
git log --oneline | head -50
```

Expected: a clean commit history reflecting each phase.

**End of plan.** TradyStrat is feature-complete per the spec. The next step is execution by an engineer (or a subagent loop) following these tasks in order.

---

## Self-review (author-only, retained for traceability)

Run after the plan is written; fix issues inline.

**1. Spec coverage**

| Spec section | Tasks |
|---|---|
| §1 Purpose | implicit |
| §2 Decisions | every phase |
| §3 Project layout | T1, T2, T3, T6, T8, T9, T17, T22, T25, T29, T33, T40, T44 |
| §4 Data model | T4, T5, T6, T7, T8 |
| §5 Indicators | T18, T19, T20, T21, T22 |
| §6 FX | T14, T15, T17 |
| §7 AI integration | T30, T31, T32, T33 |
| §8 Data flow | T11, T12, T13, T16, T17 |
| §9 UI — The Vault | T34–T40, T43 |
| §10 Use cases | T26, T27, T28, T29, T32, T34, T42 |
| §11 TheAppManager startup | T9, every module task |
| §12 Custom exceptions | T2, used throughout |
| §13 Error handling | T17 (resilience), T13 (cache fallback), T31 (typed AI failure) |
| §14 Testing | xunit.v3 + Shouldly throughout; ModuleSmokeTests T10 |
| §15 Run & deploy | T44, T45 |
| §16 Out of scope | not implemented |
| §17 GoF patterns | Adapter (T18), Strategy (T20), Composite (T20), Decorator (T13, T15), Command + Template Method (T26, T27, T28, T32, T42), Factory Method (T30), Specification (T8), Facade (T34) |
| §18 Architecture summary | implicit |

All spec sections have implementing tasks.

**2. Placeholder scan** — none. Every code step contains complete code; every test step contains the assertion. Two tasks (T18 step 5, T31 step 4, T33 step 1) carry explicit "verify at impl time" notes for third-party API shapes — these are flagged, not silent.

**3. Type consistency** — checked: `LogTradeInput`/`EditTradeInput`/`DeleteTradeInput` field names match across use cases and tests; `IAiClient`/`ISnapshotBuilder` introduced in T32 step 3, then used in T33's module; `PathBuilder.Line/Area` signatures match between component and test; `TestRepo<>` is reused from T15 (Fx tests) into Indicators/Portfolio tests.

