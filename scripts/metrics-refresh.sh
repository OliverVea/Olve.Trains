#!/usr/bin/env bash
# Regenerate the committed metrics baseline from the current tree.
set -euo pipefail
cd "$(dirname "$0")/.."
dotnet restore Olve.Trains.slnx >/dev/null
METRICS_REPO="$PWD" METRICS_OUT="tools/code-metrics/baseline.json" \
  dotnet run --project tools/code-metrics/code-metrics.csproj -c Release
echo "baseline written: tools/code-metrics/baseline.json"
echo "(to refresh the diagram overlay too, re-run the merge that builds docs/architecture-diagram/code-metrics.json)"
