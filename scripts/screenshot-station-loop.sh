#!/bin/bash
set -e

PROJECT_DIR="/home/oliver/projects/Olve.Trains"
BIN_DIR="$PROJECT_DIR/src/Olve.Trains/bin/Release/net10.0"
INSTANCE_ID="screenshot-session"
SCREENSHOT_PATH="$PROJECT_DIR/station-loop-screenshot.png"
S3_BUCKET="olve.trains"
S3_KEY="screenshots/station-loop-screenshot.png"
S3_REGION="ap-southeast-2"
SHLINK_API_KEY="${SHLINK_API_KEY:-78ed24399fc8fbc76e8a3872eb3a69a226c980e5530531f3772227126edee83b}"
SHLINK_URL="http://localhost:8844"

# Cleanup any stale processes
rm -f /tmp/.X99-lock 2>/dev/null || true
pkill -f "Xvfb :99" 2>/dev/null || true
pkill -f "On Track To Grow" 2>/dev/null || true
sleep 1

# Function to send command to game
send_cmd() {
    dotnet "$BIN_DIR/On Track To Grow.dll" --send "$1" --instance "$INSTANCE_ID"
}

echo "Starting game with Xvfb (3840x2160)..."

# Start Xvfb with 24-bit color depth
Xvfb :99 -screen 0 3840x2160x24 &
XVFB_PID=$!
export DISPLAY=:99

# Give Xvfb time to start
sleep 2

# Start the game in background with listening enabled
echo "Launching game..."
dotnet "$BIN_DIR/On Track To Grow.dll" --listen --instance "$INSTANCE_ID" &
GAME_PID=$!

# Wait for game to initialize
echo "Waiting for game to initialize..."
sleep 5

# First need to start the actual game (navigate from main menu)
echo "Starting game from main menu..."
send_cmd "start-game"
sleep 3

# Create a rectangular loop with a station on one side
# Station will be at position 2,2 facing east (creates track from 1,2 to 6,2)
# Then we'll complete the loop with curved tracks

echo "Creating station and loop track..."

# Track height
Y="0.125"

# Place the station (4 units wide, creates track automatically)
echo "Placing station..."
STATION_OUTPUT=$(send_cmd "place-building pos=2,2 type=station dir=east" 2>&1)
echo "$STATION_OUTPUT"
sleep 1

# The station creates a track from approximately (1, Y, 2) to (6, Y, 2)
# Now create the rest of the loop

# Right side: curve from station end (6,2) north to (6,6)
echo "Placing right curve..."
TRACK1_OUTPUT=$(send_cmd "place-track start=6,$Y,2 end=6,$Y,6 start-dir=north end-dir=west" 2>&1)
echo "$TRACK1_OUTPUT"
TRACK1_ID=$(echo "$TRACK1_OUTPUT" | grep -oE '[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}' | head -1)
sleep 0.5

# Top side: straight track from (6,6) to (1,6) going west
echo "Placing top straight track..."
send_cmd "place-track start=6,$Y,6 end=1,$Y,6 start-dir=west end-dir=west"
sleep 0.5

# Left side: curve from (1,6) south to (1,2)
echo "Placing left curve..."
TRACK2_OUTPUT=$(send_cmd "place-track start=1,$Y,6 end=1,$Y,2 start-dir=south end-dir=east" 2>&1)
echo "$TRACK2_OUTPUT"
TRACK2_ID=$(echo "$TRACK2_OUTPUT" | grep -oE '[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}' | head -1)
sleep 0.5

# Set camera to center on the track loop (center is around 3.5,0,4) with zoom showing the whole loop
echo "Setting camera position..."
send_cmd "set-camera target=3.5,0,4 zoom=6"
sleep 0.5

# Place first vehicle on the first curved track segment
echo "Placing first vehicle..."
send_cmd "place-vehicle track=$TRACK1_ID"
sleep 1

# Place second vehicle on the opposite side for spacing
echo "Placing second vehicle..."
send_cmd "place-vehicle track=$TRACK2_ID"
sleep 1

# Add a couple of residential buildings near the station
echo "Placing residential buildings..."
send_cmd "place-building pos=4,8 type=residential dir=north"
sleep 0.5
send_cmd "place-building pos=6,8 type=residential dir=south"
sleep 0.5

# Take screenshot
echo "Taking screenshot..."
send_cmd "screenshot path=$SCREENSHOT_PATH"
sleep 2

echo "Shutting down..."
send_cmd "exit" || true
sleep 1

# Cleanup
kill $GAME_PID 2>/dev/null || true
kill $XVFB_PID 2>/dev/null || true

echo "Screenshot saved to: $SCREENSHOT_PATH"

if [ -f "$SCREENSHOT_PATH" ]; then
    echo "Screenshot file exists! Size: $(ls -lh "$SCREENSHOT_PATH" | awk '{print $5}')"

    # Upload to S3 (requires: pip3 install awscli && aws configure)
    if command -v aws &> /dev/null; then
        echo "Uploading to S3..."
        aws s3 cp "$SCREENSHOT_PATH" "s3://${S3_BUCKET}/${S3_KEY}" --region "$S3_REGION"

        # Generate presigned URL (expires in 1 hour)
        PRESIGNED_URL=$(aws s3 presign "s3://${S3_BUCKET}/${S3_KEY}" --region "$S3_REGION" --expires-in 3600)

        # Shorten URL via Shlink if API key is available
        SHORT_URL=""
        if [ -n "$SHLINK_API_KEY" ]; then
            SHORT_URL=$(curl -sf -X POST "${SHLINK_URL}/rest/v3/short-urls" \
                -H "X-Api-Key: ${SHLINK_API_KEY}" \
                -H "Content-Type: application/json" \
                -d "{\"longUrl\": \"${PRESIGNED_URL}\"}" | python3 -c "import sys,json; print(json.load(sys.stdin)['shortUrl'])" 2>/dev/null || true)
        fi

        echo ""
        echo "============================================"
        echo "Screenshot uploaded successfully!"
        if [ -n "$SHORT_URL" ]; then
            echo "Short URL: $SHORT_URL"
        else
            echo "Presigned URL (expires in 1 hour):"
            echo "$PRESIGNED_URL"
        fi
        echo "============================================"

    else
        echo ""
        echo "AWS CLI not found. To enable S3 upload, run:"
        echo "  pip3 install awscli && aws configure"
        echo ""
        echo "Local screenshot: $SCREENSHOT_PATH"
    fi
else
    echo "WARNING: Screenshot file not found!"
fi