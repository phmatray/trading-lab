# TradingSignalPoc

A .NET 10 proof-of-concept that generates BUY/SELL/HOLD signals for a single crypto asset
using a locally-hosted LLM (LM Studio), and validates those signals through a **walk-forward
backtest** with two stacked adaptation mechanisms:

1. A confidence-threshold filter (τ* picked per segment on the adaptation window).
2. An ML.NET `LbfgsLogisticRegression` meta-model that predicts P(signal is profitable net of fees).

The LLM weights are frozen — all "learning" happens in the layers around it.

> This is a **simulation / recommendation** tool. It never places real orders and uses only
> public market data (Binance klines, no API key). A favorable walk-forward result is necessary
> but not sufficient to risk real capital — forward paper-trading would be the next gate.

## Non-negotiable principles

1. **No look-ahead bias.** At candle `i`, the system only sees `candles[0..i]`. Two regression
   tests guard this: a feature-level invariant in `Indicators.Tests` and an orchestrator-level
   test in `Backtest.Tests` (whole-list + per-index truncation variants).
2. **Adaptation and evaluation use disjoint data.** Walk-forward windows are configured so each
   segment's test window is OOS to its fit window.
3. **Transaction costs are always applied.** Default 10 bps taker fee.
4. **Predictions are logged before outcomes are known.** Two-phase write to SQLite.
5. **The baseline to beat is buy-and-hold over the identical period.** Always reported.

## Prerequisites

- .NET 10 SDK (the build targets `net10.0`; spec originally said `net8.0` but `Directory.Build.props` was updated)
- [LM Studio](https://lmstudio.ai/) running locally on `http://localhost:1234/v1` with a 14B-class
  instruct model loaded (default expected: `qwen2.5-14b-instruct`, configurable in `appsettings.json`)

## Quick start

```bash
# 1. Cache 2 years of BTCUSDT 1h data (idempotent — second run is a cache hit)
dotnet run --project src/TradingSignal.Console -- ingest

# 2. Run the full walk-forward backtest against LM Studio
#    (Long-running — 17k candles × multiple strategies. The LLM response cache makes
#    re-runs fast.)
dotnet run --project src/TradingSignal.Console -- run

# 3. Reprint the latest report
dotnet run --project src/TradingSignal.Console -- report
```

Reports land in `./runs/report.json`; the prediction store in `./runs/predictions.db`;
the LLM response cache in `./runs/llm-cache.db`; market data in `./data-cache/`.

## What the report shows

```
strategy                          trades   accuracy     brier    sharpe     max DD     cum ret
------------------------------------------------------------------------------------------
buy-and-hold                           -          -         -    +0.420     -25.31%   +120.50%
llm-only                              98     54.10%     0.241   +0.180     -32.10%     +5.20%
+threshold(τ*=0.65)                   42     58.30%     0.205   +0.310     -18.42%    +12.71%
+threshold+meta(τ*=0.65)              28     62.00%     0.189   +0.450     -11.20%    +18.34%
```

Each strategy is also broken out per walk-forward segment with the chosen τ\* and in-sample
meta-model accuracy, so you can see how those drift over time.

## Solution layout

```
src/
  TradingSignal.Core            domain models + abstractions (no deps)
  TradingSignal.Data            Binance ingestion + disk cache + validation
  TradingSignal.Indicators      Skender wrapper → FeatureSet (slice-before-compute)
  TradingSignal.Llm             LM Studio chat client + JSON-schema structured output + parser + cache
  TradingSignal.Evaluation      prediction store + outcome computer + metrics
  TradingSignal.Adaptation      threshold optimizer + ML.NET meta-model + composite
  TradingSignal.Backtest        walk-forward orchestrator + long/flat portfolio sim
  TradingSignal.Console         CLI: ingest / run / report
tests/
  TradingSignal.Indicators.Tests
  TradingSignal.Data.Tests
  TradingSignal.Llm.Tests
  TradingSignal.Evaluation.Tests
  TradingSignal.Backtest.Tests
  TradingSignal.Adaptation.Tests
```

Dependencies flow Core → everything; Backtest depends on Data/Indicators/Llm/Evaluation/Adaptation;
Console depends only on Core + Backtest.

## Configuration

`src/TradingSignal.Console/appsettings.json` (copied to output on build) controls:

- `LmStudio` — endpoint, model id, max few-shot, max output tokens
- `Market` — symbol, interval (`1h`, `30m`, `1d`, ...), history days
- `Fees` — taker bps
- `WalkForward` — adaptation/test/step days, evaluation horizon (candles)
- `Adaptation` — toggle threshold and meta-model layers independently
- `Output` — paths for `data-cache/`, `predictions.db`, `llm-cache.db`, `report.json`

Environment overrides via `TSIG_` prefix (e.g. `TSIG_LmStudio__ModelId=...`).

## Tests

```bash
dotnet test TradingSignalPoc.slnx
```

85 tests total. The two highest-value tests are the look-ahead regression guards:

- `LookAheadInvariantTests.Compute_At_Index_Is_Identical_Whether_Future_Candles_Are_Present_Or_Truncated`
  — `FeatureEngine.Compute` must return the same `FeatureSet` whether the candle list ends at
  `i` or extends past it.
- `LookAheadRegressionTests.First_Segment_Decisions_Identical_With_Or_Without_Future_Candles`
  and `Per_Index_Truncation_Yields_Same_Decisions_As_Full_Candle_List` — the orchestrator
  itself never reads beyond the decision index.

If either ever fails, the backtest's verdict is invalid.

## Out of scope (not built)

- Reinforcement learning, LLM fine-tuning, or weight training of any kind.
- Live or paper order execution against an exchange.
- Multi-asset portfolios, leverage, derivatives.
- Web UI / dashboard — the JSON report is enough for charting later.
