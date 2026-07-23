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
| Tool naming convention | **MCP tool names are `snake_case`.** C# method names follow .NET convention (`PascalCase`); each `[McpServerTool]` carries an explicit name attribute to publish the snake_case form. Reason: snake_case is the MCP-ecosystem convention, and we don't want the C# casing to leak to Claude. |
| Tool registration | **Explicit `.WithTools<T>()` per class**, not `WithToolsFromAssembly()`. Future tool classes are not auto-published. |
| Hosting model | **Inner host inside `McpCommand.ExecuteAsync`.** Fresh `Host.CreateApplicationBuilder`, re-composes the same modules the outer CLI uses (minus `PriceFeedBackgroundInfrastructureModule`), registers MCP services via `McpCliModule`, `await innerHost.RunAsync(ct)` blocks until stdin EOF. |
| MCP wiring | **`McpCliModule : IAppModule`** owns every `AddMcpServer().WithTools<T>(...)` call and the filter-pipeline registration. Aligns with the existing module-per-feature convention. |
| Stdio hygiene | **All `ILogger` output to stderr, globally for the CLI.** One-line `Logging.AddConsole(o => o.LogToStandardErrorThreshold = LogLevel.Trace)` in `Program.cs`. The `mcp` path never writes to stdout outside the MCP protocol. |
| `AnsiConsole` discipline | **No `AnsiConsole.*` calls anywhere under `Mcp/`.** Code review check: a grep for `AnsiConsole` in that folder must return empty. |
| Error model | **Centralized in an `McpExceptionTranslationFilter` (Decorator).** `TradyStratException` → `McpException` translation lives in one filter applied to every tool, **not** in per-tool `try/catch`. Input-shape `ArgumentException`s are translated to `McpException` by the same filter. Unexpected exceptions propagate; SDK reports generic error and the filter logs the stack to stderr. |
| Filter pipeline | **Three call-tool filters via `WithRequestFilters(rb => rb.AddCallToolFilter(...))`:** `McpLoggingFilter` (outermost — request log + duration + outcome), `McpTimeoutFilter` (30s default per tool call), `McpExceptionTranslationFilter` (innermost — converts thrown exceptions to MCP `CallToolResult { IsError = true }`). Using the tool-specific filter (not the broader message filter) means listing tools and unrelated MCP traffic skip the pipeline. |
| Input validation | **Per-tool guard block at method entry**, raised as `ArgumentException`. The error-translation filter then converts these to `McpException` with the original message. Ticker existence, `from <= to`, `limit` ∈ `[1, 100]`, `query_prices` window ≤ 365 days. |
| JSON serialization | **`JsonSerializerOptions` registered explicitly** and passed to every `.WithTools<T>(jsonSerializerOptions: opts)` call. Options: `PropertyNamingPolicy = JsonNamingPolicy.CamelCase`, `JsonStringEnumConverter`, custom `DateOnlyConverter` (ISO `YYYY-MM-DD`). |
| DTO-vs-Domain policy | **Always introduce an MCP DTO; never reuse Domain / Application types directly on the wire.** Reason: the MCP contract must stay stable across Application-layer refactors. The single exception is `ReplayReport`, which is already shaped for external consumption and is reused as-is. |
| Side-effect policy | **Never trigger AI calls or external writes to satisfy a query.** Missing data → `null`, not "go fetch it". |
| Cancellation rule | **Every tool method threads `CancellationToken` through to its use case and on to the DB / external calls.** The timeout filter relies on this. Manual rule, enforced by code review (no test catches a forgotten `ct`). |
| Tool dependencies | **Tools depend on use cases, not on repositories or services directly.** Mirrors the Razor-pages convention. New use cases (`QuerySuggestionsUseCase`, `GetPriceSeriesUseCase`) are created as part of this spec where existing ones don't fit — see §5.6. |
| Configuration surface | **None in v1.** `mcp` has no `[CommandOption]`s. Caps / behaviour are hard-coded constants. |
| Test project | **New `TradyStrat.Cli.Tests`** (xunit.v3 + Shouldly + `TradyStrat.TestKit`). Follows the layer-per-test-project convention. |
| Protocol-level tests | **None.** SDK is the trusted boundary. Filter behaviour *is* tested. |
| Feature flag | **None.** New subcommand; nothing else changes for users of the dashboard or the existing `replay` command. |

## 3. Project layout

New files. No existing files move. New use cases / specs live with their feature in `TradyStrat.Application/`, not under `Mcp/`.

```
TradyStrat.Cli/
  Commands/
    ReplayCommand.cs              (unchanged)
    McpCommand.cs                 NEW — Spectre AsyncCommand, registers "mcp"
  Mcp/                            NEW folder, all CLI-side MCP code lives here
    McpCliModule.cs               NEW — IAppModule; registers MCP server + tools + filters + JSON opts
    Tools/
      InstrumentTool.cs           list_instruments
      DashboardTool.cs            get_dashboard
      SuggestionTool.cs           query_suggestions
      PriceTool.cs                query_prices
      PortfolioTool.cs            get_portfolio
      ReplayTool.cs               get_replay_report
      Guards.cs                   shared input-resolution helpers (instrument lookup, date defaults)
    Filters/
      McpLoggingFilter.cs         outermost — structured per-call logging + duration
      McpTimeoutFilter.cs         per-call 30s cancellation budget
      McpExceptionTranslationFilter.cs   innermost — TradyStratException/ArgumentException → McpException
    Dto/
      InstrumentDtos.cs
      DashboardDtos.cs
      SuggestionDtos.cs
      PriceDtos.cs
      PortfolioDtos.cs
      ReplayDtos.cs               (re-exports the existing ReplayReport — see §6 exception)
    Mapping/                      one mapper file per Dto file; each is a small Strategy
      InstrumentMapper.cs
      DashboardMapper.cs
      SuggestionMapper.cs
      PriceMapper.cs
      PortfolioMapper.cs
    Serialization/
      McpJsonSerializerOptions.cs  static factory returning the shared JsonSerializerOptions
      DateOnlyJsonConverter.cs     ISO YYYY-MM-DD round-trip
  Program.cs                      MODIFIED — add Logging.AddConsole(stderr), register McpCommand

TradyStrat.Application/
  Indicators/
    IIndicatorEngine.cs           NEW — interface extracted from sealed IndicatorEngine (see §5.6)
  PriceFeed/
    Specifications/
      PriceBarsInRangeSpec.cs     NEW — bars by instrument + inclusive date range (see §5.6)
    UseCases/
      GetPriceSeriesUseCase.cs    NEW — bars (+ optional indicators) for query_prices (see §5.6)
  AiSuggestion/
    Specifications/
      SuggestionsQuerySpec.cs     NEW — newest-first, instrument + range + optional action + limit
    UseCases/
      QuerySuggestionsUseCase.cs  NEW — backs query_suggestions

TradyStrat.Cli.Tests/             NEW csproj
  TradyStrat.Cli.Tests.csproj
  Mcp/
    McpCliModuleTests.cs          asserts MCP services + filters resolve from a built host
    Tools/
      InstrumentToolTests.cs
      DashboardToolTests.cs
      SuggestionToolTests.cs
      PriceToolTests.cs
      PortfolioToolTests.cs
      ReplayToolTests.cs
    Filters/
      McpExceptionTranslationFilterTests.cs   TradyStratException → McpException, ArgumentException → McpException, unrelated propagates
      McpTimeoutFilterTests.cs                cancels after 30s, propagates ct
      McpLoggingFilterTests.cs                logs once per call with tool + duration + outcome
    Serialization/
      JsonShapeTests.cs           date / decimal / hash truncation / null / camelCase discipline
    McpCommandRegistrationTests.cs  one Spectre smoke test
```

## 4. The six tools

All tool classes are `[McpServerToolType]`. All methods are `async`. All return JSON-serializable DTOs whose shape is fixed by this spec (the SDK's auto-serializer is the wire layer).

**On the C# signatures below:** the spec uses an illustrative `Tool(Name = "...")` attribute form to show the snake_case MCP name. The actual SDK attribute spelling (e.g. `[McpServerTool(Name = "...")]` vs an alternate property) is an implementation detail confirmed at code time — the contract here is that **each tool method publishes the exact snake_case name shown**, regardless of which attribute property the SDK uses.

### 4.1 `list_instruments`

```csharp
[McpServerTool(Name = "list_instruments"), Description("List all instruments TradyStrat tracks.")]
public async Task<InstrumentListResponse> ListInstruments(CancellationToken ct);
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
[McpServerTool(Name = "get_dashboard"),
 Description("Snapshot of an instrument: price, indicators, zone, today's AI suggestion, position.")]
public async Task<DashboardSnapshot> GetDashboard(
    string? instrument = null,   // default: focus ticker from Tickers:Focus config
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
    "qty": 312, "costBasisEur": 5740.20,
    "marketValueEur": 6962.43, "unrealizedPnlEur": 1222.23, "realizedPnlEur": 0
  }
}
```

Backed by `LoadDashboardUseCase` plus the `DashboardMapper` Strategy. `suggestion` is `null` if no suggestion exists for `asOfDate` (never triggers an AI call).

**`IsHistorical` wiring.** `LoadDashboardInput` is `(DateOnly TargetDate, bool IsHistorical)`. The tool sets `IsHistorical = asOf != clock.Today()`. This matches the dashboard's existing time-travel behaviour: historical mode skips fresh-suggestion generation, which is exactly what the MCP read-only contract requires.

### 4.3 `query_suggestions`

```csharp
[McpServerTool(Name = "query_suggestions"),
 Description("Past AI suggestions for an instrument with action, conviction, and outcome.")]
public async Task<SuggestionPage> QuerySuggestions(
    string? instrument = null,    // default: focus ticker from Tickers:Focus config
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
[McpServerTool(Name = "query_prices"),
 Description("Daily OHLCV bars for an instrument, optionally with indicator series.")]
public async Task<PriceSeries> QueryPrices(
    string instrument,            // required — see note below
    string? from = null,          // ISO date; default: to.AddDays(-90)
    string? to = null,            // ISO date; default: today
    bool withIndicators = false,
    CancellationToken ct = default);
```

**Why `instrument` is required here** (and not in `get_dashboard` / `query_suggestions`): bar history is large and the cap is 365 bars per call. Defaulting silently to the focus ticker risks Claude pulling a giant series when it actually wanted a context ticker. Forcing the argument is a small friction that prevents that class of confusion.

Returns:
```jsonc
{
  "instrument": "CON3.L", "from": "2026-02-17", "to": "2026-05-18", "barCount": 65,
  "bars": [
    { "date": "2026-02-17", "open": 19.8, "high": 20.4, "low": 19.5, "close": 20.1, "volume": 12450 },
    // ...
  ],
  "indicators": {  // present iff withIndicators=true, arrays aligned to bars[]
    "rsi":          [null, null, ..., 48.2],
    "bollingerMid": [null, ..., 23.1],
    "sma200":       [null, ..., 20.6],
    "ichimoku":     [null, ..., 22.4]
  }
}
```

**Hard cap: 365 bars per call.** A request whose `[from, to]` window resolves to more than 365 days → `ArgumentException("Date range exceeds 365-day maximum. Narrow the window or make multiple calls.")`. Protects Claude's context budget; the per-instrument bar history in DB is ~2 years.

`null` entries in the indicator arrays mean the indicator's lookback window wasn't satisfied yet (e.g., RSI is `null` for the first 14 bars).

**Indicator surface — current limitations.** The spec originally promised eleven indicator arrays. Implementation reality (Phase 3.4) discovered that `IIndicatorEngine.HistoryFor` only exposes a coarse `IndicatorKind` enum (`Rsi`, `Bollinger`, `Ichimoku`, `Sma50`, `Sma200` — no `Sma20`, no granular Bollinger upper/lower, no granular Ichimoku components). The current `BollingerHistoryProvider` returns only midline values per bar; `IchimokuHistoryProvider` returns a close-price proxy as a placeholder. Surfacing the full eleven-field shape would require extending the indicator engine (new `IndicatorKind` values + new history providers) — a meaningful scope addition that belongs in its own spec. For v1 we ship the four available arrays (`rsi`, `bollingerMid`, `sma200`, `ichimoku`) and document the gap. `Sma50` is structurally available via `IndicatorKind.Sma50` but its history provider isn't registered today; once wired up, adding `sma50` is a one-line change.

### 4.5 `get_portfolio`

```csharp
[McpServerTool(Name = "get_portfolio"),
 Description("Current portfolio: per-ticker lots, aggregate value, progress toward goal.")]
public async Task<PortfolioSnapshot> GetPortfolio(
    string? asOf = null,          // ISO date; default: today
    CancellationToken ct = default);
```

Returns:
```jsonc
{
  "asOfDate": "2026-05-18",
  "aggregate": {
    "totalValueEur": 18742.10,
    "costBasisEur": 15820.45,
    "unrealizedPnlEur": 2921.65,
    "realizedPnlEur": 0,
    "goalEur": 1000000.00,
    "distanceToGoalEur": 981257.90,
    "progressPct": 1.87
  },
  "positions": [
    {
      "ticker": "CON3.L", "currency": "USD", "qty": 312,
      "costBasisEur": 5740.20, "marketValueEur": 6962.43,
      "unrealizedPnlEur": 1222.23, "realizedPnlEur": 0
    }
    // ... one per ticker held
  ],
  "trades": [
    { "date": "2026-05-12", "ticker": "CON3.L", "side": "Buy", "qty": 50,
      "pricePerShareEur": 21.36, "feesEur": 1.50 }
    // ... newest-first, capped at 500
  ],
  "tradesTruncated": false
}
```

Backed by `PortfolioService.SnapshotAsync(DateOnly asOf, IReadOnlyDictionary<int, (decimal, string, string)> priceByInstrument, decimal goalEur, CancellationToken)` plus the `PortfolioMapper` Strategy. `trades` is newest-first and **capped at 500 rows**, with a `tradesTruncated` flag set true when the cap fires. Today's ledger is small (manual entry only), but a reference architecture should bound every list output up front rather than ship an unbounded field that a future user will trip over.

**Currency surface — EUR only for positions and aggregates.** The spec originally promised dual-currency (USD + EUR) on every per-position figure. Implementation reality (Phase 4): `TradyStrat.Domain.PositionRow` is EUR-only — costs, market values, and P&L are computed in EUR by `PortfolioService` (using FX at snapshot time). Surfacing USD would require either re-routing through `FxConverter` in the mapper or extending `PortfolioService` to return both — meaningful scope beyond v1. The `lastClose` field in `get_dashboard` keeps both currencies because price bars are natively in USD; everywhere else we ship EUR-only and document the asymmetry. `currency` is preserved per position so Claude knows what the underlying instrument trades in.

### 4.6 `get_replay_report`

```csharp
[McpServerTool(Name = "get_replay_report"),
 Description("Re-run the AI prompt against historical snapshots in dry-run mode and return hit-rate / forward-return stats.")]
public async Task<ReplayReport> GetReplayReport(
    string instrument,
    string from,                  // ISO date, inclusive
    string to,                    // ISO date, inclusive
    CancellationToken ct = default);
```

Wraps `ReplaySuggestionsUseCase` with `Persist = false, Force = false` hard-coded (verified against `ReplaySuggestionsInput(int InstrumentId, DateOnly Since, DateOnly Until, bool Persist, bool Force)` — the use case already supports dry-run mode). The MCP boundary is read-only; we never let Claude trigger a database write through this path.

Returns the existing `ReplayReport` DTO shape used by the `replay` CLI command (per-action `count` / `hitRatePct` / `avgFwdReturnPct` / `avgConviction`, overall aggregate, conviction-weighted score, distinct prompt-version hashes, range echo). This is the **one DTO exception** in §6 — `ReplayReport` was already designed for external display by the `replay` CLI and is reused as-is.

**Empty-range behaviour.** If no suggestions exist in `[from, to]` for the instrument, return an empty report (per-action counts all zero, overall count zero, `convictionWeightedScore: null`, `distinctPromptVersionHashes: []`). Never throw — Claude can describe a no-data result fine.

## 5. Architecture & wiring

### 5.1 `McpCommand`

A Spectre `AsyncCommand` with no options (an empty `CommandSettings`), registered as `"mcp"` in `Program.cs`. The body is intentionally tiny — it delegates host construction to `AppManager` and MCP wiring to `McpCliModule`:

```csharp
public override async Task<int> ExecuteAsync(CommandContext context, EmptySettings _)
{
    var builder = Host.CreateApplicationBuilder();
    builder.Logging.AddConsole(o =>
        o.LogToStandardErrorThreshold = LogLevel.Trace);

    AppManager.ConfigureServices(builder.Services, builder.Configuration, modules => modules
        .AddFromAssemblyOf<ApplicationAssemblyMarker>()
        .AddFromAssemblyOf<InfrastructureAssemblyMarker>(t =>
            t != typeof(PriceFeedBackgroundInfrastructureModule))
        .Add<McpCliModule>());

    using var innerHost = builder.Build();
    await innerHost.RunAsync();   // blocks until stdin EOF
    return 0;
}
```

The module composition mirrors the outer CLI's (same exclusion of `PriceFeedBackgroundInfrastructureModule`) plus the new `McpCliModule`. The inner host is fresh: separate DI container, separate logging config, separate lifetime. The outer host (started by `Program.cs` before Spectre dispatches) is still running while the inner host runs, but is idle.

**Tradeoff acknowledged:** the inner-host model means MCP tools can't reach hosted services running in the outer host. For v1 (which doesn't need any) this is fine; if a future tool ever needs to share state with an outer-host singleton, we'll revisit by either lifting the MCP services into the outer host (with a registration gate on `args[0] == "mcp"`) or having the shared service expose itself via a cross-process boundary.

### 5.2 `McpCliModule` (the wiring locus)

The only file that calls `AddMcpServer()` and the only file that registers tools or filters. Implements `IAppModule`:

```csharp
public sealed class McpCliModule : IAppModule
{
    public void ConfigureServices(IServiceCollection services, IConfiguration config)
    {
        var jsonOptions = McpJsonSerializerOptions.Create();

        services
            .AddMcpServer()
            .WithStdioServerTransport()
            // Filters: outermost first. Logging wraps timeout wraps translation.
            .WithRequestFilters(rb => rb
                .AddCallToolFilter(McpLoggingFilter.Handle)
                .AddCallToolFilter(McpTimeoutFilter.Handle)
                .AddCallToolFilter(McpExceptionTranslationFilter.Handle))
            // Tools: each pass receives the shared JsonSerializerOptions.
            .WithTools<InstrumentTool>(jsonSerializerOptions: jsonOptions)
            .WithTools<DashboardTool>(jsonSerializerOptions: jsonOptions)
            .WithTools<SuggestionTool>(jsonSerializerOptions: jsonOptions)
            .WithTools<PriceTool>(jsonSerializerOptions: jsonOptions)
            .WithTools<PortfolioTool>(jsonSerializerOptions: jsonOptions)
            .WithTools<ReplayTool>(jsonSerializerOptions: jsonOptions);
    }
}
```

**Filter ordering note.** `WithRequestFilters(...AddCallToolFilter(...))` is middleware-style: first registered = outermost wrapper. So a tool call flows:

```
JSON-RPC → Logging → Timeout → ExceptionTranslation → tool method → DTO → JSON-RPC
```

Logging wraps everything (it sees the outcome). Timeout wraps the tool (cancels long calls). Exception translation is innermost so it can catch raw exceptions before they escape the tool method.

### 5.3 `Program.cs` changes

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

### 5.4 Filter pipeline (Decorator)

The three filters in `Mcp/Filters/` form a Decorator chain around every tool invocation. This is the same pattern the project already uses for `IChatClient` decorators (per README §17). Each filter has a static `Handle` method matching the SDK's `WithRequestFilters(...AddCallToolFilter(...))` signature.

**`McpLoggingFilter` (outermost).** Wraps every call with a structured log scope: tool name, sanitized input shape (parameter names + types, never values), start time. On completion logs `Information` with duration + outcome (`ok` / `mcp_error` / `cancelled` / `unexpected`). On exception, logs `Warning` (`McpException`) or `Error` (anything else). This is the spec's observability story.

**`McpTimeoutFilter`.** Composes a `CancellationTokenSource` linked to the incoming `CancellationToken` with a 30-second `CancelAfter`. The composed token is passed down. A timeout produces a `McpException("Tool call exceeded 30s timeout.")` — distinct from a client-initiated cancellation, which propagates `OperationCanceledException` upward unchanged.

**`McpExceptionTranslationFilter` (innermost).** The single place that converts thrown exceptions to MCP error responses:

| Caught | Translated to |
|---|---|
| `McpException` | rethrown unchanged (already-formed MCP error) |
| `TradyStratException` (or any child) | `McpException(ex.Message)` — message preserved |
| `ArgumentException` | `McpException(ex.Message)` — input-validation messages already carry actionable text |
| `OperationCanceledException` | rethrown unchanged (caller cancelled, not a tool error) |
| anything else | rethrown unchanged — SDK reports generic error; the logging filter has already logged the stack on its way out |

This replaces what would otherwise be a `try/catch` block duplicated across six tool methods. Adding a new tool requires no exception-handling code at all.

### 5.5 DI scoping and cancellation

**Scoping.** The MCP SDK creates a fresh DI scope per tool invocation. Combined with the existing module registrations (use cases as `Scoped`, `AppDbContext` as `Scoped`), this means every tool call gets a fresh `DbContext`, fresh use case instances, and proper disposal at the end of the call. No singleton-`DbContext` foot-guns. Stated explicitly because it's load-bearing for SQLite + EF Core in a long-lived process.

**Cancellation.** Every tool method has a `CancellationToken ct = default` parameter. The SDK passes the (filter-composed) token from the JSON-RPC layer. Every tool method **must** thread `ct` to its use case, which must thread it to repositories / external HTTP. Rule: any new tool whose code path doesn't pass `ct` end-to-end fails review. There is no automated test for this — it's a convention, like async-method naming.

### 5.6 New Application-layer additions required

The tool surface in §4 implies three things that don't exist yet. Implementation must add them.

**5.6.1 Interface extraction: `IIndicatorEngine`.** Today `IndicatorEngine` is a sealed concrete class registered in its module. Extract the public surface as an interface:

```csharp
public interface IIndicatorEngine
{
    Task<IndicatorReading> ComputeFor(string ticker, CancellationToken ct);
    Task<IndicatorReading> ComputeFor(string ticker, DateOnly asOf, CancellationToken ct);
    Task<IndicatorSeries> HistoryFor(string ticker, IndicatorKind kind, int lastN, CancellationToken ct);
    Task<IndicatorSeries> HistoryFor(string ticker, IndicatorKind kind, int lastN, DateOnly asOf, CancellationToken ct);
}
```

Concrete class implements the interface; module registers `IIndicatorEngine → IndicatorEngine`. Existing callers (today there are none outside its own module) are unaffected — the concrete class stays sealed. Reason for adding the interface as part of this spec: testability of `PriceTool`, and alignment with the project's "depend on abstractions at module boundaries" convention.

**5.6.2 New specifications.**

`PriceBarsInRangeSpec(int instrumentId, DateOnly from, DateOnly to)` — bars by instrument with inclusive date range, ordered ascending by date. Lives in `TradyStrat.Application/PriceFeed/Specifications/`. Combines the two existing specs (`PriceBarsAsOfSpec`, `PriceBarsSinceSpec`) into the shape `query_prices` actually needs.

`SuggestionsQuerySpec(int instrumentId, DateOnly from, DateOnly to, SuggestionAction? action, int limit)` — suggestions by instrument with inclusive date range, optional action filter, ordered **descending** by `ForDate`, capped at `limit`. Lives in `TradyStrat.Application/AiSuggestion/Specifications/`. The existing `SuggestionsInRangeSpec` orders ascending and has no action filter — it stays put for its existing consumers.

**5.6.3 New use cases.**

`GetPriceSeriesUseCase` (in `TradyStrat.Application/PriceFeed/UseCases/`). Input: `(int InstrumentId, DateOnly From, DateOnly To, bool WithIndicators)`. Output: a structured series record with bars + optional indicator arrays aligned by date. Delegates to `IReadRepositoryBase<PriceBar>` via `PriceBarsInRangeSpec`, plus `IIndicatorEngine.HistoryFor` per indicator kind when `WithIndicators` is set. Wraps the read-fan-out in a single `UseCaseBase.ExecuteAsync` for timing/logging consistency.

`QuerySuggestionsUseCase` (in `TradyStrat.Application/AiSuggestion/UseCases/`). Input: `(int InstrumentId, DateOnly From, DateOnly To, SuggestionAction? Action, int Limit)`. Output: list of suggestions with computed `forwardReturnPct` + `correct` per row (the same `ICorrectnessRule` the existing replay path uses). Delegates to `IReadRepositoryBase<Suggestion>` via `SuggestionsQuerySpec` plus the existing forward-return calculation helper. The `null` rules for not-yet-evaluable forward returns are implemented here, not in the tool.

**Why these belong in Application, not in `Mcp/`.** The Razor pages (or any future caller) might want to use the same queries. Putting them in Application keeps the layering clean and means tests in `Application.Tests` cover the use cases independently of MCP.

### 5.7 Tool class shape

Each tool class is `[McpServerToolType]`, sealed, constructor-injected with **use cases only** (no direct repository or service dependencies) plus the shared `Guards` helper and an `ILogger<T>` used only for tool-method-internal events (the filters handle request-level logging). Methods do (1) input guard via `Guards`, (2) call use case, (3) project to DTO via the feature's `Mapper` Strategy. **No `try/catch`** — the filter pipeline owns translation.

Example (`PriceTool`):

```csharp
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
        string instrument, string? from = null, string? to = null,
        bool withIndicators = false, CancellationToken ct = default)
    {
        var inst = await guards.ResolveInstrumentOrThrow(instrument, ct);
        var (f, t) = guards.ResolveDateRange(from, to, defaultBack: 90, clockToday: clock.Today());
        if ((t.DayNumber - f.DayNumber + 1) > MaxBars)
            throw new ArgumentException(
                $"Date range exceeds {MaxBars}-day maximum. Narrow the window or make multiple calls.");

        var output = await useCase.ExecuteAsync(
            new GetPriceSeriesInput(inst.Id, f, t, withIndicators), ct);
        return PriceMapper.ToPriceSeries(output, inst.Ticker);
    }
}
```

Compare this to the previous version: no inline try/catch, no `ILogger.LogWarning` (the logging filter handles it), no repo injections — just the use case, a guard, the clock, and the mapper Strategy. **This is the shape every future MCP tool follows.**

`Guards` is a `Scoped` service registered by `McpCliModule`. It holds the cached `list_instruments` result and provides `ResolveInstrumentOrThrow(ticker, ct)` and `ResolveDateRange(from?, to?, defaultBack, clockToday)`. Centralizes both validation messages and the per-session ticker cache.

## 6. DTO conventions (the wire contract)

Fixed by this spec; deviation = test failure.

| Concern | Rule |
|---|---|
| Property naming | **camelCase.** `PropertyNamingPolicy = JsonNamingPolicy.CamelCase`. DTO records are PascalCase in C#; the converter handles the wire form. |
| Dates | `DateOnly` serialized as ISO `"YYYY-MM-DD"` strings via a custom `DateOnlyJsonConverter` (not `DateTime` round-trip). |
| Money | `decimal` serialized as JSON number (never string). When both currencies are relevant, separate `usd` and `eur` fields appear at the same level, with `fxRate` echoed at the nearest aggregate. |
| Hashes | Truncated to first 8 characters in the **mapper Strategy**, before the DTO is constructed (matches `ReplayCommand.Render`). Full hashes never leave the server. |
| Missing values | `null`, never `0`, `""`, `"Unknown"`, or `false`. Applies to `forwardReturnPct`, `correct`, the suggestion block in `get_dashboard`, and indicator entries before their lookback window completes. |
| Enums | Serialized as PascalCase strings (`"Acquire"`, `"Trim"`, `"Hold"`, `"Wait"`, `"Focus"`, `"Context"`) via `JsonStringEnumConverter`. Enum *casing* is independent of property *casing* — enums stay PascalCase because they're discriminated values, not field names. |
| Ticker casing | Pass-through. `CON3.L` and `BTC-USD` stay exactly as in DB. Input matching is case-sensitive — Claude's `list_instruments` result is the canonical source. |

### 6.1 `JsonSerializerOptions` registration

The options are constructed once and shared by every `WithTools<T>(jsonSerializerOptions: opts)` call in `McpCliModule`. The factory:

```csharp
public static class McpJsonSerializerOptions
{
    public static JsonSerializerOptions Create()
    {
        var o = new JsonSerializerOptions(JsonSerializerDefaults.Web)
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = JsonIgnoreCondition.Never,  // keep nulls explicit
        };
        o.Converters.Add(new JsonStringEnumConverter());
        o.Converters.Add(new DateOnlyJsonConverter());
        return o;
    }
}
```

Defining it in a static factory (rather than registering as a DI singleton) keeps the options self-contained — the mapper Strategies and test code can construct the same options without DI.

### 6.2 Mapper Strategies (one per feature)

Each Dto file is paired with a mapper class under `Mcp/Mapping/`. Each mapper is a small Strategy: a sealed class with static methods that translate from Application/Domain types into MCP DTOs. Example: `PriceMapper.ToPriceSeries(GetPriceSeriesOutput output, string ticker)`.

Reasons for one-mapper-per-feature instead of a single `DtoMappers.cs`:
- Files stay small enough to hold in working memory.
- A feature's DTO and mapper are co-located by name, so renaming or evolving them happens together.
- The "thin DTO layer" remains visible as a layer, not a god-file.

Mappers contain hash truncation and any other field-level transforms. Mappers never call the database, never call other use cases, never throw — they are pure projection.

## 7. Error handling

All error translation is centralized in the filter pipeline (§5.4); tool methods just throw. The contract Claude sees:

| Tool-side throw | Filter behaviour | What Claude receives |
|---|---|---|
| `McpException` directly (rare; reserved for filters themselves) | rethrown | MCP `isError: true` with that message |
| `TradyStratException` (any child of the family) | translated → `McpException(ex.Message)` | MCP error with the curated domain message |
| `ArgumentException` (from input guards) | translated → `McpException(ex.Message)` | MCP error with the actionable validation message |
| `OperationCanceledException` (client cancelled) | rethrown | Cancellation propagates to the client; logging filter records `outcome: cancelled` |
| timeout (the 30s budget elapsed) | translated → `McpException("Tool call exceeded 30s timeout.")` | MCP error so Claude knows it can retry with a narrower request |
| anything else (unexpected) | rethrown | SDK reports a generic error; logging filter has already logged the stack at `Error` |

**Validation rules per tool (raised as `ArgumentException` from input guards):**

- `instrument` (when supplied): must match one of the tickers from `list_instruments` (`Guards.ResolveInstrumentOrThrow` reads the cached result).
- `from`, `to`, `asOf`: must parse as ISO `YYYY-MM-DD`. After defaults: `from <= to`.
- `query_prices` window: `<= 365` days.
- `query_suggestions.limit`: `1..100` inclusive.
- `query_suggestions.action`: one of `"Acquire"`, `"Trim"`, `"Hold"`, `"Wait"` if non-null.
- `get_replay_report.from` and `to`: both required (no defaults).

Pre-condition validation explicitly does **not** live in the use cases — the use cases assume valid inputs because Razor pages validate before calling. The MCP boundary is a new edge; it owns its own validation through `Guards`.

**Validation order in tool methods:** (1) resolve defaults (e.g., `from ← to.AddDays(-90)` when null) inside `Guards.ResolveDateRange`, (2) parse strings to typed values (`DateOnly.Parse`), (3) run guards. Parse failures throw `ArgumentException` directly with the original input echoed back, e.g. `"Invalid date 'tomorrow' — use ISO YYYY-MM-DD."`. After defaults are applied, the `from <= to` guard always holds for the implicit case, so the only way to trip it is an explicit user-supplied inversion.

**Why `ArgumentException` and not a custom `McpInputException`?** `ArgumentException` is the .NET-idiomatic input-violation type; it costs nothing to translate in the filter, and it keeps tool methods readable. A custom type would add ceremony for zero behavioural difference.

## 8. Testing

New project: `TradyStrat.Cli.Tests` (xunit.v3, Shouldly, `TradyStrat.TestKit`). Five focused layers:

**8.1 Tool-method tests** (one file per tool class). For each tool method, cover:
- Happy path with realistic fixture data, asserting on the returned DTO's shape (delegates to the mapper Strategy).
- Each input-guard rule from §7 — bad ticker, bad date range, out-of-bounds `limit`, > 365-day window, unknown `action`. Assert the exact `ArgumentException` message text (Claude's UX depends on it).

**Tool tests do NOT cover exception translation** — that's the filter's job and is tested separately. The tool method just throws; the filter test asserts the translation.

DI wiring per test mirrors the inner host (real modules + TestKit stubs for `IPriceFeed` / `IFxProvider` / `IChatClient` / `IClock`). Pattern is identical to `Application.Tests`, so test infra cost is near zero.

**8.2 Filter tests** (one file per filter). Test each filter in isolation by feeding it a minimal `next` handler:
- `McpExceptionTranslationFilterTests`: typed `TradyStratException` child → `McpException` with same message; `ArgumentException` → `McpException` with same message; `OperationCanceledException` propagates unchanged; arbitrary `InvalidOperationException` propagates unchanged.
- `McpTimeoutFilterTests`: with a `next` that sleeps 100ms and a 50ms test budget → throws `McpException("Tool call exceeded ...")`; with a `next` that completes immediately and a fresh budget → passes through; client-cancelled token propagates `OperationCanceledException`.
- `McpLoggingFilterTests`: captures `ILogger` output via xUnit's `ITestOutputHelper` sink; asserts exactly one log record per call with the expected fields (tool name, duration > 0, outcome). Asserts the `Warning` level for `McpException` outcomes and `Error` for unexpected exceptions.

**8.3 `McpCliModule` wiring test** (`McpCliModuleTests`). Single test: build an `IHost` with `AppManager` + `McpCliModule`, then resolve `IMcpServer` (or the relevant SDK service) and assert it lists all six tool names and the three filters are registered in the expected order. This catches a class of "I added a new tool and forgot to wire it" bugs.

**8.4 DTO shape tests** (`Serialization/JsonShapeTests.cs`). `JsonSerializer.Serialize` round-trip tests using `McpJsonSerializerOptions.Create()`, pinning down:
- `DateOnly` → `"YYYY-MM-DD"` (not `"2026-05-18T00:00:00"`).
- `decimal` stays a JSON number (not string).
- Property names are camelCase (`marketValueEur`, not `MarketValueEur`).
- Enum values are PascalCase strings (`"Acquire"`, not `"acquire"` or `0`).
- 8-char hash truncation fires in the mapper.
- `null` propagates for missing `forwardReturnPct` / `correct` (not dropped, not defaulted).

These catch DTO drift on the next refactor.

**8.5 Spectre registration smoke test** (one test). `CommandApp.Configure(...)` registers `"mcp"`; calling `app.Configure(...)` and inspecting the configured commands list returns it. We don't actually run the MCP loop in tests.

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
8. **No-write check.** Confirm no rows were written to the DB during steps 4–7 (`sqlite3 ~/Library/Application\ Support/TradyStrat/tradystrat.db` — `select max(CreatedAt) from Suggestion;` is unchanged).
9. **No-outbound-AI check.** Grep stderr for any `Anthropic` / `IChatClient` invocation log line during the steps above (the SDK logs each outbound HTTP call). Expected: zero hits. The MCP path must never trigger an AI call to satisfy a query.
10. **Timeout sanity check.** Manually issue a tool call whose use case sleeps for 35s (temporary instrumented build) — confirm the filter cancels at 30s and Claude sees the timeout `McpException`. Optional; revisit only if the timeout filter is changed.

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

## 13. Reference-architecture conventions

This server is the template for future MCP work in TradyStrat (and a documented reference for similar Spectre-based .NET projects). The conventions below are the load-bearing decisions another reader of this codebase must understand to extend the MCP surface safely.

### 13.1 GoF pattern map

| Pattern | Where it lives | Role |
|---|---|---|
| **Adapter** | every tool method (`PriceTool.QueryPrices`, etc.) | translates MCP protocol calls into use-case invocations and back into DTOs |
| **Facade** | `get_dashboard` adapting `LoadDashboardUseCase` (itself a Facade per README §17); `get_portfolio` adapting `PortfolioService` | bundles several Application-layer calls behind a single Claude-facing entry point |
| **Decorator** | the three filters (`McpLoggingFilter` → `McpTimeoutFilter` → `McpExceptionTranslationFilter`) | cross-cutting concerns (logging, timeout, exception translation) wrap every tool without per-tool code |
| **Strategy** | one `*Mapper` per feature in `Mcp/Mapping/` | pure Domain/Application-→-DTO projection, swappable per feature |
| **Specification (Ardalis)** | `PriceBarsInRangeSpec`, `SuggestionsQuerySpec` | reusable query shapes Tools and Razor pages both depend on |
| **Composite** | `McpCliModule : IAppModule` registered alongside other modules via `AppManager` | a feature's wiring is co-located with the feature, not sprinkled across `Program.cs` |
| **Command + Template Method** | `IUseCase<TIn,TOut>` + `UseCaseBase` (project-wide; reused by tools) | tools always go through use cases — never repos directly — getting the timing/logging template for free |

The patterns the project does *not* use here, and why:
- **No Singleton-style global state** in the MCP layer. The DI scope per call (§5.5) is the unit of state.
- **No Observer / event-bus**. The filter pipeline handles cross-cutting concerns without a publish-subscribe layer.
- **No Builder for DTOs**. Records + explicit mappers are simpler.

### 13.2 How to add a new MCP tool (the recipe)

When a future feature wants a new MCP tool, follow exactly these steps:

1. **Confirm it's read-only and side-effect-free.** Writes do not flow through MCP — see §13.5.
2. **Pick the question.** Name the tool by the question Claude asks (`get_*`, `query_*`, `list_*`), not by the use case name.
3. **Add the DTO** in `Mcp/Dto/<Feature>Dtos.cs`. Follow the §6 conventions (camelCase, ISO dates, decimals as numbers, `null` for missing).
4. **Add the mapper Strategy** in `Mcp/Mapping/<Feature>Mapper.cs` as static methods. Mappers are pure projection — no DB, no use case calls, no throws.
5. **Add the use case** in `TradyStrat.Application/<Feature>/UseCases/` if no existing one fits. Tools depend on use cases, not on repos.
6. **Add the specification** in `TradyStrat.Application/<Feature>/Specifications/` if no existing one fits.
7. **Add the tool method** in `Mcp/Tools/<Feature>Tool.cs` (existing class) or a new class. Method body:
   - guard via injected `Guards` helper,
   - call the use case (thread `CancellationToken`),
   - map via the Strategy,
   - return.
   No `try/catch`. No `ILogger`-noise. Use `[McpServerTool(Name = "snake_case_name"), Description("...")]`.
8. **Register the tool class** in `McpCliModule.ConfigureServices` with `.WithTools<NewTool>(jsonSerializerOptions: jsonOptions)`. The wiring test (§8.3) will fail if you skip this.
9. **Add tests** in `TradyStrat.Cli.Tests/Mcp/Tools/<NewTool>Tests.cs` covering the happy path and every guard rule. Filter behaviour is already covered.
10. **Update §4 of this spec** (or its successor) with the new tool's signature and JSON shape.

If the new tool needs a cross-cutting behaviour the existing filters don't handle (e.g., rate limiting, request authentication for a future HTTP variant), add it as a fourth filter — not as inline code in tool methods.

### 13.3 Tool granularity principle

When does a question deserve a new tool vs an option on an existing one?

- **New tool** when the question has a fundamentally different *return shape* (different DTO).
- **New parameter** when the question is a filter or scope on the same return shape.

Example: `query_suggestions` takes `action?` as a filter because the row shape is identical. A hypothetical "summarize suggestions" question would produce a different DTO (aggregates instead of rows) and would deserve a new tool.

A signal that a tool method is doing too much: its DTO has optional sub-objects that are populated only under certain parameter combinations. Split it.

### 13.4 DTO-vs-Domain policy

Always introduce an MCP DTO; never reuse a Domain or Application type directly on the wire. Even when the shape looks identical.

Reasons:
- The MCP contract is consumed by an external client; it must be stable across internal refactors.
- Domain types carry behaviour (methods, factories, invariants) that should not surface to JSON serialization.
- A pure record DTO is trivially testable for wire-shape regressions (§8.4).

**One documented exception:** `ReplayReport` is reused as-is because it was already designed for external display by the existing `replay` CLI command and predates this spec. New tools must not follow this exception.

### 13.5 What stays out of MCP (the exclusion principle)

The principle: **MCP is the read side of a CQRS-shaped boundary.** Writes flow through the dashboard (and, for backfill, through the CLI's existing imperative commands) because:

1. **Audit log integrity.** Mutations have a primary path that captures provenance (`CreatedAt`, user-initiated vs cached, etc.). Routing writes through a second path bypasses the existing audit semantics.
2. **AI cost control.** A write tool that triggers a fresh Anthropic call is a foot-gun for a chatty agent — easy to accidentally rack up calls.
3. **Reversibility.** Read-only is trivially safe. A write tool needs careful thought about rollback and confirmation UX, which the dashboard already handles deliberately.

Concretely, this means **no** `log_trade`, `force_refetch_suggestion`, `update_setting`, or `delete_*` tool ever lands here without first lifting the read-only stance at the spec level — which would require revisiting all three reasons above.

The same exclusion principle applies to:
- Tools that generate new data (e.g., "ask the AI to suggest X" — that's a write of a `Suggestion` row).
- Tools that change configuration (settings page is the source of truth).
- Tools that import bulk data (CSV import is a CLI command for the same audit reasons).

### 13.6 Observability default

Every tool call produces exactly one structured log record at the boundary (the `McpLoggingFilter`). Tool methods themselves log only domain-relevant events (rare). Use cases log via the existing `UseCaseBase` template-method timing.

When debugging a Claude session, the stderr log timestamps line up with the conversation in Claude Desktop's view — that's the entry point. Don't add ad-hoc `Console.Error.WriteLine` for ephemeral debugging; either extend the logging filter (durably) or use a temporary `ILogger` call inside the use case (and remove before merge).
