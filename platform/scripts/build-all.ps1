# PowerShell build script for TradingBot CLI
$ErrorActionPreference = "Stop"

Write-Host "Building TradingBot CLI for all platforms..." -ForegroundColor Green

# Clean previous builds
Write-Host "Cleaning previous builds..." -ForegroundColor Yellow
if (Test-Path "artifacts") {
    Remove-Item -Recurse -Force "artifacts"
}

# Build for Windows x64
Write-Host "Building for Windows x64..." -ForegroundColor Cyan
dotnet publish src/TradingBot.Cli/TradingBot.Cli.csproj `
  -c Release `
  -r win-x64 `
  --self-contained true `
  -p:PublishSingleFile=true `
  -p:PublishTrimmed=true `
  -p:PublishReadyToRun=true `
  -p:IncludeNativeLibrariesForSelfExtract=true `
  -o artifacts/win-x64

# Build for macOS x64
Write-Host "Building for macOS x64..." -ForegroundColor Cyan
dotnet publish src/TradingBot.Cli/TradingBot.Cli.csproj `
  -c Release `
  -r osx-x64 `
  --self-contained true `
  -p:PublishSingleFile=true `
  -p:PublishTrimmed=true `
  -p:PublishReadyToRun=true `
  -p:IncludeNativeLibrariesForSelfExtract=true `
  -o artifacts/osx-x64

# Build for macOS ARM64
Write-Host "Building for macOS ARM64..." -ForegroundColor Cyan
dotnet publish src/TradingBot.Cli/TradingBot.Cli.csproj `
  -c Release `
  -r osx-arm64 `
  --self-contained true `
  -p:PublishSingleFile=true `
  -p:PublishTrimmed=true `
  -p:PublishReadyToRun=true `
  -p:IncludeNativeLibrariesForSelfExtract=true `
  -o artifacts/osx-arm64

# Build for Linux x64
Write-Host "Building for Linux x64..." -ForegroundColor Cyan
dotnet publish src/TradingBot.Cli/TradingBot.Cli.csproj `
  -c Release `
  -r linux-x64 `
  --self-contained true `
  -p:PublishSingleFile=true `
  -p:PublishTrimmed=true `
  -p:PublishReadyToRun=true `
  -p:IncludeNativeLibrariesForSelfExtract=true `
  -o artifacts/linux-x64

Write-Host "`nBuild complete! Artifacts in artifacts/ directory" -ForegroundColor Green
Get-ChildItem -Path artifacts -Recurse -Include tradingbot*,TradingBot.Cli* | Select-Object FullName,Length