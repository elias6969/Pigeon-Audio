#!/bin/bash
SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"
echo "Running from $SCRIPT_DIR"
cd "$SCRIPT_DIR"
./build/Engine
