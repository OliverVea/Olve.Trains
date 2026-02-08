#!/usr/bin/env bash

cd "$(dirname "$0")/../.." || exit

# Clean the temp directory
rm -rf ./src/Olve.Trains.AssetPipeline/temp

# Build the docker image
docker build -f "./src/Olve.Trains.AssetPipeline/Dockerfile" -t "olve-trains-asset-pipeline" .

# Run the docker image
docker run \
   -v "$(pwd)/src/Olve.Trains.AssetPipeline/temp:/app/temp" \
   -v "$(pwd)/src/Olve.Trains/assets:/app/output" \
   -v "$(pwd)/src/Olve.Trains/resources/shaders:/app/shaders:ro" \
   --env-file "$(pwd)/src/Olve.Trains.AssetPipeline/.env" \
   olve-trains-asset-pipeline \
   . "$@"
