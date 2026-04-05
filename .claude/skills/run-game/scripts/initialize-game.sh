#!/usr/bin/env bash
# Initialize the game with the industry-loop replay scenario.
# Usage: bash .claude/skills/run-game/scripts/initialize-game.sh [--windowing native|xvfb]

set -euo pipefail

REPO_ROOT="$(cd "$(dirname "$0")/../../../.." && pwd)"
WINDOWING=""

for arg in "$@"; do
    case "$arg" in
        --windowing) shift; WINDOWING="$1"; shift ;;
        native|xvfb) WINDOWING="$arg" ;;
    esac
done

# Build Release
dotnet build "$REPO_ROOT/src/Olve.Trains/Olve.Trains.csproj" --configuration Release -v quiet

# Run the industry-loop replay
ARGS=("$REPO_ROOT/scripts/replays/industry-loop.py" --skip-build)
if [ -n "$WINDOWING" ]; then
    ARGS+=(--windowing "$WINDOWING")
fi

"$REPO_ROOT/tests/integration/.venv/Scripts/python.exe" "${ARGS[@]}"
