#!/bin/bash
set -e

# Integration Test Script
# Places a station, builds a track loop around it, adds a Y-junction branch
# (triggering junction signals), places 3 trains, selects the track placement
# tool (triggering the arrow indicator), waits for simulation, then takes
# screenshots at 3 times of day (7:30, 11:30, 22:30) and optionally uploads/saves them.
#
# Usage:
#   ./scripts/integration-test.sh [options]
#
# Options:
#   --s3                Upload screenshot to S3 and print presigned URL
#   --file <path>       Save screenshot to the given file path
#   --skip-build        Skip asset pipeline and build steps
#   --instance <id>     Game instance ID (default: integration-test-$$)
#   --windowing <mode>  Windowing mode: "native" or "xvfb" (default: native)
#   --resolution <WxH>  Screen resolution for xvfb (default: 3840x2160)
#
# If neither --s3 nor --file is given, no screenshot is taken.

PROJECT_DIR="$(cd "$(dirname "$0")/.." && pwd)"
BIN_DIR="$PROJECT_DIR/src/Olve.Trains/bin/Release/net10.0"
INSTANCE_ID="integration-test-$$"
S3_BUCKET="olve.trains"
S3_KEY_PREFIX="screenshots/integration-test"
S3_REGION="ap-southeast-2"
SHLINK_API_KEY="${SHLINK_API_KEY:-}"
SHLINK_URL="https://s.ovhome.online"
SKIP_BUILD=false
WINDOWING="native"
RESOLUTION="1920x1080"
OUTPUT_MODE=""  # "", "s3", or "file"
OUTPUT_FILE=""

# Parse arguments
while [[ $# -gt 0 ]]; do
    case $1 in
        --s3)
            OUTPUT_MODE="s3"
            shift
            ;;
        --file)
            OUTPUT_MODE="file"
            OUTPUT_FILE="$2"
            shift 2
            ;;
        --skip-build)
            SKIP_BUILD=true
            shift
            ;;
        --instance)
            INSTANCE_ID="$2"
            shift 2
            ;;
        --windowing)
            WINDOWING="$2"
            shift 2
            ;;
        --resolution)
            RESOLUTION="$2"
            shift 2
            ;;
        *)
            echo "Unknown option: $1"
            exit 1
            ;;
    esac
done

# Screenshot times and labels
TIMES=("7:30" "11:30" "22:30")
LABELS=("0730" "1130" "2230")

# Determine screenshot paths
TEMP_DIR=""
if [ "$OUTPUT_MODE" = "s3" ]; then
    TEMP_DIR=$(mktemp -d /tmp/integration-test-XXXXXX)
fi

cleanup() {
    echo "Cleaning up..."
    kill $GAME_PID 2>/dev/null || true
    if [ -n "$XVFB_PID" ]; then
        kill $XVFB_PID 2>/dev/null || true
        rm -f /tmp/.X99-lock 2>/dev/null || true
    fi
    if [ -n "$TEMP_DIR" ]; then
        rm -rf "$TEMP_DIR" 2>/dev/null || true
    fi
}
trap cleanup EXIT

# Build if needed
if [ "$SKIP_BUILD" = false ]; then
    echo "=== Asset Pipeline ==="
    dotnet run --project "$PROJECT_DIR/src/Olve.Trains.AssetPipeline/Olve.Trains.AssetPipeline.csproj"

    echo ""
    echo "=== Build (Release) ==="
    dotnet build "$PROJECT_DIR/src/Olve.Trains/Olve.Trains.csproj" --configuration Release
fi

echo ""
echo "=== Launch Game ==="

XVFB_PID=""

# Clean up stale processes
pkill -f "On Track To Grow" 2>/dev/null || true
sleep 1

if [ "$WINDOWING" = "xvfb" ]; then
    echo "Starting Xvfb..."
    rm -f /tmp/.X99-lock 2>/dev/null || true
    pkill -f "Xvfb :99" 2>/dev/null || true
    Xvfb :99 -screen 0 "${RESOLUTION}x24" &
    XVFB_PID=$!
    export DISPLAY=:99
    export LIBGL_ALWAYS_SOFTWARE=1
    sleep 2
fi

send_cmd() {
    dotnet "$BIN_DIR/On Track To Grow.dll" --send "$1" --instance "$INSTANCE_ID"
}

# Launch game
echo "Launching game..."
dotnet "$BIN_DIR/On Track To Grow.dll" --listen --instance "$INSTANCE_ID" --resolution "$RESOLUTION" &
GAME_PID=$!
sleep 5

# Navigate from main menu
echo "Starting game from main menu..."
send_cmd "start-game"
sleep 2

Y="0.125"

# Set camera first to center on the layout
echo "Setting camera..."
send_cmd "set-camera target=12.5,0,10 zoom=2.5"

echo ""
echo "=== Placing Station ==="

# Station at tile (11,1,12) facing north — creates a straight track along Z=12
# The station blueprint is 6 wide, so the station track runs from ~(10.5,12.5) to ~(17.5,12.5)
send_cmd "place-building pos=11,1,12 type=station dir=north"

echo ""
echo "=== Placing Track Loop ==="

# Station track runs from (10.5, Y, 12.5) to (17.5, Y, 12.5) — use tile centers (.5)
# Top-right curve: from station end (15.5,12.5) going east, curving south to (17.5,10.5)
echo "Placing top-right curve..."
TRACK1_OUTPUT=$(send_cmd "place-track start=15.5,$Y,12.5 end=17.5,$Y,10.5 start-dir=east end-dir=south" 2>&1)
echo "$TRACK1_OUTPUT"
TRACK1_ID=$(echo "$TRACK1_OUTPUT" | grep -oE '[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}' | head -1)

# Right curve: from (17.5,10.5) going south, curving west to (15.5,8.5)
echo "Placing bottom-right curve..."
TRACK2_OUTPUT=$(send_cmd "place-track start=17.5,$Y,10.5 end=15.5,$Y,8.5 start-dir=south end-dir=west" 2>&1)
echo "$TRACK2_OUTPUT"
TRACK2_ID=$(echo "$TRACK2_OUTPUT" | grep -oE '[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}' | head -1)

# Bottom straight: from (15.5,8.5) going west to (10.5,8.5)
echo "Placing bottom straight..."
send_cmd "place-track start=15.5,$Y,8.5 end=10.5,$Y,8.5 start-dir=west end-dir=west"

# Bottom-left curve: from (10.5,8.5) going west, curving north to (8.5,10.5)
echo "Placing bottom-left curve..."
TRACK3_OUTPUT=$(send_cmd "place-track start=10.5,$Y,8.5 end=8.5,$Y,10.5 start-dir=west end-dir=north" 2>&1)
echo "$TRACK3_OUTPUT"
TRACK3_ID=$(echo "$TRACK3_OUTPUT" | grep -oE '[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}' | head -1)

# Top-left curve: from (8.5,10.5) going north, curving east to (10.5,12.5) — completing the loop
echo "Placing top-left curve..."
send_cmd "place-track start=8.5,$Y,10.5 end=10.5,$Y,12.5 start-dir=north end-dir=east"

echo ""
echo "=== Placing Y-Junction Branch ==="

# Branch off the bottom-left junction (10.5,8.5) heading west — creates a 3-way junction with a signal
echo "Placing branch track..."
send_cmd "place-track start=10.5,$Y,8.5 end=8.5,$Y,8.5 start-dir=west end-dir=west"

echo ""
echo "=== Placing 3 Trains ==="

# Place trains on different curve segments for spacing
echo "Placing train 1..."
send_cmd "place-vehicle track=$TRACK1_ID speed=3"

echo "Placing train 2..."
send_cmd "place-vehicle track=$TRACK2_ID speed=3"

echo "Placing train 3..."
send_cmd "place-vehicle track=$TRACK3_ID speed=3"

# Wait for trains to move around the loop
echo "Waiting for simulation..."
sleep 5

# Select track placement tool so the arrow indicator is visible
echo "Selecting track placement tool..."
send_cmd "select-tool name='Place Tracks'"
sleep 0.5

# Move mouse to center of screen so the grid overlay and arrow indicator are visible
send_cmd "set-mouse pos=0,0"
sleep 0.5

# Screenshot handling — take screenshots at multiple times of day
if [ -n "$OUTPUT_MODE" ]; then
    echo ""
    echo "=== Screenshots ==="

    if [ "$OUTPUT_MODE" = "s3" ]; then
        if ! command -v aws &> /dev/null; then
            echo "ERROR: AWS CLI not found. Install with: pip3 install awscli && aws configure"
            send_cmd "exit" || true
            exit 1
        fi
    fi

    for i in "${!TIMES[@]}"; do
        TIME="${TIMES[$i]}"
        LABEL="${LABELS[$i]}"

        # Determine path for this screenshot
        if [ "$OUTPUT_MODE" = "file" ]; then
            # Insert label before extension: /path/to/file.png -> /path/to/file-0730.png
            SCREENSHOT_PATH="${OUTPUT_FILE%.png}-${LABEL}.png"
        else
            SCREENSHOT_PATH="$TEMP_DIR/integration-test-${LABEL}.png"
        fi

        echo "Setting time to ${TIME}..."
        send_cmd "set-time time=${TIME}"
        sleep 1

        echo "Taking screenshot at ${TIME}..."
        send_cmd "screenshot path=$SCREENSHOT_PATH"
        sleep 2

        if [ ! -f "$SCREENSHOT_PATH" ]; then
            echo "ERROR: Screenshot file not found at $SCREENSHOT_PATH"
            send_cmd "exit" || true
            exit 1
        fi

        FILE_SIZE=$(stat -c%s "$SCREENSHOT_PATH" 2>/dev/null || stat -f%z "$SCREENSHOT_PATH" 2>/dev/null)
        echo "  Saved: $SCREENSHOT_PATH ($FILE_SIZE bytes)"

        if [ "$FILE_SIZE" -lt 1000 ]; then
            echo "ERROR: Screenshot file is too small ($FILE_SIZE bytes), likely corrupt"
            send_cmd "exit" || true
            exit 1
        fi
    done

    if [ "$OUTPUT_MODE" = "s3" ]; then
        echo ""
        echo "Uploading to S3..."
        echo ""
        echo "============================================"
        echo "Screenshots uploaded successfully!"

        for i in "${!LABELS[@]}"; do
            LABEL="${LABELS[$i]}"
            SCREENSHOT_PATH="$TEMP_DIR/integration-test-${LABEL}.png"
            S3_KEY="${S3_KEY_PREFIX}-${LABEL}.png"

            aws s3 cp "$SCREENSHOT_PATH" "s3://${S3_BUCKET}/${S3_KEY}" --region "$S3_REGION"

            PRESIGNED_URL=$(aws s3 presign "s3://${S3_BUCKET}/${S3_KEY}" --region "$S3_REGION" --expires-in 3600)

            # Shorten URL via Shlink if API key is available
            SHORT_URL=""
            if [ -n "$SHLINK_API_KEY" ]; then
                SHORT_URL=$(curl -sf -X POST "${SHLINK_URL}/rest/v3/short-urls" \
                    -H "X-Api-Key: ${SHLINK_API_KEY}" \
                    -H "Content-Type: application/json" \
                    -d "{\"longUrl\": \"${PRESIGNED_URL}\"}" | python3 -c "import sys,json; print(json.load(sys.stdin)['shortUrl'])" 2>/dev/null || true)
            fi

            if [ -n "$SHORT_URL" ]; then
                echo "  ${TIMES[$i]}: $SHORT_URL"
            else
                echo "  ${TIMES[$i]}: $PRESIGNED_URL"
            fi
        done

        echo "============================================"
    elif [ "$OUTPUT_MODE" = "file" ]; then
        echo ""
        echo "============================================"
        for i in "${!LABELS[@]}"; do
            LABEL="${LABELS[$i]}"
            echo "  ${TIMES[$i]}: ${OUTPUT_FILE%.png}-${LABEL}.png"
        done
        echo "============================================"
    fi
fi

echo ""
echo "=== Shutting Down ==="
send_cmd "exit" || true
sleep 1

echo ""
echo "==============================="
echo "  INTEGRATION TEST PASSED"
echo "==============================="
exit 0
