# Trading Lab

> A workbench of **algorithmic-trading** experiments, bots, strategies and tooling
> — consolidated from several separate repositories (full git history preserved).

Looking for the indicators library? That lives on its own:
**[TaLibStandard](https://github.com/phmatray/TaLibStandard)** — a modern C# port of
TA-Lib (200+ technical-analysis indicators). The projects here consume that kind of
library rather than replace it.

## Projects

| Folder | What it is | From |
|---|---|---|
| [`platform/`](platform) | Extensible algo-trading **platform** — strategy execution, risk management, Blazor Server dashboard (.NET 10) | `phmatray/TradingBot` |
| [`backtester/`](backtester) | Strategy **backtesting & analysis** — hexagonal architecture, ML-powered predictions, technical indicators | `phmatray/TradingStrat` |
| [`trady-strat/`](trady-strat) | An earlier trading-strategy take | `phmatray/TradyStrat` |
| [`signal-poc/`](signal-poc) | **BUY/SELL/HOLD signal** proof-of-concept — Binance data, feature engine, LLM call strategy | `phmatray/TradingSignalPoc` |
| [`tradingview-blazor/`](tradingview-blazor) | Embed **TradingView** charting widgets in Blazor Server | `phmatray/TradingViewBlazorApp` |
| [`botzilla/`](botzilla) | Automated crypto **bot** for the Bittrex exchange | `phmatray/botzilla` |
| [`python-notebooks/`](python-notebooks) | **Python / Jupyter** algo-trading experiments (data, futures, options) | `phmatray/algoTrad` |
| [`mt5-docker/`](mt5-docker) | Docker image running **MetaTrader5** with remote VNC (KasmVNC) | `phmatray/MetaTrader5-Docker-Image` |

## History

Each folder was merged with **full git history preserved** (`git subtree`). The
original repositories are archived and redirect here.

## License

MIT — see [`LICENSE`](LICENSE).
