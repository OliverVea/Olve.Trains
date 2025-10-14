
## Usage

1) Configuration sources (IConfiguration)
- The Asset Pipeline reads configuration via Microsoft.Extensions.Configuration with providers in this order:
  - appsettings.json (optional)
  - User secrets (optional)
  - Environment variables (including values loaded from .env)
  - Command line arguments

You can configure everything via appsettings.json or environment variables.

Examples
- appsettings.json (optional):
  {
    "Logging": { "LogLevel": { "Default": "Information" } },
    "AssetPipeline": {
      "S3": { "Bucket": "...", "Key": "...", "Secret": "...", "TimeoutMs": 20000, "AllowFailure": false },
      "Shaders": { "Directory": "C:\\path\\to\\shaders" },
      "Layouts": { "Directory": "C:\\path\\to\\layouts", "Namespace": "Olve.Trains.resources.layouts" },
      "Build": { "Targets": "all" }
    }
  }

- .env (example; use double underscore to denote section nesting):
  AssetPipeline__S3__Bucket=...
  AssetPipeline__S3__Key=...
  AssetPipeline__S3__Secret=...
  AssetPipeline__S3__TimeoutMs=20000
  AssetPipeline__S3__AllowFailure=false
  AssetPipeline__Shaders__Directory=...
  AssetPipeline__Layouts__Directory=...
  AssetPipeline__Layouts__Namespace=Olve.Trains.resources.layouts
  AssetPipeline__Build__Targets=all
  Logging__LogLevel__Default=Information

Legacy environment variables are still supported for backward compatibility:
- S3_BUCKET, S3_KEY, S3_SECRET
- S3_TIMEOUT_MS, ALLOW_S3_FAILURE
- ASSET_BUILD_TARGETS
- ASSET_SHADERS_DIR, ASSET_LAYOUTS_DIR, ASSET_LAYOUTS_NAMESPACE
- LOG_LEVEL

A template file exists: [.env.template](./.env.template).

2) Run the asset pipeline
- From the repository root:
  - dotnet run --project src\Olve.Trains.AssetPipeline

3) Optional CLI overrides (deprecated; prefer .env)
- Build targets (defaults to all if none specified):
  - --shaders
  - --meshes
  - --textures
  - --terrains
  - --layouts
  - --all

- S3 options:
  - --s3-timeout <milliseconds>
  - --allow-s3-failure

- Shader options:
  - --shaders-dir <path>          or --shaders-dir=<path>

- Layout options:
  - --layouts-dir <path>          or --layouts-dir=<path>
  - --layouts-namespace <ns>      or --layouts-namespace=<ns>

4) Build the game
- Run dotnet build in the Olve.Trains project to compile the game once assets are generated.
] Load assets from S3 bucket
  - [ ] Process shaders with shader slang
  - [ ] Process assets with Silk.NET.Assimp
  - [ ] Write metadata source files
- [ ] GitHub Actions
  - [ ] Connect to tailnet + set end node
  - [ ] Run Asset Pipeline
  - [ ] (run tests)
  - [ ] Build project
  - [ ] Push assets to S3 bucket
