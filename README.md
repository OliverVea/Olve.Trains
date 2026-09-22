# Olve.Trains — *On Track To Grow*

A train-network builder game, built from scratch in **C# on a custom OpenGL 3D engine**.
No game engine (no Unity/Godot) — the rendering, scene, GUI, asset, and command systems are
all hand-written on top of raw OpenGL via [Silk.NET](https://github.com/dotnet/Silk.NET).

> ~44k lines of C#. A personal project exploring real-time graphics, engine architecture,
> and end-to-end tooling — from GLSL shaders through a code-generating asset pipeline to
> Kubernetes deployment.

<!-- TODO: add a gameplay screenshot/GIF here — the integration test already captures one:
     bash scripts/integration-test.sh --file docs/screenshot.png
     then reference it: ![On Track To Grow](docs/screenshot.png) -->

## What it demonstrates

- **A custom 3D engine (`Olve.Engine3D`)** — a reusable library the game is built on:
  rendering, scenes, camera, lighting, physics, GUI, input, an event system, and a
  command interface. Cleanly separated from the game (`Olve.Trains`).
- **A unified, fully-instanced rendering pipeline** — one `RenderingManager` drives all 2D
  and 3D drawing through instanced draw calls, sorted render groups, and per-group render
  state (blend/depth). **Compile-time-safe geometry/instance IDs** (`GeometryId<TVertex>`,
  `GroupId<TInstance>`) prevent mixing vertex/instance types.
- **Interesting rendering techniques** — Hermite-spline track meshes, **MSDF** text
  rendering, GUI rectangles with borders/rounded corners in-shader, and procedural
  heightmap terrain with an SDF grid overlay (flat normals via `dFdx`/`dFdy`).
- **A code-generating asset pipeline (`Olve.Trains.AssetPipeline`)** — reads GLSL shaders
  and XML GUI layouts and generates strongly-typed C# (uniforms, `Vertex`/`Instance`
  records with GPU attribute configuration, layout builders) via Scriban templates. Shaders
  and UI become type-safe C# APIs instead of stringly-typed calls.
- **Observability by design** — every state-changing player action is logged in
  reconstructable detail, and full game state is queryable as JSON via the command
  interface. Together this powers **replay files, debugging, and AI-driven testing**.
- **Serious testing & CI** — TUnit unit tests plus an **end-to-end integration test** that
  compiles assets, builds, launches the game **headless** (xvfb), places entities, and
  captures a screenshot as a CI artifact for visual inspection.
- **Full delivery pipeline** — Native AOT builds, containerized, deployed to Kubernetes via
  [Olve.Pipelines](https://github.com/OliverVea/Olve.Pipelines) (GitOps); assets committed
  via git LFS.

## Tech stack

C# / .NET · **Silk.NET** (OpenGL, Maths, Input, Windowing, Assimp) · GLSL · Scriban
(codegen) · MSDF fonts · TUnit · Docker · Kubernetes / Helm · git LFS.

## Architecture at a glance

```
src/
  Olve.Engine3D/            # Custom engine: rendering, scenes, GUI, camera, physics,
                           #   lighting, input, events, commands, math
  Olve.Trains/             # The game — "On Track To Grow"
  Olve.Trains.AssetPipeline/  # GLSL + XML layouts → strongly-typed C# (Scriban)
tests/
  Olve.Engine3D.Tests/     # Engine unit tests (TUnit)
  Olve.Trains.Tests/       # Game tests (save serialization, etc.)
```

See [docs/architecture.md](docs/architecture.md) for the rendering pipeline, scene system,
GUI, command system, and entity management in depth.

## Building & running

```bash
# 1. Compile assets (required after changing shaders/layouts)
cd src/Olve.Trains.AssetPipeline && dotnet run

# 2. Run the game
dotnet run --project src/Olve.Trains/Olve.Trains.csproj
```

## Platform Support

| Platform | Status |
|---|---|
| Windows | Supported |
| Linux | Supported |
| macOS | Planned |
| Nintendo Switch | Planned |
| Xbox Series | Planned |
| PlayStation 5 | Planned |
| iOS / Android | Planned |

See [docs/platform-strategy.md](docs/platform-strategy.md) for priority analysis and porting notes.

## Requirements

- MSVC for AssImp in Olve.Trains.AssetPipeline

## Validation

An integration test script verifies the full pipeline end-to-end: asset compilation, build,
headless game launch, entity placement, and screenshot capture.

```bash
# Full integration test (native window)
bash scripts/integration-test.sh --file ~/test-screenshots/test.png

# Full integration test (headless)
bash scripts/integration-test.sh --windowing xvfb --file ~/test-screenshots/test.png
```

This runs automatically in CI as the `validate-screenshot` job after the build completes.
The screenshot is uploaded as a build artifact for visual inspection.

## TODO

See [TODO.md](TODO.md).
