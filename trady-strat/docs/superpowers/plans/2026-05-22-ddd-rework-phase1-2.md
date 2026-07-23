# DDD Rework — Phase 1 + Phase 2 Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Land Phase 1 (shared kernel seed of value objects) and Phase 2 (full `Portfolio` aggregate root with FIFO accounting moved out of `PortfolioService` into the domain), delivering identical dashboard numbers and AI snapshot output as today.

**Architecture:** Phase 1 introduces six VO types + five strongly typed IDs in `TradyStrat.Domain/Shared/`, dormant until Phase 2 wires them. Phase 2 introduces `Portfolio` AR owning `Position` child entities, each owning `Lot` + `Trade` child entities. FIFO lot accounting + realized P&L move from `PortfolioService.BuildSnapshot` to `Position.Record`. `PortfolioService` and `GrowthSeriesBuilder` are deleted. `IPortfolioRepository` replaces generic `IRepositoryBase<Trade>` for AR access. EF maps the rich domain directly via value converters, owned types, and backing fields. Cross-aggregate data (instrument ticker/currency, goal target) flows in as `Snapshot(...)` parameters resolved by use cases — Position holds only `InstrumentId`.

**Tech Stack:** .NET 10, EF Core 10.0.7 (Sqlite), Ardalis.Specification 9.3.1, xunit.v3 3.2.2, Shouldly 4.3.0, hexagonal layering (`TradyStrat.Domain` → `TradyStrat.Application` → `TradyStrat.Infrastructure`).

**Spec reference:** [`docs/superpowers/specs/2026-05-22-ddd-domain-rework-design.md`](../specs/2026-05-22-ddd-domain-rework-design.md).

**Behavioral contract:** every fixture in today's `TradyStrat.Infrastructure.Tests/Portfolio/PortfolioService*Tests.cs` (108+38+63 = 209 lines, multiple `[Fact]`s) must pass byte-for-byte against the new `Portfolio.Snapshot` API. The migrated test suite (`TradyStrat.Domain.Tests/Portfolio/PortfolioTests.cs`) is the regression contract.

---

## Pre-work: Worktree setup

- [ ] **Step 0.1: Create an isolated worktree for this plan**

Per the user's memory note (prefers isolated worktrees for multi-commit work), do not work on `main`. Either use the harness `EnterWorktree` tool or git CLI:

```bash
git worktree add ../TradyStrat-ddd-phase1-2 -b worktree-ddd-phase1-2
cd ../TradyStrat-ddd-phase1-2
```

All subsequent paths in this plan are relative to the worktree root (which mirrors the main repo's layout).

- [ ] **Step 0.2: Verify baseline build + tests pass**

Run: `dotnet build TradyStrat.slnx`
Expected: build succeeds; no warnings about CS errors.

Run: `dotnet test TradyStrat.slnx --no-build`
Expected: all tests pass. Note the count for later comparison.

If this is failing on `main` already, **stop** and surface that to the user before continuing — this plan assumes a green baseline.

---

# Phase 1 — Shared kernel seed

Goal: six VO files in `TradyStrat.Domain/Shared/` + five strongly typed ID structs + EF `ValueConverter` conventions hooked into `AppDbContext` but unused. Each VO is one task with its tests. After this phase, the solution builds, all tests pass, and no production code uses the new types yet.

## Task 1: `CurrencyMismatchException`

**Files:**
- Create: `TradyStrat.Domain/Exceptions/CurrencyMismatchException.cs`

This is needed by `Money` arithmetic. Standalone first.

- [ ] **Step 1.1: Write the exception class**

Content for `TradyStrat.Domain/Exceptions/CurrencyMismatchException.cs`:

```csharp
namespace TradyStrat.Domain.Exceptions;

public sealed class CurrencyMismatchException(string message) : TradyStratException(message);
```

- [ ] **Step 1.2: Verify it compiles**

Run: `dotnet build TradyStrat.Domain`
Expected: succeeds.

- [ ] **Step 1.3: Commit**

```bash
git add TradyStrat.Domain/Exceptions/CurrencyMismatchException.cs
git commit -m "feat(domain): CurrencyMismatchException for Money VO arithmetic — Phase 1"
```

---

## Task 2: `Currency` VO

**Files:**
- Create: `TradyStrat.Domain/Shared/Currency.cs`
- Test:   `TradyStrat.Domain.Tests/Shared/CurrencyTests.cs`

- [ ] **Step 2.1: Write failing tests**

Create `TradyStrat.Domain.Tests/Shared/CurrencyTests.cs`:

```csharp
using Shouldly;
using TradyStrat.Domain.Shared;
using Xunit;

namespace TradyStrat.Domain.Tests.Shared;

public class CurrencyTests
{
    [Fact]
    public void Parse_normalizes_to_uppercase()
    {
        Currency.Parse("usd").Code.ShouldBe("USD");
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("US")]
    [InlineData("USDX")]
    [InlineData(null)]
    public void Parse_rejects_invalid_inputs(string? input)
    {
        Should.Throw<ArgumentException>(() => Currency.Parse(input!));
    }

    [Fact]
    public void Static_accessors_exist()
    {
        Currency.Eur.Code.ShouldBe("EUR");
        Currency.Usd.Code.ShouldBe("USD");
        Currency.Gbp.Code.ShouldBe("GBP");
    }

    [Fact]
    public void Equality_is_structural()
    {
        Currency.Parse("USD").ShouldBe(Currency.Parse("usd"));
        Currency.Eur.ShouldNotBe(Currency.Usd);
    }
}
```

- [ ] **Step 2.2: Run tests to verify they fail**

Run: `dotnet test TradyStrat.Domain.Tests --filter "FullyQualifiedName~CurrencyTests"`
Expected: build fails (type `Currency` not defined) or tests fail.

- [ ] **Step 2.3: Implement `Currency`**

Create `TradyStrat.Domain/Shared/Currency.cs`:

```csharp
namespace TradyStrat.Domain.Shared;

public readonly record struct Currency
{
    public string Code { get; }

    private Currency(string code) => Code = code;

    public static Currency Parse(string code)
    {
        if (string.IsNullOrWhiteSpace(code) || code.Trim().Length != 3)
            throw new ArgumentException($"Invalid ISO 4217 code: '{code}'.", nameof(code));
        return new Currency(code.Trim().ToUpperInvariant());
    }

    public static Currency Eur => new("EUR");
    public static Currency Usd => new("USD");
    public static Currency Gbp => new("GBP");

    public override string ToString() => Code;
}
```

- [ ] **Step 2.4: Run tests to verify they pass**

Run: `dotnet test TradyStrat.Domain.Tests --filter "FullyQualifiedName~CurrencyTests"`
Expected: all four facts/theories pass.

- [ ] **Step 2.5: Commit**

```bash
git add TradyStrat.Domain/Shared/Currency.cs TradyStrat.Domain.Tests/Shared/CurrencyTests.cs
git commit -m "feat(domain): Currency VO — Phase 1"
```

---

## Task 3: `Ticker` VO

**Files:**
- Create: `TradyStrat.Domain/Shared/Ticker.cs`
- Test:   `TradyStrat.Domain.Tests/Shared/TickerTests.cs`

- [ ] **Step 3.1: Write failing tests**

Create `TradyStrat.Domain.Tests/Shared/TickerTests.cs`:

```csharp
using Shouldly;
using TradyStrat.Domain.Shared;
using Xunit;

namespace TradyStrat.Domain.Tests.Shared;

public class TickerTests
{
    [Fact]
    public void Of_accepts_well_formed_yahoo_symbol()
    {
        Ticker.Of("CON3.L").Value.ShouldBe("CON3.L");
        Ticker.Of("BTC-USD").Value.ShouldBe("BTC-USD");
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("AB C")]
    [InlineData(null)]
    public void Of_rejects_invalid(string? input)
    {
        Should.Throw<ArgumentException>(() => Ticker.Of(input!));
    }

    [Fact]
    public void Equality_is_structural()
    {
        Ticker.Of("CON3.L").ShouldBe(Ticker.Of("CON3.L"));
        Ticker.Of("CON3.L").ShouldNotBe(Ticker.Of("COIN"));
    }
}
```

- [ ] **Step 3.2: Run failing test**

Run: `dotnet test TradyStrat.Domain.Tests --filter "FullyQualifiedName~TickerTests"`
Expected: build error (type not defined).

- [ ] **Step 3.3: Implement `Ticker`**

Create `TradyStrat.Domain/Shared/Ticker.cs`:

```csharp
namespace TradyStrat.Domain.Shared;

public readonly record struct Ticker
{
    public string Value { get; }

    private Ticker(string value) => Value = value;

    public static Ticker Of(string symbol)
    {
        if (string.IsNullOrWhiteSpace(symbol) || symbol.Any(char.IsWhiteSpace))
            throw new ArgumentException($"Invalid ticker: '{symbol}'.", nameof(symbol));
        return new Ticker(symbol.Trim());
    }

    public override string ToString() => Value;
}
```

- [ ] **Step 3.4: Run tests passing**

Run: `dotnet test TradyStrat.Domain.Tests --filter "FullyQualifiedName~TickerTests"`
Expected: all pass.

- [ ] **Step 3.5: Commit**

```bash
git add TradyStrat.Domain/Shared/Ticker.cs TradyStrat.Domain.Tests/Shared/TickerTests.cs
git commit -m "feat(domain): Ticker VO — Phase 1"
```

---

## Task 4: `Money` VO

**Files:**
- Create: `TradyStrat.Domain/Shared/Money.cs`
- Test:   `TradyStrat.Domain.Tests/Shared/MoneyTests.cs`

- [ ] **Step 4.1: Write failing tests**

Create `TradyStrat.Domain.Tests/Shared/MoneyTests.cs`:

```csharp
using Shouldly;
using TradyStrat.Domain.Exceptions;
using TradyStrat.Domain.Shared;
using Xunit;

namespace TradyStrat.Domain.Tests.Shared;

public class MoneyTests
{
    [Fact]
    public void Zero_is_real_value()
    {
        var z = Money.Zero(Currency.Eur);
        z.Amount.ShouldBe(0m);
        z.IsEmpty.ShouldBeFalse();
    }

    [Fact]
    public void None_is_absence_sentinel()
    {
        var n = Money.None(Currency.Eur);
        n.IsEmpty.ShouldBeTrue();
        n.ShouldNotBe(Money.Zero(Currency.Eur));
    }

    [Fact]
    public void None_equality_requires_matching_currency()
    {
        Money.None(Currency.Eur).ShouldBe(Money.None(Currency.Eur));
        Money.None(Currency.Eur).ShouldNotBe(Money.None(Currency.Usd));
    }

    [Fact]
    public void Add_requires_matching_currency()
    {
        var a = Money.Of(10m, Currency.Eur);
        var b = Money.Of(5m, Currency.Eur);
        (a + b).ShouldBe(Money.Of(15m, Currency.Eur));

        Should.Throw<CurrencyMismatchException>(() => a + Money.Of(5m, Currency.Usd));
    }

    [Fact]
    public void Subtract_requires_matching_currency_and_can_go_negative()
    {
        var a = Money.Of(3m, Currency.Eur);
        var b = Money.Of(10m, Currency.Eur);
        (a - b).ShouldBe(Money.Of(-7m, Currency.Eur));
    }

    [Fact]
    public void Multiply_by_scalar_preserves_currency()
    {
        (Money.Of(4m, Currency.Eur) * 2.5m).ShouldBe(Money.Of(10m, Currency.Eur));
    }

    [Fact]
    public void Divide_by_money_returns_ratio()
    {
        (Money.Of(50m, Currency.Eur) / Money.Of(10m, Currency.Eur)).ShouldBe(5m);
        Should.Throw<CurrencyMismatchException>(() =>
            Money.Of(10m, Currency.Eur) / Money.Of(1m, Currency.Usd));
    }

    [Fact]
    public void Arithmetic_on_None_throws()
    {
        var n = Money.None(Currency.Eur);
        var v = Money.Of(5m, Currency.Eur);
        Should.Throw<InvalidOperationException>(() => n + v);
        Should.Throw<InvalidOperationException>(() => v - n);
    }
}
```

- [ ] **Step 4.2: Run failing tests**

Run: `dotnet test TradyStrat.Domain.Tests --filter "FullyQualifiedName~MoneyTests"`
Expected: build error.

- [ ] **Step 4.3: Implement `Money`**

Create `TradyStrat.Domain/Shared/Money.cs`:

```csharp
using TradyStrat.Domain.Exceptions;

namespace TradyStrat.Domain.Shared;

public sealed record Money
{
    public decimal Amount   { get; }
    public Currency Currency { get; }
    public bool IsEmpty     { get; }

    private Money(decimal amount, Currency currency, bool isEmpty)
    {
        Amount = amount;
        Currency = currency;
        IsEmpty = isEmpty;
    }

    public static Money Of(decimal amount, Currency currency) => new(amount, currency, isEmpty: false);
    public static Money Zero(Currency currency)               => new(0m, currency, isEmpty: false);
    public static Money None(Currency currency)               => new(0m, currency, isEmpty: true);

    private static void RequireSpecified(Money m, string op)
    {
        if (m.IsEmpty)
            throw new InvalidOperationException($"Cannot perform '{op}' on Money.None({m.Currency}).");
    }

    private static void RequireMatchingCurrency(Money a, Money b, string op)
    {
        if (a.Currency != b.Currency)
            throw new CurrencyMismatchException(
                $"Cannot {op} {a.Currency} and {b.Currency}.");
    }

    public static Money operator +(Money a, Money b)
    {
        RequireSpecified(a, "+"); RequireSpecified(b, "+");
        RequireMatchingCurrency(a, b, "add");
        return Of(a.Amount + b.Amount, a.Currency);
    }

    public static Money operator -(Money a, Money b)
    {
        RequireSpecified(a, "-"); RequireSpecified(b, "-");
        RequireMatchingCurrency(a, b, "subtract");
        return Of(a.Amount - b.Amount, a.Currency);
    }

    public static Money operator *(Money m, decimal scalar)
    {
        RequireSpecified(m, "*");
        return Of(m.Amount * scalar, m.Currency);
    }

    public static Money operator *(decimal scalar, Money m) => m * scalar;

    public static Money operator /(Money m, decimal scalar)
    {
        RequireSpecified(m, "/");
        if (scalar == 0m) throw new DivideByZeroException();
        return Of(m.Amount / scalar, m.Currency);
    }

    public static decimal operator /(Money a, Money b)
    {
        RequireSpecified(a, "/"); RequireSpecified(b, "/");
        RequireMatchingCurrency(a, b, "divide");
        if (b.Amount == 0m) throw new DivideByZeroException();
        return a.Amount / b.Amount;
    }

    public override string ToString() => IsEmpty ? $"None({Currency})" : $"{Amount} {Currency}";
}
```

- [ ] **Step 4.4: Run tests pass**

Run: `dotnet test TradyStrat.Domain.Tests --filter "FullyQualifiedName~MoneyTests"`
Expected: all pass.

- [ ] **Step 4.5: Commit**

```bash
git add TradyStrat.Domain/Shared/Money.cs TradyStrat.Domain.Tests/Shared/MoneyTests.cs
git commit -m "feat(domain): Money VO with arithmetic + None sentinel — Phase 1"
```

---

## Task 5: `Quantity` VO

**Files:**
- Create: `TradyStrat.Domain/Shared/Quantity.cs`
- Test:   `TradyStrat.Domain.Tests/Shared/QuantityTests.cs`

- [ ] **Step 5.1: Write failing tests**

```csharp
using Shouldly;
using TradyStrat.Domain.Shared;
using Xunit;

namespace TradyStrat.Domain.Tests.Shared;

public class QuantityTests
{
    [Fact]
    public void Of_accepts_non_negative()
    {
        Quantity.Of(0m).Value.ShouldBe(0m);
        Quantity.Of(10.5m).Value.ShouldBe(10.5m);
    }

    [Fact]
    public void Of_rejects_negative()
    {
        Should.Throw<ArgumentException>(() => Quantity.Of(-1m));
    }

    [Fact]
    public void None_is_distinct_from_Zero()
    {
        Quantity.None.ShouldNotBe(Quantity.Of(0m));
        Quantity.None.IsSpecified.ShouldBeFalse();
        Quantity.Of(0m).IsSpecified.ShouldBeTrue();
    }

    [Fact]
    public void Add_propagates_specified()
    {
        (Quantity.Of(2m) + Quantity.Of(3m)).ShouldBe(Quantity.Of(5m));
        (Quantity.Of(2m) + Quantity.None).IsSpecified.ShouldBeFalse();
    }

    [Fact]
    public void Subtract_throws_on_negative_result()
    {
        Should.Throw<ArgumentException>(() => Quantity.Of(3m) - Quantity.Of(5m));
    }
}
```

- [ ] **Step 5.2: Run failing test**

Run: `dotnet test TradyStrat.Domain.Tests --filter "FullyQualifiedName~QuantityTests"`
Expected: build error.

- [ ] **Step 5.3: Implement `Quantity`**

```csharp
namespace TradyStrat.Domain.Shared;

public sealed record Quantity
{
    public decimal Value      { get; }
    public bool    IsSpecified { get; }

    private Quantity(decimal value, bool isSpecified) { Value = value; IsSpecified = isSpecified; }

    public static Quantity Of(decimal value)
    {
        if (value < 0m) throw new ArgumentException($"Quantity must be non-negative: {value}.", nameof(value));
        return new Quantity(value, isSpecified: true);
    }

    public static Quantity Zero => Of(0m);
    public static Quantity None { get; } = new(0m, isSpecified: false);

    public static Quantity operator +(Quantity a, Quantity b)
    {
        if (!a.IsSpecified || !b.IsSpecified) return None;
        return Of(a.Value + b.Value);
    }

    public static Quantity operator -(Quantity a, Quantity b)
    {
        if (!a.IsSpecified || !b.IsSpecified) return None;
        return Of(a.Value - b.Value);
    }

    public override string ToString() => IsSpecified ? Value.ToString() : "None";
}
```

- [ ] **Step 5.4: Run tests passing**

Run: `dotnet test TradyStrat.Domain.Tests --filter "FullyQualifiedName~QuantityTests"`
Expected: all pass.

- [ ] **Step 5.5: Commit**

```bash
git add TradyStrat.Domain/Shared/Quantity.cs TradyStrat.Domain.Tests/Shared/QuantityTests.cs
git commit -m "feat(domain): Quantity VO with None/Zero distinction — Phase 1"
```

---

## Task 6: `Price` VO

**Files:**
- Create: `TradyStrat.Domain/Shared/Price.cs`
- Test:   `TradyStrat.Domain.Tests/Shared/PriceTests.cs`

- [ ] **Step 6.1: Write failing tests**

```csharp
using Shouldly;
using TradyStrat.Domain.Shared;
using Xunit;

namespace TradyStrat.Domain.Tests.Shared;

public class PriceTests
{
    [Fact]
    public void Price_times_Quantity_returns_Money()
    {
        var p = Price.Of(Money.Of(4m, Currency.Eur));
        (p * Quantity.Of(10m)).ShouldBe(Money.Of(40m, Currency.Eur));
    }

    [Fact]
    public void Price_times_None_quantity_returns_None_money()
    {
        var p = Price.Of(Money.Of(4m, Currency.Eur));
        (p * Quantity.None).IsEmpty.ShouldBeTrue();
        (p * Quantity.None).Currency.ShouldBe(Currency.Eur);
    }

    [Fact]
    public void None_price_propagates()
    {
        var p = Price.None(Currency.Eur);
        (p * Quantity.Of(10m)).IsEmpty.ShouldBeTrue();
    }

    [Fact]
    public void Price_minus_Price_returns_Money_delta()
    {
        var hi = Price.Of(Money.Of(7m, Currency.Eur));
        var lo = Price.Of(Money.Of(5m, Currency.Eur));
        (hi - lo).ShouldBe(Money.Of(2m, Currency.Eur));
    }
}
```

- [ ] **Step 6.2: Run failing test**

Run: `dotnet test TradyStrat.Domain.Tests --filter "FullyQualifiedName~PriceTests"`
Expected: build error.

- [ ] **Step 6.3: Implement `Price`**

```csharp
namespace TradyStrat.Domain.Shared;

public sealed record Price
{
    public Money    PerUnit  { get; }
    public Currency Currency => PerUnit.Currency;
    public bool     IsEmpty  => PerUnit.IsEmpty;

    private Price(Money perUnit) => PerUnit = perUnit;

    public static Price Of(Money perUnit) => new(perUnit);
    public static Price None(Currency currency) => new(Money.None(currency));
    public static Price Zero(Currency currency) => new(Money.Zero(currency));

    public static Money operator *(Price p, Quantity q)
    {
        if (p.IsEmpty || !q.IsSpecified)
            return Money.None(p.Currency);
        return p.PerUnit * q.Value;
    }

    public static Money operator -(Price a, Price b)
    {
        if (a.IsEmpty || b.IsEmpty)
            return Money.None(a.Currency);
        return a.PerUnit - b.PerUnit;
    }

    public override string ToString() => IsEmpty ? $"None({Currency})" : PerUnit.ToString();
}
```

- [ ] **Step 6.4: Run tests passing**

Run: `dotnet test TradyStrat.Domain.Tests --filter "FullyQualifiedName~PriceTests"`
Expected: all pass.

- [ ] **Step 6.5: Commit**

```bash
git add TradyStrat.Domain/Shared/Price.cs TradyStrat.Domain.Tests/Shared/PriceTests.cs
git commit -m "feat(domain): Price VO — Phase 1"
```

---

## Task 7: `DateRange` VO

**Files:**
- Create: `TradyStrat.Domain/Shared/DateRange.cs`
- Test:   `TradyStrat.Domain.Tests/Shared/DateRangeTests.cs`

- [ ] **Step 7.1: Write failing tests**

```csharp
using Shouldly;
using TradyStrat.Domain.Shared;
using Xunit;

namespace TradyStrat.Domain.Tests.Shared;

public class DateRangeTests
{
    [Fact]
    public void From_must_be_le_To()
    {
        Should.Throw<ArgumentException>(() => new DateRange(
            new DateOnly(2026, 5, 10), new DateOnly(2026, 5, 1)));
    }

    [Fact]
    public void Single_day_range_is_valid()
    {
        var d = new DateOnly(2026, 5, 22);
        var r = new DateRange(d, d);
        r.Days.ShouldBe([d]);
    }

    [Fact]
    public void Days_enumerates_inclusive()
    {
        var r = new DateRange(new DateOnly(2026, 5, 20), new DateOnly(2026, 5, 22));
        r.Days.ShouldBe([
            new DateOnly(2026, 5, 20),
            new DateOnly(2026, 5, 21),
            new DateOnly(2026, 5, 22),
        ]);
    }

    [Fact]
    public void Contains_checks_inclusive_bounds()
    {
        var r = new DateRange(new DateOnly(2026, 5, 20), new DateOnly(2026, 5, 22));
        r.Contains(new DateOnly(2026, 5, 20)).ShouldBeTrue();
        r.Contains(new DateOnly(2026, 5, 22)).ShouldBeTrue();
        r.Contains(new DateOnly(2026, 5, 19)).ShouldBeFalse();
    }
}
```

- [ ] **Step 7.2: Run failing test**

Run: `dotnet test TradyStrat.Domain.Tests --filter "FullyQualifiedName~DateRangeTests"`
Expected: build error.

- [ ] **Step 7.3: Implement `DateRange`**

```csharp
namespace TradyStrat.Domain.Shared;

public sealed record DateRange
{
    public DateOnly From { get; }
    public DateOnly To   { get; }

    public DateRange(DateOnly from, DateOnly to)
    {
        if (from > to)
            throw new ArgumentException($"DateRange From ({from}) must be ≤ To ({to}).");
        From = from;
        To   = to;
    }

    public IEnumerable<DateOnly> Days
    {
        get
        {
            for (var d = From; d <= To; d = d.AddDays(1))
                yield return d;
        }
    }

    public bool Contains(DateOnly d) => d >= From && d <= To;

    public override string ToString() => $"{From}..{To}";
}
```

- [ ] **Step 7.4: Run tests passing**

Run: `dotnet test TradyStrat.Domain.Tests --filter "FullyQualifiedName~DateRangeTests"`
Expected: all pass.

- [ ] **Step 7.5: Commit**

```bash
git add TradyStrat.Domain/Shared/DateRange.cs TradyStrat.Domain.Tests/Shared/DateRangeTests.cs
git commit -m "feat(domain): DateRange VO — Phase 1"
```

---

## Task 8: Strongly typed IDs

**Files:**
- Create: `TradyStrat.Domain/Shared/InstrumentId.cs`
- Create: `TradyStrat.Domain/Shared/TradeId.cs`
- Create: `TradyStrat.Domain/Shared/SuggestionId.cs`
- Create: `TradyStrat.Domain/Shared/GoalId.cs`
- Create: `TradyStrat.Domain/Shared/PositionId.cs`
- Create: `TradyStrat.Domain/Shared/PortfolioId.cs`
- Test:   `TradyStrat.Domain.Tests/Shared/StronglyTypedIdsTests.cs`

Note: also include `PortfolioId` (the spec uses `PortfolioId.Singleton`). Six IDs total.

- [ ] **Step 8.1: Write failing tests**

```csharp
using Shouldly;
using TradyStrat.Domain.Shared;
using Xunit;

namespace TradyStrat.Domain.Tests.Shared;

public class StronglyTypedIdsTests
{
    [Fact]
    public void New_returns_zero_sentinel()
    {
        InstrumentId.New().Value.ShouldBe(0);
        TradeId.New().Value.ShouldBe(0);
        PositionId.New().Value.ShouldBe(0);
    }

    [Fact]
    public void Singleton_ids_have_expected_value()
    {
        PortfolioId.Singleton.Value.ShouldBe(1);
        GoalId.Singleton.Value.ShouldBe(1);
    }

    [Fact]
    public void Distinct_id_types_do_not_unify()
    {
        // Compile-time check: these types are intentionally NOT interchangeable.
        var i = new InstrumentId(7);
        var t = new TradeId(7);
        // No implicit conversion between them; both have the same int value
        // but reference different domain concepts. Equality across types is
        // not possible.
        i.Value.ShouldBe(t.Value);
    }

    [Fact]
    public void Equality_is_structural()
    {
        new InstrumentId(5).ShouldBe(new InstrumentId(5));
        new InstrumentId(5).ShouldNotBe(new InstrumentId(6));
    }
}
```

- [ ] **Step 8.2: Run failing test**

Run: `dotnet test TradyStrat.Domain.Tests --filter "FullyQualifiedName~StronglyTypedIdsTests"`
Expected: build error.

- [ ] **Step 8.3: Implement six ID structs**

Each one is the same shape. Create six files, one each.

`TradyStrat.Domain/Shared/InstrumentId.cs`:
```csharp
namespace TradyStrat.Domain.Shared;

public readonly record struct InstrumentId(int Value)
{
    public static InstrumentId New() => new(0);
    public override string ToString() => $"InstrumentId({Value})";
}
```

`TradyStrat.Domain/Shared/TradeId.cs`:
```csharp
namespace TradyStrat.Domain.Shared;

public readonly record struct TradeId(int Value)
{
    public static TradeId New() => new(0);
    public override string ToString() => $"TradeId({Value})";
}
```

`TradyStrat.Domain/Shared/SuggestionId.cs`:
```csharp
namespace TradyStrat.Domain.Shared;

public readonly record struct SuggestionId(int Value)
{
    public static SuggestionId New() => new(0);
    public override string ToString() => $"SuggestionId({Value})";
}
```

`TradyStrat.Domain/Shared/GoalId.cs`:
```csharp
namespace TradyStrat.Domain.Shared;

public readonly record struct GoalId(int Value)
{
    public static GoalId New()      => new(0);
    public static GoalId Singleton => new(1);
    public override string ToString() => $"GoalId({Value})";
}
```

`TradyStrat.Domain/Shared/PositionId.cs`:
```csharp
namespace TradyStrat.Domain.Shared;

public readonly record struct PositionId(int Value)
{
    public static PositionId New() => new(0);
    public override string ToString() => $"PositionId({Value})";
}
```

`TradyStrat.Domain/Shared/PortfolioId.cs`:
```csharp
namespace TradyStrat.Domain.Shared;

public readonly record struct PortfolioId(int Value)
{
    public static PortfolioId New()       => new(0);
    public static PortfolioId Singleton => new(1);
    public override string ToString() => $"PortfolioId({Value})";
}
```

- [ ] **Step 8.4: Run tests passing**

Run: `dotnet test TradyStrat.Domain.Tests --filter "FullyQualifiedName~StronglyTypedIdsTests"`
Expected: all pass.

- [ ] **Step 8.5: Commit**

```bash
git add TradyStrat.Domain/Shared/InstrumentId.cs \
        TradyStrat.Domain/Shared/TradeId.cs \
        TradyStrat.Domain/Shared/SuggestionId.cs \
        TradyStrat.Domain/Shared/GoalId.cs \
        TradyStrat.Domain/Shared/PositionId.cs \
        TradyStrat.Domain/Shared/PortfolioId.cs \
        TradyStrat.Domain.Tests/Shared/StronglyTypedIdsTests.cs
git commit -m "feat(domain): strongly typed IDs (Instrument/Trade/Suggestion/Goal/Position/Portfolio) — Phase 1"
```

---

## Task 9: EF `ValueConverter` conventions (dormant)

**Files:**
- Create: `TradyStrat.Infrastructure/Data/Conventions/StronglyTypedIdConventions.cs`
- Modify: `TradyStrat.Infrastructure/Data/AppDbContext.cs`

Wire converters so Phase 2's entities can be mapped with strongly typed IDs without per-property fluent calls. No tables change; no production type uses these yet.

- [ ] **Step 9.1: Implement the conventions class**

Create `TradyStrat.Infrastructure/Data/Conventions/StronglyTypedIdConventions.cs`:

```csharp
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using TradyStrat.Domain.Shared;

namespace TradyStrat.Infrastructure.Data.Conventions;

internal static class StronglyTypedIdConventions
{
    public static void ApplyTo(ModelConfigurationBuilder builder)
    {
        builder.Properties<InstrumentId>().HaveConversion<InstrumentIdConverter>();
        builder.Properties<TradeId>     ().HaveConversion<TradeIdConverter>();
        builder.Properties<SuggestionId>().HaveConversion<SuggestionIdConverter>();
        builder.Properties<GoalId>      ().HaveConversion<GoalIdConverter>();
        builder.Properties<PositionId>  ().HaveConversion<PositionIdConverter>();
        builder.Properties<PortfolioId> ().HaveConversion<PortfolioIdConverter>();
    }

    private sealed class InstrumentIdConverter : ValueConverter<InstrumentId, int>
    {
        public InstrumentIdConverter() : base(id => id.Value, v => new InstrumentId(v)) { }
    }
    private sealed class TradeIdConverter : ValueConverter<TradeId, int>
    {
        public TradeIdConverter() : base(id => id.Value, v => new TradeId(v)) { }
    }
    private sealed class SuggestionIdConverter : ValueConverter<SuggestionId, int>
    {
        public SuggestionIdConverter() : base(id => id.Value, v => new SuggestionId(v)) { }
    }
    private sealed class GoalIdConverter : ValueConverter<GoalId, int>
    {
        public GoalIdConverter() : base(id => id.Value, v => new GoalId(v)) { }
    }
    private sealed class PositionIdConverter : ValueConverter<PositionId, int>
    {
        public PositionIdConverter() : base(id => id.Value, v => new PositionId(v)) { }
    }
    private sealed class PortfolioIdConverter : ValueConverter<PortfolioId, int>
    {
        public PortfolioIdConverter() : base(id => id.Value, v => new PortfolioId(v)) { }
    }
}
```

- [ ] **Step 9.2: Wire into `AppDbContext`**

Modify `TradyStrat.Infrastructure/Data/AppDbContext.cs` — add a `ConfigureConventions` override after `OnModelCreating`:

```csharp
using Microsoft.EntityFrameworkCore;
using TradyStrat.Domain;
using TradyStrat.Infrastructure.Data.Conventions;

namespace TradyStrat.Infrastructure.Data;

public sealed class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Trade>       Trades       => Set<Trade>();
    public DbSet<PriceBar>    PriceBars    => Set<PriceBar>();
    public DbSet<FxRate>      FxRates      => Set<FxRate>();
    public DbSet<GoalConfig>  Goals        => Set<GoalConfig>();
    public DbSet<Suggestion>  Suggestions  => Set<Suggestion>();
    public DbSet<Instrument>  Instruments  => Set<Instrument>();
    public DbSet<SettingEntry> Settings    => Set<SettingEntry>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
        => modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);

    protected override void ConfigureConventions(ModelConfigurationBuilder builder)
    {
        base.ConfigureConventions(builder);
        StronglyTypedIdConventions.ApplyTo(builder);
    }
}
```

- [ ] **Step 9.3: Verify build passes — conventions are dormant since no entity has these property types yet**

Run: `dotnet build TradyStrat.Infrastructure`
Expected: succeeds.

- [ ] **Step 9.4: Verify the migration snapshot did NOT change**

Run: `git diff TradyStrat.Infrastructure/Data/Migrations/AppDbContextModelSnapshot.cs`
Expected: empty diff. The conventions match no current properties so the model is unchanged.

If the snapshot changed: something else accidentally added a strong-typed ID property. Investigate before continuing.

- [ ] **Step 9.5: Commit**

```bash
git add TradyStrat.Infrastructure/Data/Conventions/StronglyTypedIdConventions.cs \
        TradyStrat.Infrastructure/Data/AppDbContext.cs
git commit -m "feat(infra): register ValueConverters for strongly typed IDs (dormant) — Phase 1"
```

---

## Task 10: Phase 1 verification

- [ ] **Step 10.1: Full build**

Run: `dotnet build TradyStrat.slnx`
Expected: succeeds, no warnings.

- [ ] **Step 10.2: Full test pass**

Run: `dotnet test TradyStrat.slnx --no-build`
Expected: all tests pass; the Domain test count grew by the new kernel tests, every other test count is identical to baseline.

- [ ] **Step 10.3: Verify no production code consumes the new types yet**

Run: `grep -rE "TradyStrat\.Domain\.Shared" TradyStrat.Application TradyStrat.Infrastructure TradyStrat TradyStrat.Cli --include='*.cs' | grep -v 'StronglyTypedIdConventions'`
Expected: empty output (only the conventions class references the namespace; no application/infrastructure consumer wires in yet).

- [ ] **Step 10.4: Phase 1 checkpoint tag**

```bash
git tag -a phase1-kernel-seed-done -m "Phase 1 (kernel seed) complete: VOs + strongly typed IDs + dormant EF conventions"
```

Phase 1 complete. Solution builds, tests pass, kernel sits dormant until Phase 2 consumers wire in.

---

# Phase 2 — Portfolio aggregate

Goal: `Portfolio` AR replaces `PortfolioService.BuildSnapshot`. FIFO accounting and realized P&L move into `Position.Record`. `IPortfolioRepository` replaces `IRepositoryBase<Trade>` for AR access. `PortfolioService.cs` and `GrowthSeriesBuilder.cs` are deleted. Cross-aggregate data (instrument metadata, goal target) flows in via `Portfolio.Snapshot` parameters resolved by use cases. Schema migration adds `Portfolios` + `Positions` tables and `Trades.PositionId`.

**Regression contract:** the test files inventoried in Task 11 are the byte-for-byte parity contract. Every `[Fact]` in them must pass against `Portfolio.Snapshot` after migration to `TradyStrat.Domain.Tests/Portfolio/`.

## Task 11: Pin the regression contract

**Files (read-only inventory):**
- `TradyStrat.Infrastructure.Tests/Portfolio/PortfolioServiceTests.cs` (108 lines)
- `TradyStrat.Infrastructure.Tests/Portfolio/PortfolioServiceAsOfTests.cs` (38 lines)
- `TradyStrat.Infrastructure.Tests/Portfolio/PortfolioServiceMultiTickerTests.cs` (63 lines)

- [ ] **Step 11.1: Read each file end-to-end and inventory the `[Fact]`/`[Theory]` names**

Run: `grep -E '\[Fact\]|\[Theory\]|public.*Async\(' TradyStrat.Infrastructure.Tests/Portfolio/PortfolioService*Tests.cs`

Record (in your scratchpad, not the plan) the complete list of test names. These names become the migration checklist in Task 34.

- [ ] **Step 11.2: Run the regression suite once on baseline to confirm green**

Run: `dotnet test TradyStrat.Infrastructure.Tests --filter "FullyQualifiedName~PortfolioService"`
Expected: every test in those three files passes. Record the count.

No commit on this task — it's pure inventory.

---

## Task 12: Return-record types and `TradeDraft` input

**Files:**
- Create: `TradyStrat.Domain/Portfolio/TradeRecorded.cs`
- Create: `TradyStrat.Domain/Portfolio/TradeDeleted.cs`
- Create: `TradyStrat.Domain/Portfolio/TradeDraft.cs`

These are simple records used as parameter and return shapes for `Portfolio` behavior methods.

- [ ] **Step 12.1: Write `TradeRecorded`**

`TradyStrat.Domain/Portfolio/TradeRecorded.cs`:

```csharp
using TradyStrat.Domain.Shared;

namespace TradyStrat.Domain.Portfolio;

public sealed record TradeRecorded(
    TradeId    TradeId,
    PositionId PositionId,
    bool       CreatedPosition,
    Money      RealizedDelta);
```

- [ ] **Step 12.2: Write `TradeDeleted`**

`TradyStrat.Domain/Portfolio/TradeDeleted.cs`:

```csharp
using TradyStrat.Domain.Shared;

namespace TradyStrat.Domain.Portfolio;

public sealed record TradeDeleted(
    PositionId PositionId,
    Money      RealizedDelta);
```

- [ ] **Step 12.3: Write `TradeDraft`**

`TradyStrat.Domain/Portfolio/TradeDraft.cs`:

```csharp
using TradyStrat.Domain.Shared;

namespace TradyStrat.Domain.Portfolio;

public sealed record TradeDraft(
    InstrumentId InstrumentId,
    DateOnly     ExecutedOn,
    TradeSide    Side,
    Quantity     Quantity,
    Price        PricePerShare,
    Money        Fees,
    string       Note);
```

(`TradeSide` already exists today in `TradyStrat.Domain/Trades/TradeSide.cs` — unchanged.)

- [ ] **Step 12.4: Verify build**

Run: `dotnet build TradyStrat.Domain`
Expected: succeeds.

- [ ] **Step 12.5: Commit**

```bash
git add TradyStrat.Domain/Portfolio/TradeRecorded.cs \
        TradyStrat.Domain/Portfolio/TradeDeleted.cs \
        TradyStrat.Domain/Portfolio/TradeDraft.cs
git commit -m "feat(domain): Portfolio behavior return records + TradeDraft input — Phase 2"
```

---

## Task 13: Rewrite `Lot` as child entity

**Files:**
- Modify: `TradyStrat.Domain/Trades/Lot.cs` (today: `record Lot(DateOnly OpenedOn, decimal Quantity, decimal UnitCostEur)`)
- Test:   `TradyStrat.Domain.Tests/Portfolio/LotTests.cs`

`Lot` becomes a child entity owned by `Position`. Adopts `Quantity` and `Money`. Stays a record because it has no identity beyond its place in the FIFO queue, but adopts the no-null + VO conventions.

- [ ] **Step 13.1: Write failing tests**

`TradyStrat.Domain.Tests/Portfolio/LotTests.cs`:

```csharp
using Shouldly;
using TradyStrat.Domain.Portfolio;
using TradyStrat.Domain.Shared;
using Xunit;

namespace TradyStrat.Domain.Tests.Portfolio;

public class LotTests
{
    [Fact]
    public void CostBasis_is_quantity_times_unit_cost()
    {
        var lot = new Lot(
            OpenedOn: new DateOnly(2026, 1, 1),
            Quantity: Quantity.Of(10m),
            UnitCost: Money.Of(4.20m, Currency.Eur));

        lot.CostBasis.ShouldBe(Money.Of(42.00m, Currency.Eur));
    }

    [Fact]
    public void WithQuantity_returns_new_lot_with_updated_quantity()
    {
        var lot = new Lot(
            OpenedOn: new DateOnly(2026, 1, 1),
            Quantity: Quantity.Of(10m),
            UnitCost: Money.Of(4m, Currency.Eur));

        var trimmed = lot.WithQuantity(Quantity.Of(7m));

        trimmed.Quantity.ShouldBe(Quantity.Of(7m));
        trimmed.UnitCost.ShouldBe(Money.Of(4m, Currency.Eur));
        trimmed.OpenedOn.ShouldBe(lot.OpenedOn);
    }
}
```

- [ ] **Step 13.2: Run failing test**

Run: `dotnet test TradyStrat.Domain.Tests --filter "FullyQualifiedName~LotTests"`
Expected: build error (`Lot` constructor mismatch).

- [ ] **Step 13.3: Replace `Lot` implementation**

Replace contents of `TradyStrat.Domain/Trades/Lot.cs` with:

```csharp
using TradyStrat.Domain.Shared;

namespace TradyStrat.Domain.Portfolio;

public sealed record Lot(DateOnly OpenedOn, Quantity Quantity, Money UnitCost)
{
    public Money CostBasis => UnitCost * Quantity.Value;

    public Lot WithQuantity(Quantity newQuantity) => this with { Quantity = newQuantity };
}
```

Note the namespace change: `TradyStrat.Domain.Trades` → `TradyStrat.Domain.Portfolio` (Lot is a Portfolio aggregate concept now). Move the file:

```bash
git mv TradyStrat.Domain/Trades/Lot.cs TradyStrat.Domain/Portfolio/Lot.cs
```

Then apply the content edit to the new path.

- [ ] **Step 13.4: Fix references**

Run: `grep -rn 'TradyStrat\.Domain\.Trades\.Lot\|new Lot(' TradyStrat.Application TradyStrat.Infrastructure TradyStrat TradyStrat.Cli --include='*.cs'`

For each match: update to `TradyStrat.Domain.Portfolio.Lot` and adjust ctor call to use VO types. (The legacy `PortfolioService.BuildSnapshot` uses `Lot` heavily — that file is being deleted in Task 33, but for now keep it compiling. If it won't compile cleanly with the new VO-based `Lot`, temporarily restore the old shape — alternative: keep both `Lot` types side-by-side under different namespaces during the transition.)

**Simpler approach (recommended):** leave the *current* `Lot` type at `TradyStrat.Domain/Trades/Lot.cs` untouched for now, and create the new `Lot` at `TradyStrat.Domain/Portfolio/Lot.cs` as a *distinct* type. Mark the old one `[Obsolete("Replaced by TradyStrat.Domain.Portfolio.Lot in Phase 2 — remove with PortfolioService.")]`. Delete the old file in Task 33.

If you take the simpler approach, the content above goes to `TradyStrat.Domain/Portfolio/Lot.cs` (new file, not moved).

- [ ] **Step 13.5: Run test passing**

Run: `dotnet test TradyStrat.Domain.Tests --filter "FullyQualifiedName~LotTests"`
Expected: pass.

- [ ] **Step 13.6: Verify build**

Run: `dotnet build TradyStrat.slnx`
Expected: succeeds (legacy `PortfolioService` may emit obsolete warnings — acceptable; will be deleted in Task 33).

- [ ] **Step 13.7: Commit**

```bash
git add TradyStrat.Domain/Portfolio/Lot.cs TradyStrat.Domain.Tests/Portfolio/LotTests.cs
git add TradyStrat.Domain/Trades/Lot.cs   # if the old file got marked obsolete
git commit -m "feat(domain): Lot becomes Portfolio child entity with Quantity+Money — Phase 2"
```

---

## Task 14: Rewrite `Trade` as child entity

**Files:**
- Modify: `TradyStrat.Domain/Trades/Trade.cs` (today: anemic record with int Id + raw decimals + nullable Note)
- Test:   `TradyStrat.Domain.Tests/Portfolio/TradeTests.cs`

`Trade` becomes a child entity of `Position`, owned by `Portfolio`. Class, not record. Private parameterless ctor for EF. `Create` factory enforces invariants. VO-typed fields. `Note` becomes non-nullable empty string.

- [ ] **Step 14.1: Write failing tests**

`TradyStrat.Domain.Tests/Portfolio/TradeTests.cs`:

```csharp
using Shouldly;
using TradyStrat.Domain;
using TradyStrat.Domain.Exceptions;
using TradyStrat.Domain.Portfolio;
using TradyStrat.Domain.Shared;
using Xunit;

namespace TradyStrat.Domain.Tests.Portfolio;

public class TradeTests
{
    private static Trade Buy(decimal qty, decimal price, decimal fees = 0m) =>
        Trade.Create(
            executedOn: new DateOnly(2026, 1, 1),
            side: TradeSide.Buy,
            quantity: Quantity.Of(qty),
            pricePerShare: Price.Of(Money.Of(price, Currency.Eur)),
            fees: Money.Of(fees, Currency.Eur),
            note: "",
            now: new DateTime(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc));

    [Fact]
    public void Create_assigns_zero_id_sentinel()
    {
        var t = Buy(10m, 4m);
        t.Id.ShouldBe(TradeId.New());
    }

    [Fact]
    public void Create_rejects_zero_quantity()
    {
        Should.Throw<TradeValidationException>(() =>
            Trade.Create(
                executedOn: new DateOnly(2026, 1, 1),
                side: TradeSide.Buy,
                quantity: Quantity.Of(0m),
                pricePerShare: Price.Of(Money.Of(4m, Currency.Eur)),
                fees: Money.Zero(Currency.Eur),
                note: "",
                now: DateTime.UtcNow));
    }

    [Fact]
    public void Create_rejects_unspecified_quantity()
    {
        Should.Throw<TradeValidationException>(() =>
            Trade.Create(
                executedOn: new DateOnly(2026, 1, 1),
                side: TradeSide.Buy,
                quantity: Quantity.None,
                pricePerShare: Price.Of(Money.Of(4m, Currency.Eur)),
                fees: Money.Zero(Currency.Eur),
                note: "",
                now: DateTime.UtcNow));
    }

    [Fact]
    public void Create_rejects_empty_price()
    {
        Should.Throw<TradeValidationException>(() =>
            Trade.Create(
                executedOn: new DateOnly(2026, 1, 1),
                side: TradeSide.Buy,
                quantity: Quantity.Of(10m),
                pricePerShare: Price.None(Currency.Eur),
                fees: Money.Zero(Currency.Eur),
                note: "",
                now: DateTime.UtcNow));
    }

    [Fact]
    public void IsBuy_reflects_side()
    {
        Buy(10m, 4m).IsBuy.ShouldBeTrue();
        var sell = Trade.Create(
            executedOn: new DateOnly(2026, 1, 1),
            side: TradeSide.Sell,
            quantity: Quantity.Of(10m),
            pricePerShare: Price.Of(Money.Of(4m, Currency.Eur)),
            fees: Money.Zero(Currency.Eur),
            note: "",
            now: DateTime.UtcNow);
        sell.IsBuy.ShouldBeFalse();
    }
}
```

- [ ] **Step 14.2: Run failing test**

Run: `dotnet test TradyStrat.Domain.Tests --filter "FullyQualifiedName~TradeTests"`
Expected: build error.

- [ ] **Step 14.3: Replace `Trade` content**

Replace contents of `TradyStrat.Domain/Trades/Trade.cs` with the new class shape. Move the file to `TradyStrat.Domain/Portfolio/Trade.cs`:

```bash
git mv TradyStrat.Domain/Trades/Trade.cs TradyStrat.Domain/Portfolio/Trade.cs
```

Content for `TradyStrat.Domain/Portfolio/Trade.cs`:

```csharp
using TradyStrat.Domain.Exceptions;
using TradyStrat.Domain.Shared;

namespace TradyStrat.Domain.Portfolio;

public sealed class Trade
{
    public TradeId   Id           { get; private set; }
    public DateOnly  ExecutedOn   { get; private set; }
    public TradeSide Side         { get; private set; }
    public Quantity  Quantity     { get; private set; } = Quantity.None;
    public Price     PricePerShare { get; private set; } = Price.None(Currency.Eur);
    public Money     Fees         { get; private set; } = Money.Zero(Currency.Eur);
    public string    Note         { get; private set; } = "";
    public DateTime  CreatedAt    { get; private set; }

    private Trade() { }   // EF

    private Trade(
        DateOnly executedOn, TradeSide side, Quantity quantity,
        Price pricePerShare, Money fees, string note, DateTime now)
    {
        Id            = TradeId.New();
        ExecutedOn    = executedOn;
        Side          = side;
        Quantity      = quantity;
        PricePerShare = pricePerShare;
        Fees          = fees;
        Note          = note;
        CreatedAt     = now;
    }

    public static Trade Create(
        DateOnly executedOn, TradeSide side, Quantity quantity,
        Price pricePerShare, Money fees, string note, DateTime now)
    {
        if (!quantity.IsSpecified || quantity.Value <= 0m)
            throw new TradeValidationException("Quantity must be positive.");
        if (pricePerShare.IsEmpty || pricePerShare.PerUnit.Amount <= 0m)
            throw new TradeValidationException("Price per share must be positive.");
        if (fees.IsEmpty || fees.Amount < 0m)
            throw new TradeValidationException("Fees cannot be negative.");

        return new Trade(executedOn, side, quantity, pricePerShare, fees, note ?? "", now);
    }

    public bool IsBuy => Side == TradeSide.Buy;

    public Money Gross => PricePerShare * Quantity;
    public Money Net   => Side == TradeSide.Buy ? Gross + Fees : Gross - Fees;
}
```

Update the namespace of `TradeSide` while you're at it — move `TradyStrat.Domain/Trades/TradeSide.cs` to `TradyStrat.Domain/Portfolio/TradeSide.cs`:

```bash
git mv TradyStrat.Domain/Trades/TradeSide.cs TradyStrat.Domain/Portfolio/TradeSide.cs
```

Adjust namespace declaration inside that file from `TradyStrat.Domain` (or wherever it is) to `TradyStrat.Domain.Portfolio`.

- [ ] **Step 14.4: Update consumers compile-time**

Other production code today references `Trade` directly (e.g. `LogTradeUseCase`, `ImportTradesCsvUseCase`, `PortfolioService`). They'll break. Strategy: **add a temporary `using TradyStrat.Domain.Portfolio;` to each file that broke** so it picks up the new namespace; *do not* fix the property-access errors yet — those use cases are rewritten in Tasks 28-30. To make the solution compile in the meantime, **suppress** those use case files: rename `LogTradeUseCase.cs`, `DeleteTradeUseCase.cs`, `ImportTradesCsvUseCase.cs` to `.cs.bak` (and same for `PortfolioService.cs`, `GrowthSeriesBuilder.cs`):

```bash
mv TradyStrat.Application/Trades/UseCases/LogTradeUseCase.cs{,.bak}
mv TradyStrat.Application/Trades/UseCases/DeleteTradeUseCase.cs{,.bak}
mv TradyStrat.Application/Trades/UseCases/ImportTradesCsvUseCase.cs{,.bak}
mv TradyStrat.Application/Portfolio/PortfolioService.cs{,.bak}
mv TradyStrat.Application/Portfolio/GrowthSeriesBuilder.cs{,.bak}
```

These `.bak` files are not compiled. They're kept on disk as a reference for the FIFO/import logic Tasks 17, 28-30, 33 need to port. They'll be deleted at the end of Task 33.

Also temporarily disable the test files that reference `PortfolioService` directly:

```bash
mv TradyStrat.Infrastructure.Tests/Portfolio/PortfolioServiceTests.cs{,.bak}
mv TradyStrat.Infrastructure.Tests/Portfolio/PortfolioServiceAsOfTests.cs{,.bak}
mv TradyStrat.Infrastructure.Tests/Portfolio/PortfolioServiceMultiTickerTests.cs{,.bak}
```

(Migrated to `Domain.Tests/Portfolio/` in Task 34.)

Also remove the `PortfolioService`/`GrowthSeriesBuilder` registrations in `TradyStrat.Application/Portfolio/PortfolioApplicationModule.cs`. Replace with an empty module body until Task 22 wires `IPortfolioRepository`:

```csharp
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TheAppManager.Modules;

namespace TradyStrat.Application.Portfolio;

public sealed class PortfolioApplicationModule : IAppModule
{
    public void ConfigureServices(IServiceCollection services, IConfiguration config)
    {
        // Empty until IPortfolioRepository wiring lands in Task 22.
    }
}
```

In `LoadDashboardUseCase.cs`, `BuildFocusDerivedSliceUseCase.cs`, `AiSnapshotService` `PortfolioSection.cs`, `CsvImportServiceTests.cs`, and any other consumer of `PortfolioService` / `GrowthSeriesBuilder` / direct `IRepositoryBase<Trade>` — these break too. Rather than chase them all now, do the same `.bak` trick on the *containing* files and bring them back online in Tasks 28-32. The point of this checkpoint: get the Domain green so the kernel + Trade rewrite ship together, then fix Application/Infrastructure consumers incrementally.

Concretely, run this find-and-bak:

```bash
for f in \
  TradyStrat.Application/Dashboard/UseCases/LoadDashboardUseCase.cs \
  TradyStrat.Application/Dashboard/UseCases/BuildFocusDerivedSliceUseCase.cs \
  TradyStrat.Application/AiSuggestion/Snapshot/Sections/PortfolioSection.cs \
  TradyStrat.Application.Tests/Trades/CsvImportServiceTests.cs ; do
  test -f "$f" && mv "$f" "$f.bak"
done
```

Also remove the page's wiring (in the Blazor `DashboardPage`) — but `LoadDashboardUseCase.cs.bak` triggers a missing-type at the page injection point. Do the same to the Razor page's `.razor.cs` if it directly references `LoadDashboardUseCase` — find that:

Run: `grep -rln 'LoadDashboardUseCase\|BuildFocusDerivedSliceUseCase' TradyStrat --include='*.cs' --include='*.razor*'`

If matches exist outside the `.bak` files, also `.bak` them. The plan accepts that the Blazor UI will not run during Phase 2 task execution; only the test suite needs to pass at each checkpoint. The smoke test at the end of Task 37 restores all `.bak` files via their replacement implementations.

- [ ] **Step 14.5: Build the Domain test project**

Run: `dotnet build TradyStrat.Domain.Tests`
Expected: succeeds. (Some other projects may still be broken — that's OK during Phase 2; the Phase 2 verification gates at Tasks 17, 21, 27, 37 progressively re-green everything.)

- [ ] **Step 14.6: Run TradeTests**

Run: `dotnet test TradyStrat.Domain.Tests --filter "FullyQualifiedName~TradeTests"`
Expected: all pass.

- [ ] **Step 14.7: Commit**

```bash
git add -A
git commit -m "feat(domain): Trade becomes Portfolio child entity (TradeId, VO fields, factory, private setters) — Phase 2"
```

---

## Task 15: `Position` child entity skeleton

**Files:**
- Create: `TradyStrat.Domain/Portfolio/Position.cs`
- Test:   `TradyStrat.Domain.Tests/Portfolio/PositionTests.cs`

`Position` is owned by `Portfolio`. Class, not record. Identity by `PositionId`. Holds `InstrumentId`, FIFO lot queue, realized P&L, full trade history. **No Ticker or Currency snapshot** (spec §7).

This task builds the skeleton (constructor, identity, getters); the `Record` method comes in Task 16.

- [ ] **Step 15.1: Write failing tests for skeleton**

`TradyStrat.Domain.Tests/Portfolio/PositionTests.cs`:

```csharp
using Shouldly;
using TradyStrat.Domain.Portfolio;
using TradyStrat.Domain.Shared;
using Xunit;

namespace TradyStrat.Domain.Tests.Portfolio;

public class PositionTests
{
    [Fact]
    public void New_position_for_instrument_is_empty()
    {
        var p = Position.OpenFor(new InstrumentId(7));

        p.InstrumentId.ShouldBe(new InstrumentId(7));
        p.OpenLots.ShouldBeEmpty();
        p.Trades.ShouldBeEmpty();
        p.TotalQuantity.ShouldBe(Quantity.Zero);
        p.CostBasis.ShouldBe(Money.Zero(Currency.Eur));
        p.RealizedPnL.ShouldBe(Money.Zero(Currency.Eur));
    }

    [Fact]
    public void Id_starts_at_sentinel_until_persisted()
    {
        Position.OpenFor(new InstrumentId(1)).Id.ShouldBe(PositionId.New());
    }
}
```

- [ ] **Step 15.2: Run failing test**

Run: `dotnet test TradyStrat.Domain.Tests --filter "FullyQualifiedName~PositionTests"`
Expected: build error.

- [ ] **Step 15.3: Implement `Position` skeleton**

`TradyStrat.Domain/Portfolio/Position.cs`:

```csharp
using TradyStrat.Domain.Shared;

namespace TradyStrat.Domain.Portfolio;

public sealed class Position
{
    public PositionId   Id           { get; private set; }
    public InstrumentId InstrumentId { get; private set; }

    private readonly List<Lot>   _openLots = new();
    private readonly List<Trade> _trades   = new();

    private Money _realizedPnL = Money.Zero(Currency.Eur);

    public IReadOnlyList<Lot>   OpenLots   => _openLots;
    public IReadOnlyList<Trade> Trades     => _trades;
    public Money                RealizedPnL => _realizedPnL;

    public Quantity TotalQuantity
    {
        get
        {
            var sum = Quantity.Zero;
            foreach (var lot in _openLots)
                sum += lot.Quantity;
            return sum;
        }
    }

    public Money CostBasis
    {
        get
        {
            var sum = Money.Zero(Currency.Eur);
            foreach (var lot in _openLots)
                sum += lot.CostBasis;
            return sum;
        }
    }

    private Position() { }   // EF

    private Position(InstrumentId instrumentId)
    {
        Id           = PositionId.New();
        InstrumentId = instrumentId;
    }

    public static Position OpenFor(InstrumentId instrumentId) => new(instrumentId);
}
```

- [ ] **Step 15.4: Run test passing**

Run: `dotnet test TradyStrat.Domain.Tests --filter "FullyQualifiedName~PositionTests"`
Expected: pass.

- [ ] **Step 15.5: Commit**

```bash
git add TradyStrat.Domain/Portfolio/Position.cs TradyStrat.Domain.Tests/Portfolio/PositionTests.cs
git commit -m "feat(domain): Position child entity skeleton — Phase 2"
```

---

## Task 16: `Position.Record` — FIFO accounting

**Files:**
- Modify: `TradyStrat.Domain/Portfolio/Position.cs`
- Modify: `TradyStrat.Domain.Tests/Portfolio/PositionTests.cs`

Port the inner loop from the legacy `PortfolioService.BuildSnapshot` (`.cs.bak` reference) into `Position.Record(Trade)`. Same FIFO semantics, same fee-folding-into-cost-basis on buys, same pro-rata fee allocation on sells, same oversell error.

- [ ] **Step 16.1: Write failing tests for FIFO behavior**

Append to `TradyStrat.Domain.Tests/Portfolio/PositionTests.cs`:

```csharp
    private static Trade Buy(int day, decimal qty, decimal price, decimal fees = 0m) =>
        Trade.Create(
            executedOn: new DateOnly(2026, 1, day),
            side: TradeSide.Buy,
            quantity: Quantity.Of(qty),
            pricePerShare: Price.Of(Money.Of(price, Currency.Eur)),
            fees: Money.Of(fees, Currency.Eur),
            note: "",
            now: new DateTime(2026, 1, day, 12, 0, 0, DateTimeKind.Utc));

    private static Trade Sell(int day, decimal qty, decimal price, decimal fees = 0m) =>
        Trade.Create(
            executedOn: new DateOnly(2026, 1, day),
            side: TradeSide.Sell,
            quantity: Quantity.Of(qty),
            pricePerShare: Price.Of(Money.Of(price, Currency.Eur)),
            fees: Money.Of(fees, Currency.Eur),
            note: "",
            now: new DateTime(2026, 1, day, 12, 0, 0, DateTimeKind.Utc));

    [Fact]
    public void Single_buy_folds_fees_into_cost_basis()
    {
        var p = Position.OpenFor(new InstrumentId(1));
        p.Record(Buy(1, qty: 10m, price: 4.00m, fees: 2.00m));

        // unit cost = (10*4 + 2) / 10 = 4.20
        p.TotalQuantity.ShouldBe(Quantity.Of(10m));
        p.CostBasis.ShouldBe(Money.Of(42.00m, Currency.Eur));
        p.RealizedPnL.ShouldBe(Money.Zero(Currency.Eur));
    }

    [Fact]
    public void FIFO_sell_realizes_oldest_lot_first()
    {
        var p = Position.OpenFor(new InstrumentId(1));
        p.Record(Buy(1, 10m, 4.00m));    // lot @ 4.00
        p.Record(Buy(5, 10m, 5.00m));    // lot @ 5.00
        p.Record(Sell(8, 5m, 6.00m));    // sell 5 → realize 5*(6-4) = 10

        p.TotalQuantity.ShouldBe(Quantity.Of(15m));
        p.RealizedPnL.ShouldBe(Money.Of(10m, Currency.Eur));
    }

    [Fact]
    public void Sell_spanning_multiple_lots_realizes_each()
    {
        var p = Position.OpenFor(new InstrumentId(1));
        p.Record(Buy(1, 10m, 4.00m));
        p.Record(Buy(5, 10m, 5.00m));
        // sell 15 @ 7 → realize 10*(7-4) + 5*(7-5) = 30 + 10 = 40
        p.Record(Sell(8, 15m, 7.00m));

        p.TotalQuantity.ShouldBe(Quantity.Of(5m));
        p.RealizedPnL.ShouldBe(Money.Of(40m, Currency.Eur));
    }

    [Fact]
    public void Sell_allocates_fees_pro_rata_across_realization()
    {
        var p = Position.OpenFor(new InstrumentId(1));
        p.Record(Buy(1, 10m, 4.00m));
        // sell 10 @ 6, fees 3 → realize 10*(6-4) - 3 = 17
        p.Record(Sell(8, 10m, 6.00m, fees: 3m));

        p.RealizedPnL.ShouldBe(Money.Of(17m, Currency.Eur));
    }

    [Fact]
    public void Oversell_throws_TradeValidationException()
    {
        var p = Position.OpenFor(new InstrumentId(1));
        p.Record(Buy(1, 10m, 4.00m));
        Should.Throw<TradyStrat.Domain.Exceptions.TradeValidationException>(() =>
            p.Record(Sell(8, 11m, 5.00m)));
    }
```

- [ ] **Step 16.2: Run failing test**

Run: `dotnet test TradyStrat.Domain.Tests --filter "FullyQualifiedName~PositionTests"`
Expected: builds (skeleton has no `Record`); tests fail because `Record` doesn't exist yet — adjust the call sites to compile by stubbing. Actually: build will fail. That's the failing state we want.

- [ ] **Step 16.3: Implement `Position.Record`**

Add to `TradyStrat.Domain/Portfolio/Position.cs` (inside the class):

```csharp
public Money Record(Trade trade)
{
    var realizedBefore = _realizedPnL;
    _trades.Add(trade);

    if (trade.IsBuy)
    {
        // Fold fees into cost basis: unitCost = (gross + fees) / qty
        var grossPlusFees = trade.PricePerShare * trade.Quantity + trade.Fees;
        var unitCost = grossPlusFees / trade.Quantity.Value;
        _openLots.Add(new Lot(trade.ExecutedOn, trade.Quantity, unitCost));
        return Money.Zero(Currency.Eur);
    }

    var remaining = trade.Quantity.Value;
    var totalSellQty = trade.Quantity.Value;
    while (remaining > 0m)
    {
        if (_openLots.Count == 0)
            throw new Exceptions.TradeValidationException(
                $"Sell on {trade.ExecutedOn} for instrument {InstrumentId} exceeds open lots.");

        var head = _openLots[0];
        var consumed = Math.Min(head.Quantity.Value, remaining);

        // P&L from the price delta on consumed shares
        var pricePnL = (trade.PricePerShare.PerUnit - head.UnitCost) * consumed;
        // Pro-rata fee allocation on consumed shares
        var feeShare = trade.Fees * (consumed / totalSellQty);
        _realizedPnL = _realizedPnL + pricePnL - feeShare;

        if (consumed == head.Quantity.Value)
            _openLots.RemoveAt(0);
        else
            _openLots[0] = head.WithQuantity(Quantity.Of(head.Quantity.Value - consumed));

        remaining -= consumed;
    }

    return _realizedPnL - realizedBefore;
}
```

Also add a small helper method (internal — Portfolio uses it for `DeleteTrade` replay):

```csharp
internal void ResetForReplay()
{
    _openLots.Clear();
    _realizedPnL = Money.Zero(Currency.Eur);
    // _trades is preserved; caller replays them in order.
}
```

- [ ] **Step 16.4: Run tests passing**

Run: `dotnet test TradyStrat.Domain.Tests --filter "FullyQualifiedName~PositionTests"`
Expected: every Position test passes including the FIFO scenarios.

- [ ] **Step 16.5: Commit**

```bash
git add TradyStrat.Domain/Portfolio/Position.cs TradyStrat.Domain.Tests/Portfolio/PositionTests.cs
git commit -m "feat(domain): Position.Record with FIFO + fee folding + realized P&L — Phase 2"
```

---

## Task 17: `Portfolio` AR skeleton + `RecordTrade` + `DeleteTrade` + `ImportTrades`

**Files:**
- Create: `TradyStrat.Domain/Portfolio/Portfolio.cs`
- Create: `TradyStrat.Domain.Tests/Portfolio/PortfolioBehaviorTests.cs`

The AR. Owns positions. Finds-or-creates `Position` on `RecordTrade`. `DeleteTrade` triggers per-position replay. `ImportTrades` is atomic (rollback on first failure).

- [ ] **Step 17.1: Write failing tests**

`TradyStrat.Domain.Tests/Portfolio/PortfolioBehaviorTests.cs`:

```csharp
using Shouldly;
using TradyStrat.Domain.Exceptions;
using TradyStrat.Domain.Portfolio;
using TradyStrat.Domain.Shared;
using Xunit;

namespace TradyStrat.Domain.Tests.Portfolio;

public class PortfolioBehaviorTests
{
    private static readonly DateTime _now = new(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void Empty_creates_a_singleton_portfolio()
    {
        var p = Portfolio.Empty(PortfolioId.Singleton);
        p.Id.ShouldBe(PortfolioId.Singleton);
        p.Positions.ShouldBeEmpty();
    }

    [Fact]
    public void RecordTrade_creates_new_position_first_time()
    {
        var portfolio = Portfolio.Empty(PortfolioId.Singleton);
        var result = portfolio.RecordTrade(
            new InstrumentId(7),
            new DateOnly(2026, 1, 1), TradeSide.Buy,
            Quantity.Of(10m), Price.Of(Money.Of(4m, Currency.Eur)),
            Money.Zero(Currency.Eur), "", _now);

        result.CreatedPosition.ShouldBeTrue();
        portfolio.Positions.Count.ShouldBe(1);
        portfolio.Positions[0].InstrumentId.ShouldBe(new InstrumentId(7));
    }

    [Fact]
    public void RecordTrade_reuses_existing_position()
    {
        var portfolio = Portfolio.Empty(PortfolioId.Singleton);
        portfolio.RecordTrade(new InstrumentId(7), new DateOnly(2026, 1, 1), TradeSide.Buy,
            Quantity.Of(10m), Price.Of(Money.Of(4m, Currency.Eur)), Money.Zero(Currency.Eur), "", _now);
        var second = portfolio.RecordTrade(new InstrumentId(7), new DateOnly(2026, 1, 5), TradeSide.Buy,
            Quantity.Of(5m), Price.Of(Money.Of(6m, Currency.Eur)), Money.Zero(Currency.Eur), "", _now);

        second.CreatedPosition.ShouldBeFalse();
        portfolio.Positions.Count.ShouldBe(1);
        portfolio.Positions[0].Trades.Count.ShouldBe(2);
    }

    [Fact]
    public void DeleteTrade_replays_remaining_trades_in_that_position()
    {
        var portfolio = Portfolio.Empty(PortfolioId.Singleton);
        var r1 = portfolio.RecordTrade(new InstrumentId(7), new DateOnly(2026, 1, 1), TradeSide.Buy,
            Quantity.Of(10m), Price.Of(Money.Of(4m, Currency.Eur)), Money.Zero(Currency.Eur), "", _now);
        portfolio.RecordTrade(new InstrumentId(7), new DateOnly(2026, 1, 5), TradeSide.Buy,
            Quantity.Of(10m), Price.Of(Money.Of(5m, Currency.Eur)), Money.Zero(Currency.Eur), "", _now);
        portfolio.RecordTrade(new InstrumentId(7), new DateOnly(2026, 1, 8), TradeSide.Sell,
            Quantity.Of(5m), Price.Of(Money.Of(6m, Currency.Eur)), Money.Zero(Currency.Eur), "", _now);

        // After delete of the first buy, replay: only [Buy@5, Sell@6] of qty 5 each.
        // remaining lot @ 5; sell of 5 @ 6 realizes 5*(6-5)=5.
        portfolio.DeleteTrade(r1.TradeId);

        var pos = portfolio.Positions[0];
        pos.Trades.Count.ShouldBe(2);
        pos.TotalQuantity.ShouldBe(Quantity.Of(5m));
        pos.RealizedPnL.ShouldBe(Money.Of(5m, Currency.Eur));
    }

    [Fact]
    public void DeleteTrade_unknown_id_throws()
    {
        var portfolio = Portfolio.Empty(PortfolioId.Singleton);
        Should.Throw<TradeValidationException>(() =>
            portfolio.DeleteTrade(new TradeId(999)));
    }

    [Fact]
    public void ImportTrades_atomic_rolls_back_on_failure()
    {
        var portfolio = Portfolio.Empty(PortfolioId.Singleton);
        var good = new TradeDraft(
            new InstrumentId(7), new DateOnly(2026, 1, 1), TradeSide.Buy,
            Quantity.Of(10m), Price.Of(Money.Of(4m, Currency.Eur)),
            Money.Zero(Currency.Eur), "");
        // This sell would exceed open lots — should fail mid-batch.
        var bad = new TradeDraft(
            new InstrumentId(7), new DateOnly(2026, 1, 2), TradeSide.Sell,
            Quantity.Of(20m), Price.Of(Money.Of(5m, Currency.Eur)),
            Money.Zero(Currency.Eur), "");

        Should.Throw<TradeValidationException>(() =>
            portfolio.ImportTrades([good, bad], _now));

        // Atomic: nothing applied.
        portfolio.Positions.ShouldBeEmpty();
    }
}
```

- [ ] **Step 17.2: Run failing test**

Run: `dotnet test TradyStrat.Domain.Tests --filter "FullyQualifiedName~PortfolioBehaviorTests"`
Expected: build error (`Portfolio` type not defined).

- [ ] **Step 17.3: Implement `Portfolio` AR**

`TradyStrat.Domain/Portfolio/Portfolio.cs`:

```csharp
using TradyStrat.Domain.Exceptions;
using TradyStrat.Domain.Shared;

namespace TradyStrat.Domain.Portfolio;

public sealed class Portfolio
{
    public PortfolioId Id { get; private set; }

    private readonly List<Position> _positions = new();
    public IReadOnlyList<Position> Positions => _positions;

    private Portfolio() { }   // EF

    private Portfolio(PortfolioId id) { Id = id; }

    public static Portfolio Empty(PortfolioId id) => new(id);

    public TradeRecorded RecordTrade(
        InstrumentId instrumentId,
        DateOnly executedOn, TradeSide side,
        Quantity quantity, Price pricePerShare, Money fees, string note, DateTime now)
    {
        var trade = Trade.Create(executedOn, side, quantity, pricePerShare, fees, note, now);

        var position = _positions.FirstOrDefault(p => p.InstrumentId == instrumentId);
        var created = position is null;
        if (position is null)
        {
            position = Position.OpenFor(instrumentId);
            _positions.Add(position);
        }

        var realizedDelta = position.Record(trade);
        return new TradeRecorded(trade.Id, position.Id, created, realizedDelta);
    }

    public TradeDeleted DeleteTrade(TradeId tradeId)
    {
        Position? target = null;
        Trade? toRemove = null;
        foreach (var p in _positions)
        {
            var match = p.Trades.FirstOrDefault(t => t.Id == tradeId);
            if (match is not null) { target = p; toRemove = match; break; }
        }
        if (target is null || toRemove is null)
            throw new TradeValidationException($"Trade {tradeId} not found.");

        var realizedBefore = target.RealizedPnL;

        // Capture remaining trades (immutable enumeration), reset, replay.
        var remaining = target.Trades.Where(t => t.Id != tradeId)
                                     .OrderBy(t => t.ExecutedOn)
                                     .ToList();
        target.ResetForReplay();
        // Clear trade history too (private field access via the public Trades is read-only,
        // so the position's replay helper must also drain _trades). Update ResetForReplay
        // to clear _trades as well — see Task 16.5 note. For now do it explicitly:
        var clearAccessor = target.GetType()
            .GetField("_trades", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        ((List<Trade>)clearAccessor!.GetValue(target)!).Clear();

        foreach (var t in remaining) target.Record(t);

        var realizedDelta = target.RealizedPnL - realizedBefore;
        return new TradeDeleted(target.Id, realizedDelta);
    }

    public IReadOnlyList<TradeRecorded> ImportTrades(
        IReadOnlyList<TradeDraft> drafts, DateTime now)
    {
        // Snapshot current state for rollback.
        var savedPositions = _positions.Select(p => p).ToList();
        var savedLots      = _positions.ToDictionary(p => p, p => p.OpenLots.ToList());
        var savedTrades    = _positions.ToDictionary(p => p, p => p.Trades.ToList());
        var savedRealized  = _positions.ToDictionary(p => p, p => p.RealizedPnL);

        var results = new List<TradeRecorded>(drafts.Count);
        try
        {
            foreach (var d in drafts)
            {
                var r = RecordTrade(
                    d.InstrumentId, d.ExecutedOn, d.Side,
                    d.Quantity, d.PricePerShare, d.Fees, d.Note, now);
                results.Add(r);
            }
            return results;
        }
        catch
        {
            // Rollback: restore each position's state from snapshot, prune any that didn't exist.
            _positions.Clear();
            _positions.AddRange(savedPositions);
            foreach (var p in _positions)
            {
                var lotsField = p.GetType().GetField("_openLots",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!;
                var tradesField = p.GetType().GetField("_trades",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!;
                var realizedField = p.GetType().GetField("_realizedPnL",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!;

                var lots = (List<Lot>)lotsField.GetValue(p)!;
                lots.Clear(); lots.AddRange(savedLots[p]);

                var trades = (List<Trade>)tradesField.GetValue(p)!;
                trades.Clear(); trades.AddRange(savedTrades[p]);

                realizedField.SetValue(p, savedRealized[p]);
            }
            throw;
        }
    }
}
```

**Note on the reflection use:** the `_trades`/`_openLots`/`_realizedPnL` mutation via reflection in `DeleteTrade` and `ImportTrades` rollback is intentional — it keeps Position's public surface clean. There are alternatives (add internal `Position.ClearTrades()` and `Position.RestoreState(...)` helpers); both work. **Pick the helper approach** to avoid reflection in the hot path. Update `Position`:

Add to `TradyStrat.Domain/Portfolio/Position.cs`:

```csharp
    internal void ClearAllForReplay()
    {
        _openLots.Clear();
        _trades.Clear();
        _realizedPnL = Money.Zero(Currency.Eur);
    }

    internal void RestoreState(IEnumerable<Lot> lots, IEnumerable<Trade> trades, Money realized)
    {
        _openLots.Clear(); _openLots.AddRange(lots);
        _trades.Clear();   _trades.AddRange(trades);
        _realizedPnL = realized;
    }
```

Then **simplify** `Portfolio.DeleteTrade` and `Portfolio.ImportTrades` to use those helpers instead of reflection. Rewrite both methods:

```csharp
public TradeDeleted DeleteTrade(TradeId tradeId)
{
    Position? target = null;
    foreach (var p in _positions)
        if (p.Trades.Any(t => t.Id == tradeId)) { target = p; break; }
    if (target is null)
        throw new TradeValidationException($"Trade {tradeId} not found.");

    var realizedBefore = target.RealizedPnL;
    var remaining = target.Trades.Where(t => t.Id != tradeId)
                                 .OrderBy(t => t.ExecutedOn)
                                 .ToList();
    target.ClearAllForReplay();
    foreach (var t in remaining) target.Record(t);

    return new TradeDeleted(target.Id, target.RealizedPnL - realizedBefore);
}

public IReadOnlyList<TradeRecorded> ImportTrades(
    IReadOnlyList<TradeDraft> drafts, DateTime now)
{
    var savedPositions = _positions.ToList();
    var saved = _positions.ToDictionary(p => p,
        p => (lots: p.OpenLots.ToList(),
              trades: p.Trades.ToList(),
              realized: p.RealizedPnL));

    var results = new List<TradeRecorded>(drafts.Count);
    try
    {
        foreach (var d in drafts)
            results.Add(RecordTrade(d.InstrumentId, d.ExecutedOn, d.Side,
                d.Quantity, d.PricePerShare, d.Fees, d.Note, now));
        return results;
    }
    catch
    {
        _positions.Clear();
        _positions.AddRange(savedPositions);
        foreach (var p in _positions)
        {
            var (lots, trades, realized) = saved[p];
            p.RestoreState(lots, trades, realized);
        }
        throw;
    }
}
```

- [ ] **Step 17.4: Run tests passing**

Run: `dotnet test TradyStrat.Domain.Tests --filter "FullyQualifiedName~PortfolioBehaviorTests"`
Expected: all five behavior tests pass.

- [ ] **Step 17.5: Run the full Domain test suite to verify nothing else broke**

Run: `dotnet test TradyStrat.Domain.Tests`
Expected: all pass (Phase 1 kernel + Lot + Trade + Position + Portfolio).

- [ ] **Step 17.6: Commit**

```bash
git add TradyStrat.Domain/Portfolio/Portfolio.cs \
        TradyStrat.Domain/Portfolio/Position.cs \
        TradyStrat.Domain.Tests/Portfolio/PortfolioBehaviorTests.cs
git commit -m "feat(domain): Portfolio AR with RecordTrade/DeleteTrade/ImportTrades — Phase 2"
```

---

## Task 18: Update `PositionRow` and `PortfolioSnapshot` for VOs

**Files:**
- Modify: `TradyStrat.Domain/Portfolio/PositionRow.cs`
- Modify: `TradyStrat.Domain/Portfolio/PortfolioSnapshot.cs`

Read models. Adopt `Money` internally. Keep field names that the dashboard consumes (`CurrentValueEur`, `UnrealizedPnLEur`, etc.) because the Razor pages still reference those names — the legacy scalars `Shares`/`AvgCostEur` stay too (spec §13.1 temporary violation).

- [ ] **Step 18.1: Update `PositionRow`**

Replace `TradyStrat.Domain/Portfolio/PositionRow.cs`:

```csharp
using TradyStrat.Domain.Shared;

namespace TradyStrat.Domain.Portfolio;

public sealed record PositionRow(
    InstrumentId InstrumentId,
    string       Ticker,        // resolved by caller from IInstrumentRepository
    string       Currency,      // resolved by caller (instrument's native currency code)
    Quantity     Quantity,
    Money        CostBasisEur,
    Money        MarketValueEur,
    Money        UnrealizedPnLEur,
    Money        RealizedPnLEur);
```

- [ ] **Step 18.2: Update `PortfolioSnapshot`**

Replace `TradyStrat.Domain/Portfolio/PortfolioSnapshot.cs`:

```csharp
using TradyStrat.Domain.Shared;

namespace TradyStrat.Domain.Portfolio;

public sealed record PortfolioSnapshot(
    IReadOnlyList<PositionRow> Positions,
    Money   CurrentValueEur,
    Money   CostBasisEur,
    Money   UnrealizedPnLEur,
    Money   RealizedPnLEur,
    decimal ProgressPct,
    // Legacy scalars retained for HeroCapital/PortfolioRail/GrowthChart consumers.
    // Populated only when there's exactly one position. Spec §13.1 — removed when
    // the dashboard view-model rewrite lands.
    decimal Shares,
    Money   AvgCostEur);
```

- [ ] **Step 18.3: Verify Domain still builds**

Run: `dotnet build TradyStrat.Domain`
Expected: succeeds.

- [ ] **Step 18.4: Commit**

```bash
git add TradyStrat.Domain/Portfolio/PositionRow.cs TradyStrat.Domain/Portfolio/PortfolioSnapshot.cs
git commit -m "refactor(domain): PositionRow + PortfolioSnapshot adopt Money/Quantity VOs — Phase 2"
```

---

## Task 19: `Portfolio.Snapshot` and `Portfolio.SnapshotAsOf`

**Files:**
- Modify: `TradyStrat.Domain/Portfolio/Portfolio.cs`
- Create: `TradyStrat.Domain.Tests/Portfolio/PortfolioSnapshotTests.cs`

`Snapshot` takes `instrumentById`, `priceByInstrument` and `goalTarget` as parameters. `SnapshotAsOf(asOf, ...)` filters trade history before computing.

- [ ] **Step 19.1: Write failing tests for Snapshot**

`TradyStrat.Domain.Tests/Portfolio/PortfolioSnapshotTests.cs`:

```csharp
using Shouldly;
using TradyStrat.Domain;
using TradyStrat.Domain.Portfolio;
using TradyStrat.Domain.Shared;
using Xunit;

namespace TradyStrat.Domain.Tests.Portfolio;

public class PortfolioSnapshotTests
{
    private static readonly DateTime _now = new(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc);

    private static Instrument Inst(int id, string ticker, string currency) => new()
    {
        Id = id, Ticker = ticker, Name = ticker, Currency = currency,
        Exchange = "X", TimezoneId = "UTC", Kind = InstrumentKind.Held,
        AddedAt = DateTime.UtcNow,
    };

    [Fact]
    public void Empty_portfolio_snapshot_is_zero()
    {
        var portfolio = Portfolio.Empty(PortfolioId.Singleton);
        var instruments = new Dictionary<InstrumentId, Instrument>();
        var prices      = new Dictionary<InstrumentId, Price>();

        var snap = portfolio.Snapshot(instruments, prices, Money.Of(1_000_000m, Currency.Eur));

        snap.Positions.Count.ShouldBe(0);
        snap.Shares.ShouldBe(0m);
        snap.CurrentValueEur.ShouldBe(Money.Zero(Currency.Eur));
        snap.UnrealizedPnLEur.ShouldBe(Money.Zero(Currency.Eur));
        snap.RealizedPnLEur.ShouldBe(Money.Zero(Currency.Eur));
        snap.ProgressPct.ShouldBe(0m);
    }

    [Fact]
    public void Single_buy_snapshot_includes_fees_in_avg_cost()
    {
        var portfolio = Portfolio.Empty(PortfolioId.Singleton);
        var iid = new InstrumentId(1);
        portfolio.RecordTrade(iid, new DateOnly(2026, 1, 1), TradeSide.Buy,
            Quantity.Of(10m), Price.Of(Money.Of(4m, Currency.Eur)),
            Money.Of(2m, Currency.Eur), "", _now);

        var instruments = new Dictionary<InstrumentId, Instrument> {
            [iid] = Inst(1, "CON3.L", "USD"),
        };
        var prices = new Dictionary<InstrumentId, Price> {
            [iid] = Price.Of(Money.Of(5m, Currency.Eur)),
        };

        var snap = portfolio.Snapshot(instruments, prices, Money.Of(1_000_000m, Currency.Eur));

        snap.Shares.ShouldBe(10m);
        // Avg cost = (10*4 + 2)/10 = 4.20
        snap.AvgCostEur.ShouldBe(Money.Of(4.20m, Currency.Eur));
        snap.CurrentValueEur.ShouldBe(Money.Of(50m, Currency.Eur));
        snap.UnrealizedPnLEur.ShouldBe(Money.Of(8m, Currency.Eur));
    }

    [Fact]
    public void Multi_position_snapshot_sums_across_positions()
    {
        var portfolio = Portfolio.Empty(PortfolioId.Singleton);
        var a = new InstrumentId(1); var b = new InstrumentId(2);
        portfolio.RecordTrade(a, new DateOnly(2026, 1, 1), TradeSide.Buy,
            Quantity.Of(10m), Price.Of(Money.Of(4m, Currency.Eur)),
            Money.Zero(Currency.Eur), "", _now);
        portfolio.RecordTrade(b, new DateOnly(2026, 1, 1), TradeSide.Buy,
            Quantity.Of(5m), Price.Of(Money.Of(10m, Currency.Eur)),
            Money.Zero(Currency.Eur), "", _now);

        var instruments = new Dictionary<InstrumentId, Instrument> {
            [a] = Inst(1, "AAA", "USD"),
            [b] = Inst(2, "BBB", "USD"),
        };
        var prices = new Dictionary<InstrumentId, Price> {
            [a] = Price.Of(Money.Of(5m, Currency.Eur)),
            [b] = Price.Of(Money.Of(12m, Currency.Eur)),
        };

        var snap = portfolio.Snapshot(instruments, prices, Money.Of(1_000m, Currency.Eur));

        snap.Positions.Count.ShouldBe(2);
        snap.CurrentValueEur.ShouldBe(Money.Of(110m, Currency.Eur));   // 50 + 60
        snap.CostBasisEur.ShouldBe(Money.Of(90m, Currency.Eur));       // 40 + 50
        snap.UnrealizedPnLEur.ShouldBe(Money.Of(20m, Currency.Eur));
        // Multi-position: legacy scalars are zero.
        snap.Shares.ShouldBe(0m);
        snap.AvgCostEur.ShouldBe(Money.Zero(Currency.Eur));
    }

    [Fact]
    public void Snapshot_progress_pct_uses_goal_target()
    {
        var portfolio = Portfolio.Empty(PortfolioId.Singleton);
        var iid = new InstrumentId(1);
        portfolio.RecordTrade(iid, new DateOnly(2026, 1, 1), TradeSide.Buy,
            Quantity.Of(100m), Price.Of(Money.Of(10m, Currency.Eur)),
            Money.Zero(Currency.Eur), "", _now);

        var instruments = new Dictionary<InstrumentId, Instrument> { [iid] = Inst(1, "X", "EUR") };
        var prices      = new Dictionary<InstrumentId, Price>      { [iid] = Price.Of(Money.Of(20m, Currency.Eur)) };

        var snap = portfolio.Snapshot(instruments, prices, Money.Of(10_000m, Currency.Eur));

        // value = 100 * 20 = 2000; goal = 10000; pct = 20.
        snap.ProgressPct.ShouldBe(20m);
    }

    [Fact]
    public void SnapshotAsOf_excludes_trades_after_date()
    {
        var portfolio = Portfolio.Empty(PortfolioId.Singleton);
        var iid = new InstrumentId(1);
        portfolio.RecordTrade(iid, new DateOnly(2026, 1, 1), TradeSide.Buy,
            Quantity.Of(10m), Price.Of(Money.Of(4m, Currency.Eur)),
            Money.Zero(Currency.Eur), "", _now);
        portfolio.RecordTrade(iid, new DateOnly(2026, 2, 1), TradeSide.Buy,
            Quantity.Of(10m), Price.Of(Money.Of(5m, Currency.Eur)),
            Money.Zero(Currency.Eur), "", _now);

        var instruments = new Dictionary<InstrumentId, Instrument> { [iid] = Inst(1, "X", "EUR") };
        var prices      = new Dictionary<InstrumentId, Price>      { [iid] = Price.Of(Money.Of(6m, Currency.Eur)) };

        // As of mid-January, only the first buy is included.
        var snap = portfolio.SnapshotAsOf(
            new DateOnly(2026, 1, 15), instruments, prices, Money.Of(1_000m, Currency.Eur));

        snap.Shares.ShouldBe(10m);
        snap.CurrentValueEur.ShouldBe(Money.Of(60m, Currency.Eur));
    }
}
```

- [ ] **Step 19.2: Run failing test**

Run: `dotnet test TradyStrat.Domain.Tests --filter "FullyQualifiedName~PortfolioSnapshotTests"`
Expected: build error.

- [ ] **Step 19.3: Implement `Snapshot` and `SnapshotAsOf`**

Add to `TradyStrat.Domain/Portfolio/Portfolio.cs` (inside the class):

```csharp
public PortfolioSnapshot Snapshot(
    IReadOnlyDictionary<InstrumentId, Instrument> instrumentById,
    IReadOnlyDictionary<InstrumentId, Price>      priceByInstrument,
    Money goalTarget)
{
    return BuildSnapshot(_positions, instrumentById, priceByInstrument, goalTarget);
}

public PortfolioSnapshot SnapshotAsOf(
    DateOnly asOf,
    IReadOnlyDictionary<InstrumentId, Instrument> instrumentById,
    IReadOnlyDictionary<InstrumentId, Price>      priceByInstrument,
    Money goalTarget)
{
    // Rebuild a temporary copy with only trades up to and including asOf.
    var tempPortfolio = Portfolio.Empty(Id);
    foreach (var pos in _positions)
    {
        var tradesInWindow = pos.Trades
            .Where(t => t.ExecutedOn <= asOf)
            .OrderBy(t => t.ExecutedOn);
        foreach (var t in tradesInWindow)
            tempPortfolio.RecordTrade(
                pos.InstrumentId, t.ExecutedOn, t.Side,
                t.Quantity, t.PricePerShare, t.Fees, t.Note, t.CreatedAt);
    }
    return BuildSnapshot(tempPortfolio._positions, instrumentById, priceByInstrument, goalTarget);
}

private static PortfolioSnapshot BuildSnapshot(
    IReadOnlyList<Position> positions,
    IReadOnlyDictionary<InstrumentId, Instrument> instrumentById,
    IReadOnlyDictionary<InstrumentId, Price>      priceByInstrument,
    Money goalTarget)
{
    var rows = new List<PositionRow>(positions.Count);

    foreach (var pos in positions)
    {
        var hasInst  = instrumentById.TryGetValue(pos.InstrumentId, out var inst);
        var hasPrice = priceByInstrument.TryGetValue(pos.InstrumentId, out var price);

        var ticker   = hasInst  ? inst!.Ticker   : "?";
        var currency = hasInst  ? inst!.Currency : "?";

        var qty = pos.TotalQuantity;
        var costBasis = pos.CostBasis;
        var marketValue = hasPrice
            ? (price! * qty)
            : Money.Zero(Currency.Eur);
        var unrealised = marketValue - costBasis;

        rows.Add(new PositionRow(
            InstrumentId: pos.InstrumentId,
            Ticker:        ticker,
            Currency:      currency,
            Quantity:      qty,
            CostBasisEur:  costBasis,
            MarketValueEur: marketValue,
            UnrealizedPnLEur: unrealised,
            RealizedPnLEur:   pos.RealizedPnL));
    }

    var totalValue  = rows.Aggregate(Money.Zero(Currency.Eur), (a, r) => a + r.MarketValueEur);
    var totalCost   = rows.Aggregate(Money.Zero(Currency.Eur), (a, r) => a + r.CostBasisEur);
    var totalUnreal = totalValue - totalCost;
    var totalReal   = rows.Aggregate(Money.Zero(Currency.Eur), (a, r) => a + r.RealizedPnLEur);

    var pct = goalTarget.Amount == 0m
        ? 0m
        : totalValue.Amount / goalTarget.Amount * 100m;

    // Legacy single-position scalars
    var legacyShares = rows.Count == 1 ? rows[0].Quantity.Value : 0m;
    var legacyAvgCost = (rows.Count == 1 && legacyShares > 0m)
        ? Money.Of(rows[0].CostBasisEur.Amount / legacyShares, Currency.Eur)
        : Money.Zero(Currency.Eur);

    return new PortfolioSnapshot(
        Positions:        rows,
        CurrentValueEur:  totalValue,
        CostBasisEur:     totalCost,
        UnrealizedPnLEur: totalUnreal,
        RealizedPnLEur:   totalReal,
        ProgressPct:      pct,
        Shares:           legacyShares,
        AvgCostEur:       legacyAvgCost);
}
```

**Cross-aggregate note:** `BuildSnapshot` calls `inst.Ticker` and `inst.Currency` — that means the existing anemic `Instrument` record is still referenced from the AR. That's an Application/Domain boundary read, not a cross-aggregate dependency on Instrument's *behavior*, so the spec's "by ID only" rule is preserved (Position stores only `InstrumentId`). Phase 4 will add the rich `Instrument` AR; this code continues to read its `Ticker`/`Currency` fields without modification.

- [ ] **Step 19.4: Run tests passing**

Run: `dotnet test TradyStrat.Domain.Tests --filter "FullyQualifiedName~PortfolioSnapshotTests"`
Expected: all five snapshot tests pass.

- [ ] **Step 19.5: Commit**

```bash
git add TradyStrat.Domain/Portfolio/Portfolio.cs \
        TradyStrat.Domain.Tests/Portfolio/PortfolioSnapshotTests.cs
git commit -m "feat(domain): Portfolio.Snapshot + SnapshotAsOf — Phase 2"
```

---

## Task 20: `Portfolio.GrowthSeries` — faithful absorption of `GrowthSeriesBuilder`

**Files:**
- Modify: `TradyStrat.Domain/Portfolio/Portfolio.cs`
- Create: `TradyStrat.Domain.Tests/Portfolio/PortfolioGrowthSeriesTests.cs`

Today's `GrowthSeriesBuilder` walks all trades for one ticker, joins with price bars, accumulates `shares × close` per bar date, and prepends a synthetic zero-leading day. Behavioral-parity port: same shape, same numbers.

`GrowthPoint` stays a record with raw `decimal Value` for Phase 2 — Phase 5 introduces the typed `Money`/`Percentage` form.

- [ ] **Step 20.1: Write failing tests**

`TradyStrat.Domain.Tests/Portfolio/PortfolioGrowthSeriesTests.cs`:

```csharp
using Shouldly;
using TradyStrat.Domain;
using TradyStrat.Domain.Portfolio;
using TradyStrat.Domain.Shared;
using Xunit;

namespace TradyStrat.Domain.Tests.Portfolio;

public class PortfolioGrowthSeriesTests
{
    private static readonly DateTime _now = new(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void Empty_portfolio_returns_empty_series()
    {
        var portfolio = Portfolio.Empty(PortfolioId.Singleton);
        var series = portfolio.GrowthSeries(
            new Dictionary<InstrumentId, IReadOnlyList<PriceBar>>());
        series.ShouldBeEmpty();
    }

    [Fact]
    public void Prepends_synthetic_zero_day_then_accumulates_per_bar()
    {
        var portfolio = Portfolio.Empty(PortfolioId.Singleton);
        var iid = new InstrumentId(1);
        portfolio.RecordTrade(iid, new DateOnly(2026, 1, 2), TradeSide.Buy,
            Quantity.Of(10m), Price.Of(Money.Of(4m, Currency.Eur)),
            Money.Zero(Currency.Eur), "", _now);

        var bars = new Dictionary<InstrumentId, IReadOnlyList<PriceBar>>
        {
            [iid] = new List<PriceBar>
            {
                new() { Id = 0, Ticker = "X", Date = new DateOnly(2026, 1, 2),
                    Open = 4m, High = 5m, Low = 4m, Close = 5m, Volume = 0 },
                new() { Id = 0, Ticker = "X", Date = new DateOnly(2026, 1, 3),
                    Open = 5m, High = 6m, Low = 5m, Close = 6m, Volume = 0 },
            }
        };

        var series = portfolio.GrowthSeries(bars);

        series.Count.ShouldBe(3);
        series[0].Date.ShouldBe(new DateOnly(2026, 1, 1)); // synthetic
        series[0].Value.ShouldBe(0m);
        series[1].Date.ShouldBe(new DateOnly(2026, 1, 2));
        series[1].Value.ShouldBe(50m);   // 10 shares × 5
        series[2].Date.ShouldBe(new DateOnly(2026, 1, 3));
        series[2].Value.ShouldBe(60m);   // 10 shares × 6
    }
}
```

- [ ] **Step 20.2: Run failing test**

Run: `dotnet test TradyStrat.Domain.Tests --filter "FullyQualifiedName~PortfolioGrowthSeriesTests"`
Expected: build error (no `GrowthSeries` yet).

- [ ] **Step 20.3: Implement `GrowthSeries`**

Add to `TradyStrat.Domain/Portfolio/Portfolio.cs`:

```csharp
public IReadOnlyList<GrowthPoint> GrowthSeries(
    IReadOnlyDictionary<InstrumentId, IReadOnlyList<PriceBar>> barsByInstrument)
{
    // Faithful port of GrowthSeriesBuilder. Single-ticker today: walks all trades
    // (across all positions), joins with bars for the corresponding instrument,
    // accumulates shares*close per bar date. Multi-instrument support sums each
    // instrument's contribution per bar date (where bars overlap).
    var allTrades = _positions.SelectMany(p => p.Trades.Select(t => (p.InstrumentId, t)))
                              .OrderBy(x => x.t.ExecutedOn)
                              .ToList();
    if (allTrades.Count == 0) return [];

    // All bar dates across instruments, deduped + sorted.
    var allBarDates = barsByInstrument.Values
        .SelectMany(bars => bars.Select(b => b.Date))
        .Distinct()
        .OrderBy(d => d)
        .ToList();
    if (allBarDates.Count == 0) return [];

    var firstTradeDate = allTrades[0].t.ExecutedOn;
    var points = new List<GrowthPoint>(allBarDates.Count + 1)
    {
        // Synthetic leading zero: one day before first trade.
        new(firstTradeDate.AddDays(-1), 0m),
    };

    var sharesByInstrument = new Dictionary<InstrumentId, decimal>();

    // Group trades by date for O(1) lookup during the bar walk.
    var tradesByDate = allTrades
        .GroupBy(x => x.t.ExecutedOn)
        .ToDictionary(g => g.Key, g => g.ToList());

    foreach (var date in allBarDates)
    {
        if (tradesByDate.TryGetValue(date, out var todays))
            foreach (var (iid, t) in todays)
            {
                var delta = t.IsBuy ? t.Quantity.Value : -t.Quantity.Value;
                sharesByInstrument[iid] = sharesByInstrument.GetValueOrDefault(iid) + delta;
            }

        var totalValue = 0m;
        foreach (var (iid, bars) in barsByInstrument)
        {
            var bar = bars.FirstOrDefault(b => b.Date == date);
            if (bar is null) continue;
            var shares = sharesByInstrument.GetValueOrDefault(iid);
            totalValue += shares * bar.Close;
        }
        points.Add(new GrowthPoint(date, totalValue));
    }

    return points;
}
```

This handles both single-ticker (existing behavior) and degenerate-multi-ticker (sums what bars are available). Bug-parity with `GrowthSeriesBuilder` is preserved for the single-ticker case the dashboard exercises today.

- [ ] **Step 20.4: Run tests passing**

Run: `dotnet test TradyStrat.Domain.Tests --filter "FullyQualifiedName~PortfolioGrowthSeriesTests"`
Expected: pass.

- [ ] **Step 20.5: Full Domain test suite**

Run: `dotnet test TradyStrat.Domain.Tests`
Expected: all pass.

- [ ] **Step 20.6: Commit**

```bash
git add TradyStrat.Domain/Portfolio/Portfolio.cs TradyStrat.Domain.Tests/Portfolio/PortfolioGrowthSeriesTests.cs
git commit -m "feat(domain): Portfolio.GrowthSeries absorbs GrowthSeriesBuilder — Phase 2"
```

---

## Task 21: `IPortfolioRepository` port

**Files:**
- Create: `TradyStrat.Application/Portfolio/IPortfolioRepository.cs`
- Modify: `TradyStrat.Application/Portfolio/PortfolioApplicationModule.cs`

The application's contract for portfolio persistence. Repository implementation lives in Infrastructure (Task 25).

- [ ] **Step 21.1: Create the port**

`TradyStrat.Application/Portfolio/IPortfolioRepository.cs`:

```csharp
using TradyStrat.Domain.Portfolio;

namespace TradyStrat.Application.Portfolio;

public interface IPortfolioRepository
{
    Task<Portfolio> GetAsync(CancellationToken ct);
    Task SaveAsync(Portfolio portfolio, CancellationToken ct);
}
```

- [ ] **Step 21.2: PortfolioApplicationModule stays empty for now**

Already emptied in Task 14.4. Verify:

Run: `cat TradyStrat.Application/Portfolio/PortfolioApplicationModule.cs`
Expected: empty `ConfigureServices` body (no registrations).

The `IPortfolioRepository` registration lives in Infrastructure (Task 25), not Application.

- [ ] **Step 21.3: Build + commit**

Run: `dotnet build TradyStrat.Application`
Expected: succeeds.

```bash
git add TradyStrat.Application/Portfolio/IPortfolioRepository.cs
git commit -m "feat(application): IPortfolioRepository port — Phase 2"
```

---

## Task 22: EF configurations for `Portfolio` / `Position` / `Lot` / new `Trade`

**Files:**
- Create: `TradyStrat.Infrastructure/Data/Configurations/PortfolioConfiguration.cs`
- Create: `TradyStrat.Infrastructure/Data/Configurations/PositionConfiguration.cs`
- Modify: `TradyStrat.Infrastructure/Data/Configurations/TradeConfiguration.cs` (today maps the old Trade record; replace)

`Lot` is mapped as an owned-many of `Position` — no separate configuration file needed (declared inside `PositionConfiguration`).

- [ ] **Step 22.1: Write `PortfolioConfiguration`**

`TradyStrat.Infrastructure/Data/Configurations/PortfolioConfiguration.cs`:

```csharp
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TradyStrat.Domain.Portfolio;

namespace TradyStrat.Infrastructure.Data.Configurations;

public sealed class PortfolioConfiguration : IEntityTypeConfiguration<Portfolio>
{
    public void Configure(EntityTypeBuilder<Portfolio> builder)
    {
        builder.ToTable("Portfolios");
        builder.HasKey(p => p.Id);
        builder.Property(p => p.Id).ValueGeneratedNever();  // singleton with explicit Id = 1

        builder.HasMany<Position>("_positions")
               .WithOne()
               .HasForeignKey("PortfolioId")
               .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation("_positions")
               .HasField("_positions")
               .UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.Ignore(p => p.Positions);
    }
}
```

- [ ] **Step 22.2: Write `PositionConfiguration`**

`TradyStrat.Infrastructure/Data/Configurations/PositionConfiguration.cs`:

```csharp
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TradyStrat.Domain.Portfolio;
using TradyStrat.Domain.Shared;

namespace TradyStrat.Infrastructure.Data.Configurations;

public sealed class PositionConfiguration : IEntityTypeConfiguration<Position>
{
    public void Configure(EntityTypeBuilder<Position> builder)
    {
        builder.ToTable("Positions");
        builder.HasKey(p => p.Id);
        builder.Property(p => p.Id).ValueGeneratedOnAdd();
        builder.Property(p => p.InstrumentId);

        // RealizedPnL as owned Money (two columns).
        builder.OwnsOne<Money>("_realizedPnL", m =>
        {
            m.Property(x => x.Amount).HasColumnName("RealizedPnLAmount").HasColumnType("TEXT");
            m.Property(x => x.Currency).HasConversion(c => c.Code, s => Currency.Parse(s))
             .HasColumnName("RealizedPnLCurrency").HasMaxLength(3);
            m.Property(x => x.IsEmpty).HasColumnName("RealizedPnLIsEmpty");
        });

        // Open lots as owned-many.
        builder.OwnsMany<Lot>("_openLots", lots =>
        {
            lots.ToTable("PositionLots");
            lots.WithOwner().HasForeignKey("PositionId");
            lots.Property<int>("Id").ValueGeneratedOnAdd();
            lots.HasKey("Id");

            lots.Property(l => l.OpenedOn);
            lots.OwnsOne(l => l.Quantity, q =>
            {
                q.Property(x => x.Value).HasColumnName("Quantity").HasColumnType("TEXT");
                q.Property(x => x.IsSpecified).HasColumnName("QuantityIsSpecified");
            });
            lots.OwnsOne(l => l.UnitCost, m =>
            {
                m.Property(x => x.Amount).HasColumnName("UnitCostAmount").HasColumnType("TEXT");
                m.Property(x => x.Currency).HasConversion(c => c.Code, s => Currency.Parse(s))
                 .HasColumnName("UnitCostCurrency").HasMaxLength(3);
                m.Property(x => x.IsEmpty).HasColumnName("UnitCostIsEmpty");
            });
        });

        // Trades as a relationship (separate Trades table, FK PositionId).
        builder.HasMany<Trade>("_trades")
               .WithOne()
               .HasForeignKey("PositionId")
               .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation("_openLots")
               .HasField("_openLots")
               .UsePropertyAccessMode(PropertyAccessMode.Field);
        builder.Navigation("_trades")
               .HasField("_trades")
               .UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.Ignore(p => p.OpenLots);
        builder.Ignore(p => p.Trades);
        builder.Ignore(p => p.RealizedPnL);
        builder.Ignore(p => p.TotalQuantity);
        builder.Ignore(p => p.CostBasis);
    }
}
```

- [ ] **Step 22.3: Replace `TradeConfiguration`**

Replace `TradyStrat.Infrastructure/Data/Configurations/TradeConfiguration.cs`:

```csharp
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TradyStrat.Domain.Portfolio;
using TradyStrat.Domain.Shared;

namespace TradyStrat.Infrastructure.Data.Configurations;

public sealed class TradeConfiguration : IEntityTypeConfiguration<Trade>
{
    public void Configure(EntityTypeBuilder<Trade> builder)
    {
        builder.ToTable("Trades");
        builder.HasKey(t => t.Id);
        builder.Property(t => t.Id).ValueGeneratedOnAdd();

        builder.Property(t => t.ExecutedOn);
        builder.Property(t => t.Side);
        builder.Property(t => t.Note).HasMaxLength(2000);
        builder.Property(t => t.CreatedAt);

        builder.OwnsOne(t => t.Quantity, q =>
        {
            q.Property(x => x.Value).HasColumnName("Quantity").HasColumnType("TEXT");
            q.Property(x => x.IsSpecified).HasColumnName("QuantityIsSpecified");
        });

        builder.OwnsOne(t => t.PricePerShare, p =>
        {
            p.OwnsOne(x => x.PerUnit, m =>
            {
                m.Property(x => x.Amount).HasColumnName("PricePerShareAmount").HasColumnType("TEXT");
                m.Property(x => x.Currency).HasConversion(c => c.Code, s => Currency.Parse(s))
                 .HasColumnName("PricePerShareCurrency").HasMaxLength(3);
                m.Property(x => x.IsEmpty).HasColumnName("PricePerShareIsEmpty");
            });
        });

        builder.OwnsOne(t => t.Fees, m =>
        {
            m.Property(x => x.Amount).HasColumnName("FeesAmount").HasColumnType("TEXT");
            m.Property(x => x.Currency).HasConversion(c => c.Code, s => Currency.Parse(s))
             .HasColumnName("FeesCurrency").HasMaxLength(3);
            m.Property(x => x.IsEmpty).HasColumnName("FeesIsEmpty");
        });

        // Denormalized for read-path back-compat during the Phase 2 cutover
        // (spec §13.1; dropped in a later cleanup migration).
        builder.Property<int>("InstrumentId");

        builder.HasIndex(t => t.ExecutedOn);
        builder.HasIndex("InstrumentId", "ExecutedOn");

        builder.Ignore(t => t.Gross);
        builder.Ignore(t => t.Net);
        builder.Ignore(t => t.IsBuy);
    }
}
```

- [ ] **Step 22.4: Build Infrastructure**

Run: `dotnet build TradyStrat.Infrastructure`
Expected: succeeds.

- [ ] **Step 22.5: Commit**

```bash
git add TradyStrat.Infrastructure/Data/Configurations/PortfolioConfiguration.cs \
        TradyStrat.Infrastructure/Data/Configurations/PositionConfiguration.cs \
        TradyStrat.Infrastructure/Data/Configurations/TradeConfiguration.cs
git commit -m "feat(infra): EF configurations for Portfolio/Position/Lot/Trade — Phase 2"
```

---

## Task 23: Add `Portfolio` and `Position` to `AppDbContext`

**Files:**
- Modify: `TradyStrat.Infrastructure/Data/AppDbContext.cs`

- [ ] **Step 23.1: Add `DbSet`s**

Replace the body of `AppDbContext` so it exposes the new types alongside the existing ones:

```csharp
using Microsoft.EntityFrameworkCore;
using TradyStrat.Domain;
using TradyStrat.Domain.Portfolio;
using TradyStrat.Infrastructure.Data.Conventions;

namespace TradyStrat.Infrastructure.Data;

public sealed class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Portfolio>   Portfolios   => Set<Portfolio>();
    public DbSet<Position>    Positions    => Set<Position>();
    public DbSet<Trade>       Trades       => Set<Trade>();
    public DbSet<PriceBar>    PriceBars    => Set<PriceBar>();
    public DbSet<FxRate>      FxRates      => Set<FxRate>();
    public DbSet<GoalConfig>  Goals        => Set<GoalConfig>();
    public DbSet<Suggestion>  Suggestions  => Set<Suggestion>();
    public DbSet<Instrument>  Instruments  => Set<Instrument>();
    public DbSet<SettingEntry> Settings    => Set<SettingEntry>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
        => modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);

    protected override void ConfigureConventions(ModelConfigurationBuilder builder)
    {
        base.ConfigureConventions(builder);
        StronglyTypedIdConventions.ApplyTo(builder);
    }
}
```

Note: there's an ambiguity here — `Trade` is now in `TradyStrat.Domain.Portfolio`, but `DbSet<Trade>` resolution must pick the new type, not anything that might be left behind. Confirm the `Trade` reference uses `TradyStrat.Domain.Portfolio.Trade`. The `using TradyStrat.Domain.Portfolio;` line ensures this.

- [ ] **Step 23.2: Build**

Run: `dotnet build TradyStrat.Infrastructure`
Expected: succeeds.

- [ ] **Step 23.3: Commit**

```bash
git add TradyStrat.Infrastructure/Data/AppDbContext.cs
git commit -m "feat(infra): AppDbContext exposes Portfolios and Positions — Phase 2"
```

---

## Task 24: EF migration

**Files:**
- Create: `TradyStrat.Infrastructure/Data/Migrations/20260522_AddPortfolioAndPositions.cs` (auto-generated, then edited for backfill)
- Modify: `TradyStrat.Infrastructure/Data/Migrations/AppDbContextModelSnapshot.cs` (auto-regenerated by `dotnet ef`)

- [ ] **Step 24.1: Generate the migration**

Run from the repo root:

```bash
dotnet tool restore
dotnet ef migrations add AddPortfolioAndPositions \
  --project TradyStrat.Infrastructure \
  --startup-project TradyStrat \
  --output-dir Data/Migrations
```

Expected: a new pair of files appears (`*_AddPortfolioAndPositions.cs` and `.Designer.cs`), and `AppDbContextModelSnapshot.cs` regenerates.

The generated `Up` should create `Portfolios`, `Positions`, `PositionLots` tables, add the `PositionId` column to `Trades`, plus all the owned-type columns (`RealizedPnLAmount`, `Quantity`, `PricePerShareAmount`, `FeesAmount`, etc.) and may also rename/drop columns from the old `Trade` shape (`FeesEur`, `PricePerShare`). Inspect the generated file before editing.

- [ ] **Step 24.2: Inspect the generated migration**

Run: `cat TradyStrat.Infrastructure/Data/Migrations/*_AddPortfolioAndPositions.cs | head -200`

Confirm it creates the new tables. Note any column drops (`FeesEur`, `PricePerShare` from the old `Trades` schema) — those will lose data unless backfilled. We need to **preserve** existing trade data.

- [ ] **Step 24.3: Edit the migration `Up` to backfill**

Open the `*_AddPortfolioAndPositions.cs` file (not the `.Designer.cs`). At the **end of the `Up` method**, after all schema operations, add:

```csharp
        // Backfill: singleton Portfolio, one Position per distinct InstrumentId
        // referenced by existing Trades, then link Trades to their Position.
        migrationBuilder.Sql("INSERT INTO Portfolios (Id) VALUES (1);");

        migrationBuilder.Sql(@"
            INSERT INTO Positions (PortfolioId, InstrumentId, RealizedPnLAmount, RealizedPnLCurrency, RealizedPnLIsEmpty)
            SELECT 1, InstrumentId, '0', 'EUR', 0
            FROM Trades
            GROUP BY InstrumentId;");

        migrationBuilder.Sql(@"
            UPDATE Trades
            SET PositionId = (
                SELECT p.Id FROM Positions p
                WHERE p.InstrumentId = Trades.InstrumentId);");

        // Copy legacy Trade column values into new owned-type columns.
        // (The auto-generated migration may have already done this via column rename;
        //  inspect the migration body before adding these. If it dropped/recreated
        //  Trades, restore the data with INSERT...SELECT from a temp table.)
        // For SQLite, ALTER TABLE limitations mean the auto-generated migration
        // probably did a table rebuild — verify the new Trades has the same rows.
```

If the auto-generated migration rebuilt the `Trades` table (likely for SQLite), the engineer needs to:

1. Comment out the auto-generated table-drop + recreate.
2. Manually add ADD COLUMN statements for the new owned-type columns.
3. Copy values: `UPDATE Trades SET QuantityValue = Quantity, FeesAmount = FeesEur, ...`
4. Drop the old columns at the end (or leave them as legacy denormalized — spec §13.1 allows it).

**This step requires judgement.** If the engineer is uncertain, **stop and surface to the user** before running the migration in anger.

- [ ] **Step 24.4: Test the migration on a copy of the prod DB**

Locate the prod DB: `~/Library/Application Support/TradyStrat/tradystrat.db`. **Do not** apply the migration to it directly. Copy first:

```bash
cp "$HOME/Library/Application Support/TradyStrat/tradystrat.db" /tmp/tradystrat-migration-test.db
TRADYSTRAT_DB=/tmp/tradystrat-migration-test.db dotnet ef database update \
  --project TradyStrat.Infrastructure \
  --startup-project TradyStrat
```

(If the host doesn't honor `TRADYSTRAT_DB`, override via `Database:Path` in user-secrets or appsettings — adjust as needed for the actual config wiring.)

Expected: migration completes without errors. Inspect:

```bash
sqlite3 /tmp/tradystrat-migration-test.db "SELECT COUNT(*) FROM Portfolios;"   # 1
sqlite3 /tmp/tradystrat-migration-test.db "SELECT COUNT(*) FROM Positions;"   # one per distinct instrument
sqlite3 /tmp/tradystrat-migration-test.db "SELECT COUNT(*) FROM Trades WHERE PositionId IS NULL;"  # 0
```

- [ ] **Step 24.5: Commit**

```bash
git add TradyStrat.Infrastructure/Data/Migrations/
git commit -m "feat(infra): EF migration — Portfolios + Positions + Trade.PositionId — Phase 2"
```

---

## Task 25: `EfPortfolioRepository` with eager-load and rehydration

**Files:**
- Create: `TradyStrat.Infrastructure/Portfolio/EfPortfolioRepository.cs`
- Create: `TradyStrat.Infrastructure/Portfolio/PortfolioInfrastructureModule.cs`

- [ ] **Step 25.1: Implement the repository**

`TradyStrat.Infrastructure/Portfolio/EfPortfolioRepository.cs`:

```csharp
using Microsoft.EntityFrameworkCore;
using TradyStrat.Application.Portfolio;
using TradyStrat.Domain.Portfolio;
using TradyStrat.Domain.Shared;
using TradyStrat.Infrastructure.Data;

namespace TradyStrat.Infrastructure.Portfolio;

public sealed class EfPortfolioRepository(AppDbContext db) : IPortfolioRepository
{
    public async Task<Portfolio> GetAsync(CancellationToken ct)
    {
        var portfolio = await db.Portfolios
            .Include("_positions")
            .Include("_positions._openLots")
            .Include("_positions._trades")
            .SingleOrDefaultAsync(p => p.Id == PortfolioId.Singleton, ct);

        if (portfolio is null)
        {
            portfolio = Portfolio.Empty(PortfolioId.Singleton);
            db.Portfolios.Add(portfolio);
            await db.SaveChangesAsync(ct);
            return portfolio;
        }

        // First-load lot rehydration: if any position has trades but no open lots,
        // it's the post-migration first read. Replay trades → derive lots/realized,
        // then save.
        var needsRehydration = portfolio.Positions
            .Any(p => p.Trades.Count > 0 && p.OpenLots.Count == 0);

        if (needsRehydration)
        {
            foreach (var pos in portfolio.Positions)
            {
                if (pos.Trades.Count == 0) continue;
                var ordered = pos.Trades.OrderBy(t => t.ExecutedOn).ToList();
                pos.ClearAllForReplay();
                foreach (var t in ordered) pos.Record(t);
            }
            await db.SaveChangesAsync(ct);
        }

        return portfolio;
    }

    public Task SaveAsync(Portfolio portfolio, CancellationToken ct)
        => db.SaveChangesAsync(ct).ContinueWith(_ => { }, ct);
}
```

- [ ] **Step 25.2: Register in module**

`TradyStrat.Infrastructure/Portfolio/PortfolioInfrastructureModule.cs`:

```csharp
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TheAppManager.Modules;
using TradyStrat.Application.Portfolio;

namespace TradyStrat.Infrastructure.Portfolio;

public sealed class PortfolioInfrastructureModule : IAppModule
{
    public void ConfigureServices(IServiceCollection services, IConfiguration config)
    {
        services.AddScoped<IPortfolioRepository, EfPortfolioRepository>();
    }
}
```

- [ ] **Step 25.3: Build**

Run: `dotnet build TradyStrat.Infrastructure`
Expected: succeeds.

- [ ] **Step 25.4: Commit**

```bash
git add TradyStrat.Infrastructure/Portfolio/
git commit -m "feat(infra): EfPortfolioRepository with eager-load + post-migration rehydration — Phase 2"
```

---

## Task 26: Repository round-trip + rehydration tests

**Files:**
- Create: `TradyStrat.Infrastructure.Tests/Portfolio/EfPortfolioRepositoryTests.cs`

Use the existing `TradyStrat.TestKit.Specifications.InMemoryDb` helper (referenced by today's PortfolioServiceTests) to spin up an in-memory `AppDbContext`.

- [ ] **Step 26.1: Write the round-trip test**

`TradyStrat.Infrastructure.Tests/Portfolio/EfPortfolioRepositoryTests.cs`:

```csharp
using Shouldly;
using TradyStrat.Domain.Portfolio;
using TradyStrat.Domain.Shared;
using TradyStrat.Infrastructure.Portfolio;
using TradyStrat.TestKit.Specifications;
using Xunit;

namespace TradyStrat.Infrastructure.Tests.Portfolio;

public class EfPortfolioRepositoryTests
{
    private static readonly DateTime _now = new(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public async Task GetAsync_creates_empty_portfolio_when_none_exists()
    {
        await using var db = InMemoryDb.Create();
        var repo = new EfPortfolioRepository(db);

        var portfolio = await repo.GetAsync(TestContext.Current.CancellationToken);

        portfolio.Id.ShouldBe(PortfolioId.Singleton);
        portfolio.Positions.ShouldBeEmpty();
    }

    [Fact]
    public async Task Round_trip_preserves_trades_and_lots_and_realized()
    {
        await using var db = InMemoryDb.Create();
        var ct = TestContext.Current.CancellationToken;

        var repo = new EfPortfolioRepository(db);
        var portfolio = await repo.GetAsync(ct);

        portfolio.RecordTrade(new InstrumentId(1), new DateOnly(2026, 1, 1), TradeSide.Buy,
            Quantity.Of(10m), Price.Of(Money.Of(4m, Currency.Eur)),
            Money.Of(2m, Currency.Eur), "", _now);
        portfolio.RecordTrade(new InstrumentId(1), new DateOnly(2026, 1, 5), TradeSide.Sell,
            Quantity.Of(5m), Price.Of(Money.Of(6m, Currency.Eur)),
            Money.Zero(Currency.Eur), "", _now);

        await repo.SaveAsync(portfolio, ct);

        // Reload from a fresh context.
        await using var db2 = InMemoryDb.SameStore(db);   // hypothetical — see note below
        var repo2 = new EfPortfolioRepository(db2);
        var reloaded = await repo2.GetAsync(ct);

        reloaded.Positions.Count.ShouldBe(1);
        var pos = reloaded.Positions[0];
        pos.Trades.Count.ShouldBe(2);
        pos.TotalQuantity.ShouldBe(Quantity.Of(5m));
        // Sell 5 @ 6, cost basis 4.20 (fees-folded), so realized = 5*(6-4.20) = 9
        pos.RealizedPnL.Amount.ShouldBe(9.00m, tolerance: 0.0001m);
    }

    [Fact]
    public async Task Rehydration_runs_when_trades_exist_but_lots_are_empty()
    {
        await using var db = InMemoryDb.Create();
        var ct = TestContext.Current.CancellationToken;

        // Simulate post-migration state: portfolio + position exist, trades attached,
        // but no lots and zero realized P&L.
        var portfolio = Portfolio.Empty(PortfolioId.Singleton);
        db.Portfolios.Add(portfolio);
        var position = Position.OpenFor(new InstrumentId(1));
        // Insert trades into the position via reflection or via a test helper:
        // for this test, simply RecordTrade then ClearAllForReplay on the position
        // before saving, to mimic the migration state.
        var t1 = Trade.Create(new DateOnly(2026, 1, 1), TradeSide.Buy,
            Quantity.Of(10m), Price.Of(Money.Of(4m, Currency.Eur)),
            Money.Zero(Currency.Eur), "", _now);
        var t2 = Trade.Create(new DateOnly(2026, 1, 5), TradeSide.Buy,
            Quantity.Of(10m), Price.Of(Money.Of(5m, Currency.Eur)),
            Money.Zero(Currency.Eur), "", _now);
        // Use a test-only seam: RestoreState with trades but no lots.
        position.RestoreState(lots: [], trades: [t1, t2], realized: Money.Zero(Currency.Eur));

        // Attach position to portfolio via reflection (test-only).
        var positionsField = typeof(Portfolio).GetField("_positions",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!;
        ((List<Position>)positionsField.GetValue(portfolio)!).Add(position);

        await db.SaveChangesAsync(ct);

        // Now load via the repository — rehydration should trigger.
        await using var db2 = InMemoryDb.SameStore(db);
        var repo = new EfPortfolioRepository(db2);
        var reloaded = await repo.GetAsync(ct);

        var pos = reloaded.Positions[0];
        pos.OpenLots.Count.ShouldBe(2);
        pos.TotalQuantity.ShouldBe(Quantity.Of(20m));
        pos.RealizedPnL.ShouldBe(Money.Zero(Currency.Eur));
    }
}
```

**Note on `InMemoryDb.SameStore`:** the existing `InMemoryDb.Create()` helper produces a fresh `AppDbContext` over a unique in-memory database — there is no `SameStore` shared mode. Either:

- Add a `SameStore` helper to `TradyStrat.TestKit.Specifications.InMemoryDb` that creates a new DbContext over the same `InMemoryDatabaseRoot` (EF Core supports this).
- Or use Sqlite in-memory (`"DataSource=:memory:"`) with a shared connection — the existing test infrastructure may already support this; check the file.

The engineer must choose the simplest path; if `InMemoryDb` doesn't support shared-store testing, extend it. This is a 5-line change to the TestKit.

- [ ] **Step 26.2: Run tests passing**

Run: `dotnet test TradyStrat.Infrastructure.Tests --filter "FullyQualifiedName~EfPortfolioRepository"`
Expected: all three tests pass.

- [ ] **Step 26.3: Commit**

```bash
git add TradyStrat.Infrastructure.Tests/Portfolio/EfPortfolioRepositoryTests.cs
git add TradyStrat.TestKit/   # if SameStore helper was added
git commit -m "test(infra): EfPortfolioRepository round-trip + rehydration — Phase 2"
```

---

## Task 27: Rewrite `LogTradeUseCase`

**Files:**
- Restore: `TradyStrat.Application/Trades/UseCases/LogTradeUseCase.cs` (from `.bak` reference) with new shape
- Test:   `TradyStrat.Application.Tests/Trades/LogTradeUseCaseTests.cs` (new)

Loads instrument (cross-aggregate) + portfolio, builds VOs, calls `portfolio.RecordTrade`, saves.

- [ ] **Step 27.1: Write failing test**

`TradyStrat.Application.Tests/Trades/LogTradeUseCaseTests.cs`:

```csharp
using Microsoft.Extensions.Logging.Abstractions;
using Shouldly;
using TradyStrat.Application.Portfolio;
using TradyStrat.Application.Trades.UseCases;
using TradyStrat.Domain;
using TradyStrat.Domain.Portfolio;
using TradyStrat.Domain.Shared;
using TradyStrat.TestKit;
using TradyStrat.TestKit.Specifications;
using Xunit;

namespace TradyStrat.Application.Tests.Trades;

public class LogTradeUseCaseTests
{
    [Fact]
    public async Task Records_a_buy_through_Portfolio_AR()
    {
        await using var db = InMemoryDb.Create();
        var ct = TestContext.Current.CancellationToken;

        // Seed: one instrument (EUR currency).
        db.Instruments.Add(new Instrument
        {
            Id = 1, Ticker = "X", Name = "X", Currency = "EUR",
            Exchange = "X", TimezoneId = "UTC", Kind = InstrumentKind.Held,
            AddedAt = DateTime.UtcNow,
        });
        await db.SaveChangesAsync(ct);

        var portfolios = new EfPortfolioRepositoryTestFake(db);
        var instruments = new TestReadRepo<Instrument>(db);
        var clock = new TestClock(new DateTime(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc));
        var useCase = new LogTradeUseCase(
            portfolios, instruments, clock, NullLogger<LogTradeUseCase>.Instance);

        var input = new LogTradeInput(
            InstrumentId:   1,
            ExecutedOn:     new DateOnly(2026, 1, 1),
            Side:           TradeSide.Buy,
            QuantityValue:  10m,
            PriceValue:     4m,
            FeesValue:      0m,
            Note:           null);

        var result = await useCase.ExecuteAsync(input, ct);

        result.CreatedPosition.ShouldBeTrue();
        var portfolio = await portfolios.GetAsync(ct);
        portfolio.Positions.Count.ShouldBe(1);
    }
}
```

This test depends on `EfPortfolioRepositoryTestFake` (a thin wrapper for tests) and `TestReadRepo<Instrument>` / `TestClock` — the TestKit may need these. If `TestReadRepo<T>` already exists (it's used in today's `PortfolioServiceTests`), reuse. If not, add a minimal in-memory `IReadRepositoryBase<Instrument>` impl. Similarly add an `IPortfolioRepository` test fake that wraps an `AppDbContext`.

If creating a test fake is over-scope, **use the real `EfPortfolioRepository`** in the test — the goal is integration coverage anyway. Replace `EfPortfolioRepositoryTestFake(db)` with `EfPortfolioRepository(db)` directly.

- [ ] **Step 27.2: Rewrite the use case**

Delete `TradyStrat.Application/Trades/UseCases/LogTradeUseCase.cs.bak` (you've already inspected it for reference) and create the new file:

`TradyStrat.Application/Trades/UseCases/LogTradeUseCase.cs`:

```csharp
using Ardalis.Specification;
using TradyStrat.Application.Portfolio;
using TradyStrat.Application.Time;
using TradyStrat.Application.UseCases;
using TradyStrat.Domain;
using TradyStrat.Domain.Exceptions;
using TradyStrat.Domain.Portfolio;
using TradyStrat.Domain.Shared;

namespace TradyStrat.Application.Trades.UseCases;

public sealed record LogTradeInput(
    int       InstrumentId,
    DateOnly  ExecutedOn,
    TradeSide Side,
    decimal   QuantityValue,
    decimal   PriceValue,
    decimal   FeesValue,
    string?   Note);

public sealed class LogTradeUseCase(
    IPortfolioRepository portfolios,
    IReadRepositoryBase<Instrument> instruments,
    IClock clock,
    ILogger<LogTradeUseCase> log)
    : UseCaseBase<LogTradeInput, TradeRecorded>(log)
{
    protected override async Task<TradeRecorded> ExecuteCore(LogTradeInput input, CancellationToken ct)
    {
        var instrument = await instruments.GetByIdAsync(input.InstrumentId, ct)
            ?? throw new InstrumentNotFoundException(input.InstrumentId);
        var portfolio = await portfolios.GetAsync(ct);

        var instrumentCurrency = Currency.Parse(instrument.Currency);

        var quantity = Quantity.Of(input.QuantityValue);
        var price    = Price.Of(Money.Of(input.PriceValue, instrumentCurrency));
        var fees     = Money.Of(input.FeesValue, Currency.Eur);

        var result = portfolio.RecordTrade(
            new InstrumentId(instrument.Id),
            input.ExecutedOn, input.Side,
            quantity, price, fees,
            input.Note ?? "",
            clock.UtcNow());

        await portfolios.SaveAsync(portfolio, ct);
        return result;
    }
}
```

(`InstrumentNotFoundException` already exists in `TradyStrat.Domain/Exceptions/`.)

- [ ] **Step 27.3: Re-register in `TradesApplicationModule`**

The module still has `services.AddScoped<LogTradeUseCase>();` so no change needed if the file isn't `.bak`'d. Verify:

Run: `cat TradyStrat.Application/Trades/TradesApplicationModule.cs`
Expected: still registers `LogTradeUseCase`.

If module was `.bak`'d in Task 14, restore it now.

- [ ] **Step 27.4: Build + test**

Run: `dotnet build TradyStrat.Application TradyStrat.Application.Tests`
Expected: succeeds.

Run: `dotnet test TradyStrat.Application.Tests --filter "FullyQualifiedName~LogTradeUseCaseTests"`
Expected: pass.

- [ ] **Step 27.5: Commit**

```bash
git add TradyStrat.Application/Trades/UseCases/LogTradeUseCase.cs \
        TradyStrat.Application.Tests/Trades/LogTradeUseCaseTests.cs
rm -f TradyStrat.Application/Trades/UseCases/LogTradeUseCase.cs.bak
git add -A
git commit -m "feat(application): LogTradeUseCase uses IPortfolioRepository + Portfolio AR — Phase 2"
```

---

## Task 28: Rewrite `DeleteTradeUseCase`

**Files:**
- Restore: `TradyStrat.Application/Trades/UseCases/DeleteTradeUseCase.cs` with new shape

- [ ] **Step 28.1: Implement**

`TradyStrat.Application/Trades/UseCases/DeleteTradeUseCase.cs`:

```csharp
using TradyStrat.Application.Portfolio;
using TradyStrat.Application.UseCases;
using TradyStrat.Domain.Portfolio;
using TradyStrat.Domain.Shared;

namespace TradyStrat.Application.Trades.UseCases;

public sealed record DeleteTradeInput(int Id);

public sealed class DeleteTradeUseCase(
    IPortfolioRepository portfolios,
    ILogger<DeleteTradeUseCase> log)
    : UseCaseBase<DeleteTradeInput, TradeDeleted>(log)
{
    protected override async Task<TradeDeleted> ExecuteCore(DeleteTradeInput input, CancellationToken ct)
    {
        var portfolio = await portfolios.GetAsync(ct);
        var result = portfolio.DeleteTrade(new TradeId(input.Id));
        await portfolios.SaveAsync(portfolio, ct);
        return result;
    }
}
```

- [ ] **Step 28.2: Build**

Run: `dotnet build TradyStrat.Application`
Expected: succeeds.

- [ ] **Step 28.3: Commit**

```bash
git add TradyStrat.Application/Trades/UseCases/DeleteTradeUseCase.cs
rm -f TradyStrat.Application/Trades/UseCases/DeleteTradeUseCase.cs.bak
git add -A
git commit -m "feat(application): DeleteTradeUseCase uses Portfolio AR — Phase 2"
```

---

## Task 29: Rewrite `ImportTradesCsvUseCase`

**Files:**
- Restore: `TradyStrat.Application/Trades/UseCases/ImportTradesCsvUseCase.cs` with new shape

- [ ] **Step 29.1: Implement**

`TradyStrat.Application/Trades/UseCases/ImportTradesCsvUseCase.cs`:

```csharp
using Ardalis.Specification;
using TradyStrat.Application.Portfolio;
using TradyStrat.Application.Settings.Config;
using TradyStrat.Application.Settings.Specifications;
using TradyStrat.Application.Time;
using TradyStrat.Application.Trades;
using TradyStrat.Application.UseCases;
using TradyStrat.Domain;
using TradyStrat.Domain.Exceptions;
using TradyStrat.Domain.Portfolio;
using TradyStrat.Domain.Shared;

namespace TradyStrat.Application.Trades.UseCases;

public sealed record ImportTradesCsvInput(string CsvText);
public sealed record ImportTradesCsvResult(int RowsImported);

public sealed class ImportTradesCsvUseCase(
    IPortfolioRepository portfolios,
    IReadRepositoryBase<Instrument> instruments,
    IClock clock,
    ISettingsReader settings,
    ILogger<ImportTradesCsvUseCase> log)
    : UseCaseBase<ImportTradesCsvInput, ImportTradesCsvResult>(log)
{
    protected override async Task<ImportTradesCsvResult> ExecuteCore(
        ImportTradesCsvInput input, CancellationToken ct)
    {
        var focusTicker = await settings.FocusTickerAsync(ct);
        var focus = await instruments.FirstOrDefaultAsync(
            new InstrumentByTickerSpec(focusTicker), ct)
            ?? throw new CsvImportException(
                $"Focus instrument '{focusTicker}' is not registered.");

        var focusCurrency = Currency.Parse(focus.Currency);
        var rows = CsvImportService.Parse(new StringReader(input.CsvText));
        var now  = clock.UtcNow();

        var drafts = rows.Select(r => new TradeDraft(
            new InstrumentId(focus.Id),
            r.ExecutedOn, r.Side,
            Quantity.Of(r.Quantity),
            Price.Of(Money.Of(r.PricePerShare, focusCurrency)),
            Money.Of(r.FeesEur, Currency.Eur),
            r.Note ?? "")).ToList();

        var portfolio = await portfolios.GetAsync(ct);
        var results = portfolio.ImportTrades(drafts, now);
        await portfolios.SaveAsync(portfolio, ct);

        return new ImportTradesCsvResult(results.Count);
    }
}
```

- [ ] **Step 29.2: Build + check existing CsvImportServiceTests**

Run: `dotnet build TradyStrat.Application`
Expected: succeeds.

Run: `mv TradyStrat.Application.Tests/Trades/CsvImportServiceTests.cs.bak \
       TradyStrat.Application.Tests/Trades/CsvImportServiceTests.cs`

Inspect that test file: it likely uses the old `IRepositoryBase<Trade>` shape. **Rewrite** it to use the new use case shape (or delete cases that no longer make sense). The `CsvImportService.Parse` method itself probably hasn't changed; only the integration with the repository has.

If rewrite is large, scope it: keep only the `Parse`-only unit tests; delete or skip integration tests that hit the repository. Add a note to the commit message.

- [ ] **Step 29.3: Run tests**

Run: `dotnet test TradyStrat.Application.Tests --filter "FullyQualifiedName~CsvImportService"`
Expected: pass (after rewrite).

- [ ] **Step 29.4: Commit**

```bash
git add TradyStrat.Application/Trades/UseCases/ImportTradesCsvUseCase.cs \
        TradyStrat.Application.Tests/Trades/CsvImportServiceTests.cs
rm -f TradyStrat.Application/Trades/UseCases/ImportTradesCsvUseCase.cs.bak
git add -A
git commit -m "feat(application): ImportTradesCsvUseCase uses Portfolio.ImportTrades — Phase 2"
```

---

## Task 30: Update `LoadDashboardUseCase` + `BuildFocusDerivedSliceUseCase` + `PortfolioSection`

**Files:**
- Restore: `TradyStrat.Application/Dashboard/UseCases/LoadDashboardUseCase.cs`
- Restore: `TradyStrat.Application/Dashboard/UseCases/BuildFocusDerivedSliceUseCase.cs`
- Restore: `TradyStrat.Application/AiSuggestion/Snapshot/Sections/PortfolioSection.cs`

Bring the `.bak` files back online and rewrite the portfolio access paths. The rest of each use case (indicators, FX, suggestions, etc.) stays unchanged.

- [ ] **Step 30.1: Restore + rewrite `LoadDashboardUseCase`**

Run: `mv TradyStrat.Application/Dashboard/UseCases/LoadDashboardUseCase.cs.bak \
       TradyStrat.Application/Dashboard/UseCases/LoadDashboardUseCase.cs`

Edit the file:

1. Remove the `PortfolioService portfolio,` and `GrowthSeriesBuilder growth,` constructor params.
2. Add `IPortfolioRepository portfolios,` constructor param.
3. Remove `IReadRepositoryBase<Trade> tradeRepo,` — no longer needed; portfolio owns trades.
4. In the body, replace `await portfolio.SnapshotAsync(...)` calls with:

```csharp
var pf = await portfolios.GetAsync(ct);
var instrumentById = ordered.ToDictionary(i => new InstrumentId(i.Id), i => i);
var priceByInstrument = /* build from existing focus/context price resolution code */;
var snapshot = pf.Snapshot(instrumentById, priceByInstrument, Money.Of(goal.TargetEur, Currency.Eur));
```

5. Replace `await growth.BuildAsync(focusTicker, ct)` with:

```csharp
var barsByInstrument = /* dictionary of InstrumentId → IReadOnlyList<PriceBar> built from priceRepo */;
var growthSeries = pf.GrowthSeries(barsByInstrument);
```

This task requires reading the full `LoadDashboardUseCase.cs.bak` source carefully and making focused, surgical edits. **Estimated 15-30 minutes**, larger than other tasks. If the diff is sprawling, split into separate sub-commits per edit.

- [ ] **Step 30.2: Build**

Run: `dotnet build TradyStrat.Application`
Expected: succeeds. Any compile errors point at incomplete edits — finish them.

- [ ] **Step 30.3: Restore + rewrite `BuildFocusDerivedSliceUseCase`**

Run: `mv TradyStrat.Application/Dashboard/UseCases/BuildFocusDerivedSliceUseCase.cs.bak \
       TradyStrat.Application/Dashboard/UseCases/BuildFocusDerivedSliceUseCase.cs`

Same pattern: swap `PortfolioService`/`GrowthSeriesBuilder` for `IPortfolioRepository`. The slice produces a focused view; it likely calls one Snapshot variant.

- [ ] **Step 30.4: Restore + rewrite `PortfolioSection`**

Run: `mv TradyStrat.Application/AiSuggestion/Snapshot/Sections/PortfolioSection.cs.bak \
       TradyStrat.Application/AiSuggestion/Snapshot/Sections/PortfolioSection.cs`

Replace the body:

```csharp
using TradyStrat.Application.Portfolio;
using TradyStrat.Application.Settings.UseCases;
using TradyStrat.Application.UseCases;
using TradyStrat.Domain;
using TradyStrat.Domain.Portfolio;
using TradyStrat.Domain.Shared;

namespace TradyStrat.Application.AiSuggestion.Snapshot.Sections;

public sealed class PortfolioSection(
    IPortfolioRepository portfolios,
    ListInstrumentsUseCase listInstruments) : ISnapshotSectionProvider
{
    public int Order => 30;

    public async Task ContributeAsync(SnapshotBuilder builder, int instrumentId, DateOnly asOf, CancellationToken ct)
    {
        if (builder.Goal is null)
            throw new InvalidOperationException("GoalSection must run before PortfolioSection");

        var instruments = await listInstruments.ExecuteAsync(Unit.Value, ct);
        var instrumentById = instruments.ToDictionary(i => new InstrumentId(i.Id), i => i);

        var priceMap = new Dictionary<InstrumentId, Price>();
        foreach (var inst in instruments.Where(i => i.Kind == InstrumentKind.Held))
        {
            var ctx = builder.Tickers.SingleOrDefault(t => t.Ticker == inst.Ticker);
            var priceEur = ctx?.PriceEur ?? ctx?.PriceNative ?? 0m;
            priceMap[new InstrumentId(inst.Id)] =
                Price.Of(Money.Of(priceEur, Currency.Eur));
        }

        var portfolio = await portfolios.GetAsync(ct);
        builder.Portfolio = portfolio.SnapshotAsOf(
            asOf, instrumentById, priceMap,
            Money.Of(builder.Goal.TargetEur, Currency.Eur));
    }
}
```

- [ ] **Step 30.5: Build full solution**

Run: `dotnet build TradyStrat.slnx`
Expected: succeeds across all projects. Any consumer (Razor pages, MCP tools) referencing the old types must be updated. Common follow-ups:

- Razor pages that destructure `PortfolioSnapshot.CurrentValueEur` etc. — these are now `Money` not `decimal`. Add `.Amount` at the projection point (`snap.CurrentValueEur.Amount`) — that's a temporary presentation-layer adaptation; will tidy up when the dashboard view-model rewrite lands (spec §13.1).

- [ ] **Step 30.6: Commit**

```bash
git add -A
rm -f TradyStrat.Application/Dashboard/UseCases/LoadDashboardUseCase.cs.bak \
      TradyStrat.Application/Dashboard/UseCases/BuildFocusDerivedSliceUseCase.cs.bak \
      TradyStrat.Application/AiSuggestion/Snapshot/Sections/PortfolioSection.cs.bak
git add -A
git commit -m "feat(application): dashboard + AI snapshot section consume IPortfolioRepository — Phase 2"
```

---

## Task 31: Delete `PortfolioService` + `GrowthSeriesBuilder`

**Files:**
- Delete: `TradyStrat.Application/Portfolio/PortfolioService.cs.bak`
- Delete: `TradyStrat.Application/Portfolio/GrowthSeriesBuilder.cs.bak`

These have been off the compile path since Task 14. Final removal.

- [ ] **Step 31.1: Delete the files**

```bash
rm -f TradyStrat.Application/Portfolio/PortfolioService.cs.bak \
      TradyStrat.Application/Portfolio/GrowthSeriesBuilder.cs.bak
```

- [ ] **Step 31.2: Verify build + test**

Run: `dotnet build TradyStrat.slnx`
Expected: succeeds.

Run: `dotnet test TradyStrat.slnx --no-build`
Expected: all pass.

- [ ] **Step 31.3: Commit**

```bash
git add -A
git commit -m "chore(application): delete PortfolioService and GrowthSeriesBuilder — Phase 2 cleanup"
```

---

## Task 32: Migrate `PortfolioService*Tests` → `Domain.Tests/Portfolio`

**Files:**
- Move: `TradyStrat.Infrastructure.Tests/Portfolio/PortfolioServiceTests.cs.bak` → `TradyStrat.Domain.Tests/Portfolio/PortfolioServiceParityTests.cs`
- Move: `PortfolioServiceAsOfTests.cs.bak` → `Domain.Tests/Portfolio/PortfolioSnapshotAsOfParityTests.cs`
- Move: `PortfolioServiceMultiTickerTests.cs.bak` → `Domain.Tests/Portfolio/PortfolioMultiTickerParityTests.cs`

The regression contract from Task 11. Each test rewrites against `Portfolio.Snapshot`/`SnapshotAsOf` but the **numbers must remain identical**.

- [ ] **Step 32.1: Move + rename the files**

```bash
mkdir -p TradyStrat.Domain.Tests/Portfolio
git mv TradyStrat.Infrastructure.Tests/Portfolio/PortfolioServiceTests.cs.bak \
       TradyStrat.Domain.Tests/Portfolio/PortfolioServiceParityTests.cs
git mv TradyStrat.Infrastructure.Tests/Portfolio/PortfolioServiceAsOfTests.cs.bak \
       TradyStrat.Domain.Tests/Portfolio/PortfolioSnapshotAsOfParityTests.cs
git mv TradyStrat.Infrastructure.Tests/Portfolio/PortfolioServiceMultiTickerTests.cs.bak \
       TradyStrat.Domain.Tests/Portfolio/PortfolioMultiTickerParityTests.cs
```

- [ ] **Step 32.2: Rewrite the helpers and assertions**

Each file currently:
- Uses `PortfolioService` directly → replace with `Portfolio.Empty(PortfolioId.Singleton)` + `RecordTrade` per fixture.
- Constructs `Trade` with the old anemic shape → use `Trade.Create(...)` via `Portfolio.RecordTrade(...)`.
- Calls `snap.Shares` and `snap.CurrentValueEur` (raw decimal) → keep the assertion shape if comparing decimals, but `CurrentValueEur` is now `Money` so unwrap with `.Amount`.

Template rewrite (apply to each file):

```csharp
using Shouldly;
using TradyStrat.Domain;
using TradyStrat.Domain.Portfolio;
using TradyStrat.Domain.Shared;
using Xunit;

namespace TradyStrat.Domain.Tests.Portfolio;

public class PortfolioServiceParityTests
{
    private static readonly DateTime _now = new(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc);
    private static readonly InstrumentId _iid = new(1);

    private static Portfolio Buy(Portfolio p, int day, decimal qty, decimal price, decimal fees = 0m)
    {
        p.RecordTrade(_iid, new DateOnly(2026, 1, day), TradeSide.Buy,
            Quantity.Of(qty), Price.Of(Money.Of(price, Currency.Eur)),
            Money.Of(fees, Currency.Eur), "", _now);
        return p;
    }
    private static Portfolio Sell(Portfolio p, int day, decimal qty, decimal price, decimal fees = 0m)
    {
        p.RecordTrade(_iid, new DateOnly(2026, 1, day), TradeSide.Sell,
            Quantity.Of(qty), Price.Of(Money.Of(price, Currency.Eur)),
            Money.Of(fees, Currency.Eur), "", _now);
        return p;
    }

    private static (IReadOnlyDictionary<InstrumentId, Instrument>, IReadOnlyDictionary<InstrumentId, Price>)
        Map(decimal priceEur, string ticker = "CON3.L", string currency = "USD")
    {
        var inst = new Instrument
        {
            Id = 1, Ticker = ticker, Name = ticker, Currency = currency,
            Exchange = "X", TimezoneId = "UTC", Kind = InstrumentKind.Held,
            AddedAt = DateTime.UtcNow,
        };
        return (
            new Dictionary<InstrumentId, Instrument> { [_iid] = inst },
            new Dictionary<InstrumentId, Price> { [_iid] = Price.Of(Money.Of(priceEur, Currency.Eur)) }
        );
    }

    [Fact]
    public void Empty_trade_log_returns_zero_snapshot()
    {
        var p = Portfolio.Empty(PortfolioId.Singleton);
        var (insts, prices) = Map(5m);
        var snap = p.Snapshot(insts, prices, Money.Of(1_000_000m, Currency.Eur));

        snap.Positions.Count.ShouldBe(0);
        snap.Shares.ShouldBe(0m);
        snap.CurrentValueEur.Amount.ShouldBe(0m);
        snap.UnrealizedPnLEur.Amount.ShouldBe(0m);
        snap.RealizedPnLEur.Amount.ShouldBe(0m);
        snap.ProgressPct.ShouldBe(0m);
    }

    // ... rewrite each remaining [Fact] preserving numeric expectations ...
}
```

The number of test methods per file matches the `.bak` originals — work through them one by one and copy the assertions. **Do not change any numeric expectation.** The point of this task is that today's numbers hold against the new domain.

- [ ] **Step 32.3: Build + run**

Run: `dotnet build TradyStrat.Domain.Tests`
Expected: succeeds.

Run: `dotnet test TradyStrat.Domain.Tests --filter "FullyQualifiedName~Parity"`
Expected: every parity test passes.

If any test fails: the regression contract is violated — investigate and fix the *Domain* code (not the test). The test fixtures are the contract.

- [ ] **Step 32.4: Commit**

```bash
git add -A
git commit -m "test(domain): migrate PortfolioService*Tests → Domain.Tests/Portfolio (regression parity) — Phase 2"
```

---

## Task 33: Add `PortfolioInvariantsTests`

**Files:**
- Create: `TradyStrat.Domain.Tests/Portfolio/PortfolioInvariantsTests.cs`

New tests that only make sense post-AR: invariants that the old `PortfolioService` couldn't enforce (because it didn't own the data).

- [ ] **Step 33.1: Write tests**

```csharp
using Shouldly;
using TradyStrat.Domain.Exceptions;
using TradyStrat.Domain.Portfolio;
using TradyStrat.Domain.Shared;
using Xunit;

namespace TradyStrat.Domain.Tests.Portfolio;

public class PortfolioInvariantsTests
{
    private static readonly DateTime _now = new(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void RecordTrade_with_zero_quantity_throws_at_factory()
    {
        var p = Portfolio.Empty(PortfolioId.Singleton);
        Should.Throw<TradeValidationException>(() =>
            p.RecordTrade(new InstrumentId(1), new DateOnly(2026, 1, 1), TradeSide.Buy,
                Quantity.Of(0m), Price.Of(Money.Of(4m, Currency.Eur)),
                Money.Zero(Currency.Eur), "", _now));
    }

    [Fact]
    public void RecordTrade_with_None_quantity_throws()
    {
        var p = Portfolio.Empty(PortfolioId.Singleton);
        Should.Throw<TradeValidationException>(() =>
            p.RecordTrade(new InstrumentId(1), new DateOnly(2026, 1, 1), TradeSide.Buy,
                Quantity.None, Price.Of(Money.Of(4m, Currency.Eur)),
                Money.Zero(Currency.Eur), "", _now));
    }

    [Fact]
    public void RecordTrade_with_empty_price_throws()
    {
        var p = Portfolio.Empty(PortfolioId.Singleton);
        Should.Throw<TradeValidationException>(() =>
            p.RecordTrade(new InstrumentId(1), new DateOnly(2026, 1, 1), TradeSide.Buy,
                Quantity.Of(10m), Price.None(Currency.Eur),
                Money.Zero(Currency.Eur), "", _now));
    }

    [Fact]
    public void Sell_exceeding_open_lots_throws_per_position()
    {
        var p = Portfolio.Empty(PortfolioId.Singleton);
        p.RecordTrade(new InstrumentId(1), new DateOnly(2026, 1, 1), TradeSide.Buy,
            Quantity.Of(5m), Price.Of(Money.Of(4m, Currency.Eur)),
            Money.Zero(Currency.Eur), "", _now);

        Should.Throw<TradeValidationException>(() =>
            p.RecordTrade(new InstrumentId(1), new DateOnly(2026, 1, 5), TradeSide.Sell,
                Quantity.Of(10m), Price.Of(Money.Of(5m, Currency.Eur)),
                Money.Zero(Currency.Eur), "", _now));
    }

    [Fact]
    public void DeleteTrade_unknown_id_throws()
    {
        var p = Portfolio.Empty(PortfolioId.Singleton);
        Should.Throw<TradeValidationException>(() => p.DeleteTrade(new TradeId(999)));
    }
}
```

- [ ] **Step 33.2: Build + run**

Run: `dotnet test TradyStrat.Domain.Tests --filter "FullyQualifiedName~PortfolioInvariantsTests"`
Expected: pass.

- [ ] **Step 33.3: Commit**

```bash
git add TradyStrat.Domain.Tests/Portfolio/PortfolioInvariantsTests.cs
git commit -m "test(domain): PortfolioInvariantsTests for AR-only invariants — Phase 2"
```

---

## Task 34: Move trade `Specifications` to Infrastructure

**Files:**
- Move: `TradyStrat.Application/Trades/Specifications/*.cs` → `TradyStrat.Infrastructure/Trades/Specifications/*.cs`

Per spec §3: Specifications stay but become an Infrastructure-side query-construction detail.

- [ ] **Step 34.1: Inspect what's left after the use case rewrites**

Run: `ls TradyStrat.Application/Trades/Specifications/`
Expected: 5 spec files (`AllTradesSpec.cs`, `EarliestTradeSpec.cs`, `TradesAsOfSpec.cs`, `TradesByDateRangeSpec.cs`, `TradesOnInstrumentInWindowSpec.cs`).

After Tasks 27-30, the use cases no longer reference `IRepositoryBase<Trade>`. Check whether any *other* code still uses these specs:

Run: `grep -rn 'AllTradesSpec\|TradesAsOfSpec\|EarliestTradeSpec\|TradesByDateRangeSpec\|TradesOnInstrumentInWindowSpec' --include='*.cs' .`

Two outcomes:

A. **No consumers remain** (specs are now dead code). Delete them.

B. **Some consumers remain** (e.g. `AiSnapshotService.RecentTradesSection` still queries trades directly). Move the relevant specs to Infrastructure and update the consumers to use a new port instead of `IReadRepositoryBase<Trade>` — or keep the `IReadRepositoryBase<Trade>` port temporarily (it's not an AR; reads of historical trades for the AI snapshot can use it). If keeping, just move the spec files.

- [ ] **Step 34.2: Apply outcome A or B**

If A:
```bash
git rm TradyStrat.Application/Trades/Specifications/*.cs
```

If B:
```bash
mkdir -p TradyStrat.Infrastructure/Trades/Specifications
git mv TradyStrat.Application/Trades/Specifications/*.cs TradyStrat.Infrastructure/Trades/Specifications/
# Update namespaces in each file: TradyStrat.Application.Trades.Specifications → TradyStrat.Infrastructure.Trades.Specifications
# Update `using` statements in any remaining consumer.
```

- [ ] **Step 34.3: Build + test**

Run: `dotnet build TradyStrat.slnx && dotnet test TradyStrat.slnx --no-build`
Expected: green.

- [ ] **Step 34.4: Commit**

```bash
git add -A
git commit -m "chore: move Trade Specifications to Infrastructure (or delete unused) — Phase 2"
```

---

## Task 35: Final verification — full suite + smoke test

- [ ] **Step 35.1: Full clean build**

Run:
```bash
dotnet clean TradyStrat.slnx
dotnet build TradyStrat.slnx
```
Expected: succeeds with no warnings.

- [ ] **Step 35.2: Full test suite**

Run: `dotnet test TradyStrat.slnx --no-build`
Expected: every test passes. Compare the test count to the Phase 1 checkpoint — the count should have grown by ~15 Portfolio tests minus any tests deleted during the use case rewrites.

- [ ] **Step 35.3: Grep for forbidden references**

```bash
# No production code should reference the deleted classes.
grep -rn 'PortfolioService\|GrowthSeriesBuilder' --include='*.cs' --include='*.razor*' . \
  | grep -v 'docs/' \
  | grep -v 'Tests/' \
  | grep -v 'PortfolioServiceParityTests'   # the parity test class name is OK
```
Expected: no results outside docs/tests.

```bash
# Position should not store Ticker or Currency snapshots.
grep -n 'Ticker\|InstrumentCurrency' TradyStrat.Domain/Portfolio/Position.cs
```
Expected: no matches.

- [ ] **Step 35.4: Run the app, render the dashboard**

Apply the migration to the (already backed-up) dev DB:

```bash
dotnet ef database update --project TradyStrat.Infrastructure --startup-project TradyStrat
```

Run: `dotnet run --project TradyStrat`

Open http://127.0.0.1:5180 in a browser. Verify:
- The dashboard loads without errors.
- Hero capital, growth chart, and per-ticker zone cards show the same numbers they did before this rework (compare against a screenshot taken from `main` baseline if possible).
- Trades page lists the same trade history.
- Adding a new trade through the UI succeeds.
- Deleting the new trade succeeds.
- Settings page still functions.

**If any number differs from baseline:** the parity tests (Task 32) should have caught it. Either:
- A test fixture didn't cover this case → add the missing fixture + fix the domain.
- The Razor presentation layer is destructuring `Money` incorrectly → fix the unwrap (`.Amount` etc.).

Stop the app: `Ctrl-C`.

- [ ] **Step 35.5: Phase 2 checkpoint tag**

```bash
git tag -a phase2-portfolio-ar-done -m "Phase 2 (Portfolio AR) complete: FIFO + realized P&L in domain; PortfolioService deleted; behavioral parity preserved"
```

- [ ] **Step 35.6: Open PR**

```bash
git push -u origin worktree-ddd-phase1-2
gh pr create --title "DDD rework Phase 1+2 — shared kernel + Portfolio aggregate" --body "$(cat <<'EOF'
## Summary
- **Phase 1 (kernel seed):** introduces Money, Currency, Ticker, Quantity, Price, DateRange VOs + six strongly typed IDs in `TradyStrat.Domain/Shared/`. EF ValueConverter conventions wired but dormant.
- **Phase 2 (Portfolio AR):** `Portfolio` aggregate owns `Position` child entities, each owning `Lot` + `Trade`. FIFO lot accounting, fee folding, and realized P&L moved from `PortfolioService.BuildSnapshot` to `Position.Record`. `PortfolioService.cs` and `GrowthSeriesBuilder.cs` deleted. `IPortfolioRepository` replaces generic `IRepositoryBase<Trade>` for AR access. Cross-aggregate data (instrument metadata, goal target) flows in via `Snapshot(...)` parameters resolved by use cases — `Position` holds only `InstrumentId`.
- **Schema migration:** adds `Portfolios`, `Positions`, `PositionLots` tables and `Trades.PositionId`. Existing `Trades.InstrumentId` kept as denormalized column (spec §13.1). Backfill via SQL in the migration `Up`. Post-migration first read triggers idempotent lot rehydration in `EfPortfolioRepository.GetAsync`.

## Regression contract
The three `PortfolioService*Tests` files (209 lines total) migrated to `TradyStrat.Domain.Tests/Portfolio/*ParityTests.cs`. Every numeric expectation preserved. Plus new `PortfolioInvariantsTests` for AR-only checks.

## Test plan
- [ ] `dotnet test TradyStrat.slnx` is green
- [ ] Dashboard hero capital + growth chart show identical numbers vs `main` baseline
- [ ] Adding a trade through the UI persists correctly
- [ ] Deleting a trade through the UI replays remaining FIFO state correctly
- [ ] CSV import succeeds and creates a `Position` if none exists
- [ ] AI snapshot's portfolio section reflects the same shape as before

🤖 Generated with [Claude Code](https://claude.com/claude-code)
EOF
)"
```

---

# Plan summary

**Phase 1 (Tasks 1-10):** new files only; shared kernel VOs + strongly typed IDs + dormant EF conventions. ~10 small commits.

**Phase 2 (Tasks 11-35):** Portfolio aggregate vertical slice. ~25 commits. Big inflection point at Task 14 (`.bak` files take the legacy use cases offline) and Task 30 (use cases come back online wired to the AR).

**Regression contract:** Tasks 11 → 32. Every numeric expectation in today's `PortfolioService*Tests` becomes a parity test in `Domain.Tests/Portfolio/`.

**Behavioral guarantee:** Task 35 dashboard smoke test confirms identical numbers vs `main`.

---

## Self-review notes

After writing this plan, the following risks were identified and called out inline:

- Task 14 takes ~5 files offline as `.bak` to keep the Domain layer compilable while the Application/Infrastructure consumers wait their turn. This is unusual; the plan clarifies why and tracks restoration through Tasks 27-30.
- Task 24 (EF migration) requires judgement on SQLite ALTER TABLE limitations — if auto-generated migration rebuilds `Trades`, the engineer must hand-edit the migration to preserve data. The plan tells the engineer to **stop and ask** if uncertain.
- Task 26 references `InMemoryDb.SameStore` which doesn't exist today — the plan tells the engineer to add it as a 5-line TestKit helper.
- Task 30 (use case restoration) is the largest task; the plan acknowledges it's 15-30 minutes and may need sub-commits.
- Task 32 (parity tests) is the contract — the plan emphasizes "do not change any numeric expectation."

Spec coverage check (against `docs/superpowers/specs/2026-05-22-ddd-domain-rework-design.md`):
- §6 Phase 1 — Tasks 1-10 ✓
- §7 Phase 2 — Tasks 11-35 ✓ (every Phase 2 element from §7 has a task)
- §2.1 trade-offs preserved (singleton Portfolio, by-ID references, EF reflection access) ✓
- §13.1 temporary violations preserved (Trades.InstrumentId, PortfolioSnapshot legacy scalars) ✓
- §15 acceptance criteria — verified in Task 35 ✓

Phases 3-6 are out of scope; each gets its own plan when its time comes.

