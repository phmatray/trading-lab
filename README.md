![TradyStrat banner](.github/banner.png)

# TradyStrat

Personal Blazor Server dashboard tracking accumulation of **CON3.L** (Leverage Shares 3x Long Coinbase ETP, LSE, USD-quoted) toward **€1,000,000**. Daily Yahoo Finance prices for CON3.L / COIN / BTC-USD plus EUR/USD FX, technical-analysis zones (Bollinger / RSI / SMA / Ichimoku), and an Anthropic-generated cited daily suggestion — in "The Vault" UI.

> **Spec:** [`docs/superpowers/specs/2026-05-06-tradystrat-dashboard-design.md`](docs/superpowers/specs/2026-05-06-tradystrat-dashboard-design.md)
> **Visual reference:** [`docs/superpowers/specs/2026-05-06-tradystrat-vault-mockup.html`](docs/superpowers/specs/2026-05-06-tradystrat-vault-mockup.html)
> **Implementation plan:** [`docs/superpowers/plans/2026-05-06-tradystrat-dashboard.md`](docs/superpowers/plans/2026-05-06-tradystrat-dashboard.md)

## Quick start (local)

Requirements:
- **.NET 10 SDK** — verify with `dotnet --list-sdks` (must show `10.0.x`).
- An **Anthropic API key** for the daily suggestion feature (you can boot without it; the dashboard will surface a typed error and you skip the AI flow).

```bash
# 1. Restore the local dotnet-ef tool (one time)
dotnet tool restore

# 2. Set the Anthropic API key (one time, stored in user-secrets)
dotnet user-secrets set "Anthropic:ApiKey" "sk-ant-…" --project TradyStrat

# 3. Run
dotnet run --project TradyStrat
```

Visit **http://127.0.0.1:5180**.

The first run takes a moment: `PriceFeedHostedService` warms the daily caches by fetching ~2 years of bars per ticker from Yahoo plus the EUR/USD rate.

> **Why `dotnet tool restore`?** The repo pins `dotnet-ef` 10.0.7 as a local manifest (`.config/dotnet-tools.json`) — necessary because the system-global `dotnet-ef` was older and root-owned in our setup. The local manifest avoids the conflict.

> **Hot reload caveat:** `dotnet watch` does **not** apply changes to host-page Razor files (`Components/App.razor`). If you edit that file (or anything that affects the rendered HTML shell), restart the watch process to see the change.

## File locations

| | |
|---|---|
| **Database** | `~/Library/Application Support/TradyStrat/tradystrat.db` |
| **Logs** | `~/Library/Application Support/TradyStrat/logs/tradystrat-yyyymmdd.log` (daily rolling, 14 retained) |

Backup the DB by copying the file. Migrations apply automatically on startup; safe to delete the DB to start fresh.

## Quick start (Docker / OrbStack)

The image is for parity / portability; the local-only security model still applies via `127.0.0.1` port binding. Built and verified against [OrbStack](https://orbstack.dev) on macOS — the standard `docker` CLI commands work unchanged.

```bash
# Build (no secret baked in)
docker build -t tradystrat:latest .

# Run, mounting a host directory for the SQLite DB and logs,
# and supplying the Anthropic key via env (NOT baked into the image)
docker run --rm -it \
  -p 127.0.0.1:5180:5180 \
  -v "$HOME/Library/Application Support/TradyStrat":/data \
  -e Anthropic__ApiKey="$ANTHROPIC_API_KEY" \
  tradystrat:latest
```

Visit http://127.0.0.1:5180.

## MCP server (read-only, personal)

A read-only Model Context Protocol server is available as a subcommand of `TradyStrat.Cli`:

```bash
dotnet run --project TradyStrat.Cli -- mcp
```

This exposes six question-oriented tools (`list_instruments`, `get_dashboard`, `query_suggestions`, `query_prices`, `get_portfolio`, `get_replay_report`) over stdio for Claude Desktop or Claude Code. See [`docs/superpowers/specs/2026-05-18-mcp-server-design.md`](docs/superpowers/specs/2026-05-18-mcp-server-design.md) for the full design and reference-architecture conventions.

The server is read-only and personal: no writes, no auth, stdio-only, single-user.

To wire it up in Claude Desktop, add to `~/Library/Application Support/Claude/claude_desktop_config.json`:

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

## Tests

```bash
dotnet test
```

xunit.v3, Shouldly, in-memory EF for repository tests, captured Yahoo JSON fixtures for the parser. Live external services (Yahoo, Anthropic) are not exercised by tests.

## Project layout

Top-level folders inside `TradyStrat/`:

| Folder | Responsibility |
|---|---|
| `Modules/` | `TheAppManager.IAppModule` per feature; `Program.cs` is one line. |
| `Features/Dashboard/` | The Vault UI — page + six components. |
| `Features/PriceFeed/` | Yahoo client + DailyPriceCache decorator + hosted warm-up. |
| `Features/Fx/` | EUR/USD provider + cache + USD→EUR converter. |
| `Features/Indicators/` | TaLibStandard wrappers + IZoneRule strategies + ZoneClassifier composite. |
| `Features/Trades/` | Trade ledger UI + CSV import. |
| `Features/AiSuggestion/` | AiSnapshotService + IChatClient-backed SuggestionService. |
| `Features/Settings/` | Goal editor. |
| `Features/Portfolio/` | FIFO lot accounting + daily growth series. |
| `Application/UseCases/` | One class per command/query (`IUseCase<TIn,TOut>`). Razor pages depend on these, not on services. |
| `Application/Abstractions/` | `IUseCase`, `UseCaseBase` (template method with logging), `Unit`. |
| `Specifications/` | Ardalis spec classes (Trades / PriceBars / FxRates / Suggestions). |
| `Shared/Domain/` | Entities + value records + computed types. |
| `Shared/Exceptions/` | `TradyStratException` + 7 typed children. |
| `Shared/Time/` | `IClock` + `SystemClock` (timezone-per-ticker). |
| `Data/` | `AppDbContext` + `IEntityTypeConfiguration` + EF migrations. |

`docs/superpowers/{specs,plans}/` contain the design + implementation plan that drove the project.

## Configuration

Non-secret config in `appsettings.json` (committed):

```json
{
  "Anthropic": { "Model": "claude-opus-4-7", "MaxTokens": 1500 },
  "Yahoo":     { "BaseUrl": "https://query1.finance.yahoo.com" },
  "Tickers":   { "Focus": "CON3.L", "Context": ["COIN", "BTC-USD"] },
  "Fx":        { "Pair": "EURUSD" },
  "Database":  { "Path": "~/Library/Application Support/TradyStrat/tradystrat.db" }
}
```

The Anthropic API key is the only secret. Local: `dotnet user-secrets`. Docker: `Anthropic__ApiKey` env var.

## Architecture (TL;DR)

```
browser
  └─► Razor page (DashboardPage / TradesPage / SettingsPage)
        └─► UseCase  (LoadDashboard / LogTrade / GetTodaysSuggestion / …)
              └─► Feature service  (IndicatorEngine / PortfolioService / SuggestionService / …)
                    ├─► EF Core via Ardalis Specification
                    └─► IChatClient (Anthropic.SDK adapter) for AI
                                                        ↑
PriceFeedHostedService warms DailyPriceCache + DailyFxCache from Yahoo at startup.
TheAppManager modules wire everything in Program.cs (one line).
```

Design patterns called out in the spec (§17):

- **Adapter** — TaLib wrappers, FxConverter
- **Strategy + Composite** — `IZoneRule` implementations + `ZoneClassifier`
- **Decorator** — `DailyPriceCache` / `DailyFxCache` wrap the raw providers
- **Command + Template Method** — `IUseCase` + `UseCaseBase`
- **Service-orchestrator** — `AiSnapshotService.CreateAsync` (10 collaborators; was naming-mislabelled as Factory Method through Phase 1)
- **Saga (per-ticker fan-out)** — `GetAllTodaysSuggestionsUseCase` (swallow-and-continue) and `SuggestionBackfillCoordinator` (first-fail-stop) — same shape, deliberately distinct failure policies
- **Specification** — Ardalis specs for all DB queries
- **Facade** — `LoadDashboardUseCase`, `PortfolioService`

## Out of scope

- Multi-user, auth, reverse proxy, TLS.
- Broker integration; trades are entered manually (CSV is the only bulk path).
- Real-time prices or websockets — daily bars only.
- Alerts, push notifications, scheduled emails.
- Backtesting.
- Mobile-specific layouts.
- Tickers other than CON3.L / COIN / BTC-USD; currencies other than EUR/USD.

## License

Personal use.
