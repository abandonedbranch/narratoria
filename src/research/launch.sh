#!/bin/bash
set -e

# Change to the directory of this script
cd "$(dirname "$0")"

# Virtual environment name
VENV_DIR=".venv"
# Ollama model to use
OLLAMA_MODEL="gemma3:1b"

# Check if python3 is available
if ! command -v python3 &> /dev/null; then
    echo "Error: python3 is not installed or not in PATH."
    exit 1
fi

# Check if ollama is installed
if ! command -v ollama &> /dev/null; then
    echo "Error: ollama is not installed or not in PATH."
    echo "Please install Ollama from https://ollama.ai"
    exit 1
fi

# Start Ollama service if not running
echo "Checking Ollama service..."
if ! pgrep -x "ollama" > /dev/null; then
    echo "Starting Ollama service in background..."
    ollama serve > /dev/null 2>&1 &
    # Give it a moment to start
    sleep 2
fi

# Pull model if not already downloaded
echo "Ensuring model $OLLAMA_MODEL is available..."
if ! ollama list | grep -q "$OLLAMA_MODEL"; then
    echo "Pulling model $OLLAMA_MODEL (this may take a few minutes)..."
    ollama pull "$OLLAMA_MODEL"
else
    echo "Model $OLLAMA_MODEL already available."
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
