#!/bin/sh
set -e
export DEBIAN_FRONTEND=noninteractive
apt-get update -qq
apt-get install -y -qq --no-install-recommends zip libassimp-dev >/dev/null

mkdir -p "$(dirname "$WORK")" "$OUT"
cp -a "$TREE" "$WORK"
cd "$WORK"

ASSEMBLY_VERSION=$(date -u +%Y.%m.%d.%H%M)   # a valid System.Version (each part <= 65534)
VERSION="$ASSEMBLY_VERSION-$(echo "$SHA" | cut -c1-7)"

dotnet run --project src/Olve.Trains.AssetPipeline/Olve.Trains.AssetPipeline.csproj

for rid in linux-x64 win-x64; do
  dotnet publish src/Olve.Trains/Olve.Trains.csproj \
    --configuration Release --runtime "$rid" --self-contained \
    -p:Version="$ASSEMBLY_VERSION" --output "publish/$rid"
done
( cd publish && tar czf "$OUT/linux-x64.tar.gz" linux-x64 && zip -qr "$OUT/win-x64.zip" win-x64 )

echo "$VERSION" > "$OUT/version"
echo "built linux + win @ $VERSION"
