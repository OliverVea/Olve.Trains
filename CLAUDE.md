# Development Guide for Claude

## Building and Running the Project

### Asset Pipeline (IMPORTANT!)

**You must run the asset pipeline after modifying shaders or layouts.** See the `asset-pipeline` skill for full reference.

```bash
cd src/Olve.Trains.AssetPipeline && dotnet run
```

### Running the Application

```bash
dotnet run --project src/Olve.Trains/Olve.Trains.csproj
```

See the `run-game` skill for detached mode, command reference, replay system, and the Game Python class.

## Quality Gate (IMPORTANT!)

**Run `bash scripts/quality.sh` green before raising a CR.** This is a local, agent-runnable self-check — it is *not* wired into the CD pipeline. Running it before you raise a CR catches boundary/quality regressions while the change is still yours to fix.

```bash
bash scripts/quality.sh            # architecture fitness tests (blocking) + metrics ratchet
QUALITY_STRICT=1 bash scripts/quality.sh   # also make the metrics ratchet blocking
```

What it enforces:
- **Architecture fitness tests** (`tests/Olve.Architecture.Tests/`, NetArchTest) — **blocking**. Encode the layering rules: the engine must not depend on the game; `Scenes/GameLogic` must not depend on `Scenes/GameRendering`/`GameUI`. Add a rule here whenever you introduce a boundary that must hold.
- **Code-metrics ratchet** (`scripts/metrics-ratchet.py` vs `tools/code-metrics/baseline.json`) — **advisory** for now. Flags a touched file losing >5 Maintainability Index or gaining class coupling, and new files below the MI/complexity bar. Metrics come from the real Roslyn engine (`tools/code-metrics/`); see `docs/architecture-diagram/CODE_METRICS.md`.
- Assumes a buildable tree (run the asset pipeline first if you changed shaders/layouts). If a metric change is **intentional**, regenerate the baseline with `bash scripts/metrics-refresh.sh` and commit it in the same change.

## Testing (IMPORTANT!)

**Always run integration tests before committing.** See the `testing` skill for full reference.

```bash
# Quick run (skip build if already built)
bash scripts/integration-test.sh --skip-build

# Windows
bash scripts/integration-test.sh --windowing native

# Unit tests
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

### Keeping Skills and CLAUDE.md Up to Date (IMPORTANT!)

Skills (`.claude/skills/`) and `CLAUDE.md` are reference documentation that must **always** reflect the current state of the codebase. If you make changes that invalidate information in a skill or in `CLAUDE.md`, you **must** update it in the same commit. This includes:

- Adding/removing/renaming commands, fixtures, test scenarios, pipeline stages, scene services, events, etc.
- Changing APIs documented in skills (Game class methods, Result patterns, scene registration, etc.)
- Modifying workflows or processes described in skills (build steps, test commands, replay system, etc.)

Stale documentation is worse than no documentation — it causes incorrect code to be written. Both `CLAUDE.md` and skills are vital to keep current, but **`CLAUDE.md` is the highest priority** — it is loaded by every agent and conversation, so outdated information here has the widest impact.

**Write current state, not change history.** When updating documentation, describe how things work *now*. Never leave behind references to how things used to work — just replace the old description with the new one.

- Bad: "Bar used to be a child of Foo requiring a FooConfig, but it has been changed to require a BarConfig."
- Good: "Bar requires a BarConfig."

### Asset Pipeline

See the `asset-pipeline` skill for full reference. Short version: after modifying shaders/layouts or when S3 assets change, run `cd src/Olve.Trains.AssetPipeline && dotnet run`, then build, then run tests.

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

See the `olve-results` skill for the full API. Key rule: **always use `TryPickProblems`**, not `.Failed`/`.Succeeded`/`.Value`.

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

## Design Principles

### Observability
Every player-visible action must be **loggable** and the full game state must be **queryable** via the command interface. This enables replay files, debugging, and AI-assisted testing.

- **Logging**: When a player action mutates game state (placing/deleting tracks, buildings, trains, wagons, etc.), log enough detail to reconstruct the action as a replay command. Use `info` level for state-changing actions, `debug` for transient events (tool switches, hover, etc.).
- **Query commands**: Every entity type must have `list-<entity>` and `query-<entity>` commands that return full state as JSON. When adding a new entity type, add these commands as part of the feature.
- **Replay-ability**: The combination of logs + query commands should make it possible to reconstruct the full game state at any point.

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
