#!/bin/bash

# Build Narratoria with tools bundled
# This script compiles tools and builds the Flutter macOS app

set -e

SCRIPT_DIR="$( cd "$( dirname "${BASH_SOURCE[0]}" )" && pwd )"
PROJECT_ROOT="$SCRIPT_DIR"

echo "🔨 Building Narratoria..."
echo ""

# Step 1: Compile tools
echo "📦 Compiling tools..."
mkdir -p "$PROJECT_ROOT/bin"

dart compile exe "$PROJECT_ROOT/tools/torch-lighter/main.dart" -o "$PROJECT_ROOT/bin/torch-lighter"
dart compile exe "$PROJECT_ROOT/tools/door-examiner/main.dart" -o "$PROJECT_ROOT/bin/door-examiner"

echo "✓ Tools compiled to bin/"
echo ""

# Step 2: Build Flutter app
echo "🚀 Building Flutter macOS app..."
cd "$PROJECT_ROOT/src"

flutter build macos --release

echo ""
echo "✓ Build complete!"
echo ""

# Step 3: Copy tools to app bundle
echo "📋 Copying tools to app bundle..."
BUNDLE_PATH="$PROJECT_ROOT/src/build/macos/Build/Products/Release/narratoria.app"
RESOURCES_DIR="$BUNDLE_PATH/Contents/Resources/tools"

mkdir -p "$RESOURCES_DIR"
cp "$PROJECT_ROOT/bin"/* "$RESOURCES_DIR/"
chmod +x "$RESOURCES_DIR"/*

echo "✓ Tools bundled in app"
echo ""
echo "✅ Build complete!"
echo "App location: $BUNDLE_PATH"
