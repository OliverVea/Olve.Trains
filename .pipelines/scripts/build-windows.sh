#!/bin/sh
# Production step: cross-compile a self-contained win-x64 release of On Track To Grow.
#
# Identical to build-linux except for the RID and packaging (.zip). Self-contained
# win-x64 publishes cleanly from this Linux SDK image — no Windows runner needed.
set -e

export DEBIAN_FRONTEND=noninteractive
apt-get update
apt-get install -y --no-install-recommends wget tar gzip zip ca-certificates libassimp-dev

WORK=/work
mkdir -p "$WORK"

SHORT_SHA=$(wget -q --header="Authorization: token $GITHUB_TOKEN" -O - \
  "https://api.github.com/repos/$REPO/commits/$BRANCH" \
  | grep -m1 '"sha"' | cut -d'"' -f4 | cut -c1-7)
ASSEMBLY_VERSION=$(date -u +%Y.%m.%d.%H%M)
RELEASE_VERSION="${ASSEMBLY_VERSION}-${SHORT_SHA}"

wget -q --header="Authorization: token $GITHUB_TOKEN" -O /tmp/repo.tar.gz \
  "https://api.github.com/repos/$REPO/tarball/$BRANCH"
tar xzf /tmp/repo.tar.gz -C "$WORK" --strip-components=1
cd "$WORK"

# The asset pipeline runs on Linux and emits platform-neutral assets for the win-x64 build.
export Build__OutputDirectory="$WORK/src/Olve.Trains/assets"
export Build__BuildDirectory="$WORK/src/Olve.Trains.AssetPipeline/temp"
export Shader__ShadersDirectory="$WORK/src/Olve.Trains/resources/shaders"
export Layout__LayoutsDirectory="$WORK/src/Olve.Trains/resources/layouts"
dotnet run --project src/Olve.Trains.AssetPipeline/Olve.Trains.AssetPipeline.csproj

dotnet publish src/Olve.Trains/Olve.Trains.csproj \
  --configuration Release --runtime win-x64 --self-contained \
  -p:Version="$ASSEMBLY_VERSION" --output publish/win-x64

mkdir -p /output
( cd publish && zip -qr "/output/olve-trains-${RELEASE_VERSION}-win-x64.zip" win-x64 )
echo "$RELEASE_VERSION" > /output/version.txt
echo "built olve-trains-${RELEASE_VERSION}-win-x64.zip"
