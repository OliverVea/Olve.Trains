#!/bin/sh
# Processing step (gate): build and run the screenshot integration suite headlessly.
#
# Failure here stops the chain, so publish-minio and publish-itch never run on a regression.
# This builds from source rather than the bundle artifacts because the pytest harness
# launches the framework-dependent `dotnet "On Track To Grow.dll"` over a named pipe.
#
# References live in git LFS, so we `git clone` + `git lfs pull` (the GitHub tarball API
# only ships LFS pointers). NOTE: once references move to the VR app this LFS step goes
# away, and VR review-on-failure can be re-added here.
set -e

export DEBIAN_FRONTEND=noninteractive
apt-get update
apt-get install -y --no-install-recommends \
  ca-certificates curl unzip git git-lfs libassimp-dev procps \
  xvfb libglfw3 mesa-utils libgl1-mesa-dri libglx-mesa0 libegl-mesa0

# uv (drives the pytest integration harness).
curl -LsSf https://astral.sh/uv/install.sh | sh
export PATH="$HOME/.local/bin:$PATH"

WORK=/work
git lfs install
git clone --depth 1 --branch "$BRANCH" \
  "https://x-access-token:$GITHUB_TOKEN@github.com/$REPO.git" "$WORK"
git -C "$WORK" lfs pull
cd "$WORK"

# Build once (asset pipeline + Release), then run the suite against it (--skip-build).
export Build__OutputDirectory="$WORK/src/Olve.Trains/assets"
export Build__BuildDirectory="$WORK/src/Olve.Trains.AssetPipeline/temp"
export Asset__SourceDirectory="$WORK/src/Olve.Trains/resources/assets"
export Shader__ShadersDirectory="$WORK/src/Olve.Trains/resources/shaders"
export Layout__LayoutsDirectory="$WORK/src/Olve.Trains/resources/layouts"
dotnet run --project src/Olve.Trains.AssetPipeline/Olve.Trains.AssetPipeline.csproj
dotnet build src/Olve.Trains/Olve.Trains.csproj --configuration Release

export LIBGL_ALWAYS_SOFTWARE=1
export SCREENSHOT_DIFF_DIR=/tmp/screenshot-diffs
rc=0
bash scripts/integration-test.sh --skip-build --windowing xvfb || rc=$?
[ "$rc" = 0 ] && exit 0

# Failure: export diff/actual/baseline PNGs to the MinIO beta bucket so they can be
# inspected in-cluster (the endpoint is cluster-internal — no presigned URLs, they'd carry
# the private host and not resolve; fetch with an in-cluster mc/aws client). Per-test dir
# holds <name>-diff.png, <name>-actual.txt (path to the render), and <name>-actual.png for
# missing-reference cases; baselines live in the repo.
echo "=== test failed (rc=$rc) — exporting screenshot diffs to MinIO ==="
curl -fsSL https://awscli.amazonaws.com/awscli-exe-linux-x86_64.zip -o /tmp/awscliv2.zip
unzip -q /tmp/awscliv2.zip -d /tmp
/tmp/aws/install -b /usr/local/bin >/dev/null 2>&1 || true
export PATH="/usr/local/bin:$PATH"
export AWS_ACCESS_KEY_ID="$MINIO__Key" AWS_SECRET_ACCESS_KEY="$MINIO__Secret" AWS_DEFAULT_REGION="$MINIO_REGION"
aws configure set default.s3.addressing_style path   # MinIO needs path-style addressing
# Create the diff bucket if it doesn't exist yet (publish-minio does the same; the diff
# export can run before publish-minio ever has). No LocationConstraint — MinIO rejects it.
if ! aws --endpoint-url "$MINIO_ENDPOINT" s3api head-bucket --bucket "$MINIO_DIFF_BUCKET" 2>/dev/null; then
  aws --endpoint-url "$MINIO_ENDPOINT" s3api create-bucket --bucket "$MINIO_DIFF_BUCKET" || true
fi
STAMP=$(date -u +%Y%m%d-%H%M%S)
OUT=/tmp/diffout; mkdir -p "$OUT"
for d in "$SCREENSHOT_DIFF_DIR"/*/; do
  [ -d "$d" ] || continue
  for dp in "$d"*-diff.png "$d"*-actual.png; do [ -f "$dp" ] && cp "$dp" "$OUT/"; done
  for ap in "$d"*-actual.txt; do
    [ -f "$ap" ] || continue
    nm=$(basename "$ap" -actual.txt)
    af=$(cat "$ap"); [ -f "$af" ] && cp "$af" "$OUT/$nm-actual.png"
    bl="tests/integration/reference/linux/$nm.png"; [ -f "$bl" ] && cp "$bl" "$OUT/$nm-baseline.png"
  done
done
if ls "$OUT"/*.png >/dev/null 2>&1; then
  aws --endpoint-url "$MINIO_ENDPOINT" s3 cp "$OUT" "s3://$MINIO_DIFF_BUCKET/diffs/$STAMP/" --recursive >/dev/null
  echo "=== diff images uploaded to MinIO (in-cluster) ==="
  for f in "$OUT"/*.png; do
    echo "DIFF s3://$MINIO_DIFF_BUCKET/diffs/$STAMP/$(basename "$f")"
  done
fi
exit $rc
