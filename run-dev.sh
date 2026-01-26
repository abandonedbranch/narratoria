#!/bin/bash

# Run Narratoria in development mode with tools
# Compiles tools and runs the app via flutter run

set -e

SCRIPT_DIR="$( cd "$( dirname "${BASH_SOURCE[0]}" )" && pwd )"
PROJECT_ROOT="$SCRIPT_DIR"

echo "🔧 Running Narratoria (development mode)..."
echo ""

# Step 1: Compile tools
echo "📦 Compiling tools..."
mkdir -p "$PROJECT_ROOT/bin"

dart compile exe "$PROJECT_ROOT/tools/torch-lighter/main.dart" -o "$PROJECT_ROOT/bin/torch-lighter"
dart compile exe "$PROJECT_ROOT/tools/door-examiner/main.dart" -o "$PROJECT_ROOT/bin/door-examiner"

echo "✓ Tools compiled to bin/"
echo ""

# Step 2: Run Flutter app
echo "🚀 Launching app..."
cd "$PROJECT_ROOT/src"

flutter run -d macos
