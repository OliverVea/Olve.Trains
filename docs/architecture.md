# Architecture Reference

Detailed reference for the Olve.Trains codebase. See [CLAUDE.md](../CLAUDE.md) for build/run instructions and workflow conventions.

## Project Structure

```
src/
  Olve.Engine3D/           # Core engine library (rendering, scenes, GUI, commands)
  Olve.Trains/             # Main game application ("On Track To Grow")
  Olve.Trains.AssetPipeline/  # Asset compiler (shaders + layouts → C# classes)
tests/
  Olve.Engine3D.Tests/     # Unit tests (TUnit framework)
scripts/
  validate-build.sh        # CI screenshot validation (headless)
  integration-test.sh      # Integration test with station, tracks, trains, time-of-day screenshots
```

**Dependency graph:**
- `Olve.Trains` → `Olve.Engine3D`
- `Olve.Trains.AssetPipeline` → `Olve.Engine3D`
- `Olve.Engine3D.Tests` → `Olve.Engine3D`

## Rendering Pipeline

### Shader Inventory

8 shader pairs in `src/Olve.Trains/resources/shaders/`:

| Shader | Instanced Attributes | Purpose |
|--------|---------------------|---------|
| track | `iP0`, `iP1`, `iT0`, `iT1` (vec3s) | Hermite spline track rendering |
| building | `iWorld` (mat4) | Cube-based buildings with diffuse lighting |
| default | none | Standard mesh with texture + normals |
| lineStrip | none | Lines with per-vertex color |
| msdfText | `iPosPx`, `iSizePx`, `iTint`, `iUvMin`, `iUvMax` | MSDF text glyph rendering |
| texturedRectangle | `iPosPx`, `iSizePx`, `iTint`, `iBorderWidthPx`, `iBorderColor`, `iBorderRadiusPx` | GUI rectangles with borders/rounded corners |
| terrain | none (uses `gl_VertexID`) | Procedural heightmap terrain + grid overlay |
| terrainWireframe | none | Wireframe terrain mode |

### Asset Pipeline Code Generation

The pipeline reads GLSL shaders and generates C# classes via Scriban templates (`src/Olve.Trains.AssetPipeline/templates/ShaderClass.scriban`). Each shader gets:

1. **Uniform properties** — nullable, settable (e.g., `public Vector3D<float>? UColor { get; set; }`)
2. **`EntityParameters` record** — implements `IShaderParameters` for per-entity overrides
3. **`Vertex` struct** — implements `IVertexData` (non-instanced inputs): `FloatCount`, `WriteTo(Span<float>)`, `ConfigureAttributes(GL)`
4. **`Instance` struct** — implements `IInstanceData` (instanced inputs, marked with `// @instanced` in GLSL): same API as Vertex but with `gl.VertexAttribDivisor(location, 1)`

Layout XML files generate C# builder classes via `LayoutClass.scriban`. Each layout produces a record with named element properties and a flat `IReadOnlyList<GuiElement>` of all descendants.

### RenderingManager3D

3D rendering with shared geometry and multiple instances. File: `src/Olve.Engine3D/Rendering/RenderingManager3D.cs`

**Geometry registration** (once per shape):
```csharp
Result<GeometryId> RegisterGeometry<T>(ReadOnlySpan<T> vertices, ReadOnlySpan<uint> indices) where T : IVertexData
Result<GeometryId> RegisterGeometry<T>(ReadOnlySpan<T> vertices, ReadOnlySpan<uint> indices, PrimitiveType, BufferUsageARB)
Result<GeometryId> RegisterGeometry<T>(ReadOnlySpan<T> vertices, PrimitiveType, BufferUsageARB)
Result<GeometryId> RegisterDrawArraysGeometry(uint vertexCount, PrimitiveType)  // no VBO (e.g., terrain)
Result UpdateGeometry<T>(GeometryId, ReadOnlySpan<T> vertices)
Result DeregisterGeometry(GeometryId)
```

**Instance management** (many per geometry):
```csharp
Result<RenderingInstanceId> RegisterInstance(GeometryId, RenderingId<ShaderData>, Matrix4X4<float> worldMatrix)
Result DeregisterInstance(RenderingInstanceId)
Result SetInstanceWorld(RenderingInstanceId, Matrix4X4<float>)
Result<Matrix4X4<float>> GetInstanceWorld(RenderingInstanceId)
Result SetInstanceParameters(RenderingInstanceId, IShaderParameters?)  // per-instance overrides with auto-restore
```

### RenderingManager2D

2D instanced rendering with depth-sorted painter's algorithm. File: `src/Olve.Engine3D/Rendering/RenderingManager2D.cs`

```csharp
Result<RenderingInstanceId> Register<T>(RenderingId<ShaderData>, T instanceData, float depth, IShaderParameters?) where T : IInstanceData
Result Update<T>(RenderingInstanceId, T instanceData, float depth, IShaderParameters?)
Result UpdateShaderParameters(RenderingInstanceId, IShaderParameters?)
Result Deregister(RenderingInstanceId)
```

Each 2D element gets its own VAO + instance VBO. Rendered `OrderByDescending(depth)`.

### Primitives

`src/Olve.Engine3D/Rendering/Primitives/`:

- **`UnitCube`** — 24 vertices (4/face), 36 indices. Positions + normals for unit cube [0,0,0]→[1,1,1].
- **`RegisterUnitCube<TVertex>`** extension on `RenderingManager3D` — takes a `Func<position, normal, TVertex>` factory.
- `UnitQuad` and `UnitLineSamples` do not exist yet (TODO items).

### Key Types

- **`GeometryId`** — wraps `Id`, created via `GeometryId.New()`
- **`RenderingInstanceId`** — identifies a specific instance
- **`RenderState`** — `record struct(BlendMode, DepthWrite, DepthTest)` with presets: `Opaque`, `AlphaBlend`, `AlphaBlendNoDepthWrite`, etc.
- **`PrimitiveType`** — from Silk.NET: `Triangles`, `LineStrip`, `LineLoop`, etc.

## Entity Management

**`EntityStore<T>`** (composition pattern): wraps `IdDictionary<T>` with `TryAdd`, `Set`, `Remove`, `TryGet`, `Exists`, `Where`. Exposes `OnAdded`/`OnRemoved` events. Services compose this directly and expose domain-specific event names (e.g., `OnTrackAdded`).

## Scene System

4 scenes with parent-child hierarchy:

| Scene | Layer | Parent | Role |
|-------|-------|--------|------|
| MainMenuScene | 0 | (root) | Main menu UI |
| GameLogicScene | 0 | (root) | Game state, tracks, vehicles, lighting |
| GameRenderingScene | 1 | GameLogicScene | 3D rendering |
| GameUIScene | 2 | GameRenderingScene | In-game UI overlays |

**Key concepts:**
- **`SceneDefinition`**: `Id<IScene>`, `Name`, `LayerOrder`, optional `ParentId`
- **DI scoping**: root scenes create a new `IServiceScope`; child scenes share parent's scope
- **Lifecycle**: `Unloaded → Inactive → Active → Inactive → Unloaded`
- **Service ordering**: by `Priority` (ascending). `SceneServicePriority.FromDependencies(...)` / `FromDependents(...)`
- **State guard**: `Update()` and `Render()` check `State == Active` before each service — if a service unloads the scene mid-iteration, remaining services are skipped
- **`EventSceneService<T>`**: bridges events to scene lifecycle — subscribes on load, queues, processes on update, unsubscribes on unload

**Service registration**: `AddSceneService<T>(sceneId)` / `AddEventSceneService(...)`

**Loading**: `LoadAndActivateScene(sceneId)` walks parent chain, loads parent-first. Child scenes loaded via parent automatically get the parent's scope.

## GUI System

### Layout Files

XML in `src/Olve.Trains/resources/layouts/` (MainMenu.xml, ToolBar.xml, InfoBar.xml).

```xml
<Box id="Root" Justify="Justify.Center" Align="Align.Center" Vertical="true" Gap="4">
    <Box id="StartGameButton" Style="MainMenuButtonStyle" Interactive="true">
        <Text Content='"Start Game"' Style="MainMenuButtonTextStyle" InheritParentState="true" />
    </Box>
</Box>
```

Generated to `src/Olve.Trains/assets/Layouts/` as C# records with named properties. Element IDs use `Id.FromName<GuiElement>("Layout/Type/Name")` for deterministic stable IDs.

### Element Types

In `src/Olve.Engine3D/GUI/Elements/`:

- **Box** — flexbox-like container: width/height/weight/gap/margin/padding, Justify (Start/Center/End/SpaceBetween/Around/Evenly), Align, Vertical, background/border/radius. Implements `IRenderableAsRectangle`.
- **Text** — string content, font, size, color, alignment. Implements `IRenderableAsText` + `IRenderableAsRectangle`.
- **Image** — texture reference. Implements `IRenderableAsRectangle`.

### Core Services

In `src/Olve.Engine3D/GUI/`:

- **Element management**: `GuiElementService`, `GuiNodeService`, `GuiElementRegistrations`
- **Layout**: `GuiLayoutService` / `GuiLayoutUpdateService` — computes positions and sizes
- **Rendering**: `GuiDepthService`, `GuiAnchorService`
- **Input**: `GuiMouseInputService` (priority -100), `GuiFocusService`, `GuiActivationService`, `GuiCollisionService` (AABB hit testing)
- **Styling**: `GuiStyleApplierService`, `GuiAnimationService`, `GuiNodeStateService` (hover/focus/active)
- **Game styles**: `GameStyleService` + `Styles.cs` define color schemes, fonts, borders

## Command System (Detached Mode)

Infrastructure in `src/Olve.Engine3D/Commands/`:

- **`ICommandHandler`**: `Verb`, `HelpString`, `Arguments`, `Handle(CommandContext) → Result<CommandOutput>`
- **`CommandRunner`**: resolves handlers from `CommandHandlerServiceCollection`, returns `Result<CommandOutput>`
- **Named pipe IPC**: `CommandPipeServer` (background thread) + `CommandPipeClient` (static). Protocol: 4-byte length prefix + MemoryPack payload (`CommandRequest`/`CommandResponse`)
- **`CommandQueue`**: `ConcurrentQueue<PendingCommand>` with `TaskCompletionSource<CommandResponse>`. Pipe thread enqueues, main thread dequeues.
- **`CommandProcessingService`**: scene service (priority -1000, registered in MainMenu + GameLogic) that drains the queue each update tick
- **`GameInstanceId`**: determines pipe name `olve-trains-{id}`
- **CLI args**: `--listen` / `--send "cmd"` / `--instance "id"`

## Building System

- **`Building`** (record struct): `Id<Building>` + `Id<BuildingBlueprint>` + `BuildingPosition` (TilePosition + CardinalDirection)
- **`BuildingBlueprint`** (record struct): `Id<BuildingBlueprint>` + description + `TileFootprint` (W/H/D)
- **`BuildingBlueprintLibraryService`**: predefined blueprints (Station: 4x2x2)
- **`StationPlacingToolService`**: raycast to terrain, ghost preview at 50% opacity, rotate with 'R', click to place
- **`BuildingRenderingService`**: unit cubes scaled/rotated via world matrices through `RenderingManager3D`
- Files: logic in `Scenes/GameLogic/Industries/`, rendering in `Scenes/GameRendering/`, UI in `Scenes/GameUI/Tools/`

## CI/CD

GitHub Actions workflow: `.github/workflows/push_master.yml` (triggers on push/PR to master).

3 jobs:
1. **build-assets** (Ubuntu) — runs asset pipeline, uploads built assets artifact
2. **build-dotnet** (matrix: linux-x64 + win-x64) — downloads assets, builds Release
3. **validate-screenshot** (Ubuntu headless) — Xvfb + Mesa software OpenGL, runs `scripts/validate-build.sh --skip-build`, uploads screenshot artifact

Headless rendering uses `LIBGL_ALWAYS_SOFTWARE=1` + Xvfb virtual framebuffer.

## Testing

### Unit Tests

`tests/Olve.Engine3D.Tests/` using **TUnit** framework. Run with `dotnet run` (TUnit Exe runner). Tests cover GUI layout computation (`GuiLayoutServiceTests`, `GuiLayoutServicePositioningTests`) and spline sandboxes.

### Integration Test Scripts

**`scripts/validate-build.sh`** — CI validation: starts game headless, places track loop + vehicle, takes screenshot, validates file size > 1000 bytes. Used in the GitHub Actions workflow.

**`scripts/integration-test.sh`** — full integration test: places station, builds track loop around it, spawns 3 trains, waits for simulation, takes screenshots at 3 times of day (7:30, 11:30, 22:30). Supports `--s3` upload, `--file` local save, `--windowing xvfb` for headless, `--skip-build`.

Both scripts use the named pipe command system to control the game.