#!/bin/bash
set -e

# Build + Screenshot Validation Script
# Runs the full asset pipeline, builds the project, launches the game headlessly,
# places tracks/vehicles, takes a screenshot, and validates the output.
#
# Usage:
#   ./scripts/validate-build.sh              # Full run (asset pipeline + build + validate)
#   ./scripts/validate-build.sh --skip-build  # Skip asset pipeline and build (for CI)

PROJECT_DIR="$(cd "$(dirname "$0")/.." && pwd)"
BIN_DIR="$PROJECT_DIR/src/Olve.Trains/bin/Release/net10.0"
INSTANCE_ID="validation-$$"
SCREENSHOT_PATH="$PROJECT_DIR/validation-screenshot.png"
SKIP_BUILD=false

for arg in "$@"; do
    case $arg in
        --skip-build) SKIP_BUILD=true ;;
    esac
done

cleanup() {
    echo "Cleaning up..."
    kill $GAME_PID 2>/dev/null || true
    kill $XVFB_PID 2>/dev/null || true
    rm -f /tmp/.X99-lock 2>/dev/null || true
}
trap cleanup EXIT

if [ "$SKIP_BUILD" = false ]; then
    echo "=== Step 1: Asset Pipeline ==="
    dotnet run --project "$PROJECT_DIR/src/Olve.Trains.AssetPipeline/Olve.Trains.AssetPipeline.csproj"

    echo ""
    echo "=== Step 2: Build (Release) ==="
    dotnet build "$PROJECT_DIR/src/Olve.Trains/Olve.Trains.csproj" --configuration Release
else
    echo "=== Skipping asset pipeline and build ==="
fi

echo ""
echo "=== Launch Game Headlessly ==="

# Clean up stale processes
rm -f /tmp/.X99-lock 2>/dev/null || true
pkill -f "Xvfb :99" 2>/dev/null || true
pkill -f "On Track To Grow" 2>/dev/null || true
sleep 1

# Start Xvfb with software rendering
Xvfb :99 -screen 0 1920x1080x24 &
XVFB_PID=$!
export DISPLAY=:99
export LIBGL_ALWAYS_SOFTWARE=1
sleep 2

# Function to send command to game
send_cmd() {
    dotnet "$BIN_DIR/On Track To Grow.dll" --send "$1" --instance "$INSTANCE_ID"
}

# Launch game
dotnet "$BIN_DIR/On Track To Grow.dll" --listen --instance "$INSTANCE_ID" &
GAME_PID=$!
sleep 5

echo ""
echo "=== Place Entities ==="

# Navigate to game
send_cmd "start-game"
sleep 3

Y="0.125"

# Place a circular track (4 segments)
echo "Placing track segments..."
TRACK1_OUTPUT=$(send_cmd "place-track start=3,$Y,1 end=5,$Y,3 start-dir=east end-dir=north" 2>&1)
echo "$TRACK1_OUTPUT"
TRACK1_ID=$(echo "$TRACK1_OUTPUT" | grep -oE '[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}' | head -1)
sleep 0.5

send_cmd "place-track start=5,$Y,3 end=3,$Y,5 start-dir=north end-dir=west"
sleep 0.5

send_cmd "place-track start=3,$Y,5 end=1,$Y,3 start-dir=west end-dir=south"
sleep 0.5

send_cmd "place-track start=1,$Y,3 end=3,$Y,1 start-dir=south end-dir=east"
sleep 0.5

# Place a vehicle
echo "Placing vehicle..."
send_cmd "place-vehicle track=$TRACK1_ID"
sleep 1

# Set camera
send_cmd "set-camera target=3,0,3 zoom=10"
sleep 0.5

echo ""
echo "=== Take Screenshot ==="
rm -f "$SCREENSHOT_PATH"
send_cmd "screenshot path=$SCREENSHOT_PATH"
sleep 2

echo ""
echo "=== Validate ==="

send_cmd "exit" || true
sleep 1

if [ -f "$SCREENSHOT_PATH" ]; then
    FILE_SIZE=$(stat -c%s "$SCREENSHOT_PATH" 2>/dev/null || stat -f%z "$SCREENSHOT_PATH" 2>/dev/null)
    echo "Screenshot saved: $SCREENSHOT_PATH ($FILE_SIZE bytes)"

    if [ "$FILE_SIZE" -gt 1000 ]; then
        echo ""
        echo "==============================="
        echo "  VALIDATION PASSED"
        echo "==============================="
        exit 0
    else
        echo "ERROR: Screenshot file is too small ($FILE_SIZE bytes), likely corrupt"
        exit 1
    fi
else
    echo "ERROR: Screenshot file not found at $SCREENSHOT_PATH"
    exit 1
fi
