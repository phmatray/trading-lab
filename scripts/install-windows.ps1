# PowerShell installation script for TradingBot CLI (Windows)
$ErrorActionPreference = "Stop"

Write-Host "Installing TradingBot CLI..." -ForegroundColor Green

# Installation directories
$InstallDir = "$env:LOCALAPPDATA\TradingBot"
$BinDir = "$InstallDir\bin"
$ConfigDir = "$InstallDir\config"
$DataDir = "$InstallDir\data"
$LogsDir = "$InstallDir\logs"

# Create directories
Write-Host "Creating directories..." -ForegroundColor Yellow
New-Item -ItemType Directory -Force -Path $BinDir | Out-Null
New-Item -ItemType Directory -Force -Path $ConfigDir | Out-Null
New-Item -ItemType Directory -Force -Path $DataDir | Out-Null
New-Item -ItemType Directory -Force -Path $LogsDir | Out-Null

# Download latest release
Write-Host "Downloading latest release..." -ForegroundColor Yellow
$Repo = "your-username/TradingBot"  # Update with actual GitHub repo
$Platform = "win-x64"

try {
    $LatestRelease = Invoke-RestMethod -Uri "https://api.github.com/repos/$Repo/releases/latest"
    $Asset = $LatestRelease.assets | Where-Object { $_.name -like "*$Platform*" }

    if (-not $Asset) {
        throw "Could not find release for platform $Platform"
    }

    $DownloadUrl = $Asset.browser_download_url
    $TempFile = "$env:TEMP\tradingbot-$Platform.zip"

    Write-Host "Downloading from: $DownloadUrl" -ForegroundColor Cyan
    Invoke-WebRequest -Uri $DownloadUrl -OutFile $TempFile

    Write-Host "Extracting to $BinDir..." -ForegroundColor Cyan
    Expand-Archive -Path $TempFile -DestinationPath $BinDir -Force
    Remove-Item $TempFile
}
catch {
    Write-Host "Error downloading release: $_" -ForegroundColor Red
    Write-Host "Please download manually from: https://github.com/$Repo/releases/latest" -ForegroundColor Yellow
    exit 1
}

# Copy default config if exists
if (Test-Path "$BinDir\config") {
    Copy-Item -Path "$BinDir\config\*" -Destination $ConfigDir -Recurse -Force
}

# Add to PATH
$CurrentPath = [Environment]::GetEnvironmentVariable("Path", "User")
if ($CurrentPath -notlike "*$BinDir*") {
    [Environment]::SetEnvironmentVariable(
        "Path",
        "$CurrentPath;$BinDir",
        "User"
    )
    Write-Host "Added TradingBot to PATH" -ForegroundColor Green
    $env:Path += ";$BinDir"  # Update current session
}
else {
    Write-Host "TradingBot already in PATH" -ForegroundColor Green
}

# Create desktop shortcut (optional)
$WshShell = New-Object -ComObject WScript.Shell
$DesktopPath = [Environment]::GetFolderPath("Desktop")
$ShortcutPath = "$DesktopPath\TradingBot.lnk"
$Shortcut = $WshShell.CreateShortcut($ShortcutPath)
$Shortcut.TargetPath = "$BinDir\TradingBot.Cli.exe"
$Shortcut.WorkingDirectory = $DataDir
$Shortcut.Description = "TradingBot CLI"
$Shortcut.Save()

Write-Host ""
Write-Host "╔═══════════════════════════════════════════════════════════╗" -ForegroundColor Green
Write-Host "║         TradingBot CLI Installation Complete!            ║" -ForegroundColor Green
Write-Host "╚═══════════════════════════════════════════════════════════╝" -ForegroundColor Green
Write-Host ""
Write-Host "Installation directory: $InstallDir" -ForegroundColor Cyan
Write-Host "Configuration directory: $ConfigDir" -ForegroundColor Cyan
Write-Host "Data directory: $DataDir" -ForegroundColor Cyan
Write-Host "Logs directory: $LogsDir" -ForegroundColor Cyan
Write-Host ""
Write-Host "To get started:" -ForegroundColor Yellow
Write-Host "  1. Open a new PowerShell window (to reload PATH)"
Write-Host "  2. Run: tradingbot --help"
Write-Host "  3. Configure API keys: tradingbot config set-api-key"
Write-Host ""
Write-Host "Desktop shortcut created: $ShortcutPath" -ForegroundColor Green
Write-Host "Documentation: https://github.com/$Repo/docs" -ForegroundColor Cyan
Write-Host ""
