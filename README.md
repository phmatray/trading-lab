![TradingViewBlazorApp banner](.github/banner.png)

# TradingViewBlazorApp

<!-- portfolio-badges:start -->
<!-- Identity -->
[![phmatray - TradingViewBlazorApp](https://img.shields.io/static/v1?label=phmatray&message=TradingViewBlazorApp&color=blue&logo=github)](https://github.com/phmatray/TradingViewBlazorApp)
![Top language](https://img.shields.io/github/languages/top/phmatray/TradingViewBlazorApp)
[![Stars](https://img.shields.io/github/stars/phmatray/TradingViewBlazorApp?style=social)](https://github.com/phmatray/TradingViewBlazorApp/stargazers)
[![Forks](https://img.shields.io/github/forks/phmatray/TradingViewBlazorApp?style=social)](https://github.com/phmatray/TradingViewBlazorApp/network/members)

<!-- Activity -->
[![Issues](https://img.shields.io/github/issues/phmatray/TradingViewBlazorApp)](https://github.com/phmatray/TradingViewBlazorApp/issues)
[![Pull requests](https://img.shields.io/github/issues-pr/phmatray/TradingViewBlazorApp)](https://github.com/phmatray/TradingViewBlazorApp/pulls)
[![Last commit](https://img.shields.io/github/last-commit/phmatray/TradingViewBlazorApp)](https://github.com/phmatray/TradingViewBlazorApp/commits)
<!-- portfolio-badges:end -->


> Embed TradingView charting widgets in a Blazor Server application.

## Description
TradingViewBlazorApp is a Blazor Server application that integrates TradingView lightweight charts into a .NET web app. It demonstrates how to use TradingView widgets within Razor components, providing real-time financial chart visualizations powered by the TradingView charting library.

## Features
- TradingView chart widget integration in Blazor
- Blazor Server with real-time data binding
- Containerized with Docker support
- Clean component architecture

## Getting Started
```bash
git clone https://github.com/phmatray/TradingViewBlazorApp.git
cd TradingViewBlazorApp
dotnet run --project TradingViewBlazorApp
```

Or with Docker:
```bash
docker build -t tradingview-blazor .
docker run -p 8080:8080 tradingview-blazor
```

## License
MIT