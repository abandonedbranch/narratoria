#!/bin/bash
set -e

# Change to the directory of this script
cd "$(dirname "$0")"

# Virtual environment name
VENV_DIR=".venv"

# Check if python3 is available
if ! command -v python3 &> /dev/null; then
    echo "Error: python3 is not installed or not in PATH."
    exit 1
fi

# Create virtual environment if it doesn't exist
if [ ! -d "$VENV_DIR" ]; then
    echo "Creating virtual environment in $VENV_DIR..."
    python3 -m venv "$VENV_DIR"
fi

# Activate virtual environment
echo "Activating virtual environment..."
source "$VENV_DIR/bin/activate"

# Upgrade pip to ensure smooth installation
echo "Upgrading pip..."
python -m pip install --upgrade pip

# Install dependencies
if [ -f "requirements.txt" ]; then
    echo "Installing/Updating dependencies from requirements.txt..."
    pip install -r requirements.txt
else
    echo "Warning: requirements.txt not found."
fi

# Run the TUI
echo "Starting Narratoria TUI Prototype..."
python tui.py
