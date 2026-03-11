#!/bin/bash
set -e

# Integration Test Script (thin wrapper around pytest)
#
# Usage:
#   ./scripts/integration-test.sh [options]
#
# Options:
#   --skip-build        Skip asset pipeline and build steps
#   --windowing <mode>  Windowing mode: "native" or "xvfb" (default: xvfb)
#   --resolution <WxH>  Screen resolution (default: 1920x1080)
#   --update-references Save screenshots as new references

PROJECT_DIR="$(cd "$(dirname "$0")/.." && pwd)"
INTEGRATION_DIR="$PROJECT_DIR/tests/integration"

# Forward all arguments to pytest
cd "$INTEGRATION_DIR"

# Ensure uv is available
if ! command -v uv &> /dev/null; then
    echo "ERROR: uv not found. Install with: curl -LsSf https://astral.sh/uv/install.sh | sh"
    exit 1
fi

# Install dependencies if needed
uv sync --quiet

# Run pytest with all arguments forwarded
exec uv run pytest -v "$@"
