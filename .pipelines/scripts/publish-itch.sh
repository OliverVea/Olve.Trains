#!/bin/sh
# Processing step: push the Linux + Windows builds to itch.io with butler.
#
# Reads the artifacts from the bundle (/input/<step>/...) produced by the build steps,
# unpacks them, and pushes each platform to its channel. butler is a glibc binary, so
# this runs on debian (not alpine).
set -e

export DEBIAN_FRONTEND=noninteractive
apt-get update
apt-get install -y --no-install-recommends wget unzip tar gzip ca-certificates

# itch.io auth is the BUTLER_API_KEY env var; map it from the declared secret.
export BUTLER_API_KEY="$ITCH_API_KEY"

wget -q -O /tmp/butler.zip https://broth.itch.zone/butler/linux-amd64/LATEST/archive/default
mkdir -p /opt/butler
unzip -q /tmp/butler.zip -d /opt/butler
chmod +x /opt/butler/butler
BUTLER=/opt/butler/butler

VERSION=$(cat "$(ls /input/*/version.txt | head -1)")

LINUX_TGZ=$(ls /input/*/olve-trains-*-linux-x64.tar.gz | head -1)
WIN_ZIP=$(ls /input/*/olve-trains-*-win-x64.zip | head -1)

mkdir -p /stage/linux /stage/win
tar xzf "$LINUX_TGZ" -C /stage/linux   # -> /stage/linux/linux-x64/
unzip -q "$WIN_ZIP" -d /stage/win      # -> /stage/win/win-x64/

echo "publishing $VERSION to $ITCH_TARGET"
"$BUTLER" push /stage/linux/linux-x64 "$ITCH_TARGET:linux"   --userversion "$VERSION"
"$BUTLER" push /stage/win/win-x64     "$ITCH_TARGET:windows" --userversion "$VERSION"
