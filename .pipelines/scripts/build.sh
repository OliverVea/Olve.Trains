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
apt-get install -y --no-install-recommends wget tar gzip zip ca-certificates libassimp-dev

WORK=/work
mkdir -p "$WORK"

# No .git in the API tarball, so derive a version from the date + branch-head short SHA.
SHORT_SHA=$(wget -q --header="Authorization: token $GITHUB_TOKEN" -O - \
  "https://api.github.com/repos/$REPO/commits/$BRANCH" \
  | grep -m1 '"sha"' | cut -d'"' -f4 | cut -c1-7)
ASSEMBLY_VERSION=$(date -u +%Y.%m.%d.%H%M)        # valid System.Version (each part <= 65534)
RELEASE_VERSION="${ASSEMBLY_VERSION}-${SHORT_SHA}" # display / itch userversion / S3 path

wget -q --header="Authorization: token $GITHUB_TOKEN" -O /tmp/repo.tar.gz \
  "https://api.github.com/repos/$REPO/tarball/$BRANCH"
tar xzf /tmp/repo.tar.gz -C "$WORK" --strip-components=1
cd "$WORK"

# Compile assets once (S3__* injected as env; bucket/prefix come from appsettings.json).
export Build__OutputDirectory="$WORK/src/Olve.Trains/assets"
export Build__BuildDirectory="$WORK/src/Olve.Trains.AssetPipeline/temp"
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
