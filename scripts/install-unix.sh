#!/bin/bash
set -e

echo "Installing TradingBot CLI..."

# Detect OS and architecture
OS="$(uname -s)"
ARCH="$(uname -m)"

case "$OS" in
    Darwin*)
        if [ "$ARCH" = "arm64" ]; then
            PLATFORM="osx-arm64"
        else
            PLATFORM="osx-x64"
        fi
        ;;
    Linux*)
        PLATFORM="linux-x64"
        ;;
    *)
        echo "Unsupported operating system: $OS"
        exit 1
        ;;
esac

echo "Detected platform: $PLATFORM"

# Installation directory
INSTALL_DIR="$HOME/.tradingbot"
BIN_DIR="$INSTALL_DIR/bin"
CONFIG_DIR="$INSTALL_DIR/config"
DATA_DIR="$INSTALL_DIR/data"
LOGS_DIR="$INSTALL_DIR/logs"

# Create directories
echo "Creating directories..."
mkdir -p "$BIN_DIR"
mkdir -p "$CONFIG_DIR"
mkdir -p "$DATA_DIR"
mkdir -p "$LOGS_DIR"

# Download latest release
echo "Downloading latest release..."
REPO="your-username/TradingBot"  # Update with actual GitHub repo
LATEST_URL=$(curl -s "https://api.github.com/repos/$REPO/releases/latest" \
  | grep "browser_download_url.*$PLATFORM" \
  | cut -d : -f 2,3 \
  | tr -d \")

if [ -z "$LATEST_URL" ]; then
    echo "Error: Could not find release for platform $PLATFORM"
    echo "Please download manually from: https://github.com/$REPO/releases/latest"
    exit 1
fi

# Download and extract
TEMP_FILE="/tmp/tradingbot-$PLATFORM.tar.gz"
curl -L "$LATEST_URL" -o "$TEMP_FILE"

echo "Extracting to $BIN_DIR..."
tar -xzf "$TEMP_FILE" -C "$BIN_DIR"
rm "$TEMP_FILE"

# Make executable
chmod +x "$BIN_DIR/TradingBot.Cli" || chmod +x "$BIN_DIR/tradingbot"

# Copy default config if exists
if [ -d "$BIN_DIR/config" ]; then
    cp -r "$BIN_DIR/config/"* "$CONFIG_DIR/"
fi

# Add to PATH
SHELL_CONFIG=""
if [ -f "$HOME/.zshrc" ]; then
    SHELL_CONFIG="$HOME/.zshrc"
elif [ -f "$HOME/.bashrc" ]; then
    SHELL_CONFIG="$HOME/.bashrc"
elif [ -f "$HOME/.bash_profile" ]; then
    SHELL_CONFIG="$HOME/.bash_profile"
fi

if [ -n "$SHELL_CONFIG" ]; then
    if ! grep -q "tradingbot/bin" "$SHELL_CONFIG"; then
        echo '' >> "$SHELL_CONFIG"
        echo '# TradingBot CLI' >> "$SHELL_CONFIG"
        echo "export PATH=\"\$PATH:\$HOME/.tradingbot/bin\"" >> "$SHELL_CONFIG"
        echo "Added TradingBot to PATH in $SHELL_CONFIG"
        echo "Please run: source $SHELL_CONFIG"
    else
        echo "TradingBot already in PATH"
    fi
fi

echo ""
echo "╔═══════════════════════════════════════════════════════════╗"
echo "║         TradingBot CLI Installation Complete!            ║"
echo "╚═══════════════════════════════════════════════════════════╝"
echo ""
echo "Installation directory: $INSTALL_DIR"
echo "Configuration directory: $CONFIG_DIR"
echo "Data directory: $DATA_DIR"
echo "Logs directory: $LOGS_DIR"
echo ""
echo "To get started:"
echo "  1. Reload your shell: source $SHELL_CONFIG"
echo "  2. Run: tradingbot --help"
echo "  3. Configure API keys: tradingbot config set-api-key"
echo ""
echo "Documentation: https://github.com/$REPO/docs"
echo ""
