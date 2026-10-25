#!/bin/sh
# butler is a glibc binary, so this runs on debian, not alpine.
set -e
export DEBIAN_FRONTEND=noninteractive
apt-get update -qq
apt-get install -y -qq --no-install-recommends wget unzip tar gzip ca-certificates >/dev/null

export BUTLER_API_KEY="$ITCH_API_KEY"
wget -q -O /tmp/butler.zip https://broth.itch.zone/butler/linux-amd64/LATEST/archive/default
mkdir -p /opt/butler
unzip -q /tmp/butler.zip -d /opt/butler
chmod +x /opt/butler/butler

mkdir -p /stage/linux /stage/win
tar xzf "$LINUX_TGZ" -C /stage/linux
unzip -q "$WIN_ZIP" -d /stage/win

echo "publishing $VERSION to $ITCH_TARGET"
/opt/butler/butler push /stage/linux/linux-x64 "$ITCH_TARGET:linux" --userversion "$VERSION"
/opt/butler/butler push /stage/win/win-x64 "$ITCH_TARGET:windows" --userversion "$VERSION"
