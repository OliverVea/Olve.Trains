#!/bin/sh
# Production step: build self-contained linux-x64 AND win-x64 releases in one job.
#
# A single production step (rather than two parallel ones) is deliberate: with two
# parallel production steps the controller promotes the bundle twice and the resulting
# duplicate first-processing-step jobs mutually supersede each other (deadlock, neither
# runs). One step also lets the asset pipeline run once and feed both publishes.
#
# Writes both archives + version.txt to /output (-> the ArtifactBundle the processing
# steps read from /input/<step>/).
set -e

export DEBIAN_FRONTEND=noninteractive
apt-get update
apt-get install -y --no-install-recommends git git-lfs tar gzip zip ca-certificates libassimp-dev

WORK=/work

# Source art lives in git LFS, so clone + lfs pull (the GitHub tarball API only ships
# LFS pointers). The asset pipeline reads it from src/Olve.Trains/resources/assets/.
git lfs install
git clone --depth 1 --branch "$BRANCH" \
  "https://x-access-token:$GITHUB_TOKEN@github.com/$REPO.git" "$WORK"
git -C "$WORK" lfs pull
cd "$WORK"

SHORT_SHA=$(git rev-parse --short=7 HEAD)
ASSEMBLY_VERSION=$(date -u +%Y.%m.%d.%H%M)        # valid System.Version (each part <= 65534)
RELEASE_VERSION="${ASSEMBLY_VERSION}-${SHORT_SHA}" # display / itch userversion / S3 path

# Compile assets once (source art read from the committed LFS directory).
export Build__OutputDirectory="$WORK/src/Olve.Trains/assets"
export Build__BuildDirectory="$WORK/src/Olve.Trains.AssetPipeline/temp"
export Asset__SourceDirectory="$WORK/src/Olve.Trains/resources/assets"
export Shader__ShadersDirectory="$WORK/src/Olve.Trains/resources/shaders"
export Layout__LayoutsDirectory="$WORK/src/Olve.Trains/resources/layouts"
dotnet run --project src/Olve.Trains.AssetPipeline/Olve.Trains.AssetPipeline.csproj

mkdir -p /output

# linux-x64
dotnet publish src/Olve.Trains/Olve.Trains.csproj \
  --configuration Release --runtime linux-x64 --self-contained \
  -p:Version="$ASSEMBLY_VERSION" --output publish/linux-x64
( cd publish && tar czf "/output/olve-trains-${RELEASE_VERSION}-linux-x64.tar.gz" linux-x64 )

# win-x64 (cross-compiled from this Linux image)
dotnet publish src/Olve.Trains/Olve.Trains.csproj \
  --configuration Release --runtime win-x64 --self-contained \
  -p:Version="$ASSEMBLY_VERSION" --output publish/win-x64
( cd publish && zip -qr "/output/olve-trains-${RELEASE_VERSION}-win-x64.zip" win-x64 )

echo "$RELEASE_VERSION" > /output/version.txt
echo "built linux + win @ $RELEASE_VERSION"
