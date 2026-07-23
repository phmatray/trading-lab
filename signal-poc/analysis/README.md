# Trace mining (`trace_mine.py`)

A standalone, read-only linter that scans `runs/predictions.db` and flags
**rule-violation candidates** — predictions whose decoded action, indicators, or
free-text reasoning look inconsistent with the trading rules. It is meant to run in
CI as a gate: it exits non-zero when it finds anything to review.

- Pure Python 3 standard library (`sqlite3` only). No `pip` dependencies.
- **Read-only.** The database is opened with `mode=ro`; the tool never writes to it
  and never touches any `.cs` file.
- **Schema tolerant.** It inspects columns with `PRAGMA table_info(predictions)`. If
  the `run_id` / `strategy` columns exist it groups by `strategy`; otherwise it treats
  every row as one legacy/unlabelled group. It runs cleanly on the current 48-row
  legacy DB.

## Running

```bash
# default: runs/predictions.db, resolved relative to the repo root
python3 analysis/trace_mine.py

# explicit path (relative paths resolve against the repo root, not your cwd)
python3 analysis/trace_mine.py runs/predictions.db

# CI / silent mode: no report, just the exit code
python3 analysis/trace_mine.py --quiet
```

### Exit codes

| code | meaning                                              |
|------|------------------------------------------------------|
| 0    | no violation candidates found                        |
| 1    | at least one violation candidate found (CI failure)  |
| 2    | usage error (db file missing, no `predictions` table)|

Gate a CI job simply by running the tool — a non-zero exit fails the step.

## What it reads

It runs:

```sql
SELECT ... FROM predictions p
LEFT JOIN outcomes o ON o.prediction_id = p.id
```

For each row it:

- decodes `action`: `0=HOLD`, `1=BUY`, `2=SELL`;
- parses `features_json` (camelCase keys: `rsi14`, `adx14`, `volumeRatio`, `ema20`,
  `ema50`, ...);
- derives `ema_cross` from the EMAs — **bullish** if `ema20 > ema50`, **bearish** if
  `ema20 < ema50`, else **neutral** — matching `PromptBuilder.EmaCross`
  (`ema_cross` is not stored in the DB);
- scans `reason` + `reasoning` text for conviction / hedging / stand-aside wording.

Missing or unparseable values are treated as "unknown" and simply do not trigger a
check (the tool never crashes on partial data).

## The checks

A single row can fire more than one check. The summary table counts **flags** (so its
TOTAL can exceed the number of flagged rows); the final `RESULT` line reports both the
flag count and the distinct row count.

| key      | name                         | fires when |
|----------|------------------------------|------------|
| `hold`   | HOLD-cap breach              | `action=HOLD` and `confidence > 0.70`. HOLD is a no-trade decision and should not carry high conviction. |
| `r1`     | Rule 1 (bearish BUY)         | `ema_cross` bearish **and** `action=BUY` **and** `rsi14 >= 30`. Buying into a bearish EMA cross without a deeply-oversold RSI to justify a bounce. |
| `r2`     | Rule 2 (bullish SELL)        | `ema_cross` bullish **and** `action=SELL` **and** `rsi14 <= 70`. Selling into a bullish EMA cross without an overbought RSI to justify it. |
| `r4`     | Rule 4 (ranging)             | `adx14 < 20` **and** `action in (BUY,SELL)` **and** `confidence > 0.55`. Taking a confident directional trade in a ranging (trendless) market. |
| `r5`     | Rule 5 (low volume)          | `volumeRatio < 0.7` **and** `action in (BUY,SELL)` **and** `confidence > 0.6`. Confident trade on thin volume (weak confirmation). |
| `diverg` | confidence / conviction divergence | the text uses strong-conviction words (`strong`, `clear`, `definitely`, `obvious`, `certain`) but `confidence < 0.5`; **or** uses hedging words (`uncertain`, `unclear`, `mixed`, `conflicting`, `weak`) but `confidence > 0.8`. The stated numeric confidence contradicts the language. |
| `contra` | contradictory phrasing       | the text expresses stand-aside intent (e.g. "will hold", "stay flat", "wait for", "no position", "do nothing") but the action is BUY or SELL. |

### Heuristic caveats

The text-based checks (`diverg`, `contra`) are deliberately simple, documented
substring heuristics scanning the lower-cased `reason` + `reasoning` fields:

- They do **not** parse grammar or handle negation. "I will **not** hold" would still
  match the `contra` phrase "will hold". This is intentional: the tool errs toward
  surfacing candidates for human review rather than guaranteeing precision.
- Word matching is substring-based, so `strong` also matches inside `strongly`.

Treat every flag as a *candidate* for review, not a confirmed defect.

## Output

1. A **per-group summary table** (grouped by `strategy`, or a single
   `(legacy/unlabelled)` group on the legacy schema) with a count column per check,
   plus per-group row counts and totals, and an `ALL` total row.
2. A **detail list** per check showing `id`, `as_of_utc`, `strategy`, `action`, and
   `confidence`. Detail is capped at the first **50** rows per check; when more exist
   the tool prints `... N more not shown (truncated).` — there are no silent caps.
3. A final `RESULT` line with the total flag count and distinct flagged-row count.

`--quiet` suppresses all of the above and emits only the exit code.

## Current legacy DB (48 rows)

Running against the present 48-row legacy database flags **2** rows, both
confidence/conviction divergences: two `parse_failure` HOLD predictions with
`confidence = 0.0` whose reasoning text still describes a "strong trend" / "clear
edge". The tool exits `1`.
