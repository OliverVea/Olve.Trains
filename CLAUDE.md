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

### Detached Mode (Command Pipe)

The game supports a named pipe command interface for external tooling (AI agents, replay testing).

**Start with listening enabled:**
```bash
dotnet run --project src/Olve.Trains/Olve.Trains.csproj -- --listen
# Or with a specific instance ID:
dotnet run --project src/Olve.Trains/Olve.Trains.csproj -- --listen --instance my-game
```

**Send commands to a running instance:**
```bash
dotnet run --project src/Olve.Trains/Olve.Trains.csproj -- --send "echo message=hello"
# Target a specific instance:
dotnet run --project src/Olve.Trains/Olve.Trains.csproj -- --send "help" --instance my-game
```

**Enable via config** (`appsettings.json`):
```json
{ "Detached": { "Listen": true } }
```

Listening is off by default. Commands are processed on the main game thread via a `CommandProcessingService` scene service.

## Workflow

1. **Modify shaders/layouts** in `src/Olve.Trains/resources/`
2. **Compile assets**: `cd src/Olve.Trains.AssetPipeline && dotnet run`
3. **Run app**: `dotnet run --project src/Olve.Trains/Olve.Trains.csproj`

## Package Management

### Upgrade outdated packages

```bash
dotnet outdated -u
```

### Remove unused package references

```bash
pkg-trim --sln-dir . --fix
```

## Notes

### OpenGL State Management
The `RenderingManager2D` properly restores depth testing state after rendering to prevent state contamination between 3D and 2D rendering passes.
