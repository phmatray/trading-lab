# TradyStrat — MCP server (read-only, personal)

**Status:** Design approved · Ready for implementation plan
**Date:** 2026-05-18
**Author:** Philippe Matray (with Claude)
**Depends on:** the hexagonal refactor and the Spectre-based `TradyStrat.Cli` (already merged on `main`).
**Successor:** none yet — writes, HTTP transport, and authentication are explicitly v2+.

---

## 1. Purpose & goal

Expose TradyStrat's read-only state to Claude (Desktop or Code, running on the same laptop) over the Model Context Protocol so that the daily "how's my portfolio doing, what does the AI say, what's happening with the indicators" conversation can happen inside Claude instead of by clicking around the Blazor dashboard.

The MCP server is **personal, local, single-user, read-only**. Same security stance as the rest of the project: `127.0.0.1`-only and trust-the-caller. Mutations (logging trades, force-refetching suggestions, editing settings) stay in the dashboard UI. The dashboard remains the source of truth for any change to state.

## 2. Decisions at a glance

| Concern | Decision |
|---|---|
| Primary client | **Claude Desktop / Claude Code on the same laptop.** Single-user, personal assistant. |
| Capability scope | **Read-only.** No writes, no side effects, no AI-triggering tools. |
| Transport | **Stdio only.** Claude Desktop's default; no HTTP / SSE / network listener. |
| Project layout | **`mcp` subcommand of `TradyStrat.Cli`.** No new csproj for production code. New `Mcp/` folder inside `TradyStrat.Cli`. |
| SDK | **`ModelContextProtocol` (official C# SDK, Microsoft).** NuGet packages: `ModelContextProtocol` and the already-present `Microsoft.Extensions.Hosting`. |
| Tool surface shape | **Question-oriented**, not 1:1 over use cases. Six tools shaped around real questions, each backed by one or more existing use cases plus a thin DTO mapper. |
| Tool count | **Six.** `list_instruments`, `get_dashboard`, `query_suggestions`, `query_prices`, `get_portfolio`, `get_replay_report`. |
| Tool registration | **Explicit `.WithTools<T>()` per class**, not `WithToolsFromAssembly()`. Future tool classes are not auto-published. |
| Hosting model | **Inner host inside `McpCommand.ExecuteAsync`.** Fresh `Host.CreateApplicationBuilder`, re-composes the same modules the outer CLI uses (minus `PriceFeedBackgroundInfrastructureModule`), registers MCP services, `await innerHost.RunAsync(ct)` blocks until stdin EOF. |
| Stdio hygiene | **All `ILogger` output to stderr, globally for the CLI.** One-line `Logging.AddConsole(o => o.LogToStandardErrorThreshold = LogLevel.Trace)` in `Program.cs`. The `mcp` path never writes to stdout outside the MCP protocol. |
| `AnsiConsole` discipline | **No `AnsiConsole.*` calls anywhere under `Mcp/`.** Code review check: a grep for `AnsiConsole` in that folder must return empty. |
| Error model | **`TradyStratException` → `McpException` (preserve message).** Input-shape errors → `ArgumentException` with actionable text. Unexpected errors propagate; SDK reports generic error and stack is logged to stderr. |
| Input validation | **Per-tool guard block at method entry.** Ticker existence, `from <= to`, `limit` ∈ `[1, 100]`, `query_prices` window ≤ 365 days. Failed guard → `ArgumentException` with a message Claude can act on. |
| Side-effect policy | **Never trigger AI calls or external writes to satisfy a query.** Missing data → `null`, not "go fetch it". |
| Configuration surface | **None in v1.** `mcp` has no `[CommandOption]`s. Caps / behaviour are hard-coded constants in the tool methods. |
| Test project | **New `TradyStrat.Cli.Tests`** (xunit.v3 + Shouldly + `TradyStrat.TestKit`). Follows the layer-per-test-project convention. |
| Protocol-level tests | **None.** SDK is the trusted boundary. |
| Feature flag | **None.** New subcommand; nothing else changes for users of the dashboard or the existing `replay` command. |

## 3. Project layout

New files. No existing files move.

```
TradyStrat.Cli/
  Commands/
    ReplayCommand.cs              (unchanged)
    McpCommand.cs                 NEW — Spectre AsyncCommand, registers "mcp"
  Mcp/                            NEW folder, all MCP code lives here
    Tools/
      InstrumentTool.cs           list_instruments
      DashboardTool.cs            get_dashboard
      SuggestionTool.cs           query_suggestions
      PriceTool.cs                query_prices
      PortfolioTool.cs            get_portfolio
      ReplayTool.cs               get_replay_report
    Dto/
      InstrumentDtos.cs
      DashboardDtos.cs
      SuggestionDtos.cs
      PriceDtos.cs
      PortfolioDtos.cs
      ReplayDtos.cs
    Mapping/
      DtoMappers.cs               flat-projection helpers (Domain/Application → MCP DTOs)
  Program.cs                      MODIFIED — add Logging.AddConsole(stderr), register McpCommand

TradyStrat.Cli.Tests/             NEW csproj
  TradyStrat.Cli.Tests.csproj
  Mcp/
    Tools/
      InstrumentToolTests.cs
      DashboardToolTests.cs
      SuggestionToolTests.cs
      PriceToolTests.cs
      PortfolioToolTests.cs
      ReplayToolTests.cs
    Dto/
      JsonShapeTests.cs           date / decimal / hash truncation / null discipline
    McpCommandRegistrationTests.cs  one Spectre smoke test
```

## 4. The six tools

All tool classes are `[McpServerToolType]`. All methods are `async`. All return JSON-serializable DTOs whose shape is fixed by this spec (the SDK's auto-serializer is the wire layer).

### 4.1 `list_instruments`

```csharp
[McpServerTool, Description("List all instruments TradyStrat tracks.")]
public async Task<InstrumentListResponse> List_instruments(CancellationToken ct);
```

Returns:
```jsonc
{
  "instruments": [
    { "ticker": "CON3.L",  "displayName": "Leverage Shares 3x Long Coinbase ETP",
      "currency": "USD", "timezone": "Europe/London", "role": "Focus" },
    { "ticker": "COIN",    "displayName": "Coinbase Global Inc.",
      "currency": "USD", "timezone": "America/New_York", "role": "Context" },
    { "ticker": "BTC-USD", "displayName": "Bitcoin / US Dollar",
      "currency": "USD", "timezone": "UTC", "role": "Context" }
  ]
}
```

Backed by `ListInstrumentsUseCase` plus a `role` field derived from `Tickers:Focus` / `Tickers:Context` config. Tiny payload; Claude is expected to call this once per session and use the result as the ticker namespace for every other tool.

### 4.2 `get_dashboard`

```csharp
[McpServerTool, Description("Snapshot of an instrument: price, indicators, zone, today's AI suggestion, position.")]
public async Task<DashboardSnapshot> Get_dashboard(
    string? instrument = null,   // default: focus ticker (CON3.L)
    string? asOf = null,         // default: today UTC; ISO YYYY-MM-DD
    CancellationToken ct = default);
```

Returns (representative shape; exact fields fixed in `DashboardDtos.cs`):
```jsonc
{
  "ticker": "CON3.L",
  "asOfDate": "2026-05-18",
  "lastClose": { "usd": 24.13, "eur": 22.31, "fxRate": 0.9248 },
  "zone": {
    "overall": "Accumulate",
    "byIndicator": { "bollinger": "LowerHalf", "rsi": "Neutral",
                     "sma": "AboveTrend",      "ichimoku": "BullishCloud" }
  },
  "indicators": {
    "bollinger": { "percentB": 0.34, "upper": 26.5, "mid": 23.1, "lower": 19.7 },
    "rsi":       { "value": 48.2 },
    "sma":       { "sma20": 23.4, "sma50": 22.1, "sma200": 20.6 },
    "ichimoku":  { "tenkan": 23.5, "kijun": 22.8, "spanA": 23.1, "spanB": 21.2 }
  },
  "suggestion": {
    "date": "2026-05-18", "action": "Acquire", "conviction": 7,
    "reasoning": "...one paragraph...",
    "envelopeHash": "a3b1c0e2", "promptVersionHash": "9d4f7e10"
  },
  "position": {
    "qty": 312, "avgCostUsd": 18.42, "marketValueUsd": 7528.56,
    "marketValueEur": 6962.43, "unrealizedPnlUsd": 1779.12, "unrealizedPnlEur": 1645.40
  }
}
```

Backed by `LoadDashboardUseCase` plus a flat-projection mapper. `suggestion` is `null` if no suggestion exists for `asOfDate` (never triggers an AI call).

### 4.3 `query_suggestions`

```csharp
[McpServerTool, Description("Past AI suggestions for an instrument with action, conviction, and outcome.")]
public async Task<SuggestionPage> Query_suggestions(
    string? instrument = null,    // default: focus ticker
    string? from = null,          // ISO date; default: to.AddDays(-90)
    string? to = null,            // ISO date; default: today
    string? action = null,        // "Acquire" | "Trim" | "Hold" | "Wait"; default: all
    int limit = 30,               // [1, 100]; default 30
    CancellationToken ct = default);
```

Returns newest-first:
```jsonc
{
  "instrument": "CON3.L", "from": "2026-02-17", "to": "2026-05-18", "count": 28,
  "items": [
    {
      "date": "2026-05-17", "action": "Acquire", "conviction": 7,
      "envelopeHash": "a3b1c0e2", "promptVersionHash": "9d4f7e10",
      "reasoning": "...short...",
      "forwardReturnPct": null,   // not yet evaluable (< 5 trading days)
      "correct": null
    },
    // ...
  ]
}
```

`forwardReturnPct` and `correct` use the same Domain `ICorrectnessRule` the existing replay path uses (5-day forward return, ±2% threshold). `null` for either field means "not yet evaluable" — never `0` or `false`.

### 4.4 `query_prices`

```csharp
[McpServerTool, Description("Daily OHLCV bars for an instrument, optionally with indicator series.")]
public async Task<PriceSeries> Query_prices(
    string instrument,
    string? from = null,          // ISO date; default: to.AddDays(-90)
    string? to = null,            // ISO date; default: today
    bool withIndicators = false,
    CancellationToken ct = default);
```

Returns:
```jsonc
{
  "instrument": "CON3.L", "from": "2026-02-17", "to": "2026-05-18", "barCount": 65,
  "bars": [
    { "date": "2026-02-17", "open": 19.8, "high": 20.4, "low": 19.5, "close": 20.1, "volume": 12450 },
    // ...
  ],
  "indicators": {  // present iff withIndicators=true, arrays aligned to bars[]
    "rsi":       [null, null, ..., 48.2],
    "sma20":     [null, ..., 23.4],
    "sma50":     [null, ..., 22.1],
    "sma200":    [null, ..., 20.6],
    "bbUpper":   [...], "bbMid": [...], "bbLower": [...],
    "ichimokuTenkan": [...], "ichimokuKijun": [...],
    "ichimokuSpanA":  [...], "ichimokuSpanB": [...]
  }
}
```

**Hard cap: 365 bars per call.** A request whose `[from, to]` window resolves to more than 365 days → `ArgumentException("Date range exceeds 365-day maximum. Narrow the window or make multiple calls.")`. Protects Claude's context budget; the per-instrument bar history in DB is ~2 years.

`null` entries in the indicator arrays mean the indicator's lookback window wasn't satisfied yet (e.g., RSI is `null` for the first 14 bars).

### 4.5 `get_portfolio`

```csharp
[McpServerTool, Description("Current portfolio: per-ticker lots, aggregate value, progress toward goal.")]
public async Task<PortfolioSnapshot> Get_portfolio(
    string? asOf = null,          // ISO date; default: today
    CancellationToken ct = default);
```

Returns:
```jsonc
{
  "asOfDate": "2026-05-18", "fxRate": 0.9248,
  "aggregate": {
    "totalValueEur": 18742.10, "goalEur": 1000000.00,
    "distanceToGoalEur": 981257.90, "progressPct": 1.87
  },
  "positions": [
    {
      "ticker": "CON3.L", "qty": 312, "avgCostUsd": 18.42,
      "marketValueUsd": 7528.56, "marketValueEur": 6962.43,
      "realizedPnlUsd": 220.00, "unrealizedPnlUsd": 1779.12,
      "realizedPnlEur": 203.45, "unrealizedPnlEur": 1645.40
    }
    // ... one per ticker held
  ],
  "trades": [
    { "date": "2026-05-12", "ticker": "CON3.L", "side": "Buy", "qty": 50, "priceUsd": 23.10 },
    // ... full ledger
  ]
}
```

Backed by `PortfolioService` (FIFO lot accounting already exists). `trades` is the full ledger; it's small enough (manual entry only) that pagination isn't needed.

### 4.6 `get_replay_report`

```csharp
[McpServerTool, Description("Re-run the AI prompt against historical snapshots in dry-run mode and return hit-rate / forward-return stats.")]
public async Task<ReplayReport> Get_replay_report(
    string instrument,
    string from,                  // ISO date, inclusive
    string to,                    // ISO date, inclusive
    CancellationToken ct = default);
```

Wraps `ReplaySuggestionsUseCase` with `persist = false, force = false` hard-coded. The MCP boundary is read-only; we never let Claude trigger a database write through this path. Returns the existing `ReplayReport` DTO shape used by the `replay` CLI command (per-action `count` / `hitRatePct` / `avgFwdReturnPct` / `avgConviction`, overall aggregate, conviction-weighted score, distinct prompt-version hashes, range echo).

## 5. Architecture & wiring

### 5.1 `McpCommand`

A Spectre `AsyncCommand` with no options (an empty `CommandSettings`-equivalent), registered as `"mcp"` in `Program.cs`. Its body:

```csharp
public override async Task<int> ExecuteAsync(CommandContext context, EmptySettings _)
{
    var builder = Host.CreateApplicationBuilder();
    builder.Logging.AddConsole(o =>
        o.LogToStandardErrorThreshold = LogLevel.Trace);

    AppManager.ConfigureServices(builder.Services, builder.Configuration, modules => modules
        .AddFromAssemblyOf<ApplicationAssemblyMarker>()
        .AddFromAssemblyOf<InfrastructureAssemblyMarker>(t =>
            t != typeof(PriceFeedBackgroundInfrastructureModule)));

    builder.Services
        .AddMcpServer()
        .WithStdioServerTransport()
        .WithTools<InstrumentTool>()
        .WithTools<DashboardTool>()
        .WithTools<SuggestionTool>()
        .WithTools<PriceTool>()
        .WithTools<PortfolioTool>()
        .WithTools<ReplayTool>();

    using var innerHost = builder.Build();
    await innerHost.RunAsync();   // blocks until stdin EOF
    return 0;
}
```

The module composition mirrors the outer CLI's (same exclusion of `PriceFeedBackgroundInfrastructureModule`). The inner host is fresh: separate DI container, separate logging config, separate lifetime. The outer host (started by `Program.cs` before Spectre dispatches) is still running while the inner host runs, but is idle.

### 5.2 `Program.cs` changes

Two edits:

1. Add stderr-only logging globally (one line near the top of the host builder configuration):
   ```csharp
   builder.Logging.AddConsole(o => o.LogToStandardErrorThreshold = LogLevel.Trace);
   ```
   This affects every CLI command. `replay` keeps writing through `AnsiConsole` (stdout), so its user-visible output is unaffected; `ILogger` output simply moves to stderr where it belonged anyway.

2. Register `McpCommand`:
   ```csharp
   c.AddCommand<McpCommand>("mcp")
    .WithDescription("Run the read-only TradyStrat MCP server over stdio.");
   ```

No other changes to the outer host.

### 5.3 Tool class shape

Each tool class is constructor-injected with its use cases and an `ILogger<T>`. Methods do (1) input guard, (2) call use case, (3) project to DTO. Example skeleton (`PriceTool`):

```csharp
[McpServerToolType]
public sealed class PriceTool(
    IReadRepositoryBase<Instrument> instruments,
    IReadRepositoryBase<PriceBar> bars,
    IIndicatorEngine indicators,
    IClock clock,
    ILogger<PriceTool> logger)
{
    private const int MaxBars = 365;

    [McpServerTool, Description("Daily OHLCV bars for an instrument, optionally with indicator series.")]
    public async Task<PriceSeries> Query_prices(
        string instrument, string? from = null, string? to = null,
        bool withIndicators = false, CancellationToken ct = default)
    {
        var inst = await ResolveInstrumentOrThrow(instrument, ct);
        var (f, t) = ResolveDateRange(from, to, defaultBack: 90);
        if ((t.DayNumber - f.DayNumber + 1) > MaxBars)
            throw new ArgumentException(
                $"Date range exceeds {MaxBars}-day maximum. Narrow the window or make multiple calls.");

        try
        {
            var series = await LoadSeries(inst, f, t, withIndicators, ct);
            return DtoMappers.ToPriceSeries(series);
        }
        catch (TradyStratException ex)
        {
            logger.LogWarning(ex, "query_prices failed for {Ticker} {From}..{To}", instrument, f, t);
            throw new McpException(ex.Message);
        }
    }
}
```

Guard helpers (`ResolveInstrumentOrThrow`, `ResolveDateRange`) live in a shared `Mcp/Tools/Guards.cs` so the six tool classes share the same validation messages and date defaults.

## 6. DTO conventions (the wire contract)

Fixed by this spec; deviation = test failure.

| Concern | Rule |
|---|---|
| Dates | `DateOnly` serialized as ISO `"YYYY-MM-DD"` strings. Configured via `JsonSerializerOptions` registered alongside the MCP server. |
| Money | `decimal` serialized as JSON number (never string). When both currencies are relevant, separate `usd` and `eur` fields appear at the same level, with `fxRate` echoed at the nearest aggregate. |
| Hashes | Truncated to first 8 characters (matches `ReplayCommand.Render`). Full hashes never leave the server. |
| Missing values | `null`, never `0`, `""`, `"Unknown"`, or `false`. Applies to `forwardReturnPct`, `correct`, the suggestion block in `get_dashboard`, and indicator entries before their lookback window completes. |
| Enums | Serialized as PascalCase strings (`"Acquire"`, `"Trim"`, `"Hold"`, `"Wait"`, `"Focus"`, `"Context"`). Configured via a `JsonStringEnumConverter`. |
| Ticker casing | Pass-through. `CON3.L` and `BTC-USD` stay exactly as in DB. Input matching is case-sensitive — Claude's `list_instruments` result is the canonical source. |

`DtoMappers.cs` is the only place where Application / Domain types touch wire-format types. Razor pages and CLI commands keep using their own existing projections; nothing else routes through these DTOs.

## 7. Error handling

Three exception classes Claude can see:

| Exception | Source | Message contract |
|---|---|---|
| `ArgumentException` | Input guards in tool methods | Specific, actionable. Examples: `"Unknown instrument 'XYZ'. Call list_instruments to see valid tickers."`, `"Date range exceeds 365-day maximum. Narrow the window or make multiple calls."`, `"limit must be between 1 and 100 (got 250)."` |
| `McpException` | Translated from `TradyStratException` in `catch` blocks | Original `TradyStratException.Message` preserved. These are already curated for human display in the dashboard. |
| (generic) | Anything else uncaught | Bubble through SDK as a generic error. Stack trace logged to stderr at `Error` level with tool name + redacted inputs. |

Validation rules per tool (failing → `ArgumentException`):

- `instrument` (when supplied): must match one of the tickers from `list_instruments` (lookup against the cached `ListInstrumentsUseCase` result).
- `from`, `to`, `asOf`: must parse as ISO `YYYY-MM-DD`. `from <= to` required.
- `query_prices` window: `<= 365` days.
- `query_suggestions.limit`: `1..100` inclusive.
- `query_suggestions.action`: one of `"Acquire"`, `"Trim"`, `"Hold"`, `"Wait"` if non-null.
- `get_replay_report.from` and `to`: both required (no defaults).

Pre-condition validation explicitly does **not** live in the use cases — the use cases assume valid inputs because Razor pages validate before calling. The MCP boundary is a new edge; it owns its own validation.

## 8. Testing

New project: `TradyStrat.Cli.Tests` (xunit.v3, Shouldly, `TradyStrat.TestKit`). Three layers, no more:

**8.1 Tool-method tests** (one file per tool class). For each tool method, cover:
- Happy path with realistic fixture data, asserting on the returned DTO's shape.
- Each input-guard rule from §7 — bad ticker, bad date range, out-of-bounds `limit`, > 365-day window, unknown `action`. Assert the exact `ArgumentException` message text (Claude's UX depends on it).
- One exception-translation test per tool: stub a use case to throw a typed `TradyStratException` child; assert the tool re-throws as `McpException` with the same message.

DI wiring per test mirrors the inner host (real modules + TestKit stubs for `IPriceFeed` / `IFxProvider` / `IChatClient` / `IClock`). Pattern is identical to `Application.Tests`, so test infra cost is near zero.

**8.2 DTO shape tests** (one focused file, `JsonShapeTests.cs`). `JsonSerializer.Serialize` round-trip tests pinning down:
- `DateOnly` → `"YYYY-MM-DD"` (not `"2026-05-18T00:00:00"`).
- `decimal` stays a JSON number (not string).
- Enums use PascalCase strings.
- 8-char hash truncation actually fires.
- `null` propagates for missing `forwardReturnPct` / `correct`.

These catch DTO drift on the next refactor.

**8.3 Spectre registration smoke test** (one test). `CommandApp.Configure(...)` registers `"mcp"`; calling `app.Configure(...)` and inspecting the configured commands list returns it. We don't actually run the MCP loop in tests.

**Explicitly out of scope for tests:**
- Stdio protocol / JSON-RPC framing. SDK is the trusted boundary.
- Re-testing the use cases. Already covered in `Application.Tests`.
- A real Claude client end-to-end. Validated manually per §10.

## 9. Performance, scaling, and limits

Not interesting for v1, but called out so future work has numbers.

- **Cold start:** the inner host's startup is dominated by EF model construction + module composition. Expected: < 500ms on a Mac with the SQLite DB warm.
- **Per-call latency:** every tool is a SQL read against SQLite (no external HTTP). Expected p99 < 50ms for the bounded payloads.
- **Memory:** two hosts coexist while `mcp` runs. Outer host is idle (no `PriceFeedBackgroundInfrastructureModule`). Inner host plus the EF context plus 2 years of bars for 3 tickers fits comfortably in < 200 MB resident.
- **Concurrency:** the SDK serializes tool calls per session, but a future change could make them concurrent. Tool methods are written to be safe under concurrent calls (no shared mutable state in the tool classes themselves; use cases are already `Scoped` and the DI scope per call is handled by the SDK).

## 10. Manual validation (the "did this actually work" checklist)

After implementation merges, this is the runbook for confirming the server works end-to-end. Not automated.

1. `dotnet build` — solution builds.
2. `dotnet run --project TradyStrat.Cli -- mcp` — server starts, writes nothing to stdout, no errors on stderr.
3. Add to `~/Library/Application Support/Claude/claude_desktop_config.json`:
   ```jsonc
   {
     "mcpServers": {
       "tradystrat": {
         "command": "dotnet",
         "args": ["run", "--project", "/absolute/path/TradyStrat.Cli", "--", "mcp"]
       }
     }
   }
   ```
4. Restart Claude Desktop. Ask: "What instruments does TradyStrat track?" → Claude calls `list_instruments` and reports the three tickers.
5. Ask: "What's today's suggestion for CON3.L?" → Claude calls `get_dashboard` and reports the suggestion block.
6. Ask: "How am I doing toward the goal?" → Claude calls `get_portfolio` and reports the progress.
7. Ask: "What was the hit rate of suggestions for COIN over the last 60 days?" → Claude calls `get_replay_report` and reports the conviction-weighted score.
8. Confirm no rows were written to the DB during steps 4–7 (`sqlite3 ~/Library/Application\ Support/TradyStrat/tradystrat.db` — `select max(CreatedAt) from Suggestion;` is unchanged).

## 11. Out of scope for v1

Listed so they don't drift in.

- **Writes of any kind.** No `log_trade`, no `force_refetch_suggestion`, no `update_setting`. Dashboard remains the source of truth for mutations.
- **MCP Resources concept.** Everything is exposed as Tools. The instrument list is a tool, not a resource.
- **MCP Prompts concept.** No pre-baked prompt templates exposed via MCP. The daily-suggestion prompt stays inside `SuggestionService`.
- **HTTP / SSE transport.** Stdio only.
- **Authentication / authorization.** Inherits the local-only trust model.
- **Streaming responses.** Tool methods return one value. No partial results.
- **Multi-instrument batch tools.** Claude makes three calls; data is cached at the use-case layer.
- **Configuration surface on the `mcp` command.** No `[CommandOption]`s in v1.
- **Cross-cutting "summarize" tools.** Claude composes those itself from the six primitives.

## 12. Likely v2+ candidates

Capture-but-don't-build.

- `summarize_recent_activity` if Claude keeps fetching the same three things together.
- Promoting `list_instruments` to an MCP Resource if Claude Desktop's Resource UX matures.
- Multi-instrument `get_replay_report`.
- An HTTP-transport variant (`mcp serve --http`) gated on adding auth.
- A narrow set of safe writes (`force_refetch_suggestion`) if the read-only stance feels too passive in practice.
