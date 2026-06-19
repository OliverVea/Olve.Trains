#!/bin/sh
# Production step: build a self-contained linux-x64 release of On Track To Grow.
#
# Runs the asset pipeline (reads S3__Bucket/Key/Secret from the injected secrets), then
# `dotnet publish`. The packaged tarball + a version.txt are written to /output, which
# becomes bundle/<step>/ in the ArtifactBundle the processing steps consume from /input.
set -e

export DEBIAN_FRONTEND=noninteractive
apt-get update
apt-get install -y --no-install-recommends wget tar gzip ca-certificates libassimp-dev

WORK=/work
mkdir -p "$WORK"

# No .git in the API tarball, so derive a version from the date + the branch-head short SHA.
SHORT_SHA=$(wget -q --header="Authorization: token $GITHUB_TOKEN" -O - \
  "https://api.github.com/repos/$REPO/commits/$BRANCH" \
  | grep -m1 '"sha"' | cut -d'"' -f4 | cut -c1-7)
ASSEMBLY_VERSION=$(date -u +%Y.%m.%d.%H%M)        # valid System.Version (each part <= 65534)
RELEASE_VERSION="${ASSEMBLY_VERSION}-${SHORT_SHA}" # display / itch userversion / S3 path

# Fetch the source (strip the GitHub tarball's top-level <owner>-<repo>-<sha>/ dir).
wget -q --header="Authorization: token $GITHUB_TOKEN" -O /tmp/repo.tar.gz \
  "https://api.github.com/repos/$REPO/tarball/$BRANCH"
tar xzf /tmp/repo.tar.gz -C "$WORK" --strip-components=1
cd "$WORK"

# Compile assets (S3__* secrets are injected as env vars matching the .NET config keys).
# Set the pipeline directories explicitly (defaults are project-cwd relative).
export Build__OutputDirectory="$WORK/src/Olve.Trains/assets"
export Build__BuildDirectory="$WORK/src/Olve.Trains.AssetPipeline/temp"
export Shader__ShadersDirectory="$WORK/src/Olve.Trains/resources/shaders"
export Layout__LayoutsDirectory="$WORK/src/Olve.Trains/resources/layouts"
dotnet run --project src/Olve.Trains.AssetPipeline/Olve.Trains.AssetPipeline.csproj

dotnet publish src/Olve.Trains/Olve.Trains.csproj \
  --configuration Release --runtime linux-x64 --self-contained \
  -p:Version="$ASSEMBLY_VERSION" --output publish/linux-x64

mkdir -p /output
( cd publish && tar czf "/output/olve-trains-${RELEASE_VERSION}-linux-x64.tar.gz" linux-x64 )
echo "$RELEASE_VERSION" > /output/version.txt
echo "built olve-trains-${RELEASE_VERSION}-linux-x64.tar.gz"
