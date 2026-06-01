#!/usr/bin/env python3
"""trace_mine.py - read-only trace-mining / rule-violation linter for predictions.db.

Reads runs/predictions.db, joins predictions to outcomes, decodes each prediction's
action + features, and flags rows that look like rule violations (a candidate the LLM
or adaptation layer probably should not have produced). Designed to run in CI: exit
code is 0 when no violations are found and 1 when at least one is found.

Pure stdlib (sqlite3 only). The database is opened read-only and never written to.

Usage:
    python3 analysis/trace_mine.py [path/to/predictions.db] [--quiet]

If no path is given it defaults to runs/predictions.db, resolved relative to the
repository root (the parent of the directory containing this script).
"""

import json
import os
import sqlite3
import sys

# ---------------------------------------------------------------------------
# Constants
# ---------------------------------------------------------------------------

ACTION_NAMES = {0: "HOLD", 1: "BUY", 2: "SELL"}
TRADE_ACTIONS = ("BUY", "SELL")
DETAIL_CAP = 50  # max detail rows printed per check; truncation count is reported

STRONG_WORDS = ("strong", "clear", "definitely", "obvious", "certain")
HEDGE_WORDS = ("uncertain", "unclear", "mixed", "conflicting", "weak")

# Phrases that imply "I am NOT taking a position right now" (stand-aside intent).
STAND_ASIDE_PHRASES = (
    "will hold",
    "i hold",
    "recommend hold",
    "recommend holding",
    "stay flat",
    "staying flat",
    "remain flat",
    "stay on the sideline",
    "stay on the sidelines",
    "wait and see",
    "wait for",
    "waiting for",
    "stand aside",
    "no position",
    "no action",
    "do nothing",
    "hold the position",
)

# Ordered list of (key, label, description). Order controls report column order.
CHECKS = [
    ("hold_cap", "HOLD-cap breach",
     "action=HOLD but confidence > 0.70 (HOLD predictions should be low-conviction)."),
    ("rule1", "Rule 1 (bearish BUY)",
     "ema_cross bearish AND action=BUY AND rsi14 >= 30."),
    ("rule2", "Rule 2 (bullish SELL)",
     "ema_cross bullish AND action=SELL AND rsi14 <= 70."),
    ("rule4", "Rule 4 (ranging)",
     "adx14 < 20 AND action in (BUY,SELL) AND confidence > 0.55."),
    ("rule5", "Rule 5 (low volume)",
     "volume_ratio < 0.7 AND action in (BUY,SELL) AND confidence > 0.6."),
    ("divergence", "conf/conviction divergence",
     "strong-conviction wording with confidence < 0.5, or hedging wording with confidence > 0.8."),
    ("contradiction", "contradictory phrasing",
     "text says hold/wait/stay flat but action is BUY/SELL (stand-aside heuristic)."),
]
CHECK_KEYS = [c[0] for c in CHECKS]
CHECK_LABELS = {c[0]: c[1] for c in CHECKS}


# ---------------------------------------------------------------------------
# Helpers
# ---------------------------------------------------------------------------

def repo_root():
    """Repo root = parent of the directory holding this script (analysis/)."""
    return os.path.dirname(os.path.dirname(os.path.abspath(__file__)))


def resolve_db_path(argv):
    """argv[1] (the first non-flag arg) is the db path; default runs/predictions.db.

    Relative paths resolve against the repo root, not the cwd, so the tool behaves
    identically from any working directory (important for CI)."""
    path = None
    for arg in argv[1:]:
        if arg == "--quiet" or arg.startswith("--"):
            continue
        path = arg
        break
    if path is None:
        path = "runs/predictions.db"
    if not os.path.isabs(path):
        path = os.path.join(repo_root(), path)
    return os.path.normpath(path)


def table_columns(conn, table):
    """Return the set of column names for a table via PRAGMA (schema tolerance)."""
    cols = set()
    for row in conn.execute("PRAGMA table_info({})".format(table)):
        # PRAGMA table_info columns: cid, name, type, notnull, dflt_value, pk
        cols.add(row[1])
    return cols


def to_float(value):
    """Best-effort float parse; returns None on failure/missing."""
    if value is None:
        return None
    try:
        return float(value)
    except (TypeError, ValueError):
        return None


def get_feature(features, *keys):
    """Fetch the first present key (camelCase per schema) and coerce to float."""
    for key in keys:
        if key in features and features[key] is not None:
            return to_float(features[key])
    return None


def derive_ema_cross(features):
    """bullish if ema20>ema50, bearish if ema20<ema50, else neutral.

    Mirrors PromptBuilder.EmaCross. Returns 'unknown' if either EMA is missing."""
    ema20 = get_feature(features, "ema20")
    ema50 = get_feature(features, "ema50")
    if ema20 is None or ema50 is None:
        return "unknown"
    if ema20 > ema50:
        return "bullish"
    if ema20 < ema50:
        return "bearish"
    return "neutral"


def contains_any(text, words):
    return any(w in text for w in words)


# ---------------------------------------------------------------------------
# Core
# ---------------------------------------------------------------------------

def evaluate_row(row):
    """Return list of check keys that fired for a single joined row.

    `row` is a dict with: id, as_of_utc, strategy, action_code, confidence,
    reason, reasoning, features (dict)."""
    fired = []
    action = ACTION_NAMES.get(row["action_code"], "UNKNOWN")
    conf = row["confidence"]
    features = row["features"]

    rsi = get_feature(features, "rsi14")
    adx = get_feature(features, "adx14")
    vol_ratio = get_feature(features, "volumeRatio")
    ema_cross = derive_ema_cross(features)

    is_trade = action in TRADE_ACTIONS

    # HOLD-cap breach — only meaningful for RAW LLM output. Adaptation overlays
    # (labels starting with "+") relabel BUY/SELL -> HOLD while keeping the pre-gate
    # signal confidence, so a high-confidence HOLD there is expected, not a violation.
    strat = row.get("strategy") or ""
    if action == "HOLD" and conf is not None and conf > 0.70 and not strat.startswith("+"):
        fired.append("hold_cap")

    # Rule 1: bearish cross but BUY while RSI not deeply oversold
    if ema_cross == "bearish" and action == "BUY" and rsi is not None and rsi >= 30:
        fired.append("rule1")

    # Rule 2: bullish cross but SELL while RSI not deeply overbought
    if ema_cross == "bullish" and action == "SELL" and rsi is not None and rsi <= 70:
        fired.append("rule2")

    # Rule 4: ranging market (low ADX) but a confident trade
    if adx is not None and adx < 20 and is_trade and conf is not None and conf > 0.55:
        fired.append("rule4")

    # Rule 5: low volume but a confident trade
    if vol_ratio is not None and vol_ratio < 0.7 and is_trade and conf is not None and conf > 0.6:
        fired.append("rule5")

    # confidence / conviction divergence (scan reason + reasoning, lower-cased)
    text = ((row["reason"] or "") + "\n" + (row["reasoning"] or "")).lower()
    if conf is not None:
        if conf < 0.5 and contains_any(text, STRONG_WORDS):
            fired.append("divergence")
        elif conf > 0.8 and contains_any(text, HEDGE_WORDS):
            fired.append("divergence")

    # contradictory phrasing: text expresses stand-aside intent but action is a trade.
    # Documented heuristic: substring match on a small fixed phrase list. This is
    # intentionally simple and may miss negations ("I will NOT hold"); it errs toward
    # surfacing candidates for human review rather than guaranteeing precision.
    if is_trade and contains_any(text, STAND_ASIDE_PHRASES):
        fired.append("contradiction")

    return fired


def load_rows(conn, has_run_cols):
    """Load joined predictions+outcomes rows as evaluable dicts. Schema-tolerant."""
    if has_run_cols:
        strategy_expr = "p.strategy"
    else:
        strategy_expr = "'' AS strategy"

    sql = (
        "SELECT p.id, p.as_of_utc, {strategy}, p.action, p.confidence, "
        "       p.reason, p.reasoning, p.features_json "
        "FROM predictions p "
        "LEFT JOIN outcomes o ON o.prediction_id = p.id "
        "ORDER BY p.as_of_utc"
    ).format(strategy=strategy_expr)

    rows = []
    for r in conn.execute(sql):
        try:
            features = json.loads(r[7]) if r[7] else {}
            if not isinstance(features, dict):
                features = {}
        except (ValueError, TypeError):
            features = {}
        strategy = (r[2] or "").strip()
        rows.append({
            "id": r[0],
            "as_of_utc": r[1],
            "strategy": strategy if strategy else "(legacy/unlabelled)",
            "action_code": r[3],
            "confidence": to_float(r[4]),
            "reason": r[5],
            "reasoning": r[6],
            "features": features,
        })
    return rows


# ---------------------------------------------------------------------------
# Reporting
# ---------------------------------------------------------------------------

def print_summary_table(groups, quiet):
    """Per-group (strategy) counts per check."""
    if quiet:
        return
    print("=" * 78)
    print("SUMMARY (counts per check, grouped by strategy)")
    print("=" * 78)

    # Compute column widths.
    group_names = sorted(groups.keys())
    name_w = max([len("strategy")] + [len(g) for g in group_names])
    short = {
        "hold_cap": "hold", "rule1": "r1", "rule2": "r2", "rule4": "r4",
        "rule5": "r5", "divergence": "diverg", "contradiction": "contra",
    }
    col_w = {k: max(len(short[k]), 6) for k in CHECK_KEYS}

    header = "  " + "strategy".ljust(name_w)
    for k in CHECK_KEYS:
        header += "  " + short[k].rjust(col_w[k])
    header += "  " + "rows".rjust(5) + "  " + "TOTAL".rjust(6)
    print(header)
    print("  " + "-" * (len(header) - 2))

    grand = {k: 0 for k in CHECK_KEYS}
    grand_rows = 0
    grand_total = 0
    for g in group_names:
        counts = groups[g]["counts"]
        rowcount = groups[g]["rows"]
        line = "  " + g.ljust(name_w)
        total = 0
        for k in CHECK_KEYS:
            line += "  " + str(counts[k]).rjust(col_w[k])
            total += counts[k]
            grand[k] += counts[k]
        line += "  " + str(rowcount).rjust(5) + "  " + str(total).rjust(6)
        grand_rows += rowcount
        grand_total += total
        print(line)

    print("  " + "-" * (len(header) - 2))
    line = "  " + "ALL".ljust(name_w)
    for k in CHECK_KEYS:
        line += "  " + str(grand[k]).rjust(col_w[k])
    line += "  " + str(grand_rows).rjust(5) + "  " + str(grand_total).rjust(6)
    print(line)
    print()
    print("  Legend: hold=HOLD-cap  r1=Rule1  r2=Rule2  r4=Rule4(ranging)  "
          "r5=Rule5(low-vol)  diverg=conf/conviction  contra=contradictory")
    print("  Note: a single row can fire multiple checks, so TOTAL counts flags, "
          "not rows.")
    print()


def print_details(by_check, quiet):
    if quiet:
        return
    print("=" * 78)
    print("DETAIL (per check; capped at {} rows each)".format(DETAIL_CAP))
    print("=" * 78)
    for key in CHECK_KEYS:
        hits = by_check[key]
        print()
        print("[{}]  ({} flagged)".format(CHECK_LABELS[key], len(hits)))
        if not hits:
            print("    none")
            continue
        shown = hits[:DETAIL_CAP]
        for h in shown:
            print("    id={id}  as_of={as_of}  strategy={strat}  "
                  "action={action}  conf={conf}".format(
                      id=h["id"],
                      as_of=h["as_of_utc"],
                      strat=h["strategy"],
                      action=ACTION_NAMES.get(h["action_code"], "UNKNOWN"),
                      conf="{:.2f}".format(h["confidence"]) if h["confidence"] is not None else "n/a",
                  ))
        truncated = len(hits) - len(shown)
        if truncated > 0:
            print("    ... {} more not shown (truncated).".format(truncated))


# ---------------------------------------------------------------------------
# Main
# ---------------------------------------------------------------------------

def main(argv):
    quiet = "--quiet" in argv
    db_path = resolve_db_path(argv)

    if not os.path.exists(db_path):
        sys.stderr.write("error: database not found: {}\n".format(db_path))
        return 2

    # Open strictly read-only via URI so the tool can never mutate the DB.
    uri = "file:{}?mode=ro".format(db_path)
    conn = sqlite3.connect(uri, uri=True)
    try:
        pred_cols = table_columns(conn, "predictions")
        if not pred_cols:
            sys.stderr.write("error: no 'predictions' table in {}\n".format(db_path))
            return 2
        has_run_cols = "run_id" in pred_cols and "strategy" in pred_cols

        rows = load_rows(conn, has_run_cols)
    finally:
        conn.close()

    if not quiet:
        print("trace_mine: scanning {}".format(db_path))
        print("trace_mine: {} prediction rows; schema={}".format(
            len(rows),
            "labelled (run_id/strategy present)" if has_run_cols
            else "legacy (no run_id/strategy -> single group)"))
        print()

    # Group accumulator + per-check hit lists.
    groups = {}
    by_check = {k: [] for k in CHECK_KEYS}
    total_violations = 0
    violating_rows = 0

    for row in rows:
        g = groups.setdefault(row["strategy"], {
            "counts": {k: 0 for k in CHECK_KEYS},
            "rows": 0,
        })
        g["rows"] += 1

        fired = evaluate_row(row)
        if fired:
            violating_rows += 1
        for key in fired:
            g["counts"][key] += 1
            by_check[key].append(row)
            total_violations += 1

    print_summary_table(groups, quiet)
    print_details(by_check, quiet)

    if not quiet:
        print()
        print("=" * 78)
        print("RESULT: {} flag(s) across {} row(s) (of {} scanned).".format(
            total_violations, violating_rows, len(rows)))
        print("=" * 78)

    # Exit 1 if any violation found so CI can gate on it.
    return 1 if total_violations > 0 else 0


if __name__ == "__main__":
    sys.exit(main(sys.argv))
