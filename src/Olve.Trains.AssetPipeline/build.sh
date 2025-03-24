#!/usr/bin/env bash

cd "$(dirname "$0")/.." || exit

# Clean the temp directory
rm -rf ./Olve.Trains.AssetPipeline/temp

# Build the docker image
docker build -f "./Olve.Trains.AssetPipeline/Dockerfile" -t "olve-trains-asset-pipeline" .

# Run the docker image
docker run \
   -v "$(pwd)/Olve.Trains.AssetPipeline/temp:/app/temp" \
   -v "$(pwd)/Olve.Trains/assets:/app/output" \
   -v "$(pwd)/Olve.Trains/Shaders:/app/shaders:ro" \
   --env-file "$(pwd)/Olve.Trains.AssetPipeline/.env" \
   olve-trains-asset-pipeline \
   .
