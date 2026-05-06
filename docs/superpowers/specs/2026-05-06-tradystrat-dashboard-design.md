# TradyStrat — Personal CON3 Accumulation Dashboard

**Status:** Design approved · Ready for implementation plan
**Date:** 2026-05-06
**Author:** Philippe Matray (with Claude)

---

## 1. Purpose & goal

TradyStrat is a **personal, single-user, local-only** dashboard that supports a long-horizon strategy:

> Accumulate CON3 (Leverage Shares 3x Coinbase ETP, Frankfurt-listed) until the position is worth **€1,000,000**.

CON3 is leveraged exposure to Coinbase (COIN) stock, which itself is highly correlated with Bitcoin (BTC). The dashboard therefore observes all three tickers, computes technical-analysis signals per ticker, and produces a single daily "what should I do today?" suggestion via the Anthropic API.

The dashboard is **not** a broker, an alerting system, or a backtest engine. It is a daily companion: open it once a day, see the call, log a trade if appropriate, and watch the curve climb toward €1M.

## 2. Decisions at a glance

| Concern | Decision |
|---|---|
| Stack | .NET 10 + Blazor Server + EF Core + SQLite |
| Hosting | Local-only on the developer's Mac, `http://127.0.0.1:5180` |
| Code organisation | Vertical-slice features in a single project |
| Data source | Yahoo Finance (`/v8/finance/chart/{symbol}`) for CON3.DE, COIN, BTC-USD |
| Refresh cadence | One fetch per app-day, persisted; manual `↻` button |
| Strategy model | Zone-based (Accumulate / Hold / Distribute), computed per ticker |
| Indicators | Bollinger(20,2σ), RSI(14), SMA(50/200), Ichimoku |
| Indicator library | [`Atypical.TechnicalAnalysis.Functions`](https://github.com/phmatray/TaLibStandard) — Bollinger, RSI, SMA |
| Ichimoku | Computed locally (TaLibStandard / TA-Lib do not include it) |
| AI provider | Anthropic — `claude-opus-4-7`, tool-use for structured output |
| AI cadence | One call per calendar day; cached; "Re-run" allowed with confirmation |
| Position tracking | Manual trade log; lots derived from trades (FIFO) |
| Setup | Settings form for goal + many historic trades + CSV import |
| Visual direction | "The Vault" — dark, gold accent, Cormorant Garamond + JetBrains Mono |
| Auth | None (localhost only) |
| Multi-user | Out of scope |

## 3. Project layout

Single Blazor Server project; one `TradyStrat.csproj`. Folders by **vertical feature**, not by technical layer.

```
TradyStrat/
├─ Program.cs                       ← DI wiring, EF Core, hosted services, Serilog
├─ appsettings.json                 ← non-secret config (committed)
├─ appsettings.Development.json     ← dev overrides
├─ user-secrets                     ← ANTHROPIC_API_KEY (never in repo)
│
├─ Features/
│  ├─ Dashboard/
│  │   ├─ DashboardPage.razor + .razor.css   ← @page "/"
│  │   ├─ DashboardViewModel.cs
│  │   ├─ DashboardService.cs                ← composes today's snapshot for the page
│  │   └─ Components/
│  │       ├─ VaultMasthead.razor
│  │       ├─ HeroCapital.razor
│  │       ├─ TodaysCallCard.razor
│  │       ├─ PortfolioRail.razor
│  │       ├─ GrowthChart.razor
│  │       └─ RefreshFab.razor
│  │
│  ├─ PriceFeed/
│  │   ├─ IPriceFeed.cs
│  │   ├─ YahooPriceFeed.cs
│  │   ├─ YahooParser.cs                     ← pure JSON → PriceBar[]
│  │   ├─ DailyPriceCache.cs
│  │   └─ PriceFeedHostedService.cs          ← warms cache on startup
│  │
│  ├─ Indicators/
│  │   ├─ Bollinger.cs                       ← wraps TAFunc.BollingerBands
│  │   ├─ Rsi.cs                             ← wraps TAFunc.Rsi
│  │   ├─ MovingAverage.cs                   ← wraps TAFunc.Sma (period 50, 200)
│  │   ├─ Ichimoku.cs                        ← computed locally
│  │   ├─ ZoneClassifier.cs
│  │   └─ IndicatorEngine.cs
│  │
│  ├─ Trades/
│  │   ├─ TradesPage.razor                   ← @page "/trades"
│  │   ├─ TradeService.cs
│  │   ├─ CsvImportService.cs
│  │   └─ Components/AddTradeDialog.razor
│  │
│  ├─ AiSuggestion/
│  │   ├─ IAiClient.cs
│  │   ├─ AnthropicClient.cs
│  │   ├─ SnapshotBuilder.cs
│  │   ├─ SuggestionParser.cs
│  │   ├─ DailySuggestionCache.cs
│  │   └─ SuggestionService.cs
│  │
│  ├─ Settings/
│  │   ├─ SettingsPage.razor                 ← @page "/settings"
│  │   └─ SettingsService.cs
│  │
│  └─ Portfolio/
│      ├─ PortfolioService.cs                ← shares, avg cost, lots from trades
│      └─ GrowthSeriesBuilder.cs             ← daily portfolio value series for chart
│
├─ Shared/
│  ├─ Domain/
│  │   ├─ Trade.cs · TradeSide.cs
│  │   ├─ PriceBar.cs
│  │   ├─ GoalConfig.cs
│  │   ├─ Zone.cs
│  │   └─ IndicatorReading.cs · BollingerReading.cs · IchimokuReading.cs
│  └─ Time/
│      ├─ IClock.cs
│      └─ SystemClock.cs
│
├─ Data/
│  ├─ AppDbContext.cs
│  └─ Migrations/
│
└─ wwwroot/
   ├─ css/vault.css                          ← design tokens
   └─ favicon, fonts preconnect
```

**Conventions**

- A feature folder owns its UI, services, parsers, and DTOs. It depends on `Shared/` and `Data/AppDbContext`, never on another feature folder.
- `PortfolioService` is the one shared service across features (Dashboard, Trades, AiSuggestion). It lives in its own feature folder rather than `Shared/` so its logic and tests stay together.
- No "Models" folder. Domain types live in `Shared/Domain/`; per-feature DTOs live with the feature.
- Three top-level pages route by attribute: `@page "/"`, `/trades`, `/settings`.

## 4. Data model

SQLite via EF Core 9 (`Microsoft.EntityFrameworkCore.Sqlite`). Migrations checked into `Data/Migrations/` and applied on startup via `db.Database.Migrate()`.

### Entities (persisted)

```csharp
// Shared/Domain/Trade.cs
public sealed class Trade
{
    public int Id { get; init; }
    public DateOnly ExecutedOn { get; init; }
    public TradeSide Side { get; init; }            // Buy | Sell
    public decimal Quantity { get; init; }          // shares (decimals allowed)
    public decimal PricePerShare { get; init; }     // EUR
    public decimal FeesEur { get; init; }           // default 0
    public string? Note { get; init; }
    public DateTime CreatedAt { get; init; }
}

public enum TradeSide { Buy = 1, Sell = 2 }

// Shared/Domain/PriceBar.cs — one daily OHLC bar per ticker
public sealed class PriceBar
{
    public int Id { get; init; }
    public string Ticker { get; init; } = "";       // CON3.DE | COIN | BTC-USD
    public DateOnly Date { get; init; }
    public decimal Open { get; init; }
    public decimal High { get; init; }
    public decimal Low { get; init; }
    public decimal Close { get; init; }
    public long Volume { get; init; }
}

// Shared/Domain/GoalConfig.cs — single-row config table
public sealed class GoalConfig
{
    public int Id { get; init; } = 1;               // singleton
    public decimal TargetEur { get; init; } = 1_000_000m;
    public DateOnly? TargetDate { get; init; }
    public string FocusTicker { get; init; } = "CON3.DE";
    public DateTime UpdatedAt { get; init; }
}

// Features/AiSuggestion/Suggestion.cs
public sealed class Suggestion
{
    public int Id { get; init; }
    public DateOnly ForDate { get; init; }                 // unique
    public SuggestionAction Action { get; init; }          // Acquire | Hold | Trim | Wait
    public decimal? QuantityHint { get; init; }
    public decimal? MaxPriceHint { get; init; }
    public int Conviction { get; init; }                   // 1..5
    public string Rationale { get; init; } = "";
    public string CitationsJson { get; init; } = "[]";     // structured citations
    public string PromptHash { get; init; } = "";
    public DateTime CreatedAt { get; init; }
}

public enum SuggestionAction { Acquire = 1, Hold = 2, Trim = 3, Wait = 4 }
```

### Indexes & constraints

- `PriceBar (Ticker, Date)` — unique
- `Trade (ExecutedOn)` — for chronological reads
- `Suggestion (ForDate)` — unique
- `GoalConfig.Id` — always 1 (single row)

### Computed-only types (not in DB)

```csharp
public sealed record PortfolioSnapshot(
    decimal Shares, decimal AvgCostEur, decimal CurrentValueEur,
    decimal UnrealizedPnLEur, decimal RealizedPnLEur, decimal ProgressPct);

public sealed record IndicatorReading(
    string Ticker, decimal Price,
    BollingerReading? Bollinger, decimal? Rsi,
    decimal? Sma50, decimal? Sma200,
    IchimokuReading? Ichimoku,
    Zone Zone, IReadOnlyList<string> Reasons);

public enum Zone { Accumulate = 1, Hold = 2, Distribute = 3 }
```

### Lots are a view, not a table

Lots are derived from `Trade` rows in `PortfolioService`: each Buy creates an open lot; Sells consume oldest lots first (FIFO). This avoids two sources of truth and matches how brokers report cost basis.

## 5. Indicators & zone classification

### TaLibStandard wrappers

TaLibStandard's API is a faithful port of the original TA-Lib (out-arrays + `RetCode`). We wrap each indicator once so the rest of the app sees clean records.

```csharp
// Features/Indicators/Bollinger.cs
using TechnicalAnalysis.Common;
using TechnicalAnalysis.Functions;

public sealed record BollingerReading(decimal Upper, decimal Middle, decimal Lower, decimal Sigma);

public static class Bollinger
{
    public static BollingerReading? LatestFor(IReadOnlyList<PriceBar> bars,
        int period = 20, double devUp = 2.0, double devDown = 2.0)
    {
        if (bars.Count < period) return null;

        double[] closes = bars.Select(b => (double)b.Close).ToArray();
        double[] upper  = new double[closes.Length];
        double[] middle = new double[closes.Length];
        double[] lower  = new double[closes.Length];
        int begIdx = 0, nb = 0;

        var rc = TAFunc.BollingerBands(
            0, closes.Length - 1, in closes,
            in period, in devUp, in devDown, MAType.Sma,
            ref begIdx, ref nb,
            ref upper, ref middle, ref lower);

        if (rc != RetCode.Success || nb == 0) return null;

        int last = nb - 1;
        var sigma = (decimal)((upper[last] - middle[last]) / 2.0);
        return new BollingerReading(
            (decimal)upper[last], (decimal)middle[last], (decimal)lower[last], sigma);
    }
}
```

`Rsi` (calling `TAFunc.Rsi`) and `MovingAverage` (calling `TAFunc.Sma` with period 50 and 200) follow the same pattern.

### Ichimoku — computed locally

TA-Lib (and therefore TaLibStandard) does not include Ichimoku. We compute it ourselves; it's only rolling highs/lows + offsets (~30 lines).

```csharp
public sealed record IchimokuReading(
    decimal Tenkan, decimal Kijun,
    decimal SenkouA, decimal SenkouB,
    decimal Chikou,
    IchimokuSignal Signal);                    // AboveCloud | InCloud | BelowCloud

public enum IchimokuSignal { AboveCloud = 1, InCloud = 2, BelowCloud = 3 }

public static class Ichimoku
{
    // Tenkan = (max(high,9) + min(low,9)) / 2
    // Kijun  = (max(high,26) + min(low,26)) / 2
    // SenkouA = (Tenkan + Kijun) / 2 shifted +26 bars
    // SenkouB = (max(high,52) + min(low,52)) / 2 shifted +26 bars
    // Chikou  = close shifted -26 bars
    public static IchimokuReading? LatestFor(IReadOnlyList<PriceBar> bars) { /* ~30 lines */ }
}
```

### `ZoneClassifier` — combining 4 indicators into one zone per ticker

Each indicator votes Accumulate / Hold / Distribute. Final zone = majority. Ties resolve to Hold (conservative).

```csharp
public static class ZoneClassifier
{
    public static (Zone Zone, IReadOnlyList<string> Reasons) Classify(
        decimal price, BollingerReading? bb, decimal? rsi,
        decimal? sma50, decimal? sma200, IchimokuReading? ichi)
    {
        var votes = new List<(Zone vote, string reason)>();

        if (bb is not null)
        {
            if (price < bb.Lower)        votes.Add((Zone.Accumulate, $"Price {price:F2} below lower Bollinger ({bb.Lower:F2})"));
            else if (price > bb.Upper)   votes.Add((Zone.Distribute, $"Price above upper Bollinger ({bb.Upper:F2})"));
            else                         votes.Add((Zone.Hold,        "Inside Bollinger band"));
        }
        if (rsi is decimal r)
        {
            if (r < 30)      votes.Add((Zone.Accumulate, $"RSI(14) {r:F0}, oversold"));
            else if (r > 70) votes.Add((Zone.Distribute, $"RSI(14) {r:F0}, overbought"));
            else             votes.Add((Zone.Hold,        $"RSI(14) {r:F0}, neutral"));
        }
        if (sma50 is decimal s50 && sma200 is decimal s200)
        {
            if (price < s200)         votes.Add((Zone.Accumulate, $"Below 200-SMA ({s200:F2})"));
            else if (price > s50)     votes.Add((Zone.Distribute, $"Above 50-SMA ({s50:F2})"));
            else                      votes.Add((Zone.Hold,        "Between 50/200-SMA"));
        }
        if (ichi is not null)
        {
            var label = ichi.Signal switch {
                IchimokuSignal.BelowCloud => (Zone.Accumulate, "Below Ichimoku cloud"),
                IchimokuSignal.AboveCloud => (Zone.Distribute, "Above Ichimoku cloud"),
                _                         => (Zone.Hold,        "Inside Ichimoku cloud")
            };
            votes.Add(label);
        }

        var groups = votes.GroupBy(v => v.vote).Select(g => (zone: g.Key, n: g.Count()))
                          .OrderByDescending(x => x.n).ToList();
        var majority = groups[0].zone;
        if (groups.Count > 1 && groups[0].n == groups[1].n) majority = Zone.Hold;

        return (majority, votes.Select(v => v.reason).ToList());
    }
}
```

### `IndicatorEngine`

```csharp
public sealed class IndicatorEngine(AppDbContext db)
{
    public async Task<IndicatorReading> ComputeFor(string ticker, CancellationToken ct = default)
    {
        var bars = await db.PriceBars.Where(b => b.Ticker == ticker)
            .OrderBy(b => b.Date).ToListAsync(ct);
        var price = bars[^1].Close;
        var bb    = Bollinger.LatestFor(bars);
        var rsi   = Rsi.LatestFor(bars);
        var sma50 = MovingAverage.LatestFor(bars, 50);
        var sma200= MovingAverage.LatestFor(bars, 200);
        var ichi  = Ichimoku.LatestFor(bars);
        var (zone, reasons) = ZoneClassifier.Classify(price, bb, rsi, sma50, sma200, ichi);
        return new IndicatorReading(ticker, price, bb, rsi, sma50, sma200, ichi, zone, reasons);
    }
}
```

### Notes

- Indicators are pure functions of `IReadOnlyList<PriceBar>`. No DB writes from `Indicators/`.
- Decimal in domain, double inside TaLib calls. Convert at the wrapper boundary.
- Reasons strings are prepared so the AI can cite them one-to-one.

## 6. Data flow

### Yahoo Finance

```csharp
public interface IPriceFeed
{
    Task<IReadOnlyList<PriceBar>> FetchDailyAsync(
        string ticker, DateOnly from, DateOnly to, CancellationToken ct);
}

public sealed class YahooPriceFeed(HttpClient http, IClock clock, ILogger<YahooPriceFeed> log) : IPriceFeed
{
    public async Task<IReadOnlyList<PriceBar>> FetchDailyAsync(
        string ticker, DateOnly from, DateOnly to, CancellationToken ct)
    {
        var p1 = new DateTimeOffset(from.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero).ToUnixTimeSeconds();
        var p2 = new DateTimeOffset(to.ToDateTime(TimeOnly.MaxValue),   TimeSpan.Zero).ToUnixTimeSeconds();
        var url = $"/v8/finance/chart/{Uri.EscapeDataString(ticker)}?period1={p1}&period2={p2}&interval=1d";

        using var resp = await http.GetAsync(url, ct);
        resp.EnsureSuccessStatusCode();
        var doc = await JsonDocument.ParseAsync(
            await resp.Content.ReadAsStreamAsync(ct), cancellationToken: ct);
        return YahooParser.ParseDaily(ticker, doc);
    }
}
```

Registration:

```csharp
builder.Services.AddHttpClient<IPriceFeed, YahooPriceFeed>(c =>
{
    c.BaseAddress = new Uri("https://query1.finance.yahoo.com");
    c.Timeout     = TimeSpan.FromSeconds(15);
    c.DefaultRequestHeaders.UserAgent.ParseAdd("TradyStrat/1.0");
}).AddPolicyHandler(GetRetryPolicy());     // Polly: 2 retries, exp. backoff, transient only
```

### `DailyPriceCache` — one fetch per app-day

```csharp
public sealed class DailyPriceCache(IPriceFeed feed, AppDbContext db, IClock clock, ILogger<DailyPriceCache> log)
{
    public async Task EnsureFreshAsync(string ticker, CancellationToken ct)
    {
        var today  = clock.TodayInExchangeTzFor(ticker);
        var latest = await db.PriceBars.Where(b => b.Ticker == ticker)
                                       .OrderByDescending(b => b.Date)
                                       .Select(b => (DateOnly?)b.Date).FirstOrDefaultAsync(ct);
        if (latest == today) return;

        var from = latest?.AddDays(1) ?? today.AddYears(-2);
        var bars = await feed.FetchDailyAsync(ticker, from, today, ct);
        db.PriceBars.AddRange(bars);
        await db.SaveChangesAsync(ct);
        log.LogInformation("PriceFeed: appended {N} bars for {Ticker}", bars.Count, ticker);
    }
}
```

`PriceFeedHostedService.StartAsync` calls `EnsureFreshAsync` for CON3.DE, COIN, BTC-USD on app start.

**Per-ticker timezone for "today"** — avoids cache thinking it's already updated when the relevant market hasn't closed:
- CON3.DE → Europe/Berlin (XETRA close)
- COIN → America/New_York
- BTC-USD → UTC

### Anthropic — `AnthropicClient` + `SuggestionService`

NuGet: `Anthropic.SDK` (official). Model: **`claude-opus-4-7`**. API key from `dotnet user-secrets`; never in repo.

```csharp
public sealed record AiSnapshot(
    DateOnly Today,
    GoalConfigDto Goal,
    PortfolioSnapshot Portfolio,
    IReadOnlyList<IndicatorReading> Indicators,
    IReadOnlyList<TradeRecent> RecentTrades,
    PriceContext Prices);

public sealed class AnthropicClient(IAnthropicApi sdk, ILogger<AnthropicClient> log) : IAiClient
{
    private const string ToolName = "submit_suggestion";

    public async Task<Suggestion> AskAsync(AiSnapshot s, CancellationToken ct)
    {
        var sys = """
            You are a disciplined trading assistant for a personal accumulation strategy
            on CON3 (a 3x leveraged Coinbase ETP). You see snapshots once per day.
            Cite which indicators support each part of your suggestion.
            Be conservative: when signals conflict, say Hold.
            """;

        var userJson = JsonSerializer.Serialize(s, JsonOpts.Strict);
        var tool = new Tool(ToolName, "Submit your structured suggestion.", new { /* schema */ });

        var msg = await sdk.Messages.CreateAsync(new()
        {
            Model      = "claude-opus-4-7",
            System     = sys,
            Messages   = [new(Role.User, userJson)],
            Tools      = [tool],
            ToolChoice = ToolChoice.ForTool(ToolName),
            MaxTokens  = 1500,
        }, ct);

        var call = msg.Content.OfType<ToolUseBlock>().Single(t => t.Name == ToolName);
        return SuggestionParser.FromToolInput(call.Input, s.Today);
    }
}
```

Tool schema:

```json
{
  "action": "Acquire | Hold | Trim | Wait",
  "quantity_hint": 0,
  "max_price_hint": 0.0,
  "conviction": 4,
  "rationale": "1–2 sentence prose summary",
  "citations": [
    { "claim": "Below lower Bollinger band",
      "indicator": "Bollinger",
      "ticker": "CON3.DE",
      "value": "1.4σ below 20-day mean" }
  ]
}
```

`SuggestionService`:

```csharp
public sealed class SuggestionService(AppDbContext db, IAiClient ai, SnapshotBuilder snap, IClock clock)
{
    public async Task<Suggestion> GetOrFetchTodayAsync(CancellationToken ct)
    {
        var today = clock.TodayLocal();
        var existing = await db.Suggestions.FirstOrDefaultAsync(x => x.ForDate == today, ct);
        if (existing is not null) return existing;
        return await ForceRefetchAsync(ct);
    }

    public async Task<Suggestion> ForceRefetchAsync(CancellationToken ct)
    {
        var today = clock.TodayLocal();
        var s = await snap.BuildAsync(ct);
        var sug = await ai.AskAsync(s, ct);
        var existing = await db.Suggestions.FirstOrDefaultAsync(x => x.ForDate == today, ct);
        if (existing is not null) db.Suggestions.Remove(existing);
        db.Suggestions.Add(sug);
        await db.SaveChangesAsync(ct);
        return sug;
    }
}
```

The `Re-run AI` button opens a confirmation dialog ("This will use one Anthropic API call. Continue?") before calling `ForceRefetchAsync`.

### Configuration & secrets

`appsettings.json` (committed):
```json
{
  "Anthropic":  { "Model": "claude-opus-4-7", "MaxTokens": 1500 },
  "Yahoo":      { "BaseUrl": "https://query1.finance.yahoo.com" },
  "Tickers":    { "Focus": "CON3.DE", "Context": ["COIN", "BTC-USD"] },
  "Database":   { "Path": "~/Library/Application Support/TradyStrat/tradystrat.db" }
}
```

Secrets via `dotnet user-secrets`:
```
Anthropic:ApiKey = sk-ant-...
```

App fails fast on startup if `Anthropic:ApiKey` is missing.

### End-to-end daily flow

```
PriceFeedHostedService.StartAsync
  → DailyPriceCache.EnsureFreshAsync × 3 tickers           (one fetch/day)

User opens "/"
  → DashboardService.LoadAsync
      → IndicatorEngine.ComputeFor × 3 tickers
      → PortfolioService.SnapshotAsync (lots from Trade history)
      → GrowthSeriesBuilder.BuildAsync (daily portfolio value series)
      → SuggestionService.GetOrFetchTodayAsync (cached or first call)
  → DashboardViewModel populated → Razor page renders The Vault layout
```

## 7. UI — The Vault

### Aesthetic

Warm-dark, gold-accent, editorial-private-banking. Cormorant Garamond display, JetBrains Mono numerics, generous negative space. Dark `#0E0D0A` ground, ivory `#ECE6D6` ink, brushed gold `#C49A56` accent. The capital amount is the hero of the page.

### Page composition

```
DashboardPage.razor
├─ <VaultMasthead />                ← brand · entry no. · date
├─ <HeroCapital />                  ← €174,290 / of one million / progress bar
├─ <TodaysCallCard />               ← AI verb + order + reasons + buttons
├─ <PortfolioRail />                ← position cell + 3 ticker cells
├─ <GrowthChart />                  ← SVG, gold line, dashed goal trajectory
└─ <RefreshFab />                   ← floating refresh button
```

`/trades` and `/settings` reuse `<VaultMasthead />` and the same CSS variables.

### Theme tokens — `wwwroot/css/vault.css`

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

body {
  background: var(--vault-bg); color: var(--vault-ivory);
  font-family: var(--font-body);
  font-feature-settings: "kern", "liga", "onum";
}

.num { font-family: var(--font-mono); font-variant-numeric: tabular-nums; }
.label { font-family: var(--font-mono); font-size: 10px;
         letter-spacing: var(--tracking-xs); text-transform: uppercase;
         color: var(--vault-gold); }
.rule  { height: 1px; background: var(--vault-rule); }
```

Fonts loaded once in `App.razor`'s `<head>` from Google Fonts.

### `DashboardViewModel`

```csharp
public sealed record DashboardViewModel(
    DateOnly Today,
    int EntryNumber,                                  // count of distinct days with activity
    PortfolioSnapshot Portfolio,
    GoalConfigDto Goal,
    Suggestion TodaysCall,
    IReadOnlyList<IndicatorReading> Tickers,          // CON3.DE, COIN, BTC-USD
    IReadOnlyList<GrowthPoint> Growth,                // {Date, ValueEur}
    DateOnly? LatestPriceDate);                       // for "data as of …" footer
```

`DashboardService.LoadAsync(ct)` builds it. The page calls it on `OnInitializedAsync`, on every refresh, and after a trade is logged.

### `<HeroCapital />`

```razor
@* Features/Dashboard/Components/HeroCapital.razor *@
<div class="hero">
  <div class="label">Capital under accumulation</div>
  <div class="amount">
    <span class="euro">€</span>
    <span class="num">@Model.Portfolio.CurrentValueEur.ToString("N0", FrCulture)</span>
    <span class="of">— of one million —</span>
  </div>
  <div class="progress">
    <div class="bar"><span style="width:@Model.Portfolio.ProgressPct.ToString("F2")%"></span></div>
    <div class="pct num">@Model.Portfolio.ProgressPct.ToString("F1") %</div>
  </div>
</div>
```

```css
.hero { padding: 72px 56px 52px; border-bottom: var(--rule-soft); }
.amount {
  font-family: var(--font-display); font-weight: 300;
  font-size: clamp(72px, 9vw, 128px); line-height: 0.9; letter-spacing: -0.035em;
}
.amount .euro { color: var(--vault-gold); margin-right: 2px; }
.amount .of   { display: block; font-family: var(--font-mono); font-size: 13px;
                letter-spacing: var(--tracking-xs); text-transform: uppercase;
                color: rgba(236,230,214,0.55); margin-top: 20px; }
.progress { margin-top: 18px; display: flex; align-items: center; gap: 16px; }
.bar      { flex: 1; height: 2px; background: var(--vault-rule); position: relative; }
.bar span { position: absolute; left: 0; top: -2px; height: 6px;
            background: var(--vault-gold); box-shadow: 0 0 18px rgba(196,154,86,0.55); }
```

### `<TodaysCallCard />`

Maps `Suggestion.Action` to the italic verb (`Acquire.` / `Hold.` / `Trim.` / `Wait.`), shows the order line if `QuantityHint` and `MaxPriceHint` are present, renders the rationale, and lists numbered citations. `Log trade` opens `<AddTradeDialog />` pre-filled with the AI hints; `Re-run AI` opens a confirmation dialog before calling `SuggestionService.ForceRefetchAsync`.

### `<GrowthChart />` — custom SVG, no chart library

The Vault aesthetic depends on the gold line + glow + dashed goal trajectory. Off-the-shelf libraries can't match it without heavy override. Custom SVG, ~80 lines of Razor + a small `Path` builder helper. Y-scale is anchored to `Goal.TargetEur`.

### Refresh & action flow

| User action | Service called | Re-render trigger |
|---|---|---|
| Page load | `DashboardService.LoadAsync` | initial |
| `↻` refresh button | `DailyPriceCache.EnsureFreshAsync(*)` then `LoadAsync` | toast "Prices refreshed" |
| `Re-run AI` after confirm | `SuggestionService.ForceRefetchAsync` then `LoadAsync` | TodaysCallCard cross-fades |
| `Log trade` confirm | `TradeService.AddAsync` then `LoadAsync` | hero + chart update |

All re-fetches go through the same `LoadAsync`. No partial updates — the page is small enough that re-rendering is instant on Blazor Server.

### Motion — restrained

- **Page-load reveal:** hero amount counts up from 0 to current value over 800ms; rest of the page fades in with a 60ms stagger.
- **Gold-line draw:** growth chart's `path.line` uses `stroke-dasharray` reveal on first paint (1200ms ease-out).
- **Re-run AI:** the verb cross-fades when a new suggestion arrives.

No hover bounces, no parallax, no scroll-jacking.

### Settings & Trades pages

- `/settings` — form: `Goal target (EUR)` (default 1,000,000), `Target date (optional)`, `Focus ticker` (default `CON3.DE`, read-only for now). Submit → `SettingsService.UpdateAsync`.
- `/trades` — table of all trades, oldest first. Header buttons: `+ Add trade` (opens `<AddTradeDialog />`) and `Import CSV` (parses `date,side,qty,price,fees`; one row per existing lot for backfill). Inline edit + delete with confirm. Same Vault tokens, no charts.

## 8. Error handling

Three failure surfaces, three policies:

**1. Yahoo Finance failure (network, 4xx, 5xx, malformed JSON)**
- Polly: 2 retries, exponential backoff (200ms, 800ms), transient errors only.
- Final failure logs `Warning` and surfaces a non-blocking dashboard banner: *"Prices unavailable — last data from 5 May 2026. [Retry]"*.
- Page still renders from cache. Indicators and AI snapshot still compute.
- `↻` shows spinner; on failure shows inline error and re-enables.

**2. Anthropic call failure (rate limit, network, 5xx)**
- Single retry with 1s backoff; otherwise fail fast.
- `<TodaysCallCard />` shows: *"Could not fetch today's call. [Try again]"*, with the previous day's suggestion below as `Yesterday's call (cached)`.
- Never fall back to a hand-rolled "default" suggestion.

**3. SQLite / EF Core failure (corruption, schema drift, disk full)**
- Programmer errors locally — let them propagate. Default Blazor error UI shows the bubble; logs go to stderr and `logs/tradystrat-yyyymmdd.log` (Serilog rolling file).
- Exception: `db.Database.Migrate()` runs on startup. Migration failure crashes the host with a clear log line.

A single Serilog sink captures unhandled exceptions in razor components and hosted services. No external telemetry — single-user, offline app.

## 9. Testing

xUnit + Shouldly (matches TaLibStandard's stack). Pyramid biased toward fast pure-function tests where the math lives.

```
TradyStrat.Tests/
├─ Indicators/
│  ├─ BollingerTests.cs             ← golden 250-bar fixture; expected from TA-Lib reference
│  ├─ RsiTests.cs
│  ├─ MovingAverageTests.cs
│  ├─ IchimokuTests.cs              ← own fixture
│  └─ ZoneClassifierTests.cs        ← all 81 vote combinations + tie-break
│
├─ PriceFeed/
│  ├─ YahooParserTests.cs           ← captured JSON fixtures, no network
│  └─ DailyPriceCacheTests.cs       ← in-memory SQLite + fake IPriceFeed
│
├─ AiSuggestion/
│  ├─ SnapshotBuilderTests.cs
│  ├─ SuggestionParserTests.cs      ← tool-input JSON ↔ Suggestion roundtrip
│  └─ SuggestionServiceTests.cs     ← in-memory DB + fake IAiClient; cache + force-refetch
│
├─ Portfolio/
│  ├─ PortfolioServiceTests.cs      ← FIFO sells, partial sells, fees
│  └─ GrowthSeriesBuilderTests.cs
│
├─ Trades/
│  └─ TradeServiceTests.cs          ← add/edit/delete + CSV import parsing
│
└─ Dashboard/
   └─ DashboardServiceTests.cs      ← composes VM end-to-end with all fakes
```

**Not tested:**
- Razor components — they are dumb renderers of `DashboardViewModel`; the interesting behavior is in services.
- Live Yahoo or Anthropic — flaky for CI and wastes API budget. Captured fixtures cover JSON parsing; SDK and HttpClient are trusted.

**Coverage target:** 80% on `Features/Indicators/`, `Features/Portfolio/`, `Features/PriceFeed/parsing`, `Features/AiSuggestion/parsing`. No coverage chasing on Razor or DI wiring.

## 10. Run & deploy

Local-only Blazor Server, single user, on the developer's Mac.

**One-command run:**
```
dotnet run --project TradyStrat
# → http://127.0.0.1:5180
```

**First-run setup (in `README.md`):**
```
dotnet user-secrets set "Anthropic:ApiKey" "sk-ant-…"   # one-time
dotnet ef database update                               # creates tradystrat.db
dotnet run --project TradyStrat
```

**Database location:** `~/Library/Application Support/TradyStrat/tradystrat.db`. Configurable via `appsettings.json`. Backup = copy the file.

**Logs:** `~/Library/Application Support/TradyStrat/logs/tradystrat-yyyymmdd.log`. Rotated daily, retained 14 days.

**Networking:** Kestrel bound to `127.0.0.1:5180` only. No Docker, no systemd, no reverse proxy, no auth. LAN exposure is a future change with auth attached; out of scope.

**.gitignore additions** (in addition to standard .NET template):
```
.superpowers/
*.db
*.db-shm
*.db-wal
logs/
```

## 11. Out of scope (explicit)

- Multi-user, auth, reverse proxy, TLS.
- Broker integration; trades are entered manually (CSV is the only bulk path).
- Real-time prices or websockets — daily bars only.
- Alerts, push notifications, scheduled emails.
- Backtesting — AI and indicators consume only current state.
- Mobile-specific view; CSS is responsive enough at ~720px but not phone-designed.
- Tickers other than CON3.DE / COIN / BTC-USD; the focus ticker is hard-decided.

## 12. Architecture summary

```
              ┌──────────────────────────────────────┐
              │         Blazor Server (.NET 10)       │
              │                                       │
   browser ──►│  Pages: / · /trades · /settings       │
              │  Dashboard ◄── DashboardService       │
              │       │            │                  │
              │       │       ┌────┴─────────┐        │
              │       │       │ Portfolio    │        │
              │       │       │ Indicators   │ ──► TaLibStandard.Functions
              │       │       │ Suggestion   │ ──► Anthropic.SDK ──► api.anthropic.com
              │       │       └──────────────┘        │
              │       └─► EF Core ─► SQLite (local)   │
              │                  ▲                    │
              │           PriceFeedHostedService ──►  Yahoo Finance (once/day)
              └──────────────────────────────────────┘
```
