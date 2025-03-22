#!/usr/bin/env bash

cd "$(dirname "$0")/.." || exit

# Clean the temp directory
rm -rf ./Olve.Engine3D.AssetPipeline/temp

# Build the docker image
docker build -f "./Olve.Engine3D.AssetPipeline/Dockerfile" -t "olve-engine3d-asset-pipeline" .

# Run the docker image
docker run \
   -v "$(pwd)/Olve.Engine3D.AssetPipeline/temp:/app/temp" \
   -v "$(pwd)/Olve.Trains/assets:/app/output" \
   -v "$(pwd)/Olve.Trains/Shaders:/app/shaders:ro" \
   --env-file "$(pwd)/Olve.Engine3D.AssetPipeline/.env" \
   olve-engine3d-asset-pipeline \
   .
