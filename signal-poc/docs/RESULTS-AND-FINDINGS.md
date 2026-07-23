# TradingSignalPoc — Results & Findings

**Status: concluded (paused).** The core hypothesis did not hold. This document records what the
application does, how it was tested, what the results were, why they came out the way they did
(with supporting literature), and what is worth keeping.

_Last updated: 2026-06-04._

---

## TL;DR

We built and rigorously backtested an LLM-driven trading bot that decides **BUY / SELL / HOLD** each
candle from technical indicators. Over a **2-year, 18-segment, walk-forward** backtest on BTCUSDT (4h
candles, a local 35B reasoning model, 3,240 predictions per strategy):

> **The model showed no demonstrable directional skill.** On the 497 trades it actually took, it was
> right **47.3 % of the time** — statistically indistinguishable from a coin flip (and slightly below
> it). Its confidence scores were uncalibrated, and the only reason it edged out buy-and-hold on paper
> was **low market exposure** (it sat in cash ~85 % of the time, dodging drawdowns) plus **two lucky
> segments out of eighteen**.

The earlier promising number that motivated the deep dive — "67 % accuracy, Sharpe 11.5, +2.21 %" —
came from **3 trades on a 10-day window**. At N=497 it regressed to noise. The validation gate did
exactly what it was supposed to: it told us the apparent edge was luck.

A two-pass, adversarially-verified literature review confirmed the result is **expected**, not a bug:
an LLM consuming only price-derived technical indicators has **no evidence-supported path to a
net-of-fee edge**. The documented edges live in *different data* (order flow, funding rates, news
text) and/or *different horizons*.

We are stopping here. The lasting value is a **trustworthy backtesting harness** (proper walk-forward,
no look-ahead, real fees, full result provenance) and a clear, evidence-based map of where a real edge
would and would not be found.

---

## 1. What the application does

A .NET 10 console app (`src/TradingSignal.Console`) that runs a **walk-forward backtest** of an
LLM-based trading strategy:

1. **Ingest** — cache OHLCV candles from Binance for a symbol/interval (e.g. BTCUSDT 4h).
2. **Feature engineering** (`TradingSignal.Indicators`) — compute per-candle indicators:
   RSI(14), MACD line/signal/histogram, EMA(20)/EMA(50), ADX(14), ATR(14), volume ratio, 1- & 5-bar
   returns, plus pre-computed categorical features (EMA cross, price-vs-EMAs).
3. **Signal generation** (`TradingSignal.Llm`) — a local LLM (LM Studio) is given the indicator
   snapshot and a disciplined system prompt, and must return one JSON object:
   `{ action: BUY|SELL|HOLD, confidence: 0..1, reason: string }`. Two call strategies exist
   (instruct and reasoning); the final runs used the reasoning model `qwen/qwen3.6-35b-a3b`.
4. **Adaptation overlays** (`TradingSignal.Adaptation`) — three strategies are evaluated side by side:
   - `llm-only` — raw model output.
   - `+threshold` — only act when confidence ≥ τ\*, where τ\* is chosen per segment to maximise
     Sharpe on the adaptation window.
   - `+threshold+meta` — adds a meta-model (logistic regression) that gates trades by predicted
     P(profitable).
5. **Walk-forward orchestration** (`TradingSignal.Backtest`) — for each segment: fit overlays on an
   adaptation window, then trade out-of-sample on the following test window; step forward; repeat.
   Execution at next-bar open, outcome evaluated at a fixed horizon, fees applied.
6. **Evaluation & reporting** (`TradingSignal.Evaluation`) — accuracy, Brier score, annualized Sharpe,
   max drawdown, cumulative return per strategy and per segment, plus a buy-and-hold benchmark.
   Every prediction (signal, confidence, reasoning trace, features, outcome) is persisted to
   `runs/predictions.db` with a `run_id` and `strategy` label.

### Methodology safeguards

The whole point of this project was to get a **trustworthy** answer, so the harness enforces:

- **Walk-forward** segmentation with strictly disjoint adaptation/test windows (no peeking forward).
- **No look-ahead** in features or few-shot examples (covered by invariant tests).
- **Real transaction fees** (10 bps taker, 20 bps round-trip).
- **Full provenance** — `run_id` + `strategy` columns + a `runs` metadata table so any result is
  traceable to the model, window, and code revision that produced it.
- A standalone **rule-violation linter** (`analysis/trace_mine.py`) and a **conditional-edge
  analyzer** (`analysis/conditional_edge.py`) for post-hoc auditing.

---

## 2. The headline backtest

**Config:** BTCUSDT, 4h candles, 730 days (2024-05-28 → 2026-05-27), `qwen/qwen3.6-35b-a3b`
(reasoning, medium effort), 3 strategies, 18 walk-forward segments, 3,240 predictions per strategy,
~53 h wall-clock on local hardware.

| strategy            | trades | accuracy | Brier | Sharpe | max DD   | cum ret |
|---------------------|-------:|---------:|------:|-------:|---------:|--------:|
| buy-and-hold        |      — |        — |     — |  0.328 | −49.84 % |  +9.18 %|
| llm-only            |    114 |  47.28 % | 0.302 |  0.432 | −21.40 % | +13.43 %|
| +threshold(τ\*=0.80)|      0 |  40.00 % | 0.310 |  0.000 |   0.00 % |  0.00 % |
| +threshold+meta     |      0 |   0.00 % | 0.310 |  0.000 |   0.00 % |  0.00 % |

At first glance `llm-only` *beats* buy-and-hold: higher return, half the drawdown, higher Sharpe.
**That is not skill — it is luck plus low exposure.** See the next section.

> Note on "trades": 114 = portfolio position changes; the model emitted **497 non-HOLD signals**
> (276 BUY, 221 SELL) out of 3,240 — i.e. it chose to HOLD ~85 % of the time.

---

## 3. Why it's not an edge

### 3.1 Directional accuracy is a coin flip (slightly worse)

On the 497 non-HOLD calls, the model's direction was correct **235 times = 47.3 %**.

- 95 % confidence interval: **[42.9 %, 51.7 %]** — it *contains* 50 %. z ≈ −1.2 (two-sided p ≈ 0.22).
  We cannot reject "no skill," and the point estimate is *below* a coin flip.
- BUY: 49.3 % hit, **−0.13 %** average return. SELL: 44.8 % hit, **−0.23 %** average return.
  Both directions lost money on average per trade.

### 3.2 The return came from 2 lucky segments

Per-segment cumulative return split **9 positive / 9 negative**. The entire +13.4 % total came from
**two of eighteen segments** (seg 5: +15.4 %, seg 16: +15.9 %); the other sixteen netted negative.
Remove those two and the strategy is underwater. That is the textbook signature of luck, not a
repeatable edge.

### 3.3 Confidence is uncalibrated

If the model knew when it was right, accuracy would rise with confidence. It doesn't:

| confidence bucket | hit rate |
|-------------------|---------:|
| 0.50–0.60         |   48.1 % |
| 0.60–0.65         |   49.2 % |
| 0.65–0.70         | **34.5 %** |
| ≥ 0.70            |   50.0 % |

Non-monotonic, with the second-highest bucket the *worst* (Brier 0.302, worse than an always-0.5
baseline). The confidence number carries no information.

### 3.4 No conditional edge either

Slicing the 497 trades by confidence, trend strength (ADX), RSI band, volume, and trend alignment
(`analysis/conditional_edge.py`): **no sub-condition is significantly above 50 %.** The only two
statistically significant buckets are *below* 50 % (moderate-confidence trades and the RSI 30–45 band
were reliably *wrong*).

### 3.5 The adaptive overlays agreed: don't trade

Both `+threshold` and `+threshold+meta` independently gated **~all** signals to HOLD (0 trades). The
threshold optimizer, given the model's signals, found **no confidence level worth trading on** — an
independent confirmation that there was nothing to exploit.

### 3.6 What the model *did* do well

Across all **9,720 predictions**, the linter found **zero violations** of the prompt's hard rules
(no buying into bearish crosses, HOLD-cap respected, etc.). The prompt engineering held perfectly —
the model followed instructions faithfully. It simply had no predictive signal to apply them to.

---

## 4. Why this was expected — the literature

Two independent, adversarially-verified deep-research passes (≈45 sources, ~50 claims voted, 6 angles)
put the result in context. Key verified findings:

- **Short-horizon crypto direction is near-random and unprofitable after fees.** ML/TA strategies earn
  positive *gross* returns that turn *negative* after realistic costs because short holds rack up fees
  (Jaquart, Dann & Weinhardt 2021). Daily direction accuracy tops out around **53–56 %** even with
  broad feature sets (our 47.3 % is below that weak ceiling).
- **More technical indicators don't help — they overfit.** Cutting a 130+ indicator set by 80–85 %
  *maintains or improves* out-of-sample accuracy (CMC v83n2, 2025). Feature *selection* > feature
  *count*. Model depth matters less than input quality.
- **Accuracy ≠ profit.** Better forecast error does not reliably translate into tradable, transferable
  edge; profitability is regime- and asset-specific.
- **Headline crypto backtests (6654 % returns, Sharpe 4–8) are overfit / look-ahead artifacts**, not
  targets — flagged repeatedly across the literature.
- **Where edges actually live:** order-flow / microstructure (but sub-minute, needs live order books),
  funding-rate carry (real but *decaying* — Sharpe 6.45 → negative by 2025 post-ETF), and **news text
  interpreted by an LLM** (the one place LLMs add value — but for *equities*, and fragile to fees).
- **Look-ahead contamination is acute for LLM backtests** (Glasserman & Lin 2023; "Profit Mirage"
  2025): a pretrained LLM may already "know" the prices that followed a historical date, and **naive
  prompt guardrails ("only use info before X") do not fix it** — anonymization / point-in-time data is
  required. This means even our 47.3 % could be optimistic.

**Bottom line from the research:** an LLM consuming only OHLCV-derived indicators on 4h candles has no
evidence-supported path to directional skill or net-of-fee edge. We confirmed this empirically before
reading the literature; the literature explains why.

---

## 5. Where a real edge *might* be (for the record)

If the project is revived, the evidence points away from "more indicators" and toward different data
and the LLM's actual strength (reading text/events). Ranked by evidence and feasibility:

1. **LLM news-sentiment drift in stocks** — the single most defensible edge, concentrated in
   **small-cap + negative news**. Caveats: **decaying** (strategy Sharpe ~6.5 in 2021 → ~1.2 by 2024),
   concentrated in illiquid names where spreads/borrow costs erode it, and it **requires anonymization
   + point-in-time data** to avoid the look-ahead "profit mirage." Equities' near-zero commissions
   help.
2. **Prediction-market (Polymarket) odds as a calibrated event-probability *feature*** — prediction
   markets are genuinely accurate forecasters. **But** there is *no verified evidence their odds lead
   stock/crypto prices around catalysts* — a hypothesis to test, not a known edge.
3. **Directly trading Polymarket** — weakest for retail: thin liquidity (~13 % of markets tradable),
   latency-bot-dominated arbitrage, only 2 of 7 LLMs profitable *before* fees in a published
   benchmark, plus unresolved US regulatory questions.

The **binding constraint** for all of these is data + validation discipline (point-in-time,
anonymization), not the model. That is a different application than this one.

---

## 6. The honest reframe

The research could find **no replicated evidence that any LLM beats buy-and-hold net of fees on a
single asset.** So "make the bot beat the market on returns" may simply be the wrong target. The one
property this bot *did* exhibit — **lower drawdown via low exposure** (−21 % vs −50 %) — is real but is
not alpha; it's just being in cash. A defensible *product* framing would be risk-reduction
("downside-protected exposure"), not return-generation. We chose not to pursue even that here.

---

## 7. What was built and is worth keeping

Independent of the trading result, the engineering is solid and reusable:

- A **trustworthy walk-forward backtesting harness** with no-look-ahead invariants, real fees, and
  full result provenance (`run_id` + `strategy` + `runs` table).
- A robust **reasoning-model integration**: a parse-failure fix that recovers the JSON answer from the
  model's reasoning channel raised the parse rate from **48 % → ~100 %** (3 failures in 9,720 calls).
- Causal, symmetric **few-shot memory** wiring (look-ahead-safe in both adaptation and test windows).
- A **meta-model crash guard** for degenerate (single-class) adaptation windows.
- Audit tooling: `analysis/trace_mine.py` (rule-violation linter, CI-runnable) and
  `analysis/conditional_edge.py` (conditional-edge analyzer with per-bucket significance tests).
- **138 passing tests.** All of the above is merged to `main`.

This harness can evaluate *any* future model, timeframe, or asset with the same rigor — which is the
real asset to carry forward.

---

## 8. Lessons learned

- **Small samples lie.** 3 trades produced "Sharpe 11.5"; 497 trades produced 47.3 %. Always size the
  sample before believing the metric.
- **Validate before you build.** A ~half-day smoke test caught two run-killing bugs and the slowness
  problem before a multi-day run; the full gate disconfirmed the edge before any capital was risked.
- **An LLM is an interpreter, not a forecaster.** It followed every rule perfectly and still couldn't
  predict price — because next-bar direction from lagging TA isn't predictable. Use LLMs where they
  have evidenced signal (text/events), not where they don't (raw price).
- **Beware your own dashboards.** A throwaway display script once made the returns look 77× too small;
  the committed reporter was correct. Verify raw numbers before concluding.

---

## Appendix — reproduce / key references

**Reproduce the run:**
```bash
dotnet run --project src/TradingSignal.Console -- ingest   # cache 4h/730d candles
dotnet run --project src/TradingSignal.Console -- run      # walk-forward backtest (long; needs LM Studio)
dotnet run --project src/TradingSignal.Console -- report   # print the metrics table
python3 analysis/trace_mine.py runs/predictions.db         # rule-violation lint
python3 analysis/conditional_edge.py runs/predictions.db   # conditional-edge analysis
```

**Key papers referenced:**
- Jaquart, Dann & Weinhardt (2021), short-horizon crypto ML net-of-fee unprofitability.
- Lopez-Lira & Tang, "Can ChatGPT Forecast Stock Price Movements?" (arXiv 2304.07619).
- Glasserman & Lin (2023), LLM backtest look-ahead bias (arXiv 2309.17322).
- "Profit Mirage" (arXiv 2510.07920) and "All Leaks Count" (arXiv 2602.17234) on temporal leakage.
- Borri, Liu, Tsyvinski & Wu, crypto carry/funding decay (arXiv 2510.14435).
- PolyBench (arXiv 2604.14199), prediction-market LLM trading benchmark.

**Relevant commits:** prep harness `0a14991`, smoke-found fixes `1906483`, analysis tools + merge
`911287a`.
