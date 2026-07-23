![TradingViewBlazorApp banner](.github/banner.png)

# TradingViewBlazorApp

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