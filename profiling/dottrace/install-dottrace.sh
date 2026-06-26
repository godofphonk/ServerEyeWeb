#!/bin/bash
# Install dotTrace CLI for CPU profiling

set -e

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
DOTTRACE_DIR="$SCRIPT_DIR"
DOTTRACE_VERSION="2024.2"
DOTTRACE_URL="https://download.jetbrains.com/resharper/dotTrace/dotTraceCLILinux.2024.2.0.tar.gz"
DOTTRACE_ARCHIVE="$DOTTRACE_DIR/dottrace.tar.gz"
DOTTRACE_BIN="$DOTTRACE_DIR/dottrace"

echo "📦 Installing dotTrace CLI v$DOTTRACE_VERSION..."

# Check if already installed
if [ -f "$DOTTRACE_BIN" ]; then
    echo "✅ dotTrace CLI already installed at $DOTTRACE_BIN"
    "$DOTTRACE_BIN" version
    exit 0
fi

# Download dotTrace CLI
echo "⬇️  Downloading dotTrace CLI from JetBrains..."
curl -L -o "$DOTTRACE_ARCHIVE" "$DOTTRACE_URL"

# Check if download was successful
if [ ! -f "$DOTTRACE_ARCHIVE" ] || [ ! -s "$DOTTRACE_ARCHIVE" ]; then
    echo "❌ Failed to download dotTrace CLI"
    exit 1
fi

# Extract archive
echo "📂 Extracting dotTrace CLI..."
tar -xzf "$DOTTRACE_ARCHIVE" -C "$DOTTRACE_DIR"

# Make executable
chmod +x "$DOTTRACE_BIN"

# Cleanup
rm -f "$DOTTRACE_ARCHIVE"

echo "✅ dotTrace CLI installed successfully at $DOTTRACE_BIN"
"$DOTTRACE_BIN" version
