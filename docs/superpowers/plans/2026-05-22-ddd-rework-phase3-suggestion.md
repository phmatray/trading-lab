# DDD Rework — Phase 3 (Suggestion aggregate) Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Rewrite the anemic `Suggestion` record as a rich aggregate root with VO fields (`Quantity`/`Price`/`Conviction`), a typed `IReadOnlyList<Citation>` mapped as an owned-many `Citations` table (dual-written with the legacy `CitationsJson` column for the MCP read path), a `MarketSnapshot` owned VO, and a `PromptFingerprint` VO bundling the three hash columns. Replace `IRepositoryBase<Suggestion>` with `ISuggestionRepository` for AR access. Split the existing static `SuggestionGate` into a domain decision (`Domain/Suggestions/Services/SuggestionGate`) and an Application-side plumbing layer (`SuggestionGatePlumbing`).

**Architecture:** Suggestion becomes a `sealed class` with private setters, EF-friendly parameterless ctor, and a `Suggestion.From(...)` factory that enforces invariants (conviction 1..10, rationale non-empty, action enum valid, prompt-hash non-empty). All six use cases swap `IReadRepositoryBase<Suggestion>` for `ISuggestionRepository`. Specifications stay but move from Application to Infrastructure as a query-construction detail. `Citation` collection persistence is dual-write: the new `Citations` owned-many table is the canonical source; an EF interceptor maintains the legacy `CitationsJson` column for backward compatibility with the MCP server's read path.

**Tech Stack:** .NET 10, EF Core 10.0.7 (Sqlite + value converters + owned types + save-changes interceptors), Ardalis.Specification 9.3.1, xunit.v3 3.2.2, Shouldly 4.3.0.

**Spec reference:** [`docs/superpowers/specs/2026-05-22-ddd-domain-rework-design.md`](../specs/2026-05-22-ddd-domain-rework-design.md) §4, §5, §8.

**Phase 2 conventions preserved:** sealed-class AR + private setters + parameterless ctor for EF, factory methods (no public ctors), no-null in domain (`Empty`/`None` + `IsEmpty`/`IsSpecified`), `using PortfolioAr = global::...` style aliases when namespace shadowing strikes, per-aggregate repository (replacing `IRepositoryBase<T>`), EF mapping via fluent `OwnsOne`/`OwnsMany` with backing fields, `PendingModelChangesWarning` suppressed in `AppDbContext.OnConfiguring` (Phase 2 workaround stays).

---

## Pre-work: Worktree setup

- [ ] **Step 0.1: Create an isolated worktree**

Per the user's memory note (prefers isolated worktrees for multi-commit work), do not work on `main`. Use the harness `EnterWorktree` tool, OR:

```bash
git worktree add ../TradyStrat-ddd-phase3 -b worktree-ddd-phase3
cd ../TradyStrat-ddd-phase3
```

All subsequent paths are relative to the worktree root.

- [ ] **Step 0.2: Verify baseline green**

```bash
dotnet tool restore
dotnet build TradyStrat.slnx
dotnet test TradyStrat.slnx --no-build
```

Expected: 397 / 397 tests pass. If failing on `main`, stop and surface before proceeding.

---

# Phase 3 — Suggestion aggregate

Goal: `Suggestion` is the AR for (`InstrumentId`, `ForDate`). Aggregate structure (final, per spec §8):

```
Suggestion (AR — one per (InstrumentId, ForDate))
├── SuggestionId Id
├── InstrumentId InstrumentId
├── DateOnly ForDate
├── SuggestionAction Action
├── Quantity QuantityHint                ← Quantity.None when AI gave no hint
├── Price MaxPriceHint                   ← Price.None(Currency) when AI gave no hint
├── Conviction Conviction                ← VO, 1..10
├── string Rationale                     ← required, non-empty
├── IReadOnlyList<Citation> Citations    ← empty list, never null; owned-many in DB
├── MarketSnapshot Snapshot              ← MarketSnapshot.Empty when none captured
├── PromptFingerprint Fingerprint        ← VO bundling PromptHash + EnvelopeHash + PromptVersionHash
├── string ThinkingText                  ← empty string when absent
└── DateTime CreatedAt
```

`Citation` lives in `Domain/Suggestions/`. `MarketSnapshot` is moved from `Application/PredictionMarkets/` to `Domain/Suggestions/` (it's the Suggestion's snapshot, not a Prediction Markets concern — the PredictionMarket type stays in Application but `MarketSnapshot` belongs with the AR that owns it).

---

## Task 1: `Conviction` VO

**Files:**
- Create: `TradyStrat.Domain/Shared/Conviction.cs`
- Test:   `TradyStrat.Domain.Tests/Shared/ConvictionTests.cs`

- [ ] **Step 1.1: Write failing tests**

`TradyStrat.Domain.Tests/Shared/ConvictionTests.cs`:

```csharp
using Shouldly;
using TradyStrat.Domain.Shared;
using Xunit;

namespace TradyStrat.Domain.Tests.Shared;

public class ConvictionTests
{
    [Fact]
    public void Of_accepts_1_through_10()
    {
        Conviction.Of(1).Value.ShouldBe(1);
        Conviction.Of(5).Value.ShouldBe(5);
        Conviction.Of(10).Value.ShouldBe(10);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(11)]
    [InlineData(100)]
    public void Of_rejects_out_of_range(int value)
    {
        Should.Throw<ArgumentException>(() => Conviction.Of(value));
    }

    [Fact]
    public void Equality_is_structural()
    {
        Conviction.Of(5).ShouldBe(Conviction.Of(5));
        Conviction.Of(5).ShouldNotBe(Conviction.Of(6));
    }
}
```

- [ ] **Step 1.2: Run failing**

`dotnet test TradyStrat.Domain.Tests --filter "FullyQualifiedName~ConvictionTests"` — expect build error.

- [ ] **Step 1.3: Implement `Conviction`**

`TradyStrat.Domain/Shared/Conviction.cs`:

```csharp
namespace TradyStrat.Domain.Shared;

public readonly record struct Conviction
{
    public int Value { get; }

    private Conviction(int value) => Value = value;

    public static Conviction Of(int value)
    {
        if (value < 1 || value > 10)
            throw new ArgumentException($"Conviction must be in [1..10]: {value}.", nameof(value));
        return new Conviction(value);
    }

    public override string ToString() => Value.ToString();
}
```

Per spec §4.2: "No `Conviction.None` — every `Suggestion.From(...)` requires a valid conviction (the AI tool-call schema makes it mandatory)."

- [ ] **Step 1.4: Run tests passing**

`dotnet test TradyStrat.Domain.Tests --filter "FullyQualifiedName~ConvictionTests"` — expect 6 pass (1 + 4 theory + 1).

- [ ] **Step 1.5: Commit**

```bash
git add TradyStrat.Domain/Shared/Conviction.cs TradyStrat.Domain.Tests/Shared/ConvictionTests.cs
git commit -m "feat(domain): Conviction VO (1..10) — Phase 3"
```

---

## Task 2: `PromptFingerprint` aggregate-local VO

**Files:**
- Create: `TradyStrat.Domain/Suggestions/PromptFingerprint.cs`
- Test:   `TradyStrat.Domain.Tests/Suggestions/PromptFingerprintTests.cs`

Spec §8: "single `PromptFingerprint(string PromptHash, string EnvelopeHash, string PromptVersionHash)` owned VO mapped to the existing three columns. Absent components store empty string."

- [ ] **Step 2.1: Write failing tests**

```csharp
using Shouldly;
using TradyStrat.Domain.Suggestions;
using Xunit;

namespace TradyStrat.Domain.Tests.Suggestions;

public class PromptFingerprintTests
{
    [Fact]
    public void Of_requires_non_empty_PromptHash()
    {
        Should.Throw<ArgumentException>(() =>
            PromptFingerprint.Of("", "env", "v1"));
        Should.Throw<ArgumentException>(() =>
            PromptFingerprint.Of(null!, "env", "v1"));
    }

    [Fact]
    public void Of_allows_empty_optional_components()
    {
        var fp = PromptFingerprint.Of("abc", "", "");
        fp.PromptHash.ShouldBe("abc");
        fp.EnvelopeHash.ShouldBe("");
        fp.PromptVersionHash.ShouldBe("");
    }

    [Fact]
    public void Of_normalizes_null_optional_components_to_empty_string()
    {
        var fp = PromptFingerprint.Of("abc", null!, null!);
        fp.EnvelopeHash.ShouldBe("");
        fp.PromptVersionHash.ShouldBe("");
    }
}
```

- [ ] **Step 2.2: Run failing**

`dotnet test TradyStrat.Domain.Tests --filter "FullyQualifiedName~PromptFingerprintTests"` — build error.

- [ ] **Step 2.3: Implement**

`TradyStrat.Domain/Suggestions/PromptFingerprint.cs`:

```csharp
namespace TradyStrat.Domain.Suggestions;

public sealed record PromptFingerprint
{
    public string PromptHash        { get; private set; } = "";
    public string EnvelopeHash      { get; private set; } = "";
    public string PromptVersionHash { get; private set; } = "";

    private PromptFingerprint() { }   // EF

    private PromptFingerprint(string promptHash, string envelopeHash, string promptVersionHash)
    {
        PromptHash        = promptHash;
        EnvelopeHash      = envelopeHash;
        PromptVersionHash = promptVersionHash;
    }

    public static PromptFingerprint Of(string promptHash, string? envelopeHash, string? promptVersionHash)
    {
        if (string.IsNullOrWhiteSpace(promptHash))
            throw new ArgumentException("PromptHash is required.", nameof(promptHash));
        return new PromptFingerprint(promptHash, envelopeHash ?? "", promptVersionHash ?? "");
    }
}
```

- [ ] **Step 2.4: Run tests passing**

- [ ] **Step 2.5: Commit**

```bash
git add TradyStrat.Domain/Suggestions/PromptFingerprint.cs TradyStrat.Domain.Tests/Suggestions/PromptFingerprintTests.cs
git commit -m "feat(domain): PromptFingerprint aggregate-local VO — Phase 3"
```

---

## Task 3: Move `MarketSnapshot` from Application to Domain

**Files:**
- Move: `TradyStrat.Application/PredictionMarkets/MarketSnapshot.cs` → `TradyStrat.Domain/Suggestions/MarketSnapshot.cs`
- Inspect: `TradyStrat.Application/PredictionMarkets/PredictionMarket.cs`, `MarketCitation.cs` (their location decisions).

`MarketSnapshot` is part of the `Suggestion` aggregate — it captures the prediction-market context at suggestion time. Today it sits under `Application/PredictionMarkets/` because the Phase 1 layout didn't have a place for it. Move to `Domain/Suggestions/`.

`PredictionMarket` and `MarketCitation` (the records referenced by `MarketSnapshot.Markets` / `MarketSnapshot.Cited`) are also Domain-shaped data — move them too if they're not already in Domain.

- [ ] **Step 3.1: Inspect the referenced types**

```bash
find . -name 'PredictionMarket.cs' -o -name 'MarketCitation.cs' -not -path '*/bin/*' -not -path '*/obj/*' -not -path '*/worktrees/*'
```

If either is in `TradyStrat.Application/`, move to `TradyStrat.Domain/Suggestions/` along with `MarketSnapshot`. If in `TradyStrat.Domain/` already, leave them.

- [ ] **Step 3.2: Move the file(s)**

```bash
git mv TradyStrat.Application/PredictionMarkets/MarketSnapshot.cs \
       TradyStrat.Domain/Suggestions/MarketSnapshot.cs
# Also PredictionMarket.cs and MarketCitation.cs if they were in Application.
```

- [ ] **Step 3.3: Update the namespace in the moved file(s)**

Open `TradyStrat.Domain/Suggestions/MarketSnapshot.cs` and change `namespace TradyStrat.Application.PredictionMarkets;` to `namespace TradyStrat.Domain.Suggestions;`. Same for any other moved files.

Add `IsEmpty` predicate (spec §8 "MarketSnapshot.Empty when none captured"):

```csharp
namespace TradyStrat.Domain.Suggestions;

public sealed record MarketSnapshot(
    IReadOnlyList<PredictionMarket> Markets,
    IReadOnlyList<MarketCitation> Cited)
{
    public static readonly MarketSnapshot Empty = new([], []);
    public bool IsEmpty => Markets.Count == 0 && Cited.Count == 0;
}
```

- [ ] **Step 3.4: Fix all consumers' using directives**

```bash
grep -rln 'using TradyStrat\.Application\.PredictionMarkets' --include='*.cs' --include='*.razor*' .
```

For each match, replace the import with `using TradyStrat.Domain.Suggestions;` (or add a Domain.Suggestions using alongside Application.PredictionMarkets if the file still needs `PredictionMarket`).

- [ ] **Step 3.5: Build to verify**

```bash
dotnet build TradyStrat.slnx
```

Expected: 0 errors. (Some 'using' adjustments may be needed; fix them.)

- [ ] **Step 3.6: Commit**

```bash
git add -A
git commit -m "refactor(domain): MarketSnapshot moves from Application/PredictionMarkets to Domain/Suggestions — Phase 3"
```

---

## Task 4: Move `ICorrectnessRule` + `FixedThresholdCorrectness` into a `Services` subfolder

**Files:**
- Move: `TradyStrat.Domain/Suggestions/ICorrectnessRule.cs` → `TradyStrat.Domain/Suggestions/Services/ICorrectnessRule.cs`
- Move: `TradyStrat.Domain/Suggestions/FixedThresholdCorrectness.cs` → `TradyStrat.Domain/Suggestions/Services/FixedThresholdCorrectness.cs`

Spec §3: "domain services live in `Domain/<Aggregate>/Services/`". The correctness rule and its implementations are domain services consumed by the AR's `WasCorrect(...)` method (added in Task 7).

- [ ] **Step 4.1: Move the files**

```bash
mkdir -p TradyStrat.Domain/Suggestions/Services
git mv TradyStrat.Domain/Suggestions/ICorrectnessRule.cs \
       TradyStrat.Domain/Suggestions/Services/ICorrectnessRule.cs
git mv TradyStrat.Domain/Suggestions/FixedThresholdCorrectness.cs \
       TradyStrat.Domain/Suggestions/Services/FixedThresholdCorrectness.cs
```

- [ ] **Step 4.2: Update namespaces**

Both files: change `namespace TradyStrat.Domain;` to `namespace TradyStrat.Domain.Suggestions.Services;`. Read the existing files first to confirm the current namespace.

- [ ] **Step 4.3: Update consumers**

```bash
grep -rln 'ICorrectnessRule\|FixedThresholdCorrectness' --include='*.cs' . | grep -v Domain/Suggestions/Services
```

For each match, add `using TradyStrat.Domain.Suggestions.Services;` if the file's `using` doesn't already cover it (and remove `using TradyStrat.Domain;` if that was their only need from there).

- [ ] **Step 4.4: Build + commit**

```bash
dotnet build TradyStrat.slnx
git add -A
git commit -m "refactor(domain): ICorrectnessRule + FixedThresholdCorrectness move to Suggestions/Services — Phase 3"
```

---

## Task 5: `Correctness` and `GateDecision` return records

**Files:**
- Create: `TradyStrat.Domain/Suggestions/Correctness.cs`
- Create: `TradyStrat.Domain/Suggestions/GateDecision.cs`

Plain records used as return shapes for `Suggestion.WasCorrect` (§8) and `SuggestionGate.Decide` (Task 8).

- [ ] **Step 5.1: Write `Correctness`**

`TradyStrat.Domain/Suggestions/Correctness.cs`:

```csharp
using TradyStrat.Domain.Shared;

namespace TradyStrat.Domain.Suggestions;

public sealed record Correctness(bool IsCorrect, Money ForwardReturn);
```

- [ ] **Step 5.2: Write `GateDecision`**

`TradyStrat.Domain/Suggestions/GateDecision.cs`:

```csharp
namespace TradyStrat.Domain.Suggestions;

public abstract record GateDecision
{
    public sealed record Fetch : GateDecision;
    public sealed record Reuse(Suggestion Existing) : GateDecision;

    private GateDecision() { }
}
```

This is a discriminated-union-shaped record per the C# 12 idiom. `Reuse` carries the existing `Suggestion` so the caller doesn't need a separate lookup.

- [ ] **Step 5.3: Build + commit**

```bash
dotnet build TradyStrat.Domain
git add TradyStrat.Domain/Suggestions/Correctness.cs TradyStrat.Domain/Suggestions/GateDecision.cs
git commit -m "feat(domain): Correctness + GateDecision return records — Phase 3"
```

---

## Task 6: `Citation` adopts no-null + immutability conventions

**Files:**
- Modify: `TradyStrat.Domain/Suggestions/Citation.cs`

Current shape is `record Citation(string Claim, string Indicator, string Ticker, string Value)`. Spec §4.7 says Citation stays a record but adopts the new VO conventions. The fields are already non-nullable strings — minimal changes needed; just confirm it builds against the new ownership pattern (will be needed for EF owned-many in Task 11).

Citation needs a parameterless ctor + private-set properties to work as EF owned-many:

- [ ] **Step 6.1: Rewrite `Citation`**

```csharp
namespace TradyStrat.Domain.Suggestions;

public sealed record Citation
{
    public string Claim     { get; private set; } = "";
    public string Indicator { get; private set; } = "";
    public string Ticker    { get; private set; } = "";
    public string Value     { get; private set; } = "";

    private Citation() { }   // EF

    public Citation(string claim, string indicator, string ticker, string value)
    {
        Claim     = claim     ?? "";
        Indicator = indicator ?? "";
        Ticker    = ticker    ?? "";
        Value     = value     ?? "";
    }
}
```

Keeping the public ctor allows existing JSON deserialization to work.

- [ ] **Step 6.2: Build + commit**

```bash
dotnet build TradyStrat.slnx
git add TradyStrat.Domain/Suggestions/Citation.cs
git commit -m "refactor(domain): Citation adopts parameterless ctor + private setters for EF owned-many — Phase 3"
```

---

## Task 7: Rewrite `Suggestion` AR

**Files:**
- Modify: `TradyStrat.Domain/Suggestions/Suggestion.cs` (replace anemic record with rich AR)
- Test:   `TradyStrat.Domain.Tests/Suggestions/SuggestionFactoryTests.cs`

This is the heart of Phase 3. Replace the existing record with the class shape from spec §8. The legacy `CitationsJson` / `MarketSnapshotJson` / individual hash properties are gone from the domain surface; their storage is handled at the EF layer (Task 11).

- [ ] **Step 7.1: Read the existing Suggestion.cs**

```bash
cat TradyStrat.Domain/Suggestions/Suggestion.cs
```

Inventory the public fields/getters that consumers use (looking for: `Id`, `InstrumentId`, `ForDate`, `Action`, `QuantityHint`, `MaxPriceHint`, `Conviction`, `Rationale`, `CitationsJson`, `MarketSnapshotJson`, `PromptHash`, `ThinkingText`, `EnvelopeHash`, `PromptVersionHash`, `CreatedAt`, `OrderValueEur`, `Citations`).

- [ ] **Step 7.2: Write failing tests**

`TradyStrat.Domain.Tests/Suggestions/SuggestionFactoryTests.cs`:

```csharp
using Shouldly;
using TradyStrat.Domain;
using TradyStrat.Domain.Exceptions;
using TradyStrat.Domain.Shared;
using TradyStrat.Domain.Suggestions;
using Xunit;

namespace TradyStrat.Domain.Tests.Suggestions;

public class SuggestionFactoryTests
{
    private static readonly DateTime _now = new(2026, 5, 22, 12, 0, 0, DateTimeKind.Utc);

    private static Suggestion ValidSuggestion() => Suggestion.From(
        instrumentId: new InstrumentId(1),
        forDate:      new DateOnly(2026, 5, 22),
        action:       SuggestionAction.Hold,
        quantityHint: Quantity.None,
        maxPriceHint: Price.None(Currency.Eur),
        conviction:   Conviction.Of(7),
        rationale:    "Sample rationale.",
        citations:    [],
        snapshot:     MarketSnapshot.Empty,
        fingerprint:  PromptFingerprint.Of("hash1", "env1", "v1"),
        thinkingText: "",
        createdAt:    _now);

    [Fact]
    public void From_assigns_zero_id_sentinel()
    {
        ValidSuggestion().Id.ShouldBe(SuggestionId.New());
    }

    [Fact]
    public void From_preserves_all_fields()
    {
        var s = ValidSuggestion();
        s.InstrumentId.ShouldBe(new InstrumentId(1));
        s.ForDate.ShouldBe(new DateOnly(2026, 5, 22));
        s.Action.ShouldBe(SuggestionAction.Hold);
        s.QuantityHint.IsSpecified.ShouldBeFalse();
        s.MaxPriceHint.IsEmpty.ShouldBeTrue();
        s.Conviction.Value.ShouldBe(7);
        s.Rationale.ShouldBe("Sample rationale.");
        s.Citations.ShouldBeEmpty();
        s.Snapshot.IsEmpty.ShouldBeTrue();
        s.Fingerprint.PromptHash.ShouldBe("hash1");
        s.ThinkingText.ShouldBe("");
        s.CreatedAt.ShouldBe(_now);
    }

    [Fact]
    public void From_rejects_empty_rationale()
    {
        Should.Throw<ArgumentException>(() => Suggestion.From(
            instrumentId: new InstrumentId(1),
            forDate:      new DateOnly(2026, 5, 22),
            action:       SuggestionAction.Hold,
            quantityHint: Quantity.None,
            maxPriceHint: Price.None(Currency.Eur),
            conviction:   Conviction.Of(5),
            rationale:    "",
            citations:    [],
            snapshot:     MarketSnapshot.Empty,
            fingerprint:  PromptFingerprint.Of("h", "", ""),
            thinkingText: "",
            createdAt:    _now));
    }

    [Fact]
    public void OrderValue_is_None_when_quantity_or_price_absent()
    {
        var s = ValidSuggestion();
        s.OrderValue.IsEmpty.ShouldBeTrue();
        s.OrderValue.Currency.ShouldBe(Currency.Eur);
    }

    [Fact]
    public void OrderValue_multiplies_quantity_and_price_when_both_specified()
    {
        var s = Suggestion.From(
            instrumentId: new InstrumentId(1),
            forDate:      new DateOnly(2026, 5, 22),
            action:       SuggestionAction.Acquire,
            quantityHint: Quantity.Of(10m),
            maxPriceHint: Price.Of(Money.Of(4m, Currency.Eur)),
            conviction:   Conviction.Of(8),
            rationale:    "Buy on dip.",
            citations:    [new Citation("rsi oversold", "rsi", "CON3.L", "28.5")],
            snapshot:     MarketSnapshot.Empty,
            fingerprint:  PromptFingerprint.Of("h", "", ""),
            thinkingText: "",
            createdAt:    _now);

        s.OrderValue.ShouldBe(Money.Of(40m, Currency.Eur));
    }

    [Fact]
    public void Citations_are_preserved_and_immutable_to_caller()
    {
        var input = new List<Citation> { new("c1", "rsi", "X", "v1"), new("c2", "sma", "X", "v2") };
        var s = Suggestion.From(
            instrumentId: new InstrumentId(1),
            forDate:      new DateOnly(2026, 5, 22),
            action:       SuggestionAction.Hold,
            quantityHint: Quantity.None,
            maxPriceHint: Price.None(Currency.Eur),
            conviction:   Conviction.Of(7),
            rationale:    "x",
            citations:    input,
            snapshot:     MarketSnapshot.Empty,
            fingerprint:  PromptFingerprint.Of("h", "", ""),
            thinkingText: "",
            createdAt:    _now);

        s.Citations.Count.ShouldBe(2);
        s.Citations[0].Claim.ShouldBe("c1");
    }
}
```

- [ ] **Step 7.3: Run failing**

`dotnet test TradyStrat.Domain.Tests --filter "FullyQualifiedName~SuggestionFactoryTests"` — build error.

- [ ] **Step 7.4: Rewrite `Suggestion.cs`**

```csharp
using TradyStrat.Domain.Shared;
using TradyStrat.Domain.Suggestions.Services;

namespace TradyStrat.Domain.Suggestions;

public sealed class Suggestion
{
    public SuggestionId      Id            { get; private set; }
    public InstrumentId      InstrumentId  { get; private set; }
    public DateOnly          ForDate       { get; private set; }
    public SuggestionAction  Action        { get; private set; }
    public Quantity          QuantityHint  { get; private set; } = Quantity.None;
    public Price             MaxPriceHint  { get; private set; } = Price.None(Currency.Eur);
    public Conviction        Conviction    { get; private set; } = Conviction.Of(1);
    public string            Rationale     { get; private set; } = "";
    public MarketSnapshot    Snapshot      { get; private set; } = MarketSnapshot.Empty;
    public PromptFingerprint Fingerprint   { get; private set; } = PromptFingerprint.Of(" ", "", "");
    public string            ThinkingText  { get; private set; } = "";
    public DateTime          CreatedAt     { get; private set; }

    private readonly List<Citation> _citations = new();
    public IReadOnlyList<Citation> Citations => _citations;

    private Suggestion() { }   // EF

    private Suggestion(
        InstrumentId instrumentId, DateOnly forDate, SuggestionAction action,
        Quantity quantityHint, Price maxPriceHint, Conviction conviction,
        string rationale, IReadOnlyList<Citation> citations,
        MarketSnapshot snapshot, PromptFingerprint fingerprint, string thinkingText,
        DateTime createdAt)
    {
        Id           = SuggestionId.New();
        InstrumentId = instrumentId;
        ForDate      = forDate;
        Action       = action;
        QuantityHint = quantityHint;
        MaxPriceHint = maxPriceHint;
        Conviction   = conviction;
        Rationale    = rationale;
        Snapshot     = snapshot;
        Fingerprint  = fingerprint;
        ThinkingText = thinkingText;
        CreatedAt    = createdAt;
        _citations.AddRange(citations);
    }

    public static Suggestion From(
        InstrumentId instrumentId, DateOnly forDate, SuggestionAction action,
        Quantity quantityHint, Price maxPriceHint, Conviction conviction,
        string rationale, IReadOnlyList<Citation> citations,
        MarketSnapshot snapshot, PromptFingerprint fingerprint, string thinkingText,
        DateTime createdAt)
    {
        if (string.IsNullOrWhiteSpace(rationale))
            throw new ArgumentException("Rationale is required.", nameof(rationale));

        return new Suggestion(
            instrumentId, forDate, action, quantityHint, maxPriceHint, conviction,
            rationale, citations ?? [], snapshot, fingerprint, thinkingText ?? "",
            createdAt);
    }

    /// <summary>
    /// `Money.None(MaxPriceHint.Currency)` when either hint is absent; otherwise
    /// quantity × price = total committed value at the hinted price ceiling.
    /// </summary>
    public Money OrderValue =>
        QuantityHint.IsSpecified && !MaxPriceHint.IsEmpty
            ? MaxPriceHint * QuantityHint
            : Money.None(MaxPriceHint.Currency);

    /// <summary>
    /// Returns whether this suggestion's directional call was correct given
    /// forward-window price bars, per the supplied rule. Strategy-via-parameter
    /// (spec §5 double-dispatch): the rule varies; the AR doesn't know
    /// `FixedThresholdCorrectness` etc. by name.
    /// </summary>
    public Correctness WasCorrect(IReadOnlyList<PriceBar> forwardBars, ICorrectnessRule rule)
    {
        if (forwardBars.Count < 2)
            return new Correctness(false, Money.Zero(Currency.Eur));

        var start = forwardBars[0].Close;
        var end   = forwardBars[^1].Close;
        var pctReturn = start == 0m ? 0m : (end - start) / start * 100m;
        var isCorrect = rule.Evaluate(Action, pctReturn);
        // ForwardReturn expressed in EUR per share; callers can multiply by qty if needed.
        var deltaPerShare = end - start;
        return new Correctness(isCorrect, Money.Of(deltaPerShare, Currency.Eur));
    }

    internal void AssignId(SuggestionId id) => Id = id;
}
```

The legacy `CitationsJson` / `MarketSnapshotJson` / `PromptHash` / `EnvelopeHash` / `PromptVersionHash` properties are intentionally absent from the domain surface — they're persistence concerns handled by `SuggestionConfiguration` (Task 11). The `using TradyStrat.Domain.Suggestions.Services;` directive brings in `ICorrectnessRule`.

The reference to `PriceBar` requires `using TradyStrat.Domain;` (PriceBar lives at namespace `TradyStrat.Domain` per inspect). The `WasCorrect` method's signature matches spec §8.

- [ ] **Step 7.5: Run tests passing**

`dotnet test TradyStrat.Domain.Tests --filter "FullyQualifiedName~SuggestionFactoryTests"` — expect 6/6.

**At this point the rest of the solution will fail to build** because many files still reference the old anemic `Suggestion`'s properties (`CitationsJson`, `PromptHash`, etc.). That's expected; Tasks 11-21 progressively re-green them. Only `TradyStrat.Domain` + `TradyStrat.Domain.Tests` must build cleanly at this checkpoint.

- [ ] **Step 7.6: Verify Domain builds**

```bash
dotnet build TradyStrat.Domain
dotnet build TradyStrat.Domain.Tests
```

Both must succeed.

- [ ] **Step 7.7: Commit**

```bash
git add TradyStrat.Domain/Suggestions/Suggestion.cs TradyStrat.Domain.Tests/Suggestions/SuggestionFactoryTests.cs
git commit -m "feat(domain): Suggestion becomes AR with VO fields + Citations + WasCorrect — Phase 3"
```

---

## Task 8: `SuggestionGate` domain decision service

**Files:**
- Create: `TradyStrat.Domain/Suggestions/Services/SuggestionGate.cs`
- Test:   `TradyStrat.Domain.Tests/Suggestions/SuggestionGateTests.cs`

Spec §8: "Decision moves to `Domain/Suggestions/SuggestionGate.cs` as a pure domain service. Inputs: existing `Suggestion?` for `(instrumentId, today)`, `PromptFingerprint` of the candidate prompt, settings. Output: `GateDecision.Fetch` or `GateDecision.Reuse(Suggestion)`."

The existing static `SuggestionGate` (in `Application/AiSuggestion/UseCases/`) was purely the semaphore plumbing. We now have TWO things called gate: the domain decision (here, Task 8) and the Application plumbing (Task 14, renamed `SuggestionGatePlumbing`).

Rule (matching today's behavior): reuse if an existing suggestion for the same date exists AND its `PromptFingerprint` matches the candidate's; otherwise fetch.

- [ ] **Step 8.1: Write failing tests**

`TradyStrat.Domain.Tests/Suggestions/SuggestionGateTests.cs`:

```csharp
using Shouldly;
using TradyStrat.Domain.Shared;
using TradyStrat.Domain.Suggestions;
using TradyStrat.Domain.Suggestions.Services;
using Xunit;

namespace TradyStrat.Domain.Tests.Suggestions;

public class SuggestionGateTests
{
    private static readonly DateTime _now = new(2026, 5, 22, 12, 0, 0, DateTimeKind.Utc);

    private static Suggestion Existing(PromptFingerprint fp) => Suggestion.From(
        instrumentId: new InstrumentId(1),
        forDate:      new DateOnly(2026, 5, 22),
        action:       SuggestionAction.Hold,
        quantityHint: Quantity.None,
        maxPriceHint: Price.None(Currency.Eur),
        conviction:   Conviction.Of(5),
        rationale:    "ok",
        citations:    [],
        snapshot:     MarketSnapshot.Empty,
        fingerprint:  fp,
        thinkingText: "",
        createdAt:    _now);

    [Fact]
    public void Decide_returns_Fetch_when_no_existing_suggestion()
    {
        var decision = SuggestionGate.Decide(
            existing: null,
            candidateFingerprint: PromptFingerprint.Of("hashA", "envA", "v1"));
        decision.ShouldBeOfType<GateDecision.Fetch>();
    }

    [Fact]
    public void Decide_returns_Reuse_when_fingerprint_matches_existing()
    {
        var fp = PromptFingerprint.Of("hashA", "envA", "v1");
        var existing = Existing(fp);
        var decision = SuggestionGate.Decide(
            existing: existing,
            candidateFingerprint: PromptFingerprint.Of("hashA", "envA", "v1"));
        decision.ShouldBeOfType<GateDecision.Reuse>();
        ((GateDecision.Reuse)decision).Existing.ShouldBeSameAs(existing);
    }

    [Fact]
    public void Decide_returns_Fetch_when_prompt_hash_differs()
    {
        var existing = Existing(PromptFingerprint.Of("hashA", "envA", "v1"));
        var decision = SuggestionGate.Decide(
            existing: existing,
            candidateFingerprint: PromptFingerprint.Of("hashB", "envA", "v1"));
        decision.ShouldBeOfType<GateDecision.Fetch>();
    }

    [Fact]
    public void Decide_returns_Fetch_when_envelope_hash_differs()
    {
        var existing = Existing(PromptFingerprint.Of("hashA", "envA", "v1"));
        var decision = SuggestionGate.Decide(
            existing: existing,
            candidateFingerprint: PromptFingerprint.Of("hashA", "envB", "v1"));
        decision.ShouldBeOfType<GateDecision.Fetch>();
    }

    [Fact]
    public void Decide_returns_Fetch_when_prompt_version_differs()
    {
        var existing = Existing(PromptFingerprint.Of("hashA", "envA", "v1"));
        var decision = SuggestionGate.Decide(
            existing: existing,
            candidateFingerprint: PromptFingerprint.Of("hashA", "envA", "v2"));
        decision.ShouldBeOfType<GateDecision.Fetch>();
    }
}
```

- [ ] **Step 8.2: Run failing**

`dotnet test TradyStrat.Domain.Tests --filter "FullyQualifiedName~SuggestionGateTests"` — build error.

- [ ] **Step 8.3: Implement**

`TradyStrat.Domain/Suggestions/Services/SuggestionGate.cs`:

```csharp
namespace TradyStrat.Domain.Suggestions.Services;

/// <summary>
/// Pure domain decision: given an optional existing Suggestion for
/// (instrumentId, date) and a candidate PromptFingerprint, decide whether
/// to reuse the existing row or fetch a fresh AI suggestion.
///
/// Reuse iff: existing != null AND existing.Fingerprint matches the candidate.
/// The per-(date, instrumentId) concurrency plumbing is separate
/// (Application/AiSuggestion/SuggestionGatePlumbing).
/// </summary>
public static class SuggestionGate
{
    public static GateDecision Decide(Suggestion? existing, PromptFingerprint candidateFingerprint)
    {
        if (existing is null) return new GateDecision.Fetch();
        if (existing.Fingerprint == candidateFingerprint) return new GateDecision.Reuse(existing);
        return new GateDecision.Fetch();
    }
}
```

`PromptFingerprint` is a `sealed record` so `==` is structural equality — comparing all three hash strings.

- [ ] **Step 8.4: Run tests passing**

`dotnet test TradyStrat.Domain.Tests --filter "FullyQualifiedName~SuggestionGateTests"` — expect 5/5.

- [ ] **Step 8.5: Commit**

```bash
git add TradyStrat.Domain/Suggestions/Services/SuggestionGate.cs TradyStrat.Domain.Tests/Suggestions/SuggestionGateTests.cs
git commit -m "feat(domain): SuggestionGate decision service — Phase 3"
```

---

## Task 9: `ISuggestionRepository` port

**Files:**
- Create: `TradyStrat.Application/AiSuggestion/ISuggestionRepository.cs`

Spec §8 repository interface.

- [ ] **Step 9.1: Create the port**

```csharp
using TradyStrat.Domain.Shared;
using TradyStrat.Domain.Suggestions;

namespace TradyStrat.Application.AiSuggestion;

public interface ISuggestionRepository
{
    Task<Suggestion?> GetForAsync(InstrumentId instrumentId, DateOnly date, CancellationToken ct);

    Task<IReadOnlyList<Suggestion>> ListForAsync(InstrumentId instrumentId, DateRange range, CancellationToken ct);

    Task<Suggestion?> LatestForAsync(InstrumentId instrumentId, CancellationToken ct);

    /// <summary>
    /// Lists across instruments, optionally filtered by date range and action.
    /// Backs the QuerySuggestionsUseCase.
    /// </summary>
    Task<IReadOnlyList<Suggestion>> QueryAsync(
        DateRange? range, SuggestionAction? action, int take, CancellationToken ct);

    /// <summary>
    /// Returns the prior suggestion (latest before `before`) for this instrument.
    /// Used by CallDiff (BuildFocusDerivedSliceUseCase).
    /// </summary>
    Task<Suggestion?> PriorToAsync(InstrumentId instrumentId, DateOnly before, CancellationToken ct);

    Task<IReadOnlyList<Suggestion>> RecentForAsync(
        InstrumentId instrumentId, DateOnly asOf, int count, CancellationToken ct);

    Task AddAsync(Suggestion suggestion, CancellationToken ct);
}
```

The 7 methods cover the 6 existing Spec patterns plus QueryAsync for general querying.

- [ ] **Step 9.2: Build**

```bash
dotnet build TradyStrat.Application
```

Expected: succeeds (port is just an interface; no consumers wired yet).

- [ ] **Step 9.3: Commit**

```bash
git add TradyStrat.Application/AiSuggestion/ISuggestionRepository.cs
git commit -m "feat(application): ISuggestionRepository port — Phase 3"
```

---

## Task 10: `SuggestionGatePlumbing` (Application — semaphore concern only)

**Files:**
- Rename + repurpose: `TradyStrat.Application/AiSuggestion/UseCases/SuggestionGate.cs` → `TradyStrat.Application/AiSuggestion/SuggestionGatePlumbing.cs`

The existing static class is purely the per-(date, instrumentId) semaphore. Move it to the `AiSuggestion` namespace root and rename to make the split explicit.

- [ ] **Step 10.1: Move + rename**

```bash
git mv TradyStrat.Application/AiSuggestion/UseCases/SuggestionGate.cs \
       TradyStrat.Application/AiSuggestion/SuggestionGatePlumbing.cs
```

- [ ] **Step 10.2: Update content**

```csharp
using System.Collections.Concurrent;

namespace TradyStrat.Application.AiSuggestion;

/// <summary>
/// Per-(date, instrumentId) mutex serializing today's-suggestion writes.
/// The pure decision (should we fetch?) lives in
/// TradyStrat.Domain.Suggestions.Services.SuggestionGate. This class is the
/// orchestration plumbing — a semaphore keyed by (date, instrumentId) that
/// ensures concurrent dashboard fans-out don't race on the same row.
/// </summary>
internal static class SuggestionGatePlumbing
{
    private static readonly ConcurrentDictionary<(DateOnly Date, int InstrumentId), SemaphoreSlim> Gates = new();

    public static SemaphoreSlim For(DateOnly date, int instrumentId)
        => Gates.GetOrAdd((date, instrumentId), _ => new SemaphoreSlim(1, 1));
}
```

- [ ] **Step 10.3: Update consumers**

```bash
grep -rln 'SuggestionGate\.For\|SuggestionGate\b' TradyStrat.Application --include='*.cs' | grep -v Domain
```

For each match (likely `GetTodaysSuggestionUseCase` and `StreamTodaysSuggestionsUseCase`): replace `SuggestionGate.For(...)` with `SuggestionGatePlumbing.For(...)`. (These use cases will be fully rewritten in Tasks 13-14, so this is a temporary fixup to keep the build green.)

- [ ] **Step 10.4: Build + commit**

```bash
dotnet build TradyStrat.Application
git add -A
git commit -m "refactor(application): split SuggestionGate plumbing from domain decision — Phase 3"
```

---

## Task 11: EF configuration for the new `Suggestion`

**Files:**
- Modify: `TradyStrat.Infrastructure/Data/Configurations/SuggestionConfiguration.cs` (rewrite for owned types + Citations table)
- Modify: `TradyStrat.Infrastructure/Data/AppDbContext.cs` (no new DbSet — Suggestion already there)

The EF mapping uses owned types for `QuantityHint`/`MaxPriceHint`/`Snapshot`/`Fingerprint`, owned-many for `Citations`, and a JSON value-converter for `MarketSnapshot` (since it's a complex nested object).

- [ ] **Step 11.1: Read the existing TradeConfiguration as a reference template**

`TradeConfiguration` (Phase 2) shows the pattern for owned `Money` and owned `Price`-wrapping-Money. Apply the same.

- [ ] **Step 11.2: Rewrite SuggestionConfiguration**

```csharp
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TradyStrat.Domain;
using TradyStrat.Domain.Shared;
using TradyStrat.Domain.Suggestions;

namespace TradyStrat.Infrastructure.Data.Configurations;

public sealed class SuggestionConfiguration : IEntityTypeConfiguration<Suggestion>
{
    private static readonly JsonSerializerOptions SnapshotJson = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
    };

    public void Configure(EntityTypeBuilder<Suggestion> builder)
    {
        builder.ToTable("Suggestions");
        builder.HasKey(s => s.Id);
        builder.Property(s => s.Id).ValueGeneratedNever();   // Suggestion AR-assigned via factory + AssignId

        builder.Property(s => s.InstrumentId);
        builder.Property(s => s.ForDate);
        builder.Property(s => s.Action);
        builder.Property(s => s.Rationale).HasMaxLength(4000);
        builder.Property(s => s.ThinkingText).HasMaxLength(20000);
        builder.Property(s => s.CreatedAt);

        // Conviction → single int column.
        builder.Property(s => s.Conviction)
               .HasConversion(c => c.Value, v => Conviction.Of(v));

        // QuantityHint (sealed record, parameterless ctor + private setters).
        builder.OwnsOne(s => s.QuantityHint, q =>
        {
            q.Property(x => x.Value).HasColumnName("QuantityHint").HasColumnType("TEXT");
            q.Property(x => x.IsSpecified).HasColumnName("QuantityHintIsSpecified");
        });

        // MaxPriceHint (Price wraps Money — nested OwnsOne).
        builder.OwnsOne(s => s.MaxPriceHint, p =>
        {
            p.OwnsOne(x => x.PerUnit, m =>
            {
                m.Property(x => x.Amount).HasColumnName("MaxPriceHint").HasColumnType("TEXT");
                m.Property(x => x.Currency).HasConversion(c => c.Code, s => Currency.Parse(s))
                 .HasColumnName("MaxPriceHintCurrency").HasMaxLength(3);
                m.Property(x => x.IsEmpty).HasColumnName("MaxPriceHintIsEmpty");
            });
        });

        // PromptFingerprint (owned VO → three existing columns).
        builder.OwnsOne(s => s.Fingerprint, fp =>
        {
            fp.Property(x => x.PromptHash).HasColumnName("PromptHash").HasMaxLength(128);
            fp.Property(x => x.EnvelopeHash).HasColumnName("EnvelopeHash").HasMaxLength(128);
            fp.Property(x => x.PromptVersionHash).HasColumnName("PromptVersionHash").HasMaxLength(128);
        });

        // MarketSnapshot (complex nested type) → JSON-encoded into a single column.
        // Uses a value converter rather than OwnsOne because the inner lists make
        // OwnsOne mapping unwieldy.
        builder.Property(s => s.Snapshot)
               .HasConversion(
                   v => JsonSerializer.Serialize(v, SnapshotJson),
                   v => string.IsNullOrEmpty(v) ? MarketSnapshot.Empty : JsonSerializer.Deserialize<MarketSnapshot>(v, SnapshotJson) ?? MarketSnapshot.Empty)
               .HasColumnName("MarketSnapshotJson")
               .HasMaxLength(20000);

        // Citations → owned-many in a new Citations table.
        builder.OwnsMany(s => s.Citations, c =>
        {
            c.ToTable("Citations");
            c.WithOwner().HasForeignKey("SuggestionId");
            c.Property<int>("Id").ValueGeneratedOnAdd();
            c.HasKey("Id");

            c.Property(x => x.Claim).HasMaxLength(2000);
            c.Property(x => x.Indicator).HasMaxLength(64);
            c.Property(x => x.Ticker).HasMaxLength(32);
            c.Property(x => x.Value).HasMaxLength(256);
        });
        builder.Navigation(s => s.Citations).UsePropertyAccessMode(PropertyAccessMode.Field);

        // Legacy CitationsJson column — dual-written by an EF interceptor (Task 12)
        // for the MCP server's read path. Dropping the column is a later cleanup
        // (spec §13.1). For now, the column exists but isn't mapped to a property.
        builder.Property<string>("CitationsJson").HasMaxLength(8000).HasDefaultValue("[]");

        builder.HasOne<Instrument>()
               .WithMany()
               .HasForeignKey(s => s.InstrumentId)
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(s => new { s.ForDate, s.InstrumentId }).IsUnique();
        builder.Ignore(s => s.OrderValue);
    }
}
```

The `HasIndex(...).IsUnique()` preserves the existing unique constraint on `(ForDate, InstrumentId)`.

- [ ] **Step 11.3: Build Infrastructure**

```bash
dotnet build TradyStrat.Infrastructure
```

Expected: succeeds (Domain + Application already green; Infrastructure was last broken state).

- [ ] **Step 11.4: Commit**

```bash
git add TradyStrat.Infrastructure/Data/Configurations/SuggestionConfiguration.cs
git commit -m "feat(infra): SuggestionConfiguration with VO-owned types + Citations table + JSON converter — Phase 3"
```

---

## Task 12: `CitationsJson` dual-write interceptor

**Files:**
- Create: `TradyStrat.Infrastructure/AiSuggestion/CitationsJsonDualWriteInterceptor.cs`
- Modify: `TradyStrat.Infrastructure/AiSuggestion/AiSuggestionInfrastructureModule.cs` (register interceptor)
- Modify: `TradyStrat.Infrastructure/Data/AppDbContext.cs` (add interceptor at OnConfiguring)

EF SaveChanges interceptor that serializes `Suggestion.Citations` into the legacy `CitationsJson` shadow property before insert/update, so the MCP server can keep reading from the JSON column until it migrates.

- [ ] **Step 12.1: Implement interceptor**

```csharp
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;
using TradyStrat.Domain.Suggestions;

namespace TradyStrat.Infrastructure.AiSuggestion;

internal sealed class CitationsJsonDualWriteInterceptor : SaveChangesInterceptor
{
    private static readonly JsonSerializerOptions Json = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
    };

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData, InterceptionResult<int> result, CancellationToken ct = default)
    {
        var ctx = eventData.Context;
        if (ctx is null) return base.SavingChangesAsync(eventData, result, ct);

        foreach (var entry in ctx.ChangeTracker.Entries<Suggestion>())
        {
            if (entry.State is not (EntityState.Added or EntityState.Modified)) continue;
            var json = JsonSerializer.Serialize(entry.Entity.Citations, Json);
            entry.Property("CitationsJson").CurrentValue = json;
        }

        return base.SavingChangesAsync(eventData, result, ct);
    }
}
```

- [ ] **Step 12.2: Register the interceptor on the DbContext**

Edit `TradyStrat.Infrastructure/Data/AppDbContext.cs`. Replace the `OnConfiguring` body's end so it also adds the interceptor:

```csharp
protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
{
    base.OnConfiguring(optionsBuilder);
    optionsBuilder.ConfigureWarnings(w =>
        w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.RelationalEventId.PendingModelChangesWarning));
    optionsBuilder.AddInterceptors(new AiSuggestion.CitationsJsonDualWriteInterceptor());
}
```

Add `using TradyStrat.Infrastructure.AiSuggestion;` at the top if needed.

- [ ] **Step 12.3: Build**

```bash
dotnet build TradyStrat.Infrastructure
```

Expected: succeeds.

- [ ] **Step 12.4: Commit**

```bash
git add TradyStrat.Infrastructure/AiSuggestion/CitationsJsonDualWriteInterceptor.cs \
        TradyStrat.Infrastructure/Data/AppDbContext.cs
git commit -m "feat(infra): CitationsJson dual-write interceptor — Phase 3"
```

---

## Task 13: EF migration

**Files:**
- Create: `TradyStrat.Infrastructure/Data/Migrations/<timestamp>_AddCitationsTable.cs`
- Create: `TradyStrat.Infrastructure/Data/Migrations/<timestamp>_AddCitationsTable.Designer.cs`
- Modify: `TradyStrat.Infrastructure/Data/Migrations/AppDbContextModelSnapshot.cs`

Generates the SQL changes for the new Suggestion shape. The migration creates the `Citations` table, renames/adds owned-type columns (`QuantityHint`/`QuantityHintIsSpecified`, `MaxPriceHint`/`MaxPriceHintCurrency`/`MaxPriceHintIsEmpty`), and includes backfill SQL: insert one `Citations` row per existing item parsed from `CitationsJson`.

- [ ] **Step 13.1: Generate the migration**

```bash
dotnet ef migrations add AddCitationsTable \
  --project TradyStrat.Infrastructure \
  --startup-project TradyStrat \
  --output-dir Data/Migrations
```

Inspect the generated `*_AddCitationsTable.cs`. Expect: `CreateTable("Citations")`, plus column alters/renames for the owned types.

- [ ] **Step 13.2: Add backfill SQL at end of `Up`**

Append after the auto-generated operations:

```csharp
        // Backfill: parse each Suggestion's CitationsJson into the new Citations table.
        // Each citation is {Claim, Indicator, Ticker, Value}. SQLite's json_each
        // extracts array elements; we insert one row per element.
        migrationBuilder.Sql(@"
            INSERT INTO Citations (SuggestionId, Claim, Indicator, Ticker, Value)
            SELECT s.Id,
                   COALESCE(json_extract(c.value, '$.claim'),     '') AS Claim,
                   COALESCE(json_extract(c.value, '$.indicator'), '') AS Indicator,
                   COALESCE(json_extract(c.value, '$.ticker'),    '') AS Ticker,
                   COALESCE(json_extract(c.value, '$.value'),     '') AS Value
            FROM Suggestions s, json_each(s.CitationsJson) c
            WHERE s.CitationsJson IS NOT NULL AND s.CitationsJson != '' AND s.CitationsJson != '[]';");
```

The `json_each` UDF is built into SQLite and reads stringified JSON arrays.

- [ ] **Step 13.3: Test the migration against a copy of the dev DB**

```bash
cp "$HOME/Library/Application Support/TradyStrat/tradystrat.db" /tmp/tradystrat-phase3-migration-test.db
TRADYSTRAT_DB=/tmp/tradystrat-phase3-migration-test.db dotnet ef database update \
  --project TradyStrat.Infrastructure \
  --startup-project TradyStrat
```

(If the env var override isn't supported, point user-secrets at the test DB instead.) Verify:

```bash
sqlite3 /tmp/tradystrat-phase3-migration-test.db "SELECT COUNT(*) FROM Citations;"
sqlite3 /tmp/tradystrat-phase3-migration-test.db "SELECT COUNT(*) FROM Suggestions;"
sqlite3 /tmp/tradystrat-phase3-migration-test.db "PRAGMA table_info(Suggestions);" | grep -E 'QuantityHint|MaxPriceHint'
```

Expect the Citations count to match the total citations across all suggestion JSONs.

- [ ] **Step 13.4: Commit**

```bash
git add TradyStrat.Infrastructure/Data/Migrations/
git commit -m "feat(infra): EF migration AddCitationsTable + JSON backfill — Phase 3"
```

---

## Task 14: `EfSuggestionRepository`

**Files:**
- Create: `TradyStrat.Infrastructure/AiSuggestion/EfSuggestionRepository.cs`
- Modify: `TradyStrat.Infrastructure/AiSuggestion/AiSuggestionInfrastructureModule.cs` (register repo)

- [ ] **Step 14.1: Implement repo**

```csharp
using Microsoft.EntityFrameworkCore;
using TradyStrat.Application.AiSuggestion;
using TradyStrat.Domain.Shared;
using TradyStrat.Domain.Suggestions;
using TradyStrat.Infrastructure.Data;

namespace TradyStrat.Infrastructure.AiSuggestion;

public sealed class EfSuggestionRepository(AppDbContext db) : ISuggestionRepository
{
    private IQueryable<Suggestion> WithIncludes()
        => db.Suggestions
            .Include("Citations");   // owned-many shadow nav

    public async Task<Suggestion?> GetForAsync(InstrumentId instrumentId, DateOnly date, CancellationToken ct)
        => await WithIncludes()
            .SingleOrDefaultAsync(s => s.InstrumentId == instrumentId && s.ForDate == date, ct);

    public async Task<IReadOnlyList<Suggestion>> ListForAsync(
        InstrumentId instrumentId, DateRange range, CancellationToken ct)
        => await WithIncludes()
            .Where(s => s.InstrumentId == instrumentId
                       && s.ForDate >= range.From && s.ForDate <= range.To)
            .OrderBy(s => s.ForDate)
            .ToListAsync(ct);

    public async Task<Suggestion?> LatestForAsync(InstrumentId instrumentId, CancellationToken ct)
        => await WithIncludes()
            .Where(s => s.InstrumentId == instrumentId)
            .OrderByDescending(s => s.ForDate)
            .ThenByDescending(s => s.CreatedAt)
            .FirstOrDefaultAsync(ct);

    public async Task<IReadOnlyList<Suggestion>> QueryAsync(
        DateRange? range, SuggestionAction? action, int take, CancellationToken ct)
    {
        var q = WithIncludes();
        if (range is { } r) q = q.Where(s => s.ForDate >= r.From && s.ForDate <= r.To);
        if (action is { } a) q = q.Where(s => s.Action == a);
        return await q.OrderByDescending(s => s.ForDate).Take(take).ToListAsync(ct);
    }

    public async Task<Suggestion?> PriorToAsync(
        InstrumentId instrumentId, DateOnly before, CancellationToken ct)
        => await WithIncludes()
            .Where(s => s.InstrumentId == instrumentId && s.ForDate < before)
            .OrderByDescending(s => s.ForDate)
            .ThenByDescending(s => s.CreatedAt)
            .FirstOrDefaultAsync(ct);

    public async Task<IReadOnlyList<Suggestion>> RecentForAsync(
        InstrumentId instrumentId, DateOnly asOf, int count, CancellationToken ct)
        => await WithIncludes()
            .Where(s => s.InstrumentId == instrumentId && s.ForDate <= asOf)
            .OrderByDescending(s => s.ForDate)
            .Take(count)
            .ToListAsync(ct);

    public async Task AddAsync(Suggestion suggestion, CancellationToken ct)
    {
        // Per-instrument sequential ID for parity with TradeId. We use the max+1 of
        // existing Suggestions overall (not per-instrument) for the AR-assigned ID;
        // SuggestionId is a domain identity but doesn't need partitioning since
        // (InstrumentId, ForDate) provides natural uniqueness.
        var nextId = (await db.Suggestions.AsNoTracking().Select(s => s.Id.Value).DefaultIfEmpty(0).MaxAsync(ct)) + 1;
        suggestion.AssignId(new SuggestionId(nextId));
        db.Suggestions.Add(suggestion);
        await db.SaveChangesAsync(ct);
    }
}
```

The ID-assignment-on-add mirrors the Trade pattern. The dual-write interceptor (Task 12) automatically populates `CitationsJson` on save.

- [ ] **Step 14.2: Register in the Infrastructure module**

Read `TradyStrat.Infrastructure/AiSuggestion/AiSuggestionInfrastructureModule.cs` and add:

```csharp
services.AddScoped<ISuggestionRepository, EfSuggestionRepository>();
```

- [ ] **Step 14.3: Build + commit**

```bash
dotnet build TradyStrat.Infrastructure
git add TradyStrat.Infrastructure/AiSuggestion/
git commit -m "feat(infra): EfSuggestionRepository — Phase 3"
```

---

## Task 15: Rewrite use cases against `ISuggestionRepository`

Six use cases need rewiring:
- `GetTodaysSuggestionUseCase`
- `GetAllTodaysSuggestionsUseCase`
- `StreamTodaysSuggestionsUseCase`
- `QuerySuggestionsUseCase`
- `BackfillSuggestionsUseCase`
- `ForceRefetchSuggestionUseCase`
- `ReplaySuggestionsUseCase`

(`SuggestionService` in Infrastructure also constructs `Suggestion` instances; that's covered in Task 16.)

Each follows the same pattern: replace `IReadRepositoryBase<Suggestion> repo` / `IRepositoryBase<Suggestion> repo` + Spec usage with `ISuggestionRepository repo`, swap the Spec calls for the equivalent typed method.

- [ ] **Step 15.1: Rewrite `GetTodaysSuggestionUseCase`**

Read the existing file to understand its shape, then replace:

```csharp
// Before: repo.FirstOrDefaultAsync(new SuggestionForDateSpec(today, instrumentId), ct)
// After:  repo.GetForAsync(new InstrumentId(instrumentId), today, ct)
```

Also: the gate decision — the use case should now call `SuggestionGate.Decide(existing, candidateFingerprint)` from the domain service. Construct the candidate `PromptFingerprint` after invoking `IAiSnapshotService` to build the snapshot (the prompt hash depends on the snapshot bytes).

The skeleton:

```csharp
using TradyStrat.Application.AiSuggestion.Snapshot;
using TradyStrat.Application.Time;
using TradyStrat.Application.UseCases;
using TradyStrat.Domain.Shared;
using TradyStrat.Domain.Suggestions;
using TradyStrat.Domain.Suggestions.Services;
using DomainSuggestionGate = TradyStrat.Domain.Suggestions.Services.SuggestionGate;

namespace TradyStrat.Application.AiSuggestion.UseCases;

public sealed class GetTodaysSuggestionUseCase(
    ISuggestionRepository suggestions,
    IAiSnapshotService snapshotSvc,
    IAiClient ai,
    IClock clock,
    ILogger<GetTodaysSuggestionUseCase> log)
    : UseCaseBase<GetTodaysSuggestionInput, Suggestion>(log)
{
    protected override async Task<Suggestion> ExecuteCore(GetTodaysSuggestionInput input, CancellationToken ct)
    {
        var iid = new InstrumentId(input.InstrumentId);
        var today = input.Date;

        await SuggestionGatePlumbing.For(today, input.InstrumentId).WaitAsync(ct);
        try
        {
            var existing = await suggestions.GetForAsync(iid, today, ct);
            var snapshot = await snapshotSvc.CreateAsync(input.InstrumentId, today, ct);
            var candidate = PromptFingerprint.Of(
                snapshot.PromptHash, snapshot.EnvelopeHash, snapshot.PromptVersionHash);

            var decision = DomainSuggestionGate.Decide(existing, candidate);
            if (decision is GateDecision.Reuse reuse)
                return reuse.Existing;

            var aiResponse = await ai.CallAsync(snapshot, ct);
            var newSuggestion = SuggestionFromAiResponse(aiResponse, iid, today, candidate, clock.UtcNow());
            await suggestions.AddAsync(newSuggestion, ct);
            return newSuggestion;
        }
        finally
        {
            SuggestionGatePlumbing.For(today, input.InstrumentId).Release();
        }
    }

    private static Suggestion SuggestionFromAiResponse(
        AiResponse r, InstrumentId iid, DateOnly forDate,
        PromptFingerprint fp, DateTime now)
    {
        return Suggestion.From(
            instrumentId: iid,
            forDate:      forDate,
            action:       r.Action,
            quantityHint: r.QuantityHint is { } q ? Quantity.Of(q) : Quantity.None,
            maxPriceHint: r.MaxPriceHint is { } p
                              ? Price.Of(Money.Of(p, Currency.Eur))
                              : Price.None(Currency.Eur),
            conviction:   Conviction.Of(r.Conviction),
            rationale:    r.Rationale,
            citations:    r.Citations,
            snapshot:     r.MarketSnapshot ?? MarketSnapshot.Empty,
            fingerprint:  fp,
            thinkingText: r.ThinkingText ?? "",
            createdAt:    now);
    }
}
```

The exact shape of `AiResponse` (the type returned by `IAiClient.CallAsync`) and `IAiSnapshotService.CreateAsync` outputs depends on existing code — read them before assuming property names. If `IAiClient` doesn't currently return a strongly-typed response, this task may need to introduce one.

- [ ] **Step 15.2: Rewrite the other 6 use cases**

Apply the same pattern to:
- `GetAllTodaysSuggestionsUseCase` — iterates instruments, calls the gate-+-fetch logic per instrument, returns a map. Inject `ISuggestionRepository`.
- `StreamTodaysSuggestionsUseCase` — same as above but yields `SuggestionStreamEvent` per instrument as work completes (parallelism via `MaxParallelSuggestions` setting).
- `QuerySuggestionsUseCase` — use `ISuggestionRepository.QueryAsync(...)`.
- `ForceRefetchSuggestionUseCase` — bypasses the gate decision; deletes existing, fetches, adds. Note: AR doesn't have a `Delete` method yet; either add one to `ISuggestionRepository` or `RemoveAsync` via the EF context.
- `BackfillSuggestionsUseCase` — iterates dates, calls `GetForAsync` to check existence, calls AI when missing, adds.
- `ReplaySuggestionsUseCase` — pure read; produces a report from `ListForAsync`.

For each, write or update its tests in `TradyStrat.Application.Tests/AiSuggestion/UseCases/` if any survived the Phase 2 deletion sweep (most were removed; rewrite minimal coverage here).

- [ ] **Step 15.3: Build progressively after each use case**

```bash
dotnet build TradyStrat.Application
```

Expected: succeeds after each rewrite. Compile errors point at remaining places that reference the old shape.

- [ ] **Step 15.4: Commit per use case (or in logical groups)**

```bash
# After each use case file change:
git add <file>
git commit -m "refactor(application): GetTodaysSuggestionUseCase uses ISuggestionRepository + domain SuggestionGate — Phase 3"
```

---

## Task 16: Update `SuggestionService` (Infrastructure AI adapter)

**Files:**
- Modify: `TradyStrat.Infrastructure/AiSuggestion/SuggestionService.cs`

`SuggestionService` is the Anthropic adapter implementing `IAiClient`. If it currently constructs a `Suggestion` directly (legacy shape), update to return an `AiResponse` shape and let the use case construct the `Suggestion` (already done in Task 15.1).

- [ ] **Step 16.1: Read `SuggestionService.cs`**

Determine whether it builds `Suggestion` instances or returns a DTO. If it builds `Suggestion`, refactor to return an `AiResponse` DTO (defined in `Application/AiSuggestion/`).

- [ ] **Step 16.2: Apply the changes**

If `AiResponse` needs to be created, place it at `TradyStrat.Application/AiSuggestion/AiResponse.cs`:

```csharp
using TradyStrat.Domain.Suggestions;

namespace TradyStrat.Application.AiSuggestion;

public sealed record AiResponse(
    SuggestionAction Action,
    decimal? QuantityHint,
    decimal? MaxPriceHint,
    int Conviction,
    string Rationale,
    IReadOnlyList<Citation> Citations,
    MarketSnapshot? MarketSnapshot,
    string? ThinkingText);
```

- [ ] **Step 16.3: Build + commit**

```bash
dotnet build TradyStrat.Infrastructure
git add -A
git commit -m "refactor(infra): SuggestionService returns AiResponse DTO — Phase 3"
```

---

## Task 17: Update `SuggestionBackfillCoordinator`

**Files:**
- Modify: `TradyStrat.Application/AiSuggestion/Backfill/SuggestionBackfillCoordinator.cs`

The coordinator orchestrates per-instrument backfill with a first-fail-stop policy. It calls suggestion use cases internally — update its injected use case references but otherwise preserve behavior.

- [ ] **Step 17.1: Read the existing file**

- [ ] **Step 17.2: Apply the change**

Typically only the use case input or the surface property changes need adjustment. No domain logic change.

- [ ] **Step 17.3: Build + commit**

```bash
dotnet build TradyStrat.Application
git add TradyStrat.Application/AiSuggestion/Backfill/SuggestionBackfillCoordinator.cs
git commit -m "refactor(application): SuggestionBackfillCoordinator consumes ISuggestionRepository — Phase 3"
```

---

## Task 18: Update `RecentSuggestionsSection`

**Files:**
- Modify: `TradyStrat.Application/AiSuggestion/Snapshot/Sections/RecentSuggestionsSection.cs`

Already partially updated in Phase 2 (uses `IPortfolioRepository` for trade flow). Update its `Suggestion` consumption:
- `s.Citations` → typed `IReadOnlyList<Citation>` (no more JSON deserialization)
- `s.Rationale` → already a string
- `s.Conviction` → `s.Conviction.Value` (was int, now VO)
- `s.Action` → unchanged enum

- [ ] **Step 18.1: Apply the changes**

Read the file and replace any `s.Conviction` references with `s.Conviction.Value` for int contexts. Any `MarketSnapshotJson` deserialization is no longer needed — read `s.Snapshot` directly.

- [ ] **Step 18.2: Build + commit**

```bash
dotnet build TradyStrat.Application
git add TradyStrat.Application/AiSuggestion/Snapshot/Sections/RecentSuggestionsSection.cs
git commit -m "refactor(application): RecentSuggestionsSection consumes typed Suggestion fields — Phase 3"
```

---

## Task 19: Move Suggestion `Specifications` to Infrastructure (or delete)

**Files:**
- Decide: move or delete each of `TradyStrat.Application/AiSuggestion/Specifications/*.cs`

After Tasks 15-18, the Application no longer uses `IReadRepositoryBase<Suggestion>` directly — everything goes through `ISuggestionRepository`. The 6 Spec classes (`LatestSuggestionSpec`, `PriorSuggestionSpec`, `SuggestionsInRangeSpec`, `SuggestionForDateSpec`, `RecentSuggestionsForInstrumentSpec`, `SuggestionsQuerySpec`) are now dead code.

- [ ] **Step 19.1: Check for remaining consumers**

```bash
grep -rln 'LatestSuggestionSpec\|PriorSuggestionSpec\|SuggestionsInRangeSpec\|SuggestionForDateSpec\|RecentSuggestionsForInstrumentSpec\|SuggestionsQuerySpec' \
  TradyStrat.Application TradyStrat.Infrastructure TradyStrat TradyStrat.Cli --include='*.cs'
```

If no consumers remain outside the Specifications folder itself, delete them.

- [ ] **Step 19.2: Delete (or move to Infrastructure if still used)**

```bash
git rm TradyStrat.Application/AiSuggestion/Specifications/*.cs
rmdir TradyStrat.Application/AiSuggestion/Specifications 2>/dev/null
```

- [ ] **Step 19.3: Build + commit**

```bash
dotnet build TradyStrat.slnx
git add -A
git commit -m "chore: delete Suggestion Specifications (replaced by ISuggestionRepository) — Phase 3"
```

---

## Task 20: Migrate `FixedThresholdCorrectnessTests` to Domain.Tests

**Files:**
- Find: existing tests for `FixedThresholdCorrectness` (likely in `Application.Tests` or `Domain.Tests/Domain/`)
- Move to: `TradyStrat.Domain.Tests/Suggestions/FixedThresholdCorrectnessTests.cs`

- [ ] **Step 20.1: Locate the file**

```bash
find . -name 'FixedThresholdCorrectness*Tests.cs' -not -path '*/bin/*' -not -path '*/obj/*' -not -path '*/worktrees/*'
```

- [ ] **Step 20.2: Move + update namespace**

```bash
git mv <found-path> TradyStrat.Domain.Tests/Suggestions/FixedThresholdCorrectnessTests.cs
```

Change the `namespace` declaration in the moved file to `TradyStrat.Domain.Tests.Suggestions;` and update the `using` to reference `TradyStrat.Domain.Suggestions.Services;`.

- [ ] **Step 20.3: Run tests**

```bash
dotnet test TradyStrat.Domain.Tests --filter "FullyQualifiedName~FixedThresholdCorrectness"
```

Expected: pass.

- [ ] **Step 20.4: Commit**

```bash
git add -A
git commit -m "test(domain): FixedThresholdCorrectnessTests moves to Domain.Tests/Suggestions — Phase 3"
```

---

## Task 21: `EfSuggestionRepositoryTests`

**Files:**
- Create: `TradyStrat.Infrastructure.Tests/AiSuggestion/EfSuggestionRepositoryTests.cs`

Round-trip tests verifying the EF mapping. Use the existing `InMemoryDb.Create()` helper.

- [ ] **Step 21.1: Write tests**

```csharp
using Shouldly;
using TradyStrat.Domain.Shared;
using TradyStrat.Domain.Suggestions;
using TradyStrat.Infrastructure.AiSuggestion;
using TradyStrat.TestKit.Specifications;
using Xunit;

namespace TradyStrat.Infrastructure.Tests.AiSuggestion;

public class EfSuggestionRepositoryTests
{
    private static readonly DateTime _now = new(2026, 5, 22, 12, 0, 0, DateTimeKind.Utc);

    private static Suggestion Build(int instrumentId, DateOnly forDate,
        Quantity? qty = null, Price? price = null) => Suggestion.From(
        instrumentId: new InstrumentId(instrumentId),
        forDate:      forDate,
        action:       SuggestionAction.Hold,
        quantityHint: qty ?? Quantity.None,
        maxPriceHint: price ?? Price.None(Currency.Eur),
        conviction:   Conviction.Of(7),
        rationale:    "Test rationale.",
        citations:    [new Citation("c1", "rsi", "X", "30")],
        snapshot:     MarketSnapshot.Empty,
        fingerprint:  PromptFingerprint.Of("h1", "e1", "v1"),
        thinkingText: "",
        createdAt:    _now);

    [Fact]
    public async Task Add_and_get_round_trips_typed_fields()
    {
        await using var db = InMemoryDb.Create();
        var ct = TestContext.Current.CancellationToken;
        var repo = new EfSuggestionRepository(db);

        await repo.AddAsync(Build(1, new DateOnly(2026, 5, 22),
            qty: Quantity.Of(10m),
            price: Price.Of(Money.Of(4m, Currency.Eur))), ct);

        var fresh = await repo.GetForAsync(new InstrumentId(1), new DateOnly(2026, 5, 22), ct);
        fresh.ShouldNotBeNull();
        fresh.QuantityHint.Value.ShouldBe(10m);
        fresh.MaxPriceHint.PerUnit.Amount.ShouldBe(4m);
        fresh.Conviction.Value.ShouldBe(7);
        fresh.Citations.Count.ShouldBe(1);
        fresh.Citations[0].Claim.ShouldBe("c1");
        fresh.Fingerprint.PromptHash.ShouldBe("h1");
    }

    [Fact]
    public async Task ListFor_filters_by_instrument_and_date_range()
    {
        await using var db = InMemoryDb.Create();
        var ct = TestContext.Current.CancellationToken;
        var repo = new EfSuggestionRepository(db);

        await repo.AddAsync(Build(1, new DateOnly(2026, 5, 20)), ct);
        await repo.AddAsync(Build(1, new DateOnly(2026, 5, 22)), ct);
        await repo.AddAsync(Build(1, new DateOnly(2026, 5, 25)), ct);
        await repo.AddAsync(Build(2, new DateOnly(2026, 5, 22)), ct);

        var list = await repo.ListForAsync(
            new InstrumentId(1),
            new DateRange(new DateOnly(2026, 5, 21), new DateOnly(2026, 5, 24)),
            ct);
        list.Count.ShouldBe(1);
        list[0].ForDate.ShouldBe(new DateOnly(2026, 5, 22));
    }
}
```

- [ ] **Step 21.2: Run tests**

```bash
dotnet test TradyStrat.Infrastructure.Tests --filter "FullyQualifiedName~EfSuggestionRepositoryTests"
```

Expected: 2/2 pass.

- [ ] **Step 21.3: Commit**

```bash
git add TradyStrat.Infrastructure.Tests/AiSuggestion/EfSuggestionRepositoryTests.cs
git commit -m "test(infra): EfSuggestionRepository round-trip — Phase 3"
```

---

## Task 22: Final verification + smoke test

- [ ] **Step 22.1: Full clean build**

```bash
dotnet clean TradyStrat.slnx
dotnet build TradyStrat.slnx
```

Expected: 0 errors, 0 warnings.

- [ ] **Step 22.2: Full test suite**

```bash
dotnet test TradyStrat.slnx --no-build
```

Expected: every test passes. Counts up vs Phase 2 baseline (397) by ~20 new Phase 3 tests.

- [ ] **Step 22.3: Grep forbidden references**

```bash
# Old anemic Suggestion shape should be gone.
grep -rn '\.CitationsJson\|\.MarketSnapshotJson\|\.PromptHash\b\|\.EnvelopeHash\|\.PromptVersionHash' \
  TradyStrat.Application TradyStrat.Infrastructure TradyStrat TradyStrat.Cli \
  --include='*.cs' --include='*.razor*'
```

Expected: hits only in `SuggestionConfiguration.cs` (the legacy shadow property) and the dual-write interceptor — no use-case or section/page code references.

- [ ] **Step 22.4: Apply migration to a copy of the dev DB**

```bash
cp "$HOME/Library/Application Support/TradyStrat/tradystrat.db" \
   "$HOME/Library/Application Support/TradyStrat/tradystrat.db.pre-phase3.bak"
dotnet ef database update --project TradyStrat.Infrastructure --startup-project TradyStrat
```

Verify:

```bash
sqlite3 "$HOME/Library/Application Support/TradyStrat/tradystrat.db" \
  "SELECT COUNT(*) FROM Citations; SELECT MigrationId FROM __EFMigrationsHistory ORDER BY MigrationId DESC LIMIT 1;"
```

Expected: Citations populated from JSON; latest migration is `AddCitationsTable`.

- [ ] **Step 22.5: Run the app, hit each page**

```bash
dotnet run --project TradyStrat
```

In another shell (or via the background task pattern from Phase 2 verification):

```bash
curl -sf http://localhost:5180/         | head -5    # dashboard renders
curl -sf http://localhost:5180/trades   | head -5    # trades page renders
curl -sf http://localhost:5180/settings | head -5    # settings renders
```

Each should return HTML, not 500.

Optionally: trigger an AI suggestion call by visiting the dashboard with an API key set. The new suggestion should:
- Be inserted with all VO-typed fields populated.
- Have `Citations` rows in the new table.
- Have `CitationsJson` also populated (dual-write).
- Match the pre-Phase-3 user-facing format byte-for-byte (rationale, conviction display, etc.).

Stop the app with `Ctrl-C`.

- [ ] **Step 22.6: Phase 3 checkpoint tag**

```bash
git tag -a phase3-suggestion-ar-done -m "Phase 3 (Suggestion aggregate) complete"
```

- [ ] **Step 22.7: Open PR**

```bash
git push -u origin worktree-ddd-phase3
gh pr create --title "DDD rework Phase 3 — Suggestion aggregate" --body "$(cat <<'EOF'
## Summary
- Suggestion becomes a rich AR with VO fields (Quantity/Price/Conviction), typed `IReadOnlyList<Citation>` mapped as owned-many `Citations` table, MarketSnapshot owned VO via JSON converter, and PromptFingerprint owned VO bundling the 3 hash columns.
- ISuggestionRepository replaces IRepositoryBase<Suggestion> for AR access. The six Application Specs are deleted (no consumers remain).
- SuggestionGate split: domain decision (Domain/Suggestions/Services/SuggestionGate.Decide) + Application plumbing (SuggestionGatePlumbing semaphore).
- Migration AddCitationsTable backfills the new Citations rows from existing CitationsJson; an EF SaveChanges interceptor dual-writes both columns going forward for the MCP server's read path (spec §13.1 cleanup is deferred).

## Test plan
- [ ] `dotnet test TradyStrat.slnx` is green
- [ ] Dashboard renders identical numbers vs pre-Phase-3 baseline
- [ ] AI suggestion call produces a Suggestion with both Citations table rows AND CitationsJson populated
- [ ] MCP `query_suggestions` returns the same shape as before

🤖 Generated with [Claude Code](https://claude.com/claude-code)
EOF
)"
```

---

# Plan summary

**Phase 3 tasks (22):**
- 1-2: kernel VOs (Conviction, PromptFingerprint)
- 3-4: move MarketSnapshot + correctness rules to Domain
- 5-7: return records + Suggestion AR rewrite + factory tests
- 8: SuggestionGate domain service + tests
- 9-10: ISuggestionRepository port + Application plumbing rename
- 11-13: EF configuration + dual-write interceptor + migration
- 14: EfSuggestionRepository + tests
- 15-19: use case rewrites + AI service + section consumer + spec deletion
- 20-21: test migration + repo round-trip
- 22: final verification + smoke

**Behavioral guarantee:** AI-snapshot JSON sent to Anthropic, suggestion rationale displayed in UI, MCP server's `query_suggestions` output — all byte-identical to pre-Phase-3.

**Risks called out:**
- Task 11 owned-type mapping for `MarketSnapshot` via JSON converter could break if the existing serialization shape (snake_case via `SuggestionService`) differs from what `JsonSerializer.Deserialize` expects. The `SnapshotJson` options use `SnakeCaseLower` to match.
- Task 13 SQL backfill uses SQLite's `json_each` UDF; verify it's available (built-in since 3.38, should be fine on .NET 10's bundled `Microsoft.Data.Sqlite`).
- Task 15 use case rewrites are the broadest change; expect compile errors to ripple until all 6 are updated.
- The `SuggestionService` may currently construct `Suggestion` instances directly with the old shape — Task 16 may need to introduce an `AiResponse` DTO if one doesn't exist.

---

## Self-review notes

**Spec coverage** (against §4, §5, §8):
- §4 Conviction VO — Task 1 ✓
- §4 PromptFingerprint aggregate-local VO — Task 2 ✓
- §4 MarketSnapshot location (Domain/Suggestions/) — Task 3 ✓
- §8 ICorrectnessRule/FixedThresholdCorrectness in Domain/Suggestions/Services/ — Task 4 ✓
- §8 Correctness + GateDecision return records — Task 5 ✓
- §8 Suggestion rewrite as AR with WasCorrect — Task 7 ✓
- §8 Citation typed collection (owned-many) — Tasks 6, 11 ✓
- §8 SuggestionGate split (decision in Domain, plumbing in Application) — Tasks 8, 10 ✓
- §8 ISuggestionRepository per-aggregate port — Task 9 ✓
- §8 EF mapping for Suggestion AR — Task 11 ✓
- §8 Migration with backfill — Task 13 ✓
- §8 Dual-write interceptor for CitationsJson — Task 12 ✓
- §8 Use cases updated — Tasks 15-17 ✓
- §8 RecentSuggestionsSection updated — Task 18 ✓
- §8 Specifications relocated/deleted — Task 19 ✓
- §8 Tests migrated — Tasks 20-21 ✓

**Type consistency:** `PromptFingerprint.Of(string, string?, string?)` consistent in Tasks 2, 7, 8, 21. `SuggestionGate.Decide(Suggestion?, PromptFingerprint)` consistent in Tasks 5, 8, 15. `ISuggestionRepository` methods consistent in Tasks 9, 14, 15, 21.

**Placeholder scan:** spot-checked Tasks 5, 11, 15 — Tasks 15-17 (use case rewrites) are necessarily abbreviated because they touch 6 use cases each with their own context. The Task 15.1 example is full; 15.2 references "apply the same pattern" — implementers should treat the file-level rewrites as their own sub-task. This is the only "follow the pattern" handwave in the plan.
