# MCP Server (Read-Only, Personal) — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add a read-only MCP server as the `mcp` subcommand of `TradyStrat.Cli`, exposing six question-oriented tools (`list_instruments`, `get_dashboard`, `query_suggestions`, `query_prices`, `get_portfolio`, `get_replay_report`) to Claude Desktop / Code over stdio. Establish the reference architecture for future MCP work in this project.

**Architecture:** Three layers, all read-only:

1. **Application-layer additions** — new interface `IIndicatorEngine`, two specs (`PriceBarsInRangeSpec`, `SuggestionsQuerySpec`), two use cases (`GetPriceSeriesUseCase`, `QuerySuggestionsUseCase`).
2. **`Mcp/` folder inside `TradyStrat.Cli`** — DTOs (records), mapper Strategies (one per feature), three call-tool filters (Decorator chain: Logging → Timeout → ExceptionTranslation), `Guards` helper, six tool classes, `McpJsonSerializerOptions` factory, `McpCliModule : IAppModule`.
3. **`McpCommand` Spectre subcommand** — spins up an inner host, calls `AppManager.ConfigureServices` (excluding the price-feed background hosted service), registers `McpCliModule`, runs the SDK's stdio server until stdin EOF.

GoF patterns in play (per spec §13.1): Adapter (tool methods), Facade (composite tools like `get_dashboard`), Decorator (filter pipeline), Strategy (mappers), Specification (Ardalis specs), Composite (the `IAppModule`), Command + Template Method (`IUseCase` + `UseCaseBase`).

**Tech Stack:** C# 13, .NET 10, EF Core 10 (SQLite), Ardalis.Specification 9.3, Spectre.Console(.Cli) 0.51 (already present), `ModelContextProtocol` (new — official Microsoft/Anthropic C# SDK), xunit.v3 + Shouldly + `TradyStrat.TestKit`.

**Worktree:** Use `superpowers:using-git-worktrees` to work in an isolated copy off `main`. All commits run on the worktree branch; the feature ships as one PR.

**Spec:** [`docs/superpowers/specs/2026-05-18-mcp-server-design.md`](../specs/2026-05-18-mcp-server-design.md) (756 lines). The spec is the canonical source of truth — if the plan and spec disagree, the spec wins.

**Out of scope** (per spec §11): writes, MCP Resources, MCP Prompts, HTTP/SSE transport, auth, streaming, multi-instrument batches, `mcp` command options, "summarize" tools.

---

## File structure — what gets created or modified

### TradyStrat.Application (new + modified)

**New:**
- `Indicators/IIndicatorEngine.cs` — interface extracted from sealed concrete class (spec §5.6.1)
- `PriceFeed/Specifications/PriceBarsInRangeSpec.cs` — bars by instrument + inclusive date range, ascending (spec §5.6.2)
- `PriceFeed/UseCases/GetPriceSeriesInput.cs` + `GetPriceSeriesOutput.cs` + `GetPriceSeriesUseCase.cs` — bars + optional indicator series (spec §5.6.3)
- `AiSuggestion/Specifications/SuggestionsQuerySpec.cs` — newest-first, instrument + range + optional action + limit (spec §5.6.2)
- `AiSuggestion/UseCases/QuerySuggestionsInput.cs` + `QuerySuggestionsOutput.cs` + `QuerySuggestionsUseCase.cs` — suggestions with forward-return + correctness (spec §5.6.3)

**Modified:**
- `Indicators/IndicatorEngine.cs` — adopt the new interface (no behaviour change)
- `Indicators/IndicatorsApplicationModule.cs` — register `IIndicatorEngine → IndicatorEngine`
- `PriceFeed/PriceFeedApplicationModule.cs` — register `GetPriceSeriesUseCase`
- `AiSuggestion/AiSuggestionApplicationModule.cs` — register `QuerySuggestionsUseCase`

### TradyStrat.Cli (new + modified)

**New (`Mcp/` folder):**
- `Mcp/McpCliModule.cs` — `IAppModule`; the wiring locus (spec §5.2)
- `Mcp/Serialization/DateOnlyJsonConverter.cs` — ISO YYYY-MM-DD round-trip
- `Mcp/Serialization/McpJsonSerializerOptions.cs` — static factory (spec §6.1)
- `Mcp/Dto/InstrumentDtos.cs`
- `Mcp/Dto/DashboardDtos.cs`
- `Mcp/Dto/SuggestionDtos.cs`
- `Mcp/Dto/PriceDtos.cs`
- `Mcp/Dto/PortfolioDtos.cs`
- `Mcp/Dto/ReplayDtos.cs` — re-export of existing `ReplayReport` (the one DTO exception, spec §6 + §13.4)
- `Mcp/Mapping/InstrumentMapper.cs`
- `Mcp/Mapping/DashboardMapper.cs`
- `Mcp/Mapping/SuggestionMapper.cs`
- `Mcp/Mapping/PriceMapper.cs`
- `Mcp/Mapping/PortfolioMapper.cs`
- `Mcp/Filters/McpLoggingFilter.cs` — outermost
- `Mcp/Filters/McpTimeoutFilter.cs` — 30s budget
- `Mcp/Filters/McpExceptionTranslationFilter.cs` — innermost
- `Mcp/Tools/Guards.cs` — instrument resolution, date range, action parse
- `Mcp/Tools/InstrumentTool.cs`
- `Mcp/Tools/DashboardTool.cs`
- `Mcp/Tools/SuggestionTool.cs`
- `Mcp/Tools/PriceTool.cs`
- `Mcp/Tools/PortfolioTool.cs`
- `Mcp/Tools/ReplayTool.cs`
- `Commands/McpCommand.cs` — Spectre `AsyncCommand`, registers `"mcp"`
- `CliAssemblyMarker.cs` — type marker used for module discovery

**Modified:**
- `TradyStrat.Cli.csproj` — add `<PackageReference Include="ModelContextProtocol" />`
- `Program.cs` — stderr logging globally + register `McpCommand`

### Directory.Packages.props (modified)

- Add `<PackageVersion Include="ModelContextProtocol" Version="..." />` (latest stable at time of implementation)

### TradyStrat.slnx (modified)

- Add `TradyStrat.Cli.Tests/TradyStrat.Cli.Tests.csproj`

### TradyStrat.Cli.Tests (new csproj)

- `TradyStrat.Cli.Tests.csproj` — references CLI, TestKit, Hosting, Application, Infrastructure
- `Mcp/Serialization/DateOnlyJsonConverterTests.cs`
- `Mcp/Serialization/JsonShapeTests.cs` — wire-contract regression bar
- `Mcp/Mapping/InstrumentMapperTests.cs`
- `Mcp/Mapping/DashboardMapperTests.cs`
- `Mcp/Mapping/SuggestionMapperTests.cs`
- `Mcp/Mapping/PriceMapperTests.cs`
- `Mcp/Mapping/PortfolioMapperTests.cs`
- `Mcp/Filters/McpExceptionTranslationFilterTests.cs`
- `Mcp/Filters/McpTimeoutFilterTests.cs`
- `Mcp/Filters/McpLoggingFilterTests.cs`
- `Mcp/Tools/GuardsTests.cs`
- `Mcp/Tools/InstrumentToolTests.cs`
- `Mcp/Tools/DashboardToolTests.cs`
- `Mcp/Tools/SuggestionToolTests.cs`
- `Mcp/Tools/PriceToolTests.cs`
- `Mcp/Tools/PortfolioToolTests.cs`
- `Mcp/Tools/ReplayToolTests.cs`
- `Mcp/McpCliModuleTests.cs` — module wiring smoke
- `Mcp/McpCommandRegistrationTests.cs` — Spectre smoke

### TradyStrat.Application.Tests (new tests for new use cases / specs)

- `PriceFeed/Specifications/PriceBarsInRangeSpecTests.cs`
- `PriceFeed/UseCases/GetPriceSeriesUseCaseTests.cs`
- `AiSuggestion/Specifications/SuggestionsQuerySpecTests.cs`
- `AiSuggestion/UseCases/QuerySuggestionsUseCaseTests.cs`

---

## Phase 0 — Worktree setup

### Task 0.1: Create isolated worktree

**Files:** none (git operation)

- [ ] **Step 1: Invoke the worktrees skill**

The implementing engineer should invoke `superpowers:using-git-worktrees` with a feature name like `mcp-server-2026-05-18`. The skill creates `.worktrees/mcp-server-2026-05-18/` off `main` and switches to a fresh branch. From here on, **all commands run inside the worktree directory** unless otherwise noted.

- [ ] **Step 2: Verify the worktree state**

Run: `git status` and `git rev-parse --abbrev-ref HEAD`
Expected: clean working tree, branch name like `mcp-server-2026-05-18`.

---

## Phase 1 — Project scaffolding

### Task 1.1: Add `ModelContextProtocol` package

**Files:**
- Modify: `Directory.Packages.props`
- Modify: `TradyStrat.Cli/TradyStrat.Cli.csproj`

- [ ] **Step 1: Find current stable version of `ModelContextProtocol`**

Run: `dotnet package search ModelContextProtocol --prerelease false --take 5`
Expected: a list with `ModelContextProtocol` and its current version (e.g., `0.x.y`). Note the version number; it goes in the next step.

- [ ] **Step 2: Add the central package version**

Edit `Directory.Packages.props` — add this line under the existing `<!-- CLI -->` section, alphabetically near `Microsoft.Extensions.Hosting`:

```xml
    <PackageVersion Include="ModelContextProtocol" Version="<VERSION_FROM_STEP_1>" />
```

- [ ] **Step 3: Reference the package from the CLI csproj**

Edit `TradyStrat.Cli/TradyStrat.Cli.csproj` — inside the existing `<ItemGroup>` that lists `Spectre.Console`, append:

```xml
    <PackageReference Include="ModelContextProtocol" />
```

- [ ] **Step 4: Restore + build**

Run: `dotnet restore TradyStrat.Cli && dotnet build TradyStrat.Cli`
Expected: build succeeds. The package surface (`AddMcpServer`, `[McpServerTool]`, etc.) becomes available.

- [ ] **Step 5: Commit**

```bash
git add Directory.Packages.props TradyStrat.Cli/TradyStrat.Cli.csproj
git commit -m "build(cli): add ModelContextProtocol package — Phase 1"
```

### Task 1.2: Create `TradyStrat.Cli.Tests` csproj

**Files:**
- Create: `TradyStrat.Cli.Tests/TradyStrat.Cli.Tests.csproj`
- Modify: `TradyStrat.slnx`

- [ ] **Step 1: Create the directory and csproj**

```bash
mkdir -p TradyStrat.Cli.Tests/Mcp/{Serialization,Mapping,Filters,Tools}
```

Create `TradyStrat.Cli.Tests/TradyStrat.Cli.Tests.csproj`:

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <IsPackable>false</IsPackable>
    <IsTestProject>true</IsTestProject>
    <RootNamespace>TradyStrat.Cli.Tests</RootNamespace>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
  </PropertyGroup>
  <ItemGroup>
    <ProjectReference Include="..\TradyStrat.Cli\TradyStrat.Cli.csproj" />
    <ProjectReference Include="..\TradyStrat.Application\TradyStrat.Application.csproj" />
    <ProjectReference Include="..\TradyStrat.Infrastructure\TradyStrat.Infrastructure.csproj" />
    <ProjectReference Include="..\TradyStrat.Hosting\TradyStrat.Hosting.csproj" />
    <ProjectReference Include="..\TradyStrat.TestKit\TradyStrat.TestKit.csproj" />
  </ItemGroup>
  <ItemGroup>
    <PackageReference Include="xunit.v3" />
    <PackageReference Include="xunit.runner.visualstudio" />
    <PackageReference Include="Microsoft.NET.Test.Sdk" />
    <PackageReference Include="Shouldly" />
  </ItemGroup>
</Project>
```

- [ ] **Step 2: Add to slnx**

Open `TradyStrat.slnx`. Find the existing test projects section (look for `TradyStrat.Application.Tests` or similar) and add a line analogous to the others, e.g.:

```xml
    <Project Path="TradyStrat.Cli.Tests/TradyStrat.Cli.Tests.csproj" />
```

Match the casing/format of the surrounding entries.

- [ ] **Step 3: Add a smoke test file so the project has content**

Create `TradyStrat.Cli.Tests/SmokeTest.cs`:

```csharp
using Shouldly;
using Xunit;

namespace TradyStrat.Cli.Tests;

public class SmokeTest
{
    [Fact]
    public void Project_compiles_and_tests_run()
    {
        true.ShouldBeTrue();
    }
}
```

- [ ] **Step 4: Verify build + test discovery**

Run: `dotnet test TradyStrat.Cli.Tests`
Expected: `Passed: 1, Failed: 0`.

- [ ] **Step 5: Commit**

```bash
git add TradyStrat.Cli.Tests TradyStrat.slnx
git commit -m "test(cli): scaffold TradyStrat.Cli.Tests project — Phase 1"
```

### Task 1.3: Add `CliAssemblyMarker`

**Files:**
- Create: `TradyStrat.Cli/CliAssemblyMarker.cs`

- [ ] **Step 1: Create the marker**

```csharp
namespace TradyStrat.Cli;

/// Marker type used by AppManager's assembly-scan-based module discovery.
internal sealed class CliAssemblyMarker;
```

- [ ] **Step 2: Verify it compiles**

Run: `dotnet build TradyStrat.Cli`
Expected: build succeeds.

- [ ] **Step 3: Commit**

```bash
git add TradyStrat.Cli/CliAssemblyMarker.cs
git commit -m "chore(cli): add CliAssemblyMarker for module discovery — Phase 1"
```

---

## Phase 2 — JSON serialization primitives

### Task 2.1: `DateOnlyJsonConverter` (TDD)

**Files:**
- Create: `TradyStrat.Cli/Mcp/Serialization/DateOnlyJsonConverter.cs`
- Test: `TradyStrat.Cli.Tests/Mcp/Serialization/DateOnlyJsonConverterTests.cs`

- [ ] **Step 1: Write the failing tests**

```bash
mkdir -p TradyStrat.Cli/Mcp/Serialization
```

Create `TradyStrat.Cli.Tests/Mcp/Serialization/DateOnlyJsonConverterTests.cs`:

```csharp
using System.Text.Json;
using Shouldly;
using TradyStrat.Cli.Mcp.Serialization;
using Xunit;

namespace TradyStrat.Cli.Tests.Mcp.Serialization;

public class DateOnlyJsonConverterTests
{
    private static JsonSerializerOptions Opts() =>
        new() { Converters = { new DateOnlyJsonConverter() } };

    [Fact]
    public void Serializes_to_iso_yyyy_mm_dd()
    {
        var json = JsonSerializer.Serialize(new DateOnly(2026, 5, 18), Opts());
        json.ShouldBe("\"2026-05-18\"");
    }

    [Fact]
    public void Roundtrips_iso_string_to_dateonly()
    {
        var d = JsonSerializer.Deserialize<DateOnly>("\"2026-05-18\"", Opts());
        d.ShouldBe(new DateOnly(2026, 5, 18));
    }

    [Fact]
    public void Rejects_unparseable_string()
    {
        var act = () => JsonSerializer.Deserialize<DateOnly>("\"not-a-date\"", Opts());
        act.ShouldThrow<JsonException>();
    }
}
```

- [ ] **Step 2: Run the tests to verify they fail**

Run: `dotnet test TradyStrat.Cli.Tests --filter FullyQualifiedName~DateOnlyJsonConverterTests`
Expected: compilation error — `DateOnlyJsonConverter` doesn't exist yet.

- [ ] **Step 3: Implement the converter**

Create `TradyStrat.Cli/Mcp/Serialization/DateOnlyJsonConverter.cs`:

```csharp
using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace TradyStrat.Cli.Mcp.Serialization;

internal sealed class DateOnlyJsonConverter : JsonConverter<DateOnly>
{
    private const string Format = "yyyy-MM-dd";

    public override DateOnly Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var s = reader.GetString()
            ?? throw new JsonException("Expected a non-null ISO date string.");
        if (DateOnly.TryParseExact(s, Format, CultureInfo.InvariantCulture, DateTimeStyles.None, out var d))
            return d;
        throw new JsonException($"Invalid date '{s}' — use ISO {Format}.");
    }

    public override void Write(Utf8JsonWriter writer, DateOnly value, JsonSerializerOptions options)
        => writer.WriteStringValue(value.ToString(Format, CultureInfo.InvariantCulture));
}
```

- [ ] **Step 4: Run the tests to verify they pass**

Run: `dotnet test TradyStrat.Cli.Tests --filter FullyQualifiedName~DateOnlyJsonConverterTests`
Expected: `Passed: 3, Failed: 0`.

- [ ] **Step 5: Commit**

```bash
git add TradyStrat.Cli/Mcp/Serialization/DateOnlyJsonConverter.cs \
        TradyStrat.Cli.Tests/Mcp/Serialization/DateOnlyJsonConverterTests.cs
git commit -m "feat(mcp): DateOnly JSON converter — Phase 2"
```

### Task 2.2: `McpJsonSerializerOptions` factory

**Files:**
- Create: `TradyStrat.Cli/Mcp/Serialization/McpJsonSerializerOptions.cs`
- Modify: `TradyStrat.Cli.Tests/Mcp/Serialization/JsonShapeTests.cs` (will grow with each phase)

- [ ] **Step 1: Implement the factory**

Create `TradyStrat.Cli/Mcp/Serialization/McpJsonSerializerOptions.cs`:

```csharp
using System.Text.Json;
using System.Text.Json.Serialization;

namespace TradyStrat.Cli.Mcp.Serialization;

internal static class McpJsonSerializerOptions
{
    public static JsonSerializerOptions Create()
    {
        var o = new JsonSerializerOptions(JsonSerializerDefaults.Web)
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = JsonIgnoreCondition.Never,
        };
        o.Converters.Add(new JsonStringEnumConverter());
        o.Converters.Add(new DateOnlyJsonConverter());
        return o;
    }
}
```

- [ ] **Step 2: Add the first JsonShapeTests**

Create `TradyStrat.Cli.Tests/Mcp/Serialization/JsonShapeTests.cs`:

```csharp
using System.Text.Json;
using Shouldly;
using TradyStrat.Cli.Mcp.Serialization;
using Xunit;

namespace TradyStrat.Cli.Tests.Mcp.Serialization;

public class JsonShapeTests
{
    private static readonly JsonSerializerOptions Opts = McpJsonSerializerOptions.Create();

    [Fact]
    public void DateOnly_is_iso_yyyy_mm_dd()
    {
        var json = JsonSerializer.Serialize(new { date = new DateOnly(2026, 5, 18) }, Opts);
        json.ShouldContain("\"date\":\"2026-05-18\"");
    }

    [Fact]
    public void Decimal_is_json_number_not_string()
    {
        var json = JsonSerializer.Serialize(new { price = 24.13m }, Opts);
        json.ShouldContain("\"price\":24.13");
        json.ShouldNotContain("\"24.13\"");
    }

    [Fact]
    public void Property_names_are_camelCase()
    {
        var json = JsonSerializer.Serialize(new { MarketValueEur = 100m }, Opts);
        json.ShouldContain("\"marketValueEur\"");
        json.ShouldNotContain("\"MarketValueEur\"");
    }

    private enum SampleEnum { Acquire, Hold }

    [Fact]
    public void Enums_are_pascalcase_strings()
    {
        var json = JsonSerializer.Serialize(new { action = SampleEnum.Acquire }, Opts);
        json.ShouldContain("\"action\":\"Acquire\"");
    }

    [Fact]
    public void Null_propagates_explicitly()
    {
        var json = JsonSerializer.Serialize(new { forwardReturnPct = (decimal?)null }, Opts);
        json.ShouldContain("\"forwardReturnPct\":null");
    }
}
```

- [ ] **Step 3: Run the tests**

Run: `dotnet test TradyStrat.Cli.Tests --filter FullyQualifiedName~JsonShapeTests`
Expected: `Passed: 5, Failed: 0`.

- [ ] **Step 4: Commit**

```bash
git add TradyStrat.Cli/Mcp/Serialization/McpJsonSerializerOptions.cs \
        TradyStrat.Cli.Tests/Mcp/Serialization/JsonShapeTests.cs
git commit -m "feat(mcp): McpJsonSerializerOptions factory + JSON shape regression bar — Phase 2"
```

---

## Phase 3 — Application-layer additions

### Task 3.1: Extract `IIndicatorEngine` interface

**Files:**
- Create: `TradyStrat.Application/Indicators/IIndicatorEngine.cs`
- Modify: `TradyStrat.Application/Indicators/IndicatorEngine.cs`
- Modify: `TradyStrat.Application/Indicators/IndicatorsApplicationModule.cs`

- [ ] **Step 1: Inspect the concrete class to capture its surface**

Run: `grep -nE "public (Task|async Task)" TradyStrat.Application/Indicators/IndicatorEngine.cs`
Expected: four `ComputeFor` / `HistoryFor` overloads matching spec §5.6.1. Copy the exact signatures (including XML doc if any).

- [ ] **Step 2: Create the interface**

Create `TradyStrat.Application/Indicators/IIndicatorEngine.cs`:

```csharp
namespace TradyStrat.Application.Indicators;

public interface IIndicatorEngine
{
    Task<IndicatorReading> ComputeFor(string ticker, CancellationToken ct);
    Task<IndicatorReading> ComputeFor(string ticker, DateOnly asOf, CancellationToken ct);
    Task<IndicatorSeries> HistoryFor(string ticker, IndicatorKind kind, int lastN, CancellationToken ct);
    Task<IndicatorSeries> HistoryFor(string ticker, IndicatorKind kind, int lastN, DateOnly asOf, CancellationToken ct);
}
```

If the actual concrete method signatures differ (parameter names, additional defaults), match them exactly. The interface is a literal extract — no signature drift.

- [ ] **Step 3: Make `IndicatorEngine` implement the interface**

Edit `TradyStrat.Application/Indicators/IndicatorEngine.cs`:

Change `public sealed class IndicatorEngine` to `public sealed class IndicatorEngine : IIndicatorEngine`.
No other code changes — the concrete methods already match the interface.

- [ ] **Step 4: Register the interface in the module**

Edit `TradyStrat.Application/Indicators/IndicatorsApplicationModule.cs`. Find the line registering `IndicatorEngine` (likely `services.AddScoped<IndicatorEngine>()` or similar) and change it to register both the interface and the concrete:

```csharp
services.AddScoped<IIndicatorEngine, IndicatorEngine>();
```

If existing consumers inject `IndicatorEngine` directly (concrete), keep an additional registration:

```csharp
services.AddScoped<IIndicatorEngine, IndicatorEngine>();
services.AddScoped<IndicatorEngine>(sp => (IndicatorEngine)sp.GetRequiredService<IIndicatorEngine>());
```

Run: `grep -rn "IndicatorEngine " TradyStrat.Application TradyStrat.Infrastructure TradyStrat --include="*.cs" | grep -v "Indicators/" | grep -v "^.*Tests/"` to identify existing concrete-class consumers. If none exist outside the Indicators module, the simple registration suffices.

- [ ] **Step 5: Build + run existing tests**

Run: `dotnet build && dotnet test TradyStrat.Application.Tests`
Expected: build succeeds; all existing tests continue to pass.

- [ ] **Step 6: Commit**

```bash
git add TradyStrat.Application/Indicators
git commit -m "refactor(indicators): extract IIndicatorEngine interface — Phase 3"
```

### Task 3.2: `PriceBarsInRangeSpec` (TDD)

**Files:**
- Create: `TradyStrat.Application/PriceFeed/Specifications/PriceBarsInRangeSpec.cs`
- Test: `TradyStrat.Application.Tests/PriceFeed/Specifications/PriceBarsInRangeSpecTests.cs`

- [ ] **Step 1: Inspect the existing specs to match style**

Run: `cat TradyStrat.Application/PriceFeed/Specifications/PriceBarsAsOfSpec.cs`

Note the namespace, base class (likely `Specification<PriceBar>`), and ordering convention. Use the same shape.

- [ ] **Step 2: Write the failing test**

Create `TradyStrat.Application.Tests/PriceFeed/Specifications/PriceBarsInRangeSpecTests.cs`:

```csharp
using Ardalis.Specification;
using Shouldly;
using TradyStrat.Application.PriceFeed.Specifications;
using TradyStrat.Domain;
using TradyStrat.TestKit.Specifications;
using Xunit;

namespace TradyStrat.Application.Tests.PriceFeed.Specifications;

public class PriceBarsInRangeSpecTests
{
    [Fact]
    public async Task Returns_bars_in_range_ascending_for_instrument()
    {
        await using var db = InMemoryDb.Create();
        var inst = new Instrument(/* fixture: ticker "CON3.L", id 1 */);
        db.Instruments.Add(inst);
        db.PriceBars.AddRange(
            new PriceBar(inst.Id, new DateOnly(2026, 5, 16), 10m, 11m, 9m, 10.5m, 100),
            new PriceBar(inst.Id, new DateOnly(2026, 5, 17), 10.5m, 12m, 10m, 11.8m, 110),
            new PriceBar(inst.Id, new DateOnly(2026, 5, 18), 11.8m, 12.5m, 11m, 12.2m, 95),
            new PriceBar(inst.Id, new DateOnly(2026, 5, 19), 12.2m, 13m, 12m, 12.7m, 80));
        await db.SaveChangesAsync();

        var repo = new TestRepo<PriceBar>(db);
        var result = await repo.ListAsync(
            new PriceBarsInRangeSpec(inst.Id, new DateOnly(2026, 5, 17), new DateOnly(2026, 5, 18)));

        result.Select(b => b.Date).ShouldBe(new[]
        {
            new DateOnly(2026, 5, 17),
            new DateOnly(2026, 5, 18)
        });
    }

    [Fact]
    public async Task Excludes_bars_for_other_instruments()
    {
        await using var db = InMemoryDb.Create();
        // ... two instruments, one bar each on same date
        // assert only the requested instrument's bar comes back
        // (left as exercise; mirrors the structure above)
    }

    [Fact]
    public async Task Empty_range_returns_empty()
    {
        await using var db = InMemoryDb.Create();
        var inst = new Instrument(/* fixture */);
        db.Instruments.Add(inst);
        await db.SaveChangesAsync();

        var repo = new TestRepo<PriceBar>(db);
        var result = await repo.ListAsync(
            new PriceBarsInRangeSpec(inst.Id, new DateOnly(2026, 5, 17), new DateOnly(2026, 5, 18)));

        result.ShouldBeEmpty();
    }
}
```

Adjust the `Instrument` / `PriceBar` constructor calls to match the actual constructors in `TradyStrat.Domain` — peek at `TradyStrat.Domain/Instrument.cs` and `PriceBar.cs` first.

- [ ] **Step 3: Run the tests to verify they fail**

Run: `dotnet test TradyStrat.Application.Tests --filter FullyQualifiedName~PriceBarsInRangeSpecTests`
Expected: compile error — spec doesn't exist.

- [ ] **Step 4: Implement the spec**

Create `TradyStrat.Application/PriceFeed/Specifications/PriceBarsInRangeSpec.cs`:

```csharp
using Ardalis.Specification;
using TradyStrat.Domain;

namespace TradyStrat.Application.PriceFeed.Specifications;

public sealed class PriceBarsInRangeSpec : Specification<PriceBar>
{
    public PriceBarsInRangeSpec(int instrumentId, DateOnly from, DateOnly to)
    {
        Query
            .Where(b => b.InstrumentId == instrumentId
                     && b.Date >= from
                     && b.Date <= to)
            .OrderBy(b => b.Date);
    }
}
```

- [ ] **Step 5: Run the tests to verify they pass**

Run: `dotnet test TradyStrat.Application.Tests --filter FullyQualifiedName~PriceBarsInRangeSpecTests`
Expected: `Passed: 3, Failed: 0`.

- [ ] **Step 6: Commit**

```bash
git add TradyStrat.Application/PriceFeed/Specifications/PriceBarsInRangeSpec.cs \
        TradyStrat.Application.Tests/PriceFeed/Specifications/PriceBarsInRangeSpecTests.cs
git commit -m "feat(price-feed): PriceBarsInRangeSpec — Phase 3"
```

### Task 3.3: `SuggestionsQuerySpec` (TDD)

**Files:**
- Create: `TradyStrat.Application/AiSuggestion/Specifications/SuggestionsQuerySpec.cs`
- Test: `TradyStrat.Application.Tests/AiSuggestion/Specifications/SuggestionsQuerySpecTests.cs`

- [ ] **Step 1: Inspect existing spec for style**

Run: `cat TradyStrat.Application/AiSuggestion/Specifications/SuggestionsInRangeSpec.cs`

Note the namespace, base class, and how `Action` filtering would compose if added.

- [ ] **Step 2: Write the failing test**

Create `TradyStrat.Application.Tests/AiSuggestion/Specifications/SuggestionsQuerySpecTests.cs`:

```csharp
using Shouldly;
using TradyStrat.Application.AiSuggestion.Specifications;
using TradyStrat.Domain;
using TradyStrat.TestKit;
using TradyStrat.TestKit.Specifications;
using Xunit;

namespace TradyStrat.Application.Tests.AiSuggestion.Specifications;

public class SuggestionsQuerySpecTests
{
    [Fact]
    public async Task Returns_newest_first_within_range_for_instrument()
    {
        await using var db = InMemoryDb.Create();
        // seed three suggestions on consecutive days; same instrument
        // (concrete constructor call: copy from existing test helpers in TestKit)
        // ...
        await db.SaveChangesAsync();

        var repo = new TestRepo<Suggestion>(db);
        var result = await repo.ListAsync(
            new SuggestionsQuerySpec(1, new DateOnly(2026, 5, 1), new DateOnly(2026, 5, 18), action: null, limit: 30));

        result.Select(s => s.ForDate).ShouldBeInOrder(SortDirection.Descending);
    }

    [Fact]
    public async Task Filters_by_action_when_provided()
    {
        await using var db = InMemoryDb.Create();
        // seed one Acquire and one Hold on same date
        // ...
        await db.SaveChangesAsync();

        var repo = new TestRepo<Suggestion>(db);
        var result = await repo.ListAsync(
            new SuggestionsQuerySpec(1, new DateOnly(2026, 5, 1), new DateOnly(2026, 5, 18),
                action: SuggestionAction.Acquire, limit: 30));

        result.Count.ShouldBe(1);
        result[0].Action.ShouldBe(SuggestionAction.Acquire);
    }

    [Fact]
    public async Task Limit_is_honoured()
    {
        await using var db = InMemoryDb.Create();
        // seed 5 suggestions
        // ...
        await db.SaveChangesAsync();

        var repo = new TestRepo<Suggestion>(db);
        var result = await repo.ListAsync(
            new SuggestionsQuerySpec(1, DateOnly.MinValue, DateOnly.MaxValue, action: null, limit: 2));

        result.Count.ShouldBe(2);
    }
}
```

Use existing `TradyStrat.TestKit` helpers for building `Suggestion` fixtures — peek at `TradyStrat.Application.Tests/AiSuggestion/...` for patterns.

- [ ] **Step 3: Run the tests to verify they fail**

Run: `dotnet test TradyStrat.Application.Tests --filter FullyQualifiedName~SuggestionsQuerySpecTests`
Expected: compile error.

- [ ] **Step 4: Implement the spec**

Create `TradyStrat.Application/AiSuggestion/Specifications/SuggestionsQuerySpec.cs`:

```csharp
using Ardalis.Specification;
using TradyStrat.Domain;

namespace TradyStrat.Application.AiSuggestion.Specifications;

public sealed class SuggestionsQuerySpec : Specification<Suggestion>
{
    public SuggestionsQuerySpec(int instrumentId, DateOnly from, DateOnly to, SuggestionAction? action, int limit)
    {
        Query
            .Where(s => s.InstrumentId == instrumentId
                     && s.ForDate >= from
                     && s.ForDate <= to)
            .OrderByDescending(s => s.ForDate)
            .Take(limit);

        if (action.HasValue)
            Query.Where(s => s.Action == action.Value);
    }
}
```

- [ ] **Step 5: Run the tests to verify they pass**

Run: `dotnet test TradyStrat.Application.Tests --filter FullyQualifiedName~SuggestionsQuerySpecTests`
Expected: `Passed: 3, Failed: 0`.

- [ ] **Step 6: Commit**

```bash
git add TradyStrat.Application/AiSuggestion/Specifications/SuggestionsQuerySpec.cs \
        TradyStrat.Application.Tests/AiSuggestion/Specifications/SuggestionsQuerySpecTests.cs
git commit -m "feat(ai-suggestion): SuggestionsQuerySpec — Phase 3"
```

### Task 3.4: `GetPriceSeriesUseCase` (TDD)

**Files:**
- Create: `TradyStrat.Application/PriceFeed/UseCases/GetPriceSeriesInput.cs`
- Create: `TradyStrat.Application/PriceFeed/UseCases/GetPriceSeriesOutput.cs`
- Create: `TradyStrat.Application/PriceFeed/UseCases/GetPriceSeriesUseCase.cs`
- Modify: `TradyStrat.Application/PriceFeed/PriceFeedApplicationModule.cs`
- Test: `TradyStrat.Application.Tests/PriceFeed/UseCases/GetPriceSeriesUseCaseTests.cs`

- [ ] **Step 1: Define the input + output records**

Create `TradyStrat.Application/PriceFeed/UseCases/GetPriceSeriesInput.cs`:

```csharp
namespace TradyStrat.Application.PriceFeed.UseCases;

public sealed record GetPriceSeriesInput(int InstrumentId, DateOnly From, DateOnly To, bool WithIndicators);
```

Create `TradyStrat.Application/PriceFeed/UseCases/GetPriceSeriesOutput.cs`:

```csharp
using TradyStrat.Domain;

namespace TradyStrat.Application.PriceFeed.UseCases;

public sealed record GetPriceSeriesOutput(
    IReadOnlyList<PriceBar> Bars,
    IndicatorArrays? Indicators);

public sealed record IndicatorArrays(
    IReadOnlyList<decimal?> Rsi,
    IReadOnlyList<decimal?> Sma20,
    IReadOnlyList<decimal?> Sma50,
    IReadOnlyList<decimal?> Sma200,
    IReadOnlyList<decimal?> BbUpper,
    IReadOnlyList<decimal?> BbMid,
    IReadOnlyList<decimal?> BbLower,
    IReadOnlyList<decimal?> IchimokuTenkan,
    IReadOnlyList<decimal?> IchimokuKijun,
    IReadOnlyList<decimal?> IchimokuSpanA,
    IReadOnlyList<decimal?> IchimokuSpanB);
```

- [ ] **Step 2: Write the failing test**

Create `TradyStrat.Application.Tests/PriceFeed/UseCases/GetPriceSeriesUseCaseTests.cs`:

```csharp
using Shouldly;
using TradyStrat.Application.PriceFeed.UseCases;
using TradyStrat.TestKit.Specifications;
using Xunit;

namespace TradyStrat.Application.Tests.PriceFeed.UseCases;

public class GetPriceSeriesUseCaseTests
{
    [Fact]
    public async Task Returns_bars_in_range_without_indicators()
    {
        // Build an in-memory host with TestKit fixtures, resolve GetPriceSeriesUseCase,
        // seed 5 bars across a 5-day range, request days 2..4, assert 3 bars returned,
        // Indicators is null.
        // ... (use the same composition pattern as existing UseCase tests in this folder)
    }

    [Fact]
    public async Task Populates_indicator_arrays_when_withIndicators_true()
    {
        // Same as above with WithIndicators=true.
        // Assert Indicators is not null and arrays have same length as Bars.
        // Assert first N entries are null for lookback-bounded indicators (e.g., Rsi[0..13] null for RSI-14).
    }

    [Fact]
    public async Task Empty_range_returns_empty_bars_and_null_indicators()
    {
        // Request a range with no bars; assert Bars empty and Indicators null
        // (we don't allocate indicator arrays for empty input).
    }
}
```

Look at `TradyStrat.Application.Tests/Dashboard/UseCases/LoadDashboardUseCaseTests.cs` for the in-memory host composition pattern.

- [ ] **Step 3: Run to verify the tests fail**

Run: `dotnet test TradyStrat.Application.Tests --filter FullyQualifiedName~GetPriceSeriesUseCaseTests`
Expected: compile error.

- [ ] **Step 4: Implement the use case**

Create `TradyStrat.Application/PriceFeed/UseCases/GetPriceSeriesUseCase.cs`:

```csharp
using Ardalis.Specification;
using Microsoft.Extensions.Logging;
using TradyStrat.Application.Indicators;
using TradyStrat.Application.PriceFeed.Specifications;
using TradyStrat.Application.UseCases;
using TradyStrat.Domain;

namespace TradyStrat.Application.PriceFeed.UseCases;

public sealed class GetPriceSeriesUseCase(
    IReadRepositoryBase<PriceBar> bars,
    IReadRepositoryBase<Instrument> instruments,
    IIndicatorEngine indicators,
    ILogger<GetPriceSeriesUseCase> logger)
    : UseCaseBase<GetPriceSeriesInput, GetPriceSeriesOutput>(logger)
{
    protected override async Task<GetPriceSeriesOutput> ExecuteCore(GetPriceSeriesInput input, CancellationToken ct)
    {
        var range = await bars.ListAsync(
            new PriceBarsInRangeSpec(input.InstrumentId, input.From, input.To), ct);

        if (!input.WithIndicators || range.Count == 0)
            return new GetPriceSeriesOutput(range, Indicators: null);

        var instrument = await instruments.GetByIdAsync(input.InstrumentId, ct)
            ?? throw new InstrumentNotFoundException(input.InstrumentId.ToString());

        // Request full series for each indicator kind, then align to the range
        // by date. Length-N lookback means leading entries are null.
        var n = range.Count;
        var asOf = input.To;
        var rsi    = await Aligned(indicators, instrument.Ticker, IndicatorKind.Rsi,    n, asOf, range, ct);
        var sma20  = await Aligned(indicators, instrument.Ticker, IndicatorKind.Sma20,  n, asOf, range, ct);
        var sma50  = await Aligned(indicators, instrument.Ticker, IndicatorKind.Sma50,  n, asOf, range, ct);
        var sma200 = await Aligned(indicators, instrument.Ticker, IndicatorKind.Sma200, n, asOf, range, ct);
        var bbU    = await Aligned(indicators, instrument.Ticker, IndicatorKind.BollingerUpper, n, asOf, range, ct);
        var bbM    = await Aligned(indicators, instrument.Ticker, IndicatorKind.BollingerMid,   n, asOf, range, ct);
        var bbL    = await Aligned(indicators, instrument.Ticker, IndicatorKind.BollingerLower, n, asOf, range, ct);
        var tenkan = await Aligned(indicators, instrument.Ticker, IndicatorKind.IchimokuTenkan, n, asOf, range, ct);
        var kijun  = await Aligned(indicators, instrument.Ticker, IndicatorKind.IchimokuKijun,  n, asOf, range, ct);
        var spanA  = await Aligned(indicators, instrument.Ticker, IndicatorKind.IchimokuSpanA,  n, asOf, range, ct);
        var spanB  = await Aligned(indicators, instrument.Ticker, IndicatorKind.IchimokuSpanB,  n, asOf, range, ct);

        return new GetPriceSeriesOutput(range,
            new IndicatorArrays(rsi, sma20, sma50, sma200, bbU, bbM, bbL, tenkan, kijun, spanA, spanB));
    }

    private static async Task<IReadOnlyList<decimal?>> Aligned(
        IIndicatorEngine engine, string ticker, IndicatorKind kind, int lastN,
        DateOnly asOf, IReadOnlyList<PriceBar> range, CancellationToken ct)
    {
        var series = await engine.HistoryFor(ticker, kind, lastN, asOf, ct);
        // series.Points is a list of (Date, Value?) aligned to bar dates.
        // If the engine returns a list of nullable decimals indexed by date,
        // join it to range by date. The concrete return shape of IndicatorSeries
        // is in TradyStrat.Application/Indicators/IndicatorSeries.cs — read that
        // file to confirm how to extract per-date values.
        return range.Select(b => series.ValueAt(b.Date)).ToList();
    }
}
```

The exact shape of `IndicatorSeries.ValueAt` may not exist; if `IndicatorSeries` exposes `IReadOnlyList<IndicatorPoint>` instead, build a `Dictionary<DateOnly, decimal?>` once and look up per bar. Adjust to the actual shape — read `TradyStrat.Application/Indicators/IndicatorSeries.cs` and `IndicatorKind.cs` first; **if `IndicatorKind` doesn't include the granular values listed above**, that's a finding: the implementer should either extend `IndicatorKind` or change `IndicatorArrays` to match the engine's actual surface. **Do not invent enum values that don't exist** — make `IndicatorArrays` match `IndicatorKind` exactly.

- [ ] **Step 5: Register the use case in the module**

Edit `TradyStrat.Application/PriceFeed/PriceFeedApplicationModule.cs`. Add:

```csharp
services.AddScoped<GetPriceSeriesUseCase>();
```

(Match the existing registration style — likely `AddScoped` or via a convention helper.)

- [ ] **Step 6: Run the tests to verify they pass**

Run: `dotnet test TradyStrat.Application.Tests --filter FullyQualifiedName~GetPriceSeriesUseCaseTests`
Expected: `Passed: 3, Failed: 0`.

- [ ] **Step 7: Commit**

```bash
git add TradyStrat.Application/PriceFeed \
        TradyStrat.Application.Tests/PriceFeed/UseCases
git commit -m "feat(price-feed): GetPriceSeriesUseCase — Phase 3"
```

### Task 3.5: `QuerySuggestionsUseCase` (TDD)

**Files:**
- Create: `TradyStrat.Application/AiSuggestion/UseCases/QuerySuggestionsInput.cs`
- Create: `TradyStrat.Application/AiSuggestion/UseCases/QuerySuggestionsOutput.cs`
- Create: `TradyStrat.Application/AiSuggestion/UseCases/QuerySuggestionsUseCase.cs`
- Modify: `TradyStrat.Application/AiSuggestion/AiSuggestionApplicationModule.cs`
- Test: `TradyStrat.Application.Tests/AiSuggestion/UseCases/QuerySuggestionsUseCaseTests.cs`

- [ ] **Step 1: Define input + output**

Create `TradyStrat.Application/AiSuggestion/UseCases/QuerySuggestionsInput.cs`:

```csharp
using TradyStrat.Domain;

namespace TradyStrat.Application.AiSuggestion.UseCases;

public sealed record QuerySuggestionsInput(
    int InstrumentId,
    DateOnly From,
    DateOnly To,
    SuggestionAction? Action,
    int Limit);
```

Create `TradyStrat.Application/AiSuggestion/UseCases/QuerySuggestionsOutput.cs`:

```csharp
using TradyStrat.Domain;

namespace TradyStrat.Application.AiSuggestion.UseCases;

public sealed record QuerySuggestionsOutput(IReadOnlyList<QueriedSuggestion> Items);

public sealed record QueriedSuggestion(
    DateOnly Date,
    SuggestionAction Action,
    int Conviction,
    string Reasoning,
    string? EnvelopeHash,
    string? PromptVersionHash,
    decimal? ForwardReturnPct,
    bool? Correct);
```

- [ ] **Step 2: Write the failing test**

Create `TradyStrat.Application.Tests/AiSuggestion/UseCases/QuerySuggestionsUseCaseTests.cs`:

```csharp
using Shouldly;
using TradyStrat.Application.AiSuggestion.UseCases;
using TradyStrat.Domain;
using Xunit;

namespace TradyStrat.Application.Tests.AiSuggestion.UseCases;

public class QuerySuggestionsUseCaseTests
{
    [Fact]
    public async Task Returns_suggestions_newest_first_with_outcome_when_evaluable()
    {
        // Compose host, seed: 1 suggestion 10 days ago (forward return evaluable), 1 today (not yet).
        // Call ExecuteAsync with limit=10.
        // Assert: Items.Count == 2, Items[0].Date is today, Items[1].ForwardReturnPct is not null,
        // Items[0].ForwardReturnPct is null, Items[0].Correct is null.
    }

    [Fact]
    public async Task Limit_truncates_results()
    {
        // Seed 5 suggestions, request limit=2, assert Items.Count == 2.
    }

    [Fact]
    public async Task Action_filter_narrows_results()
    {
        // Seed 2 Acquire + 1 Hold, filter by Acquire, assert Items.Count == 2.
    }
}
```

- [ ] **Step 3: Run to verify failure**

Run: `dotnet test TradyStrat.Application.Tests --filter FullyQualifiedName~QuerySuggestionsUseCaseTests`
Expected: compile error.

- [ ] **Step 4: Implement the use case**

Create `TradyStrat.Application/AiSuggestion/UseCases/QuerySuggestionsUseCase.cs`:

```csharp
using Ardalis.Specification;
using Microsoft.Extensions.Logging;
using TradyStrat.Application.AiSuggestion.Specifications;
using TradyStrat.Application.UseCases;
using TradyStrat.Domain;

namespace TradyStrat.Application.AiSuggestion.UseCases;

public sealed class QuerySuggestionsUseCase(
    IReadRepositoryBase<Suggestion> suggestions,
    IReadRepositoryBase<PriceBar> bars,
    ICorrectnessRule correctness,
    ILogger<QuerySuggestionsUseCase> logger)
    : UseCaseBase<QuerySuggestionsInput, QuerySuggestionsOutput>(logger)
{
    private const int ForwardReturnWindow = 5; // trading days; matches spec §4.3

    protected override async Task<QuerySuggestionsOutput> ExecuteCore(QuerySuggestionsInput input, CancellationToken ct)
    {
        var rows = await suggestions.ListAsync(
            new SuggestionsQuerySpec(input.InstrumentId, input.From, input.To, input.Action, input.Limit), ct);

        var items = new List<QueriedSuggestion>(rows.Count);
        foreach (var s in rows)
        {
            var fwd = await ComputeForwardReturn(s, ct);
            var correct = fwd.HasValue ? correctness.WasCorrect(s.Action, fwd.Value) : (bool?)null;
            items.Add(new QueriedSuggestion(
                s.ForDate, s.Action, s.Conviction, s.Rationale,
                s.EnvelopeHash, s.PromptVersionHash,
                fwd, correct));
        }
        return new QuerySuggestionsOutput(items);
    }

    private async Task<decimal?> ComputeForwardReturn(Suggestion s, CancellationToken ct)
    {
        // Look up the bar on s.ForDate and ForwardReturnWindow trading days later.
        // If either is missing (e.g., today + 5 hasn't happened), return null.
        // Reuse whatever helper the existing RecentSuggestionsSection uses —
        // grep for "WasCorrect" or "ForwardReturn" in AiSuggestion/Snapshot/Sections
        // to find the shared computation. If only a private helper exists there,
        // extract it to a public helper or duplicate the small calc here.
        throw new NotImplementedException("Fill in by reusing existing forward-return logic");
    }
}
```

**Note for implementer:** the forward-return computation already exists in `RecentSuggestionsSection` (per spec §4.3). Find it (`grep -rn "ForwardReturn\|fwd_return" TradyStrat.Application`) and **extract** the small helper to `TradyStrat.Application/AiSuggestion/ForwardReturnCalculator.cs` so both `RecentSuggestionsSection` and `QuerySuggestionsUseCase` use it. Don't duplicate the logic.

- [ ] **Step 5: Extract `ForwardReturnCalculator` helper if not already shared**

```bash
grep -rn "ForwardReturn\|fwd_return\|ForReturnPct" TradyStrat.Application --include="*.cs"
```

If the calc is in a private method of `RecentSuggestionsSection`, lift it into a new `ForwardReturnCalculator` class (public, static or scoped) and update `RecentSuggestionsSection` to call it. Tests for `RecentSuggestionsSection` should still pass unchanged.

- [ ] **Step 6: Wire `QuerySuggestionsUseCase` to use the helper**

Replace the `throw new NotImplementedException(...)` with a call to `ForwardReturnCalculator.Compute(s, bars, ct)`.

- [ ] **Step 7: Register the use case**

Edit `TradyStrat.Application/AiSuggestion/AiSuggestionApplicationModule.cs`. Add:

```csharp
services.AddScoped<QuerySuggestionsUseCase>();
```

- [ ] **Step 8: Run the tests**

Run: `dotnet test TradyStrat.Application.Tests --filter FullyQualifiedName~QuerySuggestionsUseCaseTests`
Expected: `Passed: 3, Failed: 0`. Also run the existing `RecentSuggestionsSectionTests` to confirm the extraction didn't break anything.

- [ ] **Step 9: Commit**

```bash
git add TradyStrat.Application/AiSuggestion \
        TradyStrat.Application.Tests/AiSuggestion/UseCases
git commit -m "feat(ai-suggestion): QuerySuggestionsUseCase + extract ForwardReturnCalculator — Phase 3"
```

---

## Phase 4 — MCP DTOs

The DTOs are records with no behaviour. They get tested through the mapper tests (Phase 5) and the consolidated `JsonShapeTests` (extended as DTOs are added). This phase creates all six DTO files in one commit.

### Task 4.1: All six DTO files

**Files:**
- Create: `TradyStrat.Cli/Mcp/Dto/InstrumentDtos.cs`
- Create: `TradyStrat.Cli/Mcp/Dto/DashboardDtos.cs`
- Create: `TradyStrat.Cli/Mcp/Dto/SuggestionDtos.cs`
- Create: `TradyStrat.Cli/Mcp/Dto/PriceDtos.cs`
- Create: `TradyStrat.Cli/Mcp/Dto/PortfolioDtos.cs`
- Create: `TradyStrat.Cli/Mcp/Dto/ReplayDtos.cs`

- [ ] **Step 1: Create the folder and `InstrumentDtos.cs`**

```bash
mkdir -p TradyStrat.Cli/Mcp/Dto
```

Create `TradyStrat.Cli/Mcp/Dto/InstrumentDtos.cs`:

```csharp
namespace TradyStrat.Cli.Mcp.Dto;

public sealed record InstrumentListResponse(IReadOnlyList<InstrumentDto> Instruments);

public sealed record InstrumentDto(
    string Ticker,
    string DisplayName,
    string Currency,
    string Timezone,
    InstrumentRole Role);

public enum InstrumentRole { Focus, Context }
```

- [ ] **Step 2: Create `DashboardDtos.cs`**

```csharp
namespace TradyStrat.Cli.Mcp.Dto;

public sealed record DashboardSnapshot(
    string Ticker,
    DateOnly AsOfDate,
    MoneyDualCurrency LastClose,
    ZoneBlock Zone,
    IndicatorsBlock Indicators,
    SuggestionBrief? Suggestion,
    PositionBrief Position);

public sealed record MoneyDualCurrency(decimal Usd, decimal Eur, decimal FxRate);

public sealed record ZoneBlock(string Overall, ZoneByIndicator ByIndicator);
public sealed record ZoneByIndicator(string Bollinger, string Rsi, string Sma, string Ichimoku);

public sealed record IndicatorsBlock(
    BollingerBlock Bollinger,
    RsiBlock Rsi,
    SmaBlock Sma,
    IchimokuBlock Ichimoku);

public sealed record BollingerBlock(decimal? PercentB, decimal? Upper, decimal? Mid, decimal? Lower);
public sealed record RsiBlock(decimal? Value);
public sealed record SmaBlock(decimal? Sma20, decimal? Sma50, decimal? Sma200);
public sealed record IchimokuBlock(decimal? Tenkan, decimal? Kijun, decimal? SpanA, decimal? SpanB);

public sealed record SuggestionBrief(
    DateOnly Date, string Action, int Conviction,
    string Reasoning, string? EnvelopeHash, string? PromptVersionHash);

public sealed record PositionBrief(
    int Qty, decimal AvgCostUsd,
    decimal MarketValueUsd, decimal MarketValueEur,
    decimal UnrealizedPnlUsd, decimal UnrealizedPnlEur);
```

If the `Zone` enum in the project has known values, the `Overall` and per-indicator zone strings should match those enum names (e.g., `"Accumulate"`, `"Neutral"`). Confirm against `TradyStrat.Domain/Zone.cs` (or wherever the enum lives) before merging.

- [ ] **Step 3: Create `SuggestionDtos.cs`**

```csharp
namespace TradyStrat.Cli.Mcp.Dto;

public sealed record SuggestionPage(
    string Instrument, DateOnly From, DateOnly To, int Count,
    IReadOnlyList<SuggestionRow> Items);

public sealed record SuggestionRow(
    DateOnly Date, string Action, int Conviction,
    string? EnvelopeHash, string? PromptVersionHash,
    string Reasoning,
    decimal? ForwardReturnPct,
    bool? Correct);
```

- [ ] **Step 4: Create `PriceDtos.cs`**

```csharp
namespace TradyStrat.Cli.Mcp.Dto;

public sealed record PriceSeries(
    string Instrument, DateOnly From, DateOnly To, int BarCount,
    IReadOnlyList<BarDto> Bars,
    IndicatorArraysDto? Indicators);

public sealed record BarDto(
    DateOnly Date, decimal Open, decimal High, decimal Low, decimal Close, long Volume);

public sealed record IndicatorArraysDto(
    IReadOnlyList<decimal?> Rsi,
    IReadOnlyList<decimal?> Sma20,
    IReadOnlyList<decimal?> Sma50,
    IReadOnlyList<decimal?> Sma200,
    IReadOnlyList<decimal?> BbUpper,
    IReadOnlyList<decimal?> BbMid,
    IReadOnlyList<decimal?> BbLower,
    IReadOnlyList<decimal?> IchimokuTenkan,
    IReadOnlyList<decimal?> IchimokuKijun,
    IReadOnlyList<decimal?> IchimokuSpanA,
    IReadOnlyList<decimal?> IchimokuSpanB);
```

- [ ] **Step 5: Create `PortfolioDtos.cs`**

The MCP DTO is named `PortfolioSnapshotDto` (not `PortfolioSnapshot`) to avoid colliding with `TradyStrat.Application.Portfolio.PortfolioSnapshot`. Different name everywhere — no `using` aliases needed downstream.

```csharp
namespace TradyStrat.Cli.Mcp.Dto;

public sealed record PortfolioSnapshotDto(
    DateOnly AsOfDate, decimal FxRate,
    AggregateBlock Aggregate,
    IReadOnlyList<PositionRow> Positions,
    IReadOnlyList<TradeRow> Trades,
    bool TradesTruncated);

public sealed record AggregateBlock(
    decimal TotalValueEur, decimal GoalEur,
    decimal DistanceToGoalEur, decimal ProgressPct);

public sealed record PositionRow(
    string Ticker, int Qty, decimal AvgCostUsd,
    decimal MarketValueUsd, decimal MarketValueEur,
    decimal RealizedPnlUsd, decimal UnrealizedPnlUsd,
    decimal RealizedPnlEur, decimal UnrealizedPnlEur);

public sealed record TradeRow(DateOnly Date, string Ticker, string Side, int Qty, decimal PriceUsd);
```

- [ ] **Step 6: Create `ReplayDtos.cs`**

The MCP layer reuses the existing `ReplayReport` shape from `TradyStrat.Application.AiSuggestion.UseCases.ReplayReport` (spec §13.4 documented exception). Just expose a re-export type alias for namespace clarity:

```csharp
// TradyStrat.Cli/Mcp/Dto/ReplayDtos.cs
// The MCP layer reuses TradyStrat.Application.AiSuggestion.UseCases.ReplayReport
// directly. This file exists as a documentation anchor so future readers see
// "where do replay DTOs live?" answered with the explicit reuse note.
//
// If `ReplayReport` ever needs an MCP-specific shape, define it here and add a
// mapper. Until then, the tool method returns the Application type as-is.
```

(C# 12+ can't `using` re-export records, so we just leave the documentation file. Tool methods import `TradyStrat.Application.AiSuggestion.UseCases.ReplayReport` directly.)

- [ ] **Step 7: Build**

Run: `dotnet build TradyStrat.Cli`
Expected: build succeeds.

- [ ] **Step 8: Commit**

```bash
git add TradyStrat.Cli/Mcp/Dto
git commit -m "feat(mcp): wire DTOs for all six tools — Phase 4"
```

---

## Phase 5 — Mapper Strategies

Each mapper is a pure projection helper. One per feature. Mappers are tested both directly (per-mapper test files) and indirectly through the consolidated `JsonShapeTests` (which exercises each DTO's wire shape end-to-end).

### Task 5.1: `InstrumentMapper` (TDD)

**Files:**
- Create: `TradyStrat.Cli/Mcp/Mapping/InstrumentMapper.cs`
- Test: `TradyStrat.Cli.Tests/Mcp/Mapping/InstrumentMapperTests.cs`

- [ ] **Step 1: Write the failing test**

```bash
mkdir -p TradyStrat.Cli/Mcp/Mapping TradyStrat.Cli.Tests/Mcp/Mapping
```

Create `TradyStrat.Cli.Tests/Mcp/Mapping/InstrumentMapperTests.cs`:

```csharp
using Shouldly;
using TradyStrat.Cli.Mcp.Dto;
using TradyStrat.Cli.Mcp.Mapping;
using TradyStrat.Domain;
using Xunit;

namespace TradyStrat.Cli.Tests.Mcp.Mapping;

public class InstrumentMapperTests
{
    [Fact]
    public void Maps_focus_ticker_with_role_Focus()
    {
        var inst = new Instrument(/* CON3.L fixture */);
        var dto = InstrumentMapper.ToDto(inst, focusTicker: "CON3.L");
        dto.Ticker.ShouldBe("CON3.L");
        dto.Role.ShouldBe(InstrumentRole.Focus);
    }

    [Fact]
    public void Maps_non_focus_ticker_with_role_Context()
    {
        var inst = new Instrument(/* COIN fixture */);
        var dto = InstrumentMapper.ToDto(inst, focusTicker: "CON3.L");
        dto.Role.ShouldBe(InstrumentRole.Context);
    }

    [Fact]
    public void ToResponse_wraps_list()
    {
        var insts = new[] {
            new Instrument(/* CON3.L */),
            new Instrument(/* COIN */),
            new Instrument(/* BTC-USD */),
        };
        var resp = InstrumentMapper.ToResponse(insts, focusTicker: "CON3.L");
        resp.Instruments.Count.ShouldBe(3);
        resp.Instruments.Count(i => i.Role == InstrumentRole.Focus).ShouldBe(1);
    }
}
```

- [ ] **Step 2: Run to verify failure**

Run: `dotnet test TradyStrat.Cli.Tests --filter FullyQualifiedName~InstrumentMapperTests`
Expected: compile error.

- [ ] **Step 3: Implement the mapper**

Create `TradyStrat.Cli/Mcp/Mapping/InstrumentMapper.cs`:

```csharp
using TradyStrat.Cli.Mcp.Dto;
using TradyStrat.Domain;

namespace TradyStrat.Cli.Mcp.Mapping;

internal static class InstrumentMapper
{
    public static InstrumentDto ToDto(Instrument inst, string focusTicker)
        => new(
            Ticker: inst.Ticker,
            DisplayName: inst.DisplayName,
            Currency: inst.Currency,
            Timezone: inst.Timezone,
            Role: inst.Ticker == focusTicker ? InstrumentRole.Focus : InstrumentRole.Context);

    public static InstrumentListResponse ToResponse(IEnumerable<Instrument> instruments, string focusTicker)
        => new(instruments.Select(i => ToDto(i, focusTicker)).ToList());
}
```

Match the actual property names of `Instrument` in `TradyStrat.Domain/Instrument.cs`. If `DisplayName` is called `Name` or similar, use that.

- [ ] **Step 4: Verify**

Run: `dotnet test TradyStrat.Cli.Tests --filter FullyQualifiedName~InstrumentMapperTests`
Expected: `Passed: 3, Failed: 0`.

- [ ] **Step 5: Commit**

```bash
git add TradyStrat.Cli/Mcp/Mapping/InstrumentMapper.cs \
        TradyStrat.Cli.Tests/Mcp/Mapping/InstrumentMapperTests.cs
git commit -m "feat(mcp): InstrumentMapper — Phase 5"
```

### Task 5.2: `DashboardMapper` (TDD)

**Files:**
- Create: `TradyStrat.Cli/Mcp/Mapping/DashboardMapper.cs`
- Test: `TradyStrat.Cli.Tests/Mcp/Mapping/DashboardMapperTests.cs`

- [ ] **Step 1: Write the failing test**

Create `TradyStrat.Cli.Tests/Mcp/Mapping/DashboardMapperTests.cs`. The test should construct a representative `LoadDashboardOutput` (read its actual shape from `TradyStrat.Application/Dashboard/UseCases/LoadDashboardOutput.cs`), then assert:
- `Ticker`, `AsOfDate`, `LastClose.{Usd,Eur,FxRate}` populated.
- `Zone.Overall` and `Zone.ByIndicator.*` populated.
- All four indicator sub-blocks populated.
- `Suggestion` is `null` when the dashboard has no suggestion; populated otherwise; hashes truncated to 8 chars.
- `Position` reflects the position values.

Skeleton:

```csharp
public class DashboardMapperTests
{
    [Fact] public void Maps_full_dashboard_with_suggestion() { /* ... */ }
    [Fact] public void Suggestion_is_null_when_dashboard_has_no_suggestion() { /* ... */ }
    [Fact] public void Hashes_truncated_to_8_chars() { /* ... */ }
}
```

- [ ] **Step 2: Verify failure**

Run: `dotnet test TradyStrat.Cli.Tests --filter FullyQualifiedName~DashboardMapperTests`
Expected: compile error.

- [ ] **Step 3: Implement the mapper**

Create `TradyStrat.Cli/Mcp/Mapping/DashboardMapper.cs`:

```csharp
using TradyStrat.Application.Dashboard.UseCases;
using TradyStrat.Cli.Mcp.Dto;

namespace TradyStrat.Cli.Mcp.Mapping;

internal static class DashboardMapper
{
    public static DashboardSnapshot ToSnapshot(LoadDashboardOutput src)
        => new(
            Ticker: src.Instrument.Ticker,
            AsOfDate: src.AsOfDate,
            LastClose: new MoneyDualCurrency(src.LastCloseUsd, src.LastCloseEur, src.FxRate),
            Zone: new ZoneBlock(
                Overall: src.OverallZone.ToString(),
                ByIndicator: new ZoneByIndicator(
                    Bollinger: src.BollingerZone.ToString(),
                    Rsi: src.RsiZone.ToString(),
                    Sma: src.SmaZone.ToString(),
                    Ichimoku: src.IchimokuZone.ToString())),
            Indicators: new IndicatorsBlock(
                Bollinger: new BollingerBlock(src.BollingerPercentB, src.BollingerUpper, src.BollingerMid, src.BollingerLower),
                Rsi: new RsiBlock(src.Rsi),
                Sma: new SmaBlock(src.Sma20, src.Sma50, src.Sma200),
                Ichimoku: new IchimokuBlock(src.IchimokuTenkan, src.IchimokuKijun, src.IchimokuSpanA, src.IchimokuSpanB)),
            Suggestion: src.Suggestion is null ? null : new SuggestionBrief(
                Date: src.Suggestion.ForDate,
                Action: src.Suggestion.Action.ToString(),
                Conviction: src.Suggestion.Conviction,
                Reasoning: src.Suggestion.Rationale,
                EnvelopeHash: TruncateHash(src.Suggestion.EnvelopeHash),
                PromptVersionHash: TruncateHash(src.Suggestion.PromptVersionHash)),
            Position: new PositionBrief(
                Qty: src.PositionQty,
                AvgCostUsd: src.PositionAvgCostUsd,
                MarketValueUsd: src.PositionMarketValueUsd,
                MarketValueEur: src.PositionMarketValueEur,
                UnrealizedPnlUsd: src.PositionUnrealizedPnlUsd,
                UnrealizedPnlEur: src.PositionUnrealizedPnlEur));

    private static string? TruncateHash(string? hash)
        => hash is null ? null : (hash.Length >= 8 ? hash[..8] : hash);
}
```

**Adapt the property names** to the actual `LoadDashboardOutput` shape — read that file first. If the source uses different nesting (e.g., `src.Position.MarketValueUsd` instead of flat properties), restructure accordingly.

- [ ] **Step 4: Verify**

Run: `dotnet test TradyStrat.Cli.Tests --filter FullyQualifiedName~DashboardMapperTests`
Expected: `Passed: 3, Failed: 0`.

- [ ] **Step 5: Commit**

```bash
git add TradyStrat.Cli/Mcp/Mapping/DashboardMapper.cs \
        TradyStrat.Cli.Tests/Mcp/Mapping/DashboardMapperTests.cs
git commit -m "feat(mcp): DashboardMapper — Phase 5"
```

### Task 5.3: `SuggestionMapper` (TDD)

**Files:**
- Create: `TradyStrat.Cli/Mcp/Mapping/SuggestionMapper.cs`
- Test: `TradyStrat.Cli.Tests/Mcp/Mapping/SuggestionMapperTests.cs`

- [ ] **Step 1: Write the test**

Create `TradyStrat.Cli.Tests/Mcp/Mapping/SuggestionMapperTests.cs`:

```csharp
using Shouldly;
using TradyStrat.Application.AiSuggestion.UseCases;
using TradyStrat.Cli.Mcp.Mapping;
using TradyStrat.Domain;
using Xunit;

namespace TradyStrat.Cli.Tests.Mcp.Mapping;

public class SuggestionMapperTests
{
    [Fact]
    public void Maps_query_output_to_page_newest_first()
    {
        var output = new QuerySuggestionsOutput(new[]
        {
            new QueriedSuggestion(new DateOnly(2026, 5, 17), SuggestionAction.Acquire, 7,
                "reason", "abcdef1234", "0123456789", null, null),
            new QueriedSuggestion(new DateOnly(2026, 5, 10), SuggestionAction.Hold, 5,
                "reason2", "9876543210", "fedcba9876", 0.012m, true),
        });
        var page = SuggestionMapper.ToPage(
            output, instrument: "CON3.L",
            from: new DateOnly(2026, 5, 10), to: new DateOnly(2026, 5, 17));

        page.Instrument.ShouldBe("CON3.L");
        page.Count.ShouldBe(2);
        page.Items[0].Date.ShouldBe(new DateOnly(2026, 5, 17));
        page.Items[0].EnvelopeHash.ShouldBe("abcdef12");
        page.Items[1].ForwardReturnPct.ShouldBe(0.012m);
        page.Items[1].Correct.ShouldBe(true);
    }

    [Fact]
    public void Null_hashes_stay_null()
    {
        var output = new QuerySuggestionsOutput(new[]
        {
            new QueriedSuggestion(new DateOnly(2026, 5, 17), SuggestionAction.Acquire, 7,
                "reason", null, null, null, null),
        });
        var page = SuggestionMapper.ToPage(output, "CON3.L",
            new DateOnly(2026, 5, 17), new DateOnly(2026, 5, 17));
        page.Items[0].EnvelopeHash.ShouldBeNull();
        page.Items[0].PromptVersionHash.ShouldBeNull();
    }
}
```

- [ ] **Step 2: Verify failure**

Run: `dotnet test TradyStrat.Cli.Tests --filter FullyQualifiedName~SuggestionMapperTests`
Expected: compile error.

- [ ] **Step 3: Implement the mapper**

Create `TradyStrat.Cli/Mcp/Mapping/SuggestionMapper.cs`:

```csharp
using TradyStrat.Application.AiSuggestion.UseCases;
using TradyStrat.Cli.Mcp.Dto;

namespace TradyStrat.Cli.Mcp.Mapping;

internal static class SuggestionMapper
{
    public static SuggestionPage ToPage(QuerySuggestionsOutput src, string instrument, DateOnly from, DateOnly to)
        => new(
            Instrument: instrument,
            From: from,
            To: to,
            Count: src.Items.Count,
            Items: src.Items.Select(ToRow).ToList());

    private static SuggestionRow ToRow(QueriedSuggestion s)
        => new(
            Date: s.Date,
            Action: s.Action.ToString(),
            Conviction: s.Conviction,
            EnvelopeHash: Truncate(s.EnvelopeHash),
            PromptVersionHash: Truncate(s.PromptVersionHash),
            Reasoning: s.Reasoning,
            ForwardReturnPct: s.ForwardReturnPct,
            Correct: s.Correct);

    private static string? Truncate(string? h) => h is null ? null : (h.Length >= 8 ? h[..8] : h);
}
```

- [ ] **Step 4: Verify**

Run: `dotnet test TradyStrat.Cli.Tests --filter FullyQualifiedName~SuggestionMapperTests`
Expected: `Passed: 2, Failed: 0`.

- [ ] **Step 5: Commit**

```bash
git add TradyStrat.Cli/Mcp/Mapping/SuggestionMapper.cs \
        TradyStrat.Cli.Tests/Mcp/Mapping/SuggestionMapperTests.cs
git commit -m "feat(mcp): SuggestionMapper — Phase 5"
```

### Task 5.4: `PriceMapper` (TDD)

**Files:**
- Create: `TradyStrat.Cli/Mcp/Mapping/PriceMapper.cs`
- Test: `TradyStrat.Cli.Tests/Mcp/Mapping/PriceMapperTests.cs`

- [ ] **Step 1: Test**

Create `TradyStrat.Cli.Tests/Mcp/Mapping/PriceMapperTests.cs`:

```csharp
using Shouldly;
using TradyStrat.Application.PriceFeed.UseCases;
using TradyStrat.Cli.Mcp.Mapping;
using TradyStrat.Domain;
using Xunit;

namespace TradyStrat.Cli.Tests.Mcp.Mapping;

public class PriceMapperTests
{
    [Fact]
    public void Maps_bars_without_indicators()
    {
        var bars = new[]
        {
            new PriceBar(1, new DateOnly(2026, 5, 16), 10m, 11m, 9m, 10.5m, 100),
            new PriceBar(1, new DateOnly(2026, 5, 17), 10.5m, 12m, 10m, 11.8m, 110),
        };
        var output = new GetPriceSeriesOutput(bars, Indicators: null);
        var dto = PriceMapper.ToSeries(output, "CON3.L");

        dto.Instrument.ShouldBe("CON3.L");
        dto.BarCount.ShouldBe(2);
        dto.Bars[0].Date.ShouldBe(new DateOnly(2026, 5, 16));
        dto.Indicators.ShouldBeNull();
    }

    [Fact]
    public void Maps_indicator_arrays_aligned_to_bars()
    {
        var bars = new[]
        {
            new PriceBar(1, new DateOnly(2026, 5, 16), 10m, 11m, 9m, 10.5m, 100),
            new PriceBar(1, new DateOnly(2026, 5, 17), 10.5m, 12m, 10m, 11.8m, 110),
        };
        var indicators = new IndicatorArrays(
            Rsi:    new decimal?[] { null, 48.2m },
            Sma20:  new decimal?[] { null, 11m },
            Sma50:  new decimal?[] { null, null },
            Sma200: new decimal?[] { null, null },
            BbUpper: new decimal?[] { null, 12m },
            BbMid:   new decimal?[] { null, 11m },
            BbLower: new decimal?[] { null, 10m },
            IchimokuTenkan: new decimal?[] { null, null },
            IchimokuKijun:  new decimal?[] { null, null },
            IchimokuSpanA:  new decimal?[] { null, null },
            IchimokuSpanB:  new decimal?[] { null, null });
        var output = new GetPriceSeriesOutput(bars, indicators);
        var dto = PriceMapper.ToSeries(output, "CON3.L");

        dto.Indicators.ShouldNotBeNull();
        dto.Indicators!.Rsi.Count.ShouldBe(2);
        dto.Indicators.Rsi[1].ShouldBe(48.2m);
    }
}
```

- [ ] **Step 2: Verify failure**

Run: `dotnet test TradyStrat.Cli.Tests --filter FullyQualifiedName~PriceMapperTests`
Expected: compile error.

- [ ] **Step 3: Implement**

Create `TradyStrat.Cli/Mcp/Mapping/PriceMapper.cs`:

```csharp
using TradyStrat.Application.PriceFeed.UseCases;
using TradyStrat.Cli.Mcp.Dto;

namespace TradyStrat.Cli.Mcp.Mapping;

internal static class PriceMapper
{
    public static PriceSeries ToSeries(GetPriceSeriesOutput src, string ticker)
    {
        var bars = src.Bars
            .Select(b => new BarDto(b.Date, b.Open, b.High, b.Low, b.Close, b.Volume))
            .ToList();
        var from = bars.Count > 0 ? bars[0].Date : DateOnly.MinValue;
        var to   = bars.Count > 0 ? bars[^1].Date : DateOnly.MinValue;
        var indicators = src.Indicators is null ? null : new IndicatorArraysDto(
            Rsi: src.Indicators.Rsi,
            Sma20: src.Indicators.Sma20,
            Sma50: src.Indicators.Sma50,
            Sma200: src.Indicators.Sma200,
            BbUpper: src.Indicators.BbUpper,
            BbMid: src.Indicators.BbMid,
            BbLower: src.Indicators.BbLower,
            IchimokuTenkan: src.Indicators.IchimokuTenkan,
            IchimokuKijun:  src.Indicators.IchimokuKijun,
            IchimokuSpanA:  src.Indicators.IchimokuSpanA,
            IchimokuSpanB:  src.Indicators.IchimokuSpanB);
        return new PriceSeries(ticker, from, to, bars.Count, bars, indicators);
    }
}
```

- [ ] **Step 4: Verify**

Run: `dotnet test TradyStrat.Cli.Tests --filter FullyQualifiedName~PriceMapperTests`
Expected: `Passed: 2, Failed: 0`.

- [ ] **Step 5: Commit**

```bash
git add TradyStrat.Cli/Mcp/Mapping/PriceMapper.cs \
        TradyStrat.Cli.Tests/Mcp/Mapping/PriceMapperTests.cs
git commit -m "feat(mcp): PriceMapper — Phase 5"
```

### Task 5.5: `PortfolioMapper` (TDD)

**Files:**
- Create: `TradyStrat.Cli/Mcp/Mapping/PortfolioMapper.cs`
- Test: `TradyStrat.Cli.Tests/Mcp/Mapping/PortfolioMapperTests.cs`

- [ ] **Step 1: Test**

Create `TradyStrat.Cli.Tests/Mcp/Mapping/PortfolioMapperTests.cs`. Cover:
- Maps `PortfolioSnapshot` (from `PortfolioService.SnapshotAsync`) to the DTO, including aggregate.
- Caps trades at 500, newest-first; sets `TradesTruncated = true` when capped, false otherwise.

Skeleton:

```csharp
public class PortfolioMapperTests
{
    [Fact]
    public void Maps_aggregate_and_positions() { /* ... */ }

    [Fact]
    public void Caps_trades_at_500_newest_first_and_sets_truncated_flag()
    {
        var trades = Enumerable.Range(1, 600)
            .Select(i => new Trade(/* date: epoch + i days, etc. */))
            .ToList();
        // ... build PortfolioService output that includes those trades ...
        var dto = PortfolioMapper.ToSnapshot(/* ... */);
        dto.Trades.Count.ShouldBe(500);
        dto.TradesTruncated.ShouldBeTrue();
        dto.Trades[0].Date.ShouldBeGreaterThan(dto.Trades[^1].Date); // newest first
    }

    [Fact]
    public void Empty_ledger_truncated_flag_false() { /* ... */ }
}
```

- [ ] **Step 2: Verify failure**

Run: `dotnet test TradyStrat.Cli.Tests --filter FullyQualifiedName~PortfolioMapperTests`
Expected: compile error.

- [ ] **Step 3: Implement**

Create `TradyStrat.Cli/Mcp/Mapping/PortfolioMapper.cs`:

```csharp
using TradyStrat.Application.Portfolio;
using TradyStrat.Cli.Mcp.Dto;
using TradyStrat.Domain;

namespace TradyStrat.Cli.Mcp.Mapping;

internal static class PortfolioMapper
{
    private const int TradeCap = 500;

    public static PortfolioSnapshotDto ToSnapshot(
        PortfolioSnapshot src,                  // Application's PortfolioService snapshot record
        IReadOnlyList<Trade> ledger)
    {
        var ordered = ledger.OrderByDescending(t => t.Date).ToList();
        var truncated = ordered.Count > TradeCap;
        var trades = (truncated ? ordered.Take(TradeCap) : ordered)
            .Select(t => new TradeRow(
                Date: t.Date,
                Ticker: t.Instrument.Ticker,
                Side: t.Side.ToString(),
                Qty: t.Qty,
                PriceUsd: t.PriceUsd))
            .ToList();
        // ... map src.Aggregate, src.Positions ...
        return new PortfolioSnapshotDto(/* ... */);
    }
}
```

The MCP DTO is named `PortfolioSnapshotDto` (per Task 4.1 Step 5) to avoid colliding with the Application-layer `PortfolioSnapshot` record. Both types are visible in this file via their respective namespaces.

- [ ] **Step 4: Verify**

Run: `dotnet test TradyStrat.Cli.Tests --filter FullyQualifiedName~PortfolioMapperTests`
Expected: `Passed: 3, Failed: 0`.

- [ ] **Step 5: Commit**

```bash
git add TradyStrat.Cli/Mcp/Mapping/PortfolioMapper.cs \
        TradyStrat.Cli.Tests/Mcp/Mapping/PortfolioMapperTests.cs \
        TradyStrat.Cli/Mcp/Dto/PortfolioDtos.cs
git commit -m "feat(mcp): PortfolioMapper with 500-trade cap — Phase 5"
```

---

## Phase 6 — Filter pipeline (Decorator)

### Task 6.1: `McpExceptionTranslationFilter` (TDD, innermost)

**Files:**
- Create: `TradyStrat.Cli/Mcp/Filters/McpExceptionTranslationFilter.cs`
- Test: `TradyStrat.Cli.Tests/Mcp/Filters/McpExceptionTranslationFilterTests.cs`

- [ ] **Step 1: Inspect the SDK filter signature**

Run: `dotnet build TradyStrat.Cli` (which transitively brings the MCP package). Then:

```bash
dotnet sln --help >/dev/null 2>&1 && echo "tip: read the package contents at:" && \
  find ~/.nuget/packages/modelcontextprotocol -name "*.xml" -path "*lib/net*" 2>/dev/null | head -5
```

Look for `IMcpRequestFilterBuilder` and `AddCallToolFilter` extension method to confirm the precise filter delegate type (something like `Func<Func<RequestContext<CallToolRequestParams>, CancellationToken, ValueTask<CallToolResult>>, Func<RequestContext<CallToolRequestParams>, CancellationToken, ValueTask<CallToolResult>>>`).

If the exact type names differ, adjust the snippets below accordingly. The shape is always: a function that takes `next` and returns a wrapped function.

- [ ] **Step 2: Write the failing tests**

```bash
mkdir -p TradyStrat.Cli/Mcp/Filters TradyStrat.Cli.Tests/Mcp/Filters
```

Create `TradyStrat.Cli.Tests/Mcp/Filters/McpExceptionTranslationFilterTests.cs`:

```csharp
using ModelContextProtocol;                        // McpException
using ModelContextProtocol.Protocol;               // CallToolResult, CallToolRequestParams
using ModelContextProtocol.Server;                 // RequestContext, etc.
using Shouldly;
using TradyStrat.Cli.Mcp.Filters;
using TradyStrat.Domain.Exceptions;
using Xunit;

namespace TradyStrat.Cli.Tests.Mcp.Filters;

public class McpExceptionTranslationFilterTests
{
    private static RequestContext<CallToolRequestParams> MakeContext()
        => /* minimal stub — exact constructor depends on the SDK version */
           throw new NotImplementedException("Fill in with the SDK's actual RequestContext factory");

    [Fact]
    public async Task TradyStratException_becomes_McpException_with_same_message()
    {
        var ctx = MakeContext();
        var wrapped = McpExceptionTranslationFilter.Handle(
            (c, ct) => throw new InstrumentNotFoundException("BAD"));
        var ex = await Should.ThrowAsync<McpException>(() => wrapped(ctx, default).AsTask());
        ex.Message.ShouldContain("BAD");
    }

    [Fact]
    public async Task ArgumentException_becomes_McpException()
    {
        var ctx = MakeContext();
        var wrapped = McpExceptionTranslationFilter.Handle(
            (c, ct) => throw new ArgumentException("limit must be between 1 and 100"));
        var ex = await Should.ThrowAsync<McpException>(() => wrapped(ctx, default).AsTask());
        ex.Message.ShouldContain("limit must be");
    }

    [Fact]
    public async Task OperationCanceledException_propagates_unchanged()
    {
        var ctx = MakeContext();
        var wrapped = McpExceptionTranslationFilter.Handle(
            (c, ct) => throw new OperationCanceledException());
        await Should.ThrowAsync<OperationCanceledException>(() => wrapped(ctx, default).AsTask());
    }

    [Fact]
    public async Task Arbitrary_exception_propagates_unchanged()
    {
        var ctx = MakeContext();
        var wrapped = McpExceptionTranslationFilter.Handle(
            (c, ct) => throw new InvalidOperationException("boom"));
        await Should.ThrowAsync<InvalidOperationException>(() => wrapped(ctx, default).AsTask());
    }
}
```

**Implementer note:** the `MakeContext()` helper needs the actual SDK type. Read the SDK source to find a public way to construct a `RequestContext<CallToolRequestParams>` (or whatever the precise type is). If construction requires an `IMcpServer`, build a minimal mock with a single method. If it's truly internal, switch the test approach to call the filter via an in-memory MCP server harness (see Task 6.4 alternative below).

- [ ] **Step 3: Run to verify failure**

Run: `dotnet test TradyStrat.Cli.Tests --filter FullyQualifiedName~McpExceptionTranslationFilterTests`
Expected: compile error.

- [ ] **Step 4: Implement**

Create `TradyStrat.Cli/Mcp/Filters/McpExceptionTranslationFilter.cs`:

```csharp
using ModelContextProtocol;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;
using TradyStrat.Domain.Exceptions;

namespace TradyStrat.Cli.Mcp.Filters;

internal static class McpExceptionTranslationFilter
{
    public static Func<RequestContext<CallToolRequestParams>, CancellationToken, ValueTask<CallToolResult>> Handle(
        Func<RequestContext<CallToolRequestParams>, CancellationToken, ValueTask<CallToolResult>> next)
        => async (context, ct) =>
        {
            try
            {
                return await next(context, ct);
            }
            catch (McpException)
            {
                throw;  // already an MCP error
            }
            catch (OperationCanceledException)
            {
                throw;  // caller cancelled — propagate
            }
            catch (TradyStratException ex)
            {
                throw new McpException(ex.Message);
            }
            catch (ArgumentException ex)
            {
                throw new McpException(ex.Message);
            }
        };
}
```

(Match the exact type signature the SDK exports. If `RequestContext<T>` lives in a different namespace, adjust `using`s.)

- [ ] **Step 5: Verify**

Run: `dotnet test TradyStrat.Cli.Tests --filter FullyQualifiedName~McpExceptionTranslationFilterTests`
Expected: `Passed: 4, Failed: 0`.

- [ ] **Step 6: Commit**

```bash
git add TradyStrat.Cli/Mcp/Filters/McpExceptionTranslationFilter.cs \
        TradyStrat.Cli.Tests/Mcp/Filters/McpExceptionTranslationFilterTests.cs
git commit -m "feat(mcp): exception-translation filter — Phase 6"
```

### Task 6.2: `McpTimeoutFilter` (TDD)

**Files:**
- Create: `TradyStrat.Cli/Mcp/Filters/McpTimeoutFilter.cs`
- Test: `TradyStrat.Cli.Tests/Mcp/Filters/McpTimeoutFilterTests.cs`

- [ ] **Step 1: Test**

Create `TradyStrat.Cli.Tests/Mcp/Filters/McpTimeoutFilterTests.cs`. Cover:
- A `next` that completes within budget → passes through.
- A `next` that sleeps longer than the budget → throws `McpException` containing `"timeout"` (or the chosen wording).
- A `next` whose cancellation token is observed mid-call → completes early without throwing timeout error.

Skeleton:

```csharp
public class McpTimeoutFilterTests
{
    [Fact]
    public async Task Fast_next_passes_through() { /* ... */ }

    [Fact]
    public async Task Slow_next_throws_McpException_with_timeout_message()
    {
        // Use a TestableTimeoutFilter that takes the budget as a param to keep
        // the test fast (e.g., 100ms). See implementation step.
    }

    [Fact]
    public async Task Caller_cancellation_propagates_OperationCanceled_not_timeout() { /* ... */ }
}
```

- [ ] **Step 2: Verify failure**

Run: `dotnet test TradyStrat.Cli.Tests --filter FullyQualifiedName~McpTimeoutFilterTests`
Expected: compile error.

- [ ] **Step 3: Implement**

Create `TradyStrat.Cli/Mcp/Filters/McpTimeoutFilter.cs`:

```csharp
using ModelContextProtocol;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;

namespace TradyStrat.Cli.Mcp.Filters;

internal static class McpTimeoutFilter
{
    internal static TimeSpan Budget { get; set; } = TimeSpan.FromSeconds(30);  // internal setter for tests

    public static Func<RequestContext<CallToolRequestParams>, CancellationToken, ValueTask<CallToolResult>> Handle(
        Func<RequestContext<CallToolRequestParams>, CancellationToken, ValueTask<CallToolResult>> next)
        => async (context, ct) =>
        {
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            cts.CancelAfter(Budget);
            try
            {
                return await next(context, cts.Token);
            }
            catch (OperationCanceledException) when (cts.IsCancellationRequested && !ct.IsCancellationRequested)
            {
                throw new McpException($"Tool call exceeded {Budget.TotalSeconds:F0}s timeout.");
            }
        };
}
```

The `internal set` on `Budget` is a small concession to test ergonomics — flips it to e.g. 100ms in tests. Acceptable because tests live in the same solution and `InternalsVisibleTo` can be added if needed.

- [ ] **Step 4: Add InternalsVisibleTo**

Edit `TradyStrat.Cli/TradyStrat.Cli.csproj`. Add to the existing `<PropertyGroup>` or a new one:

```xml
  <ItemGroup>
    <InternalsVisibleTo Include="TradyStrat.Cli.Tests" />
  </ItemGroup>
```

- [ ] **Step 5: Verify**

Run: `dotnet test TradyStrat.Cli.Tests --filter FullyQualifiedName~McpTimeoutFilterTests`
Expected: `Passed: 3, Failed: 0`.

- [ ] **Step 6: Commit**

```bash
git add TradyStrat.Cli/Mcp/Filters/McpTimeoutFilter.cs \
        TradyStrat.Cli.Tests/Mcp/Filters/McpTimeoutFilterTests.cs \
        TradyStrat.Cli/TradyStrat.Cli.csproj
git commit -m "feat(mcp): timeout filter (30s default) — Phase 6"
```

### Task 6.3: `McpLoggingFilter` (TDD)

**Files:**
- Create: `TradyStrat.Cli/Mcp/Filters/McpLoggingFilter.cs`
- Test: `TradyStrat.Cli.Tests/Mcp/Filters/McpLoggingFilterTests.cs`

- [ ] **Step 1: Test**

Create `TradyStrat.Cli.Tests/Mcp/Filters/McpLoggingFilterTests.cs`. Use Microsoft's `FakeLogger` (`Microsoft.Extensions.Diagnostics.Testing` package) or a simple in-memory `ILogger<>` capture (write a small `RecordingLogger` if the project doesn't already have one). Cover:
- Successful call → exactly one `Information` log record with `outcome=ok` and a non-zero duration.
- `McpException` raised by `next` → one `Warning` record with `outcome=mcp_error`.
- Unhandled exception → one `Error` record with `outcome=unexpected`.
- `OperationCanceledException` → one `Information` record with `outcome=cancelled` (cancellation is normal).

- [ ] **Step 2: Verify failure**

Run: `dotnet test TradyStrat.Cli.Tests --filter FullyQualifiedName~McpLoggingFilterTests`
Expected: compile error.

- [ ] **Step 3: Implement**

Create `TradyStrat.Cli/Mcp/Filters/McpLoggingFilter.cs`:

```csharp
using System.Diagnostics;
using Microsoft.Extensions.Logging;
using ModelContextProtocol;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;

namespace TradyStrat.Cli.Mcp.Filters;

internal static class McpLoggingFilter
{
    public static Func<RequestContext<CallToolRequestParams>, CancellationToken, ValueTask<CallToolResult>> Handle(
        Func<RequestContext<CallToolRequestParams>, CancellationToken, ValueTask<CallToolResult>> next)
        => async (context, ct) =>
        {
            var logger = context.Services?.GetService(typeof(ILogger<McpLoggingFilterMarker>)) as ILogger
                ?? throw new InvalidOperationException("ILogger not available in MCP request context.");
            var toolName = context.Params?.Name ?? "(unknown)";
            var sw = Stopwatch.StartNew();
            try
            {
                var result = await next(context, ct);
                sw.Stop();
                logger.LogInformation("MCP tool {Tool} ok in {Ms}ms", toolName, sw.ElapsedMilliseconds);
                return result;
            }
            catch (OperationCanceledException)
            {
                sw.Stop();
                logger.LogInformation("MCP tool {Tool} cancelled after {Ms}ms", toolName, sw.ElapsedMilliseconds);
                throw;
            }
            catch (McpException ex)
            {
                sw.Stop();
                logger.LogWarning(ex, "MCP tool {Tool} mcp_error after {Ms}ms", toolName, sw.ElapsedMilliseconds);
                throw;
            }
            catch (Exception ex)
            {
                sw.Stop();
                logger.LogError(ex, "MCP tool {Tool} unexpected after {Ms}ms", toolName, sw.ElapsedMilliseconds);
                throw;
            }
        };
}

internal sealed class McpLoggingFilterMarker;  // category type for ILogger<>
```

- [ ] **Step 4: Verify**

Run: `dotnet test TradyStrat.Cli.Tests --filter FullyQualifiedName~McpLoggingFilterTests`
Expected: `Passed: 4, Failed: 0`.

- [ ] **Step 5: Commit**

```bash
git add TradyStrat.Cli/Mcp/Filters/McpLoggingFilter.cs \
        TradyStrat.Cli.Tests/Mcp/Filters/McpLoggingFilterTests.cs
git commit -m "feat(mcp): logging filter with structured per-call record — Phase 6"
```

---

## Phase 7 — Guards helper

### Task 7.1: `Guards` (TDD)

**Files:**
- Create: `TradyStrat.Cli/Mcp/Tools/Guards.cs`
- Test: `TradyStrat.Cli.Tests/Mcp/Tools/GuardsTests.cs`

- [ ] **Step 1: Test**

```bash
mkdir -p TradyStrat.Cli/Mcp/Tools TradyStrat.Cli.Tests/Mcp/Tools
```

Create `TradyStrat.Cli.Tests/Mcp/Tools/GuardsTests.cs`:

```csharp
using Shouldly;
using TradyStrat.Application.Settings.UseCases;
using TradyStrat.Cli.Mcp.Tools;
using TradyStrat.Domain;
using TradyStrat.TestKit.Specifications;
using Xunit;

namespace TradyStrat.Cli.Tests.Mcp.Tools;

public class GuardsTests
{
    [Fact]
    public async Task ResolveInstrumentOrThrow_known_ticker_returns_instrument()
    {
        // Compose minimal host with seeded instruments; resolve Guards
        // ...
        var inst = await guards.ResolveInstrumentOrThrow("CON3.L", default);
        inst.Ticker.ShouldBe("CON3.L");
    }

    [Fact]
    public async Task ResolveInstrumentOrThrow_unknown_ticker_throws_with_actionable_message()
    {
        var act = () => guards.ResolveInstrumentOrThrow("XYZ", default);
        var ex = await Should.ThrowAsync<ArgumentException>(act);
        ex.Message.ShouldContain("XYZ");
        ex.Message.ShouldContain("list_instruments");
    }

    [Fact]
    public void ResolveDateRange_uses_defaults_when_both_null()
    {
        var today = new DateOnly(2026, 5, 18);
        var (f, t) = Guards.ResolveDateRange(from: null, to: null, defaultBack: 90, clockToday: today);
        t.ShouldBe(today);
        f.ShouldBe(today.AddDays(-90));
    }

    [Fact]
    public void ResolveDateRange_parses_iso_strings()
    {
        var today = new DateOnly(2026, 5, 18);
        var (f, t) = Guards.ResolveDateRange(from: "2026-05-01", to: "2026-05-10", defaultBack: 90, clockToday: today);
        f.ShouldBe(new DateOnly(2026, 5, 1));
        t.ShouldBe(new DateOnly(2026, 5, 10));
    }

    [Fact]
    public void ResolveDateRange_throws_on_inverted_range()
    {
        var today = new DateOnly(2026, 5, 18);
        var act = () => Guards.ResolveDateRange(from: "2026-05-10", to: "2026-05-01", 90, today);
        var ex = Should.Throw<ArgumentException>(act);
        ex.Message.ShouldContain("from");
    }

    [Fact]
    public void ResolveDateRange_throws_on_bad_format()
    {
        var today = new DateOnly(2026, 5, 18);
        var act = () => Guards.ResolveDateRange(from: "tomorrow", to: null, 90, today);
        var ex = Should.Throw<ArgumentException>(act);
        ex.Message.ShouldContain("tomorrow");
        ex.Message.ShouldContain("YYYY-MM-DD");
    }
}
```

- [ ] **Step 2: Verify failure**

Run: `dotnet test TradyStrat.Cli.Tests --filter FullyQualifiedName~GuardsTests`
Expected: compile error.

- [ ] **Step 3: Implement**

Create `TradyStrat.Cli/Mcp/Tools/Guards.cs`:

```csharp
using System.Globalization;
using TradyStrat.Application.Settings.UseCases;
using TradyStrat.Domain;

namespace TradyStrat.Cli.Mcp.Tools;

internal sealed class Guards(ListInstrumentsUseCase listInstruments)
{
    private IReadOnlyList<Instrument>? _cached;

    public async Task<Instrument> ResolveInstrumentOrThrow(string ticker, CancellationToken ct)
    {
        _cached ??= (await listInstruments.ExecuteAsync(Unit.Value, ct)).ToList();  // adjust to actual output type
        var match = _cached.FirstOrDefault(i => i.Ticker == ticker);
        if (match is null)
        {
            var known = string.Join(", ", _cached.Select(i => i.Ticker));
            throw new ArgumentException(
                $"Unknown instrument '{ticker}'. Known tickers: {known}. Call list_instruments to see valid tickers.");
        }
        return match;
    }

    public static (DateOnly from, DateOnly to) ResolveDateRange(
        string? from, string? to, int defaultBack, DateOnly clockToday)
    {
        var resolvedTo = ParseDate(to, "to") ?? clockToday;
        var resolvedFrom = ParseDate(from, "from") ?? resolvedTo.AddDays(-defaultBack);
        if (resolvedFrom > resolvedTo)
            throw new ArgumentException(
                $"from ({resolvedFrom:yyyy-MM-dd}) must be on or before to ({resolvedTo:yyyy-MM-dd}).");
        return (resolvedFrom, resolvedTo);
    }

    private static DateOnly? ParseDate(string? s, string field)
    {
        if (s is null) return null;
        if (DateOnly.TryParseExact(s, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var d))
            return d;
        throw new ArgumentException($"Invalid date '{s}' for {field} — use ISO YYYY-MM-DD.");
    }
}
```

If `ListInstrumentsUseCase`'s input shape is `Unit` vs `EmptyInput` vs something else, adapt to the real signature.

- [ ] **Step 4: Verify**

Run: `dotnet test TradyStrat.Cli.Tests --filter FullyQualifiedName~GuardsTests`
Expected: `Passed: 6, Failed: 0`.

- [ ] **Step 5: Commit**

```bash
git add TradyStrat.Cli/Mcp/Tools/Guards.cs \
        TradyStrat.Cli.Tests/Mcp/Tools/GuardsTests.cs
git commit -m "feat(mcp): Guards (instrument cache + date range parsing) — Phase 7"
```

---

## Phase 8 — Tools

Each tool follows the same shape (per spec §5.7): inject use cases + `Guards` (+ `IClock` where needed), apply input guards, call the use case, map to DTO. No `try/catch` — filters handle errors. Tests cover happy path + each guard rule.

### Task 8.1: `InstrumentTool` (TDD)

**Files:**
- Create: `TradyStrat.Cli/Mcp/Tools/InstrumentTool.cs`
- Test: `TradyStrat.Cli.Tests/Mcp/Tools/InstrumentToolTests.cs`

- [ ] **Step 1: Test**

```csharp
public class InstrumentToolTests
{
    [Fact]
    public async Task ListInstruments_returns_all_with_focus_role_on_focus_ticker()
    {
        // Compose host with 3 seeded instruments + Tickers:Focus=CON3.L config.
        var resp = await tool.ListInstruments(default);
        resp.Instruments.Count.ShouldBe(3);
        resp.Instruments.Single(i => i.Ticker == "CON3.L").Role.ShouldBe(InstrumentRole.Focus);
        resp.Instruments.Where(i => i.Ticker != "CON3.L").ShouldAllBe(i => i.Role == InstrumentRole.Context);
    }
}
```

- [ ] **Step 2: Verify failure**

Expected: compile error.

- [ ] **Step 3: Implement**

```csharp
using System.ComponentModel;
using Microsoft.Extensions.Configuration;
using ModelContextProtocol.Server;
using TradyStrat.Application.Settings.UseCases;
using TradyStrat.Cli.Mcp.Dto;
using TradyStrat.Cli.Mcp.Mapping;

namespace TradyStrat.Cli.Mcp.Tools;

[McpServerToolType]
public sealed class InstrumentTool(
    ListInstrumentsUseCase listInstruments,
    IConfiguration config)
{
    [McpServerTool(Name = "list_instruments"),
     Description("List all instruments TradyStrat tracks.")]
    public async Task<InstrumentListResponse> ListInstruments(CancellationToken ct)
    {
        var instruments = await listInstruments.ExecuteAsync(default, ct);
        var focus = config["Tickers:Focus"] ?? "CON3.L";
        return InstrumentMapper.ToResponse(instruments, focus);
    }
}
```

Match `ListInstrumentsUseCase`'s real input type. If it's `Unit`, pass `Unit.Value`; if `EmptyInput`, pass that.

- [ ] **Step 4: Verify**

Run: `dotnet test TradyStrat.Cli.Tests --filter FullyQualifiedName~InstrumentToolTests`
Expected: `Passed: 1, Failed: 0`.

- [ ] **Step 5: Commit**

```bash
git add TradyStrat.Cli/Mcp/Tools/InstrumentTool.cs \
        TradyStrat.Cli.Tests/Mcp/Tools/InstrumentToolTests.cs
git commit -m "feat(mcp): InstrumentTool (list_instruments) — Phase 8"
```

### Task 8.2: `DashboardTool` (TDD)

**Files:**
- Create: `TradyStrat.Cli/Mcp/Tools/DashboardTool.cs`
- Test: `TradyStrat.Cli.Tests/Mcp/Tools/DashboardToolTests.cs`

- [ ] **Step 1: Test**

Cover:
- Happy path with explicit instrument + asOf → returns snapshot with expected ticker/date.
- Default instrument = focus ticker from config.
- Default asOf = `clock.Today()`.
- Historical asOf → `LoadDashboardInput.IsHistorical = true` (verify via a spy on the use case).
- Unknown instrument → `ArgumentException` from `Guards`.
- Invalid date format → `ArgumentException` from `Guards`.

- [ ] **Step 2: Verify failure**

Expected: compile error.

- [ ] **Step 3: Implement**

```csharp
using System.ComponentModel;
using Microsoft.Extensions.Configuration;
using ModelContextProtocol.Server;
using TradyStrat.Application.Dashboard.UseCases;
using TradyStrat.Application.Time;
using TradyStrat.Cli.Mcp.Dto;
using TradyStrat.Cli.Mcp.Mapping;

namespace TradyStrat.Cli.Mcp.Tools;

[McpServerToolType]
public sealed class DashboardTool(
    LoadDashboardUseCase useCase,
    Guards guards,
    IClock clock,
    IConfiguration config)
{
    [McpServerTool(Name = "get_dashboard"),
     Description("Snapshot of an instrument: price, indicators, zone, today's AI suggestion, position.")]
    public async Task<DashboardSnapshot> GetDashboard(
        string? instrument = null,
        string? asOf = null,
        CancellationToken ct = default)
    {
        var ticker = instrument ?? config["Tickers:Focus"] ?? "CON3.L";
        var inst = await guards.ResolveInstrumentOrThrow(ticker, ct);
        var (_, target) = Guards.ResolveDateRange(from: asOf, to: asOf, defaultBack: 0, clockToday: clock.Today());
        var isHistorical = target != clock.Today();

        var output = await useCase.ExecuteAsync(new LoadDashboardInput(target, isHistorical), ct);
        return DashboardMapper.ToSnapshot(output);
    }
}
```

If `LoadDashboardInput` has additional required fields (e.g., `InstrumentId`), adapt accordingly — read the input record's actual shape and pass everything required.

- [ ] **Step 4: Verify**

Run: `dotnet test TradyStrat.Cli.Tests --filter FullyQualifiedName~DashboardToolTests`
Expected: all guard + happy-path tests pass.

- [ ] **Step 5: Commit**

```bash
git add TradyStrat.Cli/Mcp/Tools/DashboardTool.cs \
        TradyStrat.Cli.Tests/Mcp/Tools/DashboardToolTests.cs
git commit -m "feat(mcp): DashboardTool (get_dashboard) — Phase 8"
```

### Task 8.3: `SuggestionTool` (TDD)

**Files:**
- Create: `TradyStrat.Cli/Mcp/Tools/SuggestionTool.cs`
- Test: `TradyStrat.Cli.Tests/Mcp/Tools/SuggestionToolTests.cs`

- [ ] **Step 1: Test**

Cover:
- Happy path with default args → returns suggestions newest-first.
- Explicit `action` filter narrows results.
- `limit = 0` → `ArgumentException` with message about `[1, 100]`.
- `limit = 250` → same.
- `action = "Bogus"` → `ArgumentException` with "must be one of...".
- Unknown instrument → `ArgumentException`.

- [ ] **Step 2: Verify failure**

Expected: compile error.

- [ ] **Step 3: Implement**

```csharp
using System.ComponentModel;
using Microsoft.Extensions.Configuration;
using ModelContextProtocol.Server;
using TradyStrat.Application.AiSuggestion.UseCases;
using TradyStrat.Application.Time;
using TradyStrat.Cli.Mcp.Dto;
using TradyStrat.Cli.Mcp.Mapping;
using TradyStrat.Domain;

namespace TradyStrat.Cli.Mcp.Tools;

[McpServerToolType]
public sealed class SuggestionTool(
    QuerySuggestionsUseCase useCase,
    Guards guards,
    IClock clock,
    IConfiguration config)
{
    [McpServerTool(Name = "query_suggestions"),
     Description("Past AI suggestions for an instrument with action, conviction, and outcome.")]
    public async Task<SuggestionPage> QuerySuggestions(
        string? instrument = null,
        string? from = null,
        string? to = null,
        string? action = null,
        int limit = 30,
        CancellationToken ct = default)
    {
        if (limit < 1 || limit > 100)
            throw new ArgumentException($"limit must be between 1 and 100 (got {limit}).");

        SuggestionAction? parsedAction = null;
        if (action is not null)
        {
            if (!Enum.TryParse<SuggestionAction>(action, ignoreCase: false, out var a))
                throw new ArgumentException(
                    $"action must be one of {string.Join(", ", Enum.GetNames<SuggestionAction>())} (got '{action}').");
            parsedAction = a;
        }

        var ticker = instrument ?? config["Tickers:Focus"] ?? "CON3.L";
        var inst = await guards.ResolveInstrumentOrThrow(ticker, ct);
        var (f, t) = Guards.ResolveDateRange(from, to, defaultBack: 90, clockToday: clock.Today());

        var output = await useCase.ExecuteAsync(
            new QuerySuggestionsInput(inst.Id, f, t, parsedAction, limit), ct);
        return SuggestionMapper.ToPage(output, ticker, f, t);
    }
}
```

- [ ] **Step 4: Verify**

Run: `dotnet test TradyStrat.Cli.Tests --filter FullyQualifiedName~SuggestionToolTests`
Expected: all tests pass.

- [ ] **Step 5: Commit**

```bash
git add TradyStrat.Cli/Mcp/Tools/SuggestionTool.cs \
        TradyStrat.Cli.Tests/Mcp/Tools/SuggestionToolTests.cs
git commit -m "feat(mcp): SuggestionTool (query_suggestions) — Phase 8"
```

### Task 8.4: `PriceTool` (TDD)

**Files:**
- Create: `TradyStrat.Cli/Mcp/Tools/PriceTool.cs`
- Test: `TradyStrat.Cli.Tests/Mcp/Tools/PriceToolTests.cs`

- [ ] **Step 1: Test**

Cover:
- Happy path with default range → returns bars.
- `withIndicators=true` populates indicator arrays.
- > 365-day range → `ArgumentException` with the exact message from §7.
- Unknown instrument → `ArgumentException`.
- `instrument` is required — missing argument is rejected at the SDK level (no explicit test needed; document in a comment).

- [ ] **Step 2: Verify failure**

Expected: compile error.

- [ ] **Step 3: Implement**

```csharp
using System.ComponentModel;
using ModelContextProtocol.Server;
using TradyStrat.Application.PriceFeed.UseCases;
using TradyStrat.Application.Time;
using TradyStrat.Cli.Mcp.Dto;
using TradyStrat.Cli.Mcp.Mapping;

namespace TradyStrat.Cli.Mcp.Tools;

[McpServerToolType]
public sealed class PriceTool(
    GetPriceSeriesUseCase useCase,
    Guards guards,
    IClock clock)
{
    private const int MaxBars = 365;

    [McpServerTool(Name = "query_prices"),
     Description("Daily OHLCV bars for an instrument, optionally with indicator series.")]
    public async Task<PriceSeries> QueryPrices(
        string instrument,
        string? from = null,
        string? to = null,
        bool withIndicators = false,
        CancellationToken ct = default)
    {
        var inst = await guards.ResolveInstrumentOrThrow(instrument, ct);
        var (f, t) = Guards.ResolveDateRange(from, to, defaultBack: 90, clockToday: clock.Today());
        if ((t.DayNumber - f.DayNumber + 1) > MaxBars)
            throw new ArgumentException(
                $"Date range exceeds {MaxBars}-day maximum. Narrow the window or make multiple calls.");

        var output = await useCase.ExecuteAsync(
            new GetPriceSeriesInput(inst.Id, f, t, withIndicators), ct);
        return PriceMapper.ToSeries(output, inst.Ticker);
    }
}
```

- [ ] **Step 4: Verify**

Run: `dotnet test TradyStrat.Cli.Tests --filter FullyQualifiedName~PriceToolTests`
Expected: all tests pass.

- [ ] **Step 5: Commit**

```bash
git add TradyStrat.Cli/Mcp/Tools/PriceTool.cs \
        TradyStrat.Cli.Tests/Mcp/Tools/PriceToolTests.cs
git commit -m "feat(mcp): PriceTool (query_prices) with 365-bar cap — Phase 8"
```

### Task 8.5: `PortfolioTool` (TDD)

**Files:**
- Create: `TradyStrat.Cli/Mcp/Tools/PortfolioTool.cs`
- Test: `TradyStrat.Cli.Tests/Mcp/Tools/PortfolioToolTests.cs`

- [ ] **Step 1: Test**

Cover:
- Happy path → returns aggregate + positions + trades.
- Default `asOf` = today.
- Explicit `asOf` flows through to `PortfolioService.SnapshotAsync` overload.
- Invalid `asOf` format → `ArgumentException`.

- [ ] **Step 2: Verify failure**

Expected: compile error.

- [ ] **Step 3: Implement**

```csharp
using System.ComponentModel;
using ModelContextProtocol.Server;
using TradyStrat.Application.Portfolio;
using TradyStrat.Application.Settings.UseCases;
using TradyStrat.Application.Time;
using TradyStrat.Cli.Mcp.Dto;
using TradyStrat.Cli.Mcp.Mapping;

namespace TradyStrat.Cli.Mcp.Tools;

[McpServerToolType]
public sealed class PortfolioTool(
    PortfolioService portfolio,
    /* + whatever services PortfolioService needs from the caller — e.g., price lookups,
         goal lookup. Mirror what LoadDashboardUseCase passes to PortfolioService.SnapshotAsync. */
    Guards guards,
    IClock clock)
{
    [McpServerTool(Name = "get_portfolio"),
     Description("Current portfolio: per-ticker lots, aggregate value, progress toward goal.")]
    public async Task<PortfolioSnapshotDto> GetPortfolio(
        string? asOf = null,
        CancellationToken ct = default)
    {
        var (_, target) = Guards.ResolveDateRange(from: asOf, to: asOf, defaultBack: 0, clockToday: clock.Today());
        // ... call PortfolioService.SnapshotAsync with the per-instrument price map and goal ...
        // (peek at LoadDashboardUseCase for how it builds the priceByInstrument dictionary)
        // ... then map to the MCP DTO ...
        throw new NotImplementedException("Fill in by mirroring LoadDashboardUseCase's PortfolioService call");
    }
}
```

**Implementer note:** `PortfolioService.SnapshotAsync` needs a `IReadOnlyDictionary<int, (decimal, string, string)> priceByInstrument` (latest price + currency + something) and `decimal goalEur`. Find the existing caller (`LoadDashboardUseCase`) and replicate the price-map-building logic — extract a shared helper if appropriate.

- [ ] **Step 4: Verify**

Run: `dotnet test TradyStrat.Cli.Tests --filter FullyQualifiedName~PortfolioToolTests`
Expected: all tests pass.

- [ ] **Step 5: Commit**

```bash
git add TradyStrat.Cli/Mcp/Tools/PortfolioTool.cs \
        TradyStrat.Cli.Tests/Mcp/Tools/PortfolioToolTests.cs
git commit -m "feat(mcp): PortfolioTool (get_portfolio) — Phase 8"
```

### Task 8.6: `ReplayTool` (TDD)

**Files:**
- Create: `TradyStrat.Cli/Mcp/Tools/ReplayTool.cs`
- Test: `TradyStrat.Cli.Tests/Mcp/Tools/ReplayToolTests.cs`

- [ ] **Step 1: Test**

Cover:
- Happy path with valid range → returns `ReplayReport`.
- `persist = false, force = false` always — verify the use case is invoked with those values regardless of any tool arg (there shouldn't be one).
- Empty range (no suggestions in window) → returns empty report (per spec §4.6), not an error.
- Unknown instrument → `ArgumentException`.
- Inverted range → `ArgumentException` (via `Guards.ResolveDateRange`).

- [ ] **Step 2: Verify failure**

Expected: compile error.

- [ ] **Step 3: Implement**

```csharp
using System.ComponentModel;
using ModelContextProtocol.Server;
using TradyStrat.Application.AiSuggestion.UseCases;
using TradyStrat.Application.Time;

namespace TradyStrat.Cli.Mcp.Tools;

[McpServerToolType]
public sealed class ReplayTool(
    ReplaySuggestionsUseCase useCase,
    Guards guards,
    IClock clock)
{
    [McpServerTool(Name = "get_replay_report"),
     Description("Re-run the AI prompt against historical snapshots in dry-run mode and return hit-rate / forward-return stats.")]
    public async Task<ReplayReport> GetReplayReport(
        string instrument,
        string from,
        string to,
        CancellationToken ct = default)
    {
        var inst = await guards.ResolveInstrumentOrThrow(instrument, ct);
        var (f, t) = Guards.ResolveDateRange(from, to, defaultBack: 0, clockToday: clock.Today());
        return await useCase.ExecuteAsync(
            new ReplaySuggestionsInput(inst.Id, f, t, Persist: false, Force: false), ct);
    }
}
```

The return type is `TradyStrat.Application.AiSuggestion.UseCases.ReplayReport` (the documented DTO exception in §6 / §13.4) — confirmed reused as-is.

- [ ] **Step 4: Verify**

Run: `dotnet test TradyStrat.Cli.Tests --filter FullyQualifiedName~ReplayToolTests`
Expected: all tests pass.

- [ ] **Step 5: Commit**

```bash
git add TradyStrat.Cli/Mcp/Tools/ReplayTool.cs \
        TradyStrat.Cli.Tests/Mcp/Tools/ReplayToolTests.cs
git commit -m "feat(mcp): ReplayTool (get_replay_report) — Phase 8"
```

---

## Phase 9 — Wiring

### Task 9.1: `McpCliModule`

**Files:**
- Create: `TradyStrat.Cli/Mcp/McpCliModule.cs`

- [ ] **Step 1: Implement the module**

```csharp
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ModelContextProtocol.Server;
using TheAppManager;     // IAppModule (vendored)
using TradyStrat.Cli.Mcp.Filters;
using TradyStrat.Cli.Mcp.Serialization;
using TradyStrat.Cli.Mcp.Tools;

namespace TradyStrat.Cli.Mcp;

public sealed class McpCliModule : IAppModule
{
    public void ConfigureServices(IServiceCollection services, IConfiguration config)
    {
        services.AddScoped<Guards>();

        var jsonOptions = McpJsonSerializerOptions.Create();

        services
            .AddMcpServer()
            .WithStdioServerTransport()
            .WithRequestFilters(rb => rb
                .AddCallToolFilter(McpLoggingFilter.Handle)
                .AddCallToolFilter(McpTimeoutFilter.Handle)
                .AddCallToolFilter(McpExceptionTranslationFilter.Handle))
            .WithTools<InstrumentTool>(jsonSerializerOptions: jsonOptions)
            .WithTools<DashboardTool>(jsonSerializerOptions: jsonOptions)
            .WithTools<SuggestionTool>(jsonSerializerOptions: jsonOptions)
            .WithTools<PriceTool>(jsonSerializerOptions: jsonOptions)
            .WithTools<PortfolioTool>(jsonSerializerOptions: jsonOptions)
            .WithTools<ReplayTool>(jsonSerializerOptions: jsonOptions);
    }
}
```

If `IAppModule` lives at a different namespace in the vendored `TheAppManager` (now `TradyStrat.Hosting`), correct the `using` (`grep -rn "interface IAppModule" TradyStrat.Hosting`).

If `.WithTools<T>(jsonSerializerOptions: ...)` doesn't accept that overload exactly, check the SDK's actual signature and adapt (it may be a separate fluent call like `.WithSerializerOptions(opts)`).

- [ ] **Step 2: Build**

Run: `dotnet build TradyStrat.Cli`
Expected: build succeeds.

- [ ] **Step 3: Commit**

```bash
git add TradyStrat.Cli/Mcp/McpCliModule.cs
git commit -m "feat(mcp): McpCliModule (wiring locus) — Phase 9"
```

### Task 9.2: `McpCliModule` wiring test

**Files:**
- Create: `TradyStrat.Cli.Tests/Mcp/McpCliModuleTests.cs`

- [ ] **Step 1: Test**

```csharp
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using ModelContextProtocol.Server;
using Shouldly;
using TradyStrat.Application;
using TradyStrat.Cli.Mcp;
using TradyStrat.Cli.Mcp.Tools;
using TradyStrat.Infrastructure;
using TradyStrat.Infrastructure.PriceFeed;
using TheAppManager.Startup;
using Xunit;

namespace TradyStrat.Cli.Tests.Mcp;

public class McpCliModuleTests
{
    [Fact]
    public void Builds_and_resolves_all_tools_and_filters()
    {
        var builder = Host.CreateApplicationBuilder();
        AppManager.ConfigureServices(builder.Services, builder.Configuration, modules => modules
            .AddFromAssemblyOf<ApplicationAssemblyMarker>()
            .AddFromAssemblyOf<InfrastructureAssemblyMarker>(t =>
                t != typeof(PriceFeedBackgroundInfrastructureModule))
            .AddFromAssemblyOf<CliAssemblyMarker>());

        using var host = builder.Build();

        // All six tool types resolve from DI (they're transients via WithTools<T>)
        using var scope = host.Services.CreateScope();
        scope.ServiceProvider.GetRequiredService<InstrumentTool>().ShouldNotBeNull();
        scope.ServiceProvider.GetRequiredService<DashboardTool>().ShouldNotBeNull();
        scope.ServiceProvider.GetRequiredService<SuggestionTool>().ShouldNotBeNull();
        scope.ServiceProvider.GetRequiredService<PriceTool>().ShouldNotBeNull();
        scope.ServiceProvider.GetRequiredService<PortfolioTool>().ShouldNotBeNull();
        scope.ServiceProvider.GetRequiredService<ReplayTool>().ShouldNotBeNull();

        // MCP server services exist
        scope.ServiceProvider.GetService<IMcpServer>().ShouldNotBeNull();
    }
}
```

If `IMcpServer` isn't directly resolvable until the host runs, replace that line with a resolution of an `IHostedService` whose type name contains `Mcp` (use reflection / `.OfType<>`).

- [ ] **Step 2: Run**

Run: `dotnet test TradyStrat.Cli.Tests --filter FullyQualifiedName~McpCliModuleTests`
Expected: `Passed: 1, Failed: 0`.

- [ ] **Step 3: Commit**

```bash
git add TradyStrat.Cli.Tests/Mcp/McpCliModuleTests.cs
git commit -m "test(mcp): McpCliModule wiring smoke — Phase 9"
```

### Task 9.3: `McpCommand`

**Files:**
- Create: `TradyStrat.Cli/Commands/McpCommand.cs`

- [ ] **Step 1: Implement**

```csharp
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Spectre.Console.Cli;
using TheAppManager.Startup;
using TradyStrat.Application;
using TradyStrat.Cli.Mcp;
using TradyStrat.Infrastructure;
using TradyStrat.Infrastructure.PriceFeed;

namespace TradyStrat.Cli.Commands;

internal sealed class McpCommand : AsyncCommand
{
    public override async Task<int> ExecuteAsync(CommandContext context)
    {
        var builder = Host.CreateApplicationBuilder();
        builder.Logging.ClearProviders();
        builder.Logging.AddConsole(o => o.LogToStandardErrorThreshold = LogLevel.Trace);

        AppManager.ConfigureServices(builder.Services, builder.Configuration, modules => modules
            .AddFromAssemblyOf<ApplicationAssemblyMarker>()
            .AddFromAssemblyOf<InfrastructureAssemblyMarker>(t =>
                t != typeof(PriceFeedBackgroundInfrastructureModule))
            .AddFromAssemblyOf<CliAssemblyMarker>());

        using var innerHost = builder.Build();
        await innerHost.RunAsync();   // blocks until stdin EOF
        return 0;
    }
}
```

- [ ] **Step 2: Build**

Run: `dotnet build TradyStrat.Cli`
Expected: build succeeds.

- [ ] **Step 3: Commit**

```bash
git add TradyStrat.Cli/Commands/McpCommand.cs
git commit -m "feat(cli): McpCommand (Spectre subcommand) — Phase 9"
```

### Task 9.4: `Program.cs` — global stderr logging + register `McpCommand`

**Files:**
- Modify: `TradyStrat.Cli/Program.cs`

- [ ] **Step 1: Add stderr logging configuration to the outer host**

Edit `TradyStrat.Cli/Program.cs`. After `var builder = Host.CreateApplicationBuilder(args);`, add:

```csharp
builder.Logging.AddConsole(o => o.LogToStandardErrorThreshold = LogLevel.Trace);
```

- [ ] **Step 2: Register `McpCommand` in the Spectre `CommandApp`**

In the same file, locate `app.Configure(c => { c.AddCommand<ReplayCommand>("replay") ... });` and add the new command:

```csharp
app.Configure(c =>
{
    c.AddCommand<ReplayCommand>("replay")
     .WithDescription("Replay the AI prompt against historical snapshots and score the results.");
    c.AddCommand<McpCommand>("mcp")
     .WithDescription("Run the read-only TradyStrat MCP server over stdio.");
});
```

- [ ] **Step 3: Build + smoke-test the CLI surface**

Run: `dotnet run --project TradyStrat.Cli -- --help`
Expected: Spectre lists both `replay` and `mcp` commands.

Run: `dotnet run --project TradyStrat.Cli -- mcp --help`
Expected: Spectre prints `mcp`'s description; no MCP loop starts because Spectre intercepts `--help`.

- [ ] **Step 4: Commit**

```bash
git add TradyStrat.Cli/Program.cs
git commit -m "feat(cli): register mcp subcommand + stderr-logging — Phase 9"
```

### Task 9.5: Spectre registration smoke test

**Files:**
- Create: `TradyStrat.Cli.Tests/Mcp/McpCommandRegistrationTests.cs`

- [ ] **Step 1: Test**

```csharp
using Shouldly;
using Spectre.Console.Cli;
using TradyStrat.Cli.Commands;
using Xunit;

namespace TradyStrat.Cli.Tests.Mcp;

public class McpCommandRegistrationTests
{
    [Fact]
    public void Mcp_command_is_registered_under_mcp_name()
    {
        var app = new CommandApp();
        app.Configure(c =>
        {
            c.AddCommand<McpCommand>("mcp");
        });

        // Spectre exposes the configured model via TestApp / configuration introspection.
        // Easiest assertion: invoke `--help` and parse output for the "mcp" command line.
        var result = app.Run(new[] { "--help" });
        result.ShouldBe(0);
        // Capturing stdout for assertion needs a console redirect — left as exercise
        // if the test reveals an actual bug. The smoke is "Configure didn't throw".
    }
}
```

If introspecting `CommandApp` is cumbersome, the bar drops to "Configure with `McpCommand` doesn't throw" — that catches enough regressions for a smoke.

- [ ] **Step 2: Run**

Run: `dotnet test TradyStrat.Cli.Tests --filter FullyQualifiedName~McpCommandRegistrationTests`
Expected: `Passed: 1, Failed: 0`.

- [ ] **Step 3: Commit**

```bash
git add TradyStrat.Cli.Tests/Mcp/McpCommandRegistrationTests.cs
git commit -m "test(cli): McpCommand Spectre registration smoke — Phase 9"
```

### Task 9.6: Full test sweep

- [ ] **Step 1: Run the full test suite**

Run: `dotnet test`
Expected: all tests pass across `Domain.Tests`, `Application.Tests`, `Infrastructure.Tests`, `E2E.Tests`, and the new `Cli.Tests`.

If anything fails, fix and re-commit before proceeding to Phase 10.

---

## Phase 10 — Manual validation

The automated tests stop at the SDK boundary. This phase confirms end-to-end behaviour against Claude Desktop. Not automatable; run once before merging.

### Task 10.1: Local smoke

- [ ] **Step 1: Build the release**

Run: `dotnet build --configuration Release`
Expected: build succeeds.

- [ ] **Step 2: Start the MCP server bare**

Run (in one terminal): `dotnet run --project TradyStrat.Cli --configuration Release -- mcp`
Expected: process starts; **stdout is silent**; stderr shows the host startup logs.

Send `Ctrl+D` (EOF) on stdin; expected: process exits cleanly with code 0.

- [ ] **Step 3: Verify Spectre help still works**

Run: `dotnet run --project TradyStrat.Cli -- --help`
Expected: `replay` and `mcp` listed.

### Task 10.2: Claude Desktop integration

- [ ] **Step 1: Add MCP server config**

Edit `~/Library/Application Support/Claude/claude_desktop_config.json`. Add (or merge):

```jsonc
{
  "mcpServers": {
    "tradystrat": {
      "command": "dotnet",
      "args": ["run", "--project", "/absolute/path/to/TradyStrat.Cli", "--", "mcp"]
    }
  }
}
```

Replace `/absolute/path/to/TradyStrat.Cli` with the real worktree path.

- [ ] **Step 2: Restart Claude Desktop**

Quit and reopen Claude Desktop. Check the MCP-servers indicator (varies by Claude Desktop version — usually a small icon in the chat composer) shows `tradystrat` as connected.

- [ ] **Step 3: Run the six tool checks**

In a fresh conversation:

1. Ask: "What instruments does TradyStrat track?"
   Expected: Claude calls `list_instruments` and reports three tickers.

2. Ask: "What's today's suggestion for CON3.L?"
   Expected: Claude calls `get_dashboard` and reports the suggestion block.

3. Ask: "Show me CON3.L's last 30 days of suggestions."
   Expected: Claude calls `query_suggestions` with appropriate `from`/`to`.

4. Ask: "What's COIN been doing this month? Include indicators."
   Expected: Claude calls `query_prices` with `withIndicators=true`.

5. Ask: "How am I doing toward the goal?"
   Expected: Claude calls `get_portfolio` and reports `aggregate.progressPct`.

6. Ask: "What was the hit rate of suggestions for COIN over the last 60 days?"
   Expected: Claude calls `get_replay_report` and reports the conviction-weighted score.

- [ ] **Step 4: No-write check**

Run: `sqlite3 ~/Library/Application\ Support/TradyStrat/tradystrat.db "select max(CreatedAt) from Suggestion;"`

Note the value. Repeat steps 10.2.3 (the six Claude questions). Re-run the query.

Expected: `max(CreatedAt)` is **unchanged**. The MCP server did not write.

- [ ] **Step 5: No-outbound-AI check**

While Claude Desktop is making MCP calls, watch the stderr log (visible in Claude Desktop's "view logs" UI or as the running `dotnet run` process's stderr).

Run: `grep -i "anthropic\|chat.completions\|IChatClient" /path/to/captured/stderr.log`
Expected: **zero matches** during the six-question sequence. The MCP path must not trigger outbound AI calls.

- [ ] **Step 6: Timeout sanity (optional — only if comfortable with a temporary patch)**

In a scratch branch, replace one tool's use case call with `await Task.Delay(35_000, ct); return ...;`. Restart Claude Desktop. Ask the corresponding question. Expected: after ~30 seconds, Claude reports the timeout MCP error. Revert the patch.

- [ ] **Step 7: Commit the runbook results**

The manual validation produces no code changes (assuming everything passed). If anything failed, fix and re-test before merging.

If you want a record of the manual session, append findings to a top-level comment in the PR.

---

## Phase 11 — Documentation sync and PR

### Task 11.1: Update the README

**Files:**
- Modify: `README.md`

- [ ] **Step 1: Add a section to the README about the MCP server**

Insert below the existing "Quick start" section, before "Tests":

```markdown
## MCP server (read-only, personal)

A read-only MCP server is available as a subcommand of `TradyStrat.Cli`:

```bash
dotnet run --project TradyStrat.Cli -- mcp
```

This exposes six question-oriented tools (`list_instruments`, `get_dashboard`,
`query_suggestions`, `query_prices`, `get_portfolio`, `get_replay_report`) over
stdio for use with Claude Desktop or Claude Code. See
[`docs/superpowers/specs/2026-05-18-mcp-server-design.md`](docs/superpowers/specs/2026-05-18-mcp-server-design.md)
for the full design and reference-architecture conventions.

The server is read-only and personal: no writes, no auth, stdio-only,
single-user.
```

- [ ] **Step 2: Commit**

```bash
git add README.md
git commit -m "docs(readme): describe the MCP subcommand"
```

### Task 11.2: Open the PR

- [ ] **Step 1: Push the worktree branch**

```bash
git push -u origin HEAD
```

- [ ] **Step 2: Open PR via gh**

```bash
gh pr create --title "feat: read-only MCP server (Phase 1)" --body "$(cat <<'EOF'
## Summary

- Adds a read-only MCP server as the `mcp` subcommand of `TradyStrat.Cli`.
- Six question-oriented tools (`list_instruments`, `get_dashboard`,
  `query_suggestions`, `query_prices`, `get_portfolio`, `get_replay_report`).
- Filter pipeline (Decorator): logging → 30s timeout → exception translation.
- `McpCliModule : IAppModule` is the single wiring locus.
- Two new Application use cases (`GetPriceSeriesUseCase`,
  `QuerySuggestionsUseCase`) plus their specs; `IIndicatorEngine` interface
  extracted from the existing sealed class.
- New `TradyStrat.Cli.Tests` test project with tool, filter, mapper, and
  module-wiring tests.

Establishes the reference architecture for future MCP work — see
[the spec](docs/superpowers/specs/2026-05-18-mcp-server-design.md) §13.

## Test plan

- [x] `dotnet test` passes across all five test projects.
- [x] `dotnet run --project TradyStrat.Cli -- mcp` starts, writes nothing
      to stdout, exits cleanly on stdin EOF.
- [x] Claude Desktop connects; the six tool checks (see plan §10.2.3) all
      pass with sensible responses.
- [x] No DB writes during MCP usage (verified by `max(CreatedAt)` before/after).
- [x] No outbound Anthropic HTTP calls during MCP usage (verified by grep
      on stderr logs).

🤖 Generated with [Claude Code](https://claude.com/claude-code)
EOF
)"
```

- [ ] **Step 2: Print the PR URL**

The `gh pr create` command prints the URL on success. Verify by running `gh pr view --web`.

---

## Self-review checklist

After implementation, before merging, the implementer should sanity-check:

1. **Spec §2 row-by-row coverage.** Every decision in §2 maps to at least one task — read the table top-to-bottom.
2. **Spec §13.2 "how to add a new tool" recipe.** Pick a hypothetical 7th tool and walk the steps. If any step references a file/pattern that doesn't exist after this PR, fix the gap.
3. **Spec §4 tool descriptions vs implementation.** Every JSON example in §4 should match the actual serialized output for the same inputs. Sample at least one tool's output by inspecting the tests.
4. **No `try/catch` in any tool method.** `grep -rn "try$\|catch" TradyStrat.Cli/Mcp/Tools` should return zero non-test lines. The filter pipeline owns translation.
5. **No `AnsiConsole.*` under `Mcp/`.** `grep -rn "AnsiConsole" TradyStrat.Cli/Mcp` should return zero hits.
6. **No stdout writes outside the MCP protocol.** `grep -rn "Console.Write\|Console.Out" TradyStrat.Cli/Mcp` should return zero hits (the SDK owns stdout).
7. **All six tools have unique snake_case names.** `grep -rn "McpServerTool(Name" TradyStrat.Cli/Mcp/Tools` shows six distinct names.

If any check fails, fix and re-commit.
