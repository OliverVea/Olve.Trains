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

### Command Reference

Use `help` to see all available commands. Key commands:

**place-track** - Place track segments between two points with optional direction control.
```bash
# Straight track (directions auto-calculated from start to end)
--send "place-track start=0,0.125,0 end=4,0.125,0"

# Curved track (explicit directions for curves)
--send "place-track start=0,0.125,0 end=4,0.125,4 start-dir=east end-dir=north"
```

Directions: `north`, `south`, `east`, `west` (or `n`, `s`, `e`, `w`)

**place-vehicle** - Place a vehicle (train) on a track.
```bash
--send "place-vehicle track=<track-id>"
```

**screenshot** - Take a screenshot.
```bash
--send "screenshot path=~/screenshot.png"
```

### Example: Creating a 4x4 Circle Track with Train

Build a complete circular track loop in 4 segments (4 units per side) and place a train:

```bash
# Start game in listening mode
dotnet run --project src/Olve.Trains/Olve.Trains.csproj -- --listen &

# Wait for game to initialize
sleep 3

# Build the 4x4 square loop (Y=0.125 is track height)
# Bottom edge: east then curve to north
dotnet run --project src/Olve.Trains/Olve.Trains.csproj -- --send "place-track start=0,0.125,0 end=4,0.125,0 start-dir=east end-dir=north"

# Right edge: north then curve to west
dotnet run --project src/Olve.Trains/Olve.Trains.csproj -- --send "place-track start=4,0.125,0 end=4,0.125,4 start-dir=north end-dir=west"

# Top edge: west then curve to south
dotnet run --project src/Olve.Trains/Olve.Trains.csproj -- --send "place-track start=4,0.125,4 end=0,0.125,4 start-dir=west end-dir=south"

# Left edge: south then curve to east (completing the loop)
dotnet run --project src/Olve.Trains/Olve.Trains.csproj -- --send "place-track start=0,0.125,4 end=0,0.125,0 start-dir=south end-dir=east"

# Place a train on the first track segment (use track ID from place-track output)
dotnet run --project src/Olve.Trains/Olve.Trains.csproj -- --send "place-vehicle track=<track-id-from-output>"

# Take a screenshot
dotnet run --project src/Olve.Trains/Olve.Trains.csproj -- --send "screenshot path=~/circle-track.png"
```

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

## Olve.* Packages

Documentation base URL: https://olivervea.github.io/Olve.Utilities/

API docs follow the pattern: `https://olivervea.github.io/Olve.Utilities/api/{Namespace}.html`

Key packages used in this project:

- **Olve.Results** - Result type for error handling (used throughout the codebase)
- **Olve.Paths** - Path manipulation inspired by Python's pathlib (API: https://olivervea.github.io/Olve.Utilities/api/Olve.Paths.html)
- **Olve.Utilities** - General utilities

### Olve.Paths Example

```csharp
using Olve.Paths;

// Create paths
var path = Paths.Path.Create("/home/user/documents");
var file = Paths.Path.Create("screenshots/image.png");

// Path operations
var parent = path.Parent;                    // /home/user
var joined = path / "subfolder" / "file.txt"; // Path joining with /
var absolute = file.Absolute;                // Resolves to absolute path

// File system checks
if (path.Exists()) { ... }
path.TryGetElementType(out var elementType); // Directory, File, etc.

// Get special paths
var cwd = Paths.Path.GetCurrentDirectory();
var home = Paths.Path.GetHomeDirectory();
Paths.Path.TryGetAssemblyExecutable(out var exe);
```

## Notes

### OpenGL State Management
The `RenderingManager2D` properly restores depth testing state after rendering to prevent state contamination between 3D and 2D rendering passes.
