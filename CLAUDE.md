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

## Testing (IMPORTANT!)

**Always run integration tests before committing.**

```bash
# Quick validation (headless, CI-style — requires Xvfb on Linux)
bash scripts/validate-build.sh

# Full integration test (native window)
bash scripts/integration-test.sh --file ~/test-screenshots/test.png

# Full integration test (headless)
bash scripts/integration-test.sh --windowing xvfb --file ~/test-screenshots/test.png

# Skip rebuild if you've already built
bash scripts/integration-test.sh --skip-build --file ~/test-screenshots/test.png
```

Both scripts use the named pipe command system to launch the game, place tracks/buildings/trains, and validate via screenshots.

Unit tests (TUnit framework):
```bash
dotnet run --project tests/Olve.Engine3D.Tests/Olve.Engine3D.Tests.csproj
```

## Architecture Reference

See [docs/architecture.md](docs/architecture.md) for detailed codebase architecture: project structure, rendering pipeline, scene system, GUI system, command system, entity management, CI/CD.

## Workflow

### Epic-Driven Development

All work must be tied to an epic in `TODO.md`. Before starting any task, identify which epic and step it belongs to. If the work doesn't map to an existing epic, either find the right one or discuss with the user first.

**Two modes of work:**

1. **Exploration** — triggered by open-ended questions, "let's explore...", investigating options, or generating new epics. Free of epic constraints but the agent must explicitly state it is in exploration mode. No commits expected.
2. **Epic work** — any task that produces code changes. Must be tied to a specific epic and step in `TODO.md`. If the work doesn't map to an existing epic, either find the right one or discuss with the user first.

**Epic types:** Feature, Technical, Visual, Tooling, Testing. Annotated in parentheses after the epic name in `TODO.md`.

**Commits:**
- Each commit should map to a step (checkbox) in an epic.
- In the same commit as the code changes, check off the completed step in `TODO.md`.
- When all steps under a parent item are checked, check the parent too.
- When all items in an epic are checked, the epic is done — move it to the "Done" section.

**Priority:** Work top-to-bottom in `TODO.md`. The topmost incomplete epic/task is always the next priority.

**Milestone semantics:** Demo and v1.0 items are all *required* for that release. v1.1 items are loose future ideas, not concrete.

**Questioning untracked work:**
- If about to do work that doesn't map to any epic step, stop and clarify with the user.
- Ad-hoc fixes and refactors are fine if they support an epic step — just note which one.

### Asset Pipeline

When modifying shaders or layouts:

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

Each package has two documentation resources:

- **README (general guidance)**: `https://olivervea.github.io/Olve.Utilities/src/{Package}/README.html`
  - Start here. Contains usage philosophy, deprecation notices, and best practices.
- **API docs (technical reference)**: `https://olivervea.github.io/Olve.Utilities/api/{Namespace}.html`
  - Detailed type/method signatures and parameters.

Key packages used in this project:

- **Olve.Results** - Result type for error handling (used throughout the codebase)
- **Olve.Paths** - Path manipulation inspired by Python's pathlib
- **Olve.Utilities** - General utilities (Id<T>, DictionaryExtensions, etc.)

### Olve.Results Quick Reference

The primary error handling pattern. **Always use `TryPickProblems`**, not `.Failed`/`.Succeeded`/`.Value`.

```csharp
// Valueless result — check for failure
if (DoSomething().TryPickProblems(out var problems))
{
    return problems.Prepend("Context about what failed");
}

// Valued result — get value on success, problems on failure
if (GetValue().TryPickProblems(out var problems, out var value))
{
    return problems;
}
// 'value' is safe to use here
```

**Creating problems** — use format strings with `{0}`, `{1}`, NOT interpolation:
```csharp
return new ResultProblem("Failed to parse '{0}' as {1}", input, typeName);  // correct
return new ResultProblem($"Failed to parse '{input}'");                      // WRONG
```

**Composition:**
- `Result.Chain(a, b, c)` — sequential dependent steps. Stops on first failure.
- `Result.Concat(a, b, c)` — independent steps. Aggregates all problems.
- `.Map(x => transform(x))` — transform value (non-Result function)
- `.Bind(x => getResult(x))` — transform value (Result-returning function)
- `.ToEmptyResult()` — discard value, keep success/failure status
- `.Prepend("context")` — add hierarchical error context when propagating

**DeletionResult** — three states for delete operations:
```csharp
return DeletionResult.Success();    // deleted
return DeletionResult.NotFound();   // entity didn't exist
return DeletionResult.Error(...);   // something went wrong
```

**Implicit conversions** — `ResultProblem` converts to `Result`/`Result<T>`:
```csharp
return new ResultProblem("Something failed");  // works directly, no wrapper needed
```

### Olve.Utilities.Ids Quick Reference

Type-safe identifiers backed by `Guid`. Prevents mixing IDs of different entity types.

```csharp
// Random new ID (runtime entities)
var trackId = Id.New<Track>();

// Deterministic ID from name (stable/reproducible — layouts, blueprints)
var elementId = Id.FromName<GuiElement>("toolbar/button/delete");

// Parse from string (user input, commands)
if (Id.TryParse<Track>(inputString, out var parsedId)) { ... }
```

**Rules:**
- Use `Id<T>` (typed) everywhere, not bare `Id` (untyped)
- Use `Id.New<T>()` for runtime-created entities
- Use `Id.FromName<T>(name)` for stable identifiers that must be the same across runs
- Use `Id.TryParse<T>()` for parsing — never `Guid.Parse()`
- For display/logging, pass the `Id<T>` directly (has `ToString()`)

### Olve.Paths Quick Reference

Path manipulation inspired by Python's pathlib. Prefer over `System.IO.Path`.

```csharp
// Path joining with / operator
var joined = basePath / "subfolder" / "file.txt";

// Filesystem operations (use these, not System.IO equivalents)
path.Parent                      // instead of Path.GetDirectoryName()
path.Name                        // instead of Path.GetFileName()
path.Exists()                    // instead of File.Exists() / Directory.Exists()
path.EnsurePathExists()          // instead of Directory.CreateDirectory()
path.TryGlob("**/*.cs", out _)  // instead of Directory.GetFiles()

// Special paths
var home = Path.GetHomeDirectory();
Path.TryGetAssemblyExecutable(out var exe);
```

## Code Organization Rules

### Data Classes Must Be Behavior-Free
Data classes and structs (e.g. `MeshData`, `LineStripData`, `BuildingPosition`) hold data only — no logic, no dependencies on other namespaces. **Never** add methods that reference types from outside the data class's own domain (e.g. rendering interfaces on an asset data class).

- **Services/managers** for logic and orchestration
- **Data classes/structs** for holding data (properties, validation at most)
- **Static helpers and extension methods** for mapping, conversion, and cross-cutting utilities (e.g. `MeshData` → vertex buffer population belongs in a rendering extension method, not on `MeshData` itself)

## Notes

### OpenGL State Management
The `RenderingManager2D` properly restores depth testing state after rendering to prevent state contamination between 3D and 2D rendering passes.

### Matrix Memory Layout
`Silk.NET.Maths.Matrix4X4<T>` is stored **row-major** (matching `System.Numerics`). `M41`/`M42`/`M43` hold translation (row 4, cols 1-3). When writing matrix data to GPU buffers (vertex attributes, instance data), use **row-major order** (`M11, M12, M13, M14, M21, ...`) — the same order as `Matrix4X4.CopyTo()` and `UniformMatrix4(transpose: false)`. Do NOT write column-major (`M11, M21, M31, M41, ...`) — this transposes the matrix and produces incorrect transforms.
