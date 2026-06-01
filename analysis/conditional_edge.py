#!/usr/bin/env python3
"""Conditional-edge analysis for llm-only non-HOLD trades.

Slices the model's directional calls by confidence and market regime to test
whether it has an edge in any sub-condition, even though it has none overall.

Guards against false discovery: every bucket reports n and a two-sided z-test
vs the 50% coin-flip null. A single starred bucket among ~25 is expected by
chance (~1.25 at p<0.05) — only trust a COHERENT pattern (e.g. hit-rate rising
monotonically with confidence), not an isolated spike.

Usage: python3 analysis/conditional_edge.py [path/to/predictions.db] [strategy]
"""
import sys, json, sqlite3, math

DB = sys.argv[1] if len(sys.argv) > 1 else "runs/predictions.db"
STRAT = sys.argv[2] if len(sys.argv) > 2 else "llm-only"

con = sqlite3.connect(DB); con.row_factory = sqlite3.Row
rows = con.execute("""
    SELECT p.action, p.confidence, p.features_json,
           o.direction_correct AS dc, o.realized_return_pct AS ret
    FROM predictions p JOIN outcomes o ON o.prediction_id = p.id
    WHERE p.strategy = ? AND p.action IN (1, 2)
""", (STRAT,)).fetchall()

trades = []
for r in rows:
    f = json.loads(r["features_json"])
    ema20, ema50 = f.get("ema20", 0), f.get("ema50", 0)
    trades.append({
        "action": "BUY" if r["action"] == 1 else "SELL",
        "conf": r["confidence"], "dc": r["dc"], "ret": r["ret"],
        "rsi": f.get("rsi14"), "adx": f.get("adx14"), "vol": f.get("volumeRatio"),
        "ema_cross": "bull" if ema20 > ema50 else "bear",
    })

def stats(sub):
    n = len(sub)
    if n == 0: return None
    c = sum(t["dc"] for t in sub)
    p = c / n
    avg = sum(t["ret"] for t in sub) / n * 100
    z = (c - n / 2) / math.sqrt(n / 4) if n > 0 else 0.0
    return n, p * 100, avg, z

def table(title, buckets):
    print(f"\n=== {title} ===")
    print(f"  {'bucket':<22}{'n':>5}{'hit%':>7}{'avgRet%':>9}{'z':>7}  sig")
    print("  " + "-" * 52)
    for label, sub in buckets:
        s = stats(sub)
        if s is None:
            print(f"  {label:<22}{'0':>5}"); continue
        n, hit, avg, z = s
        sig = "  *" if abs(z) >= 1.96 else ""
        print(f"  {label:<22}{n:>5}{hit:>7.1f}{avg:>9.3f}{z:>7.2f}{sig}")

ov = stats(trades)
print(f"{STRAT}: {ov[0]} non-HOLD trades  overall hit={ov[1]:.1f}%  avgRet={ov[2]:.3f}%  z={ov[3]:.2f}")

def by(keyfn, edges, labels):
    out = []
    for lab, (lo, hi) in zip(labels, edges):
        out.append((lab, [t for t in trades if keyfn(t) is not None and lo <= keyfn(t) < hi]))
    return out

# Confidence — the key calibration question: does high conf trade better?
table("by CONFIDENCE bucket", by(lambda t: t["conf"],
    [(0,0.5),(0.5,0.6),(0.6,0.65),(0.65,0.7),(0.7,1.01)],
    ["conf [0,0.5)","conf [0.5,0.6)","conf [0.6,0.65)","conf [0.65,0.7)","conf >=0.7"]))

table("by ADX (trend strength)", by(lambda t: t["adx"],
    [(0,20),(20,30),(30,1e9)], ["adx <20 (ranging)","adx 20-30","adx >=30 (trend)"]))

table("by RSI band", by(lambda t: t["rsi"],
    [(0,30),(30,45),(45,55),(55,70),(70,1e9)],
    ["rsi <30","rsi 30-45","rsi 45-55","rsi 55-70","rsi >70"]))

table("by VOLUME ratio", by(lambda t: t["vol"],
    [(0,0.7),(0.7,1.0),(1.0,1.5),(1.5,1e9)],
    ["vol <0.7","vol 0.7-1.0","vol 1.0-1.5","vol >=1.5"]))

# Action vs trend alignment
table("by ACTION x EMA-cross", [
    ("BUY  w/ bull cross", [t for t in trades if t["action"]=="BUY" and t["ema_cross"]=="bull"]),
    ("BUY  vs bear cross",  [t for t in trades if t["action"]=="BUY" and t["ema_cross"]=="bear"]),
    ("SELL w/ bear cross",  [t for t in trades if t["action"]=="SELL" and t["ema_cross"]=="bear"]),
    ("SELL vs bull cross",  [t for t in trades if t["action"]=="SELL" and t["ema_cross"]=="bull"]),
])
