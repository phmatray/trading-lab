# TradyStrat — Personal CON3 Accumulation Dashboard

**Status:** Design approved · Ready for implementation plan
**Date:** 2026-05-06
**Author:** Philippe Matray (with Claude)

---

## 1. Purpose & goal

TradyStrat is a **personal, single-user, local-only** dashboard that supports a long-horizon strategy:

> Accumulate CON3 (Leverage Shares 3x Coinbase ETP, Frankfurt-listed) until the position is worth **€1,000,000**.

CON3 is leveraged exposure to Coinbase (COIN) stock, which itself is highly correlated with Bitcoin (BTC). The dashboard observes all three tickers, handles EUR/USD currency conversion for the dollar-denominated context tickers, computes technical-analysis signals per ticker, and produces a single daily "what should I do today?" suggestion via the Anthropic API.

The dashboard is **not** a broker, an alerting system, or a backtest engine. It is a daily companion: open it once a day, see the call, log a trade if appropriate, and watch the curve climb toward €1M.

## 2. Decisions at a glance

| Concern | Decision |
|---|---|
| Stack | .NET 10 + Blazor Server + EF Core 10 + SQLite |
| Hosting | Local on the developer's Mac at `http://127.0.0.1:5180`; Docker image for parity |
| Code organisation | Vertical-slice features in a single project |
| Application startup | [`TheAppManager`](https://github.com/phmatray/TheAppManager) modules — one `IAppModule` per feature |
| Application layer | Use Case pattern (one class per command/query, `IUseCase<TIn,TOut>`) |
| Data access | EF Core 10 + [`Ardalis.Specification`](https://github.com/ardalis/Specification) for query specifications |
| Entities | `sealed record` with `required` properties, `init` accessors, derived (computed) read-only properties |
| Data source | Yahoo Finance for prices: `CON3.DE`, `COIN`, `BTC-USD`, and FX `EURUSD=X` |
| Refresh cadence | One fetch per app-day, persisted; manual `↻` button |
| Currency model | EUR is the home currency (CON3 native). COIN/BTC USD prices converted via FX rate. |
| Strategy model | Zone-based (Accumulate / Hold / Distribute), computed per ticker |
| Indicators | Bollinger(20,2σ), RSI(14), SMA(50/200), Ichimoku(9,26,52) |
| Indicator library | [`Atypical.TechnicalAnalysis.Functions`](https://github.com/phmatray/TaLibStandard) for Bollinger/RSI/SMA |
| Ichimoku | Computed locally — TA-Lib (and therefore TaLibStandard) does not include it |
| AI abstraction | [`Microsoft.Extensions.AI`](https://learn.microsoft.com/en-us/dotnet/ai/microsoft-extensions-ai) `IChatClient` |
| AI provider impl | `Anthropic.SDK` 5.10+ (registered as `IChatClient`); model `claude-opus-4-7` |
| AI cadence | One call per calendar day; cached; "Re-run" with confirmation |
| Position tracking | Manual trade log; lots derived from trades (FIFO) |
| Setup | Settings form for goal + many historic trades + CSV import |
| Visual direction | "The Vault" — see [`2026-05-06-tradystrat-vault-mockup.html`](./2026-05-06-tradystrat-vault-mockup.html) |
| Auth | None (localhost only) |
| Test framework | `xunit.v3` + `Shouldly` |
| Exceptions | Domain hierarchy rooted at `TradyStratException` |
| Multi-user | Out of scope |

## 3. Project layout

Single Blazor Server project; one `TradyStrat.csproj`. Folders are organised by **vertical feature** for the UI/services, complemented by orthogonal layers for use cases, specifications, modules, and shared primitives.

```
TradyStrat/
├─ Program.cs                       ← AppManager.Start(args, m => m.AddFromAssemblyOf<DashboardModule>())
├─ Dockerfile                       ← multi-stage build for .NET 10
├─ .dockerignore
├─ appsettings.json                 ← non-secret config (committed)
├─ appsettings.Development.json
│  user-secrets                     ← Anthropic:ApiKey (never in repo)
│
├─ Modules/                         ← TheAppManager IAppModule implementations
│  ├─ DatabaseModule.cs             ← EF Core, migrations on startup, Specifications wiring
│  ├─ DashboardModule.cs            ← dashboard service + components
│  ├─ PriceFeedModule.cs            ← Yahoo HttpClient + DailyPriceCache + hosted service
│  ├─ FxModule.cs                   ← EUR/USD provider + conversion service
│  ├─ IndicatorsModule.cs           ← IIndicator strategies + ZoneClassifier composite
│  ├─ TradesModule.cs               ← TradeService + CSV importer
│  ├─ AiSuggestionModule.cs         ← IChatClient registration (Anthropic.SDK)
│  ├─ SettingsModule.cs
│  └─ PortfolioModule.cs
│
├─ Application/                     ← orthogonal: use case layer
│  ├─ Abstractions/
│  │   ├─ IUseCase.cs               ← IUseCase<TInput, TOutput>
│  │   └─ UseCaseBase.cs            ← Template Method: logging, exception wrapping
│  └─ UseCases/
│      ├─ Dashboard/LoadDashboardUseCase.cs
│      ├─ Trades/{LogTrade,EditTrade,DeleteTrade,ImportTradesCsv}UseCase.cs
│      ├─ AiSuggestion/{GetTodaysSuggestion,ForceRefetchSuggestion}UseCase.cs
│      ├─ Prices/RefreshAllPricesUseCase.cs
│      └─ Settings/UpdateGoalUseCase.cs
│
├─ Features/                        ← Vertical slices: UI + feature services
│  ├─ Dashboard/
│  │   ├─ DashboardPage.razor + .razor.css   ← @page "/"
│  │   ├─ DashboardViewModel.cs
│  │   └─ Components/{VaultMasthead,HeroCapital,TodaysCallCard,PortfolioRail,GrowthChart,RefreshFab}.razor
│  ├─ PriceFeed/
│  │   ├─ IPriceFeed.cs · YahooPriceFeed.cs · YahooParser.cs
│  │   ├─ DailyPriceCache.cs                 ← Decorator over IPriceFeed
│  │   └─ PriceFeedHostedService.cs
│  ├─ Fx/
│  │   ├─ IFxRateProvider.cs · YahooFxProvider.cs
│  │   ├─ DailyFxCache.cs
│  │   └─ FxConverter.cs                     ← USD → EUR (Adapter)
│  ├─ Indicators/
│  │   ├─ IIndicator.cs                      ← marker
│  │   ├─ Bollinger.cs · Rsi.cs · MovingAverage.cs · Ichimoku.cs
│  │   ├─ TaLibAdapter.cs                    ← Adapter over TAFunc
│  │   ├─ Rules/{IZoneRule,BollingerZoneRule,RsiZoneRule,MovingAverageZoneRule,IchimokuZoneRule}.cs
│  │   ├─ ZoneClassifier.cs                  ← Composite of IZoneRule
│  │   └─ IndicatorEngine.cs
│  ├─ Trades/
│  │   ├─ TradesPage.razor                   ← @page "/trades"
│  │   ├─ TradeService.cs · CsvImportService.cs
│  │   └─ Components/AddTradeDialog.razor
│  ├─ AiSuggestion/
│  │   ├─ SnapshotBuilder.cs                 ← Factory Method (composes AiSnapshot)
│  │   ├─ SuggestionTool.cs                  ← AIFunction wrapper for tool-use
│  │   ├─ SuggestionParser.cs                ← tool-input → Suggestion
│  │   └─ SuggestionService.cs               ← orchestrates IChatClient call
│  ├─ Settings/
│  │   ├─ SettingsPage.razor                 ← @page "/settings"
│  │   └─ SettingsService.cs
│  └─ Portfolio/
│      ├─ PortfolioService.cs                ← Facade over trades + prices + FX
│      └─ GrowthSeriesBuilder.cs
│
├─ Specifications/                  ← orthogonal: Ardalis.Specification subclasses
│  ├─ Trades/{AllTrades,TradesByDateRange,LatestTrades}Spec.cs
│  ├─ PriceBars/{PriceBarsForTicker,LatestPriceBar,PriceBarsSince}Spec.cs
│  ├─ FxRates/LatestFxRateSpec.cs
│  └─ Suggestions/{SuggestionForDate,LatestSuggestion}Spec.cs
│
├─ Shared/
│  ├─ Domain/
│  │   ├─ Trade.cs · TradeSide.cs
│  │   ├─ PriceBar.cs · FxRate.cs
│  │   ├─ GoalConfig.cs · Suggestion.cs · SuggestionAction.cs · Citation.cs
│  │   ├─ Zone.cs · IndicatorReading.cs · BollingerReading.cs · IchimokuReading.cs
│  │   └─ Money.cs                           ← record struct (Amount, Currency)
│  ├─ Exceptions/
│  │   ├─ TradyStratException.cs             ← root
│  │   ├─ PriceFeedUnavailableException.cs
│  │   ├─ FxRateUnavailableException.cs
│  │   ├─ AnthropicCallFailedException.cs
│  │   ├─ AnthropicConfigurationException.cs
│  │   ├─ IndicatorComputationException.cs
│  │   ├─ TradeValidationException.cs
│  │   └─ CsvImportException.cs
│  └─ Time/
│      ├─ IClock.cs · SystemClock.cs
│
├─ Data/
│  ├─ AppDbContext.cs
│  ├─ Configurations/                        ← IEntityTypeConfiguration<T> per entity
│  └─ Migrations/
│
└─ wwwroot/
   ├─ css/vault.css
   └─ favicon, fonts preconnect
```

**Conventions**

- **Vertical slice ↔ Module** — every `Features/<X>/` has one `Modules/<X>Module.cs` registering the slice's services. The page/component code lives in `Features/`; the registration boilerplate is centralised in `Modules/` so `Program.cs` stays empty.
- **Use cases sit above features**, not inside them. They orchestrate feature services; Razor pages only depend on use cases.
- **Specifications are orthogonal**, not per-feature, because they describe persistent queries that can be reused across features.
- A feature folder depends on `Shared/`, `Application/Abstractions/`, `Specifications/` and `Data/AppDbContext` only — never on another feature folder.

## 4. Data model

SQLite via EF Core 10 (`Microsoft.EntityFrameworkCore.Sqlite`). Migrations checked into `Data/Migrations/` and applied on startup via `db.Database.Migrate()`. Entity configurations live in `Data/Configurations/<Entity>Configuration.cs` (one per entity, registered via `OnModelCreating`'s `ApplyConfigurationsFromAssembly`).

### Entity style

All persisted entities are `sealed record` types with **required** properties, **init** accessors (so they're effectively immutable post-construction), and **derived (computed) read-only properties**. EF Core 10's reflection-based materialiser handles records with required properties out of the box. Modifications happen through new instances + `db.Entry(entity).CurrentValues.SetValues(updated)` or via `db.Update(updated)` on records keyed by `Id`.

### Trade

```csharp
public sealed record Trade
{
    public required int Id { get; init; }
    public required DateOnly ExecutedOn { get; init; }
    public required TradeSide Side { get; init; }              // Buy | Sell
    public required decimal Quantity { get; init; }            // shares (decimals allowed)
    public required decimal PricePerShare { get; init; }       // EUR
    public decimal FeesEur { get; init; }                       // default 0
    public string? Note { get; init; }
    public required DateTime CreatedAt { get; init; }

    // Derived
    public decimal GrossEur => Quantity * PricePerShare;
    public decimal NetEur   => Side == TradeSide.Buy ? GrossEur + FeesEur : GrossEur - FeesEur;
    public bool    IsBuy    => Side == TradeSide.Buy;
}

public enum TradeSide { Buy = 1, Sell = 2 }
```

### PriceBar

```csharp
public sealed record PriceBar
{
    public required int Id { get; init; }
    public required string Ticker { get; init; }   // CON3.DE | COIN | BTC-USD
    public required DateOnly Date { get; init; }
    public required decimal Open { get; init; }
    public required decimal High { get; init; }
    public required decimal Low { get; init; }
    public required decimal Close { get; init; }
    public required long Volume { get; init; }

    // Derived
    public decimal Range  => High - Low;
    public decimal Change => Close - Open;
    public bool    IsUp   => Close >= Open;
}
```

### FxRate

```csharp
public sealed record FxRate
{
    public required int Id { get; init; }
    public required DateOnly Date { get; init; }
    public required string Pair { get; init; }       // "EURUSD"
    public required decimal UsdPerEur { get; init; } // standard quote: e.g. 1.08
    public required DateTime FetchedAt { get; init; }

    // Derived
    public decimal EurPerUsd => 1m / UsdPerEur;
}
```

### GoalConfig

```csharp
public sealed record GoalConfig
{
    public required int Id { get; init; }                 // singleton (= 1)
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

### Suggestion

```csharp
public sealed record Suggestion
{
    public required int Id { get; init; }
    public required DateOnly ForDate { get; init; }                 // unique
    public required SuggestionAction Action { get; init; }          // Acquire | Hold | Trim | Wait
    public decimal? QuantityHint { get; init; }
    public decimal? MaxPriceHint { get; init; }
    public required int Conviction { get; init; }                   // 1..5
    public required string Rationale { get; init; }
    public required string CitationsJson { get; init; }
    public required string PromptHash { get; init; }
    public required DateTime CreatedAt { get; init; }

    // Derived
    public decimal? OrderValueEur =>
        QuantityHint is decimal q && MaxPriceHint is decimal p ? q * p : null;

    public IReadOnlyList<Citation> Citations =>
        JsonSerializer.Deserialize<List<Citation>>(CitationsJson, JsonOpts.Strict) ?? [];
}

public enum SuggestionAction { Acquire = 1, Hold = 2, Trim = 3, Wait = 4 }

public sealed record Citation(string Claim, string Indicator, string Ticker, string Value);
```

### Indexes & constraints

- `PriceBar (Ticker, Date)` — unique
- `FxRate (Pair, Date)` — unique
- `Trade (ExecutedOn)` — for chronological reads
- `Suggestion (ForDate)` — unique
- `GoalConfig.Id` — always `1` (single row)

### Computed-only types (not persisted)

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

### Specifications

Query logic is encapsulated in `Specifications/` using `Ardalis.Specification`. Repositories or services consume them via `IRepositoryBase<T>` (also from Ardalis), or directly via `db.Trades.WithSpecification(spec)`.

```csharp
public sealed class TradesByDateRangeSpec : Specification<Trade>
{
    public TradesByDateRangeSpec(DateOnly from, DateOnly to)
    {
        Query.Where(t => t.ExecutedOn >= from && t.ExecutedOn <= to)
             .OrderBy(t => t.ExecutedOn);
    }
}

public sealed class LatestPriceBarSpec : Specification<PriceBar>
{
    public LatestPriceBarSpec(string ticker)
    {
        Query.Where(b => b.Ticker == ticker)
             .OrderByDescending(b => b.Date)
             .Take(1);
    }
}

public sealed class SuggestionForDateSpec : Specification<Suggestion>
{
    public SuggestionForDateSpec(DateOnly date)
    {
        Query.Where(s => s.ForDate == date);
    }
}

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

Use cases query via specs, never via raw `IQueryable`:

```csharp
var spec = new TradesByDateRangeSpec(from, to);
var trades = await db.Trades.WithSpecification(spec).ToListAsync(ct);
```

## 5. Indicators & zone classification

### Bollinger / RSI / SMA — Adapter over TaLibStandard

TaLibStandard's API is a faithful port of the original TA-Lib (out-arrays + `RetCode`). Each indicator class is a thin **Adapter** wrapping `TAFunc.*` and producing a clean record.

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

        if (rc != RetCode.Success || nb == 0)
            throw new IndicatorComputationException($"Bollinger failed: {rc}");

        int last = nb - 1;
        var sigma = (decimal)((upper[last] - middle[last]) / 2.0);
        return new BollingerReading(
            (decimal)upper[last], (decimal)middle[last], (decimal)lower[last], sigma);
    }
}
```

`Rsi` and `MovingAverage` follow the same pattern (`TAFunc.Rsi`, `TAFunc.Sma`).

### Ichimoku — computed locally

TA-Lib (and therefore TaLibStandard) does not include Ichimoku — verified by enumerating TaLibStandard's full Functions catalogue (Adx → Wma, no `Ichimoku`/`Tenkan`/`Kijun`/`Senkou`/`Chikou` symbols anywhere in the dev branch). We compute it ourselves; it's only rolling highs/lows + offsets.

```csharp
public sealed record IchimokuReading(
    decimal Tenkan,                         // (max(high,9)  + min(low,9))  / 2
    decimal Kijun,                          // (max(high,26) + min(low,26)) / 2
    decimal SenkouA,                        // (Tenkan + Kijun) / 2 — leading span A
    decimal SenkouB,                        // (max(high,52) + min(low,52)) / 2 — leading span B
    decimal Chikou,                         // close shifted -26
    IchimokuSignal Signal);                 // AboveCloud | InCloud | BelowCloud

public enum IchimokuSignal { AboveCloud = 1, InCloud = 2, BelowCloud = 3 }

public static class Ichimoku
{
    public static IchimokuReading? LatestFor(IReadOnlyList<PriceBar> bars)
    {
        if (bars.Count < 52 + 26) return null;

        decimal MidOver(int n)
        {
            var window = bars.Skip(bars.Count - n).Take(n).ToArray();
            return (window.Max(b => b.High) + window.Min(b => b.Low)) / 2m;
        }

        var tenkan  = MidOver(9);
        var kijun   = MidOver(26);
        var senkouA = (tenkan + kijun) / 2m;
        var senkouB = MidOver(52);
        var chikou  = bars[^27].Close;
        var price   = bars[^1].Close;
        var top     = Math.Max(senkouA, senkouB);
        var bottom  = Math.Min(senkouA, senkouB);
        var signal  = price > top ? IchimokuSignal.AboveCloud
                    : price < bottom ? IchimokuSignal.BelowCloud
                    : IchimokuSignal.InCloud;

        return new IchimokuReading(tenkan, kijun, senkouA, senkouB, chikou, signal);
    }
}
```

### Zone rules — Strategy + Composite

Each indicator's vote rule implements `IZoneRule` (**Strategy**). The `ZoneClassifier` (**Composite**) aggregates votes from all rules registered in DI:

```csharp
public sealed record ZoneVote(Zone Vote, string Reason);

public interface IZoneRule
{
    string Name { get; }
    ZoneVote? Apply(decimal price, IndicatorBundle r);   // null when prerequisite reading is missing
}

public sealed record IndicatorBundle(
    BollingerReading? Bollinger, decimal? Rsi,
    decimal? Sma50, decimal? Sma200, IchimokuReading? Ichimoku);

public sealed class BollingerZoneRule : IZoneRule
{
    public string Name => nameof(Bollinger);
    public ZoneVote? Apply(decimal price, IndicatorBundle r) => r.Bollinger switch
    {
        null => null,
        var bb when price < bb.Lower => new(Zone.Accumulate, $"Price {price:F2} below lower Bollinger ({bb.Lower:F2})"),
        var bb when price > bb.Upper => new(Zone.Distribute, $"Price above upper Bollinger ({bb.Upper:F2})"),
        _ => new(Zone.Hold, "Inside Bollinger band")
    };
}
// RsiZoneRule, MovingAverageZoneRule, IchimokuZoneRule similarly
```

```csharp
public sealed class ZoneClassifier(IEnumerable<IZoneRule> rules)
{
    public (Zone Zone, IReadOnlyList<string> Reasons) Classify(decimal price, IndicatorBundle r)
    {
        var votes = rules
            .Select(rule => rule.Apply(price, r))
            .Where(v => v is not null)
            .Select(v => v!)
            .ToList();

        if (votes.Count == 0) return (Zone.Hold, []);

        var groups = votes.GroupBy(v => v.Vote)
                          .Select(g => (zone: g.Key, n: g.Count()))
                          .OrderByDescending(x => x.n)
                          .ToList();

        var majority = groups[0].zone;
        if (groups.Count > 1 && groups[0].n == groups[1].n)
            majority = Zone.Hold;                                 // tie → conservative

        return (majority, votes.Select(v => v.Reason).ToList());
    }
}
```

### IndicatorEngine

```csharp
public sealed class IndicatorEngine(IReadRepositoryBase<PriceBar> bars, ZoneClassifier classifier)
{
    public async Task<IndicatorReading> ComputeFor(string ticker, CancellationToken ct = default)
    {
        var series = await bars.ListAsync(new PriceBarsForTickerSpec(ticker), ct);
        if (series.Count == 0)
            throw new IndicatorComputationException($"No price bars for {ticker}");

        var price = series[^1].Close;
        var bundle = new IndicatorBundle(
            Bollinger.LatestFor(series),
            Rsi.LatestFor(series),
            MovingAverage.LatestFor(series, 50),
            MovingAverage.LatestFor(series, 200),
            Ichimoku.LatestFor(series));

        var (zone, reasons) = classifier.Classify(price, bundle);
        return new IndicatorReading(ticker, price,
            bundle.Bollinger, bundle.Rsi, bundle.Sma50, bundle.Sma200, bundle.Ichimoku,
            zone, reasons);
    }
}
```

## 6. FX conversion — EUR is the home currency

CON3 trades on XETRA in EUR; COIN and BTC-USD prices come from Yahoo in USD. The dashboard is denominated in EUR (the goal is €1,000,000). All USD values pass through `FxConverter` before display or before being included in the AI snapshot.

### Source

Yahoo Finance ticker `EURUSD=X` returns daily OHLC for the EUR/USD pair, quoted standard-style as **USD per 1 EUR** (e.g., 1.0820). One FX bar per day, fetched alongside price bars by the same hosted service.

### Provider

```csharp
public interface IFxRateProvider
{
    Task<IReadOnlyList<FxRate>> FetchAsync(string pair, DateOnly from, DateOnly to, CancellationToken ct);
}

public sealed class YahooFxProvider(HttpClient http) : IFxRateProvider
{
    public Task<IReadOnlyList<FxRate>> FetchAsync(string pair, DateOnly from, DateOnly to, CancellationToken ct)
    {
        // Calls /v8/finance/chart/EURUSD=X with the same envelope as YahooPriceFeed.
        // Maps Close → UsdPerEur. Throws FxRateUnavailableException on transient/parse failure.
    }
}
```

### Cache

`DailyFxCache.EnsureFreshAsync(pair = "EURUSD", ct)` is called by `PriceFeedHostedService` on startup, alongside the three price fetches. Same logic as `DailyPriceCache`: skip if today's bar already in DB.

### Converter

```csharp
public sealed class FxConverter(IReadRepositoryBase<FxRate> rates)
{
    public async Task<decimal> UsdToEurAsync(decimal usd, DateOnly asOf, CancellationToken ct)
    {
        var fx = await rates.FirstOrDefaultAsync(new LatestFxRateSpec("EURUSD", asOf), ct)
            ?? throw new FxRateUnavailableException($"No EURUSD rate on or before {asOf}");
        return usd * fx.EurPerUsd;
    }
}
```

### Where it's used

- **`PortfolioRail`** (UI) — COIN and BTC tiles show the EUR-converted price prominently with the original USD figure as a subtitle: `€199 · $215`.
- **`SnapshotBuilder`** — the AI snapshot includes both the USD price and the FX-as-of date for each USD-denominated ticker so the model can reason in either currency. The `Citation.value` strings will say e.g. *"COIN $215 (≈ €199 at 1.0820)"*.
- **`PortfolioService`** — irrelevant: position math uses CON3 EUR prices throughout.

## 7. AI integration — Microsoft.Extensions.AI

The application depends on `Microsoft.Extensions.AI`'s `IChatClient` abstraction. The Anthropic-specific wiring is contained in `AiSuggestionModule`. Switching providers later (Bedrock, Vertex, OpenAI) is a one-module change.

### Module

```csharp
// Modules/AiSuggestionModule.cs
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
                .AsChatClient(model)                  // M.E.AI adapter from Anthropic.SDK
                .AsBuilder()
                .UseFunctionInvocation()              // tool calling
                .Build());

        builder.Services.AddScoped<SnapshotBuilder>();
        builder.Services.AddScoped<SuggestionParser>();
        builder.Services.AddScoped<SuggestionService>();
    }
}
```

> **Implementation note:** verify the exact `Anthropic.SDK 5.10+` extension method name at impl time (`AsChatClient` vs `AsIChatClient`). The shape — an extension that exposes `IChatClient` — is stable; the name is the only thing to confirm.

### Tool-use for structured output

`Microsoft.Extensions.AI` provides `AIFunctionFactory.Create` to expose .NET delegates as tools. We define a single tool `submit_suggestion` and configure the chat call to require it.

```csharp
public sealed class SuggestionService(
    IChatClient chat,
    SnapshotBuilder snap,
    IRepositoryBase<Suggestion> repo,
    IClock clock)
{
    private const string ToolName  = "submit_suggestion";
    private const string SystemMsg = """
        You are a disciplined trading assistant for a personal accumulation strategy
        on CON3 (a 3x leveraged Coinbase ETP). You see snapshots once per day.
        Cite which indicators support each part of your suggestion.
        Be conservative: when signals conflict, say Hold.
        """;

    public async Task<Suggestion> AskAsync(AiSnapshot snapshot, CancellationToken ct)
    {
        Suggestion? captured = null;

        var submit = AIFunctionFactory.Create(
            (SuggestionAction action, decimal? quantityHint, decimal? maxPriceHint,
             int conviction, string rationale, IReadOnlyList<Citation> citations) =>
            {
                captured = new Suggestion
                {
                    Id = 0, // assigned by EF
                    ForDate = snapshot.Today,
                    Action = action,
                    QuantityHint = quantityHint,
                    MaxPriceHint = maxPriceHint,
                    Conviction = conviction,
                    Rationale = rationale,
                    CitationsJson = JsonSerializer.Serialize(citations, JsonOpts.Strict),
                    PromptHash = snapshot.PromptHash,
                    CreatedAt = clock.UtcNow(),
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
            new(ChatRole.System, SystemMsg),
            new(ChatRole.User,   JsonSerializer.Serialize(snapshot, JsonOpts.Strict)),
        };

        try
        {
            await chat.GetResponseAsync(messages, options, ct);
        }
        catch (Exception ex) when (ex is not AnthropicCallFailedException)
        {
            throw new AnthropicCallFailedException("Anthropic call failed.", ex);
        }

        return captured
            ?? throw new AnthropicCallFailedException("Model did not invoke submit_suggestion.");
    }
}
```

### Caching — once per day

```csharp
// Application/UseCases/AiSuggestion/GetTodaysSuggestionUseCase.cs
public sealed class GetTodaysSuggestionUseCase(
    IRepositoryBase<Suggestion> repo,
    SuggestionService svc,
    SnapshotBuilder snap,
    IClock clock,
    ILogger<GetTodaysSuggestionUseCase> log)
    : UseCaseBase<Unit, Suggestion>(log)
{
    protected override async Task<Suggestion> ExecuteCore(Unit _, CancellationToken ct)
    {
        var today = clock.TodayLocal();
        var existing = await repo.FirstOrDefaultAsync(new SuggestionForDateSpec(today), ct);
        if (existing is not null) return existing;

        var snapshot = await snap.BuildAsync(ct);
        var fresh    = await svc.AskAsync(snapshot, ct);
        await repo.AddAsync(fresh, ct);
        return fresh;
    }
}

// ForceRefetchSuggestionUseCase deletes today's row first, then runs the same flow.
```

The `Re-run AI` button on the dashboard opens a confirmation dialog (*"This will use one Anthropic API call. Continue?"*) before invoking `ForceRefetchSuggestionUseCase`.

## 8. Data flow

### Yahoo Finance — `YahooPriceFeed`

```csharp
public interface IPriceFeed
{
    Task<IReadOnlyList<PriceBar>> FetchDailyAsync(
        string ticker, DateOnly from, DateOnly to, CancellationToken ct);
}

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
            resp.EnsureSuccessStatusCode();
            using var stream = await resp.Content.ReadAsStreamAsync(ct);
            var doc = await JsonDocument.ParseAsync(stream, cancellationToken: ct);
            return YahooParser.ParseDaily(ticker, doc);
        }
        catch (Exception ex) when (ex is HttpRequestException or JsonException or TaskCanceledException)
        {
            throw new PriceFeedUnavailableException($"Yahoo fetch failed for {ticker}", ex);
        }
    }
}
```

Module registration with Polly retry (transient errors only):

```csharp
// Modules/PriceFeedModule.cs
public sealed class PriceFeedModule : IAppModule
{
    public void ConfigureServices(WebApplicationBuilder builder)
    {
        builder.Services.AddHttpClient<IPriceFeed, YahooPriceFeed>(c =>
        {
            c.BaseAddress = new Uri("https://query1.finance.yahoo.com");
            c.Timeout     = TimeSpan.FromSeconds(15);
            c.DefaultRequestHeaders.UserAgent.ParseAdd("TradyStrat/1.0");
        }).AddPolicyHandler(GetRetryPolicy());

        builder.Services.AddScoped<DailyPriceCache>();
        builder.Services.AddHostedService<PriceFeedHostedService>();
    }
}
```

### `DailyPriceCache` — Decorator

`DailyPriceCache` wraps `IPriceFeed` and adds **persist + skip-if-fresh** semantics. From the rest of the app's perspective, it's an enhanced `IPriceFeed` with caching — the **Decorator** pattern in its textbook form.

```csharp
public sealed class DailyPriceCache(
    IPriceFeed feed,
    IRepositoryBase<PriceBar> bars,
    IClock clock,
    ILogger<DailyPriceCache> log)
{
    public async Task EnsureFreshAsync(string ticker, CancellationToken ct)
    {
        var today  = clock.TodayInExchangeTzFor(ticker);
        var latest = (await bars.FirstOrDefaultAsync(new LatestPriceBarSpec(ticker), ct))?.Date;
        if (latest == today) return;

        var from = latest?.AddDays(1) ?? today.AddYears(-2);
        var fetched = await feed.FetchDailyAsync(ticker, from, today, ct);
        foreach (var b in fetched) await bars.AddAsync(b, ct);
        await bars.SaveChangesAsync(ct);
        log.LogInformation("PriceFeed: appended {N} bars for {Ticker}", fetched.Count, ticker);
    }
}
```

`PriceFeedHostedService.StartAsync` calls `EnsureFreshAsync` for `CON3.DE`, `COIN`, `BTC-USD`, plus `DailyFxCache.EnsureFreshAsync("EURUSD")`.

**Per-ticker timezone for "today"** — avoids cache thinking it's already updated when the relevant market hasn't closed:
- CON3.DE → Europe/Berlin (XETRA close)
- COIN → America/New_York
- BTC-USD → UTC
- EURUSD → UTC

### Configuration & secrets

`appsettings.json` (committed):
```json
{
  "Anthropic":  { "Model": "claude-opus-4-7", "MaxTokens": 1500 },
  "Yahoo":      { "BaseUrl": "https://query1.finance.yahoo.com" },
  "Tickers":    { "Focus": "CON3.DE", "Context": ["COIN", "BTC-USD"] },
  "Fx":         { "Pair": "EURUSD" },
  "Database":   { "Path": "~/Library/Application Support/TradyStrat/tradystrat.db" }
}
```

Secrets via `dotnet user-secrets`:
```
Anthropic:ApiKey = sk-ant-...
```

`AiSuggestionModule` throws `AnthropicConfigurationException` on startup if the key is missing.

### End-to-end daily flow

```
PriceFeedHostedService.StartAsync
  → DailyPriceCache.EnsureFreshAsync × 3 tickers           (one fetch/day)
  → DailyFxCache.EnsureFreshAsync("EURUSD")

User opens "/"
  → Razor injects LoadDashboardUseCase
  → LoadDashboardUseCase.ExecuteAsync
      → IndicatorEngine.ComputeFor × 3 tickers
      → FxConverter.UsdToEurAsync (per USD ticker)
      → PortfolioService.SnapshotAsync (lots from Trade history)
      → GrowthSeriesBuilder.BuildAsync (daily portfolio value series)
      → GetTodaysSuggestionUseCase (cached or first call)
  → DashboardViewModel populated → Razor page renders The Vault layout
```

## 9. UI — The Vault

**Visual reference:** [`2026-05-06-tradystrat-vault-mockup.html`](./2026-05-06-tradystrat-vault-mockup.html) (open in a browser; Direction B is the agreed concept). The mockup represents pixel-level intent; the implementation matches it within ±1 token (not ±1px).

### Aesthetic

Warm-dark, gold-accent, editorial-private-banking. Cormorant Garamond display, JetBrains Mono numerics, generous negative space. Dark `#0E0D0A` ground, ivory `#ECE6D6` ink, brushed gold `#C49A56` accent. The capital amount is the hero of the page.

### Page composition

```
DashboardPage.razor
├─ <VaultMasthead />                ← brand · entry no. · date
├─ <HeroCapital />                  ← €174,290 / of one million / progress bar
├─ <TodaysCallCard />               ← AI verb + order + reasons + buttons
├─ <PortfolioRail />                ← position cell + 3 ticker cells (USD shown alongside EUR)
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

.num   { font-family: var(--font-mono); font-variant-numeric: tabular-nums; }
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
    GoalConfig Goal,
    Suggestion TodaysCall,
    IReadOnlyList<TickerView> Tickers,                // CON3.DE (EUR), COIN (USD+EUR), BTC-USD (USD+EUR)
    IReadOnlyList<GrowthPoint> Growth,                // {Date, ValueEur}
    DateOnly? LatestPriceDate);                       // for "data as of …" footer

public sealed record TickerView(
    string Ticker, decimal Price, string Currency,
    decimal? PriceEur,                                // converted via FX, null for CON3.DE
    decimal? DeltaPct,
    Zone Zone);
```

### Razor page → Use Case binding

```razor
@page "/"
@inject LoadDashboardUseCase LoadDashboard
@inject ForceRefetchSuggestionUseCase ForceRefetch
@inject RefreshAllPricesUseCase RefreshPrices

@code {
    private DashboardViewModel? _vm;

    protected override async Task OnInitializedAsync()
        => _vm = await LoadDashboard.ExecuteAsync(Unit.Value, default);

    private async Task ReloadAsync()
        => _vm = await LoadDashboard.ExecuteAsync(Unit.Value, default);

    private async Task OnRefreshClicked()
    {
        await RefreshPrices.ExecuteAsync(Unit.Value, default);
        await ReloadAsync();
    }

    private async Task OnRerunAiConfirmed()
    {
        await ForceRefetch.ExecuteAsync(Unit.Value, default);
        await ReloadAsync();
    }
}
```

### Components

`<HeroCapital />`, `<TodaysCallCard />`, `<PortfolioRail />`, `<GrowthChart />` follow the markup and CSS shown in the mockup HTML referenced above. `<GrowthChart />` is custom SVG (no chart library) — the gold line + glow + dashed goal trajectory cannot be matched by an off-the-shelf component without heavy override. ~80 lines of Razor + a small `Path`-builder helper. Y-scale is anchored to `Goal.TargetEur`.

### Settings & Trades pages

- `/settings` — form with `Goal target (EUR)` (default 1,000,000), `Target date (optional)`, `Focus ticker` (default `CON3.DE`, read-only). Submits via `UpdateGoalUseCase`.
- `/trades` — table of trades, oldest first. Header buttons: `+ Add trade` (opens `<AddTradeDialog />`) and `Import CSV` (parses `date,side,qty,price,fees`). Inline edit + delete with confirm. All actions go through `LogTradeUseCase`, `EditTradeUseCase`, `DeleteTradeUseCase`, `ImportTradesCsvUseCase`.

### Motion

- **Page-load reveal:** hero amount counts up 0 → current over 800ms; stagger 60ms.
- **Gold-line draw:** `path.line` `stroke-dasharray` reveal, 1200ms ease-out.
- **Re-run AI:** verb cross-fades when new suggestion arrives.

No hover bounces, no parallax, no scroll-jacking.

## 10. Use cases — the application layer

Every user action and every page-load orchestration is a `IUseCase<TInput, TOutput>` (Command pattern). Razor pages depend on use cases, never on services directly. This forces the dependency direction to flow `UI → UseCases → FeatureServices → Repositories/Specs`.

### Abstractions

```csharp
// Application/Abstractions/IUseCase.cs
public interface IUseCase<in TInput, TOutput>
{
    Task<TOutput> ExecuteAsync(TInput input, CancellationToken ct);
}

public readonly record struct Unit
{
    public static readonly Unit Value = default;
}
```

### Template Method base — logging + exception wrapping

```csharp
// Application/Abstractions/UseCaseBase.cs
public abstract class UseCaseBase<TInput, TOutput>(ILogger logger) : IUseCase<TInput, TOutput>
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
            // Domain exceptions bubble up untouched — they're already typed.
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

### Concrete use cases (one per file)

```
Application/UseCases/Dashboard/LoadDashboardUseCase.cs
Application/UseCases/Trades/LogTradeUseCase.cs
Application/UseCases/Trades/EditTradeUseCase.cs
Application/UseCases/Trades/DeleteTradeUseCase.cs
Application/UseCases/Trades/ImportTradesCsvUseCase.cs
Application/UseCases/AiSuggestion/GetTodaysSuggestionUseCase.cs
Application/UseCases/AiSuggestion/ForceRefetchSuggestionUseCase.cs
Application/UseCases/Prices/RefreshAllPricesUseCase.cs
Application/UseCases/Settings/UpdateGoalUseCase.cs
```

### Example — `LogTradeUseCase`

```csharp
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
        if (input.Quantity <= 0)
            throw new TradeValidationException("Quantity must be positive.");
        if (input.PricePerShare <= 0)
            throw new TradeValidationException("Price per share must be positive.");

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

## 11. Application startup with TheAppManager

`TheAppManager.Modules.IAppModule` exposes three optional hooks (all default no-op):
- `ConfigureServices(WebApplicationBuilder builder)` — register DI services (access via `builder.Services` and `builder.Configuration`)
- `ConfigureMiddleware(WebApplication app)` — pipeline order
- `ConfigureEndpoints(IEndpointRouteBuilder endpoints)` — endpoint mapping

`Program.cs` delegates to auto-discovery (modules found in the entry assembly):

```csharp
// Program.cs
using TheAppManager.Startup;

AppManager.Start(args);   // auto-discovers all IAppModule types in this assembly
```

If we ever need explicit ordering, the form becomes `AppManager.Start(args, m => m.Add<DatabaseModule>().Add<DashboardModule>())`. For TradyStrat, registration order doesn't matter (no middleware ordering dependencies).

### `DatabaseModule` — DI registration + startup migrations

```csharp
// Modules/DatabaseModule.cs
public sealed class DatabaseModule : IAppModule
{
    public void ConfigureServices(WebApplicationBuilder builder)
    {
        var dbPath = ExpandPath(builder.Configuration["Database:Path"]!);
        builder.Services.AddDbContext<AppDbContext>(opt => opt.UseSqlite($"Data Source={dbPath}"));
        builder.Services.AddScoped(typeof(IRepositoryBase<>),     typeof(EfRepository<>));
        builder.Services.AddScoped(typeof(IReadRepositoryBase<>), typeof(EfRepository<>));
    }

    public void ConfigureMiddleware(WebApplication app)
    {
        // Apply pending migrations before any request is served.
        using var scope = app.Services.CreateScope();
        scope.ServiceProvider.GetRequiredService<AppDbContext>().Database.Migrate();
    }
}
```

### `IndicatorsModule`

```csharp
public sealed class IndicatorsModule : IAppModule
{
    public void ConfigureServices(WebApplicationBuilder builder)
    {
        // GoF Strategy registrations — order doesn't matter for ZoneClassifier
        builder.Services.AddSingleton<IZoneRule, BollingerZoneRule>();
        builder.Services.AddSingleton<IZoneRule, RsiZoneRule>();
        builder.Services.AddSingleton<IZoneRule, MovingAverageZoneRule>();
        builder.Services.AddSingleton<IZoneRule, IchimokuZoneRule>();
        builder.Services.AddScoped<ZoneClassifier>();
        builder.Services.AddScoped<IndicatorEngine>();
    }
}
```

### `DashboardModule`

```csharp
public sealed class DashboardModule : IAppModule
{
    public void ConfigureServices(WebApplicationBuilder builder)
    {
        builder.Services.AddRazorComponents().AddInteractiveServerComponents();
        builder.Services.AddScoped<LoadDashboardUseCase>();
        builder.Services.AddScoped<RefreshAllPricesUseCase>();
        builder.Services.AddScoped<ForceRefetchSuggestionUseCase>();
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

(Other modules — `PriceFeedModule`, `FxModule`, `TradesModule`, `AiSuggestionModule`, `SettingsModule`, `PortfolioModule` — follow the same shape: register their feature's services + use cases. They typically only override `ConfigureServices`.)

## 12. Custom exceptions

All domain failures throw a typed exception derived from `TradyStratException`. Generic `Exception`, `InvalidOperationException`, etc. are reserved for true programmer errors.

```csharp
// Shared/Exceptions/TradyStratException.cs
public abstract class TradyStratException : Exception
{
    protected TradyStratException(string message, Exception? inner = null)
        : base(message, inner) { }
}

public sealed class PriceFeedUnavailableException(string message, Exception? inner = null)
    : TradyStratException(message, inner);

public sealed class FxRateUnavailableException(string message, Exception? inner = null)
    : TradyStratException(message, inner);

public sealed class AnthropicCallFailedException(string message, Exception? inner = null)
    : TradyStratException(message, inner);

public sealed class AnthropicConfigurationException(string message)
    : TradyStratException(message);

public sealed class IndicatorComputationException(string message, Exception? inner = null)
    : TradyStratException(message, inner);

public sealed class TradeValidationException(string message)
    : TradyStratException(message);

public sealed class CsvImportException(string message, int? lineNumber = null)
    : TradyStratException(lineNumber.HasValue ? $"line {lineNumber}: {message}" : message)
{
    public int? LineNumber { get; } = lineNumber;
}
```

A global Razor error boundary in `MainLayout.razor` catches `TradyStratException` and renders a domain-specific banner with the message; everything else falls through to Blazor's default error UI.

## 13. Error handling

Three failure surfaces, three policies — all backed by typed exceptions from §12.

**1. Yahoo Finance / FX failure** (`PriceFeedUnavailableException`, `FxRateUnavailableException`)
- HttpClient registered with **Polly**: 2 retries, exponential backoff (200ms, 800ms), transient errors only.
- Final failure surfaces a non-blocking dashboard banner: *"Prices unavailable — last data from 5 May 2026. [Retry]"*.
- Page still renders from the cached `PriceBar`/`FxRate` rows. Indicators and AI snapshot still compute.
- `↻` button shows spinner; on failure, the Razor error boundary displays the banner and re-enables.

**2. Anthropic call failure** (`AnthropicCallFailedException`)
- Single retry with 1s backoff inside `SuggestionService`; otherwise rethrow.
- `<TodaysCallCard />` shows: *"Could not fetch today's call. [Try again]"*, with the previous day's suggestion below as `Yesterday's call (cached)`.
- Never fall back to a hand-rolled "default" suggestion.

**3. SQLite / EF Core failure** (programmer error, generally not wrapped)
- Let `DbUpdateException`/`SqliteException` propagate — these usually indicate schema drift or disk problems, not user-facing conditions.
- `db.Database.Migrate()` runs on startup. Migration failure crashes the host with a clear log line — better than running on a half-migrated DB.

### Logging

A single Serilog sink writes to `~/Library/Application Support/TradyStrat/logs/tradystrat-yyyymmdd.log`, rotated daily, retained 14 days. No external telemetry.

## 14. Testing

`xunit.v3` + `Shouldly` (matches TaLibStandard's stack and modernises the test runner). Pyramid biased toward fast pure-function tests where the math lives.

```
TradyStrat.Tests/
├─ TradyStrat.Tests.csproj          ← <PackageReference Include="xunit.v3" />
│                                     <PackageReference Include="xunit.runner.visualstudio" />
│                                     <PackageReference Include="Shouldly" />
│
├─ Indicators/
│  ├─ BollingerTests.cs             ← golden 250-bar fixture; expected from TA-Lib reference
│  ├─ RsiTests.cs · MovingAverageTests.cs · IchimokuTests.cs
│  └─ Rules/                        ← one test class per IZoneRule + ZoneClassifierTests
│
├─ PriceFeed/
│  ├─ YahooParserTests.cs           ← captured JSON fixtures, no network
│  └─ DailyPriceCacheTests.cs       ← in-memory SQLite + fake IPriceFeed
│
├─ Fx/
│  └─ FxConverterTests.cs           ← in-memory + fake IFxRateProvider
│
├─ AiSuggestion/
│  ├─ SnapshotBuilderTests.cs
│  ├─ SuggestionParserTests.cs      ← AIFunction tool-input ↔ Suggestion roundtrip
│  └─ SuggestionServiceTests.cs     ← in-memory + fake IChatClient (returns canned tool call)
│
├─ Portfolio/
│  ├─ PortfolioServiceTests.cs      ← FIFO sells, partial sells, fees
│  └─ GrowthSeriesBuilderTests.cs
│
├─ Trades/
│  └─ TradeServiceTests.cs          ← add/edit/delete + CSV parsing
│
├─ Specifications/
│  └─ SpecsRoundtripTests.cs        ← each Spec compiled against in-memory SQLite returns expected rows
│
├─ UseCases/
│  ├─ LoadDashboardUseCaseTests.cs  ← composes all fakes
│  ├─ LogTradeUseCaseTests.cs       ← validation + happy path
│  └─ ForceRefetchSuggestionUseCaseTests.cs
│
└─ Modules/
   └─ ModuleSmokeTests.cs           ← AppModuleTestHost from TheAppManager: each module wires up cleanly
```

**Microsoft.Extensions.AI fakes** — implement a minimal `IChatClient` that returns a pre-canned tool call. No network, deterministic.

**Not tested:**
- Razor components — they are dumb renderers of `DashboardViewModel`; the interesting behaviour is in use cases.
- Live Yahoo or Anthropic — flaky for CI and wastes API budget. Captured fixtures cover JSON parsing.

**Coverage target:** 80% on `Features/Indicators/`, `Features/Portfolio/`, `Features/PriceFeed/parsing`, `Features/Fx/`, `Features/AiSuggestion/parsing`, and `Application/UseCases/`. No coverage chasing on Razor or DI wiring.

## 15. Run & deploy

### Local one-command run

```
dotnet run --project TradyStrat
# → http://127.0.0.1:5180
```

**First-run setup (in `README.md`):**
```
dotnet user-secrets set "Anthropic:ApiKey" "sk-ant-…"
dotnet ef database update                               # creates tradystrat.db
dotnet run --project TradyStrat
```

**Database location:** `~/Library/Application Support/TradyStrat/tradystrat.db`. Configurable via `appsettings.json`. Backup = copy the file.
**Logs:** same parent directory under `logs/tradystrat-yyyymmdd.log`, rotated daily, 14 days retention.
**Networking:** Kestrel bound to `127.0.0.1:5180` only.

### Docker

`Dockerfile` (multi-stage; .NET 10 LTS images on Alpine):

```dockerfile
# syntax=docker/dockerfile:1.7
FROM mcr.microsoft.com/dotnet/sdk:10.0-alpine AS build
WORKDIR /src
COPY *.sln Directory.Build.props Directory.Packages.props ./
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
EXPOSE 5180
VOLUME /data
ENTRYPOINT ["dotnet", "TradyStrat.dll"]
```

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
```

**Run with Docker:**
```
docker build -t tradystrat:latest .
docker run --rm -it \
  -p 127.0.0.1:5180:5180 \
  -v "$HOME/Library/Application Support/TradyStrat":/data \
  -e Anthropic__ApiKey=$ANTHROPIC_API_KEY \
  tradystrat:latest
```

The image is for parity (run anywhere) and easy distribution. **It does not change the security model** — exposure stays localhost-only via the `127.0.0.1:5180` port mapping. LAN exposure is a future change with auth attached; out of scope here.

## 16. Out of scope (explicit)

- Multi-user, auth, reverse proxy, TLS.
- Broker integration; trades are entered manually (CSV is the only bulk path).
- Real-time prices or websockets — daily bars only.
- Alerts, push notifications, scheduled emails.
- Backtesting — AI and indicators consume only current state.
- Mobile-specific view; CSS is responsive enough at ~720px but not phone-designed.
- Tickers other than CON3.DE / COIN / BTC-USD; the focus ticker is hard-decided.
- Currencies other than EUR/USD.

## 17. GoF patterns at a glance

Patterns are applied where they fit naturally — not forced. Each entry below names where the pattern lives so reviewers and future maintainers can recognise it.

| Pattern | Where | Why |
|---|---|---|
| **Adapter** | `TaLibAdapter`, indicator wrappers (`Bollinger`, `Rsi`, `MovingAverage`); `FxConverter`; `Anthropic.SDK.AsChatClient(...)` | Hide verbose / external APIs behind clean records and the `IChatClient` abstraction. |
| **Strategy** | `IZoneRule` implementations: `BollingerZoneRule`, `RsiZoneRule`, `MovingAverageZoneRule`, `IchimokuZoneRule` | Each indicator's vote rule is interchangeable; the classifier doesn't know which rules are present. |
| **Composite** | `ZoneClassifier` over `IEnumerable<IZoneRule>` | Aggregates Strategy results into a single Zone with majority + tie-break. |
| **Decorator** | `DailyPriceCache` over `IPriceFeed`; `DailyFxCache` over `IFxRateProvider` | Add persistence + skip-if-fresh without changing the underlying interface. |
| **Facade** | `PortfolioService`, `DashboardService`, `LoadDashboardUseCase` | Single entry points hiding multi-component orchestration. |
| **Command** | `IUseCase<TInput, TOutput>` implementations | Each user action and page-load is an explicit command object, testable in isolation. |
| **Template Method** | `UseCaseBase<TIn, TOut>.ExecuteAsync` with abstract `ExecuteCore` | Cross-cutting logging + exception wrapping; concrete use cases override the body. |
| **Factory Method** | `SnapshotBuilder.BuildAsync`; `GoalConfig.Default` | Construct rich aggregate objects (`AiSnapshot`, default config) from many sources. |
| **Specification** | `Ardalis.Specification` subclasses in `Specifications/` | Encapsulate query logic, reusable + composable, testable separately. |
| **Singleton** (DI lifetime) | `IClock`, `IChatClient`, all `IZoneRule` implementations | Single instance for stateless services; managed by the DI container. |
| **Module** (TheAppManager, related to Composite/Facade) | `Modules/<X>Module.cs` | Each feature owns its DI registration, middleware, and endpoints. |

## 18. Architecture summary

```
                       ┌──────────────────────────────────────┐
                       │       Blazor Server (.NET 10)        │
                       │                                      │
   browser  ────────►  │  Pages: / · /trades · /settings      │
                       │      │                               │
                       │      ▼                               │
                       │  Use Cases (Application/UseCases)    │
                       │      │                               │
                       │      ▼                               │
                       │  Feature services (Features/*)       │
                       │   · IndicatorEngine (TaLibStandard)  │
                       │   · IndicatorRules (Strategy)        │
                       │   · ZoneClassifier (Composite)       │
                       │   · DailyPriceCache (Decorator)      │
                       │   · DailyFxCache (Decorator)         │
                       │   · FxConverter (Adapter)            │
                       │   · SnapshotBuilder (Factory)        │
                       │   · SuggestionService → IChatClient ────► Anthropic.SDK ──► api.anthropic.com
                       │   · PortfolioService (Facade)        │
                       │      │                               │
                       │      ▼                               │
                       │  Specifications (Ardalis)            │
                       │      │                               │
                       │      ▼                               │
                       │  EF Core 10 ──► SQLite (local file)  │
                       │      ▲                               │
                       │      │ once/day                      │
                       │  PriceFeedHostedService ───────────────► Yahoo Finance (chart + EURUSD=X)
                       │                                      │
                       │  Composed by TheAppManager modules   │
                       └──────────────────────────────────────┘
```
