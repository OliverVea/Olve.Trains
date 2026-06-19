#!/bin/sh
# Processing step (gate): build and run the screenshot integration suite headlessly.
#
# Failure here stops the chain, so publish-itch and publish-s3 never run on a regression.
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
  ca-certificates curl git git-lfs libassimp-dev procps \
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
export Shader__ShadersDirectory="$WORK/src/Olve.Trains/resources/shaders"
export Layout__LayoutsDirectory="$WORK/src/Olve.Trains/resources/layouts"
dotnet run --project src/Olve.Trains.AssetPipeline/Olve.Trains.AssetPipeline.csproj
dotnet build src/Olve.Trains/Olve.Trains.csproj --configuration Release

export LIBGL_ALWAYS_SOFTWARE=1
bash scripts/integration-test.sh --skip-build --windowing xvfb
