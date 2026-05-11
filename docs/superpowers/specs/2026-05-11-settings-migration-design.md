# Settings migration: appsettings.json → DB-backed Settings page

**Date:** 2026-05-11
**Status:** Approved (design)

## Goal

Move the user-tunable knobs currently living in `appsettings.json` into a
SQLite-backed `Settings` table, editable from the existing `/settings` page.
Changes take effect live — no app restart. Deployment/infrastructure config
(API key, host endpoints, DB path, logging) stays in `appsettings.json`.

## Scope

**Migrated to the DB-backed Settings page:**

| appsettings key | Settings key (string constant) | Type |
|---|---|---|
| `Anthropic:Model` | `anthropic.model` | string |
| `Anthropic:MaxTokens` | `anthropic.maxTokens` | int |
| `Polymarket:SearchQueries` | `polymarket.searchQueries` | `string[]` (JSON in value) |
| `Polymarket:MaxMarkets` | `polymarket.maxMarkets` | int |
| `Polymarket:MinVolumeUsd` | `polymarket.minVolumeUsd` | decimal |
| `Polymarket:MaxHorizonDays` | `polymarket.maxHorizonDays` | int |
| `Tickers:Focus` | `tickers.focus` | string |

**Staying in `appsettings.json` / env:**

- `Anthropic:ApiKey` — secret, sourced from env / user-secrets / `appsettings.Development.json`.
- `Yahoo:BaseUrl`, `Polymarket:BaseUrl` — host endpoints, rarely change.
- `Database:Path`, `Logging:*`.

**Note — `anthropic.maxTokens` is new wiring, not a move.** `Anthropic:MaxTokens`
is read nowhere today; `SuggestionService` hard-codes `MaxOutputTokens = 1500`.
This work both *sources* it from the table and *connects* it to the chat call
for the first time.

## Architecture

### Persistence — key/value `Settings` table

```
Settings
  Key        TEXT PRIMARY KEY      -- e.g. "anthropic.model"
  Value      TEXT NOT NULL         -- raw string; parsed by the setting descriptor
  UpdatedAt  TEXT NOT NULL         -- DateTime (UTC), stored per SQLite text convention
```

- Entity: `TradyStrat.Common.Domain.SettingEntry { string Key; string Value; DateTime UpdatedAt; }`
  (immutable record, `Key` as the EF key — mirrors how `GoalConfig` is shaped).
- EF configuration: `TradyStrat/Data/Configurations/SettingEntryConfiguration.cs`,
  registered automatically via `ApplyConfigurationsFromAssembly` (existing pattern).
- `AppDbContext` gains `public DbSet<SettingEntry> Settings => Set<SettingEntry>();`.
- A migration creates the table only — **no `HasData` seeding** (see "Seeding" below).
- Data access goes through the existing `IReadRepositoryBase<SettingEntry>` /
  `IRepositoryBase<SettingEntry>` (Ardalis.Specification). No bespoke DAL.

### Setting descriptors — Strategy + Registry

The per-setting facts (key name, default value, how to parse the stored string,
how to validate it) are defined **once**, as a `SettingDescriptor` object per
setting, held in a `SettingsRegistry`. This is the single source of truth for the
seed defaults, the `Get<T>` parsing, and the `UpdateSettingUseCase` validation —
it replaces what would otherwise be a `switch (key)` plus duplicated constants
across the table, the service, and the use case.

```csharp
namespace TradyStrat.Features.Settings.Config;

public sealed class SettingDescriptor
{
    public required string Key { get; init; }
    public required string DefaultRaw { get; init; }          // exact string stored on first seed
    public required Func<string, object> Parse { get; init; }  // raw -> typed value (InvariantCulture)
    public Action<object>? Validate { get; init; }            // throws SettingValidationException on bad value
    public Func<object, string>? Format { get; init; }        // typed -> raw; defaults to InvariantCulture ToString
}

public static class SettingsKeys
{
    public const string AnthropicModel         = "anthropic.model";
    public const string AnthropicMaxTokens     = "anthropic.maxTokens";
    public const string PolymarketSearchQueries = "polymarket.searchQueries";
    public const string PolymarketMaxMarkets   = "polymarket.maxMarkets";
    public const string PolymarketMinVolumeUsd = "polymarket.minVolumeUsd";
    public const string PolymarketMaxHorizonDays = "polymarket.maxHorizonDays";
    public const string TickersFocus           = "tickers.focus";
}

public interface ISettingsRegistry
{
    IReadOnlyDictionary<string, SettingDescriptor> All { get; }
    SettingDescriptor Get(string key);   // throws InvalidOperationException for an unknown key
}
```

Registered as a **singleton** (`SettingsRegistry`, immutable). The `tickers.focus`
descriptor's `Validate` needs to check the value against the `Instruments` table,
so it can't be a pure static lambda — that one validation lives in
`UpdateSettingUseCase` instead (it has DB access). The descriptor's `Validate` for
`tickers.focus` only does the non-empty check; the "is a known instrument" check is
applied by the use case after parsing. (Stated explicitly so the descriptor stays
DB-free.)

**Descriptor table (the validation rules — this table *is* the registry):**

| Key | DefaultRaw | Parse | Validate |
|---|---|---|---|
| `anthropic.model` | `claude-opus-4-7` | identity (string) | non-empty |
| `anthropic.maxTokens` | `1500` | `int.Parse` (invariant) | 1 ≤ n ≤ 100000 |
| `polymarket.searchQueries` | `["bitcoin","ethereum","coinbase","fed"]` | `JsonSerializer.Deserialize<string[]>` | ≥ 1 entry, every entry non-empty/non-whitespace |
| `polymarket.maxMarkets` | `8` | `int.Parse` (invariant) | n ≥ 1 |
| `polymarket.minVolumeUsd` | `50000` | `decimal.Parse` (invariant) | n ≥ 0 |
| `polymarket.maxHorizonDays` | `365` | `int.Parse` (invariant) | n ≥ 1 |
| `tickers.focus` | `CON3.L` | identity (string) | non-empty (descriptor); known `Instrument.Ticker` (use case) |

All numeric parse/format uses `CultureInfo.InvariantCulture` in both directions —
the `Value` column is culture-neutral text. The string-typed descriptors
(`anthropic.model`, `tickers.focus`) use identity for both `Parse` and `Format`;
the numeric ones supply an explicit invariant `Format`; `polymarket.searchQueries`
supplies `Format = JsonSerializer.Serialize` so a save re-normalises the array to
canonical JSON.

### Service layer

Two abstractions only.

```csharp
namespace TradyStrat.Features.Settings.Config;

public interface ISettingsService                       // raw KV read/write
{
    string  GetRaw(string key);                          // throws InvalidOperationException if missing (= bug)
    T       Get<T>(string key);                           // descriptor.Parse, cast to T
    Task    SetAsync(string key, string rawValue, CancellationToken ct);   // upsert + stamp UpdatedAt
    Task<DateTime?> LastUpdatedAsync(IEnumerable<string> keys, CancellationToken ct);  // MAX over the keys
}

public interface ISettingsReader                        // typed Facade over ISettingsService
{
    AnthropicSettings  Anthropic();
    PolymarketSettings Polymarket();
    string             FocusTicker();
}

public sealed record AnthropicSettings(string Model, int MaxTokens);
public sealed record PolymarketSettings(
    IReadOnlyList<string> SearchQueries, int MaxMarkets, decimal MinVolumeUsd, int MaxHorizonDays);
```

- `SettingsService` and `SettingsReader` are **scoped** (they ride `AppDbContext`
  via the repository). Reads hit the DB per call — no cache. SQLite-local cost is
  negligible; if a hot path shows up later, add an `IMemoryCache` invalidated on
  `SetAsync`. (Not now — YAGNI.)
- **Captive-dependency rule:** singletons (`SuggestionBackfillCoordinator`,
  `PriceFeedHostedService`, the `SettingsRegistry` itself) must never inject
  `ISettingsService`/`ISettingsReader` directly — they reach settings through a
  scope created from `IServiceScopeFactory`. The existing coordinators already do
  this; this rule just keeps it that way.
- `SettingsReader` builds its records by reading the relevant keys through
  `ISettingsService` each time it's called. No memoization within a request — keep
  it simple; callers that need the value more than once hold the returned record.

### Use case — one `UpdateSettingUseCase`, registry-driven

```csharp
public sealed record UpdateSettingInput(string Key, string RawValue);

public sealed class UpdateSettingUseCase(
    ISettingsRegistry registry,
    IReadRepositoryBase<Instrument> instruments,
    ISettingsService settings,
    ILogger<UpdateSettingUseCase> log)
    : UseCaseBase<UpdateSettingInput, DateTime>(log)
{
    protected override async Task<DateTime> ExecuteCore(UpdateSettingInput input, CancellationToken ct)
    {
        var descriptor = registry.Get(input.Key);          // InvalidOperationException if unknown
        object parsed;
        try { parsed = descriptor.Parse(input.RawValue); }
        catch (Exception ex) { throw new SettingValidationException($"'{input.Key}' value is not valid.", ex); }

        descriptor.Validate?.Invoke(parsed);                // throws SettingValidationException

        if (input.Key == SettingsKeys.TickersFocus)
        {
            var ticker = (string)parsed;
            var known = await instruments.AnyAsync(new InstrumentByTickerSpec(ticker), ct);
            if (!known) throw new SettingValidationException($"No instrument with ticker '{ticker}'.");
        }

        var raw = descriptor.Format?.Invoke(parsed) ?? input.RawValue;   // normalise (e.g. re-serialise the array)
        await settings.SetAsync(input.Key, raw, ct);
        return (await settings.LastUpdatedAsync([input.Key], ct))!.Value;
    }
}
```

No `switch (key)`. Adding a setting = add a `SettingDescriptor` to the registry
(plus a UI field); the use case, the seeder, and `Get<T>` need no changes.

New exception: `SettingValidationException : TradyStratException` (sealed),
alongside the existing `TradeValidationException` etc. — so `UseCaseBase` re-throws
it cleanly and the forms show its `Message`.

### Seeding — startup upsert, not `HasData`

A `SettingsSeederHostedService` (an `IHostedService`, like the existing
`PriceFeedHostedService`) does the seeding in `StartAsync`. Hosted services start
*after* the middleware pipeline is built — i.e. after `DatabaseModule.ConfigureMiddleware`
has run `Database.Migrate()` — so the `Settings` table is guaranteed to exist, and
this holds regardless of module discovery order. It's a singleton; it creates a
scope via `IServiceScopeFactory` (no captive dependency) and, for every descriptor
in the registry, inserts a row with `DefaultRaw` + current UTC time **if and only
if** that key is absent from the `Settings` table. Existing rows are never touched.
Net effect:

- Fresh DB → all 7 rows present with the documented defaults.
- Existing DB (post-migration) → same; nothing the user previously customised
  is overwritten (there's nothing to overwrite yet on first run, but the upsert is
  idempotent and future-proof).
- Adding a setting in a later release → just add a descriptor; the next startup
  back-fills its row. No new EF migration needed for seed data.

This also makes `ISettingsService.GetRaw` throwing on a missing key *safe* — by the
time any request runs, every registry key has a row.

### Wiring the existing consumers

**Anthropic (`AiSuggestionModule` + `SuggestionService`).** No factory. Keep
`IChatClient` a **singleton**, but drop `ConfigureOptions(o => o.ModelId = model)`
from the builder — the client is registered with no fixed model:

```csharp
builder.Services.AddSingleton<IChatClient>(_ =>
    new Anthropic.SDK.AnthropicClient(apiKey)
        .Messages
        .AsBuilder()
        .UseFunctionInvocation()
        .Build());
```

`SuggestionService` injects `ISettingsReader`. In `AskAsync`, before building
`ChatOptions`, read `var ai = settingsReader.Anthropic();` and set
`ModelId = ai.Model` and `MaxOutputTokens = ai.MaxTokens` on the `ChatOptions` it
already constructs (`SuggestionService.cs:79-83`). The hard-coded
`MaxOutputTokens = 1500` is removed. Because `SuggestionService` is scoped and a
fresh `ChatOptions` is built per call, the next suggestion picks up a saved change
with no restart.

**Polymarket (`PredictionMarketsModule` + `PolymarketGammaProvider`).** Delete
`PolymarketOptions` *and* `PolymarketOptionsBinder` entirely — the provider never
read `BaseUrl` from the options (it uses relative URLs), and the only `BaseUrl`
consumer is the `AddHttpClient` registration, which reads it straight from
`IConfiguration` (kept). `PolymarketGammaProvider`'s constructor changes from
`PolymarketOptions options` to `ISettingsReader settingsReader`; inside
`GetMarketsAsync`/`FetchQueryAsync` it calls `var p = settingsReader.Polymarket();`
once and uses `p.SearchQueries`, `p.MaxMarkets`, `p.MinVolumeUsd`,
`p.MaxHorizonDays`. The typed `HttpClient` registration stays:

```csharp
builder.Services
    .AddHttpClient<IPredictionMarketProvider, PolymarketGammaProvider>(c =>
    {
        c.BaseAddress = new Uri(builder.Configuration["Polymarket:BaseUrl"]
            ?? "https://gamma-api.polymarket.com");
        c.Timeout = TimeSpan.FromSeconds(10);
        c.DefaultRequestHeaders.UserAgent.ParseAdd("TradyStrat/1.0");
    })
    .AddStandardResilienceHandler();
```

(Typed `HttpClient` registrations are transient, so depending on the scoped
`ISettingsReader` is fine — but confirm no singleton resolves
`IPredictionMarketProvider` directly; today only use cases do, inside a scope.)

The validation moved out of `PolymarketOptionsBinder` now lives in the
`SettingDescriptor`s and is enforced on write by `UpdateSettingUseCase`, so a bad
value can't reach the provider.

**`Tickers:Focus` callsites** — `SettingsPage.razor.cs:19-20`,
`TradesPage.razor.cs:25`, `DashboardPage.razor.cs:50`. Replace
`Configuration["Tickers:Focus"]` with `ISettingsReader.FocusTicker()`. Drop the
`[Inject] IConfiguration` where it's no longer used. (`DatabaseModule` and the
HttpClient registrations still use `IConfiguration` — that stays.)

**`appsettings.json`** — remove `Anthropic:Model`, `Anthropic:MaxTokens`, the four
`Polymarket:*` knob keys (keep `Polymarket:BaseUrl`), and the entire `Tickers`
section. Resulting file: `Anthropic` (`ApiKey` only), `Yahoo` (`BaseUrl`),
`Polymarket` (`BaseUrl`), `Database`, `Logging`. `appsettings.Development.json` is
unaffected (it only has `Logging`).

### UI — the Settings page

`SettingsPage.razor` keeps its current Goal form and `AddInstrumentForm`, and gains
three sections between them, each its own component pair under
`Features/Settings/Components/` (following `AddInstrumentForm`'s `.razor` +
`.razor.cs` + `.razor.css` shape):

1. **Goal** *(unchanged)*
2. **`AnthropicSettingsForm`** — `Model` (text input), `Max tokens` (number input).
3. **`PolymarketSettingsForm`** — `Search queries` (comma-separated text input,
   parsed to `string[]` and stored as JSON), `Max markets` / `Min volume USD` /
   `Max horizon days` (number inputs).
4. **`FocusTickerForm`** — `<select>` bound to the `Instruments` table (via the
   existing `ListInstrumentsUseCase` or a spec query); the current value is
   pre-selected. Because the select only offers known instruments, the use case's
   "known ticker" check is belt-and-braces, not the primary guard.
5. **Add instrument** *(unchanged — `AddInstrumentForm`)*

Each new form:

- Loads its current values via `ISettingsReader` in `OnInitializedAsync`, and its
  "last updated" via `ISettingsService.LastUpdatedAsync(itsKeys)` — for a
  multi-key section that's `MAX(UpdatedAt)` over the section's keys.
- Has its own **Save** button (independent submit per section, like the Goal form),
  with the existing `_busy` / `_msg` / `_isError` pattern and the
  `.msg.ok` / `.msg.err` styling.
- On save, calls `UpdateSettingUseCase.ExecuteAsync` **once per changed key** in
  the section (so the Polymarket form may issue up to four calls). The button text
  reads "Saving…" while any are in flight; on completion it refreshes its
  "last updated" from the returned timestamps.
- Mirrors the descriptor validation rules client-side for immediate feedback, but
  the server (`UpdateSettingUseCase`) is the authority — a `SettingValidationException`
  surfaces in the red `.msg.err` line.

**Known limitation (accepted):** `SettingsPage` also renders
`<VaultMasthead Today=@(Clock.TodayInExchangeTzFor(FocusTicker())) … />`. Saving a
new focus ticker won't refresh that masthead until the page is re-navigated. After
a successful focus save the `FocusTickerForm` may call its parent's
`StateHasChanged` (cascaded callback) to re-render the page; if that's more
plumbing than it's worth, leaving the masthead stale until navigation is
acceptable for a single-user app. No settings-changed event bus — YAGNI.

### Module registration (`SettingsModule`)

```csharp
public void ConfigureServices(WebApplicationBuilder builder)
{
    builder.Services.AddSingleton<ISettingsRegistry, SettingsRegistry>();
    builder.Services.AddScoped<ISettingsService, SettingsService>();
    builder.Services.AddScoped<ISettingsReader, SettingsReader>();
    builder.Services.AddScoped<UpdateSettingUseCase>();
    builder.Services.AddHostedService<SettingsSeederHostedService>();
    // existing:
    builder.Services.AddScoped<UpdateGoalUseCase>();
    builder.Services.AddScoped<ProbeInstrumentUseCase>();
    builder.Services.AddScoped<AddInstrumentUseCase>();
    builder.Services.AddScoped<ListInstrumentsUseCase>();
}
```

`SettingsSeederHostedService(IServiceScopeFactory scopes, ISettingsRegistry registry)`
— in `StartAsync`, opens a scope, resolves `ISettingsService` (or queries the
`SettingEntry` repository directly), and upserts each missing descriptor key with
its `DefaultRaw` + `UtcNow`. `StopAsync` is a no-op.

`AiSuggestionModule` and `PredictionMarketsModule` depend on `ISettingsReader`
being registered — `SettingsModule` must register it. Module discovery order in
`TheAppManager` is by type; if ordering can't be relied on, the safe move is that
all three modules only *resolve* `ISettingsReader` at request time (they do —
`SuggestionService`, `PolymarketGammaProvider`), not at `ConfigureServices` time,
so registration order doesn't matter.

## Data flow

1. User edits a field in `PolymarketSettingsForm`, clicks Save.
2. Form calls `UpdateSettingUseCase.ExecuteAsync(new("polymarket.maxMarkets","12"))`.
3. Use case: `registry.Get(key)` → descriptor; `descriptor.Parse("12")` → `12`;
   `descriptor.Validate(12)` → ok; `descriptor.Format(12)` → `"12"`;
   `settings.SetAsync("polymarket.maxMarkets","12")` upserts the row, stamps `UpdatedAt`.
4. Next time `LoadDashboardUseCase` (or whatever) triggers a prediction-markets
   fetch, `PolymarketGammaProvider.GetMarketsAsync` calls
   `settingsReader.Polymarket()`, which reads the four keys fresh from the DB →
   `MaxMarkets == 12`. No restart.

Same shape for `anthropic.*` (read by `SuggestionService` per `AskAsync`) and
`tickers.focus` (read by the pages on render).

## Error handling

- **User-facing validation** — `SettingValidationException : TradyStratException`,
  thrown by `UpdateSettingUseCase` (bad parse, failed `Validate`, unknown focus
  ticker). `UseCaseBase` re-throws `TradyStratException` untouched; the form shows
  `ex.Message` in `.msg.err`. Unexpected exceptions show a generic
  "Save failed — see logs." like the existing Goal form.
- **Missing key on read** — `ISettingsService.GetRaw` throws
  `InvalidOperationException`. This is a programmer/seed bug (the startup upsert
  guarantees every registry key has a row), not a user condition; no silent
  fallback to `appsettings.json` — the migration is one-way.
- **Corrupt stored value** (e.g. non-JSON in `polymarket.searchQueries`,
  non-numeric in a numeric key) — the descriptor `Parse` throws; `Get<T>` lets it
  propagate. Can't happen via the UI (the descriptor `Format` re-normalises on
  write) or via seeding (defaults are valid). Treated as corruption, not handled
  gracefully.
- **Unknown key passed to `UpdateSettingUseCase`** — `registry.Get` throws
  `InvalidOperationException` (the UI never does this; it's a coding error).
- **Concurrency** — single-user local app; `SetAsync` is last-write-wins, no
  optimistic concurrency token. Two browser tabs racing a write is theoretically
  possible and explicitly out of scope.

## Testing

Project `TradyStrat.Tests`, following its existing structure (`Settings/`,
`Modules/`, `Common/`).

- **`SettingsRegistryTests`** — registry contains exactly the 7 expected keys;
  every descriptor's `DefaultRaw` round-trips through its own `Parse` (and `Format`
  where present) without throwing; `Get` on an unknown key throws
  `InvalidOperationException`.
- **`SettingsServiceTests`** — `SetAsync` then `GetRaw`/`Get<T>` round-trip for
  each type (string, int, decimal, `string[]`); decimal/int parse is
  culture-invariant (run a case under a `,`-decimal culture); `GetRaw` throws
  `InvalidOperationException` for a key with no row; `LastUpdatedAsync` returns the
  `MAX` over multiple keys.
- **`SettingsSeedingTests`** — after migrations + `SettingsSeederHostedService.StartAsync`,
  all 7 rows exist with the documented defaults; running the seeder again is
  idempotent; a pre-existing customised row is not overwritten.
- **`UpdateSettingUseCaseTests`** — one happy path per key; each `Validate`
  boundary fails with `SettingValidationException` (`maxTokens` 0 and 100001,
  `maxMarkets` 0, `minVolumeUsd` -1, `maxHorizonDays` 0, empty `searchQueries`
  array, `searchQueries` with a whitespace entry, empty `model`, empty focus);
  `tickers.focus` set to a ticker absent from `Instruments` fails; `tickers.focus`
  set to a seeded ticker (`CON3.L`) succeeds; an unknown key throws
  `InvalidOperationException`.
- **`SettingsReaderTests`** — `Anthropic()`, `Polymarket()`, `FocusTicker()`
  reflect the current DB state: write a change via `ISettingsService`, re-read via
  `ISettingsReader`, assert the new value (proves no caching).
- **Consumer smoke tests** —
  - `SuggestionService` builds `ChatOptions` with `ModelId`/`MaxOutputTokens` from
    `ISettingsReader` (mock the `IChatClient`, capture the `ChatOptions`); after a
    write, the next call uses the new values.
  - `PolymarketGammaProvider` uses the current `MaxMarkets`/`MinVolumeUsd`/etc.
    from `ISettingsReader` (mock `HttpMessageHandler`, assert the over-fetch
    `limit` and the post-filter cutoffs reflect a freshly-written value).
  - `TradesPage` / `DashboardPage` resolve their focus ticker from
    `ISettingsReader.FocusTicker()` (existing page tests, swap the config stub for
    the reader).
- **Existing tests** — anything currently stubbing `Tickers:Focus` /
  `Anthropic:Model` / `Polymarket:*` via `IConfiguration` in `WebApplicationFactory`
  setups must be updated to seed the `Settings` table (or rely on the startup
  upsert defaults) instead.

## Out of scope

- `Tickers:Focus` becoming a true per-session "active selection" with a global
  picker (it stays a single configured value here).
- Hot-reload via a custom `IConfigurationSource` / `IOptionsMonitor` (we read from
  the DB on demand instead).
- Caching of settings reads.
- Optimistic concurrency on writes.
- A settings-changed event bus / live masthead refresh on the Settings page.
- Migrating `Yahoo:BaseUrl` / `Polymarket:BaseUrl` / `Anthropic:ApiKey`.
