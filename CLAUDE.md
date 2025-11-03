# Development Guide for Claude

## Building and Running the Project

### Asset Pipeline (IMPORTANT!)

**You must run the asset pipeline after modifying shaders or layouts.**

```bash
cd src/Olve.Trains.AssetPipeline
dotnet run
```

This compiles:
- Shaders from `src/Olve.Trains/resources/shaders/` → `src/Olve.Trains/assets/Shaders/`
- Layouts from `src/Olve.Trains/resources/layouts/` → `src/Olve.Trains/assets/Layouts/`

Configuration is in `src/Olve.Trains.AssetPipeline/Properties/appsettings.local.json`.

### Running the Application

```bash
dotnet run --project src/Olve.Trains/Olve.Trains.csproj
```

## Workflow

1. **Modify shaders/layouts** in `src/Olve.Trains/resources/`
2. **Compile assets**: `cd src/Olve.Trains.AssetPipeline && dotnet run`
3. **Run app**: `dotnet run --project src/Olve.Trains/Olve.Trains.csproj`

## Notes

### OpenGL State Management
The `RenderingManager2D` properly restores depth testing state after rendering to prevent state contamination between 3D and 2D rendering passes.
