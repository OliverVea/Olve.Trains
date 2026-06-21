#!/usr/bin/env bash
# Canonical quality gate — an agent-runnable self-check. Run this GREEN before raising a CR.
# Local only; deliberately NOT wired into the CD pipeline.
#   - architecture fitness tests : BLOCKING (boundary/cohesion violations fail the gate)
#   - code-metrics ratchet       : advisory by default; QUALITY_STRICT=1 makes it blocking
set -euo pipefail
cd "$(dirname "$0")/.."
ROOT="$PWD"

echo "== [1/3] restore =="
dotnet restore Olve.Trains.slnx >/dev/null
dotnet restore tools/code-metrics/code-metrics.csproj >/dev/null

echo "== [2/3] architecture fitness tests (blocking) =="
dotnet run --project tests/Olve.Architecture.Tests/Olve.Architecture.Tests.csproj -c Release

echo "== [3/3] code-metrics ratchet =="
CUR="$(mktemp)"
METRICS_REPO="$ROOT" METRICS_OUT="$CUR" \
  dotnet run --project tools/code-metrics/code-metrics.csproj -c Release >/dev/null
python3 scripts/metrics-ratchet.py tools/code-metrics/baseline.json "$CUR" ${QUALITY_STRICT:+--strict}

echo "== quality gate: PASSED =="
