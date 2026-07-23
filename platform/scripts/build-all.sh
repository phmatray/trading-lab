#!/bin/bash
set -e

echo "Building TradingBot CLI for all platforms..."

# Clean previous builds
echo "Cleaning previous builds..."
rm -rf artifacts

# Build for Windows x64
echo "Building for Windows x64..."
dotnet publish src/TradingBot.Cli/TradingBot.Cli.csproj \
  -c Release \
  -r win-x64 \
  --self-contained true \
  -p:PublishSingleFile=true \
  -p:PublishTrimmed=true \
  -p:PublishReadyToRun=true \
  -p:IncludeNativeLibrariesForSelfExtract=true \
  -o artifacts/win-x64

# Build for macOS x64
echo "Building for macOS x64..."
dotnet publish src/TradingBot.Cli/TradingBot.Cli.csproj \
  -c Release \
  -r osx-x64 \
  --self-contained true \
  -p:PublishSingleFile=true \
  -p:PublishTrimmed=true \
  -p:PublishReadyToRun=true \
  -p:IncludeNativeLibrariesForSelfExtract=true \
  -o artifacts/osx-x64

# Build for macOS ARM64
echo "Building for macOS ARM64..."
dotnet publish src/TradingBot.Cli/TradingBot.Cli.csproj \
  -c Release \
  -r osx-arm64 \
  --self-contained true \
  -p:PublishSingleFile=true \
  -p:PublishTrimmed=true \
  -p:PublishReadyToRun=true \
  -p:IncludeNativeLibrariesForSelfExtract=true \
  -o artifacts/osx-arm64

# Build for Linux x64
echo "Building for Linux x64..."
dotnet publish src/TradingBot.Cli/TradingBot.Cli.csproj \
  -c Release \
  -r linux-x64 \
  --self-contained true \
  -p:PublishSingleFile=true \
  -p:PublishTrimmed=true \
  -p:PublishReadyToRun=true \
  -p:IncludeNativeLibrariesForSelfExtract=true \
  -o artifacts/linux-x64

echo "Build complete! Artifacts in artifacts/ directory"
ls -lh artifacts/*/tradingbot* || ls -lh artifacts/*/TradingBot.Cli*