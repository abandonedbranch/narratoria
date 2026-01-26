#!/bin/bash

# Copy compiled tools to the macOS app bundle
# This script is run during the build process by Flutter

set -e

echo "📦 Copying tools to app bundle..."

# Get the project root (up from src/macos/Runner/Scripts)
PROJECT_ROOT="$( cd "$( dirname "${BASH_SOURCE[0]}" )/../../../../.." && pwd )"
BIN_DIR="$PROJECT_ROOT/bin"

# Destination is the app bundle Resources directory
RESOURCES_DIR="${BUILT_PRODUCTS_DIR}/${PRODUCT_NAME}.app/Contents/Resources"
TOOLS_DIR="${RESOURCES_DIR}/tools"

# Create tools directory if it doesn't exist
mkdir -p "$TOOLS_DIR"

# Copy compiled tool binaries
if [ -d "$BIN_DIR" ]; then
    echo "  Copying from: $BIN_DIR"
    echo "  Copying to: $TOOLS_DIR"
    
    # Copy each tool binary
    for tool in "$BIN_DIR"/*; do
        if [ -f "$tool" ] && [ -x "$tool" ]; then
            tool_name=$(basename "$tool")
            echo "  ✓ Copying $tool_name"
            cp "$tool" "$TOOLS_DIR/"
            chmod +x "$TOOLS_DIR/$tool_name"
        fi
    done
    
    echo "✓ Tools copied successfully"
else
    echo "⚠️  Warning: $BIN_DIR not found. Tools will not be available."
    echo "    Run: dart compile exe tools/*/main.dart -o bin/<tool-name>"
    exit 0  # Don't fail the build, just warn
fi
