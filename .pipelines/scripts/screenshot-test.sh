#!/bin/sh
set -e
export DEBIAN_FRONTEND=noninteractive
apt-get update -qq
apt-get install -y -qq --no-install-recommends \
  ca-certificates curl libassimp-dev procps \
  xvfb libglfw3 mesa-utils libgl1-mesa-dri libglx-mesa0 libegl-mesa0 >/dev/null

curl -LsSf https://astral.sh/uv/install.sh | sh
export PATH="$HOME/.local/bin:$PATH"

mkdir -p "$(dirname "$WORK")" /work/game
cp -a "$TREE" "$WORK"
tar xzf "$LINUX_TGZ" -C /work/game
export TRAINS_GAME_DIR=/work/game/linux-x64
cd "$WORK"

export LIBGL_ALWAYS_SOFTWARE=1
export SCREENSHOT_DIFF_DIR=/work/screenshot-runs
rc=0
bash scripts/integration-test.sh --skip-build --windowing xvfb || rc=$?
[ "$rc" = 0 ] && exit 0

# Each test's dir holds <name>-diff.png, <name>-actual.png (missing reference) or
# <name>-actual.txt (the path of the render); baselines live in the repo.
mkdir -p "$DIFFS"
for d in "$SCREENSHOT_DIFF_DIR"/*/; do
  [ -d "$d" ] || continue
  for f in "$d"*-diff.png "$d"*-actual.png; do [ -f "$f" ] && cp "$f" "$DIFFS/"; done
  for ap in "$d"*-actual.txt; do
    [ -f "$ap" ] || continue
    nm=$(basename "$ap" -actual.txt)
    af=$(cat "$ap"); [ -f "$af" ] && cp "$af" "$DIFFS/$nm-actual.png"
    bl="tests/integration/reference/linux/$nm.png"; [ -f "$bl" ] && cp "$bl" "$DIFFS/$nm-baseline.png"
  done
done
exit $rc
