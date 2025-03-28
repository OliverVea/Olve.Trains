# Change directory to the script location
$scriptPath = Split-Path -Parent $MyInvocation.MyCommand.Definition
Set-Location "$scriptPath\.."

# Remove the temp directory if it exists
$TempDir = ".\Olve.Trains.AssetPipeline\temp"
if (Test-Path $TempDir) {
    Remove-Item -Recurse -Force $TempDir
}

# Build the Docker image
docker build -f ".\Olve.Trains.AssetPipeline\Dockerfile" -t "olve-trains-asset-pipeline" .

# Run the Docker image
docker run `
    -v "$(Get-Location)\Olve.Trains.AssetPipeline\temp:/app/temp" `
    -v "$(Get-Location)\Olve.Trains\assets:/app/output" `
    -v "$(Get-Location)\Olve.Trains\Shaders:/app/shaders:ro" `
    --env-file "$(Get-Location)\Olve.Trains.AssetPipeline\.env" `
    olve-trains-asset-pipeline `
    .
